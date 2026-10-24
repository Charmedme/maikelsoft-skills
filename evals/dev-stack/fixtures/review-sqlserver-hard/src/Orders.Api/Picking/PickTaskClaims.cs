using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Picking;

public sealed class PickTaskClaims(OrdersDbContext db, TimeProvider time)
{
    // Takes the oldest open task of the zone in one statement. UPDLOCK and READPAST: two pickers
    // never get the same task, and a picker does not wait for a task that another picker is taking.
    public async Task<PickTask?> TakeNextAsync(string pickerId, string zone, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var taken = await db.PickTasks.FromSql($"""
            WITH next AS (
                SELECT TOP (1) *
                FROM dbo.PickTasks WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = 0
                  AND Zone = {zone}
                ORDER BY CreatedAt
            )
            UPDATE next
            SET Status = 1, PickerId = {pickerId}, TakenAt = {now}
            OUTPUT inserted.*
            """).ToListAsync(ct);

        return taken.SingleOrDefault();
    }
}
