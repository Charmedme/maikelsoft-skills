# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Connection and packages
- Reads
- Writes and invalidation
- Keys, TTLs, and values
- Locks, pub/sub, and streams
- Tests
- Decisions
- Severity
- Do not flag

## Connection and packages

| # | Check | Type |
|---|-------|------|
| 1 | One `IConnectionMultiplexer` for the process, made at startup with `ConnectAsync`. No `Connect` in a method, a constructor, or a scoped service. | tool (RDS-MUX-CONNECT), judge |
| 2 | `AbortOnConnectFail = false` (or `abortConnect=false`), and `ClientName` is set. | tool (RDS-ABORT-CONNECT) |
| 3 | `AddStackExchangeRedisCache` gets the same multiplexer through `ConnectionMultiplexerFactory`. | judge |
| 4 | `StackExchange.Redis` 3.x resolves in each project (a direct reference or a pinned `PackageVersion`). A direct 2.x reference (for example 2.13.17) is the old line: SER305 to SER308 do not exist there. | tool (`dotnet package list --include-transitive`, RDS-PKG-SER2) |
| 5 | No `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` on a Redis call. SER305 to SER308 are errors in `.editorconfig`. | tool (SER305, SER306, SER307, SER308) |
| 6 | No `ThreadPool.SetMinThreads` as a fix for timeouts. | tool (RDS-MIN-THREADS) |
| 7 | No `Microsoft.Extensions.Caching.Redis` (the old package). | tool (RDS-PKG-OLD) |
| 8 | No `AddDistributedMemoryCache` as the shared L2 in a service with more than one instance. | judge |

## Reads

| # | Check | Type |
|---|-------|------|
| 9 | Reads use `HybridCache.GetOrCreateAsync`, not hand-written cache-aside with `IDistributedCache` and a lock. | judge |
| 10 | The factory uses the state overload with a `static` lambda, or captures only cheap values. | judge |
| 11 | Each value that must be fresh on all nodes has `DisableLocalCache`, or the service has a backplane (CACHE-07). | judge |
| 12 | `LocalCacheExpiration` is shorter than `Expiration`, and short enough for the staleness that the product allows. | judge |
| 13 | Entry options are `static readonly`, not new for each call. | judge |
| 13a | No write decision (the price that checkout charges, stock, a permission) reads through the cache. Check existing write paths that the change depends on. A stale price that the shop charges is blocking. | judge |

## Writes and invalidation

| # | Check | Type |
|---|-------|------|
| 14 | The write path saves to the database first, then calls `RemoveAsync` or `RemoveByTagAsync`. | judge |
| 15 | The write path does not call `SetAsync` with the new value. | judge |
| 16 | No `KEYS`, `server.Keys`, or prefix delete to clear a group. Tags or a new key version instead. | tool (RDS-KEYS-SCAN) |
| 17 | The code does not expect a tag removal to reach the L1 of other nodes. | judge |

## Keys, TTLs, and values

| # | Check | Type |
|---|-------|------|
| 18 | Keys follow CACHE-03: `InstanceName = "{service}:"`, key `v{n}:{entity}:{id}`, lowercase, with delimiters. | judge |
| 18a | The key version goes up when the change adds, removes, or renames a field of a cached type. An existing key without a version gets one. Else the old version writes the old shape during the rolling deploy, and the new version reads default values. | judge |
| 19 | No raw user input in a key. Free text is normalized and hashed, and its variants are capped. | judge |
| 20 | Each direct key has a TTL. Keys written at the same time have jitter. | judge |
| 21 | Cached values are DTOs (`sealed record`, `[ImmutableObject(true)]`), never EF entities. | judge |
| 22 | A source-generated JSON context is registered with `AddKeyedSingleton<JsonSerializerOptions>(typeof(IHybridCacheSerializer<>), ...)`, or the project has an accepted other choice (CACHE-06). | judge |
| 23 | Values stay under `MaximumPayloadBytes` (1 MB). The code does not set it to 1 MB again. | judge |

## Locks, pub/sub, and streams

| # | Check | Type |
|---|-------|------|
| 24 | A lock is released with `LockReleaseAsync` and its token, never `KeyDeleteAsync`. | tool (RDS-KEY-DELETE-LOCK) |
| 25 | Correctness does not depend on the lock. A database constraint or a concurrency token protects the data. | judge |
| 26 | No RedLock. Work that can outlive the TTL uses DistributedLock.Redis (CACHE-08). | tool (RDS-PKG-REDLOCK), judge |
| 27 | Pub/sub carries only signals that can get lost. Channels use `RedisChannel.Literal` or `Pattern`. | tool (CS0618), judge |
| 28 | Work uses a stream with a consumer group and `StreamAcknowledgeAsync`, or a broker. | judge |
| 29 | Cache data and durable data are on separate instances. The cache instance has `maxmemory` and `allkeys-lru`. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 30 | Tests use a Valkey container with a pinned image, not a fake, for behavior across requests. | judge |
| 31 | A value that must be fresh on all nodes has a test with two app instances (read on B, change on A, read on B). | judge |
| 32 | Each cached DTO has a round-trip test (two reads on one node). | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 33 | New cache code follows the accepted CACHE decisions, or the output marks them as assumed. | judge |

## Severity

A cached value that makes the app charge or show a wrong price, stock, or status on other nodes is blocking. Examples: a write decision through the cache (13a), and a default L1 TTL on data that must be fresh (11). Also a cached computed price that outlives its end time.

## Do not flag

These patterns are correct. A finding against them is a false positive.

- `LockReleaseAsync(key, token)` in `finally`. It deletes the lock only when the token is still ours.
- A lock expiry longer than the timer interval, for example 2 minutes with a 1 minute timer. This is correct when the release is in `finally` and the work is idempotent. The lock only prevents duplicate work (check 25).
- A direct key on the raw multiplexer that carries the service prefix itself (`orders:v1:lock:...`). Only `IDistributedCache` adds `InstanceName`. A HybridCache key with the prefix again is a defect: it becomes `orders:orders:...`.
- A new DI scope for each run in a singleton `BackgroundService` that uses a scoped `DbContext`.
