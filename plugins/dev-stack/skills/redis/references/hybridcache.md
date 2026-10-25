# HybridCache

Setup, entry options, freshness across nodes, tags, serialization, and limits. Package `Microsoft.Extensions.Caching.Hybrid` 10.11.0.

## Contents

- Setup
- How a read works
- Entry options and flags
- Freshness across nodes
- Tags
- Serialization
- Limits
- When not to cache

## Setup

`AddHybridCache` registers `HybridCache` as a singleton. When the container has an `IDistributedCache`, HybridCache uses it as L2. `AddDistributedMemoryCache` does not count: HybridCache ignores `MemoryDistributedCache`, because a second memory layer gives nothing.

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(cacheConnection);
    options.InstanceName = "orders:"; // CACHE-03: the service prefix comes from here
});
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),          // L2
        LocalCacheExpiration = TimeSpan.FromMinutes(1), // L1: the maximum staleness on other nodes
    };
});
```

`AddKeyedHybridCache` and `HybridCacheOptions.DistributedCacheServiceKey` exist in 10.11.0. Use them only when one service needs two caches with different L2 servers.

## How a read works

`GetOrCreateAsync(key, state, factory, options, tags, ct)`:

1. Look in L1 (memory of this process).
2. Look in L2 (Redis or Valkey).
3. Call the factory. Other callers in this process with the same key wait for the same call.
4. Write the value to L2 and L1.

The first caller gets the instance that the factory returned. Later L1 hits deserialize the stored bytes again, unless the type is `sealed` and has `[ImmutableObject(true)]`. Then L1 keeps and returns the instance.

The stampede protection works in one process only. With three instances, a cold key can call the factory three times.

Use the overload with state and a `static` lambda, so the call allocates no closure:

```csharp
var dto = await cache.GetOrCreateAsync(
    $"v1:product:{id}",
    (db, id),
    static async (state, token) => await state.db.Products
        .Where(p => p.Id == state.id)
        .Select(p => new ProductDto(p.Id, p.Name, p.Price))
        .SingleOrDefaultAsync(token),
    cancellationToken: ct);
```

## Entry options and flags

| Option | Effect |
|--------|--------|
| `Expiration` | TTL in L2. HybridCache sends it to `IDistributedCache` as an absolute expiration relative to now. |
| `LocalCacheExpiration` | TTL in L1. Keep it shorter than `Expiration`. When it is not set, L1 gets the `Expiration` of the same options object (source: `GetEffectiveLocalCacheExpiration`). With neither value set, both are 5 minutes. |
| `Flags = DisableLocalCache` | No L1 read and no L1 write. Each read goes to L2. |
| `Flags = DisableDistributedCache` | No L2. Use it for data that only this process needs. |
| `Flags = DisableLocalCacheRead`, `DisableLocalCacheWrite`, `DisableDistributedCacheRead`, `DisableDistributedCacheWrite` | One direction only. |
| `Flags = DisableUnderlyingData` | Do not call the factory. Read the cache only. |

Make the options `static readonly` fields. Do not build a new `HybridCacheEntryOptions` for each call.

## Freshness across nodes

HybridCache 10.11.0 has no backplane. The two-node lab showed:

- `RemoveAsync(key)` on node A removes the key from the L1 of node A and from L2.
- Node B keeps its L1 copy and returns the old value until `LocalCacheExpiration`.
- `RemoveByTagAsync(tag)` on node A behaves the same: node B keeps its copy.
- With `DisableLocalCache`, node B reads L2 and gets the new value.

So choose per type:

| Data | Entry options |
|------|---------------|
| Must be fresh at once on each node (status after a user action, price, permission) | `Flags = DisableLocalCache` |
| Can be stale for a known time | `LocalCacheExpiration` = that time |
| Changes only by deploy or by a batch job | Long TTLs, a new key version on change |

When many types need L1 and fresh data together, use FusionCache with its Redis backplane (decision CACHE-07). An own pub/sub eviction is possible, but a lost message leaves a stale L1 with no error.

## Tags

```csharp
await cache.GetOrCreateAsync(
    $"v1:invoice:{invoiceId}",
    (db, invoiceId),
    static async (state, token) => await InvoiceQueries.LoadAsync(state.db, state.invoiceId, token),
    tags: [$"tenant:{tenantId}"],
    cancellationToken: ct);

await cache.RemoveByTagAsync($"tenant:{tenantId}", ct);
```

- A tag removal is logical. HybridCache records the time of the removal and ignores older values on read. The old values stay in L2 until they expire.
- `RemoveByTagAsync("*")` removes all values.
- Tags have no wildcard and no prefix match. `RemoveByPrefixAsync` does not exist.

## Serialization

The default serializer is System.Text.Json with reflection. For a source-generated context, register a keyed `JsonSerializerOptions` with the key `typeof(IHybridCacheSerializer<>)`. Use the generic `AddKeyedSingleton<JsonSerializerOptions>`: the non-generic call with a `Type` is ambiguous (CS0121).

```csharp
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OrderResponse))]
internal sealed partial class CacheJsonContext : JsonSerializerContext;

builder.Services.AddKeyedSingleton<JsonSerializerOptions>(
    typeof(IHybridCacheSerializer<>),
    new JsonSerializerOptions { TypeInfoResolver = CacheJsonContext.Default });
```

The lab showed that the stored JSON follows these options. Microsoft Learn does not document the key. A test that reads a cached DTO back from L2 protects you against a change (`references/testing.md`).

An EF entity is not a cache value. The fixture entity `Order` has private setters and a public constructor. It came back from JSON with `Id = Guid.Empty`, the status from the constructor, and no lines, with no error. Map to a DTO in the factory.

For one type with special needs, `AddSerializer<T, TImpl>()` on the builder of `AddHybridCache` registers an own `IHybridCacheSerializer<T>`.

## Limits

| Option | Default | Effect |
|--------|---------|--------|
| `MaximumPayloadBytes` | 1 MB | A larger value is logged and not stored. The lab confirmed it. |
| `MaximumKeyLength` | 1024 characters | A longer key is logged and the cache is skipped. |

Do not set `MaximumPayloadBytes` to 1 MB "to make big values cache". That is the default. Cache a smaller DTO instead.

## When not to cache

- The data must be correct at the moment of a write decision (stock, balance, the price that checkout charges). Read the database. A checkout that reads the price through the product cache charges the old price on other nodes until their L1 expires.
- The query is fast and the load is low. A cache adds staleness and code for no gain.
- The data is per user and rarely read two times. The hit rate stays low.
