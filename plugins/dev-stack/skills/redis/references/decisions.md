# Cache decisions

The team decisions that cache code needs. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

## Contents

- CACHE-01. Caching layer
- CACHE-02. Cache server: Redis or Valkey
- CACHE-03. Cache key naming
- CACHE-04. Default TTLs
- CACHE-05. Invalidation style
- CACHE-06. Cache serialization
- CACHE-07. Fresh L1 on all nodes
- CACHE-08. Distributed lock

## CACHE-01. Caching layer

- Question: Does this service need a cache, and if so, HybridCache in the process only, or HybridCache with Redis or Valkey as L2?
- Ask when: the first cache in a service, or a second instance of a service that already caches in memory.
- Options: No cache: read the database each time. HybridCache, L1 only: in the process, for one instance. HybridCache with L2: L1 in each process plus a shared Redis or Valkey. `IMemoryCache` or `IDistributedCache` directly: older APIs without stampede protection or tags.
- Recommended: No cache until a measurement shows a need (latency, database load). Then HybridCache: L1 only for one instance, with Redis or Valkey as L2 for more than one instance. HybridCache gives L1, L2, stampede protection, and tags in one API.
- Choose another option when: the service already uses `IMemoryCache` or `IDistributedCache` (existing code keeps its style). Many cached types need a fresh L1 on all nodes: FusionCache with its backplane (see CACHE-07).
- Depends on: none.
- Revisit when: HybridCache gets a backplane (github.com/dotnet/extensions issue 5517).
- Check: judge.
- Detect: DET-CACHE-01.

## CACHE-02. Cache server: Redis or Valkey

- Question: Which server is the shared cache: Valkey, Redis Open Source, or a managed service?
- Ask when: the first L2 cache.
- Options: Valkey 9: BSD license, Linux Foundation, API compatible with Redis. Redis Open Source 8: AGPLv3, RSALv2, or SSPLv1. A managed service (Azure Managed Redis, AWS ElastiCache or MemoryDB): no server to operate.
- Recommended: Valkey 9 for self-hosted servers. Write the code server-neutral with StackExchange.Redis 3.x, which works with both servers. Then a move to Redis or to a managed service changes only the connection string.
- Choose another option when: the platform team runs a managed service. A Redis-only feature is necessary (for example Redis Query Engine modules).
- Depends on: CACHE-01.
- Revisit when: Valkey and Redis APIs drift apart in a feature that the code uses.
- Check: judge.
- Detect: DET-CACHE-02.

## CACHE-03. Cache key naming

- Question: Which key format do we use?
- Ask when: the first cache key.
- Options: `InstanceName = "{service}:"` plus key `v{n}:{entity}:{id}[:{variant}]`: the full key is `{service}:v{n}:{entity}:{id}`. Free text keys: no structure. The service in each key and in `InstanceName`: the prefix comes two times.
- Recommended: `InstanceName = "{service}:"` and key `v{n}:{entity}:{id}[:{variant}]`. Lowercase, internal IDs only. The version segment lets a deploy with a new DTO shape ignore the old values. `InstanceName` already adds the service, so the key does not repeat it.
- Choose another option when: more than one service shares one cache database and an existing format exists. Keep that format.
- Depends on: CACHE-01.
- Revisit when: a key collision or a cache flood from user input.
- Check: judge.
- Detect: DET-CACHE-03.

## CACHE-04. Default TTLs

- Question: Which default expiration do cache entries get in L2 and in L1?
- Ask when: the first `AddHybridCache`.
- Options: Global defaults: `Expiration` 5 minutes, `LocalCacheExpiration` 1 minute. No default: each call sets its own TTL. Long TTLs (hours): fewer database reads, more stale data.
- Recommended: Global `Expiration` 5 minutes and `LocalCacheExpiration` 1 minute, with an override per entry for data with a known change rate. The L1 TTL is the maximum staleness on other nodes, so keep it short.
- Choose another option when: the product owner states a staleness limit for a type. Use that limit as the L1 TTL, or `DisableLocalCache` (CACHE-07).
- Depends on: CACHE-01.
- Revisit when: the cache hit rate is low, or users report stale data.
- Check: judge.
- Detect: DET-CACHE-04.

