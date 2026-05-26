using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs;
using SmartHome.Models;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/product-search")]
public class ProductSearchController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductSearchController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-class/{classNodeId:int}")]
    public async Task<ActionResult<IEnumerable<ProductWithParametersDto>>> GetProductsByClass(int classNodeId)
    {
        var classNode = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == classNodeId);
        if (classNode == null)
            return NotFound($"Класс изделия с id={classNodeId} не найден.");

        var products = await _context.Products
            .Include(x => x.ClassNode)
            .Where(x => x.ClassNodeId == classNodeId)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var result = new List<ProductWithParametersDto>();

        foreach (var product in products)
        {
            var parameters = await BuildProductParameterValues(product.Id, product.ClassNodeId)
                .ToListAsync();

            result.Add(new ProductWithParametersDto
            {
                Id = product.Id,
                Name = product.Name,
                ShortName = product.ShortName,
                ClassNodeId = product.ClassNodeId,
                ClassNodeName = product.ClassNode.Name,
                Parameters = parameters
            });
        }

        return Ok(result);
    }

    [HttpGet("by-class/{classNodeId:int}/with-descendants")]
    public async Task<ActionResult<IEnumerable<ProductWithParametersDto>>> GetProductsByClassWithDescendants(int classNodeId)
    {
        var classNode = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == classNodeId);
        if (classNode == null)
            return NotFound($"Класс изделия с id={classNodeId} не найден.");

        var classIds = await GetDescendantClassIds(classNodeId);
        classIds.Add(classNodeId);

        var products = await _context.Products
            .Include(x => x.ClassNode)
            .Where(x => classIds.Contains(x.ClassNodeId))
            .OrderBy(x => x.ClassNode.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var result = new List<ProductWithParametersDto>();

        foreach (var product in products)
        {
            var parameters = await BuildProductParameterValues(product.Id, product.ClassNodeId)
                .ToListAsync();

            result.Add(new ProductWithParametersDto
            {
                Id = product.Id,
                Name = product.Name,
                ShortName = product.ShortName,
                ClassNodeId = product.ClassNodeId,
                ClassNodeName = product.ClassNode.Name,
                Parameters = parameters
            });
        }

        return Ok(result);
    }

    [HttpPost("filter")]
    public async Task<ActionResult<IEnumerable<ProductWithParametersDto>>> Filter(ProductFilterDto dto)
    {
        var classNode = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == dto.ClassNodeId);
        if (classNode == null)
            return NotFound($"Класс изделия с id={dto.ClassNodeId} не найден.");

        var query = _context.Products
            .Include(x => x.ClassNode)
            .Where(x => x.ClassNodeId == dto.ClassNodeId);

        if (dto.ClassParameterId.HasValue)
        {
            var classParameter = await _context.ClassParameters
                .Include(x => x.ParameterDefinition)
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.ClassParameterId.Value &&
                    x.ClassNodeId == dto.ClassNodeId);

            if (classParameter == null)
                return BadRequest("Указанный параметр не принадлежит выбранному классу изделия.");

            var validationError = ValidateFilterForParameter(classParameter.ParameterDefinition.ValueType, dto);
            if (validationError != null)
                return BadRequest(validationError);

            query = ApplyParameterFilter(query, classParameter, dto);
        }

        var products = await query
            .OrderBy(x => x.Name)
            .ToListAsync();

        var result = new List<ProductWithParametersDto>();

        foreach (var product in products)
        {
            var parameters = await BuildProductParameterValues(product.Id, product.ClassNodeId)
                .ToListAsync();

            result.Add(new ProductWithParametersDto
            {
                Id = product.Id,
                Name = product.Name,
                ShortName = product.ShortName,
                ClassNodeId = product.ClassNodeId,
                ClassNodeName = product.ClassNode.Name,
                Parameters = parameters
            });
        }

        return Ok(result);
    }

    [HttpPost("filter-with-descendants")]
    public async Task<ActionResult<IEnumerable<ProductWithParametersDto>>> FilterWithDescendants(ProductFilterDto dto)
    {
        var classNode = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == dto.ClassNodeId);
        if (classNode == null)
            return NotFound($"Класс изделия с id={dto.ClassNodeId} не найден.");

        var classIds = await GetDescendantClassIds(dto.ClassNodeId);
        classIds.Add(dto.ClassNodeId);

        var query = _context.Products
            .Include(x => x.ClassNode)
            .Where(x => classIds.Contains(x.ClassNodeId));

        if (dto.ClassParameterId.HasValue)
        {
            var classParameter = await _context.ClassParameters
                .Include(x => x.ParameterDefinition)
                .FirstOrDefaultAsync(x => x.Id == dto.ClassParameterId.Value);

            if (classParameter == null)
                return BadRequest($"Параметр класса с id={dto.ClassParameterId.Value} не найден.");

            var validationError = ValidateFilterForParameter(classParameter.ParameterDefinition.ValueType, dto);
            if (validationError != null)
                return BadRequest(validationError);

            query = ApplyParameterFilter(query, classParameter, dto);
        }

        var products = await query
            .OrderBy(x => x.ClassNode.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var result = new List<ProductWithParametersDto>();

        foreach (var product in products)
        {
            var parameters = await BuildProductParameterValues(product.Id, product.ClassNodeId)
                .ToListAsync();

            result.Add(new ProductWithParametersDto
            {
                Id = product.Id,
                Name = product.Name,
                ShortName = product.ShortName,
                ClassNodeId = product.ClassNodeId,
                ClassNodeName = product.ClassNode.Name,
                Parameters = parameters
            });
        }

        return Ok(result);
    }

    private IQueryable<Product> ApplyParameterFilter(
        IQueryable<Product> query,
        ClassParameter classParameter,
        ProductFilterDto dto)
    {
        var parameterType = classParameter.ParameterDefinition.ValueType;
        var classParameterId = classParameter.Id;

        return parameterType switch
        {
            ParameterValueType.Integer => query.Where(product =>
                _context.ProductParameterValues.Any(value =>
                    value.ProductId == product.Id &&
                    value.ClassParameterId == classParameterId &&
                    (!dto.IntegerValue.HasValue || value.IntegerValue == dto.IntegerValue.Value))),

            ParameterValueType.Number => query.Where(product =>
                _context.ProductParameterValues.Any(value =>
                    value.ProductId == product.Id &&
                    value.ClassParameterId == classParameterId &&
                    (!dto.NumberFrom.HasValue || value.NumberValue >= dto.NumberFrom.Value) &&
                    (!dto.NumberTo.HasValue || value.NumberValue <= dto.NumberTo.Value))),

            ParameterValueType.String => query.Where(product =>
                _context.ProductParameterValues.Any(value =>
                    value.ProductId == product.Id &&
                    value.ClassParameterId == classParameterId &&
                    value.StringValue != null &&
                    dto.StringContains != null &&
                    value.StringValue.ToLower().Contains(dto.StringContains.ToLower()))),

            ParameterValueType.DateTime => query.Where(product =>
                _context.ProductParameterValues.Any(value =>
                    value.ProductId == product.Id &&
                    value.ClassParameterId == classParameterId &&
                    (!dto.DateFrom.HasValue || value.DateTimeValue >= dto.DateFrom.Value) &&
                    (!dto.DateTo.HasValue || value.DateTimeValue <= dto.DateTo.Value))),

            ParameterValueType.Enum => query.Where(product =>
                _context.ProductParameterValues.Any(value =>
                    value.ProductId == product.Id &&
                    value.ClassParameterId == classParameterId &&
                    value.EnumValueId == dto.EnumValueId)),

            _ => query
        };
    }

    private IQueryable<ProductParameterValueDto> BuildProductParameterValues(
        int productId,
        int classNodeId)
    {
        return _context.ClassParameters
            .Include(x => x.ParameterGroup)
            .Include(x => x.ParameterDefinition)
                .ThenInclude(x => x.MeasureUnit)
            .Include(x => x.ParameterDefinition)
                .ThenInclude(x => x.EnumClass)
            .Where(x => x.ClassNodeId == classNodeId)
            .GroupJoin(
                _context.ProductParameterValues
                    .Include(v => v.Product)
                    .Include(v => v.EnumValue)
                    .Where(v => v.ProductId == productId),
                classParameter => classParameter.Id,
                productValue => productValue.ClassParameterId,
                (classParameter, productValues) => new
                {
                    ClassParameter = classParameter,
                    ProductValue = productValues.FirstOrDefault()
                })
            .OrderBy(x => x.ClassParameter.SortOrder)
            .ThenBy(x => x.ClassParameter.ParameterDefinition.Name)
            .Select(x => new ProductParameterValueDto
            {
                Id = x.ProductValue != null ? x.ProductValue.Id : 0,
                ProductId = productId,
                ProductName = x.ProductValue != null ? x.ProductValue.Product.Name : string.Empty,
                ClassParameterId = x.ClassParameter.Id,
                ParameterDefinitionId = x.ClassParameter.ParameterDefinitionId,
                ParameterName = x.ClassParameter.ParameterDefinition.Name,
                ParameterShortName = x.ClassParameter.ParameterDefinition.ShortName,
                ValueType = x.ClassParameter.ParameterDefinition.ValueType,
                ParameterGroupId = x.ClassParameter.ParameterGroupId,
                ParameterGroupName = x.ClassParameter.ParameterGroup != null ? x.ClassParameter.ParameterGroup.Name : null,
                SortOrder = x.ClassParameter.SortOrder,
                IsRequired = x.ClassParameter.IsRequired,
                MinNumberValue = x.ClassParameter.MinNumberValue,
                MaxNumberValue = x.ClassParameter.MaxNumberValue,
                MeasureUnitId = x.ClassParameter.ParameterDefinition.MeasureUnitId,
                MeasureUnitName = x.ClassParameter.ParameterDefinition.MeasureUnit != null
                    ? x.ClassParameter.ParameterDefinition.MeasureUnit.Name
                    : null,
                MeasureUnitShortName = x.ClassParameter.ParameterDefinition.MeasureUnit != null
                    ? x.ClassParameter.ParameterDefinition.MeasureUnit.ShortName
                    : null,
                EnumClassId = x.ClassParameter.ParameterDefinition.EnumClassId,
                EnumClassName = x.ClassParameter.ParameterDefinition.EnumClass != null
                    ? x.ClassParameter.ParameterDefinition.EnumClass.Name
                    : null,
                IntegerValue = x.ProductValue != null ? x.ProductValue.IntegerValue : null,
                NumberValue = x.ProductValue != null ? x.ProductValue.NumberValue : null,
                StringValue = x.ProductValue != null ? x.ProductValue.StringValue : null,
                DateTimeValue = x.ProductValue != null ? x.ProductValue.DateTimeValue : null,
                EnumValueId = x.ProductValue != null ? x.ProductValue.EnumValueId : null,
                EnumDisplayName = x.ProductValue != null && x.ProductValue.EnumValue != null
                    ? x.ProductValue.EnumValue.DisplayName
                    : null,
                EnumStringValue = x.ProductValue != null && x.ProductValue.EnumValue != null
                    ? x.ProductValue.EnumValue.StringValue
                    : null,
                EnumNumberValue = x.ProductValue != null && x.ProductValue.EnumValue != null
                    ? x.ProductValue.EnumValue.NumberValue
                    : null,
                EnumIconValue = x.ProductValue != null && x.ProductValue.EnumValue != null
                    ? x.ProductValue.EnumValue.IconValue
                    : null
            });
    }

    private static string? ValidateFilterForParameter(
        ParameterValueType valueType,
        ProductFilterDto dto)
    {
        return valueType switch
        {
            ParameterValueType.Integer when dto.IntegerValue == null
                => "Для фильтрации по параметру Integer необходимо указать IntegerValue.",

            ParameterValueType.Number when dto.NumberFrom == null && dto.NumberTo == null
                => "Для фильтрации по параметру Number необходимо указать NumberFrom или NumberTo.",

            ParameterValueType.String when string.IsNullOrWhiteSpace(dto.StringContains)
                => "Для фильтрации по параметру String необходимо указать StringContains.",

            ParameterValueType.DateTime when dto.DateFrom == null && dto.DateTo == null
                => "Для фильтрации по параметру DateTime необходимо указать DateFrom или DateTo.",

            ParameterValueType.Enum when dto.EnumValueId == null
                => "Для фильтрации по параметру Enum необходимо указать EnumValueId.",

            ParameterValueType.Number when dto.NumberFrom != null && dto.NumberTo != null && dto.NumberFrom > dto.NumberTo
                => "NumberFrom не может быть больше NumberTo.",

            ParameterValueType.DateTime when dto.DateFrom != null && dto.DateTo != null && dto.DateFrom > dto.DateTo
                => "DateFrom не может быть больше DateTo.",

            _ => null
        };
    }

    private async Task<List<int>> GetDescendantClassIds(int classNodeId)
    {
        var allNodes = await _context.ClassNodes
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.ParentId
            })
            .ToListAsync();

        var result = new List<int>();

        void CollectChildren(int currentId)
        {
            var children = allNodes
                .Where(x => x.ParentId == currentId)
                .Select(x => x.Id)
                .ToList();

            foreach (var childId in children)
            {
                result.Add(childId);
                CollectChildren(childId);
            }
        }

        CollectChildren(classNodeId);

        return result;
    }
}