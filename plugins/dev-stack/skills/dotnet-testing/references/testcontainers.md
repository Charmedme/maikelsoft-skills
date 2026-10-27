# Testcontainers, Respawn, and WebApplicationFactory

Facts for Testcontainers 4.16.0, Respawn 7.0.0, and Microsoft.AspNetCore.Mvc.Testing 10.0.14 with MSTest 4.5.2. Checked on 2026-10-16, versions checked again on 2026-10-27. The code compiled and passed against a real Docker daemon: `examples/tests/Shop.IntegrationTests/` and `examples/tests/Shop.ContainerSamples/`.

## Contents

- Requirements
- One container per test assembly
- WebApplicationFactory
- Respawn reset and parallel runs
- Module builders and pinned images
- Generic containers and wait strategies
- CI notes
- Traps

## Requirements

- Docker on the developer machine and on the CI agent. GitHub and Azure DevOps hosted Linux agents have it. Windows agents run Windows containers only.
- No `Testcontainers.MSTest` package exists (only xUnit packages). Write the lifetime code in an MSTest assembly fixture.
- Testcontainers 4.10 and later need the image in the builder constructor. The constructor without an image is `[Obsolete]` (CS0618, verified). 4.12 moved to Docker.DotNet 4.x. 4.16 removed the MinIO module.

## One container per test assembly

A container start costs seconds (the sample run: 8 s, mostly the start). Start it one time in `[AssemblyInitialize]`, apply the schema, create the Respawn checkpoint, and create the factory. Dispose all in `[AssemblyCleanup]`.

```csharp
[TestClass]
public static class TestHost
{
    // Pin the tag that production uses.
    private const string PostgresImage = "postgres:18.6-alpine";
    private static PostgreSqlContainer? s_database;
    private static Respawner? s_respawner;

    public static ApiFactory Factory { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // Only one [AssemblyInitialize] can exist in an assembly. The FluentAssertions 7 bridge goes first.
        Services.ThrowException = message => throw new AssertFailedException(message);

        s_database = new PostgreSqlBuilder(PostgresImage).Build();
        await s_database.StartAsync(context.CancellationToken);

        await using var connection = new NpgsqlConnection(s_database.GetConnectionString());
        await connection.OpenAsync(context.CancellationToken);
        // Apply the real migrations here.

        s_respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });

        Factory = new ApiFactory(s_database.GetConnectionString());
    }

    public static async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(s_database!.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await s_respawner!.ResetAsync(connection);
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await Factory.DisposeAsync();
        if (s_database is not null)
        {
            await s_database.DisposeAsync();
        }
    }
}
```

With AwesomeAssertions, remove the `Services.ThrowException` line.

Apply the real schema: run the EF Core migrations (`context.Database.MigrateAsync()`) or the SQL scripts of the project. Do not use `EnsureCreated`, because it skips the migrations that production runs.

## WebApplicationFactory

```csharp
public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    // Program.cs reads the connection string before ConfigureAppConfiguration runs, so use UseSetting.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseSetting("ConnectionStrings:Orders", connectionString);
}
```

- `UseSetting` works because the minimal hosting model reads it before `Program.cs` reads configuration. `ConfigureAppConfiguration` runs too late for values that `Program.cs` reads at the top.
- On .NET 10 the test project can see the top-level `Program` class without `public partial class Program;` (verified). Older code still has the line. It does no harm.
- Create one `HttpClient` per test with `Factory.CreateClient()` and dispose it.
- Replace an external service (payment provider, mail) in `ConfigureTestServices`. Do not replace the database.
- .NET 11 adds `ConfigureWebApplicationBuilder`. Do not use it on .NET 10.

A test (compiled: `examples/tests/Shop.IntegrationTests/OrdersEndpointTests.cs`):

