---
name: dotnet-observability
description: Logs, traces, and metrics in .NET code with ILogger, LoggerMessage, OpenTelemetry, and Azure Monitor (Application Insights). Use when code or a review touches ILogger, log levels, message templates, LoggerMessage, BeginScope, personal data or secrets in logs, log redaction, ActivitySource and spans, Meter, IMeterFactory, counters, OpenTelemetry setup (AddOpenTelemetry, UseOtlpExporter), Application Insights (connection string, UseMicrosoftOpenTelemetry, UseAzureMonitor, TelemetryClient), Serilog, trace sampling, or tests with FakeLogger and MetricCollector. Not for the middleware order or exception handlers (dev-stack:aspnetcore), an Aspire ServiceDefaults project (aspire), or dashboards and alert rules in a monitoring tool.
metadata:
  verified: "2026-10-19"
  versions: ".NET 10, OpenTelemetry .NET 1.20, Microsoft.OpenTelemetry 1.2, Azure.Monitor.OpenTelemetry.AspNetCore 1.6, Microsoft.Extensions.Telemetry and Compliance.Redaction 10.11, Serilog 4.4 with Serilog.AspNetCore 10.0"
---

# .NET observability

Logs, traces, and metrics in application code, and the telemetry setup in `Program.cs`. Middleware and exception handlers: `dev-stack:aspnetcore`. A project with an Aspire ServiceDefaults project already wires OpenTelemetry there: use the `aspire` skill for that file.

## First

1. Read `references/stack-rules.md`.
2. Find the telemetry packages in `*.csproj` or `Directory.Packages.props`:
   - `Microsoft.OpenTelemetry` (the Microsoft OpenTelemetry Distro, `UseMicrosoftOpenTelemetry`) or `Azure.Monitor.OpenTelemetry.AspNetCore` (`UseAzureMonitor`): use this file.
   - `Microsoft.ApplicationInsights.AspNetCore` 2.x, `Microsoft.Extensions.Logging.ApplicationInsights`, or `TelemetryConfiguration.Active`: read `references/appinsights.md` before you change code.
   - `Serilog.AspNetCore`: read `references/serilog.md`.
   - Only `OpenTelemetry.*` packages: use this file.
3. Read `.editorconfig` and `Directory.Build.props` for the log analyzer settings (CA1848, CA1873, `AnalysisLevel`).

Done when you can name the telemetry client, the logging pipeline (MEL or Serilog), and the CA1848 setting.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Backend | Azure Monitor (Application Insights) on Azure. Grafana stack elsewhere. Code uses OpenTelemetry APIs only. | OPS-18 |
| Azure Monitor client | Microsoft OpenTelemetry Distro for a new app. Keep `UseAzureMonitor` where it exists. | OPS-24 |
| Pipeline | Microsoft.Extensions.Logging plus OpenTelemetry. Serilog only where it exists or on request. | OPS-23 |
| Log style | `[LoggerMessage]` in services and libraries. The project setting wins. | OPS-23 |
| Log format | OTLP export, JSON console in containers (`Logging__Console__FormatterName=Json`). | OPS-19 |
| Sampling | Keep all traces, set in configuration. Ratio sampling above about 100 requests per second. | OPS-20 |
| Personal data | Classify and redact. HMAC when support must search by the value. | SEC-14 |

Existing code keeps its style (stack rule 3).

## Rules

### Pipeline and client

1. Keep one logging pipeline: Microsoft.Extensions.Logging (MEL) plus OpenTelemetry. `AddSerilog()` replaces the `ILoggerFactory`. Then other logger providers (also the provider of the Azure Monitor distro) get nothing unless `writeToProviders: true`, and compliance redaction stops.
2. New app on Azure: call `UseMicrosoftOpenTelemetry` with `ExportTarget.AzureMonitor`. Keep `UseAzureMonitor` where it exists. Use one distro: `UseAzureMonitor` plus `UseMicrosoftOpenTelemetry` stops the host with `NotSupportedException` ("Multiple calls to UseAzureMonitorExporter").
3. Register the distro only when the connection string is set. Without `APPLICATIONINSIGHTS_CONNECTION_STRING` it throws `InvalidOperationException` at host start, so local runs and `WebApplicationFactory` tests fail. The package has no public guard method: read the configuration value.
4. Read the connection string from configuration (`APPLICATIONINSIGHTS_CONNECTION_STRING`, from the environment or a secret store). Do not use an instrumentation key: key ingestion is out of support since 2025-03-31.
5. Set sampling in code or configuration (OPS-20). The distro samples 5 traces per second by default, and its trace-based log sampler drops the logs of unsampled traces. `SamplingRatio = 1.0F` with `TracesPerSecond = null` keeps all.
6. Register each own `ActivitySource` and `Meter` by name. With the distro, call `ConfigureOpenTelemetryTracerProvider` and `ConfigureOpenTelemetryMeterProvider`. Without a registration, `StartActivity` returns null and the counter records nothing.