## CACHE-05. Invalidation style

- Question: How does the code remove a cached value after a change?
- Ask when: the first cached value that the service also changes.
- Options: Explicit `RemoveAsync` after the commit. Tags for groups (`RemoveByTagAsync`). TTL only: no removal, wait for the expiration. Write-through: put the new value in the cache from the write path.
- Recommended: Explicit `RemoveAsync` after the commit, tags for groups (for example `tenant:{id}`), and a short L1 TTL. A tag removal does not reach the L1 of other nodes either, so the L1 TTL stays the limit there.
- Choose another option when: the data changes only through a batch job: TTL only. Never write-through from a request: two writers can store the older value last.
- Depends on: CACHE-01, CACHE-04.
- Revisit when: the service gets an event that tells all nodes about a change (then a backplane, CACHE-07).
- Check: judge.
- Detect: DET-CACHE-05.

## CACHE-06. Cache serialization

- Question: How does HybridCache serialize cached values?
- Ask when: the first cached type.
- Options: System.Text.Json with a source-generated context, plugged in with a keyed `JsonSerializerOptions` (key `typeof(IHybridCacheSerializer<>)`): one line for all types. `AddSerializer<T, TImpl>()` per type: documented, more code. The default reflection serializer: no code, slower, and no error when a type cannot round-trip. MessagePack or protobuf: smaller, one more package.
- Recommended: System.Text.Json with a source-generated context, DTO records only, plugged in with the keyed `JsonSerializerOptions`. The keyed key is not on Microsoft Learn, so add a round-trip test for each cached DTO. A change in the library then breaks a test and not production.
- Choose another option when: the payloads are large and a measurement shows that JSON costs too much. Then MessagePack, with the approval of the package.
- Depends on: CACHE-01.
- Revisit when: Microsoft documents the keyed options, or a new HybridCache version changes the serializer lookup.
- Check: judge (round-trip test).
- Detect: DET-CACHE-06.

## CACHE-07. Fresh L1 on all nodes

- Question: How do all nodes see a change at once, when HybridCache has no backplane?
- Ask when: the first cached value that must be fresh on all nodes after a change (status, price, permission).
- Options: No backplane: `Flags = HybridCacheEntryFlags.DisableLocalCache` for data that must be fresh, a short L1 TTL for other data. Own pub/sub eviction: about 15 lines, a lost message leaves a stale L1. FusionCache with its Redis backplane (MIT): a second cache library.
- Recommended: No backplane. Use `DisableLocalCache` for data that must be fresh on each node, so each read goes to L2. Use FusionCache with its backplane when many cached types need a fresh L1.
- Choose another option when: L2 latency for the fresh data is too high in a measurement. Then FusionCache.
- Depends on: CACHE-01, CACHE-05.
- Revisit when: HybridCache ships a backplane (github.com/dotnet/extensions issue 5517).
- Check: judge, and a test with two app instances on one cache (`references/testing.md`).
- Detect: DET-CACHE-07.

## CACHE-08. Distributed lock

- Question: Which lock do we use to prevent duplicate work across instances?
- Ask when: the first job or handler that must not run two times at once.
- Options: `LockTakeAsync` and `LockReleaseAsync` with a token: built into StackExchange.Redis, for short work. DistributedLock.Redis (MIT): detects a lost lock (`HandleLostToken`) and extends it. RedLock.net: the RedLock algorithm, unsafe under GC pauses and failovers. A database lock (advisory lock, `sp_getapplock`): when the work is in the database already.
- Recommended: `LockTakeAsync` and `LockReleaseAsync` with a token for short work. DistributedLock.Redis when the work can outlive the TTL. A lock only prevents duplicate work: correctness comes from a database constraint or a concurrency token.
- Choose another option when: the work changes one database only. A database lock has no second system.
- Depends on: CACHE-02.
- Revisit when: a job needs a lock for longer than a few minutes.
- Check: tool (RDS-KEY-DELETE-LOCK, RDS-PKG-REDLOCK), judge.
- Detect: DET-CACHE-08.
