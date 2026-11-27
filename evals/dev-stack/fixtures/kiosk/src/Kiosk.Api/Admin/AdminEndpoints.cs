using Kiosk.Api.Catalog;
using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Kiosk.Api.Orders;
using Kiosk.Api.Users;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Admin;

public sealed record CreateProductRequest(string Name, string Description, decimal Price, string Category, string Sku, string SupplierEmail, int Stock);

public static class AdminEndpoints
{
    public static IServiceCollection AddAdmin(this IServiceCollection services)
    {
        services.AddSingleton<ExportScheduler>();
        services.AddScoped<ShopStatistics>();
        return services;
    }

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin");
        admin.MapGet("/users", ListUsersAsync).RequireAuthorization(Policies.Admin);
        admin.MapGet("/products", ListProductsAsync).RequireAuthorization(Policies.Admin);
        admin.MapPost("/products", CreateProductAsync).RequireAuthorization(Policies.Admin);
        admin.MapDelete("/products/{id:int}", DeleteProductAsync).RequireAuthorization(Policies.Admin);
        admin.MapGet("/orders", ListOrdersAsync).RequireAuthorization(Policies.Staff);
        admin.MapDelete("/reviews/{id:int}", DeleteReviewAsync);
        admin.MapGet("/exports", (ExportScheduler scheduler) => scheduler.All).RequireAuthorization(Policies.Admin);
        admin.MapPost("/exports", (ExportSchedule schedule, ExportScheduler scheduler) =>
        {
            scheduler.Save(schedule);
            return Results.Ok(schedule);
        }).RequireAuthorization(Policies.Admin);
        return app;
    }

    private static async Task<List<UserSummary>> ListUsersAsync(KioskDbContext db, CancellationToken ct) =>
        await db.Users.AsNoTracking().OrderBy(u => u.Id).Select(u => new UserSummary(u.Id, u.Email, u.Role, u.DisplayName)).ToListAsync(ct);

    private static async Task<List<ProductDto>> ListProductsAsync(string? supplier, ProductQueries queries, CancellationToken ct)
    {
        var products = string.IsNullOrWhiteSpace(supplier) ? queries.Active() : queries.FromSupplier(supplier);
        return (await products.OrderBy(p => p.Name).ToListAsync(ct)).Select(ProductDto.From).ToList();
    }

    private static async Task<IResult> CreateProductAsync(CreateProductRequest request, KioskDbContext db, IOutputCacheStore cache, CancellationToken ct)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            Sku = request.Sku,
            SupplierEmail = request.SupplierEmail,
            Stock = request.Stock,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync(ProductEndpoints.CacheTag, ct);
        return Results.Created($"/api/products/{product.Id}", ProductDto.From(product));
    }

    private static async Task<IResult> DeleteProductAsync(int id, KioskDbContext db, TimeProvider time, IOutputCacheStore cache, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null)
        {
            return Results.NotFound();
        }

        product.DeletedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync(ProductEndpoints.CacheTag, ct);
        return Results.NoContent();
    }

    private static async Task<List<OrderDto>> ListOrdersAsync(KioskDbContext db, CancellationToken ct) =>
        (await db.Orders.AsNoTracking().Include(o => o.Lines).OrderByDescending(o => o.CreatedAt).Take(200).ToListAsync(ct))
            .Select(OrderDto.From).ToList();

    private static async Task<IResult> DeleteReviewAsync(int id, KioskDbContext db, CancellationToken ct) =>
        await db.Reviews.Where(r => r.Id == id).ExecuteDeleteAsync(ct) == 0 ? Results.NotFound() : Results.NoContent();
}
