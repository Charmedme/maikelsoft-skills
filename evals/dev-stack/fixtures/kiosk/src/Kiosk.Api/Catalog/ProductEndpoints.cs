using Kiosk.Api.Data;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Catalog;

public static class ProductEndpoints
{
    public const string CacheTag = "products";

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products");
        products.MapGet("/", ListAsync).CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(60)).Tag(CacheTag));
        products.MapGet("/{id:int}", GetAsync);
        products.MapGet("/search", SearchAsync);
        products.MapPut("/{id:int}", UpdateAsync);

        app.MapGet("/api/categories/{category}/products", async (string category, ProductQueries queries, CancellationToken ct) =>
            (await queries.InCategory(category).OrderBy(p => p.Name).ToListAsync(ct)).Select(ProductDto.From).ToList());
        return app;
    }

    private static async Task<List<ProductDto>> ListAsync(ProductQueries queries, CancellationToken ct) =>
        (await queries.Active().OrderBy(p => p.Name).ToListAsync(ct)).Select(ProductDto.From).ToList();

    private static async Task<IResult> GetAsync(int id, ProductQueries queries, CancellationToken ct) =>
        await queries.Active().SingleOrDefaultAsync(p => p.Id == id, ct) is { } product
            ? Results.Ok(ProductDto.From(product))
            : Results.NotFound();

    private static async Task<List<ProductSearchRow>> SearchAsync(string? q, ProductSearch search, CancellationToken ct) =>
        await search.SearchAsync(q, ct);

    private static async Task<IResult> UpdateAsync(int id, UpdateProductRequest request, KioskDbContext db, IOutputCacheStore cache, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, ct);
        if (product is null)
        {
            return Results.NotFound();
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.Stock = request.Stock;
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync(CacheTag, ct);
        return Results.Ok(ProductDto.From(product));
    }
}
