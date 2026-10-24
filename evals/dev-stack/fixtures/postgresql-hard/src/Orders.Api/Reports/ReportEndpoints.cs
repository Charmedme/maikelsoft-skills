using Microsoft.AspNetCore.Http.HttpResults;
using Orders.Api.Common;
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
        if (!DateRange.IsValid(from, to))
        {
            return Problems.Validation(nameof(to), $"Give a range of at most {DateRange.MaxDays} days, with 'to' on or after 'from'.");
        }

        return TypedResults.Ok(await SalesReports.DailyAsync(db, from, to, ct));
    }
}
