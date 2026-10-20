using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for rabbitmq-interop-trap. Each test starts its own RabbitMQ 4.3 broker on fixed host ports,
// so that a stop and a start keep the address that the app uses. The probe reads the queue
// warehouse.order-placed through the management API, so it does not need RabbitMQ.Client.
[TestClass]
[DoNotParallelize]
public sealed class RabbitMqInteropProbe
{
    private const string Image = "rabbitmq:4.3-management-alpine";
    private const string Queue = "warehouse.order-placed";

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task WarehouseQueue_IsQuorum()
    {
        await using var broker = await Broker.StartAsync(TestContext.CancellationToken);
        await using var factory = Factory(broker);
        using var client = factory.CreateClient();
        await MigrateAsync(factory, TestContext.CancellationToken);

        // An app may declare its topology at start or before its first publish. Both are valid, so place one order first.
        await PlaceOrderAsync(client, TimeSpan.FromSeconds(30), TestContext.CancellationToken);
        var queue = await broker.WaitForQueueAsync(Queue, TimeSpan.FromSeconds(60), TestContext.CancellationToken);

        Assert.IsNotNull(queue, $"The app did not declare the queue {Queue} within 60 seconds after the first order.");
        Assert.AreEqual("quorum", queue.Value.GetProperty("type").GetString(), $"The queue {Queue} is not a quorum queue: {queue}");
    }

    [TestMethod]
    public async Task Event_HasTypeMessageIdAndCamelCaseBody()
    {
        await using var broker = await Broker.StartAsync(TestContext.CancellationToken);
        await using var factory = Factory(broker);
        using var client = factory.CreateClient();
        await MigrateAsync(factory, TestContext.CancellationToken);
        var orderId = await PlaceOrderAsync(client, TimeSpan.FromSeconds(30), TestContext.CancellationToken);
        var message = await broker.WaitForMessageAsync(Queue, orderId, TimeSpan.FromSeconds(60), TestContext.CancellationToken);

        Assert.IsNotNull(message, $"No message for order {orderId} reached {Queue} within 60 seconds.");
        var m = message.Value;
        Assert.AreEqual("orders.events", m.GetProperty("exchange").GetString(), "Wrong exchange.");
        Assert.AreEqual("order.placed.v1", m.GetProperty("routing_key").GetString(), "Wrong routing key.");
        var properties = m.GetProperty("properties");
        Assert.AreEqual(JsonValueKind.Object, properties.ValueKind, "The message has no AMQP properties (type, message_id, delivery_mode, content_type).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(Text(properties, "type")), $"The message has no AMQP type property: {properties}");
        Assert.IsFalse(string.IsNullOrWhiteSpace(Text(properties, "message_id")), $"The message has no message_id: {properties}");
        Assert.AreEqual(2, properties.TryGetProperty("delivery_mode", out var mode) ? mode.GetInt32() : 0, $"The message is not persistent: {properties}");
        Assert.Contains("json", Text(properties, "content_type") ?? string.Empty, $"The content type is not JSON: {properties}");

        using var body = JsonDocument.Parse(m.GetProperty("payload").GetString()!);
        foreach (var name in new[] { "orderId", "customerId", "total", "placedAt" })
        {
            Assert.IsTrue(body.RootElement.TryGetProperty(name, out _), $"The body has no camelCase field '{name}': {body.RootElement}");
        }

        Assert.AreEqual(orderId, body.RootElement.GetProperty("orderId").GetGuid());
    }

    [TestMethod]
    public async Task BrokerDown_OrderAccepted_EventArrivesLater()
    {
        await using var broker = await Broker.StartAsync(TestContext.CancellationToken);
        Guid orderId;
        var first = Factory(broker);
        try
        {
            using var client = first.CreateClient();
            await MigrateAsync(first, TestContext.CancellationToken);

            await broker.StopAsync(TestContext.CancellationToken);
            orderId = await PlaceOrderAsync(client, TimeSpan.FromSeconds(30), TestContext.CancellationToken);
        }
        finally
        {
            // The service restarts while RabbitMQ is down. An event that was only in memory is lost now.
            await first.DisposeAsync();
        }

        await broker.RestartAsync(TestContext.CancellationToken);
        await using var second = Factory(broker);
        using var secondClient = second.CreateClient();

        var message = await broker.WaitForMessageAsync(Queue, orderId, TimeSpan.FromSeconds(90), TestContext.CancellationToken);
        Assert.IsNotNull(message, $"The order {orderId} was accepted while RabbitMQ was down and the service restarted, but its event did not reach {Queue} within 90 seconds after the restart.");
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static WebApplicationFactory<Program> Factory(Broker broker) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("ConnectionStrings:RabbitMq", broker.AmqpUri);
        });