```csharp
[TestClass]
[DoNotParallelize] // The tests share one database.
public sealed class OrdersEndpointTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public Task ResetDatabaseAsync() => TestHost.ResetDatabaseAsync(TestContext.CancellationToken);

    [TestMethod]
    public async Task Post_ValidOrder_ReturnsCreated()
    {
        using HttpClient client = TestHost.Factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync("/orders", new { Total = 10.5m }, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

## Respawn reset and parallel runs

- `Respawner.CreateAsync` reads the schema one time. `ResetAsync` deletes all rows in foreign-key order. A reset takes milliseconds.
- Respawn 7 infers `DbAdapter` from the connection (SQL Server, PostgreSQL, MySQL, Oracle, Informix). Set it only for another database. Set `SchemasToInclude`. Add the tables that the reset must keep to `TablesToIgnore`: the EF Core history table (`new Table("__EFMigrationsHistory")`) and tables with reference data that the migrations seed. Otherwise the reset deletes them.
- Default (TEST-08, TEST-17): `[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]` and `[DoNotParallelize]` on each class that uses the database.
- When the suite is slow: create one database per test class in the same container. A transaction per test is faster, but it breaks when the code opens its own transaction or a second connection, and `WebApplicationFactory` tests often do.

## Module builders and pinned images

Pass the image to the constructor. Keep the tags in one place and use the tags of production.

```csharp
public static class Images
{
    // One place for the pinned tags. Use the tags that production uses.
    public const string SqlServer = "mcr.microsoft.com/mssql/server:2025-CU10-ubuntu-24.04";
    public const string Redis = "redis:8.2";
    public const string RabbitMq = "rabbitmq:4.3-management-alpine";
}

public static class ModuleBuilders
{
    public static MsSqlContainer SqlServer() => new MsSqlBuilder(Images.SqlServer).Build();

    public static RedisContainer Redis() => new RedisBuilder(Images.Redis).Build();

    public static RabbitMqContainer RabbitMq() => new RabbitMqBuilder(Images.RabbitMq).Build();
}
```

- Module packages are `Testcontainers.<Name>`, for example `Testcontainers.PostgreSql`, `Testcontainers.MsSql`, `Testcontainers.Redis`, `Testcontainers.RabbitMq`, `Testcontainers.Elasticsearch`, `Testcontainers.Kafka`, `Testcontainers.Azurite`, `Testcontainers.Mailpit`. Check the exact name on nuget.org before you add one (stack rule 5).
- A module includes its wait strategy and its connection string.
- The SQL Server image is large and slow to start. PostgreSQL alpine images start fast.

## Generic containers and wait strategies

A generic container needs its own readiness check. An open port does not mean that the service is ready.

```csharp
await using IContainer redis = new ContainerBuilder(Images.Redis)
    .WithPortBinding(6379, assignRandomHostPort: true)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
    .Build();
await redis.StartAsync(TestContext.CancellationToken);
```

Other wait strategies on `Wait.ForUnixContainer()`: `UntilHttpRequestIsSucceeded(r => r.ForPort(80).ForPath("/health"))`, `UntilContainerIsHealthy()`, `UntilCommandIsCompleted(...)`, `UntilInternalTcpPortIsAvailable(port)`.

Read the address with `Hostname` and `GetMappedPublicPort(port)`. Do not use `localhost` or a fixed host port.

## CI notes

- GitLab needs the `docker:dind` service and `DOCKER_HOST`.
- Keep the Resource Reaper (Ryuk) on. It removes containers when the test process stops. Bitbucket Pipelines is the documented exception (`TESTCONTAINERS_RYUK_DISABLED=true` there only).
- Put integration tests in their own project, so that a unit test job can run without Docker.

## Traps

- `WithReuse(true)` is experimental. It turns off the Resource Reaper, and a stopped reused container gets a new random port.
- `new PostgreSqlBuilder().WithImage("postgres:15")` uses the obsolete constructor. Use `new PostgreSqlBuilder("postgres:18.6-alpine")`.
- `WithPortBinding(5432, 5432)` collides when two runs share a machine.
- `UseInMemoryDatabase` in the factory hides SQL, transaction, and constraint bugs.
- A second `[AssemblyInitialize]` fails discovery (UTA013). Put all assembly setup in one method.
