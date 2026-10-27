# StackExchange.Redis

Direct Redis or Valkey code with StackExchange.Redis 3.x: the connection, keys and TTLs, locks, pub/sub, streams, server settings, and timeouts.

## Contents

- Package version
- Connection
- Keys and TTLs
- Do not scan keys
- Locks
- Pub/sub and streams
- Server settings
- Timeouts

## Package version

`Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.14 (and 11.0 RC 3) declares `StackExchange.Redis >= 2.7.27`. NuGet takes the lowest version that matches, so the app runs on 2.7.27 from 2023 unless the project references 3.x.

With central package management and transitive pinning, a `PackageVersion` is enough:

```xml
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
</PropertyGroup>
<ItemGroup>
  <PackageVersion Include="StackExchange.Redis" Version="3.4.20" />
</ItemGroup>
```

Without transitive pinning, add a `PackageReference` to the project. Check the result with `dotnet package list --include-transitive`.

StackExchange.Redis 3.x brings its own analyzers. SER307 (blocking on a Redis call) and SER308 (blocking through `Wait` helpers) are warnings by default. SER305 (waiting on a command in a batch before `Execute`) and SER306 (reading a fire-and-forget result) find bugs. Set all four to `error`.

## Connection

One `ConnectionMultiplexer` serves the whole process. It is thread-safe and pipelines commands from many callers over one socket.

```csharp
var options = ConfigurationOptions.Parse(connectionString);
options.AbortOnConnectFail = false; // start, and reconnect in the background
options.ClientName = "orders-api";  // shows in CLIENT LIST on the server
IConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(options);
builder.Services.AddSingleton(mux);
```

- Connect at startup with `ConnectAsync`. `Connect` blocks a thread.
- `AbortOnConnectFail = false` (or `abortConnect=false` in the string): the app starts when the server is down and connects later. The default `true` throws at startup.
- Give the same instance to `AddStackExchangeRedisCache` through `ConnectionMultiplexerFactory`. Else the cache makes a second connection.
- Get `IDatabase` with `mux.GetDatabase()` when you need it. It is cheap. Do not store it in a field of a scoped service.
- Use TLS (`ssl=true`) and a password or ACL user from a secret store for a server outside the host.

## Keys and TTLs

- Key format (decision CACHE-03): `{service}:v{n}:{entity}:{id}[:{variant}]`. With `InstanceName = "{service}:"`, the code writes only `v{n}:{entity}:{id}`.
- Build keys from internal IDs or allow-listed values. Hash a long or free input (for example a search text) after you normalize it, and cap the number of variants.
- Give each key a TTL. A key without a TTL stays until the server evicts it, and with `noeviction` the server then refuses writes.

```csharp
var db = mux.GetDatabase();
var ttl = TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(Random.Shared.Next(0, 60)); // jitter
await db.StringSetAsync($"orders:v1:rate:{currency}", payload, ttl);
```

## Do not scan keys

`KEYS` blocks the server for the time of the scan. `server.Keys(...)` uses `SCAN`, but it still reads the whole key space and does not delete atomically. To remove a group:

- HybridCache values: tags (`RemoveByTagAsync`).
- Direct keys: a new version segment in the key (`v2:`), so the old keys expire by TTL.
- A known small set: keep the member keys in a set and delete them.

## Locks

A Redis lock prevents duplicate work. It does not give correctness: a lock that expires during a GC pause, a long query, or a failover has two holders. Protect the data with a database constraint or a concurrency token.

```csharp
var db = mux.GetDatabase();
RedisKey key = "orders:v1:lock:nightly-export";
RedisValue token = Guid.NewGuid().ToString("N");
if (!await db.LockTakeAsync(key, token, TimeSpan.FromMinutes(2)))
{
    return; // another instance runs the export
}

try
{
    await RunExportAsync(ct);
}
finally
{
    await db.LockReleaseAsync(key, token); // releases only when the token is still ours
}
```

- `KeyDeleteAsync(lockKey)` releases the lock of another holder after your lock expired. Use `LockReleaseAsync` with the token.
- `LockExtendAsync(key, token, expiry)` extends a lock that you still hold.
- When the work can outlive the TTL, use DistributedLock.Redis (decision CACHE-08). It extends the lock and signals a lost lock with `HandleLostToken`.
- Do not use RedLock (RedLock.net). Its safety depends on clocks and pauses that a cloud host does not give.

## Pub/sub and streams

Pub/sub has no storage. A subscriber that is offline or slow loses messages. Use it only for signals that can get lost, for example a cache eviction that a TTL also covers.

```csharp
var subscriber = mux.GetSubscriber();
await subscriber.PublishAsync(RedisChannel.Literal("orders:evict"), key);
```

A string channel compiles with warning CS0618 (the implicit conversion is obsolete). Use `RedisChannel.Literal` or `RedisChannel.Pattern`.

For work that must not get lost, use a stream with a consumer group:

```csharp
var db = mux.GetDatabase();
await db.StreamAddAsync("orders:v1:jobs", [new NameValueEntry("orderId", orderId.ToString())]);

// Worker. Create the group one time (it throws when the group exists).
var entries = await db.StreamReadGroupAsync("orders:v1:jobs", "exporters", "worker-1", ">", count: 10);
foreach (var entry in entries)
{
    await HandleAsync(entry, ct);
    await db.StreamAcknowledgeAsync("orders:v1:jobs", "exporters", entry.Id);
}
```

For messages between services, use a broker (`dev-stack:rabbitmq`, `dev-stack:wolverine`).

## Server settings

- Put cache data and durable data (streams, locks that matter, counters) on separate instances or databases with separate settings.
- On the cache instance, set `maxmemory` and `maxmemory-policy allkeys-lru`. Without `maxmemory`, the server uses all memory of the host.
- On a durable instance, use `noeviction` and persistence (AOF).
- Pin the image tag (`valkey/valkey:9.1`, not `latest`). Pin the minor version and take each patch release.
- Use Valkey 9.1.3, 9.0.7, or 8.1.11 or later, or Redis Open Source 8.10.3 or later. These security releases of 2026-10-26 fix a heap buffer overflow when a client publishes an oversized message in cluster mode (Valkey GHSA-f4q9-j22m-xqq9).

## Timeouts

A `RedisTimeoutException` names the reason in its text. Read the fields before you change settings:

- `qs` (queued) high and `in` high: the client cannot read the replies fast enough. Look for sync over async (SER307) and thread pool starvation (`IOCP` and `WORKER` busy counts).
- A large value: `MaximumPayloadBytes` exceeded, or one key holds a big list. Split it.
- A slow command on the server: `SLOWLOG GET`. `KEYS` and big `SMEMBERS` are the usual cause.

Raising `ThreadPool.SetMinThreads` hides starvation and makes the next spike worse. Remove the blocking calls instead.
