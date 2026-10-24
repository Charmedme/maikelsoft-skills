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
        group.MapGet("/monthly-sales", MonthlySalesAsync);
        group.MapGet("/unused-coupons", UnusedCouponsAsync);
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

    private static async Task<Results<Ok<List<MonthlySalesRow>>, ValidationProblem>> MonthlySalesAsync(
        DateOnly from,
        DateOnly to,
        OrdersDbContext db,
        CancellationToken ct)
    {
        if (!DateRange.IsValid(from, to))
        {
            return Problems.Validation(nameof(to), $"Give a range of at most {DateRange.MaxDays} days, with 'to' on or after 'from'.");
        }

        return TypedResults.Ok(await SalesReports.MonthlyAsync(db, from, to, ct));
    }

    private static async Task<Ok<List<UnusedCouponRow>>> UnusedCouponsAsync(OrdersDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await SalesReports.UnusedCouponsAsync(db, ct));
}
