using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Reporting;

public sealed record VoucherDay(string Code, DateOnly Day, int Orders, decimal Discount);

public sealed class VoucherReportService(OrdersDbContext db)
{
    public async Task<IReadOnlyList<VoucherDay>> BuildAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var start = ShopCalendar.StartOfDayUtc(from);
        var end = ShopCalendar.StartOfDayUtc(to.AddDays(1));

        // PlacedAt is datetime2 in UTC: mark it as UTC first, then convert it to Amsterdam time.
        return await db.Database.SqlQuery<VoucherDay>($"""
            SELECT o.VoucherCode AS Code,
                   CAST(o.PlacedAt AT TIME ZONE 'UTC' AT TIME ZONE 'W. Europe Standard Time' AS date) AS Day,
                   COUNT(*) AS Orders,
                   SUM(o.Discount) AS Discount
            FROM dbo.Orders AS o
            WHERE o.VoucherCode IS NOT NULL AND o.PlacedAt >= {start} AND o.PlacedAt < {end}
            GROUP BY o.VoucherCode, CAST(o.PlacedAt AT TIME ZONE 'UTC' AT TIME ZONE 'W. Europe Standard Time' AS date)
            """).OrderBy(row => row.Day).ThenBy(row => row.Code).ToListAsync(ct);
    }
}
