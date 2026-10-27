---
name: redis
description: Caching and Redis or Valkey code in .NET with HybridCache and StackExchange.Redis 3. Use when code or a review touches a cache (HybridCache, IDistributedCache, IMemoryCache, GetOrCreateAsync, cache keys, TTLs, invalidation, tags), AddStackExchangeRedisCache, IConnectionMultiplexer, Redis or Valkey keys, a distributed lock, pub/sub, streams, or a cached value that must be fresh on more than one instance. Not for output caching middleware or HTTP cache headers (dev-stack:aspnetcore), EF Core queries without a cache (dev-stack:efcore), or a message broker (dev-stack:rabbitmq, dev-stack:wolverine).
metadata:
  verified: "2026-10-27"
  versions: "Microsoft.Extensions.Caching.Hybrid 10.11.0, Microsoft.Extensions.Caching.StackExchangeRedis 10.0.14, StackExchange.Redis 3.4.20, Valkey 9.1, Redis Open Source 8.10, .NET 10"
---

# Redis and caching

Caching in .NET with HybridCache: L1 in the process, Redis or Valkey as L2. Also direct StackExchange.Redis code: the connection, keys, TTLs, locks, pub/sub, and streams. The write path of the data (`SaveChangesAsync`, then remove the key) starts in `dev-stack:efcore`. Output caching and HTTP cache headers are in `dev-stack:aspnetcore`.

## First

1. Read `references/stack-rules.md`.
2. Find the package versions: `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.Extensions.Caching.StackExchangeRedis`, and `StackExchange.Redis` in `*.csproj` or `Directory.Packages.props`. Find the image tag of the cache server in the compose files.
   - StackExchange.Redis 3.x: use this file.
   - StackExchange.Redis 2.x, or no direct reference: the cache package alone resolves 2.7.27. Run `dotnet package list --include-transitive` to see it. Add a direct 3.x reference in each change that touches cache code (rule 2), also when the task does not name the client.
   - No HybridCache, only `IDistributedCache` or `IMemoryCache`: existing code keeps its style. New cache code uses HybridCache (CACHE-01).
3. Count the instances of the app. One instance needs no L2. More than one instance needs L2, and each cached value needs a freshness rule (rule 6).

Done when you can name the package versions, the server (Redis or Valkey), and the number of instances.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Layer | No cache until a measured need. Then HybridCache. Redis or Valkey as L2 only for more than one instance. | CACHE-01 |
| Server and client | Valkey 9, server-neutral code, StackExchange.Redis 3.x with a direct reference | CACHE-02 |
| Keys | `InstanceName = "{service}:"`, key `v{n}:{entity}:{id}[:{variant}]` | CACHE-03 |
| TTL | `Expiration` 5 min, `LocalCacheExpiration` 1 min | CACHE-04 |
| Invalidation | `RemoveAsync` after the commit, tags for groups, short L1 TTL | CACHE-05 |
| Serialization | System.Text.Json with a source-generated context, `sealed` DTO records | CACHE-06 |
| Fresh on all nodes | No backplane. `DisableLocalCache` for data that must be fresh | CACHE-07 |
| Locks | `LockTakeAsync` and `LockReleaseAsync` with a token. DistributedLock.Redis for work that can outlive the TTL. | CACHE-08 |

## Existing code

In cache code, the defects of stack rule 3 include these. Each one gives stale or wrong data without an error, and a test with one app instance passes.

| Defect | Rule |
|--------|------|
| No direct `StackExchange.Redis` 3.x reference: the app runs on 2.7.27. | 2 |
| A write decision (a price to charge, stock, a permission) reads its value through the cache. | 5a |
| No `LocalCacheExpiration`: L1 lives as long as `Expiration` on each node. | 6 |
| `RemoveAsync` before `SaveChangesAsync`. | 7 |
| A key without a version segment for a type that the change gives new fields. | 9a |

