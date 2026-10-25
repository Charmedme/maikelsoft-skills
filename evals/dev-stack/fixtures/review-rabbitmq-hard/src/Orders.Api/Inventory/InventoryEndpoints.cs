using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Inventory;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventory(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory").WithTags("Inventory");
        group.MapGet("/{sku}", GetAsync);
        group.MapPost("/{sku}/receipts", ReceiveAsync);
        group.MapPost("/{sku}/reservations", ReserveAsync);
        return app;
    }

    private static async Task<Results<Ok<StockResponse>, NotFound>> GetAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        var item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Sku == normalized, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item.ToResponse());
    }

    private static async Task<Ok<StockResponse>> ReceiveAsync(string sku, ReceiveStockRequest request, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Sku == normalized, ct);
        if (item is null)
        {
            item = new StockItem(normalized);
            db.StockItems.Add(item);
        }

        item.Receive(request.Quantity);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(item.ToResponse());
    }

    private static async Task<Results<Ok<StockResponse>, NotFound, Conflict<string>>> ReserveAsync(
        string sku,
        ReserveStockRequest request,
        StockReservationService reservations,
        CancellationToken ct)
    {
        var outcome = await reservations.ReserveAsync(sku, request.Quantity, ct);
        return outcome switch
        {
            { Item: null } => TypedResults.NotFound(),
            { Reserved: false } => TypedResults.Conflict("Not enough stock."),
            _ => TypedResults.Ok(outcome.Item.ToResponse()),
        };
    }
}
