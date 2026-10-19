using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Caching;
using Orders.Api.Data;

namespace Orders.Api.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Products");
        group.MapGet("/", SearchAsync);
        group.MapGet("/{sku}/price", GetPriceAsync);
        group.MapPut("/{sku}/price", ChangePriceAsync);
        group.MapPost("/prices/reset", ResetPricesAsync);
        return app;
    }

    private static async Task<Ok<List<PriceResponse>>> SearchAsync(
        string query,
        SearchCache search,
        CancellationToken ct) =>
        TypedResults.Ok(await search.SearchAsync(query, ct));

    private static async Task<Results<Ok<PriceResponse>, NotFound>> GetPriceAsync(
        string sku,
        PriceCache prices,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var cached = prices.GetPrice(sku);
        if (cached is not null)
        {
            return TypedResults.Ok(new PriceResponse(sku, cached.Value));
        }

        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Sku == sku, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        await prices.SetPriceAsync(sku, product.Price);
        return TypedResults.Ok(new PriceResponse(sku, product.Price));
    }

    private static async Task<Results<NoContent, NotFound>> ChangePriceAsync(
        string sku,
        ChangePriceRequest request,
        PriceCache prices,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Sku == sku, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        await prices.SetPriceAsync(sku, request.Price);
        product.ChangePrice(request.Price, time.GetUtcNow());
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ResetPricesAsync(PriceCache prices)
    {
        await prices.ClearAllAsync();
        return TypedResults.NoContent();
    }
}
