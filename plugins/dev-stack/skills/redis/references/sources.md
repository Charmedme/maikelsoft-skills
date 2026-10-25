# Sources

Checked on 2026-10-19, and again on 2026-10-25. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile and test
- Credits

## Versions

| Item | Version on 2026-10-25 | Source |
|------|-----------------------|--------|
| StackExchange.Redis | 3.4.20 (2026-10-23, stability fixes only, no breaking change). 4.0 alpha. Depends on `RESPite` 3.4.20. | [nuget.org](https://www.nuget.org/packages/StackExchange.Redis), [release 3.4.20](https://github.com/StackExchange/StackExchange.Redis/releases/tag/3.4.20) |
| Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.13 and 11.0.0 RC 2. Both declare `StackExchange.Redis >= 2.7.27` (nuspec checked 2026-10-25) | [nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis) |
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

## Checked on 2026-10-25

With the .NET SDK 10.0.402 (.NET 10.0.13, the newest on 2026-10-25):

| Fact | Version | Source |
|------|---------|--------|
| The cache package alone resolves `StackExchange.Redis` 2.7.27. | Caching.StackExchangeRedis 10.0.13 | Lab: `dotnet package list --include-transitive` |
| A direct reference resolves 3.4.20 and brings `RESPite` 3.4.20. | StackExchange.Redis 3.4.20 | Lab |
| `.Result` on `StringGetAsync` gives SER307. `db.Wait(task)` gives SER308. A string channel gives CS0618. | 3.4.20 | Lab build |
| SER305 (await a batch or transaction command before `Execute`) and SER306 (read a fire-and-forget result) ship in the analyzer `StackExchange.Redis.Build.dll`. | 3.4.20 | Diagnostic texts in the analyzer assembly. Not reproduced in the lab |
| `AbortOnConnectFail` is `true` for a plain endpoint. Only Azure endpoints default to `false`. | 3.4.20 | Lab (`ConfigurationOptions.Parse`), [Configuration](https://stackexchange.github.io/StackExchange.Redis/Configuration) |
| Without `LocalCacheExpiration`, L1 uses the `Expiration` of the same options object. With neither, both are 5 minutes. | Caching.Hybrid 10.11.0 | [DefaultHybridCache.cs](https://github.com/dotnet/extensions/blob/main/src/Libraries/Microsoft.Extensions.Caching.Hybrid/Internal/DefaultHybridCache.cs), `GetEffectiveLocalCacheExpiration` |
| HybridCache has no backplane. The proposal is still an issue. | Caching.Hybrid 10.11.0 | [dotnet/runtime issue 125602](https://github.com/dotnet/runtime/issues/125602) |
| `MaximumPayloadBytes` default 1 MB, `MaximumKeyLength` 1024. | Caching.Hybrid 10.11.0 | [HybridCache library](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid#limits) |
| Newest versions: Caching.Hybrid 10.11.0, Testcontainers.Redis 4.16.0, DistributedLock.Redis 1.1.1, FusionCache 2.9.0, Valkey 9.1.2, Redis Open Source 8.10.2. No change since 2026-10-19. | - | nuget.org flat container, Docker Hub tags |

## Credits

- StackExchange.Redis documentation (MIT): paraphrased in `stackexchange-redis.md`.
- The owner's skill `valkey-caching`: the L1 section and the "when not to cache" list.
