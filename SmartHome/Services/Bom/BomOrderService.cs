using Microsoft.EntityFrameworkCore;
using SmartHome.Data;
using SmartHome.DTOs.Bom;
using SmartHome.Models.Bom;

namespace SmartHome.Services.Bom;

public sealed class BomOrderService(AppDbContext db)
{
    public async Task<List<OrderSummaryDto>> List() => await db.Set<BomOrder>().AsNoTracking().OrderByDescending(x => x.Id)
        .Select(x => new OrderSummaryDto(x.Id, x.Number, x.Date, x.UpdateToken)).ToListAsync();

    public async Task<OrderDetailsDto?> Get(int id)
    {
        var order = await db.Set<BomOrder>().AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id);
        return order == null ? null : Response(order);
    }

    private static OrderDetailsDto Response(BomOrder order) => new(new(order.Id, order.Number, order.Date, order.UpdateToken),
        order.Lines.OrderBy(x => x.SortOrder).Select(x => new OrderLine(x.ConfiguredRevisionId, x.Quantity)).ToList());

    public async Task<OrderDetailsDto> Save(int? id, OrderWrite input)
    {
        if (string.IsNullOrWhiteSpace(input.Number) || input.Number.Length > 128 || input.Date == default || input.Lines.Count == 0 || input.Lines.Count > 500)
            throw new ArgumentException("Заполните номер, дату и позиции заказа.");
        foreach (var line in input.Lines)
        {
            if (line.Quantity <= 0 || line.Quantity > 1_000_000_000) throw new ArgumentException("Количество заказа вне диапазона (0; 1000000000].");
            if (!await db.Set<BomRevision>().AnyAsync(x => x.Id == line.VariantId && x.IsConfigured && x.IsFinalized)) throw new ArgumentException("Выберите готовое исполнение.");
        }
        BomOrder order;
        if (id == null)
        {
            if (input.ExpectedUpdateToken != null) throw new ArgumentException("Неверный токен нового заказа.");
            order = new BomOrder { Number = input.Number.Trim(), Date = input.Date }; db.Add(order);
        }
        else
        {
            order = await db.Set<BomOrder>().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id) ?? throw new ArgumentException("Заказ не найден.");
            if (order.UpdateToken != input.ExpectedUpdateToken) throw new InvalidOperationException("Заказ уже изменён. Обновите страницу.");
            if (order.Number != input.Number.Trim() || order.Date != input.Date) throw new ArgumentException("Номер и дата заказа зафиксированы.");
            // Flush deletions first: duplicate variants remain distinct rows by SortOrder.
            db.RemoveRange(order.Lines); await db.SaveChangesAsync(); order.Lines.Clear(); order.UpdateToken++;
        }
        order.Lines = input.Lines.Select((line, index) => new BomOrderLine { SortOrder = index, ConfiguredRevisionId = line.VariantId, Quantity = line.Quantity }).ToList();
        await db.SaveChangesAsync();
        return Response(order);
    }
}
