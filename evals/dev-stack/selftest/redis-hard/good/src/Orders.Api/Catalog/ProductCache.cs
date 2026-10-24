using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Orders.Api.Data;

namespace Orders.Api.Catalog;

/// <summary>
/// Cached reads of the product page. Checkout does not use this cache: it reads the price from the database.
/// </summary>
public sealed class ProductCache(HybridCache cache, IServiceScopeFactory scopes)
{
    // No L1: HybridCache has no backplane, and a price change or the end of a promotion must show on each instance at once.
    private static readonly HybridCacheEntryOptions s_entry = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        Flags = HybridCacheEntryFlags.DisableLocalCache,
    };

    private static readonly string[] s_tags = ["products"];

    // v2: the cached shape changed (ProductSnapshot). The old version writes "product:{sku}" during a rolling deploy.
    public static string Key(string sku) => $"v2:product:{Normalize(sku)}";

    public static string Normalize(string sku) => sku.Trim().ToUpperInvariant();

    public async Task<ProductSnapshot?> GetAsync(string sku, CancellationToken ct)
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
                return product is null ? null : ProductSnapshot.From(product);
            },
            s_entry,
            s_tags,
            ct);
    }

    public ValueTask InvalidateAsync(string sku, CancellationToken ct) => cache.RemoveAsync(Key(sku), ct);
}
