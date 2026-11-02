using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.Models;
using SmartHome.DTOs;

namespace SmartHome.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _context.Products
            .Include(x => x.ClassNode)
            .OrderBy(x => x.Name)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ClassNodeId = x.ClassNodeId,
                ClassNodeName = x.ClassNode.Name
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _context.Products
            .Include(x => x.ClassNode)
            .Where(x => x.Id == id)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ClassNodeId = x.ClassNodeId,
                ClassNodeName = x.ClassNode.Name
            })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound();

        return Ok(product);
    }
    
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto)
    {
        var nameExists = await _context.Products.AnyAsync(x => x.Name == dto.Name);
        if (nameExists)
            return BadRequest($"Продукт с именем '{dto.Name}' уже существует.");

        var shortNameExists = await _context.Products.AnyAsync(x => x.ShortName == dto.ShortName);
        if (shortNameExists)
            return BadRequest($"Продукт с коротким именем '{dto.ShortName}' уже существует.");

        var classNode = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == dto.ClassNodeId);
        if (classNode == null)
            return BadRequest($"Класс с id={dto.ClassNodeId} не найден.");

        var product = new Models.Product
        {
            Name = dto.Name,
            ShortName = dto.ShortName,
            ClassNodeId = dto.ClassNodeId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _context.Products
            .Include(x => x.ClassNode)
            .Where(x => x.Id == product.Id)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ClassNodeId = x.ClassNodeId,
                ClassNodeName = x.ClassNode.Name
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (product == null)
            return NotFound($"Продукт с id={id} не найден.");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpPut("{id:int}/move")]
    public async Task<ActionResult<ProductDto>> Move(int id, MoveProductDto dto)
    {
        var product = await _context.Products
            .Include(x => x.ClassNode)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return NotFound($"Продукт с id={id} не найден.");

        var targetClass = await _context.ClassNodes.FirstOrDefaultAsync(x => x.Id == dto.NewClassNodeId);
        if (targetClass == null)
            return BadRequest($"Класс с id={dto.NewClassNodeId} не найден.");

        product.ClassNodeId = dto.NewClassNodeId;
        await _context.SaveChangesAsync();

        var result = await _context.Products
            .Include(x => x.ClassNode)
            .Where(x => x.Id == product.Id)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortName = x.ShortName,
                ClassNodeId = x.ClassNodeId,
                ClassNodeName = x.ClassNode.Name
            })
            .FirstAsync();

        return Ok(result);
    }
}