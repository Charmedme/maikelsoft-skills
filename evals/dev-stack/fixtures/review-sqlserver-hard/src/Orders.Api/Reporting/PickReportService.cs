using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Reporting;

public sealed record PickDay(DateOnly Day, string Zone, int Picks);

// The picks of each zone for each day in Amsterdam time, for the shift leaders.
public sealed class PickReportService(OrdersDbContext db)
{
    private static readonly TimeZoneInfo s_amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    public async Task<IReadOnlyList<PickDay>> BuildAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        // PickedAt holds UTC: the range starts and ends at midnight in Amsterdam.
        var start = TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), s_amsterdam);
        var end = TimeZoneInfo.ConvertTimeToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue), s_amsterdam);

        // The shift leaders refresh this page all day. Do not hold up the pickers.
        return await db.Database.SqlQuery<PickDay>($"""
            SELECT CAST(p.PickedAt AT TIME ZONE 'W. Europe Standard Time' AS date) AS Day,
                   p.Zone,
                   COUNT(*) AS Picks
            FROM dbo.PickTasks AS p WITH (NOLOCK)
            WHERE p.Status = 2 AND p.PickedAt >= {start} AND p.PickedAt < {end}
            GROUP BY CAST(p.PickedAt AT TIME ZONE 'W. Europe Standard Time' AS date), p.Zone
            """).OrderBy(row => row.Day).ThenBy(row => row.Zone).ToListAsync(ct);
    }
}
