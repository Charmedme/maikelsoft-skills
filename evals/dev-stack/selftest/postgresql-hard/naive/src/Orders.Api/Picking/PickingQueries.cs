using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Picking;

public static class PickingQueries
{
    /// <summary>
    /// Finds the next placed order (express first, then the oldest) that no other worker has locked.
    /// SKIP LOCKED makes the workers skip each other instead of waiting.
    /// </summary>
    public static async Task<Guid?> NextOrderIdAsync(OrdersDbContext db, CancellationToken ct)
    {
        var ids = await db.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value"
            FROM orders
            WHERE "Status" = 'Placed'
            ORDER BY "Express" DESC, "PlacedAt", "Id"
            LIMIT 1
            FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);
        return ids.Count == 0 ? null : ids[0];
    }
}
