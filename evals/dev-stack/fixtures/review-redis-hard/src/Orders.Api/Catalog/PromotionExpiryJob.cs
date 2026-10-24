using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Orders.Api.Data;
using StackExchange.Redis;

namespace Orders.Api.Catalog;

/// <summary>
/// Ends the promotions that passed their end time, and removes those products from the cache.
/// The job runs on each instance; a Redis lock lets one instance do the work for each tick.
/// </summary>
public sealed class PromotionExpiryJob(
    IServiceScopeFactory scopes,
    IConnectionMultiplexer redis,
    HybridCache cache,
    TimeProvider time,
    ILogger<PromotionExpiryJob> logger) : BackgroundService
{
    private const string LockKey = "orders:v1:lock:promotion-expiry";
    private static readonly TimeSpan s_interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan s_lockExpiry = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(s_interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Ending expired promotions failed.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        var db = redis.GetDatabase();
        RedisValue token = Environment.MachineName + ":" + Guid.CreateVersion7().ToString("N");
        if (!await db.LockTakeAsync(LockKey, token, s_lockExpiry))
        {
            return;
        }

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var orders = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var now = time.GetUtcNow();
            var expired = await orders.Products
                .Where(p => p.PromotionEndsAt != null && p.PromotionEndsAt <= now)
                .ToListAsync(ct);
            foreach (var product in expired)
            {
                product.EndPromotion();
            }

            await orders.SaveChangesAsync(ct);
            foreach (var product in expired)
            {
                await cache.RemoveAsync($"orders:product:{product.Sku}", ct);
            }
        }
        finally
        {
            await db.LockReleaseAsync(LockKey, token);
        }
    }
}
