using System.Net.Sockets;
using System.Text;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]

namespace Shop.ContainerSamples;

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

[TestClass]
public sealed class GenericContainerTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Ping_RunningRedis_ReturnsPong()
    {
        // A generic container needs its own readiness check. An open port does not mean "ready".
        await using IContainer redis = new ContainerBuilder(Images.Redis)
            .WithPortBinding(6379, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
            .Build();
        await redis.StartAsync(TestContext.CancellationToken);
        using var client = new TcpClient();
        await client.ConnectAsync(redis.Hostname, redis.GetMappedPublicPort(6379), TestContext.CancellationToken);
        await using NetworkStream stream = client.GetStream();

        await stream.WriteAsync("PING\r\n"u8.ToArray(), TestContext.CancellationToken);
        byte[] buffer = new byte[7];
        int read = await stream.ReadAtLeastAsync(buffer, 7, cancellationToken: TestContext.CancellationToken);

        Encoding.ASCII.GetString(buffer, 0, read).Should().Be("+PONG\r\n");
    }
}
