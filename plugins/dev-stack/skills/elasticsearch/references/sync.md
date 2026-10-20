# Sync from the database

How a change in the database reaches the index: the outbox, the worker, the external version, deletes, and the refresh. Decision SEARCH-02.

## Contents

- Why not a second write in the request
- Outbox
- External version
- Worker
- Deletes
- Refresh

## Why not a second write in the request

```csharp
await db.SaveChangesAsync(ct);
await client.IndexAsync(document, ct); // wrong: dual write
```

When the second call fails (timeout, cluster down, 409), the database has the change and the index does not, and nothing retries. When the request is cancelled between the two calls, the same happens. Two requests for one order can also reach Elasticsearch in the other order.

## Outbox

Write an outbox row in the same transaction as the data. Then the change and the signal to index it commit together or not at all.

- With Wolverine (MSG-09 accepted): use its outbox (`dev-stack:wolverine`).
- Without Wolverine: an own table and a `BackgroundService` are enough for one indexer.

A compact way with EF Core is to override `SaveChangesAsync` (or use the DATA-14 interceptor). It increases the version and adds the outbox row for each changed aggregate:

```csharp
public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
{
    // Each change of an order gets a higher version and an outbox row in the same transaction.
    // The version is also the concurrency token, so two saves cannot write the same version.
    ChangeTracker.DetectChanges();
    foreach (var entry in ChangeTracker.Entries<Order>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList())
    {
        var version = entry.Property(order => order.Version);
        version.CurrentValue = version.OriginalValue + 1;
        SearchOutbox.Add(new SearchOutboxEntry(entry.Entity.Id));
    }

    return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
}
```

The outbox row holds the ID only. The worker reads the current state of the aggregate, so a late row never writes old data.

## External version

Elasticsearch keeps a version per document. With `VersionType.External`, a write succeeds only when its version is higher than the stored version. Else Elasticsearch answers 409. So an older update that arrives late cannot overwrite a newer one.

The version must grow with each change of the same document, also across instances and restarts:

| Source | Use |
|--------|-----|
| A `long Version` column that each save increments, and that is the concurrency token | Recommended. The row lock and the token make it strictly monotonic per order. |
| The outbox sequence (`bigint` identity) | Good when the worker reads current data. Two transactions can commit out of identity order, so combine it with a concurrency token. |
| SQL Server `rowversion` (8 bytes) | Safe: 64 bits, increases per database. |
| PostgreSQL `xmin` | Not safe: a 32-bit transaction ID that wraps around. After a wrap, a newer update has a smaller number and Elasticsearch drops it as stale. |
| A timestamp | Not safe: clocks of two instances differ, and two changes can share one millisecond. |

The request for a single document has `version_type=external&version=7` in the URL. In a bulk request the action line has `"version_type":"external","version":7`.

## Worker

```csharp
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
```

- Read a batch in ID order, index the current state, then delete exactly the processed rows. Do not delete `Id <= last`: a row with a lower ID from a transaction that commits later gets lost.
- `IndexAsync` skips 409 items and throws for other failed items (`references/client-and-errors.md`). The batch stays in the outbox and runs again.
- Catch exceptions in the loop, log them, and wait before the next try. An unhandled exception in a `BackgroundService` stops the host.
- More than one instance can run the worker. External versions make a second write of the same change harmless. Use `FOR UPDATE SKIP LOCKED` (`dev-stack:postgresql`) when the duplicate work costs too much.
- The worker needs its own scope per batch (`IServiceScopeFactory`), because the `DbContext` is scoped.

## Deletes

Send a delete with the external version too. Else a late index request for an old version brings the deleted document back.

```csharp
var response = await client.DeleteAsync<ProductDocument>(id, d => d.Index("products").VersionType(VersionType.External).Version(version), ct);
if (!response.IsValidResponse && response.ApiCallDetails.HttpStatusCode is not (404 or 409))
{
    throw new InvalidOperationException(response.DebugInformation);
}
```

For a soft delete, index the document with a `deleted` flag and filter it out, or delete it with the version of the delete.

## Refresh

A new document is searchable after the next refresh (default 1 second on an index that got a search in the last 30 seconds).

- Production code uses the default. Do not write `Refresh.True`: each forced refresh makes a small segment, and merges then cost CPU and I/O.
- `Refresh.WaitFor` waits for the next refresh. Use it only when the user must see their own write at once, and never in a bulk loop.
- Tests call `client.Indices.RefreshAsync("orders")` before they search.
