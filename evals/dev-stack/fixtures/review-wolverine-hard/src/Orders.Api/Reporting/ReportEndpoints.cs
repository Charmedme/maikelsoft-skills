using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Reporting;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReports(this IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/sales", GetSalesAsync).WithTags("Reporting");
        return app;
    }

    private static async Task<Results<Ok<SalesReport>, ValidationProblem>> GetSalesAsync(
        DateOnly from,
        DateOnly to,
        SalesReportService reports,
        CancellationToken ct)
    {
        if (to < from || to.DayNumber - from.DayNumber + 1 > SalesReportService.MaxDays)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(to)] = [$"Give a range of 1 to {SalesReportService.MaxDays} days."],
            });
        }

        return TypedResults.Ok(await reports.BuildAsync(from, to, ct));
    }
}
