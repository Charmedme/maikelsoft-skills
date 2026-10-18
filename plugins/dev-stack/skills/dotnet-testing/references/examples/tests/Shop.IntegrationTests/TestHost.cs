using FluentAssertions.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]

namespace Shop.IntegrationTests;

public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    // Program.cs reads the connection string before ConfigureAppConfiguration runs, so use UseSetting.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseSetting("ConnectionStrings:Orders", connectionString);
}

[TestClass]
public static class TestHost
{
    // Pin the tag that production uses.
    private const string PostgresImage = "postgres:17.6-alpine";
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
        await using (var schema = new NpgsqlCommand("CREATE TABLE orders (id serial PRIMARY KEY, total numeric NOT NULL);", connection))
        {
            // Apply the real migrations here.
            await schema.ExecuteNonQueryAsync(context.CancellationToken);
        }

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
