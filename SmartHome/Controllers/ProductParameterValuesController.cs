using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs;
using SmartHome.Models;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/products/{productId:int}/parameters")]
public class ProductParameterValuesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductParameterValuesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductParameterValueDto>>> GetByProduct(int productId)
    {
        var product = await _context.Products
            .Include(x => x.ClassNode)
            .FirstOrDefaultAsync(x => x.Id == productId);

        if (product == null)
            return NotFound($"Продукт с id={productId} не найден.");

        var parameters = await BuildProductParameterValues(productId, product.ClassNodeId)
            .ToListAsync();

        return Ok(parameters);
    }

    [HttpGet("card")]
    public async Task<ActionResult<ProductWithParametersDto>> GetProductCard(int productId)
    {
        var product = await _context.Products
            .Include(x => x.ClassNode)
            .FirstOrDefaultAsync(x => x.Id == productId);

        if (product == null)
            return NotFound($"Продукт с id={productId} не найден.");

        var parameters = await BuildProductParameterValues(productId, product.ClassNodeId)
            .ToListAsync();

        var result = new ProductWithParametersDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortName = product.ShortName,
            ClassNodeId = product.ClassNodeId,
            ClassNodeName = product.ClassNode.Name,
            Parameters = parameters
        };

        return Ok(result);
    }

    [HttpPut("{classParameterId:int}")]
    public async Task<ActionResult<ProductParameterValueDto>> WriteValue(
        int productId,
        int classParameterId,
        WriteProductParameterValueDto dto)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == productId);

        if (product == null)
            return NotFound($"Продукт с id={productId} не найден.");

        var classParameter = await _context.ClassParameters
            .Include(x => x.ParameterDefinition)
            .FirstOrDefaultAsync(x =>
                x.Id == classParameterId &&
                x.ClassNodeId == product.ClassNodeId);

        if (classParameter == null)
            return BadRequest("Параметр не принадлежит классу данного продукта.");

        var validationError = await ValidateValue(classParameter, dto);
        if (validationError != null)
            return BadRequest(validationError);

        var value = await _context.ProductParameterValues
            .FirstOrDefaultAsync(x =>
                x.ProductId == productId &&
                x.ClassParameterId == classParameterId);

        if (value == null)
        {
            value = new ProductParameterValue
            {
                ProductId = productId,
                ClassParameterId = classParameterId
            };

            _context.ProductParameterValues.Add(value);
        }

        value.IntegerValue = null;
        value.NumberValue = null;
        value.StringValue = null;
        value.DateTimeValue = null;
        value.EnumValueId = null;

        switch (classParameter.ParameterDefinition.ValueType)
        {
            case ParameterValueType.Integer:
                value.IntegerValue = dto.IntegerValue;
                break;

            case ParameterValueType.Number:
                value.NumberValue = dto.NumberValue;
                break;

            case ParameterValueType.String:
                value.StringValue = dto.StringValue;
                break;

            case ParameterValueType.DateTime:
                value.DateTimeValue = dto.DateTimeValue;
                break;

            case ParameterValueType.Enum:
                value.EnumValueId = dto.EnumValueId;
                break;

            default:
                return BadRequest("Неизвестный тип параметра.");
        }

        await _context.SaveChangesAsync();

        var result = await BuildProductParameterValues(productId, product.ClassNodeId)
            .FirstAsync(x => x.ClassParameterId == classParameterId);

        return Ok(result);
    }

    [HttpDelete("{classParameterId:int}")]
    public async Task<IActionResult> DeleteValue(int productId, int classParameterId)
    {
        var value = await _context.ProductParameterValues
            .FirstOrDefaultAsync(x =>
                x.ProductId == productId &&
                x.ClassParameterId == classParameterId);

        if (value == null)
            return NotFound("Значение параметра для данного продукта не найдено.");

        _context.ProductParameterValues.Remove(value);
        await _context.SaveChangesAsync();

        return NoContent();
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

    private async Task<string?> ValidateValue(
        ClassParameter classParameter,
        WriteProductParameterValueDto dto)
    {
        var filledFieldsCount = 0;

        if (dto.IntegerValue != null) filledFieldsCount++;
        if (dto.NumberValue != null) filledFieldsCount++;
        if (!string.IsNullOrWhiteSpace(dto.StringValue)) filledFieldsCount++;
        if (dto.DateTimeValue != null) filledFieldsCount++;
        if (dto.EnumValueId != null) filledFieldsCount++;

        if (filledFieldsCount == 0)
            return "Необходимо указать значение параметра.";

        if (filledFieldsCount > 1)
            return "Можно указать только одно значение параметра в соответствии с его типом.";

        var parameterDefinition = classParameter.ParameterDefinition;

        switch (parameterDefinition.ValueType)
        {
            case ParameterValueType.Integer:
                return ValidateIntegerValue(classParameter, dto);

            case ParameterValueType.Number:
                return ValidateNumberValue(classParameter, dto);

            case ParameterValueType.String:
                return ValidateStringValue(dto);

            case ParameterValueType.DateTime:
                return ValidateDateTimeValue(dto);

            case ParameterValueType.Enum:
                return await ValidateEnumValue(parameterDefinition, dto);

            default:
                return "Неизвестный тип параметра.";
        }
    }

    private static string? ValidateIntegerValue(
        ClassParameter classParameter,
        WriteProductParameterValueDto dto)
    {
        if (dto.IntegerValue == null)
            return "Для параметра типа Integer необходимо указать IntegerValue.";

        if (dto.NumberValue != null || dto.StringValue != null || dto.DateTimeValue != null || dto.EnumValueId != null)
            return "Для параметра типа Integer нельзя указывать другие поля значения.";

        if (classParameter.MinNumberValue != null && dto.IntegerValue < classParameter.MinNumberValue)
            return $"Значение меньше минимально допустимого: {classParameter.MinNumberValue}.";

        if (classParameter.MaxNumberValue != null && dto.IntegerValue > classParameter.MaxNumberValue)
            return $"Значение больше максимально допустимого: {classParameter.MaxNumberValue}.";

        return null;
    }

    private static string? ValidateNumberValue(
        ClassParameter classParameter,
        WriteProductParameterValueDto dto)
    {
        if (dto.NumberValue == null)
            return "Для параметра типа Number необходимо указать NumberValue.";

        if (dto.IntegerValue != null || dto.StringValue != null || dto.DateTimeValue != null || dto.EnumValueId != null)
            return "Для параметра типа Number нельзя указывать другие поля значения.";

        if (classParameter.MinNumberValue != null && dto.NumberValue < classParameter.MinNumberValue)
            return $"Значение меньше минимально допустимого: {classParameter.MinNumberValue}.";

        if (classParameter.MaxNumberValue != null && dto.NumberValue > classParameter.MaxNumberValue)
            return $"Значение больше максимально допустимого: {classParameter.MaxNumberValue}.";

        return null;
    }

    private static string? ValidateStringValue(WriteProductParameterValueDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StringValue))
            return "Для параметра типа String необходимо указать StringValue.";

        if (dto.IntegerValue != null || dto.NumberValue != null || dto.DateTimeValue != null || dto.EnumValueId != null)
            return "Для параметра типа String нельзя указывать другие поля значения.";

        return null;
    }

    private static string? ValidateDateTimeValue(WriteProductParameterValueDto dto)
    {
        if (dto.DateTimeValue == null)
            return "Для параметра типа DateTime необходимо указать DateTimeValue.";

        if (dto.IntegerValue != null || dto.NumberValue != null || dto.StringValue != null || dto.EnumValueId != null)
            return "Для параметра типа DateTime нельзя указывать другие поля значения.";

        return null;
    }

    private async Task<string?> ValidateEnumValue(
        ParameterDefinition parameterDefinition,
        WriteProductParameterValueDto dto)
    {
        if (dto.EnumValueId == null)
            return "Для параметра типа Enum необходимо указать EnumValueId.";

        if (dto.IntegerValue != null || dto.NumberValue != null || dto.StringValue != null || dto.DateTimeValue != null)
            return "Для параметра типа Enum нельзя указывать другие поля значения.";

        if (parameterDefinition.EnumClassId == null)
            return "Для параметра типа Enum не указан EnumClassId.";

        var enumValue = await _context.EnumValues
            .FirstOrDefaultAsync(x => x.Id == dto.EnumValueId.Value);

        if (enumValue == null)
            return $"Значение перечисления с id={dto.EnumValueId.Value} не найдено.";

        if (enumValue.EnumClassId != parameterDefinition.EnumClassId.Value)
            return "Выбранное значение перечисления не принадлежит перечислению, связанному с этим параметром.";

        return null;
    }
}