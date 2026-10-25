using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Products;

public static class ProductEndpoints
{
    private const int PageSize = 100;

    public static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Products");
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{sku}", GetAsync);
        group.MapPut("/{sku}/price", ChangePriceAsync);
        group.MapPost("/{sku}/deactivate", DeactivateAsync);
        return app;
    }

    private static async Task<Results<Created<ProductResponse>, Conflict<string>>> CreateAsync(
        CreateProductRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        if (await db.Products.AnyAsync(p => p.Sku == request.Sku, ct))
        {
            return TypedResults.Conflict("A product with this SKU exists.");
        }

        var product = new Product(request.Sku, request.Name, request.Price);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/products/{product.Sku}", product.ToResponse());
    }

    private static async Task<Ok<List<ProductResponse>>> ListAsync(int? page, OrdersDbContext db, CancellationToken ct)
    {
        var skip = Math.Max(0, (page ?? 1) - 1) * PageSize;
        var products = await db.Products.AsNoTracking()
            .Where(p => p.Active)
            .OrderBy(p => p.Sku)
            .Skip(skip)
            .Take(PageSize)
            .ToListAsync(ct);
        return TypedResults.Ok(products.Select(p => p.ToResponse()).ToList());
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Sku == sku, ct);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product.ToResponse());
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> ChangePriceAsync(
        string sku,
        ChangePriceRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Sku == sku, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.ChangePrice(request.Price);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(product.ToResponse());
    }

    private static async Task<Results<NoContent, NotFound>> DeactivateAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Sku == sku, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.Deactivate();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
