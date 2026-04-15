using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.Models;
using SmartHome.DTOs;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/classnodes")]
public class ClassNodesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClassNodesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassNodeDto>>> GetAll()
    {
        var nodes = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.SortOrder)
            .Select(x => new ClassNodeDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                IsTerminal = x.IsTerminal,
                SortOrder = x.SortOrder,
                ParentId = x.ParentId,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .ToListAsync();

        return Ok(nodes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClassNodeDto>> GetById(int id)
    {
        var node = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .Where(x => x.Id == id)
            .Select(x => new ClassNodeDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                IsTerminal = x.IsTerminal,
                SortOrder = x.SortOrder,
                ParentId = x.ParentId,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .FirstOrDefaultAsync();

        if (node == null)
            return NotFound();

        return Ok(node);
    }

    [HttpGet("{id:int}/children")]
    public async Task<ActionResult<IEnumerable<ClassNodeDto>>> GetChildren(int id)
    {
        var parentExists = await _context.ClassNodes.AnyAsync(x => x.Id == id);
        if (!parentExists)
            return NotFound($"Узел с id={id} не найден.");

        var children = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .Where(x => x.ParentId == id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ClassNodeDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                IsTerminal = x.IsTerminal,
                SortOrder = x.SortOrder,
                ParentId = x.ParentId,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .ToListAsync();

        return Ok(children);
    }

    [HttpGet("roots")]
    public async Task<ActionResult<IEnumerable<ClassNodeDto>>> GetRoots()
    {
        var roots = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .Where(x => x.ParentId == null)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ClassNodeDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                IsTerminal = x.IsTerminal,
                SortOrder = x.SortOrder,
                ParentId = x.ParentId,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .ToListAsync();

        return Ok(roots);
    }

    [HttpGet("leaves")]
    public async Task<ActionResult<IEnumerable<ClassNodeDto>>> GetLeaves()
    {
        var leaves = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .Where(x => x.IsTerminal)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ClassNodeDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                IsTerminal = x.IsTerminal,
                SortOrder = x.SortOrder,
                ParentId = x.ParentId,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .ToListAsync();

        return Ok(leaves);
    }

    [HttpPost]
    public async Task<ActionResult<ClassNodeDto>> Create(CreateClassNodeDto dto)
    {
        var nameExists = await _context.ClassNodes.AnyAsync(x => x.Name == dto.Name);
        if (nameExists)
            return BadRequest($"Класс с именем '{dto.Name}' уже существует.");

        var shortNameExists = await _context.ClassNodes.AnyAsync(x => x.ShortName == dto.ShortName);
        if (shortNameExists)
            return BadRequest($"Класс с коротким именем '{dto.ShortName}' уже существует.");

        if (dto.ParentId.HasValue)
        {
            var parentExists = await _context.ClassNodes.AnyAsync(x => x.Id == dto.ParentId.Value);
            if (!parentExists)
                return BadRequest($"Родительский узел с id={dto.ParentId.Value} не найден.");
        }

        if (dto.MeasureUnitId.HasValue)
        {
            var measureExists = await _context.MeasureUnits.AnyAsync(x => x.Id == dto.MeasureUnitId.Value);
            if (!measureExists)
                return BadRequest($"Единица измерения с id={dto.MeasureUnitId.Value} не найдена.");
        }

        var node = new Models.ClassNode
        {
            Name = dto.Name,
            ShortName = dto.ShortName,
            IsTerminal = dto.IsTerminal,
            SortOrder = dto.SortOrder,
            ParentId = dto.ParentId,
            MeasureUnitId = dto.MeasureUnitId
        };

        _context.ClassNodes.Add(node);
        await _context.SaveChangesAsync();

        var result = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .Where(x => x.Id == node.Id)
            .Select(x => new ClassNodeDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                IsTerminal = x.IsTerminal,
                SortOrder = x.SortOrder,
                ParentId = x.ParentId,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit != null ? x.MeasureUnit.Name : null,
                MeasureUnitShortName = x.MeasureUnit != null ? x.MeasureUnit.ShortName : null
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var node = await _context.ClassNodes
            .Include(x => x.Children)
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            return NotFound($"Узел с id={id} не найден.");

        if (node.Children.Any())
            return BadRequest("Нельзя удалить узел, у которого есть дочерние узлы.");

        if (node.Products.Any())
            return BadRequest("Нельзя удалить узел, у которого есть связанные продукты.");

        _context.ClassNodes.Remove(node);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpPut("{id:int}/move")]
    public async Task<ActionResult<ClassNodeDto>> Move(int id, MoveClassNodeDto dto)
    {
        var node = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            return NotFound($"Узел с id={id} не найден.");

        if (dto.NewParentId == id)
            return BadRequest("Узел нельзя сделать родителем самому себе.");

        if (dto.NewParentId.HasValue)
        {
            var newParent = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == dto.NewParentId.Value);
            if (newParent == null)
                return BadRequest($"Новый родитель с id={dto.NewParentId.Value} не найден.");

            var hasCycle = await WouldCreateCycle(id, dto.NewParentId.Value);
            if (hasCycle)
                return BadRequest("Перемещение создаёт цикл в иерархии.");
        }

        node.ParentId = dto.NewParentId;
        await _context.SaveChangesAsync();

        var result = new ClassNodeDto
        {
            Id = node.Id,
            Name = node.Name,
            ShortName = node.ShortName,
            IsTerminal = node.IsTerminal,
            SortOrder = node.SortOrder,
            ParentId = node.ParentId,
            MeasureUnitId = node.MeasureUnitId,
            MeasureUnitName = node.MeasureUnit?.Name,
            MeasureUnitShortName = node.MeasureUnit?.ShortName
        };

        return Ok(result);
    }
    
    [HttpPut("{id:int}/sort-order")]
    public async Task<ActionResult<ClassNodeDto>> UpdateSortOrder(int id, UpdateSortOrderDto dto)
    {
        var node = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            return NotFound($"Узел с id={id} не найден.");

        node.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync();

        var result = new ClassNodeDto
        {
            Id = node.Id,
            Name = node.Name,
            ShortName = node.ShortName,
            IsTerminal = node.IsTerminal,
            SortOrder = node.SortOrder,
            ParentId = node.ParentId,
            MeasureUnitId = node.MeasureUnitId,
            MeasureUnitName = node.MeasureUnit?.Name,
            MeasureUnitShortName = node.MeasureUnit?.ShortName
        };

        return Ok(result);
    }
    
    [HttpGet("{id:int}/parents")]
    public async Task<ActionResult<IEnumerable<ParentChainNodeDto>>> GetParents(int id)
    {
        var node = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == id);
        if (node == null)
            return NotFound($"Узел с id={id} не найден.");

        var result = new List<ParentChainNodeDto>();
        var current = node;
        var level = 1;

        while (current != null)
        {
            result.Add(new ParentChainNodeDto
            {
                Id = current.Id,
                Name = current.Name,
                ShortName = current.ShortName,
                Level = level
            });

            if (current.ParentId == null)
                break;

            current = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == current.ParentId.Value);
            level++;
        }

        return Ok(result);
    }
    
    [HttpGet("{id:int}/descendants")]
    public async Task<ActionResult<TreeNodeDto>> GetDescendants(int id)
    {
        var allNodes = await _context.ClassNodes
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var root = allNodes.FirstOrDefault(x => x.Id == id);
        if (root == null)
            return NotFound($"Узел с id={id} не найден.");

        TreeNodeDto BuildTree(ClassNode node)
        {
            var children = allNodes
                .Where(x => x.ParentId == node.Id)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(BuildTree)
                .ToList();

            return new TreeNodeDto
            {
                Id = node.Id,
                Name = node.Name,
                ShortName = node.ShortName,
                IsTerminal = node.IsTerminal,
                SortOrder = node.SortOrder,
                ParentId = node.ParentId,
                Children = children
            };
        }

        return Ok(BuildTree(root));
    }
    
    [HttpGet("{id:int}/leaves")]
    public async Task<ActionResult<IEnumerable<ClassNodeDto>>> GetLeavesByClass(int id)
    {
        var allNodes = await _context.ClassNodes
            .Include(x => x.MeasureUnit)
            .ToListAsync();

        var root = allNodes.FirstOrDefault(x => x.Id == id);
        if (root == null)
            return NotFound($"Узел с id={id} не найден.");

        var descendantIds = new HashSet<int>();

        void CollectDescendants(int currentId)
        {
            descendantIds.Add(currentId);

            var children = allNodes.Where(x => x.ParentId == currentId).Select(x => x.Id).ToList();
            foreach (var childId in children)
                CollectDescendants(childId);
        }

        CollectDescendants(id);

        var leaves = allNodes
            .Where(x => descendantIds.Contains(x.Id) && x.IsTerminal)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ClassNodeDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                IsTerminal = x.IsTerminal,
                SortOrder = x.SortOrder,
                ParentId = x.ParentId,
                MeasureUnitId = x.MeasureUnitId,
                MeasureUnitName = x.MeasureUnit?.Name,
                MeasureUnitShortName = x.MeasureUnit?.ShortName
            })
            .ToList();

        return Ok(leaves);
    }
    
    private async Task<bool> WouldCreateCycle(int nodeId, int newParentId)
    {
        var currentParentId = newParentId;

        while (true)
        {
            if (currentParentId == nodeId)
                return true;

            var parent = await _context.ClassNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == currentParentId);

            if (parent == null || parent.ParentId == null)
                return false;

            currentParentId = parent.ParentId.Value;
        }
    }
}

