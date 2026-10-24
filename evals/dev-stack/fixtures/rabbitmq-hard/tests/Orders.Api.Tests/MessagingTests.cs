using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;
using Orders.Api.Inventory;
using Orders.Api.Messaging;
using Orders.Api.Messaging.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class MessagingTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;
    private static IConnection s_connection = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
        s_connection = await new ConnectionFactory { Uri = new Uri(AssemblyFixture.RabbitMqUri) }.CreateConnectionAsync(context.CancellationToken);
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        await s_connection.DisposeAsync();
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task PlaceOrder_PublishesOrderPlacedThroughTheOutbox()
    {
        var ct = TestContext.CancellationToken;
        await using var channel = await s_connection.CreateChannelAsync(cancellationToken: ct);
        await RabbitMqTopology.DeclareOrdersExchangeAsync(channel, ct);
        var queue = (await channel.QueueDeclareAsync(cancellationToken: ct)).QueueName;
        await channel.QueueBindAsync(queue, RabbitMqTopology.OrdersExchange, OrderPlacedV1.WireName, cancellationToken: ct);

        var received = new TaskCompletionSource<(IReadOnlyBasicProperties Properties, string Body)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new AsyncEventingBasicConsumer(channel);
        var customer = await s_client.CreateCustomerAsync(ct);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.Span);
            if (body.Contains(customer.Id.ToString(), StringComparison.Ordinal))
            {
                received.TrySetResult((ea.BasicProperties, body));
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
        };
        await channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);

        var order = await s_client.CreateOrderAsync(customer.Id, ct);

        var (properties, json) = await received.Task.WaitAsync(TimeSpan.FromSeconds(20), ct);
        properties.Type.Should().Be(OrderPlacedV1.WireName);
        properties.Persistent.Should().BeTrue();
        properties.CorrelationId.Should().Be(order.Id.ToString());
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("orderId").GetGuid().Should().Be(order.Id);
        doc.RootElement.GetProperty("total").GetDecimal().Should().Be(order.Total);
    }

    [TestMethod]
    public async Task StockReceived_AddsStockOnce()
    {
        var ct = TestContext.CancellationToken;
        var sku = "SKU-RCV-" + Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant();
        var receipt = new StockReceivedV1(Guid.CreateVersion7(), sku, 12, DateTimeOffset.UtcNow);
        await WaitForQueueAsync(RabbitMqTopology.StockReceivedQueue, ct);

        await using var channel = await s_connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
        var body = JsonSerializer.SerializeToUtf8Bytes(receipt); // The warehouse service sends PascalCase.
        var properties = new BasicProperties { Persistent = true, MessageId = Guid.CreateVersion7().ToString(), Type = StockReceivedV1.WireName, ContentType = "application/json" };
        await channel.BasicPublishAsync(RabbitMqTopology.WarehouseExchange, StockReceivedV1.WireName, mandatory: true, properties, body, ct);
        await channel.BasicPublishAsync(RabbitMqTopology.WarehouseExchange, StockReceivedV1.WireName, mandatory: true, properties, body, ct);

        var stock = await WaitForStockAsync(sku, ct);
        stock.Should().Be(12);
        var receipts = await CountReceiptsAsync(receipt.ReceiptId, ct);
        receipts.Should().Be(1);
        var response = await s_client.GetFromJsonAsync<StockResponse>($"/inventory/{sku}", ct);
        response!.OnHand.Should().Be(12);
    }

    private static async Task WaitForQueueAsync(string queue, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        while (true)
        {
            await using var channel = await s_connection.CreateChannelAsync(cancellationToken: timeout.Token);
            try
            {
                var ok = await channel.QueueDeclarePassiveAsync(queue, timeout.Token);
                if (ok.ConsumerCount > 0)
                {
                    return;
                }
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
            {
                // The consumer has not declared the queue yet.
            }

            await Task.Delay(200, timeout.Token);
        }
    }

    private static async Task<int> WaitForStockAsync(string sku, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        while (true)
        {
            await using var scope = s_factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Sku == sku, timeout.Token);
            if (item is not null && await db.StockReceipts.CountAsync(r => r.Sku == sku, timeout.Token) > 0)
            {
                await Task.Delay(500, timeout.Token); // give a duplicate the time to arrive
                return (await db.StockItems.AsNoTracking().FirstAsync(s => s.Sku == sku, timeout.Token)).OnHand;
            }

            await Task.Delay(200, timeout.Token);
        }
    }

    private static async Task<int> CountReceiptsAsync(Guid receiptId, CancellationToken ct)
    {
        await using var scope = s_factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        return await db.StockReceipts.CountAsync(r => r.ReceiptId == receiptId, ct);
    }
}
