using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for dotnet-observability-hard. It dispatches shipments through the public API against a fake
// carrier gateway on a local port, with a dummy Application Insights connection string, so that the
// production telemetry setup (the Azure Monitor distro) starts without network access. It records every
// log entry (message, structured values and scopes), every span and every metric measurement.
[TestClass]
[DoNotParallelize]
public sealed class CarrierTelemetryProbe
{
    private const string AiVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";
    private const string DummyConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9/;LiveEndpoint=http://127.0.0.1:9/";

    private const string RecipientName = "Probe Vandermeulen-Ruisdael";
    private const string Street = "Probelaan 4711";
    private const string PostalCode = "9999 ZQ";
    private const string City = "Probedorp";
    private const string Phone = "+31687654321";
    private const string Email = "probe.recipient.4711@example.nl";
    private const string RefusalCode = "ADDRESS_UNDELIVERABLE";
    private const string RefusalMessage = "The carrier does not deliver to this address.";

    // The phone number is checked without the plus sign, because JSON escapes it as \u002B.
    private static readonly string[] s_personal = [RecipientName, Street, PostalCode, City, Phone[1..], Email, "Vandermeulen"];
    private static string? s_previousAi;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        s_previousAi = Environment.GetEnvironmentVariable(AiVariable);
        Environment.SetEnvironmentVariable(AiVariable, DummyConnectionString);
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(AssemblyFixture.ConnectionString).Options;
        await using var db = new OrdersDbContext(options);
        await db.Database.MigrateAsync(context.CancellationToken);
    }

    [ClassCleanup]
    public static void Cleanup() => Environment.SetEnvironmentVariable(AiVariable, s_previousAi);

    [TestMethod]
    public async Task Dispatch_Booked_StoresTrackingNumber()
    {
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();

        var dispatch = await DispatchAsync(client, GatewayMode.Booked, gateway);

        Assert.AreEqual(HttpStatusCode.OK, dispatch.Status, "A booked dispatch must answer 200: " + dispatch.Body);
        Assert.Contains(dispatch.Tracking!, dispatch.Body, "The answer does not hold the tracking number of the gateway.");
        Assert.AreEqual("Dispatched", await StatusAsync(client, dispatch.ShipmentId), "The shipment is not dispatched.");
    }

    [TestMethod]
    public async Task Dispatch_Refused_Returns409WithReason_ShipmentStaysPending()
    {
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();

        var dispatch = await DispatchAsync(client, GatewayMode.Refused, gateway);

        Assert.AreEqual(HttpStatusCode.Conflict, dispatch.Status, "A refused booking must answer 409: " + dispatch.Body);
        Assert.Contains(RefusalCode, dispatch.Body, "The 409 answer does not hold the reason code of the gateway.");
        Assert.AreEqual("Pending", await StatusAsync(client, dispatch.ShipmentId), "A refused shipment must stay pending.");

        gateway.Mode = GatewayMode.Booked;
        using var retry = await client.PostAsJsonAsync($"/shipments/{dispatch.ShipmentId}/dispatch", new { carrier = "postnl" }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, retry.StatusCode, "The warehouse must be able to dispatch again after a refusal: " + await retry.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Dispatch_GatewayError_Returns502_ShipmentStaysPending()
    {
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();

        var dispatch = await DispatchAsync(client, GatewayMode.Error, gateway);

        Assert.AreEqual(HttpStatusCode.BadGateway, dispatch.Status, "A gateway error must answer 502: " + dispatch.Body);
        Assert.AreEqual("Pending", await StatusAsync(client, dispatch.ShipmentId), "The shipment must stay pending after a gateway error.");
    }

    [TestMethod]
    public async Task Refusal_LogNamesReasonAndShipment()
    {
        var logs = new RecordingLoggerProvider();
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, logs);
        using var client = factory.CreateClient();

        var dispatch = await DispatchAsync(client, GatewayMode.Refused, gateway);
        await Task.Delay(300, TestContext.CancellationToken);

        var reasons = logs.Entries.Where(e => e.Contains(RefusalCode, StringComparison.Ordinal)).ToList();
        Assert.IsNotEmpty(reasons, $"No log entry holds the reason code {RefusalCode} ({logs.Entries.Count} entries).");
        Assert.IsTrue(
            reasons.Any(e => e.Contains(dispatch.ShipmentId, StringComparison.OrdinalIgnoreCase) || e.Contains(dispatch.ShipmentId.Replace("-", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)),
            "No log entry with the reason code carries the shipment id (in its values or its scope): " + string.Join(" || ", reasons.Take(2)));
    }

    [TestMethod]
    public async Task Refusal_LogsHoldNoRecipientData()
    {
        var logs = new RecordingLoggerProvider();
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, logs);
        using var client = factory.CreateClient();

        await DispatchAsync(client, GatewayMode.Booked, gateway);
        await DispatchAsync(client, GatewayMode.Refused, gateway);
        await DispatchAsync(client, GatewayMode.Error, gateway);
        await Task.Delay(300, TestContext.CancellationToken);

        var leaks = logs.Entries.Where(ContainsPersonalData).ToList();
        Assert.IsEmpty(leaks, "A log entry holds personal data of the recipient: " + string.Join(" || ", leaks.Take(2)));
    }

    [TestMethod]
    public async Task Booking_SpansHoldNoRecipientData()
    {
        var spans = new ConcurrentQueue<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => spans.Enqueue(Describe(activity)),
        };
        ActivitySource.AddActivityListener(listener);
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();

        await DispatchAsync(client, GatewayMode.Booked, gateway);
        await DispatchAsync(client, GatewayMode.Refused, gateway);
        await DispatchAsync(client, GatewayMode.Error, gateway);

        var leaks = spans.Where(ContainsPersonalData).ToList();
        Assert.IsEmpty(leaks, "A span (tags, events or status) holds personal data of the recipient: " + string.Join(" || ", leaks.Take(2)));
    }

    [TestMethod]
    public async Task BookingMetrics_PerCarrierAndOutcome()
    {
        using var metrics = new RecordingMeterListener();
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();

        metrics.Phase = "booked";
        await DispatchAsync(client, GatewayMode.Booked, gateway);
        metrics.Phase = "refused";
        await DispatchAsync(client, GatewayMode.Refused, gateway);
        metrics.Phase = "none";

        var histograms = metrics.Measurements
            .Where(m => m.IsHistogram && !IsLibraryName(m.Meter))
            .GroupBy(m => $"{m.Meter}/{m.Instrument}");
        var found = histograms.Any(group =>
        {
            var booked = group.Where(m => m.Phase == "booked" && HasCarrier(m)).Select(TagsWithoutCarrier).ToHashSet();
            var refused = group.Where(m => m.Phase == "refused" && HasCarrier(m)).Select(TagsWithoutCarrier).ToHashSet();
            return booked.Count > 0 && refused.Count > 0 && !booked.SetEquals(refused);
        });
        Assert.IsTrue(
            found,
            "No histogram of the service records the booking time with the carrier as a tag and a different outcome tag for a booked and a refused booking. App instruments seen: "
            + string.Join(", ", metrics.Measurements.Where(m => !IsLibraryName(m.Meter)).Select(m => $"{m.Meter}/{m.Instrument}[{m.Tags}]").Distinct().Take(8)));
    }

    [TestMethod]
    public async Task Metrics_HaveNoIdOrPersonalTags()
    {
        using var metrics = new RecordingMeterListener();
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();

        var booked = await DispatchAsync(client, GatewayMode.Booked, gateway);
        var refused = await DispatchAsync(client, GatewayMode.Refused, gateway);
        using var delivered = await client.PostAsync($"/shipments/{booked.ShipmentId}/delivered", null, TestContext.CancellationToken);

        string[] badKeys = ["customer", "shipment_id", "shipment.id", "order_id", "order.id", "orderid", "shipmentid", "customerid", "tracking", "email", "phone", "postal", "recipient", "reference"];
        string[] badValues = [.. s_personal, booked.ShipmentId, booked.OrderId, booked.CustomerId, refused.ShipmentId, refused.OrderId, refused.CustomerId, booked.Tracking!];
        var bad = metrics.Measurements
            .SelectMany(m => m.TagList.Select(t => (m, t.Key, t.Value)))
            .Where(x => badKeys.Any(k => x.Key.Contains(k, StringComparison.OrdinalIgnoreCase))
                || badValues.Any(v => x.Value.Contains(v, StringComparison.OrdinalIgnoreCase)))
            .Select(x => $"{x.m.Meter}/{x.m.Instrument}: {x.Key}={x.Value}")
            .Distinct()
            .ToList();
        Assert.IsEmpty(bad, "A metric carries an id or personal data as a tag (one time series per value): " + string.Join(" || ", bad.Take(3)));    }

    [TestMethod]
    public async Task Sampling_KeepsEveryTrace()
    {
        var recorded = 0;
        var seen = 0;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.None,
            ActivityStopped = activity =>
            {
                Interlocked.Increment(ref seen);
                if (activity.Recorded)
                {
                    Interlocked.Increment(ref recorded);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();
        AssertDistroRuns(factory);

        const int Requests = 40;
        for (var i = 0; i < Requests; i++)
        {
            using var response = await client.GetAsync("/healthz/live", TestContext.CancellationToken);
        }

        await Task.Delay(200, TestContext.CancellationToken);
        Assert.IsGreaterThanOrEqualTo(Requests - 1, recorded, $"Only {recorded} of {seen} request traces were sampled (OPS-20: keep all traces).");
    }

    [TestMethod]
    public async Task LogScopes_AreExported()
    {
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();
        AssertDistroRuns(factory);

        var options = factory.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryLoggerOptions>>().CurrentValue;

        Assert.IsTrue(options.IncludeScopes, "The OpenTelemetry log exporter drops scopes (IncludeScopes is false), so the ShipmentId scope never reaches Application Insights.");
    }

    [TestMethod]
    public async Task AppSourcesAndMeters_AreExported()
    {
        var sources = new ConcurrentDictionary<string, ActivitySource>();
        var instruments = new ConcurrentDictionary<string, Instrument>();
        using var sourceSpy = new ActivityListener
        {
            ShouldListenTo = source =>
            {
                if (IsServiceName(source.Name))
                {
                    sources[source.Name] = source;
                }

                return false;
            },
        };
        ActivitySource.AddActivityListener(sourceSpy);
        using var meterSpy = new MeterListener
        {
            InstrumentPublished = (instrument, _) =>
            {
                if (IsServiceName(instrument.Meter.Name))
                {
                    instruments[$"{instrument.Meter.Name}/{instrument.Name}"] = instrument;
                }
            },
        };
        meterSpy.Start();
        await using var gateway = FakeGateway.Start();
        await using var factory = new ProbeFactory(gateway.BaseUrl, new RecordingLoggerProvider());
        using var client = factory.CreateClient();
        AssertDistroRuns(factory);

        var booked = await DispatchAsync(client, GatewayMode.Booked, gateway);
        await DispatchAsync(client, GatewayMode.Refused, gateway);
        using var delivered = await client.PostAsync($"/shipments/{booked.ShipmentId}/delivered", null, TestContext.CancellationToken);

        Assert.IsNotEmpty(sources, "The service has no ActivitySource of its own.");
        var silentSources = sources.Values.Where(s => !s.HasListeners()).Select(s => s.Name).ToList();
        var silentInstruments = instruments.Where(i => !i.Value.Enabled).Select(i => i.Key).ToList();
        Assert.IsTrue(
            silentSources.Count == 0 && silentInstruments.Count == 0,
            $"Telemetry of the service is not exported (not registered with the distro). Sources: {string.Join(", ", silentSources)}. Instruments: {string.Join(", ", silentInstruments)}.");
    }

    private static void AssertDistroRuns(ProbeFactory factory) =>
        Assert.IsNotNull(
            factory.Services.GetService<TracerProvider>(),
            $"No OpenTelemetry tracer provider: the telemetry setup did not start with {AiVariable} set.");

    // A source or meter of the service: its name names the service or the shipping domain.
    private static bool IsServiceName(string name) =>
        !IsLibraryName(name)
        && new[] { "Orders", "Shipping", "Shipment", "Carrier", "Booking", "Dispatch" }.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static bool IsLibraryName(string name) =>
        name.StartsWith("Microsoft", StringComparison.Ordinal) || name.StartsWith("System", StringComparison.Ordinal)
        || name.StartsWith("OpenTelemetry", StringComparison.Ordinal) || name.StartsWith("Azure", StringComparison.Ordinal)
        || name.StartsWith("Npgsql", StringComparison.Ordinal) || name.StartsWith("Experimental", StringComparison.Ordinal)
        || name.StartsWith("Testcontainers", StringComparison.Ordinal) || name.StartsWith("Docker", StringComparison.Ordinal)
        || name.StartsWith("Polly", StringComparison.Ordinal) || name.StartsWith("MSTest", StringComparison.Ordinal)
        || name.StartsWith("Private.", StringComparison.Ordinal) || name.StartsWith("StandardMetric", StringComparison.Ordinal)
        || name.StartsWith("LiveMetric", StringComparison.Ordinal) || name.Length == 0;

    private static bool HasCarrier(RecordedMeasurement m) =>
        m.TagList.Any(t => t.Value.Equals("postnl", StringComparison.OrdinalIgnoreCase));

    private static string TagsWithoutCarrier(RecordedMeasurement m) =>
        string.Join(";", m.TagList.Where(t => !t.Value.Equals("postnl", StringComparison.OrdinalIgnoreCase)).OrderBy(t => t.Key, StringComparer.Ordinal).Select(t => $"{t.Key}={t.Value}"));

    private static bool ContainsPersonalData(string text) =>
        s_personal.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static string Describe(Activity activity)
    {
        var text = new StringBuilder($"{activity.Source.Name}/{activity.DisplayName} status={activity.StatusDescription}");
        foreach (var tag in activity.TagObjects)
        {
            text.Append(CultureInfo.InvariantCulture, $" {tag.Key}={tag.Value}");
        }

        foreach (var e in activity.Events)
        {
            text.Append(CultureInfo.InvariantCulture, $" event {e.Name}");
            foreach (var tag in e.Tags)
            {
                text.Append(CultureInfo.InvariantCulture, $" {tag.Key}={tag.Value}");
            }
        }

        foreach (var baggage in activity.Baggage)
        {
            text.Append(CultureInfo.InvariantCulture, $" baggage {baggage.Key}={baggage.Value}");
        }

        return text.ToString();
    }

    private async Task<string> StatusAsync(HttpClient client, string shipmentId)
    {
        var json = await client.GetStringAsync(new Uri($"/shipments/{shipmentId}", UriKind.Relative), TestContext.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("status").GetString() ?? string.Empty;
    }

    private async Task<DispatchResult> DispatchAsync(HttpClient client, GatewayMode mode, FakeGateway gateway)
    {
        var ct = TestContext.CancellationToken;
        var customerId = await PostForIdAsync(client, "/customers", new { name = RecipientName, email = Email, kind = 0, companyName = (string?)null }, ct);
        var orderId = await PostForIdAsync(client, "/orders", new
        {
            customerId,
            customerReference = "PO-PROBE",
            deliveryInstructions = "Leave at the back door.",
            deliveryAddress = new { recipientName = RecipientName, street = Street, postalCode = PostalCode, city = City, countryCode = "NL", phone = Phone },
            lines = new[] { new { sku = "SKU-PROBE-1", quantity = 1, unitPrice = 9.95m } },
        }, ct);
        var shipmentId = await PostForIdAsync(client, "/shipments", new { orderId }, ct);

        gateway.Mode = mode;
        using var response = await client.PostAsJsonAsync($"/shipments/{shipmentId}/dispatch", new { carrier = "postnl" }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return new DispatchResult(response.StatusCode, body, shipmentId, orderId, customerId, gateway.LastTracking);
    }

    private static async Task<string> PostForIdAsync(HttpClient client, string path, object body, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(path, body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, $"POST {path} failed: {text}");
        using var json = JsonDocument.Parse(text);
        return json.RootElement.GetProperty("id").ToString();
    }

    private sealed record DispatchResult(HttpStatusCode Status, string Body, string ShipmentId, string OrderId, string CustomerId, string? Tracking);

    private enum GatewayMode
    {
        Booked,
        Refused,
        Error,
    }

    private sealed class ProbeFactory(Uri gatewayUrl, RecordingLoggerProvider logs) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(AiVariable, DummyConnectionString);
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("CarrierGateway:BaseUrl", gatewayUrl.ToString());
            builder.UseSetting("CarrierGateway:ApiKey", "probe-api-key");
            builder.UseSetting("CarrierGateway:TimeoutSeconds", "10");
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
            builder.ConfigureServices(GiveHmacRedactorATestKey);
        }

        // A solution may use the HMAC redactor with its key from configuration. The probe host does not
        // read the test settings of the solution, so it gives the redactor a test key when the package is
        // present. Reflection keeps the probe compiling without the package.
        private static void GiveHmacRedactorATestKey(IServiceCollection services)
        {
            var type = Type.GetType("Microsoft.Extensions.Compliance.Redaction.HmacRedactorOptions, Microsoft.Extensions.Compliance.Redaction");
            if (type is null)
            {
                return;
            }

            typeof(ProbeFactory).GetMethod(nameof(PostConfigure), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, [services]);
        }

        private static void PostConfigure<T>(IServiceCollection services)
            where T : class =>
            services.PostConfigureAll<T>(options =>
            {
                typeof(T).GetProperty("Key")?.SetValue(options, Convert.ToBase64String(new byte[32]));
                typeof(T).GetProperty("KeyId")?.SetValue(options, 1);
            });
    }

    // Records each log entry as one line: category, event id, message, structured values and the active scopes.
    private sealed class RecordingLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

        public ConcurrentQueue<string> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, this);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(string category, RecordingLoggerProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => owner._scopes.Push(state);

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var text = new StringBuilder($"{category} [{eventId.Id}] {logLevel}: {formatter(state, exception)}");
                if (state is IEnumerable<KeyValuePair<string, object?>> values)
                {
                    text.Append(" | ").AppendJoin("; ", values.Select(v => $"{v.Key}={v.Value}"));
                }

                if (exception is not null)
                {
                    text.Append(" | exception: ").Append(exception.GetType().Name).Append(": ").Append(exception.Message);
                }

                owner._scopes.ForEachScope(
                    (scope, sb) =>
                    {
                        sb.Append(" | scope: ");
                        if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
                        {
                            sb.AppendJoin("; ", pairs.Select(p => $"{p.Key}={p.Value}"));
                        }
                        else
                        {
                            sb.Append(scope);
                        }
                    },
                    text);
                owner.Entries.Enqueue(text.ToString());
            }
        }
    }

    private sealed record RecordedMeasurement(string Phase, string Meter, string Instrument, bool IsHistogram, IReadOnlyList<KeyValuePair<string, string>> TagList)
    {
        public string Tags => string.Join(";", TagList.Select(t => $"{t.Key}={t.Value}"));
    }

    private sealed class RecordingMeterListener : IDisposable
    {
        private readonly MeterListener _listener = new();

        public RecordingMeterListener()
        {
            _listener.InstrumentPublished = (instrument, listener) => listener.EnableMeasurementEvents(instrument);
            _listener.SetMeasurementEventCallback<long>((i, _, tags, _) => Record(i, tags));
            _listener.SetMeasurementEventCallback<int>((i, _, tags, _) => Record(i, tags));
            _listener.SetMeasurementEventCallback<double>((i, _, tags, _) => Record(i, tags));
            _listener.SetMeasurementEventCallback<float>((i, _, tags, _) => Record(i, tags));
            _listener.SetMeasurementEventCallback<decimal>((i, _, tags, _) => Record(i, tags));
            _listener.Start();
        }

        public string Phase { get; set; } = "none";

        public ConcurrentQueue<RecordedMeasurement> Measurements { get; } = new();

        public void Dispose() => _listener.Dispose();

        private void Record(Instrument instrument, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (var tag in tags)
            {
                list.Add(new(tag.Key, Convert.ToString(tag.Value, CultureInfo.InvariantCulture) ?? string.Empty));
            }

            var isHistogram = instrument.GetType().IsGenericType && instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>);
            Measurements.Enqueue(new RecordedMeasurement(Phase, instrument.Meter.Name, instrument.Name, isHistogram, list));
        }
    }

    // A fake carrier gateway: POST /v2/bookings answers by the mode of the test. A refusal echoes the recipient.
    private sealed class FakeGateway : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _stop = new();
        private int _count;
        private Task _loop = Task.CompletedTask;

        private FakeGateway(int port)
        {
            BaseUrl = new Uri($"http://127.0.0.1:{port}/");
            _listener.Prefixes.Add(BaseUrl.ToString());
        }

        public Uri BaseUrl { get; }

        public GatewayMode Mode { get; set; }

        public string? LastTracking { get; private set; }

        public static FakeGateway Start()
        {
            var gateway = new FakeGateway(FreePort());
            gateway._listener.Start();
            gateway._loop = gateway.LoopAsync();
            return gateway;
        }

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            _listener.Stop();
            try
            {
                await _loop;
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or OperationCanceledException)
            {
            }

            _listener.Close();
            _stop.Dispose();
        }

        private static int FreePort()
        {
            using var socket = new TcpListener(IPAddress.Loopback, 0);
            socket.Start();
            return ((IPEndPoint)socket.LocalEndpoint).Port;
        }

        private async Task LoopAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                var request = await reader.ReadToEndAsync();
                var (status, body) = Answer(context.Request.Url?.AbsolutePath ?? string.Empty, request);
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                var bytes = Encoding.UTF8.GetBytes(body);
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }

        private (int Status, string Body) Answer(string path, string request)
        {
            if (!path.EndsWith("/v2/bookings", StringComparison.Ordinal))
            {
                return (404, "{}");
            }

            switch (Mode)
            {
                case GatewayMode.Booked:
                    LastTracking = $"3SPROBE{Interlocked.Increment(ref _count):D6}";
                    return (201, $$"""{"trackingNumber":"{{LastTracking}}","labelUrl":"https://labels.gateway.test/{{LastTracking}}.pdf"}""");
                case GatewayMode.Refused:
                    JsonNode? recipient = null;
                    try
                    {
                        recipient = JsonNode.Parse(request)?["recipient"]?.DeepClone();
                    }
                    catch (JsonException)
                    {
                    }

                    var refusal = new JsonObject
                    {
                        ["code"] = RefusalCode,
                        ["message"] = RefusalMessage,
                        ["recipient"] = recipient ?? new JsonObject { ["name"] = RecipientName, ["postalCode"] = PostalCode, ["phone"] = Phone, ["email"] = Email },
                    };
                    return (422, refusal.ToJsonString());
                default:
                    return (503, """{"error":"upstream carrier unavailable"}""");
            }
        }
    }
}
