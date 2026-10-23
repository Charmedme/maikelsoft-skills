using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Catalog;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Products");
        group.MapGet("/", ListAsync).WithName("ListProducts");
        group.MapGet("/{sku}", GetAsync).WithName("GetProduct");
        return app;
    }

    /// <summary>Lists the active products.</summary>
    private static async Task<Ok<List<ProductResponse>>> ListAsync(OrdersDbContext db, CancellationToken ct)
    {
        var products = await db.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Sku)
            .ToListAsync(ct);

        return TypedResults.Ok(products.Select(product => product.ToResponse()).ToList());
    }

    /// <summary>Gets one product, also when it is no longer active.</summary>
    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetAsync(string sku, IProductCatalog catalog, CancellationToken ct)
    {
        var product = await catalog.FindAsync(sku, ct);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product.ToResponse());
    }
}
