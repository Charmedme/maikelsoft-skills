using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Picking;

public static class PickingQueries
{
    /// <summary>
    /// Takes the next order for the worker in one statement: express orders first, then the oldest.
    /// The row lock of FOR UPDATE SKIP LOCKED covers the UPDATE, so two workers never get the same order,
    /// and a worker never waits for another worker.
    /// </summary>
    public static async Task<Guid?> ClaimNextAsync(OrdersDbContext db, string workerId, DateTimeOffset now, CancellationToken ct)
    {
        var ids = await db.Database.SqlQuery<Guid>($"""
            UPDATE orders
            SET "Status" = 'Picking', "PickedBy" = {workerId}, "PickingStartedAt" = {now}
            WHERE "Id" = (
                SELECT "Id"
                FROM orders
                WHERE "Status" = 'Placed'
                ORDER BY "Express" DESC, "PlacedAt", "Id"
                LIMIT 1
                FOR UPDATE SKIP LOCKED)
            RETURNING "Id" AS "Value"
            """).ToListAsync(ct); // EF cannot compose UPDATE ... RETURNING, so no SingleOrDefaultAsync
        return ids.Count == 0 ? null : ids[0];
    }
}
