# Resources, integrations, and tests

Detail for rules 7, 10, and 13 of `SKILL.md`. Each code block names its evidence: a build on SDK 11.0.100, the 17.0.0 source or template, or the docs only. Before you use a form from the docs only, run `aspire docs api search <name> --language csharp`.

## Contents

- Hosting packages
- PostgreSQL and EF Core migrations
- Redis
- RabbitMQ
- Orleans
- Local data and container images
- Tests

## Hosting packages

The AppHost references one hosting package for each resource type. The service references the client package (`SKILL.md`, rule 10).

| Resource | Hosting package | AppHost call | Evidence |
|----------|-----------------|--------------|----------|
| PostgreSQL | `Aspire.Hosting.PostgreSQL` | `AddPostgres("postgres").AddDatabase("orders")` | build |
| Redis | `Aspire.Hosting.Redis` | `AddRedis("cache")` | build |
| Valkey | `Aspire.Hosting.Valkey` | `AddValkey("cache")` | source |
| RabbitMQ | `Aspire.Hosting.RabbitMQ` | `AddRabbitMQ("messaging")` | source |
| Orleans | `Aspire.Hosting.Orleans` | `AddOrleans("cluster")` | source |
| .NET project | `Aspire.Hosting.Dotnet` | `AddDotnetProject("api", "../Api/Api.csproj")` | build |

`AddDotnetProject` also takes a `.cs` file or a folder.

## PostgreSQL and EF Core migrations

- `AddDatabase("orders")` gives the connection name `orders`. The service calls `builder.AddNpgsqlDataSource("orders")` (build) or `builder.AddNpgsqlDbContext<OrdersDb>("orders")` (source tree).
- `WithPgAdmin()` and `WithPgWeb()` add a web tool for the developer. Add one only when the team asks for it.
- The migration deployment is decision DATA-18 of `dev-stack:efcore`. The AppHost does not run migrations at service start.

## Redis

- `AddRedis("cache")` and, in the service, `builder.AddRedisClient("cache")` give one `IConnectionMultiplexer`.
- For HybridCache with Redis as L2, call `builder.AddRedisDistributedCache("cache")`, then `AddHybridCache`. The cache rules (keys, TTLs, `LocalCacheExpiration`) are in `dev-stack:redis`.
- `dev-stack:redis` recommends Valkey 9 (CACHE-02). With Aspire, use `AddValkey` when the team accepted Valkey. The client calls do not change.

## RabbitMQ

```csharp
// AppHost (source: Aspire.Hosting.RabbitMQ 17.0.0, not built)
var messaging = builder.AddRabbitMQ("messaging").WithDataVolume().WithManagementPlugin();

builder.AddDotnetProject("worker", "../Orders.Worker/Orders.Worker.csproj")
    .WithReference(messaging).WaitFor(messaging);

// Service (source: Aspire.RabbitMQ.Client 17.0.0, not built)
builder.AddRabbitMQClient("messaging");
```

- Aspire 17 removed `Aspire.RabbitMQ.Client.v6`. `Aspire.RabbitMQ.Client` uses RabbitMQ.Client 7, with `IChannel` and async calls. For code on version 6, read `dev-stack:rabbitmq`, section "Version 6 to 7".
- With Wolverine, the transport takes the connection string from the configuration (`ConnectionStrings:messaging`). See `dev-stack:wolverine`.
- Exchanges, queues, and dead letters are in `dev-stack:rabbitmq`. The AppHost does not declare them.

## Orleans

The AppHost side is in this skill. The silo code (grains, `UseOrleans`, providers) is in `dev-stack:orleans`.

```csharp
// AppHost (methods checked in the 17.0.0 source, not built)
var orleansStore = builder.AddRedis("orleans-redis");

var orleans = builder.AddOrleans("cluster")
    .WithClustering(orleansStore)
    .WithGrainStorage("Default", orleansStore);

var silo = builder.AddDotnetProject("silo", "../Orders.Silo/Orders.Silo.csproj")
    .WithReference(orleans)
    .WaitFor(orleansStore);

builder.AddDotnetProject("api", "../Orders.Api/Orders.Api.csproj")
    .WithReference(orleans.AsClient())
    .WaitFor(silo);
```

- The silo gets `WithReference(orleans)`. A client gets `WithReference(orleans.AsClient())`.
- The silo still references the Orleans provider packages (for example `Microsoft.Orleans.Clustering.Redis` and `Microsoft.Orleans.Persistence.Redis`), and it registers the Redis client with the resource name (`builder.AddKeyedRedisClient("orleans-redis")`). Then it calls `builder.UseOrleans()`. This silo form is from the Aspire Orleans integration page and was not built. `dev-stack:orleans` owns it.
- `WithMemoryGrainStorage`, `WithMemoryReminders`, and `WithDevelopmentClustering` exist. Use them only for local runs. Memory storage loses the state at each stop.

## Local data and container images

- `WithDataVolume()` keeps the data of a database or a broker between two runs (AHS-03). Without it, each `aspire run` starts with an empty database.
- The default images in 17.0 are PostgreSQL 18.6 and Redis 8.10. When production runs another major version, set the same tag with `WithImageTag("<tag>")`, so that local runs and tests use the production engine.
- Do not use `WithHostPort` for a fixed port, unless a tool outside Aspire needs it. A fixed port fails when two AppHosts run on one machine.

## Tests

The full form of an Aspire smoke test with MSTest 4. The 17.0.0 `aspire-mstest` template uses this form. It was not built.

```csharp
[TestClass]
public sealed class OrdersSmokeTests
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(120);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Health_returns_200_when_the_api_is_healthy()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Orders_AppHost>();

        await using var app = await appHost.BuildAsync(ct).WaitAsync(StartTimeout, ct);
        await app.StartAsync(ct).WaitAsync(StartTimeout, ct);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", ct).WaitAsync(StartTimeout, ct);

        using var http = app.CreateHttpClient("api");
        using var response = await http.GetAsync("/health", ct);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- The test project references the AppHost project with a `ProjectReference`. The type `Projects.Orders_AppHost` comes from that reference: the AppHost project `Orders.AppHost` gives `Orders_AppHost`.
- `TestContext` must be a property of the class. A probe answer used `TestContext` without the property, and it did not compile.
- Use a timeout of 120 seconds or more for the start. The first run pulls the container images.
- Start the AppHost one time for each test class, not for each test, when the tests do not change the data. A start takes seconds to minutes.
- For the tests of one API, prefer `WebApplicationFactory` with Testcontainers (AHS-07, and TEST-07 of `test-stack:dotnet-testing`). They start faster and give each test its own data.