```csharp
// Program.cs. Packages: Microsoft.OpenTelemetry, Microsoft.Extensions.Telemetry, Microsoft.Extensions.Compliance.Redaction.
builder.Logging.EnableRedaction();
builder.Services.AddRedaction(redaction => redaction.SetHmacRedactor(
    options =>
    {
        options.KeyId = 1;
        options.Key = builder.Configuration["Redaction:HmacKey"]!; // base64, from the secret store
    },
    new DataClassificationSet(DataTaxonomy.Personal)));

// The distro throws at start without a connection string (local runs, tests).
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource(Telemetry.Name));
    builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddMeter(Telemetry.Name));
    builder.UseMicrosoftOpenTelemetry(options =>
    {
        options.Exporters = ExportTarget.AzureMonitor;
        options.AzureMonitor.SamplingRatio = 1.0F; // OPS-20: keep all traces
        options.AzureMonitor.TracesPerSecond = null;
    });
}
```

Without Azure, use the OpenTelemetry SDK with `.UseOtlpExporter()` one time for all signals. Example and options: `references/appinsights.md`.

### Log calls

7. Use a message template with PascalCase placeholders. Do not use string interpolation or concatenation in a log call. The analyzers report it: CA2254 (template varies), CA2017 (placeholder count), CA2253 (numeric placeholder), CA1727 (placeholder case).
8. Write new log calls as `[LoggerMessage]` methods with a unique `EventId` per class (OPS-23). CA1873 warns on each `LogTrace`, `LogDebug`, or `LogInformation` call that passes arguments, also when the project turns CA1848 off. With `-warnaserror` the build then fails. Existing code keeps its style.
9. Pass the exception as the exception argument, not `ex.Message`. Log a failure one time, at the boundary that handles it. A log and a rethrow give two entries for one failure.
10. Do not log secrets, tokens, connection strings, full bodies, or headers. Classify personal data with a `DataClassificationAttribute` (for example `[PersonalData] string email`), and turn on `EnableRedaction` with `AddRedaction`. Use the HMAC redactor when support must search by the value: the log then holds `<key id>:<hash>`. Redaction works only in the MEL logger factory.
11. With the OpenTelemetry log exporter, set `IncludeScopes = true` when the code depends on `BeginScope`. OpenTelemetry drops scopes by default.

```csharp
internal static partial class OrderLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Order {OrderId} created for customer {CustomerId} ({CustomerEmail})")]
    public static partial void OrderCreated(ILogger logger, Guid orderId, Guid customerId, [PersonalData] string? customerEmail);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Creating an order for customer {CustomerId} failed")]
    public static partial void CreateFailed(ILogger logger, Exception exception, Guid customerId);
}
```

Taxonomy, attributes, the HMAC key, and security events: `references/redaction.md`.

### Traces and metrics

12. Use one `static readonly ActivitySource` per component. Use `activity?.` on the result of `StartActivity`: it is null when no listener samples the trace.
13. Create meters through `IMeterFactory` (`meterFactory.Create(name)`), not with `new Meter(...)`. The factory owns the meter and disposes it, and tests can collect its measurements.
14. Keep metric tags low-cardinality: a status, a channel, a region, a kind. Put no customer ID, email, or order ID on a metric. Each value makes a new time series, and the tag puts personal data in metrics. Put IDs on spans and logs. A count per customer is a query on logs or spans.
15. Add `AddConsoleExporter()` only in development. In production it writes each span and metric to stdout.

```csharp
public sealed class OrdersTelemetry
{
    public const string Name = "Orders.Api";
    public static readonly ActivitySource Source = new(Name);
    private readonly Counter<long> _created;

    public OrdersTelemetry(IMeterFactory meterFactory) =>
        _created = meterFactory.Create(Name).CreateCounter<long>("orders.created", unit: "{order}");

    public void OrderCreated(string channel) =>
        _created.Add(1, new KeyValuePair<string, object?>("orders.channel", channel));
}
```

### Tests

16. Test log output with `FakeLogger` (package `Microsoft.Extensions.Diagnostics.Testing`). Assert the level, the `EventId`, and the structured values. Assert that the record holds no raw personal data. Get the logger from a service provider with `EnableRedaction()` and `AddFakeLogging()`: a bare `new FakeLogger<T>()` shows classified values as `(null)`. Test metrics with `MetricCollector<T>`. Keep the distro off in tests (rule 3).

