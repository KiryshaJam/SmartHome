using SmartHome.DTOs.Bom;
using SmartHome.Services.Bom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Models.Bom;
using SmartHome.Data;

namespace SmartHome.Controllers;

[ApiController, Route("api/bom")]
public class BomController(AppDbContext db) : ControllerBase
{
    private readonly BomService service = new(db);
    private async Task<IActionResult> Write(Func<Task<object>> action)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await service.Lock(); var result = await action(); await tx.CommitAsync(); return Ok(result);
        }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
        catch (OverflowException) { return BadRequest(new { message = "Результат вычисления слишком велик." }); }
    }
    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> Spec(int id, [FromQuery] int? revisionId)
    {
        var r = revisionId.HasValue ? await service.Revision(revisionId.Value) : await service.Latest(id);
        if (r != null && r.ProductId != id) return BadRequest(new { message = "Версия другого изделия." });
        return Ok(new { baseEffective = r?.BaseRevisionId is int b ? await service.Effective(await service.Revision(b)) : null, revision = r, effective = r == null ? null : await service.Effective(r), parameters = r == null ? await service.Parameters(id) : (await service.Effective(r)).Parameters });
    }
    [HttpGet("products/{id:int}/history")]
    public async Task<IActionResult> History(int id) => Ok(await db.Set<BomRevision>().AsNoTracking().Where(x => x.ProductId == id && !x.IsConfigured && x.IsFinalized).OrderByDescending(x => x.Id).Select(x => new { x.Id, x.PreviousId, x.BaseRevisionId, x.CreatedAt, x.Reason }).ToListAsync());
    [HttpPut("products/{id:int}")]
    public Task<IActionResult> Save(int id, SpecWrite input) => Write(async () => await service.Save(id, input));
    [HttpPost("modifications")]
    public Task<IActionResult> Modification(ModificationWrite input) => Write(async () => await service.CreateModification(input));
    [HttpGet("variants")]
    public async Task<IActionResult> Variants([FromQuery] int? productId) => Ok(await db.Set<BomRevision>().AsNoTracking()
        .Where(x => x.IsConfigured && x.IsFinalized && x.Name != null && (productId == null || x.ProductId == productId))
        .OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Name, RevisionId = x.SourceRevisionId, x.ProductId, x.CreatedAt }).ToListAsync());
    [HttpPost("variants")]
    public Task<IActionResult> CreateVariant(VariantWrite input) => Write(async () => await service.CreateVariant(input));
    [HttpPost("preview")]
    public Task<IActionResult> Preview(VariantWrite input) => Write(async () => await service.Resolve(input.RevisionId, input.Values));
    [HttpGet("variants/{id:int}")]
    public async Task<IActionResult> Variant(int id) => Ok(await service.Variant(id));
    [HttpGet("variants/{id:int}/tree")]
    public async Task<IActionResult> Tree(int id, [FromQuery] decimal quantity = 1) => Ok(await service.Tree(id, quantity));
    [HttpGet("variants/{id:int}/totals")]
    public async Task<IActionResult> Totals(int id, [FromQuery] decimal quantity = 1, [FromQuery] int? classId = null) => Ok(await service.Totals(id, quantity, classId));
}
