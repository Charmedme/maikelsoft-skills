using Microsoft.Extensions.Caching.Hybrid;

namespace Orders.Api.Orders;

internal static class OrderCache
{
    // A cancel must show on all nodes at once. HybridCache has no backplane, so the order
    // has no copy in the memory of a node (L1). Each read goes to Valkey (CACHE-07).
    public static readonly HybridCacheEntryOptions Entry = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        Flags = HybridCacheEntryFlags.DisableLocalCache,
    };

    public static string Key(Guid id) => $"v1:order:{id:N}";
}
