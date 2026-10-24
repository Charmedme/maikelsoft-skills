using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Reporting;

public sealed record VoucherDay(string Code, DateOnly Day, int Orders, decimal Discount);

// The same day rules as the sales report: a day in Amsterdam time.
public sealed class VoucherReportService(OrdersDbContext db)
{
    public async Task<IReadOnlyList<VoucherDay>> BuildAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return await db.Database.SqlQuery<VoucherDay>($"""
            SELECT o.VoucherCode AS Code,
                   CAST(o.PlacedAt AT TIME ZONE 'W. Europe Standard Time' AS date) AS Day,
                   COUNT(*) AS Orders,
                   SUM(o.Discount) AS Discount
            FROM dbo.Orders AS o
            WHERE o.VoucherCode IS NOT NULL AND o.PlacedAt >= {start} AND o.PlacedAt < {end}
            GROUP BY o.VoucherCode, CAST(o.PlacedAt AT TIME ZONE 'W. Europe Standard Time' AS date)
            """).OrderBy(row => row.Day).ThenBy(row => row.Code).ToListAsync(ct);
    }
}
