using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Orders.Api.Data;

namespace Orders.Api.Catalog;

/// <summary>
/// Cached reads of the catalog. The product page and checkout both read products through this class.
/// </summary>
public sealed class ProductCache(HybridCache cache, IServiceScopeFactory scopes)
{
    private static readonly string[] s_tags = ["products"];

    public static string Key(string sku) => $"product:{Normalize(sku)}";

    public static string Normalize(string sku) => sku.Trim().ToUpperInvariant();

    public async Task<ProductResponse?> GetAsync(string sku, CancellationToken ct)
    {
        var normalized = Normalize(sku);
        return await cache.GetOrCreateAsync(
            Key(normalized),
            (scopes, normalized),
            static async (state, token) =>
            {
                await using var scope = state.scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == state.normalized, token);
                return product?.ToResponse();
            },
            tags: s_tags,
            cancellationToken: ct);
    }

    public ValueTask InvalidateAsync(string sku, CancellationToken ct) => cache.RemoveAsync(Key(sku), ct);
}
