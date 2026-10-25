using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Search;

// Reads the search outbox and indexes the current state of each changed product.
public sealed class ProductIndexWorker(
    IServiceScopeFactory scopes,
    ProductIndex index,
    TimeProvider time,
    ILogger<ProductIndexWorker> logger) : BackgroundService
{
    private const int BatchSize = 500;
    private const int BackfillBatchSize = 1000;
    private static readonly TimeSpan s_idleDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan s_errorDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await index.EnsureCurrentAsync(BackfillAsync, stoppingToken);
                break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "The search index is not ready");
                await Task.Delay(s_errorDelay, stoppingToken);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await ProcessBatchAsync(stoppingToken))
                {
                    await Task.Delay(s_idleDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Indexing products failed. The batch stays in the outbox");
                await Task.Delay(s_errorDelay, stoppingToken);
            }
        }
    }

    // Adds every product to the outbox, for a new physical index.
    private async Task BackfillAsync(CancellationToken ct)
    {
        var after = Guid.Empty;
        while (true)
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var ids = await db.Products.AsNoTracking()
                .Where(p => p.Id.CompareTo(after) > 0)
                .OrderBy(p => p.Id)
                .Select(p => p.Id)
                .Take(BackfillBatchSize)
                .ToListAsync(ct);
            if (ids.Count == 0)
            {
                return;
            }

            var now = time.GetUtcNow();
            db.ProductSearchOutbox.AddRange(ids.Select(id => new ProductSearchOutboxEntry(id, now)));
            await db.SaveChangesAsync(ct);
            after = ids[^1];
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var batch = await db.ProductSearchOutbox.AsNoTracking().OrderBy(e => e.Id).Take(BatchSize).ToListAsync(ct);
        if (batch.Count == 0)
        {
            return false;
        }

        var productIds = batch.Select(e => e.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToListAsync(ct);
        var skus = products.Select(p => p.Sku).ToList();
        var stock = await db.StockItems.AsNoTracking().Where(s => skus.Contains(s.Sku)).ToDictionaryAsync(s => s.Sku, ct);

        await index.IndexAsync([.. products.Select(p => ProductDocument.From(p, stock.GetValueOrDefault(p.Sku)))], ct);

        // Delete only the rows of this batch. A row with a lower id can still be in an open transaction.
        var entryIds = batch.Select(e => e.Id).ToList();
        await db.ProductSearchOutbox.Where(e => entryIds.Contains(e.Id)).ExecuteDeleteAsync(ct);
        return true;
    }
}
