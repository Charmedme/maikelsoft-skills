using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Orders.Api.Data;
using Orders.Api.Products;

namespace Orders.Api.Caching;

public sealed class SearchCache(HybridCache cache, OrdersDbContext db)
{
    private const int PageSize = 20;

    public async Task<List<PriceResponse>> SearchAsync(string query, CancellationToken ct) =>
        await cache.GetOrCreateAsync(
            $"search:{query}",
            async token => await db.Products
                .AsNoTracking()
                .Where(product => product.Sku.StartsWith(query))
                .OrderBy(product => product.Sku)
                .Take(PageSize)
                .Select(product => new PriceResponse(product.Sku, product.Price))
                .ToListAsync(token),
            cancellationToken: ct);
}
