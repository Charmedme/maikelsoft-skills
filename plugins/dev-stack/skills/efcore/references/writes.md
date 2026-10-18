# Writes

Rules for `SaveChanges`, set-based updates, concurrency, transactions, bulk work, and cache invalidation.

## Contents

- SaveChanges
- ExecuteUpdate and ExecuteDelete
- Concurrency tokens
- Uniqueness
- Transactions and the execution strategy
- Retries and idempotency
- Bulk inserts
- Large imports
- Cache invalidation after a write

## SaveChanges

- One `SaveChangesAsync` call per request or message (DATA-06). It sends all changes in one transaction, in batches.
- Follow the code into repositories, audit helpers, and progress hooks. A hidden `SaveChangesAsync` in a loop commits each row on its own. One final call does not undo the earlier commits.
- `Add` and `AddRange` do not call `DetectChanges`. Changing one to the other does not make an import faster.
- Do not read IDs in the order of the inserts. After `SaveChangesAsync`, read the ID from each entity object.

## ExecuteUpdate and ExecuteDelete

They send one `UPDATE` or `DELETE` at once, without a `SELECT`.

```csharp
var now = clock.GetUtcNow();
return db.Orders
    .Where(o => o.Status == OrderStatus.Draft && o.CreatedAt < before)
    .ExecuteUpdateAsync(s => s
        .SetProperty(o => o.Status, OrderStatus.Cancelled)
        .SetProperty(o => o.UpdatedAt, now), ct);
```

EF 10 accepts a block lambda. Use it for conditional setters, not a built expression tree:

```csharp
db.Orders
    .Where(o => o.Id == id)
    .ExecuteUpdateAsync(s =>
    {
        if (reference is not null)
        {
            s.SetProperty(o => o.Reference, reference);
        }

        if (deliveryDate is not null)
        {
            s.SetProperty(o => o.DeliveryDate, deliveryDate);
        }
    }, ct);
```

What they skip, and what that means:

| Skipped | Effect | What to do |
|---------|--------|------------|
| Change tracker | Tracked entities keep the old values. A later `SaveChangesAsync` can write the old values back. | Do not change the same rows through tracked entities in the same unit of work. |
| Concurrency token | No check. | Add the token to the `Where`, set a new value, and check the returned row count. |
| `SaveChanges` interceptors | No audit columns, no soft delete. | Set the audit columns in the setters. |
| Transaction | Each call commits on its own. | Use an explicit transaction when it must be atomic with other writes. |
| Client cascade delete | Children that EF deletes in memory stay. | Let the database cascade, or delete the children first. |

JSON columns: `ExecuteUpdate` can set properties inside a JSON column only when the column is a complex type.

## Concurrency tokens

SQL Server: `[Timestamp] public byte[] RowVersion { get; set; }` maps to `rowversion`. PostgreSQL: `[Timestamp] public uint Version { get; set; }` maps to `xmin`. Detail in the provider references.

For an update from an HTTP request, put the version from the client in the original value. Then EF compares it with the database:

```csharp
var order = await db.Orders.SingleAsync(o => o.Id == id, ct);
db.Entry(order).Property(o => o.RowVersion).OriginalValue = rowVersion;
order.Reference = reference;
try
{
    await db.SaveChangesAsync(ct);
    return true;
}
catch (DbUpdateConcurrencyException)
{
    return false; // the endpoint returns 409 Conflict
}
```

For a message handler, reload and retry, or move the message to the retry policy:

```csharp
catch (DbUpdateConcurrencyException ex)
{
    foreach (var entry in ex.Entries)
    {
        await entry.ReloadAsync(ct);
    }

    return false;
}
```

## Uniqueness

A check before the insert (`if (!await db.Users.AnyAsync(...)) db.Users.Add(...)`) lets two requests insert the same value. Put a unique index on the column and catch the violation:

```csharp
public static bool IsUniqueViolation(DbUpdateException ex) => ex.InnerException switch
{
    SqlException sql => sql.Number is 2601 or 2627,
    PostgresException pg => pg.SqlState == PostgresErrorCodes.UniqueViolation,
    _ => false,
};
```

## Transactions and the execution strategy

`SaveChangesAsync` already uses a transaction. Use an explicit transaction only when several calls must be atomic, for example `SaveChangesAsync` plus `ExecuteDeleteAsync`.

With `EnableRetryOnFailure`, each explicit transaction runs inside the execution strategy. Without it, EF throws `InvalidOperationException`.

