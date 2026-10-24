using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Common;
using Orders.Api.Data;

namespace Orders.Api.Stock;

public static class StockEndpoints
{
    private const int MaxSkuLength = 32;

    public static IEndpointRouteBuilder MapStock(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/stock").WithTags("Stock");
        group.MapGet("/{sku}", GetAsync);
        group.MapPut("/{sku}", SetAsync);
        return app;
    }

    private static async Task<Results<Ok<StockResponse>, NotFound>> GetAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
        var level = await db.StockLevels.AsNoTracking().SingleOrDefaultAsync(s => s.Sku == sku, ct);
        return level is null ? TypedResults.NotFound() : TypedResults.Ok(level.ToResponse());
    }

    private static async Task<Results<Ok<StockResponse>, ValidationProblem>> SetAsync(
        string sku,
        SetStockRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (sku.Length > MaxSkuLength)
        {
            return Problems.Validation(nameof(sku), $"A SKU has at most {MaxSkuLength} characters.");
        }

        var now = time.GetUtcNow();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO stock_levels ("Sku", "Available", "UpdatedAt")
            VALUES ({sku}, {request.Available}, {now})
            ON CONFLICT ("Sku") DO UPDATE SET "Available" = EXCLUDED."Available", "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, ct);
        var level = await db.StockLevels.AsNoTracking().SingleAsync(s => s.Sku == sku, ct);
        return TypedResults.Ok(level.ToResponse());
    }
}