Examples: `references/testing.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `services.AddApplicationInsightsTelemetry("<key>")`, `"InstrumentationKey": "..."` | Connection string from `APPLICATIONINSIGHTS_CONNECTION_STRING`, `UseMicrosoftOpenTelemetry` for a new app |
| `new TelemetryClient(TelemetryConfiguration.Active)` | `TelemetryClient` from DI (SDK 3.x), or an `ActivitySource` and a `Meter` |
| `builder.Logging.AddApplicationInsights();` | Nothing: the distro collects `ILogger` logs |
| `builder.UseMicrosoftOpenTelemetry(...)` without a check | Register it only when the connection string is set |
| `.UseAzureMonitor()` and `UseMicrosoftOpenTelemetry(...)` together | One distro |
| `builder.Host.UseSerilog(...)` | MEL plus OpenTelemetry. Where Serilog stays: `builder.Services.AddSerilog(..., writeToProviders: true)` |
| `.WriteTo.ApplicationInsights(TelemetryConfiguration.Active, ...)` | `AddSerilog(..., writeToProviders: true)` plus the distro, or MEL only |
| `logger.LogInformation($"Order {id} shipped")` | `[LoggerMessage(EventId = 1001, Message = "Order {OrderId} shipped")]` |
| `logger.LogError("Failed: " + ex.Message)` | `OrderLog.CreateFailed(logger, ex, customerId)` |
| `private readonly Meter _meter = new("Orders");` | `meterFactory.Create("Orders.Api")` |
| `_created.Add(1, new("customer.id", customerId))` | `_created.Add(1, new("orders.channel", channel))` |
| `EnableAdaptiveSampling = false` | `SamplingRatio` and `TracesPerSecond` |
| `.AddConsoleExporter()` in all environments | `.UseOtlpExporter()`, console only in development |
| `new FakeLogger<T>()` to test classified values | A logger from a provider with `EnableRedaction()` and `AddFakeLogging()` |

## Check

1. Build with the analyzers as errors:

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings. With `AnalysisLevel` `latest-recommended`, CA2254, CA2017, CA2253, CA1727, and CA1873 run. CA1848 runs where the project keeps it on.

2. List the telemetry packages:

   ```
   dotnet package list --include-transitive
   ```

   Pass: no `Microsoft.Extensions.Logging.ApplicationInsights`, no `Microsoft.ApplicationInsights` below 3.0, no `Serilog.Sinks.ApplicationInsights` next to SDK 3.x, and one distro.

3. Run the tests without `APPLICATIONINSIGHTS_CONNECTION_STRING`: `dotnet test`. Pass: all green. A `FakeLogger` test proves that the raw personal data is not in the log.

4. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

## Review checklist

The full list is in `references/review-checklist.md`.

1. One logging pipeline. Serilog only with `writeToProviders: true` or without other providers. tool (OBS-USE-SERILOG), judge
2. One distro, registered only when the connection string is set. tool (OBS-TWO-DISTROS), judge
3. No instrumentation key, no classic Application Insights API. tool (OBS-IKEY, OBS-AI-CLASSIC, OBS-PKG-MEL-AI, OBS-PKG-AI-2X)
4. Sampling is explicit (OPS-20). judge
5. Own sources and meters are registered by name. judge
6. Message templates only, no interpolation. tool (CA2254, CA2017, CA2253, CA1727)
7. New log calls are `[LoggerMessage]` methods or follow the project style. tool (CA1873, CA1848), judge
8. The exception is the exception argument. Each failure is logged one time. judge
9. No secrets or raw personal data in logs. Personal data is classified and redacted with the MEL factory. judge
10. Metric tags are low-cardinality. No IDs or emails on metrics. judge
11. Meters come from `IMeterFactory`. tool (OBS-NEW-METER)
12. No console exporter in production. tool (OBS-CONSOLE-EXPORTER)
13. `StartActivity` results use `activity?.`. tool (CS8602)
14. Tests prove the log and metric behavior with `FakeLogger` and `MetricCollector<T>`. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/decisions.md`: read before new code needs an OPS-18, OPS-19, OPS-20, OPS-23, or OPS-24 decision. It has the question, the options, and the recommendation.
- `references/appinsights.md`: read when the project uses Application Insights, when you choose between the distros, or when you migrate from SDK 2.x or an instrumentation key. It also has the OTLP setup.
- `references/redaction.md`: read before you log a value that can be personal data or a secret, or a security event.
- `references/serilog.md`: read when the project has Serilog or the human asks for it.
- `references/testing.md`: read before you write a test for logs, spans, or metrics.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language. Output follows `references/language.md`.
- `scripts/stackcheck.py`: the rule check of this skill.
