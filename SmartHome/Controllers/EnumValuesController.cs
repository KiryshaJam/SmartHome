using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs;
using SmartHome.Models;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/enum-classes/{enumClassId:int}/values")]
public class EnumValuesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EnumValuesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnumValueDto>>> GetValues(int enumClassId)
    {
        var enumClassExists = await _context.EnumClasses.AnyAsync(x => x.Id == enumClassId);
        if (!enumClassExists)
            return NotFound($"Перечисление с id={enumClassId} не найдено.");

        var values = await _context.EnumValues
            .Where(x => x.EnumClassId == enumClassId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new EnumValueDto
            {
                Id = x.Id,
                EnumClassId = x.EnumClassId,
                StringValue = x.StringValue,
                NumberValue = x.NumberValue,
                IconValue = x.IconValue,
                DisplayName = x.DisplayName,
                SortOrder = x.SortOrder
            })
            .ToListAsync();

        return Ok(values);
    }

    [HttpGet("{valueId:int}")]
    public async Task<ActionResult<EnumValueDto>> GetById(int enumClassId, int valueId)
    {
        var value = await _context.EnumValues
            .Where(x => x.EnumClassId == enumClassId && x.Id == valueId)
            .Select(x => new EnumValueDto
            {
                Id = x.Id,
                EnumClassId = x.EnumClassId,
                StringValue = x.StringValue,
                NumberValue = x.NumberValue,
                IconValue = x.IconValue,
                DisplayName = x.DisplayName,
                SortOrder = x.SortOrder
            })
            .FirstOrDefaultAsync();

        if (value == null)
            return NotFound($"Значение с id={valueId} не найдено.");

        return Ok(value);
    }

    [HttpPost]
    public async Task<ActionResult<EnumValueDto>> Create(int enumClassId, CreateEnumValueDto dto)
    {
        var enumClass = await _context.EnumClasses.FirstOrDefaultAsync(x => x.Id == enumClassId);
        if (enumClass == null)
            return NotFound($"Перечисление с id={enumClassId} не найдено.");

        var validationError = ValidateValueByType(enumClass.ValueType, dto);
        if (validationError != null)
            return BadRequest(validationError);

        var sortOrderExists = await _context.EnumValues
            .AnyAsync(x => x.EnumClassId == enumClassId && x.SortOrder == dto.SortOrder);

        if (sortOrderExists)
            return BadRequest($"Позиция с SortOrder={dto.SortOrder} уже существует в этом перечислении.");

        var duplicateExists = await IsDuplicateValue(enumClassId, enumClass.ValueType, dto);
        if (duplicateExists)
            return BadRequest("Такое значение уже существует в перечислении.");

        var value = new EnumValue
        {
            EnumClassId = enumClassId,
            StringValue = enumClass.ValueType == EnumValueType.String ? dto.StringValue : null,
            NumberValue = enumClass.ValueType == EnumValueType.Number ? dto.NumberValue : null,
            IconValue = enumClass.ValueType == EnumValueType.Icon ? dto.IconValue : null,
            DisplayName = dto.DisplayName,
            SortOrder = dto.SortOrder
        };

        _context.EnumValues.Add(value);
        await _context.SaveChangesAsync();

        var result = new EnumValueDto
        {
            Id = value.Id,
            EnumClassId = value.EnumClassId,
            StringValue = value.StringValue,
            NumberValue = value.NumberValue,
            IconValue = value.IconValue,
            DisplayName = value.DisplayName,
            SortOrder = value.SortOrder
        };

        return CreatedAtAction(nameof(GetById), new { enumClassId, valueId = result.Id }, result);
    }

    [HttpPut("{valueId:int}/sort-order")]
    public async Task<ActionResult<EnumValueDto>> UpdateSortOrder(
        int enumClassId,
        int valueId,
        UpdateSortOrderDto dto)
    {
        var value = await _context.EnumValues
            .FirstOrDefaultAsync(x => x.EnumClassId == enumClassId && x.Id == valueId);

        if (value == null)
            return NotFound($"Значение с id={valueId} не найдено.");

        var sortOrderExists = await _context.EnumValues
            .AnyAsync(x =>
                x.EnumClassId == enumClassId &&
                x.SortOrder == dto.SortOrder &&
                x.Id != valueId);

        if (sortOrderExists)
            return BadRequest($"Позиция с SortOrder={dto.SortOrder} уже существует в этом перечислении.");

        value.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync();

        var result = new EnumValueDto
        {
            Id = value.Id,
            EnumClassId = value.EnumClassId,
            StringValue = value.StringValue,
            NumberValue = value.NumberValue,
            IconValue = value.IconValue,
            DisplayName = value.DisplayName,
            SortOrder = value.SortOrder
        };

        return Ok(result);
    }

    [HttpPost("check")]
    public async Task<ActionResult<bool>> CheckValue(int enumClassId, CheckEnumValueDto dto)
    {
        var enumClass = await _context.EnumClasses.FirstOrDefaultAsync(x => x.Id == enumClassId);
        if (enumClass == null)
            return NotFound($"Перечисление с id={enumClassId} не найдено.");

        var exists = enumClass.ValueType switch
        {
            EnumValueType.String => await _context.EnumValues.AnyAsync(x =>
                x.EnumClassId == enumClassId &&
                x.StringValue == dto.StringValue),

            EnumValueType.Number => await _context.EnumValues.AnyAsync(x =>
                x.EnumClassId == enumClassId &&
                x.NumberValue == dto.NumberValue),

            EnumValueType.Icon => await _context.EnumValues.AnyAsync(x =>
                x.EnumClassId == enumClassId &&
                x.IconValue == dto.IconValue),

            _ => false
        };

        return Ok(exists);
    }

    [HttpDelete("{valueId:int}")]
    public async Task<IActionResult> Delete(int enumClassId, int valueId)
    {
        var value = await _context.EnumValues
            .FirstOrDefaultAsync(x => x.EnumClassId == enumClassId && x.Id == valueId);

        if (value == null)
            return NotFound($"Значение с id={valueId} не найдено.");

        _context.EnumValues.Remove(value);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string? ValidateValueByType(EnumValueType valueType, CreateEnumValueDto dto)
    {
        return valueType switch
        {
            EnumValueType.String when string.IsNullOrWhiteSpace(dto.StringValue)
                => "Для строкового перечисления необходимо указать StringValue.",

            EnumValueType.Number when dto.NumberValue == null
                => "Для численного перечисления необходимо указать NumberValue.",

            EnumValueType.Icon when string.IsNullOrWhiteSpace(dto.IconValue)
                => "Для перечисления с иконками необходимо указать IconValue.",

            EnumValueType.String when dto.NumberValue != null || dto.IconValue != null
                => "Для строкового перечисления нельзя указывать NumberValue или IconValue.",

            EnumValueType.Number when dto.StringValue != null || dto.IconValue != null
                => "Для численного перечисления нельзя указывать StringValue или IconValue.",

            EnumValueType.Icon when dto.StringValue != null || dto.NumberValue != null
                => "Для перечисления с иконками нельзя указывать StringValue или NumberValue.",

            _ => null
        };
    }

    private async Task<bool> IsDuplicateValue(
        int enumClassId,
        EnumValueType valueType,
        CreateEnumValueDto dto)
    {
        return valueType switch
        {
            EnumValueType.String => await _context.EnumValues.AnyAsync(x =>
                x.EnumClassId == enumClassId &&
                x.StringValue == dto.StringValue),

            EnumValueType.Number => await _context.EnumValues.AnyAsync(x =>
                x.EnumClassId == enumClassId &&
                x.NumberValue == dto.NumberValue),

            EnumValueType.Icon => await _context.EnumValues.AnyAsync(x =>
                x.EnumClassId == enumClassId &&
                x.IconValue == dto.IconValue),

            _ => false
        };
    }
}