    private static async Task MigrateAsync(WebApplicationFactory<Program> factory, CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient client, TimeSpan wait, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(wait);
        var body = new { customerId = Guid.CreateVersion7(), lines = new[] { new { sku = "SKU-1", quantity = 2, unitPrice = 5m } } };
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/orders", body, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Assert.Fail($"POST /orders did not answer within {wait.TotalSeconds} seconds.");
            throw;
        }

        using var _ = response;
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, $"POST /orders returned {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    private sealed class Broker : IAsyncDisposable
    {
        private readonly IContainer _container;
        private readonly HttpClient _http;
        private readonly int _amqpPort;

        private Broker(int amqpPort, int httpPort)
        {
            _amqpPort = amqpPort;
            _container = new ContainerBuilder(Image)
                .WithPortBinding(amqpPort, 5672)
                .WithPortBinding(httpPort, 15672)
                .Build();
            _http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpPort}/") };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("guest:guest")));
        }

        public string AmqpUri => $"amqp://guest:guest@127.0.0.1:{_amqpPort}/";

        public static async Task<Broker> StartAsync(CancellationToken ct)
        {
            var broker = new Broker(FreePort(), FreePort());
            await broker._container.StartAsync(ct);
            await broker.WaitUntilReadyAsync(ct);
            return broker;
        }

        public Task StopAsync(CancellationToken ct) => _container.StopAsync(ct);

        public async Task RestartAsync(CancellationToken ct)
        {
            await _container.StartAsync(ct);
            await WaitUntilReadyAsync(ct);
        }

        public async Task<JsonElement?> WaitForQueueAsync(string queue, TimeSpan wait, CancellationToken ct)
        {
            var end = DateTime.UtcNow + wait;
            while (DateTime.UtcNow < end)
            {
                using var response = await _http.GetAsync(new Uri($"api/queues/%2F/{queue}", UriKind.Relative), ct);
                if (response.IsSuccessStatusCode)
                {
                    return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)).RootElement.Clone();
                }

                await Task.Delay(500, ct);
            }

            return null;
        }

        public async Task<JsonElement?> WaitForMessageAsync(string queue, Guid orderId, TimeSpan wait, CancellationToken ct)
        {
            var end = DateTime.UtcNow + wait;
            var id = orderId.ToString();
            while (DateTime.UtcNow < end)
            {
                try
                {
                    using var response = await _http.PostAsJsonAsync(
                        new Uri($"api/queues/%2F/{queue}/get", UriKind.Relative),
                        new { count = 50, ackmode = "ack_requeue_false", encoding = "auto" },
                        ct);
                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                        foreach (var message in doc.RootElement.EnumerateArray())
                        {
                            if (message.GetProperty("payload").GetString()?.Contains(id, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                return message.Clone();
                            }
                        }
                    }
                }
                catch (HttpRequestException)
                {
                }

                await Task.Delay(500, ct);
            }

            return null;
        }

        private async Task WaitUntilReadyAsync(CancellationToken ct)
        {
            var end = DateTime.UtcNow + TimeSpan.FromSeconds(90);
            while (DateTime.UtcNow < end)
            {
                try
                {
                    using var response = await _http.GetAsync(new Uri("api/health/checks/port-listener/5672", UriKind.Relative), ct);
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                }

                await Task.Delay(500, ct);
            }

            throw new InvalidOperationException("RabbitMQ did not start within 90 seconds.");
        }

        private static int FreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        public async ValueTask DisposeAsync()
        {
            _http.Dispose();
            await _container.DisposeAsync();
        }
    }
}
