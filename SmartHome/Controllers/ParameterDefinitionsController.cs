using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs;
using SmartHome.Models;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/parameter-definitions")]
public class ParameterDefinitionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ParameterDefinitionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ParameterDefinitionDto>>> GetAll()
    {
        var parameters = await _context.ParameterDefinitions
            .Include(x => x.MeasureUnit)
            .Include(x => x.EnumClass)
            .OrderBy(x => x.Name)
            .Select(x => new ParameterDefinitionDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ValueType = x.ValueType,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null,
                EnumClassId = x.EnumClassId,
                EnumClassName = x.EnumClass != null ? x.EnumClass.Name : null
            })
            .ToListAsync();

        return Ok(parameters);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ParameterDefinitionDto>> GetById(int id)
    {
        var parameter = await _context.ParameterDefinitions
            .Include(x => x.MeasureUnit)
            .Include(x => x.EnumClass)
            .Where(x => x.Id == id)
            .Select(x => new ParameterDefinitionDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ValueType = x.ValueType,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null,
                EnumClassId = x.EnumClassId,
                EnumClassName = x.EnumClass != null ? x.EnumClass.Name : null
            })
            .FirstOrDefaultAsync();

        if (parameter == null)
            return NotFound($"Параметр с id={id} не найден.");

        return Ok(parameter);
    }

    [HttpPost]
    public async Task<ActionResult<ParameterDefinitionDto>> Create(CreateParameterDefinitionDto dto)
    {
        var validationError = await ValidateParameterDefinition(dto);
        if (validationError != null)
            return BadRequest(validationError);

        var nameExists = await _context.ParameterDefinitions.AnyAsync(x => x.Name == dto.Name);
        if (nameExists)
            return BadRequest($"Параметр с именем '{dto.Name}' уже существует.");

        var shortNameExists = await _context.ParameterDefinitions.AnyAsync(x => x.ShortName == dto.ShortName);
        if (shortNameExists)
            return BadRequest($"Параметр с коротким именем '{dto.ShortName}' уже существует.");

        var parameter = new ParameterDefinition
        {
            Name = dto.Name,
            ShortName = dto.ShortName,
            ValueType = dto.ValueType,
            MeasureUnitId = dto.MeasureUnitId,
            EnumClassId = dto.EnumClassId
        };

        _context.ParameterDefinitions.Add(parameter);
        await _context.SaveChangesAsync();

        var result = await _context.ParameterDefinitions
            .Include(x => x.MeasureUnit)
            .Include(x => x.EnumClass)
            .Where(x => x.Id == parameter.Id)
            .Select(x => new ParameterDefinitionDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ValueType = x.ValueType,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null,
                EnumClassId = x.EnumClassId,
                EnumClassName = x.EnumClass != null ? x.EnumClass.Name : null
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var parameter = await _context.ParameterDefinitions
            .Include(x => x.ClassParameters)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (parameter == null)
            return NotFound($"Параметр с id={id} не найден.");

        if (parameter.ClassParameters.Any())
            return BadRequest("Нельзя удалить параметр, который уже привязан к классам изделий.");

        _context.ParameterDefinitions.Remove(parameter);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string?> ValidateParameterDefinition(CreateParameterDefinitionDto dto)
    {
        if (dto.ValueType == ParameterValueType.Enum)
        {
            if (dto.EnumClassId == null)
                return "Для параметра типа Enum необходимо указать EnumClassId.";

            var enumClassExists = await _context.EnumClasses.AnyAsync(x => x.Id == dto.EnumClassId.Value);
            if (!enumClassExists)
                return $"Перечисление с id={dto.EnumClassId.Value} не найдено.";

            if (dto.MeasureUnitId != null)
                return "Для параметра типа Enum единица измерения не указывается. Единица измерения задаётся в самом перечислении, если она нужна.";
        }

        if (dto.ValueType != ParameterValueType.Enum && dto.EnumClassId != null)
            return "EnumClassId можно указывать только для параметра типа Enum.";

        if (dto.ValueType is ParameterValueType.Number or ParameterValueType.Integer)
        {
            if (dto.MeasureUnitId.HasValue)
            {
                var measureExists = await _context.MeasureUnits.AnyAsync(x => x.Id == dto.MeasureUnitId.Value);
                if (!measureExists)
                    return $"Единица измерения с id={dto.MeasureUnitId.Value} не найдена.";
            }
        }

        if (dto.ValueType is ParameterValueType.String or ParameterValueType.DateTime)
        {
            if (dto.MeasureUnitId != null)
                return "Для строкового параметра и параметра даты единица измерения не указывается.";
        }

        return null;
    }
}