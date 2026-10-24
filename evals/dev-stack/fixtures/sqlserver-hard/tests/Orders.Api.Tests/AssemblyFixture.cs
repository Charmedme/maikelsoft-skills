using FluentAssertions.Common;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Orders.Api.Tests;

[TestClass]
public static class AssemblyFixture
{
    private static MsSqlContainer? s_sqlServer;

    // The container connection string names the master database. The tests use their own database.
    public static string ConnectionString =>
        s_sqlServer is null
            ? throw new InvalidOperationException("The database container did not start.")
            : new SqlConnectionStringBuilder(s_sqlServer.GetConnectionString()) { InitialCatalog = "Orders" }.ConnectionString;

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // FluentAssertions 7 does not detect MSTest 4. Report its failures as MSTest assertion failures.
        Services.ThrowException = message => throw new AssertFailedException(message);

        s_sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04").Build();
        await s_sqlServer.StartAsync(context.CancellationToken);
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (s_sqlServer is not null)
        {
            await s_sqlServer.DisposeAsync();
        }
    }
}
