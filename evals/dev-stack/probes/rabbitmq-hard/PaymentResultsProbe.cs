using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for rabbitmq-hard. Each test starts its own RabbitMQ 4.3 broker with the topology that the
// production broker already has (docs/messaging.md), starts the app against it, and acts like the payment
// gateway and the warehouse: it publishes through the management API and reads queues through it.
// So the probe does not depend on the RabbitMQ.Client code of the run.
[TestClass]
[DoNotParallelize]
public sealed class PaymentResultsProbe
{
    private const string Image = "rabbitmq:4.3-management-alpine";
    private const string Captured = "payment.captured.v1";

    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [ClassInitialize]
    public static async Task MigrateAsync(TestContext context)
    {
        // Migrate before an app host starts, so that a background service never sees a missing table.
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(AssemblyFixture.ConnectionString).Options;
        await using var db = new OrdersDbContext(options);
        await db.Database.MigrateAsync(context.CancellationToken);
    }

    [TestMethod]
    public async Task PaymentCaptured_MarksOrderPaidAndStoresPayment()
    {
        await using var broker = await Broker.StartAsync(Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();
        var order = await CreateOrderAsync(client);

        await broker.PublishPaymentAsync(Captured, CapturedBody(NewPaymentId(), order.Id, order.Total), order.Id, Ct);

        var paid = await WaitAsync(async () => await OrderStatusAsync(client, order.Id) == "Paid" && (await PaymentStatusesAsync(client, order.Id)).Contains("Captured"), TimeSpan.FromSeconds(30));
        Assert.IsTrue(paid, $"Within 30 s after payment.captured.v1, the order status is '{await OrderStatusAsync(client, order.Id)}' (expected Paid) and the payments are [{string.Join(", ", await PaymentStatusesAsync(client, order.Id))}] (expected one Captured). {await broker.DescribePaymentQueueAsync(Ct)}");
    }

    [TestMethod]
    public async Task PaymentCaptured_PublishesOrderPaid()
    {
        await using var broker = await Broker.StartAsync(Ct);
        var probeQueue = await broker.BindProbeQueueAsync("orders.events", "order.paid.v1", Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();
        var order = await CreateOrderAsync(client);

        await broker.PublishPaymentAsync(Captured, CapturedBody(NewPaymentId(), order.Id, order.Total), order.Id, Ct);

        var message = await broker.WaitForMessageAsync([probeQueue], order.Id.ToString(), TimeSpan.FromSeconds(40), Ct);
        Assert.IsNotNull(message, $"No order.paid.v1 message for order {order.Id} reached orders.events within 40 s. {await broker.DescribePaymentQueueAsync(Ct)}");
        var properties = message["properties"];
        Assert.AreEqual("order.paid.v1", properties?["type"]?.GetValue<string>(), $"The AMQP type of the event is not order.paid.v1: {properties?.ToJsonString()}");
        Assert.IsFalse(string.IsNullOrWhiteSpace(properties?["message_id"]?.GetValue<string>()), "The event has no message_id.");
        using var body = JsonDocument.Parse(message["payload"]!.GetValue<string>());
        Assert.IsTrue(body.RootElement.TryGetProperty("orderId", out var orderId) && orderId.GetGuid() == order.Id, $"The body has no camelCase orderId of the order: {body.RootElement}");
    }

    [TestMethod]
    public async Task SamePaymentTwice_StoresOnePayment()
    {
        await using var broker = await Broker.StartAsync(Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();
        var order = await CreateOrderAsync(client);
        var paymentId = NewPaymentId();

        // The gateway publishes the same result again with a new message_id when it misses a confirm.
        await broker.PublishPaymentAsync(Captured, CapturedBody(paymentId, order.Id, order.Total), order.Id, Ct);
        await broker.PublishPaymentAsync(Captured, CapturedBody(paymentId, order.Id, order.Total), order.Id, Ct);

        var paid = await WaitAsync(async () => (await PaymentStatusesAsync(client, order.Id)).Contains("Captured"), TimeSpan.FromSeconds(30));
        Assert.IsTrue(paid, $"The captured payment was not stored within 30 s. {await broker.DescribePaymentQueueAsync(Ct)}");
        var drained = await WaitAsync(async () => await broker.PaymentQueueDepthAsync(Ct) == 0, TimeSpan.FromSeconds(20));
        var statuses = await PaymentStatusesAsync(client, order.Id);
        Assert.AreEqual(1, statuses.Count(s => s == "Captured"), $"The same payment result (same paymentId, new message_id) was stored {statuses.Count(s => s == "Captured")} times.");
        Assert.IsTrue(drained, $"The second copy of the payment result stays in the queue. {await broker.DescribePaymentQueueAsync(Ct)}");
    }

    [TestMethod]
    public async Task PaymentQueue_KeepsDeadLettersAtLeastOnce()
    {
        await using var broker = await Broker.StartAsync(Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();

        var queue = await broker.WaitForPaymentQueueAsync(Ct);
        Assert.IsNotNull(queue, "No queue is bound to payments.events for payment.captured.v1 within 40 s after the start.");
        var settings = Broker.Settings(queue);
        Assert.AreEqual("quorum", queue["type"]?.GetValue<string>(), $"The payment queue is not a quorum queue: {queue["type"]}");
        Assert.IsFalse(string.IsNullOrEmpty(Text(settings, "dead-letter-exchange")), $"The payment queue has no dead-letter exchange: {settings.ToJsonString()}");
        Assert.AreEqual("at-least-once", Text(settings, "dead-letter-strategy"), $"The payment queue dead-letters at-most-once (the default): a dead letter can get lost. Settings: {settings.ToJsonString()}");
        Assert.AreEqual("reject-publish", Text(settings, "overflow"), $"At-least-once dead-lettering needs overflow reject-publish, else it falls back to at-most-once. Settings: {settings.ToJsonString()}");
    }

    [TestMethod]
    public async Task PaymentForUnknownOrder_EndsInDeadLetterQueue()
    {
        await using var broker = await Broker.StartAsync(Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();
        await CreateOrderAsync(client); // the app is up
        var paymentId = NewPaymentId();

        await broker.PublishPaymentAsync(Captured, CapturedBody(paymentId, Guid.CreateVersion7(), 19.99m), Guid.CreateVersion7(), Ct);

        var queue = await broker.WaitForPaymentQueueAsync(Ct);
        Assert.IsNotNull(queue, "No queue is bound to payments.events for payment.captured.v1.");
        var deadLetters = await broker.DeadLetterQueuesAsync(queue, Ct);
        Assert.AreNotEqual(0, deadLetters.Count, $"The payment queue has no dead-letter exchange with a bound queue. {await broker.DescribePaymentQueueAsync(Ct)}");
        var message = await broker.WaitForMessageAsync(deadLetters, paymentId, TimeSpan.FromSeconds(90), Ct, peek: true);
        Assert.IsNotNull(message, $"A captured payment for an unknown order did not reach a dead-letter queue ({string.Join(", ", deadLetters)}) within 90 s. Money was taken, so the result must stay for finance. {await broker.DescribePaymentQueueAsync(Ct)}");
        var drained = await WaitAsync(async () => await broker.PaymentQueueDepthAsync(Ct) == 0, TimeSpan.FromSeconds(10));
        Assert.IsTrue(drained, $"The payment queue still holds the message. {await broker.DescribePaymentQueueAsync(Ct)}");
    }

    [TestMethod]
    public async Task StockReceiptWithZeroQuantity_EndsInOrdersDlq()
    {
        await using var broker = await Broker.StartAsync(Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();
        await broker.WaitForConsumerAsync("orders.stock-received", Ct);
        var sku = NewSku();

        // StockItem.Receive rejects a quantity of 0: this receipt fails each time.
        await broker.PublishAsync("warehouse.events", "stock.received.v1", StockBody(Guid.CreateVersion7().ToString(), sku, 0), Ct);

        var message = await broker.WaitForMessageAsync(["orders.dlq"], sku, TimeSpan.FromSeconds(60), Ct, peek: true);
        Assert.IsNotNull(message, $"A stock receipt that fails each time did not reach orders.dlq within 60 s. {await broker.DescribeQueueAsync("orders.stock-received", Ct)}");
        var drained = await WaitAsync(async () => await broker.QueueDepthAsync("orders.stock-received", Ct) == 0, TimeSpan.FromSeconds(10));
        Assert.IsTrue(drained, $"orders.stock-received still holds the receipt. {await broker.DescribeQueueAsync("orders.stock-received", Ct)}");
    }

    [TestMethod]
    public async Task MalformedStockReceipt_EndsInOrdersDlq()
    {
        await using var broker = await Broker.StartAsync(Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();
        await broker.WaitForConsumerAsync("orders.stock-received", Ct);
        var marker = NewSku();

        // ReceiptId is not a GUID: the body cannot be read.
        await broker.PublishAsync("warehouse.events", "stock.received.v1", StockBody("not-a-guid-" + marker, marker, 3), Ct);

        var message = await broker.WaitForMessageAsync(["orders.dlq"], marker, TimeSpan.FromSeconds(60), Ct, peek: true);
        Assert.IsNotNull(message, $"A stock receipt that cannot be read did not reach orders.dlq within 60 s. {await broker.DescribeQueueAsync("orders.stock-received", Ct)}");
    }

    [TestMethod]
    public async Task StockReceipt_OnProductionTopology_IsBooked()
    {
        await using var broker = await Broker.StartAsync(Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();
        var sku = NewSku();

        await broker.WaitForConsumerAsync("orders.stock-received", Ct);
        await broker.PublishAsync("warehouse.events", "stock.received.v1", StockBody(Guid.CreateVersion7().ToString(), sku, 5), Ct);

        var booked = await WaitAsync(async () => await OnHandAsync(client, sku) == 5, TimeSpan.FromSeconds(30));
        Assert.IsTrue(booked, $"A valid stock receipt was not booked within 30 s on a broker that already has the production queues. {await broker.DescribeQueueAsync("orders.stock-received", Ct)}");
    }

    [TestMethod]
    public async Task PaymentConsumer_HasPrefetchLimit()
    {
        await using var broker = await Broker.StartAsync(Ct);
        await using var app = App(broker);
        using var client = app.CreateClient();

        var queue = await broker.WaitForPaymentQueueAsync(Ct);
        Assert.IsNotNull(queue, "No queue is bound to payments.events for payment.captured.v1 within 40 s after the start.");
        var name = queue["name"]!.GetValue<string>();
        await broker.WaitForConsumerAsync(name, Ct);
        var details = (await broker.QueueAsync(name, Ct))?["consumer_details"]?.AsArray() ?? [];
        Assert.AreNotEqual(0, details.Count, $"No consumer on {name}.");
        foreach (var consumer in details)
        {
            var prefetch = consumer?["prefetch_count"]?.GetValue<int>() ?? 0;
            Assert.IsTrue(prefetch is > 0 and <= 1000, $"A consumer of {name} has prefetch_count {prefetch}. 0 means no limit: the broker pushes every message to one consumer.");
        }
    }

    private static string NewPaymentId() => "pay_" + Guid.CreateVersion7().ToString("N")[^20..];

    private static string NewSku() => "SKU-P" + Guid.CreateVersion7().ToString("N")[^10..].ToUpperInvariant();

    private static string CapturedBody(string paymentId, Guid orderId, decimal amount) =>
        new JsonObject
        {
            ["paymentId"] = paymentId,
            ["orderId"] = orderId.ToString(),
            ["amount"] = amount,
            ["currency"] = "EUR",
            ["capturedAt"] = DateTimeOffset.UtcNow.ToString("O"),
        }.ToJsonString();

    private static string StockBody(string receiptId, string sku, int quantity) =>
        $"{{\"ReceiptId\":\"{receiptId}\",\"Sku\":\"{sku}\",\"Quantity\":{quantity},\"ReceivedAt\":\"{DateTimeOffset.UtcNow:O}\"}}";

    private static string? Text(JsonObject settings, string key) =>
        settings[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static WebApplicationFactory<Program> App(Broker broker) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("ConnectionStrings:RabbitMq", broker.AmqpUri);
        });

    private async Task<(Guid Id, decimal Total)> CreateOrderAsync(HttpClient client)
    {
        var customer = await client.PostAsJsonAsync("/customers", new { name = "Probe Buyer", email = $"probe-{Guid.CreateVersion7():N}@example.com", kind = 0 }, Ct);
        var customerText = await customer.Content.ReadAsStringAsync(Ct);
        Assert.AreEqual(HttpStatusCode.Created, customer.StatusCode, $"POST /customers returned {(int)customer.StatusCode}: {customerText}");
        var customerId = JsonDocument.Parse(customerText).RootElement.GetProperty("id").GetGuid();
        var order = await client.PostAsJsonAsync("/orders", new { customerId, lines = new[] { new { sku = "SKU-PROBE-1", quantity = 2, unitPrice = 21.25m } } }, Ct);
        var orderText = await order.Content.ReadAsStringAsync(Ct);
        Assert.AreEqual(HttpStatusCode.Created, order.StatusCode, $"POST /orders returned {(int)order.StatusCode}: {orderText}");
        var root = JsonDocument.Parse(orderText).RootElement;
        return (root.GetProperty("id").GetGuid(), root.GetProperty("total").GetDecimal());
    }

    private async Task<string?> OrderStatusAsync(HttpClient client, Guid orderId)
    {
        using var response = await client.GetAsync(new Uri($"/orders/{orderId}", UriKind.Relative), Ct);
        return response.IsSuccessStatusCode
            ? JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement.GetProperty("status").GetString()
            : $"HTTP {(int)response.StatusCode}";
    }

    private async Task<List<string>> PaymentStatusesAsync(HttpClient client, Guid orderId)
    {
        using var response = await client.GetAsync(new Uri($"/orders/{orderId}/payments", UriKind.Relative), Ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return [.. doc.RootElement.EnumerateArray().Select(p => p.GetProperty("status").GetString() ?? string.Empty)];
    }

    private async Task<int?> OnHandAsync(HttpClient client, string sku)
    {
        using var response = await client.GetAsync(new Uri($"/inventory/{sku}", UriKind.Relative), Ct);
        return response.IsSuccessStatusCode
            ? JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement.GetProperty("onHand").GetInt32()
            : null;
    }

    private async Task<bool> WaitAsync(Func<Task<bool>> condition, TimeSpan wait)
    {
        var end = DateTime.UtcNow + wait;
        while (DateTime.UtcNow < end)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(500, Ct);
        }

        return await condition();
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
            await broker.DeclareProductionTopologyAsync(ct);
            return broker;
        }

        // The exchanges and queues that the production broker already has (docs/messaging.md and the payments team).
        private async Task DeclareProductionTopologyAsync(CancellationToken ct)
        {
            foreach (var (name, type) in new[] { ("orders.events", "topic"), ("warehouse.events", "topic"), ("payments.events", "topic"), ("orders.dlx", "fanout") })
            {
                await PutAsync($"api/exchanges/%2F/{name}", new JsonObject { ["type"] = type, ["durable"] = true }, ct);
            }

            await PutAsync("api/queues/%2F/orders.dlq", new JsonObject { ["durable"] = true, ["arguments"] = new JsonObject { ["x-queue-type"] = "quorum" } }, ct);
            await PutAsync("api/queues/%2F/orders.stock-received", new JsonObject
            {
                ["durable"] = true,
                ["arguments"] = new JsonObject { ["x-queue-type"] = "quorum", ["x-dead-letter-exchange"] = "orders.dlx", ["x-delivery-limit"] = 20 },
            }, ct);
            await PostAsync("api/bindings/%2F/e/orders.dlx/q/orders.dlq", new JsonObject { ["routing_key"] = string.Empty }, ct);
            await PostAsync("api/bindings/%2F/e/warehouse.events/q/orders.stock-received", new JsonObject { ["routing_key"] = "stock.received.v1" }, ct);
        }

        public async Task<string> BindProbeQueueAsync(string exchange, string routingKey, CancellationToken ct)
        {
            var name = "probe." + Guid.CreateVersion7().ToString("N")[^12..];
            await PutAsync($"api/queues/%2F/{name}", new JsonObject { ["durable"] = true, ["arguments"] = new JsonObject { ["x-queue-type"] = "quorum" } }, ct);
            await PostAsync($"api/bindings/%2F/e/{exchange}/q/{name}", new JsonObject { ["routing_key"] = routingKey }, ct);
            return name;
        }

        public async Task PublishPaymentAsync(string routingKey, string body, Guid orderId, CancellationToken ct)
        {
            var properties = new JsonObject
            {
                ["message_id"] = Guid.CreateVersion7().ToString(),
                ["correlation_id"] = orderId.ToString(),
                ["type"] = routingKey,
                ["content_type"] = "application/json",
                ["delivery_mode"] = 2,
            };

            // Wait until the app has bound its queue, as the gateway publishes only into bound queues.
            var end = DateTime.UtcNow + TimeSpan.FromSeconds(40);
            while (true)
            {
                if (await TryPublishAsync("payments.events", routingKey, body, properties, ct))
                {
                    return;
                }

                if (DateTime.UtcNow > end)
                {
                    Assert.Fail($"No queue is bound to payments.events for {routingKey} within 40 s after the start of the app.");
                }

                await Task.Delay(500, ct);
            }
        }

        public async Task PublishAsync(string exchange, string routingKey, string body, CancellationToken ct)
        {
            var properties = new JsonObject
            {
                ["message_id"] = Guid.CreateVersion7().ToString(),
                ["type"] = routingKey,
                ["content_type"] = "application/json",
                ["delivery_mode"] = 2,
            };
            Assert.IsTrue(await TryPublishAsync(exchange, routingKey, body, properties, ct), $"The message to {exchange} with {routingKey} was not routed to a queue.");
        }

        private async Task<bool> TryPublishAsync(string exchange, string routingKey, string body, JsonObject properties, CancellationToken ct)
        {
            var request = new JsonObject
            {
                ["properties"] = properties.DeepClone(),
                ["routing_key"] = routingKey,
                ["payload"] = body,
                ["payload_encoding"] = "string",
            };
            using var response = await _http.PostAsync(new Uri($"api/exchanges/%2F/{exchange}/publish", UriKind.Relative), Json(request), ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            return response.IsSuccessStatusCode && JsonNode.Parse(text)?["routed"]?.GetValue<bool>() == true;
        }

        public async Task<JsonObject?> WaitForPaymentQueueAsync(CancellationToken ct)
        {
            var end = DateTime.UtcNow + TimeSpan.FromSeconds(40);
            while (DateTime.UtcNow < end)
            {
                var name = await PaymentQueueNameAsync(ct);
                if (name is not null)
                {
                    return await QueueAsync(name, ct);
                }

                await Task.Delay(500, ct);
            }

            return null;
        }

        private async Task<string?> PaymentQueueNameAsync(CancellationToken ct)
        {
            using var response = await _http.GetAsync(new Uri("api/exchanges/%2F/payments.events/bindings/source", UriKind.Relative), ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var bindings = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))?.AsArray() ?? [];
            return bindings
                .Where(b => b?["destination_type"]?.GetValue<string>() == "queue" && TopicMatches(b["routing_key"]?.GetValue<string>() ?? string.Empty, Captured))
                .Select(b => b!["destination"]!.GetValue<string>())
                .FirstOrDefault();
        }

        public async Task<int> PaymentQueueDepthAsync(CancellationToken ct)
        {
            var name = await PaymentQueueNameAsync(ct);
            return name is null ? -1 : await QueueDepthAsync(name, ct);
        }

        public async Task<string> DescribePaymentQueueAsync(CancellationToken ct)
        {
            var name = await PaymentQueueNameAsync(ct);
            return name is null ? "No payment queue is bound." : await DescribeQueueAsync(name, ct);
        }

        public async Task<int> QueueDepthAsync(string name, CancellationToken ct)
        {
            var queue = await QueueAsync(name, ct);
            return queue?["messages"]?.GetValue<int>() ?? -1;
        }

        public async Task<string> DescribeQueueAsync(string name, CancellationToken ct)
        {
            var queue = await QueueAsync(name, ct);
            if (queue is null)
            {
                return $"Queue {name} does not exist.";
            }

            var stats = queue["message_stats"];
            return $"Queue {name}: type={queue["type"]}, messages={queue["messages"]}, unacked={queue["messages_unacknowledged"]}, consumers={queue["consumers"]}, "
                + $"delivered={stats?["deliver_get"]}, redelivered={stats?["redeliver"]}, settings={Settings(queue).ToJsonString()}.";
        }

        public async Task<JsonObject?> QueueAsync(string name, CancellationToken ct)
        {
            using var response = await _http.GetAsync(new Uri($"api/queues/%2F/{Uri.EscapeDataString(name)}", UriKind.Relative), ct);
            return response.IsSuccessStatusCode ? JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))?.AsObject() : null;
        }

        public async Task WaitForConsumerAsync(string name, CancellationToken ct)
        {
            var end = DateTime.UtcNow + TimeSpan.FromSeconds(40);
            while (DateTime.UtcNow < end)
            {
                var queue = await QueueAsync(name, ct);
                if (queue?["consumers"]?.GetValue<int>() > 0)
                {
                    return;
                }

                await Task.Delay(500, ct);
            }

            Assert.Fail($"No consumer on {name} within 40 s after the start of the app. {await DescribeQueueAsync(name, ct)}");
        }

        // The queues that receive the dead letters of a queue: the queues bound to its dead-letter exchange.
        public async Task<List<string>> DeadLetterQueuesAsync(JsonObject queue, CancellationToken ct)
        {
            var exchange = Settings(queue)["dead-letter-exchange"]?.GetValue<string>();
            if (string.IsNullOrEmpty(exchange))
            {
                return [];
            }

            using var response = await _http.GetAsync(new Uri($"api/exchanges/%2F/{Uri.EscapeDataString(exchange)}/bindings/source", UriKind.Relative), ct);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var bindings = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))?.AsArray() ?? [];
            return [.. bindings.Where(b => b?["destination_type"]?.GetValue<string>() == "queue").Select(b => b!["destination"]!.GetValue<string>()).Distinct()];
        }

        public async Task<JsonObject?> WaitForMessageAsync(IReadOnlyList<string> queues, string marker, TimeSpan wait, CancellationToken ct, bool peek = false)
        {
            var end = DateTime.UtcNow + wait;
            while (DateTime.UtcNow < end)
            {
                foreach (var queue in queues)
                {
                    var request = new JsonObject { ["count"] = 200, ["ackmode"] = peek ? "ack_requeue_true" : "ack_requeue_false", ["encoding"] = "auto" };
                    using var response = await _http.PostAsync(new Uri($"api/queues/%2F/{Uri.EscapeDataString(queue)}/get", UriKind.Relative), Json(request), ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var messages = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))?.AsArray() ?? [];
                    var hit = messages.FirstOrDefault(m => m?["payload"]?.GetValue<string>().Contains(marker, StringComparison.OrdinalIgnoreCase) == true);
                    if (hit is not null)
                    {
                        return hit.AsObject();
                    }
                }

                await Task.Delay(1000, ct);
            }

            return null;
        }

        // The queue arguments without the "x-" prefix, with the effective policy on top.
        public static JsonObject Settings(JsonObject queue)
        {
            var settings = new JsonObject();
            if (queue["arguments"] is JsonObject arguments)
            {
                foreach (var (key, value) in arguments)
                {
                    settings[key.StartsWith("x-", StringComparison.Ordinal) ? key[2..] : key] = value?.DeepClone();
                }
            }

            if (queue["effective_policy_definition"] is JsonObject policy)
            {
                foreach (var (key, value) in policy)
                {
                    settings[key] ??= value?.DeepClone();
                }
            }

            return settings;
        }

        private static bool TopicMatches(string pattern, string key)
        {
            static bool Match(string[] p, int i, string[] k, int j)
            {
                if (i == p.Length)
                {
                    return j == k.Length;
                }

                if (p[i] == "#")
                {
                    for (var skip = j; skip <= k.Length; skip++)
                    {
                        if (Match(p, i + 1, k, skip))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return j < k.Length && (p[i] == "*" || p[i] == k[j]) && Match(p, i + 1, k, j + 1);
            }

            return Match(pattern.Split('.'), 0, key.Split('.'), 0);
        }

        private static StringContent Json(JsonNode node) => new(node.ToJsonString(), Encoding.UTF8, "application/json");

        private async Task PutAsync(string path, JsonNode body, CancellationToken ct)
        {
            using var response = await _http.PutAsync(new Uri(path, UriKind.Relative), Json(body), ct);
            Assert.IsTrue(response.IsSuccessStatusCode, $"PUT {path} failed: {await response.Content.ReadAsStringAsync(ct)}");
        }

        private async Task PostAsync(string path, JsonNode body, CancellationToken ct)
        {
            using var response = await _http.PostAsync(new Uri(path, UriKind.Relative), Json(body), ct);
            Assert.IsTrue(response.IsSuccessStatusCode, $"POST {path} failed: {await response.Content.ReadAsStringAsync(ct)}");
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
