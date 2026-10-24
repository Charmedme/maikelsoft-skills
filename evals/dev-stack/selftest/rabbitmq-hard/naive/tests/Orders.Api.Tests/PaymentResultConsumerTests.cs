using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Orders.Api.Messaging;
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
    public async Task PaymentCaptured_Twice_StoresOneCapturedPaymentAndMarksOrderPaid()
    {
        var ct = TestContext.CancellationToken;
        var customer = await s_client.CreateCustomerAsync(ct);
        var order = await s_client.CreateOrderAsync(customer.Id, ct);
        var body = $"{{\"paymentId\":\"pay_{Guid.CreateVersion7():N}\",\"orderId\":\"{order.Id}\",\"amount\":{order.Total},\"currency\":\"EUR\",\"capturedAt\":\"2026-10-20T09:15:02Z\"}}";

        await PublishAsync("payment.captured.v1", body, ct);
        await PublishAsync("payment.captured.v1", body, ct);

        var payments = await WaitForPaymentsAsync(order.Id, ct);
        payments.Should().ContainSingle().Which.Status.Should().Be(nameof(PaymentStatus.Captured));
        (await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", ct))!.Status.Should().Be(nameof(OrderStatus.Paid));
    }

    [TestMethod]
    public async Task PaymentFailed_StoresFailedPaymentAndKeepsOrderPlaced()
    {
        var ct = TestContext.CancellationToken;
        var customer = await s_client.CreateCustomerAsync(ct);
        var order = await s_client.CreateOrderAsync(customer.Id, ct);
        var body = $"{{\"paymentId\":\"pay_{Guid.CreateVersion7():N}\",\"orderId\":\"{order.Id}\",\"amount\":{order.Total},\"currency\":\"EUR\",\"reason\":\"card_declined\",\"failedAt\":\"2026-10-20T09:16:40Z\"}}";

        await PublishAsync("payment.failed.v1", body, ct);

        var payments = await WaitForPaymentsAsync(order.Id, ct);
        payments.Should().ContainSingle().Which.Should().Match<PaymentResponse>(p => p.Status == "Failed" && p.DeclineReason == "card_declined");
        (await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", ct))!.Status.Should().Be(nameof(OrderStatus.Placed));
    }

    private static async Task PublishAsync(string routingKey, string body, CancellationToken ct)
    {
        await using var channel = await s_connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        while (true)
        {
            try
            {
                await channel.QueueDeclarePassiveAsync(RabbitMqTopology.PaymentResultsQueue, timeout.Token);
                break;
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
            {
                await Task.Delay(200, timeout.Token);
            }
        }

        await using var publish = await s_connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
        var properties = new BasicProperties { Persistent = true, MessageId = Guid.CreateVersion7().ToString(), Type = routingKey, ContentType = "application/json" };
        await publish.BasicPublishAsync(RabbitMqTopology.PaymentsExchange, routingKey, mandatory: true, properties, Encoding.UTF8.GetBytes(body), ct);
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
                await Task.Delay(1000, timeout.Token); // give a duplicate the time to arrive
                return (await s_client.GetFromJsonAsync<List<PaymentResponse>>($"/orders/{orderId}/payments", timeout.Token))!;
            }

            await Task.Delay(200, timeout.Token);
        }
    }
}
