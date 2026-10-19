using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Warehouse;

public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouse(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/warehouse").WithTags("Warehouse");
        group.MapPut("/stock/{sku}", UpsertAsync);
        group.MapGet("/stock", SearchAsync);
        group.MapPost("/reservations", ReserveAsync);
        group.MapPost("/picks", PickAsync);
        group.MapGet("/reports/daily-picks", DailyPicksAsync);
        return app;
    }

    private static async Task<NoContent> UpsertAsync(string sku, StockRequest request, StockRepository stock, CancellationToken ct)
    {
        await stock.UpsertAsync(sku, request.Available, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<StockLevel>>> SearchAsync(string prefix, string? sort, StockRepository stock, CancellationToken ct) =>
        TypedResults.Ok(await stock.SearchAsync(prefix, sort ?? "Sku", ct));

    private static async Task<Results<NoContent, Conflict>> ReserveAsync(ReservationRequest request, StockRepository stock, CancellationToken ct) =>
        await stock.ReserveAsync([.. request.Lines.Select(line => (line.Sku, line.Quantity))], ct)
            ? TypedResults.NoContent()
            : TypedResults.Conflict();

    private static async Task<Results<NoContent, Conflict>> PickAsync(PickRequest request, StockRepository stock, CancellationToken ct) =>
        await stock.PickAsync(request.Sku, request.Quantity, ct) ? TypedResults.NoContent() : TypedResults.Conflict();

    private static async Task<Ok<IReadOnlyList<DailyPicks>>> DailyPicksAsync(DateOnly from, DateOnly to, StockRepository stock, CancellationToken ct) =>
        TypedResults.Ok(await stock.DailyPicksAsync(from, to, ct));

    public sealed record StockRequest(int Available);

    public sealed record ReservationLine(string Sku, int Quantity);

    public sealed record ReservationRequest(IReadOnlyList<ReservationLine> Lines);

    public sealed record PickRequest(string Sku, int Quantity);
}
