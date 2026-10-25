using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
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
        var normalized = StockItem.Normalize(sku);
        var item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Sku == normalized, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item.ToResponse());
    }

    private static async Task<Ok<StockResponse>> ReceiveAsync(string sku, ReceiveStockRequest request, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = StockItem.Normalize(sku);

        // One statement: the warehouse posts a receipt for a new SKU or for a known one.
        // HOLDLOCK stops two parallel receipts of a new SKU from both inserting (error 2627).
        // The SKU goes as varchar(32), like the column, so SQL Server can seek the unique index.
        var skuParameter = new SqlParameter("@sku", SqlDbType.VarChar, 32) { Value = normalized };
        await db.Database.ExecuteSqlAsync($"""
            MERGE dbo.StockItems WITH (HOLDLOCK) AS t
            USING (VALUES ({skuParameter}, {request.Quantity})) AS s (Sku, Quantity)
                ON t.Sku = s.Sku
            WHEN MATCHED THEN
                UPDATE SET OnHand = t.OnHand + s.Quantity
            WHEN NOT MATCHED THEN
                INSERT (Sku, OnHand, Reserved) VALUES (s.Sku, s.Quantity, 0);
            """, ct);

        var item = await db.StockItems.AsNoTracking().FirstAsync(s => s.Sku == normalized, ct);
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
