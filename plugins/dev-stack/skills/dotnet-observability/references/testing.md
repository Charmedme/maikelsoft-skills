# Tests for logs, spans, and metrics

Test the behavior that a reader of the logs and the dashboards depends on: the event ID, the level, the structured values, no raw personal data, and the metric tags.

## Contents

- Logs with FakeLogger
- Metrics with MetricCollector
- Spans with the in-memory exporter
- Integration tests and the distro

## Logs with FakeLogger

Package: `Microsoft.Extensions.Diagnostics.Testing` 10.11.

Get the logger from a service provider that has the same redaction as the app. A bare `new FakeLogger<T>()` does not run the redaction, and it shows classified values as `(null)`.

```csharp
var services = new ServiceCollection();
services.AddLogging(logging => logging.EnableRedaction().AddFakeLogging());
services.AddRedaction(redaction =>
    redaction.SetRedactor<ErasingRedactor>(new DataClassificationSet(DataTaxonomy.Personal)));
await using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<OrderService>>();

OrderLog.OrderCreated(logger, orderId, customerId, "jan@example.nl");

var record = provider.GetFakeLogCollector().LatestRecord;
Assert.AreEqual(1001, record.Id.Id);
Assert.AreEqual(LogLevel.Information, record.Level);
Assert.AreEqual(orderId.ToString(), record.GetStructuredStateValue("OrderId"));
Assert.IsFalse(record.Message.Contains("jan@example.nl", StringComparison.Ordinal));
```

In a `WebApplicationFactory` test, add `services.AddFakeLogging()` in `ConfigureTestServices` and read `FakeLogCollector` from `factory.Services`.

## Metrics with MetricCollector

```csharp
using var meterFactory = new TestMeterFactory(); // or factory.Services.GetRequiredService<IMeterFactory>()
var telemetry = new OrdersTelemetry(meterFactory);
using var collector = new MetricCollector<long>(meterFactory, OrdersTelemetry.Name, "orders.created");

telemetry.OrderCreated("webshop");

var measurement = collector.GetMeasurementSnapshot().Single();
Assert.AreEqual(1, measurement.Value);
Assert.AreEqual("webshop", measurement.Tags["orders.channel"]);
Assert.IsFalse(measurement.Tags.ContainsKey("customer.id"));
```

`TestMeterFactory` is a small `IMeterFactory` in the test project that creates and disposes `Meter` instances. With a host, use the `IMeterFactory` from its services.

## Spans with the in-memory exporter

Package: `OpenTelemetry.Exporter.InMemory` 1.20.

```csharp
var spans = new List<Activity>();
using var tracer = Sdk.CreateTracerProviderBuilder()
    .AddSource(OrdersTelemetry.Name)
    .AddInMemoryExporter(spans)
    .Build();
```

Run the code, then assert on `spans`: the name, the status, and the tags (IDs are allowed on spans).

## Integration tests and the distro

- Do not set `APPLICATIONINSIGHTS_CONNECTION_STRING` in tests. The app registers the distro only when the value is set (rule 3 of `SKILL.md`), so the tests start without network access.
- A test that must start the distro sets a dummy connection string with an unreachable endpoint: `InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9/`.
