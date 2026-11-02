using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Bom;
using SmartHome.Data;
using SmartHome.Models;
using Npgsql;
using NpgsqlTypes;

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
    public async Task<IActionResult> History(int id) => Ok(await db.Set<BomRevision>().AsNoTracking().Where(x => x.ProductId == id).OrderByDescending(x => x.Id).Select(x => new { x.Id, x.PreviousId, x.BaseRevisionId, x.CreatedAt, x.Reason }).ToListAsync());
    [HttpPut("products/{id:int}")]
    public Task<IActionResult> Save(int id, SpecWrite input) => Write(async () => await service.Save(id, input));
    [HttpPost("modifications")]
    public Task<IActionResult> Modification(ModificationWrite input) => Write(async () =>
    {
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.ShortName) || input.Name.Length > 256 || input.ShortName.Length > 128) throw new ArgumentException("Укажите имя и обозначение модификации.");
        var r = await service.Revision(input.BaseRevisionId);
        var basis = await db.Products.AsNoTracking().SingleAsync(x => x.Id == r.ProductId);
        var p = new Product { Name = input.Name.Trim(), ShortName = input.ShortName.Trim(), ClassNodeId = basis.ClassNodeId }; db.Add(p); await db.SaveChangesAsync();
        var doc = await service.Effective(r);
        var created = await service.Save(p.Id, new SpecWrite { UnitId = r.UnitId, Reason = input.Reason, Rules = doc.Rules, Calculations = doc.Calculations }, r.Id);
        return new { productId = p.Id, revision = created };
    });
    [HttpGet("variants")]
    public async Task<IActionResult> Variants([FromQuery] int? productId) => Ok(await (from v in db.Set<BomVariant>().AsNoTracking() join r in db.Set<BomRevision>() on v.RevisionId equals r.Id where productId == null || r.ProductId == productId select new { v.Id, v.Name, v.RevisionId, r.ProductId, v.CreatedAt }).OrderByDescending(x => x.Id).ToListAsync());
    [HttpPost("variants")]
    public Task<IActionResult> CreateVariant(VariantWrite input) => Write(async () => await service.CreateVariant(input));
    [HttpPost("preview")]
    public Task<IActionResult> Preview(VariantWrite input) => Write(async () => await service.Resolve(input.RevisionId, input.Values));
    [HttpGet("variants/{id:int}")]
    public async Task<IActionResult> Variant(int id) => Ok(await service.Variant(id));
    private async Task<object> Query(string function, params (string name, object? value, NpgsqlDbType type)[] args)
    {
        await db.Database.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand($"SELECT coalesce(jsonb_agg(t),'[]'::jsonb)::text FROM {function}({string.Join(',', args.Select(x => "@" + x.name))}) t", (NpgsqlConnection)db.Database.GetDbConnection());
        foreach (var a in args) cmd.Parameters.AddWithValue(a.name, a.type, a.value ?? DBNull.Value);
        using var document = JsonDocument.Parse((string)(await cmd.ExecuteScalarAsync())!);
        return document.RootElement.Clone();
    }
    [HttpGet("variants/{id:int}/tree")]
    public async Task<IActionResult> Tree(int id, [FromQuery] decimal quantity = 1) => Ok(await Query("find_bom", ("id", id, NpgsqlDbType.Integer), ("q", quantity, NpgsqlDbType.Numeric)));
    [HttpGet("variants/{id:int}/totals")]
    public async Task<IActionResult> Totals(int id, [FromQuery] decimal quantity = 1, [FromQuery] int? classId = null) => Ok(await Query("calc_bom", ("id", id, NpgsqlDbType.Integer), ("q", quantity, NpgsqlDbType.Numeric), ("c", classId, NpgsqlDbType.Integer)));
    [HttpGet("orders")]
    public async Task<IActionResult> Orders() => Ok(await db.Set<BomOrder>().AsNoTracking().OrderByDescending(x => x.Id).ToListAsync());
    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> Order(int id) => Ok(new { order = await db.Set<BomOrder>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id), history = await db.Set<BomOrderRevision>().AsNoTracking().Where(x => x.OrderId == id).OrderByDescending(x => x.Id).ToListAsync() });
    [HttpPost("orders")]
    public Task<IActionResult> CreateOrder(OrderWrite input) => SaveOrder(null, input);
    [HttpPut("orders/{id:int}")]
    public Task<IActionResult> UpdateOrder(int id, OrderWrite input) => SaveOrder(id, input);
    private Task<IActionResult> SaveOrder(int? id, OrderWrite input) => Write(async () =>
    {
        if (string.IsNullOrWhiteSpace(input.Number) || input.Number.Length > 128 || input.Date == default || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 512 || input.Lines.Count == 0 || input.Lines.Count > 500) throw new ArgumentException("Заполните номер, дату, причину и позиции заказа.");
        BomOrder order;
        if (id == null) { if (input.ExpectedRevisionId != null) throw new ArgumentException("Неверная версия нового заказа."); order = new BomOrder { Number = input.Number.Trim(), Date = input.Date }; db.Add(order); await db.SaveChangesAsync(); }
        else
        {
            order = await db.Set<BomOrder>().SingleOrDefaultAsync(x => x.Id == id) ?? throw new ArgumentException("Заказ не найден.");
            if (order.Number != input.Number.Trim() || order.Date != input.Date) throw new ArgumentException("Номер и дата заказа зафиксированы. Изменяйте позиции.");
        }
        var latest = await db.Set<BomOrderRevision>().AsNoTracking().Where(x => x.OrderId == order.Id).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        if (latest?.Id != input.ExpectedRevisionId) throw new InvalidOperationException("Заказ уже изменён. Обновите страницу.");
        foreach (var line in input.Lines) { if (line.Quantity <= 0 || line.Quantity > 1_000_000_000) throw new ArgumentException("Количество заказа должно быть положительным и ограниченным."); await service.Variant(line.VariantId); }
        var revision = new BomOrderRevision { OrderId = order.Id, PreviousId = latest?.Id, Reason = input.Reason.Trim(), Lines = BomJson.Encode(input.Lines) };
        db.Add(revision); await db.SaveChangesAsync(); return new { order, revision };
    });
    [HttpGet("order-revisions/{id:int}/totals")]
    public async Task<IActionResult> OrderTotals(int id, [FromQuery] int? classId = null) => Ok(await Query("calc_order_bom", ("id", id, NpgsqlDbType.Integer), ("c", classId, NpgsqlDbType.Integer)));
}
