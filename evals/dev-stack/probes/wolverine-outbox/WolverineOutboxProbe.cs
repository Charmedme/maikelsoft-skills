using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using JasperFx.CommandLine;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Wolverine;
using Wolverine.Tracking;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for wolverine-outbox-trap. Two tests run Wolverine without external transports and use
// tracked sessions. Two tests start a RabbitMQ 4.3 broker on fixed host ports, so that a stop and a
// start keep the address that the app uses. The probe reads the broker through the management API.
[TestClass]
[DoNotParallelize]
public sealed class WolverineOutboxProbe
{
    private const string Image = "rabbitmq:4.3-management-alpine";

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task CancelOrder_IsHandled()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = LocalFactory();
        using var client = factory.CreateClient();
        await MigrateAsync(factory, ct);
        var (orderId, customerId) = await PlaceOrderAsync(client, ct);

        var host = factory.Services.GetRequiredService<IHost>();
        await host.TrackActivity().Timeout(TimeSpan.FromSeconds(30)).InvokeMessageAndWaitAsync(new CancelOrder(orderId, "eval probe"));

        Assert.AreEqual("Cancelled", await StatusAsync(client, customerId, orderId, ct), "The CancelOrder handler did not cancel the order.");
    }

    [TestMethod]
    public async Task CancelShippedOrder_IsNotRetried()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = LocalFactory();
        using var client = factory.CreateClient();
        await MigrateAsync(factory, ct);
        var (orderId, customerId) = await PlaceOrderAsync(client, ct);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = await db.Orders.SingleAsync(o => o.Id == orderId, ct);
            order.MarkShipped();
            await db.SaveChangesAsync(ct);
        }

        var host = factory.Services.GetRequiredService<IHost>();
        var session = await host.TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .Timeout(TimeSpan.FromSeconds(60))
            .SendMessageAndWaitAsync(new CancelOrder(orderId, "eval probe"));

        var starts = session.AllRecordsInOrder(MessageEventType.ExecutionStarted)
            .Count(record => record.Envelope?.Message is CancelOrder);
        Assert.AreEqual(1, starts, $"The CancelOrder for a shipped order ran {starts} times. A business error must not be retried.");
        Assert.AreEqual("Shipped", await StatusAsync(client, customerId, orderId, ct), "A shipped order must stay shipped.");
    }

    [TestMethod]
    public async Task CancelQueue_IsQuorum()
    {
        var ct = TestContext.CancellationToken;
        await using var broker = await Broker.StartAsync(ct);
        await using var factory = BrokerFactory(broker);
        using var client = factory.CreateClient();
        await MigrateAsync(factory, ct);

        var queue = await broker.WaitForAsync("api/queues/%2F/orders-api.cancel-order", TimeSpan.FromSeconds(30), ct);

        Assert.IsNotNull(queue, "The app did not declare the queue orders-api.cancel-order within 30 seconds after the start.");
        Assert.AreEqual("quorum", queue.Value.GetProperty("type").GetString(), $"The queue orders-api.cancel-order is not a quorum queue: {queue}");
    }

    [TestMethod]
    public async Task BrokerDown_OrderSaved_MessageSentLater()
    {
        var ct = TestContext.CancellationToken;
        await using var broker = await Broker.StartAsync(ct);
        Guid orderId;
        var first = BrokerFactory(broker);
        try
        {
            using var client = first.CreateClient();
            await MigrateAsync(first, ct);
            Assert.IsNotNull(
                await broker.WaitForAsync("api/exchanges/%2F/orders.events", TimeSpan.FromSeconds(30), ct),
                "The app did not declare the exchange orders.events within 30 seconds after the start.");
            await broker.BindProbeQueueAsync("orders.events", ct);

            await broker.StopAsync(ct);
            (orderId, _) = await PlaceOrderAsync(client, ct);
        }
        finally
        {
            // The service restarts while RabbitMQ is down. A message that was only in memory is lost now.
            await first.DisposeAsync();
        }

        await broker.RestartAsync(ct);
        await using var second = BrokerFactory(broker);
        using var secondClient = second.CreateClient();

        var message = await broker.WaitForMessageAsync(orderId, TimeSpan.FromSeconds(90), ct);
        Assert.IsTrue(message, $"The order {orderId} was saved while RabbitMQ was down and the service restarted, but no OrderPlaced message reached orders.events within 90 seconds after the restart.");
    }

    private static WebApplicationFactory<Program> LocalFactory()
    {
        // An app that ends with RunJasperFxCommands(args) starts its host under WebApplicationFactory only with this flag.
        JasperFxEnvironment.AutoStartHost = true;
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("ConnectionStrings:RabbitMq", "amqp://guest:guest@127.0.0.1:5672/");
            builder.ConfigureServices(services =>
            {
                services.DisableAllExternalWolverineTransports();
                services.RunWolverineInSoloMode();
            });
        });
    }

    private static WebApplicationFactory<Program> BrokerFactory(Broker broker)
    {
        JasperFxEnvironment.AutoStartHost = true;
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("ConnectionStrings:RabbitMq", broker.AmqpUri);
            builder.ConfigureServices(services => services.RunWolverineInSoloMode());
        });
    }

    private static async Task MigrateAsync(WebApplicationFactory<Program> factory, CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
    }

    private static async Task<(Guid OrderId, Guid CustomerId)> PlaceOrderAsync(HttpClient client, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var customerId = Guid.CreateVersion7();
        var body = new { customerId, lines = new[] { new { sku = "SKU-1", quantity = 2, unitPrice = 5m } } };
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/orders", body, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            Assert.Fail("POST /orders did not answer within 30 seconds.");
            throw;
        }

        using var _ = response;
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, $"POST /orders returned {(int)response.StatusCode}: {text}");
        return (JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid(), customerId);
    }

    private static async Task<string?> StatusAsync(HttpClient client, Guid customerId, Guid orderId, CancellationToken ct)
    {
        var text = await client.GetStringAsync(new Uri($"/orders?customerId={customerId}", UriKind.Relative), ct);
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.EnumerateArray()
            .Where(order => order.GetProperty("id").GetGuid() == orderId)
            .Select(order => order.GetProperty("status").GetString())
            .FirstOrDefault();
    }

    private sealed class Broker : IAsyncDisposable
    {
        private const string ProbeQueue = "eval-probe.order-placed";
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

        public async Task BindProbeQueueAsync(string exchange, CancellationToken ct)
        {
            using var queue = await _http.PutAsJsonAsync(
                new Uri($"api/queues/%2F/{ProbeQueue}", UriKind.Relative),
                new { durable = true, auto_delete = false, arguments = new Dictionary<string, object> { ["x-queue-type"] = "quorum" } },
                ct);
            queue.EnsureSuccessStatusCode();
            using var binding = await _http.PostAsJsonAsync(
                new Uri($"api/bindings/%2F/e/{exchange}/q/{ProbeQueue}", UriKind.Relative),
                new { routing_key = "#" },
                ct);
            binding.EnsureSuccessStatusCode();
        }

        public async Task<JsonElement?> WaitForAsync(string path, TimeSpan wait, CancellationToken ct)
        {
            var end = DateTime.UtcNow + wait;
            while (DateTime.UtcNow < end)
            {
                using var response = await _http.GetAsync(new Uri(path, UriKind.Relative), ct);
                if (response.IsSuccessStatusCode)
                {
                    return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)).RootElement.Clone();
                }

                await Task.Delay(500, ct);
            }

            return null;
        }

        public async Task<bool> WaitForMessageAsync(Guid orderId, TimeSpan wait, CancellationToken ct)
        {
            var end = DateTime.UtcNow + wait;
            var id = orderId.ToString();
            while (DateTime.UtcNow < end)
            {
                try
                {
                    using var response = await _http.PostAsJsonAsync(
                        new Uri($"api/queues/%2F/{ProbeQueue}/get", UriKind.Relative),
                        new { count = 50, ackmode = "ack_requeue_false", encoding = "auto" },
                        ct);
                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                        if (doc.RootElement.EnumerateArray().Any(m => m.GetProperty("payload").GetString()?.Contains(id, StringComparison.OrdinalIgnoreCase) == true))
                        {
                            return true;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                }

                await Task.Delay(500, ct);
            }

            return false;
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
