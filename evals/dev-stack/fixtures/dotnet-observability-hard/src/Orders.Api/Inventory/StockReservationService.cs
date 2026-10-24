using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Inventory;

public sealed record ReservationOutcome(StockItem? Item, bool Reserved);

public sealed class StockReservationService(OrdersDbContext db, ILogger<StockReservationService> logger)
{
    private const int MaxAttempts = 3;

    public async Task<ReservationOutcome> ReserveAsync(string sku, int quantity, CancellationToken ct)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        for (var attempt = 1; ; attempt++)
        {
            var item = await db.StockItems.FirstOrDefaultAsync(s => s.Sku == normalized, ct);
            if (item is null)
            {
                return new ReservationOutcome(null, false);
            }

            if (!item.TryReserve(quantity))
            {
                return new ReservationOutcome(item, false);
            }

            try
            {
                await db.SaveChangesAsync(ct);
                return new ReservationOutcome(item, true);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                logger.LogWarning("Stock of {Sku} changed during reservation, attempt {Attempt} of {MaxAttempts}", normalized, attempt, MaxAttempts);
                db.ChangeTracker.Clear();
            }
        }
    }
}
