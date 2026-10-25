using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Inventory;

public sealed record ReservationOutcome(StockItem? Item, bool Reserved);

public sealed class StockReservationService(OrdersDbContext db)
{
    // One conditional UPDATE, so two parallel reservations cannot both take the last units.
    // The caller owns the transaction (POST /orders reserves all lines in one transaction).
    public async Task<bool> TryReserveAsync(string sku, int quantity, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        var normalized = StockItem.Normalize(sku);
        var rows = await db.Database.ExecuteSqlAsync($"""
            UPDATE dbo.StockItems
            SET Reserved = Reserved + {quantity}
            WHERE Sku = {normalized} AND OnHand - Reserved >= {quantity}
            """, ct);
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
