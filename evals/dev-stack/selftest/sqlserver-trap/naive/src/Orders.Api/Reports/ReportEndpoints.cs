using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReports(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reports").WithTags("Reports");
        group.MapGet("/daily-sales", DailySalesAsync);
        return app;
    }

    private static async Task<Results<Ok<List<DailySalesRow>>, ValidationProblem>> DailySalesAsync(
        DateOnly from,
        DateOnly to,
        OrdersDbContext db,
        CancellationToken ct)
    {
        if (to < from || to.DayNumber - from.DayNumber > 366)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["to"] = ["'to' must be on or after 'from', at most 366 days later."],
            });
        }

        var end = to.AddDays(1);

        // NOLOCK: the report must not block order placement.
        var rows = await db.Database.SqlQuery<DailySalesRow>($"""
            SELECT CAST(o.PlacedAt AT TIME ZONE 'Europe/Amsterdam' AS date) AS Day,
                   COUNT(DISTINCT o.Id) AS Orders,
                   SUM(l.Quantity * l.UnitPrice) AS Revenue
            FROM dbo.Orders AS o WITH (NOLOCK)
            JOIN dbo.OrderLines AS l WITH (NOLOCK) ON l.OrderId = o.Id
            WHERE o.Status <> 'Cancelled'
              AND o.PlacedAt >= (CAST({from} AS datetime2) AT TIME ZONE 'Europe/Amsterdam')
              AND o.PlacedAt < (CAST({end} AS datetime2) AT TIME ZONE 'Europe/Amsterdam')
            GROUP BY CAST(o.PlacedAt AT TIME ZONE 'Europe/Amsterdam' AS date)
            ORDER BY Day
            """).ToListAsync(ct);

        return TypedResults.Ok(rows);
    }
}
