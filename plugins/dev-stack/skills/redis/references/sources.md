# Sources

Checked on 2026-10-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile and test
- Credits

## Versions

| Item | Version on 2026-10-19 | Source |
|------|-----------------------|--------|
| StackExchange.Redis | 3.4.5 (2026-10-13). 4.0 alpha. | [nuget.org](https://www.nuget.org/packages/StackExchange.Redis) |
| Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.13 and 11.0.0 RC 2. Both declare `StackExchange.Redis >= 2.7.27` | [nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis) |
| Microsoft.Extensions.Caching.Hybrid | 10.11.0 (2026-10-14), no backplane | [nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Hybrid), [dotnet/runtime issue 125602](https://github.com/dotnet/runtime/issues/125602) |
| Testcontainers.Redis | 4.16.0 | [nuget.org](https://www.nuget.org/packages/Testcontainers.Redis) |
| DistributedLock.Redis | 1.1.1 | [nuget.org](https://www.nuget.org/packages/DistributedLock.Redis) |
| ZiggyCreatures.FusionCache | 2.9.0 | [nuget.org](https://www.nuget.org/packages/ZiggyCreatures.FusionCache) |
| Valkey server | 9.1.2, 9.0.6, 8.1.10 | [Docker Hub valkey/valkey](https://hub.docker.com/r/valkey/valkey/tags) |
| Redis Open Source | 8.10.2 | [Docker Hub redis](https://hub.docker.com/_/redis/tags) |

## Documentation

- [HybridCache library in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid): keys, limits, tags, the note on L1 and other nodes.
- [Distributed caching in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/caching/distributed): `AddStackExchangeRedisCache`, `InstanceName`.
- [StackExchange.Redis Basics](https://stackexchange.github.io/StackExchange.Redis/Basics): one multiplexer for the process.
- [StackExchange.Redis Configuration](https://stackexchange.github.io/StackExchange.Redis/Configuration): `abortConnect`, `ClientName`, TLS.
- [StackExchange.Redis Timeouts](https://stackexchange.github.io/StackExchange.Redis/Timeouts): the fields of a timeout message.
- [Sync over async in StackExchange.Redis](https://seredis.dev/SyncOverAsync): SER307.
- [Valkey documentation: eviction](https://valkey.io/topics/lru-cache/): `maxmemory`, `allkeys-lru`.
- [Valkey documentation: distributed locks](https://valkey.io/topics/distlock/): the limits of RedLock.
- [FusionCache backplane](https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/Backplane.md): the option for CACHE-07.
- [DistributedLock](https://github.com/madelson/DistributedLock): `HandleLostToken`.

## Checked by compile and test

On 2026-10-19 with the .NET SDK 10.0.402:

- Two HybridCache nodes on one L2: a fake `IDistributedCache` in the lab, and a real Valkey 9.1 container in the eval probe. After `RemoveAsync` and after `RemoveByTagAsync` on node A, node B returned its L1 copy. With `DisableLocalCache`, node B returned the new value.
- 20 parallel callers on one node gave 1 factory call.
- The fixture entity `Order` came back from the cache with `Id = Guid.Empty` and no lines.
- The keyed `JsonSerializerOptions` (key `typeof(IHybridCacheSerializer<>)`) changed the stored JSON. The non-generic `AddKeyedSingleton(Type, object)` call is ambiguous (CS0121).
- A value over `MaximumPayloadBytes` was not stored in L2. Defaults: 1048576 bytes, key length 1024.
- `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.13 alone resolved `StackExchange.Redis` 2.7.27. A `PackageVersion` 3.4.5 with transitive pinning resolved 3.4.5.
- `.Result` on `StringGetAsync` gave warning SER307. A string channel gave CS0618.
- `UseSetting` in a `WebApplicationFactory` was visible in `Program.cs` before `Build()`.
- All C# examples in this skill compiled with warnings as errors.

## Credits

- StackExchange.Redis documentation (MIT): paraphrased in `stackexchange-redis.md`.
- The owner's skill `valkey-caching`: the L1 section and the "when not to cache" list.
