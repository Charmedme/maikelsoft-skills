using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Orders.Api.Health;

public sealed class CacheHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    private static readonly TimeSpan s_slow = TimeSpan.FromMilliseconds(250);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = redis.GetDatabase().PingAsync().Result;
            return Task.FromResult(latency > s_slow
                ? HealthCheckResult.Degraded($"The cache answered in {latency.TotalMilliseconds:F0} ms.")
                : HealthCheckResult.Healthy());
        }
        catch (RedisException ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("The cache does not answer.", ex));
        }
    }
}
