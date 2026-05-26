using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs;
using SmartHome.Models;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/classnodes/{classNodeId:int}/parameters")]
public class ClassParametersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClassParametersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassParameterDto>>> GetByClass(int classNodeId)
    {
        var classExists = await _context.ClassNodes.AnyAsync(x => x.Id == classNodeId);
        if (!classExists)
            return NotFound($"Класс изделия с id={classNodeId} не найден.");

        var parameters = await _context.ClassParameters
            .Include(x => x.ClassNode)
            .Include(x => x.ParameterGroup)
            .Include(x => x.ParameterDefinition)
                .ThenInclude(x => x.MeasureUnit)
            .Include(x => x.ParameterDefinition)
                .ThenInclude(x => x.EnumClass)
            .Where(x => x.ClassNodeId == classNodeId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ParameterDefinition.Name)
            .Select(x => new ClassParameterDto
            {
                Id = x.Id,
                ClassNodeId = x.ClassNodeId,
                ClassNodeName = x.ClassNode.Name,
                ParameterDefinitionId = x.ParameterDefinitionId,
                ParameterName = x.ParameterDefinition.Name,
                ParameterShortName = x.ParameterDefinition.ShortName,
                ValueType = x.ParameterDefinition.ValueType,
                MeasureUnitId = x.ParameterDefinition.MeasureUnitId,
                MeasureUnitName = x.ParameterDefinition.MeasureUnit != null ? x.ParameterDefinition.MeasureUnit.Name : null,
                MeasureUnitShortName = x.ParameterDefinition.MeasureUnit != null ? x.ParameterDefinition.MeasureUnit.ShortName : null,
                EnumClassId = x.ParameterDefinition.EnumClassId,
                EnumClassName = x.ParameterDefinition.EnumClass != null ? x.ParameterDefinition.EnumClass.Name : null,
                ParameterGroupId = x.ParameterGroupId,
                ParameterGroupName = x.ParameterGroup != null ? x.ParameterGroup.Name : null,
                SortOrder = x.SortOrder,
                IsRequired = x.IsRequired,
                IsInherited = x.IsInherited,
                MinNumberValue = x.MinNumberValue,
                MaxNumberValue = x.MaxNumberValue
            })
            .ToListAsync();

        return Ok(parameters);
    }

    [HttpGet("{parameterId:int}")]
    public async Task<ActionResult<ClassParameterDto>> GetById(int classNodeId, int parameterId)
    {
        var parameter = await _context.ClassParameters
            .Include(x => x.ClassNode)
            .Include(x => x.ParameterGroup)
            .Include(x => x.ParameterDefinition)
                .ThenInclude(x => x.MeasureUnit)
            .Include(x => x.ParameterDefinition)
                .ThenInclude(x => x.EnumClass)
            .Where(x => x.ClassNodeId == classNodeId && x.Id == parameterId)
            .Select(x => new ClassParameterDto
            {
                Id = x.Id,
                ClassNodeId = x.ClassNodeId,
                ClassNodeName = x.ClassNode.Name,
                ParameterDefinitionId = x.ParameterDefinitionId,
                ParameterName = x.ParameterDefinition.Name,
                ParameterShortName = x.ParameterDefinition.ShortName,
                ValueType = x.ParameterDefinition.ValueType,
                MeasureUnitId = x.ParameterDefinition.MeasureUnitId,
                MeasureUnitName = x.ParameterDefinition.MeasureUnit != null ? x.ParameterDefinition.MeasureUnit.Name : null,
                MeasureUnitShortName = x.ParameterDefinition.MeasureUnit != null ? x.ParameterDefinition.MeasureUnit.ShortName : null,
                EnumClassId = x.ParameterDefinition.EnumClassId,
                EnumClassName = x.ParameterDefinition.EnumClass != null ? x.ParameterDefinition.EnumClass.Name : null,
                ParameterGroupId = x.ParameterGroupId,
                ParameterGroupName = x.ParameterGroup != null ? x.ParameterGroup.Name : null,
                SortOrder = x.SortOrder,
                IsRequired = x.IsRequired,
                IsInherited = x.IsInherited,
                MinNumberValue = x.MinNumberValue,
                MaxNumberValue = x.MaxNumberValue
            })
            .FirstOrDefaultAsync();

        if (parameter == null)
            return NotFound($"Параметр класса с id={parameterId} не найден для класса id={classNodeId}.");

        return Ok(parameter);
    }

    [HttpPost]
    public async Task<ActionResult<ClassParameterDto>> Create(
        int classNodeId,
        CreateClassParameterDto dto)
    {
        var classNode = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == classNodeId);
        if (classNode == null)
            return NotFound($"Класс изделия с id={classNodeId} не найден.");

        var parameterDefinition = await _context.ParameterDefinitions
            .FirstOrDefaultAsync(x => x.Id == dto.ParameterDefinitionId);

        if (parameterDefinition == null)
            return BadRequest($"Описание параметра с id={dto.ParameterDefinitionId} не найдено.");

        var duplicateExists = await _context.ClassParameters.AnyAsync(x =>
            x.ClassNodeId == classNodeId &&
            x.ParameterDefinitionId == dto.ParameterDefinitionId);

        if (duplicateExists)
            return BadRequest("Этот параметр уже привязан к указанному классу изделия.");

        if (dto.ParameterGroupId.HasValue)
        {
            var groupExists = await _context.ParameterGroups.AnyAsync(x => x.Id == dto.ParameterGroupId.Value);
            if (!groupExists)
                return BadRequest($"Группа параметров с id={dto.ParameterGroupId.Value} не найдена.");
        }

        var validationError = ValidateClassParameterRestrictions(parameterDefinition.ValueType, dto.MinNumberValue, dto.MaxNumberValue);
        if (validationError != null)
            return BadRequest(validationError);

        var classParameter = new ClassParameter
        {
            ClassNodeId = classNodeId,
            ParameterDefinitionId = dto.ParameterDefinitionId,
            ParameterGroupId = dto.ParameterGroupId,
            SortOrder = dto.SortOrder,
            IsRequired = dto.IsRequired,
            IsInherited = false,
            MinNumberValue = dto.MinNumberValue,
            MaxNumberValue = dto.MaxNumberValue
        };

        _context.ClassParameters.Add(classParameter);
        await _context.SaveChangesAsync();

        await CopyParameterToDescendantsInternal(classNodeId, classParameter.Id);

        var result = await BuildClassParameterDtoQuery()
            .FirstAsync(x => x.Id == classParameter.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { classNodeId, parameterId = result.Id },
            result);
    }

    [HttpPut("{parameterId:int}")]
    public async Task<ActionResult<ClassParameterDto>> Update(
        int classNodeId,
        int parameterId,
        UpdateClassParameterDto dto)
    {
        var classParameter = await _context.ClassParameters
            .Include(x => x.ParameterDefinition)
            .FirstOrDefaultAsync(x => x.ClassNodeId == classNodeId && x.Id == parameterId);

        if (classParameter == null)
            return NotFound($"Параметр класса с id={parameterId} не найден для класса id={classNodeId}.");

        if (dto.ParameterGroupId.HasValue)
        {
            var groupExists = await _context.ParameterGroups.AnyAsync(x => x.Id == dto.ParameterGroupId.Value);
            if (!groupExists)
                return BadRequest($"Группа параметров с id={dto.ParameterGroupId.Value} не найдена.");
        }

        var validationError = ValidateClassParameterRestrictions(
            classParameter.ParameterDefinition.ValueType,
            dto.MinNumberValue,
            dto.MaxNumberValue);

        if (validationError != null)
            return BadRequest(validationError);

        classParameter.ParameterGroupId = dto.ParameterGroupId;
        classParameter.SortOrder = dto.SortOrder;
        classParameter.IsRequired = dto.IsRequired;
        classParameter.MinNumberValue = dto.MinNumberValue;
        classParameter.MaxNumberValue = dto.MaxNumberValue;

        await _context.SaveChangesAsync();

        var result = await BuildClassParameterDtoQuery()
            .FirstAsync(x => x.Id == classParameter.Id);

        return Ok(result);
    }
    
    [HttpPost("copy-from-parent")]
    public async Task<ActionResult<IEnumerable<ClassParameterDto>>> CopyFromParent(int classNodeId)
    {
        var classNode = await _context.ClassNodes
            .FirstOrDefaultAsync(x => x.Id == classNodeId);

        if (classNode == null)
            return NotFound($"Класс изделия с id={classNodeId} не найден.");

        if (classNode.ParentId == null)
            return BadRequest("У корневого класса нет родительского класса.");

        var parentParameters = await _context.ClassParameters
            .Where(x => x.ClassNodeId == classNode.ParentId.Value)
            .ToListAsync();

        if (!parentParameters.Any())
            return BadRequest("У родительского класса нет параметров для наследования.");

        foreach (var parentParameter in parentParameters)
        {
            var alreadyExists = await _context.ClassParameters.AnyAsync(x =>
                x.ClassNodeId == classNodeId &&
                x.ParameterDefinitionId == parentParameter.ParameterDefinitionId);

            if (alreadyExists)
                continue;

            var inheritedParameter = new ClassParameter
            {
                ClassNodeId = classNodeId,
                ParameterDefinitionId = parentParameter.ParameterDefinitionId,
                ParameterGroupId = parentParameter.ParameterGroupId,
                SortOrder = parentParameter.SortOrder,
                IsRequired = parentParameter.IsRequired,
                IsInherited = true,
                MinNumberValue = parentParameter.MinNumberValue,
                MaxNumberValue = parentParameter.MaxNumberValue
            };

            _context.ClassParameters.Add(inheritedParameter);
        }

        await _context.SaveChangesAsync();

        var result = await BuildClassParameterDtoQuery()
            .Where(x => x.ClassNodeId == classNodeId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ParameterName)
            .ToListAsync();

        return Ok(result);
    }
    [HttpPost("{parameterId:int}/copy-to-descendants")]
    public async Task<ActionResult<IEnumerable<ClassParameterDto>>> CopyToDescendants(
        int classNodeId,
        int parameterId)
    {
        var sourceParameter = await _context.ClassParameters
            .FirstOrDefaultAsync(x => x.ClassNodeId == classNodeId && x.Id == parameterId);

        if (sourceParameter == null)
            return NotFound($"Параметр класса с id={parameterId} не найден для класса id={classNodeId}.");

        var descendantIds = await GetDescendantClassIds(classNodeId);

        if (!descendantIds.Any())
            return BadRequest("У класса нет дочерних классов для наследования параметра.");

        foreach (var descendantId in descendantIds)
        {
            var alreadyExists = await _context.ClassParameters.AnyAsync(x =>
                x.ClassNodeId == descendantId &&
                x.ParameterDefinitionId == sourceParameter.ParameterDefinitionId);

            if (alreadyExists)
                continue;

            var inheritedParameter = new ClassParameter
            {
                ClassNodeId = descendantId,
                ParameterDefinitionId = sourceParameter.ParameterDefinitionId,
                ParameterGroupId = sourceParameter.ParameterGroupId,
                SortOrder = sourceParameter.SortOrder,
                IsRequired = sourceParameter.IsRequired,
                IsInherited = true,
                MinNumberValue = sourceParameter.MinNumberValue,
                MaxNumberValue = sourceParameter.MaxNumberValue
            };

            _context.ClassParameters.Add(inheritedParameter);
        }

        await _context.SaveChangesAsync();

        var result = await BuildClassParameterDtoQuery()
            .Where(x => descendantIds.Contains(x.ClassNodeId))
            .OrderBy(x => x.ClassNodeId)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.ParameterName)
            .ToListAsync();

        return Ok(result);
    }

    [HttpDelete("{parameterId:int}")]
    public async Task<IActionResult> Delete(int classNodeId, int parameterId)
    {
        var classParameter = await _context.ClassParameters
            .Include(x => x.ProductValues)
            .FirstOrDefaultAsync(x => x.ClassNodeId == classNodeId && x.Id == parameterId);

        if (classParameter == null)
            return NotFound($"Параметр класса с id={parameterId} не найден для класса id={classNodeId}.");

        if (classParameter.IsInherited)
            return BadRequest("Нельзя удалить унаследованный параметр напрямую. Удалите или измените параметр в родительском классе.");
        if (classParameter.ProductValues.Any())
            return BadRequest("Нельзя удалить параметр класса, потому что у продуктов уже есть значения этого параметра.");

        var descendantIds = await GetDescendantClassIds(classNodeId);

        var inheritedCopies = await _context.ClassParameters
            .Include(x => x.ProductValues)
            .Where(x =>
                descendantIds.Contains(x.ClassNodeId) &&
                x.ParameterDefinitionId == classParameter.ParameterDefinitionId &&
                x.IsInherited)
            .ToListAsync();

        var inheritedCopiesWithValues = inheritedCopies
            .Where(x => x.ProductValues.Any())
            .ToList();

        if (inheritedCopiesWithValues.Any())
            return BadRequest("Нельзя удалить параметр, потому что у продуктов дочерних классов уже есть значения унаследованного параметра.");

        _context.ClassParameters.RemoveRange(inheritedCopies);
        _context.ClassParameters.Remove(classParameter);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private IQueryable<ClassParameterDto> BuildClassParameterDtoQuery()
    {
        return _context.ClassParameters
            .Include(x => x.ClassNode)
            .Include(x => x.ParameterGroup)
            .Include(x => x.ParameterDefinition)
                .ThenInclude(x => x.MeasureUnit)
            .Include(x => x.ParameterDefinition)
                .ThenInclude(x => x.EnumClass)
            .Select(x => new ClassParameterDto
            {
                Id = x.Id,
                ClassNodeId = x.ClassNodeId,
                ClassNodeName = x.ClassNode.Name,
                ParameterDefinitionId = x.ParameterDefinitionId,
                ParameterName = x.ParameterDefinition.Name,
                ParameterShortName = x.ParameterDefinition.ShortName,
                ValueType = x.ParameterDefinition.ValueType,
                MeasureUnitId = x.ParameterDefinition.MeasureUnitId,
                MeasureUnitName = x.ParameterDefinition.MeasureUnit != null ? x.ParameterDefinition.MeasureUnit.Name : null,
                MeasureUnitShortName = x.ParameterDefinition.MeasureUnit != null ? x.ParameterDefinition.MeasureUnit.ShortName : null,
                EnumClassId = x.ParameterDefinition.EnumClassId,
                EnumClassName = x.ParameterDefinition.EnumClass != null ? x.ParameterDefinition.EnumClass.Name : null,
                ParameterGroupId = x.ParameterGroupId,
                ParameterGroupName = x.ParameterGroup != null ? x.ParameterGroup.Name : null,
                SortOrder = x.SortOrder,
                IsRequired = x.IsRequired,
                IsInherited = x.IsInherited,
                MinNumberValue = x.MinNumberValue,
                MaxNumberValue = x.MaxNumberValue
            });
    }

    private static string? ValidateClassParameterRestrictions(
        ParameterValueType valueType,
        decimal? minNumberValue,
        decimal? maxNumberValue)
    {
        if (valueType is not ParameterValueType.Number and not ParameterValueType.Integer)
        {
            if (minNumberValue != null || maxNumberValue != null)
                return "Минимальное и максимальное значения можно задавать только для численных параметров.";
        }

        if (minNumberValue != null && maxNumberValue != null && minNumberValue > maxNumberValue)
            return "Минимальное значение не может быть больше максимального.";

        return null;
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

    private async Task CopyParameterToDescendantsInternal(int classNodeId, int parameterId)
    {
        var sourceParameter = await _context.ClassParameters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClassNodeId == classNodeId && x.Id == parameterId);

        if (sourceParameter == null)
            return;

        var descendantIds = await GetDescendantClassIds(classNodeId);

        foreach (var descendantId in descendantIds)
        {
            var alreadyExists = await _context.ClassParameters.AnyAsync(x =>
                x.ClassNodeId == descendantId &&
                x.ParameterDefinitionId == sourceParameter.ParameterDefinitionId);

            if (alreadyExists)
                continue;

            var inheritedParameter = new ClassParameter
            {
                ClassNodeId = descendantId,
                ParameterDefinitionId = sourceParameter.ParameterDefinitionId,
                ParameterGroupId = sourceParameter.ParameterGroupId,
                SortOrder = sourceParameter.SortOrder,
                IsRequired = sourceParameter.IsRequired,
                IsInherited = true,
                MinNumberValue = sourceParameter.MinNumberValue,
                MaxNumberValue = sourceParameter.MaxNumberValue
            };

            _context.ClassParameters.Add(inheritedParameter);
        }

        await _context.SaveChangesAsync();
    }
}