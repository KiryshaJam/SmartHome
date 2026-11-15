using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs.Bom;
using SmartHome.Services.Bom;

namespace SmartHome.Controllers;

[ApiController, Route("api/bom")]
public class BomOrdersController(AppDbContext db) : ControllerBase
{
    private readonly BomService service = new(db);
    private readonly BomOrderService orders = new(db);
    private async Task<IActionResult> Write(Func<Task<object>> action)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await service.Lock(); var result = await action(); await tx.CommitAsync(); return Ok(result);
        }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "Заказ уже изменён. Обновите страницу." }); }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
        catch (OverflowException) { return BadRequest(new { message = "Результат вычисления слишком велик." }); }
    }
    [HttpGet("orders")]
    public async Task<IActionResult> Orders() => Ok(await orders.List());

    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> Order(int id)
    {
        var result = await orders.Get(id);
        return result == null ? NotFound(new { message = "Заказ не найден." }) : Ok(result);
    }

    [HttpPost("orders")]
    public Task<IActionResult> CreateOrder(OrderWrite input) => Write(async () => await orders.Save(null, input));

    [HttpPut("orders/{id:int}")]
    public Task<IActionResult> UpdateOrder(int id, OrderWrite input) => Write(async () => await orders.Save(id, input));

    [HttpGet("orders/{id:int}/totals")]
    public async Task<IActionResult> OrderTotals(int id, [FromQuery] int? classId = null) => Ok(await service.OrderTotals(id, classId));
}