```csharp
var strategy = db.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async token =>
{
    db.ChangeTracker.Clear(); // the delegate can run again: start clean
    await using var tx = await db.Database.BeginTransactionAsync(token);

    var order = await db.Orders.SingleAsync(o => o.Id == id, token);
    order.Status = OrderStatus.Shipped;
    await db.SaveChangesAsync(token);

    await db.Orders
        .Where(o => o.CustomerId == order.CustomerId && o.Status == OrderStatus.Draft)
        .ExecuteDeleteAsync(token);

    await tx.CommitAsync(token);
}, ct);
```

- Load the data inside the delegate. A retry then starts from the current database state.
- Keep the transaction short. Do not call an HTTP service, send a message, or wait for input inside it. Use an outbox for messages (MSG-09, `dev-stack:wolverine`).
- The default retry count is 6, with a maximum delay of 30 seconds.
- Isolation levels, deadlocks, and lock behavior are engine topics: `dev-stack:sqlserver`, `dev-stack:postgresql`.

## Retries and idempotency

A commit can succeed on the server while the client gets a network error. The retry then runs the work a second time. Make the unit idempotent, or give the strategy a check that tells if the commit happened:

```csharp
db.Shipments.Add(shipment);
await db.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
    token => db.SaveChangesAsync(acceptAllChangesOnSuccess: false, token),
    token => db.Shipments.AsNoTracking().AnyAsync(s => s.Id == shipment.Id, token),
    ct);
db.ChangeTracker.AcceptAllChanges();
```

`acceptAllChangesOnSuccess: false` keeps the entities in the `Added` state until the commit is certain. A key that the client generates (DATA-07) makes the check easy.

## Bulk inserts

Order of choice (DATA-22):

1. `ExecuteUpdateAsync` and `ExecuteDeleteAsync` for set-based changes.
2. `AddRange` and `SaveChangesAsync` in batches of some thousand rows, for medium imports. EF batches the inserts.
3. Provider bulk copy for large inserts. It skips the model, the interceptors, and value generation, so set each key and audit value yourself.
4. A third-party bulk library only after a license check and the approval of the human. `EFCore.BulkExtensions` has a dual license. `Z.EntityFramework.Extensions` is commercial. `BulkInsertAsync` on a `DbSet` comes from these packages, not from EF.

SQL Server, `SqlBulkCopy` on the connection of the context:

```csharp
var connection = (SqlConnection)db.Database.GetDbConnection();
await db.Database.OpenConnectionAsync(ct);
try
{
    using var copy = new SqlBulkCopy(connection) { DestinationTableName = "dbo.Parcels" };
    await copy.WriteToServerAsync(rows, ct);
}
finally
{
    await db.Database.CloseConnectionAsync();
}
```

PostgreSQL, binary `COPY`:

```csharp
var connection = (NpgsqlConnection)db.Database.GetDbConnection();
await db.Database.OpenConnectionAsync(ct);
try
{
    await using var writer = await connection.BeginBinaryImportAsync(
        "COPY parcels (barcode, weight_kg) FROM STDIN (FORMAT BINARY)", ct);
    foreach (var (barcode, weight) in rows)
    {
        await writer.StartRowAsync(ct);
        await writer.WriteAsync(barcode, NpgsqlTypes.NpgsqlDbType.Text, ct);
        await writer.WriteAsync(weight, NpgsqlTypes.NpgsqlDbType.Numeric, ct);
    }

    await writer.CompleteAsync(ct);
}
finally
{
    await db.Database.CloseConnectionAsync();
}
```

## Large imports

- The change tracker keeps each saved entity. With many rows, each `SaveChangesAsync` and each `ChangeTracker.Entries()` call scans more entities. Use a new context for each batch, or call `ChangeTracker.Clear()` after each batch when the batch owns the context.
- Count progress in your own variable, not with `ChangeTracker.Entries()`.
- Turn off `AutoDetectChangesEnabled` only for a measured problem. Restore it in `finally`, and call `DetectChanges()` before the save.
- A retried feed needs a unique key per input row or a stored checkpoint. Chunks alone do not make a feed idempotent.

## Cache invalidation after a write

When a cache holds the data (CACHE-01, `dev-stack:redis`):

1. Save to the database first.
2. Then remove the cache key or tag.

Do not put the new value in the cache from the write path. Two writers can then leave the old value in the cache. Cache DTOs, not EF entities: entities can have cycles and proxies.

Not compiled (fragment, `cache` is a `HybridCache`):

```csharp
await db.SaveChangesAsync(ct);                       // 1. source of truth
await cache.RemoveAsync($"orders:v1:order:{id}", ct); // 2. then invalidate
```

`ExecuteUpdateAsync` and `ExecuteDeleteAsync` change rows without entities. Remove the keys or tags of all changed rows after the call.
