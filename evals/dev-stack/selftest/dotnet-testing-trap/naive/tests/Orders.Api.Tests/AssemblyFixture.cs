using Testcontainers.PostgreSql;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Orders.Api.Tests;

[TestClass]
public static class AssemblyFixture
{
    private static PostgreSqlContainer? s_postgres;

    public static string ConnectionString =>
        s_postgres?.GetConnectionString() ?? throw new InvalidOperationException("The database container did not start.");

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        s_postgres = new PostgreSqlBuilder("postgres:17.6-alpine").Build();
        await s_postgres.StartAsync(context.CancellationToken);
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (s_postgres is not null)
        {
            await s_postgres.DisposeAsync();
        }
    }
}
