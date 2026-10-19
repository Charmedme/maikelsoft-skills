using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for dotnet-observability-trap. It posts an order with a customer email address,
// records every log entry (message and structured values) and every metric measurement,
// and checks that the raw email and the customer id do not leak.
// A dummy Application Insights connection string lets a distro start without network access.
[TestClass]
[DoNotParallelize]
public sealed class ObservabilityProbe
{
    private const string Email = "probe.customer.4711@example.nl";
    private const string DummyConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9/;LiveEndpoint=http://127.0.0.1:9/";

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task CreateOrder_LogContainsNoRawEmail()
    {
        var logs = new RecordingLoggerProvider();
        await using var factory = new ProbeFactory(logs);
        await factory.MigrateAsync(TestContext.CancellationToken);
        using var client = factory.CreateClient();
        var customerId = Guid.CreateVersion7();

        var orderId = await CreateOrderAsync(client, customerId, TestContext.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.CancellationToken);

        var entries = logs.Entries.ToList();
        var orderEntries = entries.Where(e => e.Contains(customerId.ToString()) || e.Contains(orderId)).ToList();
        Assert.IsTrue(orderEntries.Count > 0,
            $"No log entry names the order id or the customer id ({entries.Count} entries recorded). The app must log the created order through ILogger, and the logs must reach the logger providers.");
        var leaks = entries.Where(e => e.Contains(Email, StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.IsTrue(leaks.Count == 0, "The raw email address is in the log: " + string.Join(" || ", leaks.Take(3)));
    }

    [TestMethod]
    public async Task OrdersCreatedCounter_HasNoCustomerTag()
    {
        var customerId = Guid.CreateVersion7();
        var bad = new ConcurrentQueue<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) => l.EnableMeasurementEvents(instrument);
        void Inspect(Instrument instrument, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            foreach (var tag in tags)
            {
                var value = Convert.ToString(tag.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                if (tag.Key.Contains("customer", StringComparison.OrdinalIgnoreCase)
                    || tag.Key.Contains("email", StringComparison.OrdinalIgnoreCase)
                    || value.Contains(customerId.ToString(), StringComparison.OrdinalIgnoreCase)
                    || value.Contains(Email, StringComparison.OrdinalIgnoreCase))
                {
                    bad.Enqueue($"{instrument.Meter.Name}/{instrument.Name}: {tag.Key}={value}");
                }
            }
        }

        listener.SetMeasurementEventCallback<long>((i, _, tags, _) => Inspect(i, tags));
        listener.SetMeasurementEventCallback<int>((i, _, tags, _) => Inspect(i, tags));
        listener.SetMeasurementEventCallback<double>((i, _, tags, _) => Inspect(i, tags));
        listener.SetMeasurementEventCallback<decimal>((i, _, tags, _) => Inspect(i, tags));
        listener.Start();

        await using var factory = new ProbeFactory(new RecordingLoggerProvider());
        await factory.MigrateAsync(TestContext.CancellationToken);
        using var client = factory.CreateClient();
        await CreateOrderAsync(client, customerId, TestContext.CancellationToken);
        await CreateOrderAsync(client, customerId, TestContext.CancellationToken);
        listener.RecordObservableInstruments();

        Assert.IsTrue(bad.IsEmpty, "A metric carries a customer tag (high cardinality, personal data): " + string.Join(" || ", bad.Distinct().Take(3)));
    }

    private static async Task<string> CreateOrderAsync(HttpClient client, Guid customerId, CancellationToken ct)
    {
        var body = new
        {
            customerId,
            customerEmail = Email,
            lines = new[] { new { sku = "SKU-1", quantity = 2, unitPrice = 3.50m } },
        };
        using var response = await client.PostAsJsonAsync("/orders", body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "POST /orders failed: " + text);
        using var json = JsonDocument.Parse(text);
        return json.RootElement.TryGetProperty("id", out var id) ? id.ToString() : Guid.Empty.ToString();
    }

    private sealed class ProbeFactory(RecordingLoggerProvider logs) : WebApplicationFactory<Program>
    {
        public async Task MigrateAsync(CancellationToken ct)
        {
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", DummyConnectionString);
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
        }
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Entries);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(string category, ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var text = $"{category} [{eventId.Id}] {formatter(state, exception)}";
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                text += " | " + string.Join("; ", values.Select(v => $"{v.Key}={v.Value}"));
            }

            entries.Enqueue(text);
        }
    }
}
