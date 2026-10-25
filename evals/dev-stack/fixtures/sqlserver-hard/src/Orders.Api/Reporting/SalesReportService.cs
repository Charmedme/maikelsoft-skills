using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Reporting;

// The shop is Dutch: a report day is a day in Amsterdam time.
public sealed class SalesReportService(OrdersDbContext db)
{
    public const int MaxDays = 92;

    public async Task<SalesReport> BuildAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from)
        {
            throw new ArgumentException("The end date is before the start date.", nameof(to));
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxDays)
        {
            throw new ArgumentException($"A report covers at most {MaxDays} days.", nameof(to));
        }

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // One pass over the orders of the range, grouped by day in the database.
        var rows = await db.Database.SqlQuery<SalesDayRow>($"""
            SELECT CAST(o.PlacedAt AT TIME ZONE 'W. Europe Standard Time' AS date) AS Day,
                   COUNT(*) AS Orders,
                   SUM(CASE WHEN o.Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledOrders,
                   SUM(CASE WHEN o.Status = 'Cancelled' THEN 0 ELSE t.Total END) AS NetRevenue
            FROM dbo.Orders AS o
            CROSS APPLY (SELECT SUM(l.Quantity * l.UnitPrice) AS Total FROM dbo.OrderLines AS l WHERE l.OrderId = o.Id) AS t
            WHERE o.PlacedAt >= {start} AND o.PlacedAt < {end}
            GROUP BY CAST(o.PlacedAt AT TIME ZONE 'W. Europe Standard Time' AS date)
            """).ToListAsync(ct);

        var byDay = rows.ToDictionary(row => row.Day);
        var days = new List<SalesSummary>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            days.Add(byDay.TryGetValue(day, out var row)
                ? new SalesSummary(day, row.Orders, row.CancelledOrders, row.NetRevenue)
                : new SalesSummary(day, 0, 0, 0m));
        }

        return new SalesReport(from, to, days, days.Sum(d => d.NetRevenue));
    }

    private sealed record SalesDayRow(DateOnly Day, int Orders, int CancelledOrders, decimal NetRevenue);
}