New code that copies one of these patterns depends on the defect. So does new code that changes the data behind it. Fix both, and name the fix under "Changes".

## Rules

### Connection

1. Register one `IConnectionMultiplexer` as a singleton. Connect with `ConnectAsync` at startup, set `AbortOnConnectFail = false` and `ClientName`, and give the same instance to `AddStackExchangeRedisCache` through `ConnectionMultiplexerFactory`. A multiplexer per call uses up sockets, and `abortConnect=true` stops the app when the server is down for a moment.
2. Reference `StackExchange.Redis` 3.x directly, or add a `PackageVersion` when the repository uses `CentralPackageTransitivePinningEnabled`. `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.14 declares `StackExchange.Redis >= 2.7.27`, so NuGet takes 2.7.27 from 2023.
3. Await each Redis call. Set SER305 to SER308 to `error` in `.editorconfig`. Do not raise `ThreadPool.SetMinThreads` to fix timeouts. A thread that blocks on a Redis task starves the pool that must process the reply.

```csharp
var cacheOptions = ConfigurationOptions.Parse(
    builder.Configuration.GetConnectionString("Cache")
        ?? throw new InvalidOperationException("ConnectionStrings:Cache is not set."));
cacheOptions.AbortOnConnectFail = false; // start, and reconnect, while the server is down
cacheOptions.ClientName = "orders-api";
var cacheConnection = await ConnectionMultiplexer.ConnectAsync(cacheOptions);
builder.Services.AddSingleton<IConnectionMultiplexer>(cacheConnection);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(cacheConnection);
    options.InstanceName = "orders:";
});
builder.Services.AddKeyedSingleton<JsonSerializerOptions>(
    typeof(IHybridCacheSerializer<>),
    new JsonSerializerOptions { TypeInfoResolver = CacheJsonContext.Default });
builder.Services.AddHybridCache(options =>
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1),
    });
```

### Reads with HybridCache

4. Read through `HybridCache.GetOrCreateAsync`. Pass the state as an argument and use a `static` lambda. Do not write cache-aside by hand with `IDistributedCache` and a lock. HybridCache does L1, L2, and one factory call per key in one call.
5. Expect the factory to run one time per process, not one time per cluster. The stampede protection does not cross instances.
5a. Read a value for a write decision from the database, not from the cache. Examples: the price that checkout charges, stock, a permission. Find each write path that reads through a cached read method (for example a `ProductCache` in checkout). When the change makes that value change, fix the read. Then say in the report how the write path gets the current value.
6. L1 has no backplane. `RemoveAsync` and `RemoveByTagAsync` on node A clear the L1 of node A and the L2. Node B serves its L1 copy until `LocalCacheExpiration`. Give data that must be fresh on all nodes `Flags = HybridCacheEntryFlags.DisableLocalCache`. For other data, keep L1 shorter than L2: the L1 TTL is the maximum staleness. Without `LocalCacheExpiration`, L1 gets the `Expiration` of the same options. So `Expiration = 30 min` alone keeps a stale L1 copy for 30 minutes.

```csharp
public static readonly HybridCacheEntryOptions OrderEntry = new()
{
    Expiration = TimeSpan.FromMinutes(5),
    Flags = HybridCacheEntryFlags.DisableLocalCache, // a cancel must show on each node at once
};

var order = await cache.GetOrCreateAsync(
    $"v1:order:{id:N}",
    (db, id),
    static async (state, token) =>
    {
        var entity = await state.db.Orders.AsNoTracking().Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == state.id, token);
        return entity?.ToResponse(); // a DTO, never the entity
    },
    OrderEntry,
    cancellationToken: ct);
