using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Stock;

/// <summary>
/// Stock is reserved when an order is placed and released when it is cancelled.
/// Only SKUs with a stock level are managed. Other SKUs ship from the supplier and have no limit.
/// </summary>
public static class StockReservations
{
    /// <summary>Reserves each line. Returns the first SKU without enough stock, or null. Call it in a transaction.</summary>
    public static async Task<string?> ReserveAsync(OrdersDbContext db, IEnumerable<OrderLine> lines, DateTimeOffset now, CancellationToken ct)
    {
        // Sorted by SKU, so two orders lock the rows in the same order and cannot deadlock.
        foreach (var line in lines.GroupBy(l => l.Sku).Select(g => (Sku: g.Key, Quantity: g.Sum(l => l.Quantity))).OrderBy(l => l.Sku, StringComparer.Ordinal))
        {
            var managed = await db.StockLevels.AnyAsync(s => s.Sku == line.Sku, ct);
            if (!managed)
            {
                continue;
            }

            var updated = await db.StockLevels
                .Where(s => s.Sku == line.Sku && s.Available >= line.Quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Available, x => x.Available - line.Quantity)
                    .SetProperty(x => x.UpdatedAt, now), ct);
            if (updated == 0)
            {
                return line.Sku;
            }
        }

        return null;
    }

    public static async Task ReleaseAsync(OrdersDbContext db, IEnumerable<OrderLine> lines, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var line in lines.GroupBy(l => l.Sku).Select(g => (Sku: g.Key, Quantity: g.Sum(l => l.Quantity))).OrderBy(l => l.Sku, StringComparer.Ordinal))
        {
            await db.StockLevels
                .Where(s => s.Sku == line.Sku)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Available, x => x.Available + line.Quantity)
                    .SetProperty(x => x.UpdatedAt, now), ct);
        }
    }
}
