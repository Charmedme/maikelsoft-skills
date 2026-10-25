using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Inventory;

public sealed record ReservationOutcome(StockItem? Item, bool Reserved);

public sealed class StockReservationService(OrdersDbContext db)
{
    // One conditional UPDATE, so two parallel reservations cannot both take the last units.
    // LINQ sends the SKU as varchar(32), like the column, so SQL Server can seek the unique index.
    // The caller owns the transaction (POST /orders reserves all lines in one transaction).
    public async Task<bool> TryReserveAsync(string sku, int quantity, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        var normalized = StockItem.Normalize(sku);
        var rows = await db.StockItems
            .Where(s => s.Sku == normalized && s.OnHand - s.Reserved >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Reserved, x => x.Reserved + quantity), ct);
        return rows == 1;
    }

    public async Task<ReservationOutcome> ReserveAsync(string sku, int quantity, CancellationToken ct)
    {
        var reserved = await TryReserveAsync(sku, quantity, ct);
        var normalized = StockItem.Normalize(sku);
        var item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Sku == normalized, ct);
        return new ReservationOutcome(item, reserved && item is not null);
    }
}
