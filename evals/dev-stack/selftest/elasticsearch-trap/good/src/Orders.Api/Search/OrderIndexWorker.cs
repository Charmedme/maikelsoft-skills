using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Search;

// Reads the outbox, indexes the current state of each changed order, and deletes the rows.
// A failed batch stays in the outbox and runs again.
public sealed partial class OrderIndexWorker(
    IServiceScopeFactory scopes,
    TimeProvider time,
    ILogger<OrderIndexWorker> logger) : BackgroundService
{
    private const int BatchSize = 200;
    private static readonly TimeSpan s_idleDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan s_errorDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var indexReady = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var index = scope.ServiceProvider.GetRequiredService<OrderIndex>();
                if (!indexReady)
                {
                    await index.EnsureCreatedAsync(stoppingToken);
                    indexReady = true;
                }

                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                if (!await ProcessBatchAsync(db, index, stoppingToken))
                {
                    await Task.Delay(s_idleDelay, time, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogBatchFailed(logger, exception);
                await Task.Delay(s_errorDelay, time, stoppingToken);
            }
        }
    }

    private static async Task<bool> ProcessBatchAsync(OrdersDbContext db, OrderIndex index, CancellationToken ct)
    {
        var batch = await db.SearchOutbox.AsNoTracking().OrderBy(e => e.Id).Take(BatchSize).ToListAsync(ct);
        if (batch.Count == 0)
        {
            return false;
        }

        var orderIds = batch.Select(e => e.OrderId).Distinct().ToList();
        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync(ct);

        await index.IndexAsync([.. orders.Select(OrderDocument.From)], ct);

        // Delete only the rows of this batch. A row with a lower id can still be in a transaction.
        var entryIds = batch.Select(e => e.Id).ToList();
        await db.SearchOutbox.Where(e => entryIds.Contains(e.Id)).ExecuteDeleteAsync(ct);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Indexing a batch of orders failed. The batch runs again.")]
    private static partial void LogBatchFailed(ILogger logger, Exception exception);
}
