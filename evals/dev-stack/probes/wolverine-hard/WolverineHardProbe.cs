using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using JasperFx.CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Wolverine;
using Wolverine.Tracking;
using Wolverine.Util;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for wolverine-hard. Each test starts the app with its own fake parcel platform on a local port
// (the real HttpCarrierClient registration stays), creates an order through the public API, and delivers a
// payment capture to the app as the RabbitMQ listener would. The last test starts RabbitMQ 4.3 with the
// warehouse queue that the warehouse team declares (docs/warehouse.md).
[TestClass]
[DoNotParallelize]
public sealed class WolverineHardProbe
{
    private const string RejectedPostalCode = "9999 ZZ";
    private static readonly TimeSpan s_session = TimeSpan.FromSeconds(60);

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task MigrateAsync(TestContext context)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(AssemblyFixture.ConnectionString).Options;
        await using var db = new OrdersDbContext(options);
        await db.Database.MigrateAsync(context.CancellationToken);
    }

    [TestMethod]
    public async Task PaymentCaptured_BooksParcelAndSendsPickOrder()
    {
        await using var carrier = await FakeCarrier.StartAsync();
        await using var factory = Factory(carrier.Url);
        using var client = factory.CreateClient();
        var (orderId, name) = await CreateOrderAsync(client, "7271 AB");

        var session = await Host(factory).TrackActivity().Timeout(s_session)
            .SendMessageAndWaitAsync(Capture(orderId, ShippingSpeed.Express));

        var shipment = await SingleShipmentAsync(factory, orderId);
        Assert.AreEqual("Booked", shipment.Status, $"The shipment of a paid order is not booked. Carrier log: {carrier.Summary}");
        var requests = carrier.RequestsFor(name);
        Assert.HasCount(1, requests, $"The parcel platform got {requests.Count} booking requests for the order.");
        Assert.AreEqual("express", requests[0].Service, "The booking does not use the shipping speed of the capture.");
        Assert.AreEqual(carrier.TrackingFor(name), shipment.TrackingNumber, "The shipment does not store the tracking number of the booking.");
        AssertPickOrder(session, orderId, shipment);
    }

    [TestMethod]
    public async Task PaymentCapturedV1_StillBooksParcel()
    {
        await using var carrier = await FakeCarrier.StartAsync();
        await using var factory = Factory(carrier.Url);
        using var client = factory.CreateClient();
        var (orderId, name) = await CreateOrderAsync(client, "7271 AC");

        // A version 1 event from a payments node that is not updated yet.
        var session = await Host(factory).TrackActivity().Timeout(s_session)
            .InvokeMessageAndWaitAsync(new PaymentCaptured(Guid.CreateVersion7(), orderId, 5.00m, "EUR", DateTimeOffset.UtcNow));

        var shipment = await SingleShipmentAsync(factory, orderId);
        Assert.AreEqual("Booked", shipment.Status, $"A version 1 capture does not book the parcel. Carrier log: {carrier.Summary}");
        var requests = carrier.RequestsFor(name);
        Assert.HasCount(1, requests, $"The parcel platform got {requests.Count} booking requests for the order.");
        Assert.AreEqual("standard", requests[0].Service, "A version 1 capture must book standard delivery.");
        AssertPickOrder(session, orderId, shipment);
    }

    [TestMethod]
    public async Task DuplicateCapture_BooksOneParcel()
    {
        await using var carrier = await FakeCarrier.StartAsync();
        await using var factory = Factory(carrier.Url);
        using var client = factory.CreateClient();
        var (orderId, name) = await CreateOrderAsync(client, "7271 AD");
        var capture = Capture(orderId, ShippingSpeed.Standard);

        await Host(factory).TrackActivity().Timeout(s_session).SendMessageAndWaitAsync(capture);
        // The payments service sends the same event again: same PaymentId, a new envelope.
        var second = await Host(factory).TrackActivity().Timeout(s_session).DoNotAssertOnExceptionsDetected()
            .SendMessageAndWaitAsync(capture with { });

        var shipments = await ShipmentsAsync(factory, orderId);
        Assert.HasCount(1, shipments, $"The order has {shipments.Count} shipments after a duplicate capture.");
        Assert.AreEqual("Booked", shipments[0].Status, $"The shipment is not booked. Carrier log: {carrier.Summary}");
        Assert.AreEqual(1, carrier.BookingsFor(name), $"The parcel platform created {carrier.BookingsFor(name)} parcels for one order. Carrier log: {carrier.Summary}");
        Assert.IsEmpty(second.MovedToErrorQueue.Envelopes(), "The duplicate capture went to the error queue.");
    }

    [TestMethod]
    public async Task CarrierOutage_ParcelBookedAfterOutage()
    {
        await using var carrier = await FakeCarrier.StartAsync(outage: TimeSpan.FromSeconds(4));
        await using var factory = Factory(carrier.Url);
        using var client = factory.CreateClient();
        var (orderId, name) = await CreateOrderAsync(client, "7271 AE");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(Capture(orderId, ShippingSpeed.Standard));
        }

        var wait = TimeSpan.FromSeconds(100);
        var booked = await WaitForBookedAsync(factory, orderId, wait);
        Assert.IsTrue(booked, $"The parcel platform was down for 4 seconds. The parcel of the paid order was not booked within {wait.TotalSeconds} s. Carrier log: {carrier.Summary}");
        Assert.AreEqual(1, carrier.BookingsFor(name), "The parcel platform created more than one parcel.");
    }

    [TestMethod]
    public async Task AddressRejected_WaitsForCustomerService()
    {
        await using var carrier = await FakeCarrier.StartAsync();
        await using var factory = Factory(carrier.Url);
        using var client = factory.CreateClient();
        var (orderId, name) = await CreateOrderAsync(client, RejectedPostalCode);

        var session = await Host(factory).TrackActivity().Timeout(s_session).DoNotAssertOnExceptionsDetected()
            .SendMessageAndWaitAsync(Capture(orderId, ShippingSpeed.Standard));

        var shipments = await ShipmentsAsync(factory, orderId);
        Assert.HasCount(1, shipments, "A refused address must leave one shipment for customer service.");
        Assert.AreEqual("AddressProblem", shipments[0].Status, "The shipment of a refused address does not have the status AddressProblem.");
        Assert.HasCount(1, carrier.RequestsFor(name), $"The refused address was sent to the parcel platform {carrier.RequestsFor(name).Count} times. A refusal must not be retried.");
        Assert.IsEmpty(session.Sent.MessagesOf<PickOrder>(), "The warehouse got a pick order for a parcel that is not booked.");
    }

    [TestMethod]
    public async Task AddressCorrected_ManualBooking_SendsPickOrder()
    {
        await using var carrier = await FakeCarrier.StartAsync();
        await using var factory = Factory(carrier.Url);
        using var client = factory.CreateClient();
        var (orderId, _) = await CreateOrderAsync(client, RejectedPostalCode);
        await Host(factory).TrackActivity().Timeout(s_session).DoNotAssertOnExceptionsDetected()
            .SendMessageAndWaitAsync(Capture(orderId, ShippingSpeed.Express));
        var waiting = await SingleShipmentAsync(factory, orderId);
        Assert.AreEqual("AddressProblem", waiting.Status, "The shipment of a refused address does not have the status AddressProblem.");

        using var fix = await client.PutAsJsonAsync(new Uri($"/orders/{orderId}/delivery-address", UriKind.Relative),
            new { street = "Hofstraat 3", postalCode = "7271 AF", city = "Borculo", countryCode = "NL" }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, fix.StatusCode, "PUT /orders/{id}/delivery-address failed.");

        HttpResponseMessage? book = null;
        var session = await Host(factory).TrackActivity().Timeout(s_session).ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ =>
            book = await client.PostAsJsonAsync(new Uri($"/shipments/{waiting.Id}/book", UriKind.Relative), new { speed = 1 }, TestContext.CancellationToken)));

        using (book)
        {
            Assert.AreEqual(HttpStatusCode.OK, book!.StatusCode, $"POST /shipments/{{id}}/book after the address fix returned {(int)book.StatusCode}: {await book.Content.ReadAsStringAsync(TestContext.CancellationToken)}");
        }

        var shipment = await SingleShipmentAsync(factory, orderId);
        Assert.AreEqual("Booked", shipment.Status, "The manual booking did not book the shipment.");
        AssertPickOrder(session, orderId, shipment);
    }

    [TestMethod]
    public async Task ManualBooking_PendingShipment_SendsPickOrder()
    {
        await using var carrier = await FakeCarrier.StartAsync();
        await using var factory = Factory(carrier.Url);
        using var client = factory.CreateClient();
        var (orderId, _) = await CreateOrderAsync(client, "7271 AG");
        using var created = await client.PostAsJsonAsync(new Uri("/shipments", UriKind.Relative), new { orderId }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode, "POST /shipments failed.");
        var shipmentId = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.CancellationToken)).RootElement.GetProperty("id").GetGuid();

        HttpResponseMessage? book = null;
        var session = await Host(factory).TrackActivity().Timeout(s_session).ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ =>
            book = await client.PostAsJsonAsync(new Uri($"/shipments/{shipmentId}/book", UriKind.Relative), new { speed = 0 }, TestContext.CancellationToken)));

        using (book)
        {
            Assert.AreEqual(HttpStatusCode.OK, book!.StatusCode, $"POST /shipments/{{id}}/book returned {(int)book.StatusCode}.");
        }

        AssertPickOrder(session, orderId, await SingleShipmentAsync(factory, orderId));
    }

    [TestMethod]
    public async Task Production_StaticCode_BooksParcel()
    {
        await using var carrier = await FakeCarrier.StartAsync();
        // Start once in development, so that the Wolverine tables exist before the production start.
        await using (var development = Factory(carrier.Url))
        {
            _ = development.Services;
        }

        await using var factory = Factory(carrier.Url, "Production");
        HttpClient? client = null;
        Exception? startError = null;
        try
        {
            client = factory.CreateClient();
        }
        catch (Exception ex)
        {
            startError = ex;
        }

        Assert.IsNull(startError, $"The app does not start in production: {Describe(startError)}");
        using var http = client!;
        var (orderId, _) = await CreateOrderAsync(http, "7271 AH");
        Exception? captureError = null;
        try
        {
            await Host(factory).TrackActivity().Timeout(s_session).SendMessageAndWaitAsync(Capture(orderId, ShippingSpeed.Standard));
        }
        catch (Exception ex)
        {
            captureError = ex;
        }

        Assert.IsNull(captureError, $"In production the payment capture failed: {Describe(captureError)}");
        Assert.AreEqual("Booked", (await SingleShipmentAsync(factory, orderId)).Status, "In production the parcel of a paid order is not booked.");
    }

    [TestMethod]
    [Timeout(240_000, CooperativeCancellation = true)]
    public async Task ZWarehouseQueue_GetsPickOrder()
    {
        var ct = TestContext.CancellationToken;
        await using var carrier = await FakeCarrier.StartAsync();
        await using var broker = await Broker.StartAsync(ct);
        await broker.DeclareWarehouseQueueAsync(ct);

        var errors = new ErrorLog();
        var factory = Factory(carrier.Url, rabbitMq: broker.AmqpUri, errors: errors);
        var start = Task.Run(() => factory.CreateClient(), ct);
        var startWait = TimeSpan.FromSeconds(90);
        if (await Task.WhenAny(start, Task.Delay(startWait, ct)) != start)
        {
            // The host still tries to connect. Do not wait for it.
            Assert.Fail($"The app did not start within {startWait.TotalSeconds} s on a broker where the warehouse queue exists as the warehouse team declares it.");
        }

        try
        {
            using var client = await start;
            var (orderId, _) = await CreateOrderAsync(client, "7271 AJ");
            Assert.IsNotNull(
                await broker.WaitForQueueAsync("orders-api.payment-captured", TimeSpan.FromSeconds(30), ct),
                "The app did not declare its queue orders-api.payment-captured within 30 seconds after the start.");

            // The payments service publishes the capture on the queue of this service, as a Wolverine envelope.
            await broker.PublishCaptureAsync("orders-api.payment-captured", Capture(orderId, ShippingSpeed.Standard), ct);

            var wait = TimeSpan.FromSeconds(60);
            var found = await broker.WaitForMessageAsync("warehouse.pick-orders", orderId, wait, ct);
            Assert.IsTrue(found, $"No pick order for {orderId} reached the queue warehouse.pick-orders within {wait.TotalSeconds} s. Carrier log: {carrier.Summary}");
            var queue = await broker.GetAsync("api/queues/%2F/warehouse.pick-orders", ct);
            Assert.AreEqual("quorum", queue.GetProperty("type").GetString(), "The warehouse queue is no longer the queue of the warehouse team.");
            var brokerErrors = errors.Entries.Where(e => e.Contains("warehouse.pick-orders", StringComparison.Ordinal) || e.Contains("start message broker", StringComparison.Ordinal)).ToList();
            Assert.IsEmpty(brokerErrors, $"The app logged broker errors for the queue of the warehouse team: {string.Join(" | ", brokerErrors.Take(2))}");
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    private static PaymentCapturedV2 Capture(Guid orderId, ShippingSpeed speed) =>
        new(Guid.CreateVersion7(), orderId, 5.00m, "EUR", DateTimeOffset.UtcNow, speed);

    private static IHost Host(WebApplicationFactory<Program> factory) => factory.Services.GetRequiredService<IHost>();

    private static string Describe(Exception? ex)
    {
        var inner = ex;
        while (inner is AggregateException { InnerException: { } next })
        {
            inner = next;
        }

        return inner is null ? "no error" : $"{inner.GetType().Name}: {inner.Message.Split('\n')[0]}";
    }

    private static void AssertPickOrder(ITrackedSession session, Guid orderId, ShipmentRow shipment)
    {
        var picks = session.Sent.MessagesOf<PickOrder>().Where(p => p.OrderId == orderId).ToList();
        Assert.HasCount(1, picks, $"The warehouse got {picks.Count} pick orders for the booked parcel.");
        Assert.AreEqual(shipment.Id, picks[0].ShipmentId, "The pick order has another shipment id.");
        Assert.AreEqual(shipment.TrackingNumber, picks[0].TrackingNumber, "The pick order has another tracking number.");
        Assert.IsNotEmpty(picks[0].Lines, "The pick order has no lines.");
    }

    private static WebApplicationFactory<Program> Factory(string carrierUrl, string environment = "Development", string? rabbitMq = null, ErrorLog? errors = null)
    {
        // Program.cs ends with RunJasperFxCommands(args). The host starts in a test only with this flag.
        JasperFxEnvironment.AutoStartHost = true;
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("ConnectionStrings:RabbitMq", rabbitMq ?? "amqp://guest:guest@127.0.0.1:5672/");
            builder.UseSetting("Carrier:BaseUrl", carrierUrl);
            builder.UseSetting("Carrier:ApiKey", "probe-key");
            builder.UseSetting("Payments:BaseUrl", "http://127.0.0.1:9/");
            builder.ConfigureLogging(logging =>
            {
                if (errors is not null)
                {
                    logging.AddProvider(errors);
                }
            });
            builder.ConfigureServices(services =>
            {
                if (rabbitMq is null)
                {
                    services.DisableAllExternalWolverineTransports();
                }

                services.RunWolverineInSoloMode();
            });
        });
    }

    private async Task<(Guid OrderId, string Name)> CreateOrderAsync(HttpClient client, string postalCode)
    {
        var ct = TestContext.CancellationToken;
        var name = $"Probe {Guid.CreateVersion7():N}";
        using var customer = await client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative),
            new { name, email = $"probe-{Guid.CreateVersion7():N}@example.com", kind = 0 }, ct);
        Assert.AreEqual(HttpStatusCode.Created, customer.StatusCode, $"POST /customers failed: {await customer.Content.ReadAsStringAsync(ct)}");
        var customerId = JsonDocument.Parse(await customer.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("id").GetGuid();

        using var order = await client.PostAsJsonAsync(new Uri("/orders", UriKind.Relative), new
        {
            customerId,
            lines = new[] { new { sku = "SKU-PROBE-1", quantity = 2, unitPrice = 2.50m } },
            deliveryAddress = new { street = "Hofstraat 1", postalCode, city = "Borculo", countryCode = "NL" },
        }, ct);
        Assert.AreEqual(HttpStatusCode.Created, order.StatusCode, $"POST /orders failed: {await order.Content.ReadAsStringAsync(ct)}");
        return (JsonDocument.Parse(await order.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("id").GetGuid(), name);
    }

    private sealed record ShipmentRow(Guid Id, string Status, string? TrackingNumber);

    private async Task<List<ShipmentRow>> ShipmentsAsync(WebApplicationFactory<Program> factory, Guid orderId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var rows = await db.Shipments.AsNoTracking().Where(s => s.OrderId == orderId).ToListAsync(TestContext.CancellationToken);
        return [.. rows.Select(s => new ShipmentRow(s.Id, s.Status.ToString(), s.TrackingNumber))];
    }

    private async Task<ShipmentRow> SingleShipmentAsync(WebApplicationFactory<Program> factory, Guid orderId)
    {
        var rows = await ShipmentsAsync(factory, orderId);
        Assert.HasCount(1, rows, $"The order has {rows.Count} shipments. Expected one.");
        return rows[0];
    }

    private async Task<bool> WaitForBookedAsync(WebApplicationFactory<Program> factory, Guid orderId, TimeSpan wait)
    {
        var end = DateTime.UtcNow + wait;
        while (DateTime.UtcNow < end)
        {
            var rows = await ShipmentsAsync(factory, orderId);
            if (rows.Any(r => r.Status == "Booked"))
            {
                return true;
            }

            await Task.Delay(500, TestContext.CancellationToken);
        }

        return false;
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    // Keeps the error and critical log entries of the app.
    private sealed class ErrorLog : ILoggerProvider
    {
        public ConcurrentQueue<string> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class Logger(ErrorLog log, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel))
                {
                    log.Entries.Enqueue($"{category}: {formatter(state, exception)} {exception?.Message}");
                }
            }
        }
    }

    private sealed record CarrierRequest(string Name, string PostalCode, string Service, string? Key, int Status);

    // The parcel platform of docs/carrier-api.md: idempotency keys, 422 for one postal code, an optional outage.
    private sealed class FakeCarrier : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly TimeSpan _outage;
        private readonly ConcurrentQueue<CarrierRequest> _requests = new();
        private readonly ConcurrentDictionary<string, (string Id, string Tracking, string Name)> _bookings = new();
        private DateTime? _outageEnd;
        private int _next;

        private FakeCarrier(int port, TimeSpan outage)
        {
            _outage = outage;
            Url = $"http://127.0.0.1:{port}/";
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Logging.ClearProviders();
            _app = builder.Build();
            _app.MapPost("/v2/bookings", HandleAsync);
        }

        public string Url { get; }

        public string Summary => string.Join("; ", _requests.Select(r => $"{r.Service} {r.PostalCode} key={r.Key} -> {r.Status}")) is { Length: > 0 } s ? s : "no requests";

        public static async Task<FakeCarrier> StartAsync(TimeSpan outage = default)
        {
            var carrier = new FakeCarrier(FreePort(), outage);
            await carrier._app.StartAsync();
            return carrier;
        }

        public List<CarrierRequest> RequestsFor(string name) => [.. _requests.Where(r => r.Name == name)];

        public int BookingsFor(string name) => _bookings.Values.Count(b => b.Name == name);

        public string? TrackingFor(string name) => _bookings.Values.Where(b => b.Name == name).Select(b => b.Tracking).FirstOrDefault();

        private async Task<IResult> HandleAsync(HttpRequest request)
        {
            using var doc = await JsonDocument.ParseAsync(request.Body);
            var root = doc.RootElement;
            var recipient = root.GetProperty("recipient");
            var name = recipient.GetProperty("name").GetString() ?? string.Empty;
            var postalCode = recipient.GetProperty("postalCode").GetString() ?? string.Empty;
            var service = root.GetProperty("service").GetString() ?? string.Empty;
            var key = request.Headers["Idempotency-Key"].FirstOrDefault();

            lock (_requests)
            {
                _outageEnd ??= DateTime.UtcNow + _outage;
            }

            if (DateTime.UtcNow < _outageEnd)
            {
                _requests.Enqueue(new(name, postalCode, service, key, 503));
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            if (postalCode == RejectedPostalCode)
            {
                _requests.Enqueue(new(name, postalCode, service, key, 422));
                return Results.Json(new { code = "address_rejected", detail = "The carrier does not deliver to this postal code." }, statusCode: 422);
            }

            var bookingKey = key ?? Guid.NewGuid().ToString("N");
            var existed = _bookings.TryGetValue(bookingKey, out var booking);
            if (!existed)
            {
                var n = Interlocked.Increment(ref _next);
                booking = _bookings.GetOrAdd(bookingKey, ($"bk-{n}", $"JVGL{Random.Shared.Next(100000000, 999999999)}{n}", name));
            }

            _requests.Enqueue(new(name, postalCode, service, key, existed ? 200 : 201));
            return Results.Json(new { bookingId = booking.Id, carrier = "dhl", trackingNumber = booking.Tracking }, statusCode: existed ? 200 : 201);
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private sealed class Broker : IAsyncDisposable
    {
        private readonly IContainer _container;
        private readonly HttpClient _http;
        private readonly int _amqpPort;

        private Broker(int amqpPort, int httpPort)
        {
            _amqpPort = amqpPort;
            _container = new ContainerBuilder("rabbitmq:4.3-management-alpine")
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
            var end = DateTime.UtcNow + TimeSpan.FromSeconds(90);
            while (DateTime.UtcNow < end)
            {
                try
                {
                    using var response = await broker._http.GetAsync(new Uri("api/health/checks/port-listener/5672", UriKind.Relative), ct);
                    if (response.IsSuccessStatusCode)
                    {
                        return broker;
                    }
                }
                catch (HttpRequestException)
                {
                }

                await Task.Delay(500, ct);
            }

            throw new InvalidOperationException("RabbitMQ did not start within 90 seconds.");
        }

        // As the warehouse service does it (docs/warehouse.md).
        public async Task DeclareWarehouseQueueAsync(CancellationToken ct)
        {
            using var exchange = await _http.PutAsJsonAsync(new Uri("api/exchanges/%2F/warehouse.dlx", UriKind.Relative),
                new { type = "fanout", durable = true }, ct);
            exchange.EnsureSuccessStatusCode();
            using var queue = await _http.PutAsJsonAsync(new Uri("api/queues/%2F/warehouse.pick-orders", UriKind.Relative),
                new { durable = true, auto_delete = false, arguments = new Dictionary<string, object> { ["x-queue-type"] = "quorum", ["x-dead-letter-exchange"] = "warehouse.dlx" } }, ct);
            queue.EnsureSuccessStatusCode();
        }

        public async Task<JsonElement?> WaitForQueueAsync(string queue, TimeSpan wait, CancellationToken ct)
        {
            var end = DateTime.UtcNow + wait;
            while (DateTime.UtcNow < end)
            {
                using var response = await _http.GetAsync(new Uri($"api/queues/%2F/{queue}", UriKind.Relative), ct);
                if (response.IsSuccessStatusCode)
                {
                    using var consumers = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (consumers.RootElement.TryGetProperty("consumers", out var count) && count.GetInt32() > 0)
                    {
                        return consumers.RootElement.Clone();
                    }
                }

                await Task.Delay(500, ct);
            }

            return null;
        }

        public async Task PublishCaptureAsync(string queue, PaymentCapturedV2 capture, CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(capture);
            using var response = await _http.PostAsJsonAsync(new Uri("api/exchanges/%2F/amq.default/publish", UriKind.Relative), new
            {
                routing_key = queue,
                payload,
                payload_encoding = "string",
                properties = new
                {
                    content_type = "application/json",
                    message_id = Guid.CreateVersion7().ToString(),
                    type = typeof(PaymentCapturedV2).ToMessageTypeName(),
                },
            }, ct);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync(ct);
            Assert.Contains("true", text, $"RabbitMQ did not route the capture to {queue}: {text}");
        }

        public async Task<JsonElement> GetAsync(string path, CancellationToken ct)
        {
            var text = await _http.GetStringAsync(new Uri(path, UriKind.Relative), ct);
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        public async Task<bool> WaitForMessageAsync(string queue, Guid orderId, TimeSpan wait, CancellationToken ct)
        {
            var end = DateTime.UtcNow + wait;
            var id = orderId.ToString();
            while (DateTime.UtcNow < end)
            {
                try
                {
                    using var response = await _http.PostAsJsonAsync(new Uri($"api/queues/%2F/{queue}/get", UriKind.Relative),
                        new { count = 50, ackmode = "ack_requeue_true", encoding = "auto" }, ct);
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

                await Task.Delay(1000, ct);
            }

            return false;
        }

        public async ValueTask DisposeAsync()
        {
            _http.Dispose();
            await _container.DisposeAsync();
        }
    }
}
