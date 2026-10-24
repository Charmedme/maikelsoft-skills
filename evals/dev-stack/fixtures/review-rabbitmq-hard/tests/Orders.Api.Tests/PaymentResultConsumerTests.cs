using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Orders.Api.Messaging;
using Orders.Api.Messaging.Contracts;
using Orders.Api.Orders;
using Orders.Api.Payments;
using RabbitMQ.Client;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PaymentResultConsumerTests
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
    public async Task PaymentCaptured_StoresPaymentAndMarksOrderPaid()
    {
        var ct = TestContext.CancellationToken;
        var customer = await s_client.CreateCustomerAsync(ct);
        var order = await s_client.CreateOrderAsync(customer.Id, ct);
        var result = new PaymentResultV1($"pay_{Guid.CreateVersion7():N}", order.Id, order.Total, "EUR", DateTimeOffset.UtcNow, null, null);

        await PublishAsync(PaymentResultV1.CapturedWireName, result, ct);

        var payments = await WaitForPaymentsAsync(order.Id, ct);
        payments.Should().ContainSingle().Which.Status.Should().Be(nameof(PaymentStatus.Captured));
        (await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", ct))!.Status.Should().Be(nameof(OrderStatus.Paid));
    }

    [TestMethod]
    public async Task PaymentFailed_StoresFailedPayment()
    {
        var ct = TestContext.CancellationToken;
        var customer = await s_client.CreateCustomerAsync(ct);
        var order = await s_client.CreateOrderAsync(customer.Id, ct);
        var result = new PaymentResultV1($"pay_{Guid.CreateVersion7():N}", order.Id, order.Total, "EUR", null, "card_declined", DateTimeOffset.UtcNow);

        await PublishAsync(PaymentResultV1.FailedWireName, result, ct);

        var payments = await WaitForPaymentsAsync(order.Id, ct);
        payments.Should().ContainSingle().Which.Should().Match<PaymentResponse>(p => p.Status == "Failed" && p.DeclineReason == "card_declined");
    }

    private static async Task PublishAsync(string routingKey, PaymentResultV1 result, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        while (true)
        {
            await using var probe = await s_connection.CreateChannelAsync(cancellationToken: timeout.Token);
            try
            {
                await probe.QueueDeclarePassiveAsync(RabbitMqTopology.PaymentResultsQueue, timeout.Token);
                break;
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
            {
                await Task.Delay(200, timeout.Token);
            }
        }

        await using var channel = await s_connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
        var properties = new BasicProperties { Persistent = true, MessageId = Guid.CreateVersion7().ToString(), Type = routingKey, ContentType = "application/json" };
        await channel.BasicPublishAsync(RabbitMqTopology.PaymentsExchange, routingKey, mandatory: true, properties, JsonSerializer.SerializeToUtf8Bytes(result), ct);
    }

    private static async Task<List<PaymentResponse>> WaitForPaymentsAsync(Guid orderId, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        while (true)
        {
            var payments = await s_client.GetFromJsonAsync<List<PaymentResponse>>($"/orders/{orderId}/payments", timeout.Token);
            if (payments is { Count: > 0 })
            {
                return payments;
            }

            await Task.Delay(200, timeout.Token);
        }
    }
}
