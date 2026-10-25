using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Search;

namespace Orders.Api.Catalog;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Catalog");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{sku}", GetAsync);
        group.MapPut("/{sku}/price", ChangePriceAsync);
        group.MapPut("/{sku}/descriptions", DescribeAsync);
        group.MapDelete("/{sku}", DeactivateAsync);
        return app;
    }

    private static async Task<Results<Created<ProductResponse>, Conflict>> CreateAsync(CreateProductRequest request, OrdersDbContext db, CancellationToken ct)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(p => p.Sku == sku, ct))
        {
            return TypedResults.Conflict();
        }

        var product = new Product(sku, request.Name, request.Price, request.VatRate, request.Brand, request.Category);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/products/{product.Sku}", product.ToResponse());
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product.ToResponse());
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> ChangePriceAsync(string sku, ChangePriceRequest request, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.ChangePrice(request.Price);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(product.ToResponse());
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> DescribeAsync(
        string sku,
        ProductDescriptionsRequest request,
        OrdersDbContext db,
        ElasticsearchClient search,
        CancellationToken ct)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.Describe(request.Nl, request.De);
        await db.SaveChangesAsync(ct);

        // Merchandisers check the text in the shop right after they save it, so do not wait for the worker.
        var stock = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Sku == product.Sku, ct);
        await search.IndexAsync(ProductDocument.From(product, stock), i => i.Index(ProductIndex.Name).Refresh(Refresh.WaitFor), ct);
        return TypedResults.Ok(product.ToResponse());
    }

    private static async Task<Results<NoContent, NotFound>> DeactivateAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == normalized, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.Deactivate();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
