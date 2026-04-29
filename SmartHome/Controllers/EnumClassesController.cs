using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs;
using SmartHome.Models;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/enum-classes")]
public class EnumClassesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EnumClassesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnumClassDto>>> GetAll()
    {
        var enumClasses = await _context.EnumClasses
            .Include(x => x.MeasureUnit)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new EnumClassDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ValueType = x.ValueType,
                SortOrder = x.SortOrder,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .ToListAsync();

        return Ok(enumClasses);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EnumClassDto>> GetById(int id)
    {
        var enumClass = await _context.EnumClasses
            .Include(x => x.MeasureUnit)
            .Where(x => x.Id == id)
            .Select(x => new EnumClassDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ValueType = x.ValueType,
                SortOrder = x.SortOrder,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .FirstOrDefaultAsync();

        if (enumClass == null)
            return NotFound($"Перечисление с id={id} не найдено.");

        return Ok(enumClass);
    }

    [HttpPost]
    public async Task<ActionResult<EnumClassDto>> Create(CreateEnumClassDto dto)
    {
        var nameExists = await _context.EnumClasses.AnyAsync(x => x.Name == dto.Name);
        if (nameExists)
            return BadRequest($"Перечисление с именем '{dto.Name}' уже существует.");

        var shortNameExists = await _context.EnumClasses.AnyAsync(x => x.ShortName == dto.ShortName);
        if (shortNameExists)
            return BadRequest($"Перечисление с коротким именем '{dto.ShortName}' уже существует.");

        if (dto.ValueType == EnumValueType.Number && dto.MeasureUnitId == null)
            return BadRequest("Для численного перечисления необходимо указать единицу измерения.");

        if (dto.ValueType != EnumValueType.Number && dto.MeasureUnitId != null)
            return BadRequest("Единица измерения указывается только для численных перечислений.");

        if (dto.MeasureUnitId.HasValue)
        {
            var measureExists = await _context.MeasureUnits.AnyAsync(x => x.Id == dto.MeasureUnitId.Value);
            if (!measureExists)
                return BadRequest($"Единица измерения с id={dto.MeasureUnitId.Value} не найдена.");
        }

        var enumClass = new EnumClass
        {
            Name = dto.Name,
            ShortName = dto.ShortName,
            ValueType = dto.ValueType,
            SortOrder = dto.SortOrder,
            MeasureUnitId = dto.MeasureUnitId
        };

        _context.EnumClasses.Add(enumClass);
        await _context.SaveChangesAsync();

        var result = await _context.EnumClasses
            .Include(x => x.MeasureUnit)
            .Where(x => x.Id == enumClass.Id)
            .Select(x => new EnumClassDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ValueType = x.ValueType,
                SortOrder = x.SortOrder,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}/sort-order")]
    public async Task<ActionResult<EnumClassDto>> UpdateSortOrder(int id, UpdateSortOrderDto dto)
    {
        var enumClass = await _context.EnumClasses
            .Include(x => x.MeasureUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (enumClass == null)
            return NotFound($"Перечисление с id={id} не найдено.");

        enumClass.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync();

        var result = new EnumClassDto
        {
            Id = enumClass.Id,
            Name = enumClass.Name,
            ShortName = enumClass.ShortName,
            ValueType = enumClass.ValueType,
            SortOrder = enumClass.SortOrder,
            MeasureUnitId = enumClass.MeasureUnitId,
            MeasureUnitName = enumClass.MeasureUnit?.Name,
            MeasureUnitShortName = enumClass.MeasureUnit?.ShortName
        };

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var enumClass = await _context.EnumClasses
            .Include(x => x.Values)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (enumClass == null)
            return NotFound($"Перечисление с id={id} не найдено.");

        _context.EnumClasses.Remove(enumClass);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}