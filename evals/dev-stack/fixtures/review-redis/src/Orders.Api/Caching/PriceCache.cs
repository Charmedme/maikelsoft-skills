using System.Globalization;
using StackExchange.Redis;

namespace Orders.Api.Caching;

public sealed class PriceCache : IDisposable
{
    private static readonly TimeSpan s_priceTtl = TimeSpan.FromHours(1);

    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public PriceCache(IConfiguration configuration)
    {
        _redis = ConnectionMultiplexer.Connect(configuration.GetConnectionString("Cache")!);
        _db = _redis.GetDatabase();
    }

    public decimal? GetPrice(string sku)
    {
        var value = _db.StringGetAsync($"orders:v1:price:{sku}").Result;
        return value.HasValue ? decimal.Parse(value.ToString(), CultureInfo.InvariantCulture) : null;
    }

    public async Task SetPriceAsync(string sku, decimal price)
    {
        await _db.StringSetAsync($"orders:v1:price:{sku}", price.ToString(CultureInfo.InvariantCulture), s_priceTtl);
    }

    public async Task ClearAllAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints()[0]);
        foreach (var key in server.Keys(pattern: "orders:v1:price:*"))
        {
            await _db.KeyDeleteAsync(key);
        }
    }

    public void Dispose() => _redis.Dispose();
}
