using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Catalog;
using Orders.Api.Data;
using Orders.Api.Shipping;

namespace Orders.Api.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdmin(this IEndpointRouteBuilder app)
    {
        // Staff only: the admin endpoints are not part of the partner contract.
        var admin = app.MapGroup("/admin").WithTags("Admin").AddEndpointFilter<AdminKeyFilter>().ExcludeFromDescription();
        admin.MapGet("/stats", GetStatsAsync).WithName("GetAdminStats");
        admin.MapPost("/products/{sku}/deactivate", DeactivateProductAsync).WithName("DeactivateProduct");
        admin.MapShippingAdmin();
        return app;
    }

    /// <summary>Counts orders, returns, and products for the back-office dashboard.</summary>
    private static async Task<Ok<AdminStatsResponse>> GetStatsAsync(OrdersDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await AdminStats.ReadAsync(db, ct));

    /// <summary>Takes a product out of the catalog. Existing orders keep the product.</summary>
    private static async Task<Results<NoContent, NotFound>> DeactivateProductAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
        var key = Product.Normalize(sku);
        var product = await db.Products.SingleOrDefaultAsync(p => p.Sku == key, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.Deactivate();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
