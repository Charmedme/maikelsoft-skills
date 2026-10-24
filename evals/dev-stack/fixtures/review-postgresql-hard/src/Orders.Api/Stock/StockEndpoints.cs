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
        var before = await db.StockLevels.AsNoTracking()
            .Where(s => s.Sku == sku)
            .Select(s => (int?)s.Available)
            .SingleOrDefaultAsync(ct);
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO stock_levels ("Sku", "Available", "UpdatedAt")
            VALUES ({sku}, {request.Available}, {now})
            ON CONFLICT ("Sku") DO UPDATE SET "Available" = EXCLUDED."Available", "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, ct);

        // Back in stock: mark the open alerts of this SKU, so that the mail job sends them.
        if (before is null or 0 && request.Available > 0)
        {
            var notifiedAt = time.GetLocalNow().DateTime;
            await db.BackInStockAlerts
                .Where(a => a.Sku == sku && a.NotifiedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.NotifiedAt, notifiedAt), ct);
        }

        var level = await db.StockLevels.AsNoTracking().SingleAsync(s => s.Sku == sku, ct);
        return TypedResults.Ok(level.ToResponse());
    }
}
