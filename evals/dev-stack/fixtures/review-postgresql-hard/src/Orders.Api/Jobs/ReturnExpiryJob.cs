using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Returns;

namespace Orders.Api.Jobs;

/// <summary>Expires the return requests whose parcel did not arrive in time. Each instance runs it; the UPDATE is idempotent.</summary>
public sealed class ReturnExpiryJob(
    IServiceScopeFactory scopes,
    TimeProvider time,
    IOptions<ReturnsOptions> options,
    ILogger<ReturnExpiryJob> logger) : BackgroundService
{
    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var cutoff = time.GetUtcNow() - TimeSpan.FromDays(options.Value.ExpiryDays);
        return await db.Returns
            .Where(r => r.Status == ReturnStatus.Requested && r.RequestedAt < cutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ReturnStatus.Expired), ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.ExpiryJobEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.ExpiryJobIntervalMinutes), time);
        do
        {
            try
            {
                var expired = await RunOnceAsync(stoppingToken);
                if (expired > 0 && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Expired {Count} return requests.", expired);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "The return expiry job failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
