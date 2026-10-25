using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Reports;

public static class SalesReports
{
    /// <summary>
    /// One row per calendar day in Amsterdam time with the orders that are not cancelled.
    /// The revenue is the sum of the line totals, before discounts.
    /// The WHERE clause compares the bare column with the UTC instants of local midnight, so the index can serve it.
    /// </summary>
    public static Task<List<DailySalesRow>> DailyAsync(OrdersDbContext db, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var toExclusive = to.AddDays(1);
        return db.Database.SqlQuery<DailySalesRow>($"""
            SELECT (o."PlacedAt" AT TIME ZONE 'Europe/Amsterdam')::date AS "Day",
                   count(DISTINCT o."Id")::int AS "Orders",
                   coalesce(sum(l."Quantity" * l."UnitPrice"), 0) AS "Revenue"
            FROM orders AS o
            JOIN order_lines AS l ON l."OrderId" = o."Id"
            WHERE o."Status" <> 'Cancelled'
              AND o."PlacedAt" >= ({from}::timestamp AT TIME ZONE 'Europe/Amsterdam')
              AND o."PlacedAt" < ({toExclusive}::timestamp AT TIME ZONE 'Europe/Amsterdam')
            GROUP BY 1
            ORDER BY 1
            """).ToListAsync(ct);
    }

    /// <summary>One row per week (Monday to Sunday) in Amsterdam time, for the days from 'from' to 'to'.</summary>
    public static Task<List<WeeklySalesRow>> WeeklyAsync(OrdersDbContext db, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var toExclusive = to.AddDays(1);
        return db.Database.SqlQuery<WeeklySalesRow>($"""
            SELECT date_trunc('week', o."PlacedAt" AT TIME ZONE 'Europe/Amsterdam')::date AS "WeekStart",
                   count(DISTINCT o."Id")::int AS "Orders",
                   coalesce(sum(l."Quantity" * l."UnitPrice"), 0) AS "Revenue"
            FROM orders AS o
            JOIN order_lines AS l ON l."OrderId" = o."Id"
            WHERE o."Status" <> 'Cancelled'
              AND o."PlacedAt" >= ({from}::timestamp AT TIME ZONE 'Europe/Amsterdam')
              AND o."PlacedAt" < ({toExclusive}::timestamp AT TIME ZONE 'Europe/Amsterdam')
            GROUP BY 1
            ORDER BY 1
            """).ToListAsync(ct);
    }
}
