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

        // The day is the calendar day in Amsterdam. The WHERE clause compares the bare column
        // with the UTC instants of local midnight, so the index on PlacedAt can serve it.
        var rows = await db.Database.SqlQuery<DailySalesRow>($"""
            SELECT (o."PlacedAt" AT TIME ZONE 'Europe/Amsterdam')::date AS "Day",
                   count(DISTINCT o."Id")::int AS "Orders",
                   sum(l."Quantity" * l."UnitPrice") AS "Revenue"
            FROM orders AS o
            JOIN order_lines AS l ON l."OrderId" = o."Id"
            WHERE o."Status" <> 'Cancelled'
              AND o."PlacedAt" >= ({from}::timestamp AT TIME ZONE 'Europe/Amsterdam')
              AND o."PlacedAt" < ({end}::timestamp AT TIME ZONE 'Europe/Amsterdam')
            GROUP BY 1
            ORDER BY 1
            """).ToListAsync(ct);

        return TypedResults.Ok(rows);
    }
}
