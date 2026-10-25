using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Reports;

public static class SalesReports
{
    /// <summary>
    /// One row per calendar day (Dutch time) with the orders that are not cancelled.
    /// The revenue is the sum of the line totals, before discounts.
    /// </summary>
    public static Task<List<DailySalesRow>> DailyAsync(OrdersDbContext db, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return db.Database.SqlQuery<DailySalesRow>($"""
            SELECT o."PlacedAt"::date AS "Day",
                   count(DISTINCT o."Id")::int AS "Orders",
                   coalesce(sum(l."Quantity" * l."UnitPrice"), 0) AS "Revenue"
            FROM orders AS o
            JOIN order_lines AS l ON l."OrderId" = o."Id"
            WHERE o."Status" <> 'Cancelled'
              AND o."PlacedAt" >= {fromUtc}
              AND o."PlacedAt" < {toUtc}
            GROUP BY 1
            ORDER BY 1
            """).ToListAsync(ct);
    }
}
