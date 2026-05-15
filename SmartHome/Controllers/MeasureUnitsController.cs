using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs;
using SmartHome.Models;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/measure-units")]
public class MeasureUnitsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MeasureUnitsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeasureUnitDto>>> GetAll()
    {
        var units = await _context.MeasureUnits
            .OrderBy(x => x.Name)
            .Select(x => new MeasureUnitDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName
            })
            .ToListAsync();

        return Ok(units);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MeasureUnitDto>> GetById(int id)
    {
        var unit = await _context.MeasureUnits
            .Where(x => x.Id == id)
            .Select(x => new MeasureUnitDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName
            })
            .FirstOrDefaultAsync();

        if (unit == null)
            return NotFound($"Единица измерения с id={id} не найдена.");

        return Ok(unit);
    }

    [HttpPost]
    public async Task<ActionResult<MeasureUnitDto>> Create(CreateMeasureUnitDto dto)
    {
        var nameExists = await _context.MeasureUnits.AnyAsync(x => x.Name == dto.Name);
        if (nameExists)
            return BadRequest($"Единица измерения с именем '{dto.Name}' уже существует.");

        var shortNameExists = await _context.MeasureUnits.AnyAsync(x => x.ShortName == dto.ShortName);
        if (shortNameExists)
            return BadRequest($"Единица измерения с коротким именем '{dto.ShortName}' уже существует.");

        var entity = new MeasureUnit
        {
            Name = dto.Name,
            ShortName = dto.ShortName
        };

        _context.MeasureUnits.Add(entity);
        await _context.SaveChangesAsync();

        var result = new MeasureUnitDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ShortName = entity.ShortName
        };

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
