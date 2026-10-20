using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

[assembly: DoNotParallelize]

namespace Billing.Worker.Tests;

[TestClass]
public sealed class ConsumerTests
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task OrderPlaced_CreatesInvoice()
    {
        var ct = TestContext.CancellationToken;
        await using var broker = new RabbitMqBuilder().Build();
        await broker.StartAsync(ct);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:RabbitMq"] = broker.GetConnectionString() })
            .Build();
        var invoices = new InvoiceStore();
        using var consumer = new OrderPlacedConsumer(configuration, invoices, NullLogger<OrderPlacedConsumer>.Instance);
        await consumer.StartAsync(ct);
        await Task.Delay(3000, ct);

        var factory = new ConnectionFactory { Uri = new Uri(broker.GetConnectionString()) };
        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var body = JsonSerializer.SerializeToUtf8Bytes(new OrderPlaced(Guid.CreateVersion7(), Guid.CreateVersion7(), 10m, DateTimeOffset.UtcNow), s_json);
        await channel.BasicPublishAsync("orders.events", "order.placed.v1", body, ct);
        await Task.Delay(3000, ct);

        Assert.AreEqual(1, invoices.Count);
        await consumer.StopAsync(ct);
    }
}
