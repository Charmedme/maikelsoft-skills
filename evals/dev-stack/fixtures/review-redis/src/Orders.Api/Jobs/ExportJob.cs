using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using StackExchange.Redis;

namespace Orders.Api.Jobs;

public sealed partial class ExportJob(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopes,
    TimeProvider time,
    ILogger<ExportJob> logger) : BackgroundService
{
    private const string LockKey = "orders:v1:lock:export";
    private static readonly TimeSpan s_lockTime = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExportOnceAsync(stoppingToken);
        }
    }

    public async Task ExportOnceAsync(CancellationToken ct)
    {
        var db = redis.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        if (!await db.LockTakeAsync(LockKey, token, s_lockTime))
        {
            LogLockTaken(logger);
            return;
        }

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var orders = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var since = time.GetUtcNow().AddDays(-1);
            var count = await orders.Orders.CountAsync(order => order.PlacedAt >= since, ct);
            LogExported(logger, count);
        }
        finally
        {
            await db.KeyDeleteAsync(LockKey);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Another instance runs the export.")]
    private static partial void LogLockTaken(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exported {Count} orders.")]
    private static partial void LogExported(ILogger logger, int count);
}