```

### Writes and invalidation

7. In the write path, save to the database first, then call `RemoveAsync`. Do not write the new value into the cache from the write path. Two writers can store the older value last.
8. Remove groups with tags (`tags: ["tenant:42"]`, then `RemoveByTagAsync("tenant:42")`). A tag removal is logical. The old values stay in L2 until they expire, and other nodes keep their L1 copies (rule 6). `RemoveByTagAsync("*")` removes all. A prefix delete and `KEYS` do not exist as a cache API.

```csharp
await db.SaveChangesAsync(ct);
await cache.RemoveAsync($"v1:order:{id:N}", ct); // after the commit, never SetAsync with the new value
```

### Keys, TTLs, and values

9. Build keys only from internal IDs or allow-listed values, with a delimiter and a version segment. Raw user input in a key lets a caller fill the cache, and keys without a delimiter collide.
9a. Increase the version segment (`v1:` to `v2:`) when the change adds, removes, or renames a field of a cached type. Add a segment when the existing key has none. During a rolling deploy, the old version writes the old shape under the same key. The new version reads it with default values (`0`, `null`) and no error. Give this reason in the report.
10. Give each direct Redis key a TTL (`StringSetAsync(key, value, expiry)`). Add jitter to keys that you write at the same time, so they do not expire at the same time. HybridCache sends `Expiration` to L2 itself.
11. Cache DTOs, never EF entities. An entity with private setters loses data in the JSON round trip, with no error. Mark a DTO `sealed` with `[ImmutableObject(true)]`, so L1 can return the same instance. Plug in the source-generated context with a keyed `JsonSerializerOptions` (key `typeof(IHybridCacheSerializer<>)`), and add a round-trip test for each cached DTO.
12. Keep each value under `MaximumPayloadBytes` (default 1 MB). HybridCache logs a larger value and does not store it.

### Locks, pub/sub, and streams

13. Use a Redis lock only to prevent duplicate work. Release it with its token (`LockReleaseAsync`), never with `KeyDeleteAsync`. Protect correctness with a database constraint or a concurrency token. Do not use RedLock. A lock that expires during a GC pause or a failover has two holders.
14. Use pub/sub only for signals that can get lost, with `RedisChannel.Literal`. Use a stream with a consumer group and `StreamAcknowledgeAsync` for work. Put cache data and durable data on separate instances, and set `maxmemory` with `allkeys-lru` on the cache instance.

Detail and examples: `references/hybridcache.md` and `references/stackexchange-redis.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `using var redis = ConnectionMultiplexer.Connect(cs);` in a method or a scoped service | One singleton from `ConnectAsync` at startup |
| `db.StringGetAsync(key).Result` | `await db.StringGetAsync(key)` |
| `ThreadPool.SetMinThreads(200, 200)` to fix timeouts | Remove the sync over async (SER307) |
| `cache.GetOrSetAsync(...)`, `services.AddRedisCache(...)`, `AddHybridCache().AddRedis(...)`, `db.GetAsync<T>(...)` | `GetOrCreateAsync`, `AddStackExchangeRedisCache` plus `AddHybridCache` |
| `server.Keys(pattern: "orders:*")` to clear a prefix | Tags (`RemoveByTagAsync`) or a new version segment in the key |
| `await cache.SetAsync(key, updated)` before or after `SaveChangesAsync` | `SaveChangesAsync`, then `RemoveAsync(key)` |
| `GetOrCreateAsync(key, ct => db.Orders.Include(o => o.Lines)...)` that returns the entity | Map to a `sealed record` DTO in the factory |
| `o.MaximumPayloadBytes = 1024 * 1024;` to "make big objects cache" | Nothing: 1 MB is the default, and HybridCache logs a skip |
| `AddKeyedSingleton(typeof(IHybridCacheSerializer<>), options)` | `AddKeyedSingleton<JsonSerializerOptions>(typeof(IHybridCacheSerializer<>), options)` (the other call is ambiguous, CS0121) |
| `await db.KeyDeleteAsync(lockKey)` to release a lock | `await db.LockReleaseAsync(lockKey, token)` |
| `subscriber.PublishAsync("channel", message)` | `subscriber.PublishAsync(RedisChannel.Literal("channel"), message)` |
| `services.AddDistributedMemoryCache()` as the shared L2 | A Redis or Valkey `IDistributedCache` (HybridCache ignores the memory one) |
| `<PackageVersion Include="StackExchange.Redis" Version="2.13.17" />` as "latest stable 2.x" | `Version="3.4.20"`: SER305 to SER308 exist only in 3.x, so on 2.x their `.editorconfig` lines do nothing |
| `new RedisBuilder().Build()` | `new RedisBuilder("valkey/valkey:9.1").Build()` |

