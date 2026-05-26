using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs;
using SmartHome.Models;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/parameter-groups")]
public class ParameterGroupsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ParameterGroupsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ParameterGroupDto>>> GetAll()
    {
        var groups = await _context.ParameterGroups
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ParameterGroupDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                SortOrder = x.SortOrder
            })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ParameterGroupDto>> GetById(int id)
    {
        var group = await _context.ParameterGroups
            .Where(x => x.Id == id)
            .Select(x => new ParameterGroupDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                SortOrder = x.SortOrder
            })
            .FirstOrDefaultAsync();

        if (group == null)
            return NotFound($"Группа параметров с id={id} не найдена.");

        return Ok(group);
    }

    [HttpPost]
    public async Task<ActionResult<ParameterGroupDto>> Create(CreateParameterGroupDto dto)
    {
        var nameExists = await _context.ParameterGroups.AnyAsync(x => x.Name == dto.Name);
        if (nameExists)
            return BadRequest($"Группа параметров с именем '{dto.Name}' уже существует.");

        var shortNameExists = await _context.ParameterGroups.AnyAsync(x => x.ShortName == dto.ShortName);
        if (shortNameExists)
            return BadRequest($"Группа параметров с коротким именем '{dto.ShortName}' уже существует.");

        var group = new ParameterGroup
        {
            Name = dto.Name,
            ShortName = dto.ShortName,
            SortOrder = dto.SortOrder
        };

        _context.ParameterGroups.Add(group);
        await _context.SaveChangesAsync();

        var result = new ParameterGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            ShortName = group.ShortName,
            SortOrder = group.SortOrder
        };

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}/sort-order")]
    public async Task<ActionResult<ParameterGroupDto>> UpdateSortOrder(int id, UpdateSortOrderDto dto)
    {
        var group = await _context.ParameterGroups.FirstOrDefaultAsync(x => x.Id == id);

        if (group == null)
            return NotFound($"Группа параметров с id={id} не найдена.");

        group.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync();

        var result = new ParameterGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            ShortName = group.ShortName,
            SortOrder = group.SortOrder
        };

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _context.ParameterGroups
            .Include(x => x.ClassParameters)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (group == null)
            return NotFound($"Группа параметров с id={id} не найдена.");

        if (group.ClassParameters.Any())
            return BadRequest("Нельзя удалить группу параметров, которая используется в параметрах классов.");

        _context.ParameterGroups.Remove(group);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}