using FluentAssertions.Common;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Orders.Api.Tests;

[TestClass]
public static class AssemblyFixture
{
    private static PostgreSqlContainer? s_postgres;
    private static RedisContainer? s_valkey;

    public static string ConnectionString =>
        s_postgres?.GetConnectionString() ?? throw new InvalidOperationException("The database container did not start.");

    public static string CacheConnectionString =>
        s_valkey?.GetConnectionString() ?? throw new InvalidOperationException("The cache container did not start.");

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // FluentAssertions 7 does not detect MSTest 4. Report its failures as MSTest assertion failures.
        Services.ThrowException = message => throw new AssertFailedException(message);

        s_postgres = new PostgreSqlBuilder("postgres:18.6-alpine").Build();
        s_valkey = new RedisBuilder("valkey/valkey:9.1").Build();
        await s_postgres.StartAsync(context.CancellationToken);
        await s_valkey.StartAsync(context.CancellationToken);
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (s_postgres is not null)
        {
            await s_postgres.DisposeAsync();
        }

        if (s_valkey is not null)
        {
            await s_valkey.DisposeAsync();
        }
    }
}