## Check

1. Build with the analyzers as errors. Add this to `.editorconfig` first:

   ```
   [*.cs]
   dotnet_diagnostic.SER305.severity = error
   dotnet_diagnostic.SER306.severity = error
   dotnet_diagnostic.SER307.severity = error
   dotnet_diagnostic.SER308.severity = error
   ```

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings. CS0618 catches a string channel.

2. Check the resolved client version:

   ```
   dotnet package list --include-transitive
   ```

   Pass: `StackExchange.Redis` 3.x in each project. stackcheck cannot see a transitive version, so run this command.

3. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

4. Run the tests. Data that must be fresh on all nodes needs a test with two app instances on one Valkey container (`references/testing.md`). Pass: all tests green.

## Review checklist

The full list is in `references/review-checklist.md`.

1. One multiplexer for the process, `ConnectAsync`, `AbortOnConnectFail = false`. tool (RDS-MUX-CONNECT, RDS-ABORT-CONNECT)
2. `StackExchange.Redis` 3.x resolves in each project. tool (`dotnet package list --include-transitive`, RDS-PKG-SER2)
3. No sync over async on Redis calls, no `SetMinThreads`. tool (SER307, SER308, RDS-MIN-THREADS)
4. No `KEYS` or `server.Keys`. tool (RDS-KEYS-SCAN)
5. Locks release with their token. tool (RDS-KEY-DELETE-LOCK), judge
6. Reads use `GetOrCreateAsync`, not hand-written cache-aside. judge
7. Each cached value that must be fresh on all nodes has `DisableLocalCache` or a backplane. judge
7a. No write decision (checkout price, stock, permission) reads through the cache. This includes existing code that the change depends on. Severity: blocking. judge
8. The write path saves first, then removes the key. No `SetAsync` with the new value. judge
9. Cached values are DTOs, not EF entities, with a round-trip test. judge
10. Keys have a version segment and come from internal IDs, never raw user input. The version goes up when a cached type gets new fields. judge
11. Each direct Redis key has a TTL. L1 is shorter than L2. judge
12. Correctness does not depend on a Redis lock. judge
13. Work goes through streams with acknowledgement, not pub/sub. tool (CS0618 for string channels), judge
14. No old or unsafe packages (`Microsoft.Extensions.Caching.Redis`, `RedLock.net`). `dotnet package list --deprecated` does not flag them. tool (RDS-PKG-OLD, RDS-PKG-REDLOCK)

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run. For each cached type, state in one sentence why it is a DTO and not the entity (rule 11). Also give:

- Under "Packages": the `StackExchange.Redis` version that resolved before the change, and the direct 3.x reference (rule 2).
- Under "Changes": the key version and its reason (rule 9a), and how each write decision now reads its value (rule 5a).
- Under "Decisions": each CACHE decision that the change uses, at least CACHE-03, CACHE-04, and CACHE-07 when it touches keys, TTLs, or freshness. Mark each one accepted or assumed.

## References

- `references/decisions.md`: read before new code needs a CACHE decision. It has the question, the options, and the recommendation.
- `references/hybridcache.md`: read when you set up HybridCache, choose entry options or tags, plug in a serializer, or decide how nodes see a change.
- `references/stackexchange-redis.md`: read when you write direct Redis code: the connection, keys and TTLs, locks, pub/sub, streams, server settings, or timeouts.
- `references/testing.md`: read before you write a test for cached code or for Redis code.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
