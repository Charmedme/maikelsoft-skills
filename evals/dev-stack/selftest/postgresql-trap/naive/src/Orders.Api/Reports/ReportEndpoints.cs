using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReports(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reports").WithTags("Reports");
        group.MapGet("/daily-sales", DailySalesAsync);
        return app;
    }

    private static async Task<Ok<List<DailySalesRow>>> DailySalesAsync(
        DateOnly from,
        DateOnly to,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rows = await db.Database.SqlQuery<DailySalesRow>($"""
            SELECT o."PlacedAt"::date AS "Day",
                   count(DISTINCT o."Id")::int AS "Orders",
                   sum(l."Quantity" * l."UnitPrice") AS "Revenue"
            FROM orders AS o
            JOIN order_lines AS l ON l."OrderId" = o."Id"
            WHERE o."Status" <> 'Cancelled' AND o."PlacedAt" >= {start} AND o."PlacedAt" < {end}
            GROUP BY 1
            ORDER BY 1
            """).ToListAsync(ct);
        return TypedResults.Ok(rows);
    }
}
