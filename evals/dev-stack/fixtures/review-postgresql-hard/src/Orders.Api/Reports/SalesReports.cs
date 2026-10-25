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

    /// <summary>One row per calendar month with the orders that are not cancelled, for the months from 'from' to 'to'.</summary>
    public static Task<List<MonthlySalesRow>> MonthlyAsync(OrdersDbContext db, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return db.Database.SqlQuery<MonthlySalesRow>($"""
            SELECT date_trunc('month', o."PlacedAt")::date AS "Month",
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

    /// <summary>Coupons that no order has used yet, so that marketing can stop them.</summary>
    public static Task<List<UnusedCouponRow>> UnusedCouponsAsync(OrdersDbContext db, CancellationToken ct) =>
        db.Database.SqlQuery<UnusedCouponRow>($"""
            SELECT c."Code", c."PercentOff", c."ValidUntil"
            FROM coupons AS c
            WHERE c."Code" NOT IN (SELECT o."CouponCode" FROM orders AS o)
            ORDER BY c."Code"
            """).ToListAsync(ct);
}
