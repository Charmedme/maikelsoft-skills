using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Catalog;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Catalog");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{sku}", GetAsync);
        group.MapPut("/{sku}/price", ChangePriceAsync);
        group.MapPut("/{sku}/promotion", StartPromotionAsync);
        group.MapDelete("/{sku}/promotion", EndPromotionAsync);
        group.MapDelete("/{sku}", DeactivateAsync);
        return app;
    }

    private static async Task<Results<Created<ProductResponse>, Conflict>> CreateAsync(
        CreateProductRequest request,
        OrdersDbContext db,
        ProductCache cache,
        TimeProvider time,
        CancellationToken ct)
    {
        var sku = ProductCache.Normalize(request.Sku);
        if (await db.Products.AnyAsync(p => p.Sku == sku, ct))
        {
            return TypedResults.Conflict();
        }

        var product = new Product(sku, request.Name, request.Price, request.VatRate);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        // A read before the create can have cached "not found".
        await cache.InvalidateAsync(sku, ct);
        return TypedResults.Created($"/products/{product.Sku}", product.ToResponse(time.GetUtcNow()));
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetAsync(string sku, ProductCache cache, CancellationToken ct)
    {
        var product = await cache.GetAsync(sku, ct);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> ChangePriceAsync(
        string sku,
        ChangePriceRequest request,
        OrdersDbContext db,
        ProductCache cache,
        TimeProvider time,
        CancellationToken ct)
    {
        var normalized = ProductCache.Normalize(sku);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.ChangePrice(request.Price);
        await cache.InvalidateAsync(normalized, ct);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(product.ToResponse(time.GetUtcNow()));
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound, ValidationProblem>> StartPromotionAsync(
        string sku,
        StartPromotionRequest request,
        OrdersDbContext db,
        ProductCache cache,
        TimeProvider time,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();
        if (request.EndsAt <= now)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.EndsAt)] = ["The end of the promotion must be in the future."],
            });
        }

        var normalized = ProductCache.Normalize(sku);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.StartPromotion(request.PercentOff, request.EndsAt!.Value);
        await cache.InvalidateAsync(normalized, ct);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(product.ToResponse(now));
    }

    private static async Task<Results<NoContent, NotFound>> EndPromotionAsync(
        string sku,
        OrdersDbContext db,
        ProductCache cache,
        CancellationToken ct)
    {
        var normalized = ProductCache.Normalize(sku);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.EndPromotion();
        await cache.InvalidateAsync(normalized, ct);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeactivateAsync(
        string sku,
        OrdersDbContext db,
        ProductCache cache,
        CancellationToken ct)
    {
        var normalized = ProductCache.Normalize(sku);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.Deactivate();
        await db.SaveChangesAsync(ct);
        await cache.InvalidateAsync(normalized, ct);
        return TypedResults.NoContent();
    }
}
