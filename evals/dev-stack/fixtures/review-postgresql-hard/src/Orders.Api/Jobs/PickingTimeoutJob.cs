using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Orders;
using Orders.Api.Picking;

namespace Orders.Api.Jobs;

/// <summary>Puts orders back in the queue when a picker started them and did not finish in time.</summary>
public sealed class PickingTimeoutJob(
    IServiceScopeFactory scopes,
    TimeProvider time,
    IOptions<PickingOptions> options,
    ILogger<PickingTimeoutJob> logger) : BackgroundService
{
    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var cutoff = time.GetUtcNow() - TimeSpan.FromMinutes(options.Value.TimeoutMinutes);

        // One statement for each run: the condition on the status makes it safe next to a picker that ships the order.
        return await db.Orders
            .Where(o => o.Status == OrderStatus.Picking && o.PickingStartedAt < cutoff)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, OrderStatus.Placed)
                .SetProperty(o => o.PickedBy, (string?)null)
                .SetProperty(o => o.PickingStartedAt, (DateTimeOffset?)null), ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.TimeoutJobEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5), time);
        do
        {
            try
            {
                var released = await RunOnceAsync(stoppingToken);
                if (released > 0 && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Put {Count} orders back in the picking queue.", released);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "The picking timeout job failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
