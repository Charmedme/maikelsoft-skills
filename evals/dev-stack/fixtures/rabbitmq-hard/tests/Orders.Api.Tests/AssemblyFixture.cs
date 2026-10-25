using FluentAssertions.Common;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Orders.Api.Tests;

[TestClass]
public static class AssemblyFixture
{
    private static PostgreSqlContainer? s_postgres;
    private static RabbitMqContainer? s_rabbitMq;

    public static string ConnectionString =>
        s_postgres?.GetConnectionString() ?? throw new InvalidOperationException("The database container did not start.");

    public static string RabbitMqUri =>
        s_rabbitMq?.GetConnectionString() ?? throw new InvalidOperationException("The RabbitMQ container did not start.");

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // FluentAssertions 7 does not detect MSTest 4. Report its failures as MSTest assertion failures.
        Services.ThrowException = message => throw new AssertFailedException(message);

        s_postgres = new PostgreSqlBuilder("postgres:18.6-alpine").Build();
        s_rabbitMq = new RabbitMqBuilder("rabbitmq:4.3-management-alpine").Build();
        await Task.WhenAll(s_postgres.StartAsync(context.CancellationToken), s_rabbitMq.StartAsync(context.CancellationToken));
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (s_postgres is not null)
        {
            await s_postgres.DisposeAsync();
        }

        if (s_rabbitMq is not null)
        {
            await s_rabbitMq.DisposeAsync();
        }
    }
}
