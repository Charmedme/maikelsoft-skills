using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Search;

public static class OrderSearchEndpoints
{
    public static IEndpointRouteBuilder MapOrderSearch(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Order search");
        group.MapGet("/search", SearchAsync);
        group.MapGet("/export", Export);
        app.MapPost("/search/index", CreateIndexAsync).WithTags("Order search");
        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<OrderDocument>>> SearchAsync(
        string q,
        string status,
        DateTimeOffset placedFrom,
        int? page,
        OrderSearch search,
        CancellationToken ct) =>
        TypedResults.Ok(await search.SearchAsync(q, status, placedFrom, page ?? 0, ct));

    private static PushStreamHttpResult Export(OrderExport export, CancellationToken ct) =>
        TypedResults.Stream(stream => export.WriteCsvAsync(stream, ct), "text/csv", "orders.csv");

    private static async Task<NoContent> CreateIndexAsync(OrderIndex index, CancellationToken ct)
    {
        await index.EnsureCreatedAsync(ct);
        return TypedResults.NoContent();
    }
}
