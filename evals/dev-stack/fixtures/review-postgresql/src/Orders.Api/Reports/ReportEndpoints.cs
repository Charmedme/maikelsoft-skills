using Microsoft.AspNetCore.Http.HttpResults;
using Orders.Api.Import;

namespace Orders.Api.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReports(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/reports").WithTags("Reports");
        reports.MapGet("/sales", SalesAsync);
        reports.MapGet("/active-stock", ActiveStockAsync);

        app.MapPost("/stock/import", ImportAsync).WithTags("Stock");
        return app;
    }

    private static async Task<Ok<IReadOnlyList<SkuSales>>> SalesAsync(
        [AsParameters] SalesQuery query,
        SalesReport report,
        CancellationToken ct) =>
        TypedResults.Ok(await report.ForOrdersAsync(query.OrderIds, ct));

    private static async Task<Ok<IReadOnlyList<ActiveStock>>> ActiveStockAsync(
        DiscontinuedReport report,
        CancellationToken ct) =>
        TypedResults.Ok(await report.ActiveStockAsync(ct));

    private static async Task<Ok<int>> ImportAsync(
        HttpRequest request,
        StockImport import,
        CancellationToken ct) =>
        TypedResults.Ok(await import.ImportAsync(request.Headers["X-File-Name"].ToString(), request.Body, ct));

    public sealed record SalesQuery(Guid[] OrderIds);
}
