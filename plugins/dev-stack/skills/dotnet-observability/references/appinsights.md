# Application Insights and OTLP

The client packages for Application Insights, the migration from the old SDK, and the OTLP setup without Azure.

## Contents

- Which client
- The Microsoft OpenTelemetry Distro
- The Azure Monitor distro
- Log scopes with a distro
- Migration from SDK 2.x or an instrumentation key
- OTLP without Azure

## Which client

| Package | Entry point | Use |
|---------|-------------|-----|
| `Microsoft.OpenTelemetry` 1.2 | `builder.UseMicrosoftOpenTelemetry(...)` | New app (OPS-24). Learn recommends it on each Azure Monitor page. |
| `Azure.Monitor.OpenTelemetry.AspNetCore` 1.6 | `builder.Services.AddOpenTelemetry().UseAzureMonitor()` | Existing apps. Learn: no action needed. |
| `Microsoft.ApplicationInsights.AspNetCore` 3.x | `AddApplicationInsightsTelemetry()` | Apps that call `TelemetryClient`. SDK 3.x runs on OpenTelemetry. |
| `Microsoft.ApplicationInsights.AspNetCore` 2.x | | Out of support for new work. Migrate. |
| `Microsoft.Extensions.Logging.ApplicationInsights` | `AddApplicationInsights()` | Last version 2.23, no 3.x. Remove it: the distros collect `ILogger` logs. |

Use one distro per app. The second call stops the host: `NotSupportedException: Multiple calls to UseAzureMonitorExporter on the same IServiceCollection are not supported.`

## The Microsoft OpenTelemetry Distro

- Reads the connection string from `APPLICATIONINSIGHTS_CONNECTION_STRING` in configuration or the environment, or from `options.AzureMonitor.ConnectionString`.
- Throws `InvalidOperationException` ("A connection string was not found") at host start when no connection string is set. Check the configuration value before the call. The package has no public guard method.
- Adds ASP.NET Core, HttpClient, and SqlClient instrumentation, live metrics, and the `ILogger` export.
- Sampling: `options.AzureMonitor.SamplingRatio` (0.0 to 1.0, default 1.0) and `options.AzureMonitor.TracesPerSecond` (default 5). When both have a value, `TracesPerSecond` wins. For a ratio, set `TracesPerSecond = null` too. `EnableTraceBasedLogsSampler` (on by default) exports only the logs of sampled traces. Logs without a trace are always exported.
- Caveat for version 1.2.0: Learn says that the default keeps 5 traces per second. But checked by run: the distro exported all 50 of 50 spans to Azure Monitor with the default, with `TracesPerSecond = 1.0`, and with `SamplingRatio = 0.1F` plus `TracesPerSecond = null`. The cause in the source: `UseMicrosoftOpenTelemetry` calls `UseAgent365` after `UseAzureMonitor`, and `UseAgent365` sets a `ParentBasedSampler` with `AlwaysOnSampler`, which replaces the rate-limited sampler. Still write the explicit settings (OPS-20), so the code is correct when the distro honors them. Do not promise lower volume from a ratio. After a deploy, check the retained percentage with the `itemCount` query of the Learn sampling page.
- Own sources and meters: `builder.Services.ConfigureOpenTelemetryTracerProvider(t => t.AddSource(name))` and `ConfigureOpenTelemetryMeterProvider(m => m.AddMeter(name))`. Checked: with this registration, `ActivitySource.HasListeners()` is true after the host starts.
- Other targets: `options.Exporters = ExportTarget.AzureMonitor | ExportTarget.Otlp`.

## The Azure Monitor distro

The same rules apply: one call, a connection string from configuration, explicit sampling (`SamplingRatio`, `TracesPerSecond` on `AzureMonitorOptions`). Keep it in an existing app. A move to the Microsoft distro is a separate change with its own test run.

- Version 1.5.0 (2026-04-30) changed the default sampler from 100 % to `RateLimitedSampler` with 5 traces per second. Code that set only `SamplingRatio = 1.0F` before 1.5.0 kept all traces. After the upgrade it keeps 5 traces per second. `SamplingRatio` already defaults to 1.0, so that line does nothing. Checked by run with 1.6.0: `SamplingRatio = 1.0F` alone recorded 2 of 50 root spans, and with `TracesPerSecond = null` 50 of 50.
- The fix: `options.TracesPerSecond = null;` next to `SamplingRatio`, or the environment variables `OTEL_TRACES_SAMPLER=microsoft.fixed_percentage` and `OTEL_TRACES_SAMPLER_ARG=1.0`.
- `EnableTraceBasedLogsSampler` is on by default, so the logs of the dropped traces are lost too.

## Log scopes with a distro

Both distros support log scopes, but OpenTelemetry turns them off by default (`OpenTelemetryLoggerOptions.IncludeScopes` is false). Without the setting, no `BeginScope` value reaches `customDimensions`:

```csharp
builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.IncludeScopes = true);
```

- Checked by run with Microsoft.OpenTelemetry 1.2.0: without the setting, the exported log record has no scope value. With it, the scope value is on the record.
- With duplicate keys, the outermost scope wins. A key in the message template wins over the same key in a scope.

## Migration from SDK 2.x or an instrumentation key

1. Replace `"ApplicationInsights": { "InstrumentationKey": ... }` and `InstrumentationKey = ...` with a connection string in `APPLICATIONINSIGHTS_CONNECTION_STRING`. Key ingestion is out of support since 2025-03-31.
2. Remove `Microsoft.Extensions.Logging.ApplicationInsights` and `builder.Logging.AddApplicationInsights()`.
3. Replace `TelemetryConfiguration.Active` (removed in SDK 3.x) with DI: inject `TelemetryClient`, or use `TelemetryConfiguration.CreateDefault()` outside DI.
4. Replace `TrackEvent` and `TrackMetric` calls in new code with an `ActivitySource` and a `Meter`. Keep existing calls on SDK 3.x.
5. Replace `EnableAdaptiveSampling` with `SamplingRatio` or `TracesPerSecond`.
6. Serilog with `Serilog.Sinks.ApplicationInsights`: version 5.0.1 needs `Microsoft.ApplicationInsights` below 3.0. Move to `AddSerilog(..., writeToProviders: true)` plus the distro, or to MEL (`serilog.md`).

## OTLP without Azure

Register the OpenTelemetry SDK one time, with one exporter for all signals:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("orders-api"))
    .WithTracing(tracing => tracing
        .AddSource(Telemetry.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter(Telemetry.Name)
        .AddAspNetCoreInstrumentation())
    .WithLogging(configureBuilder: null, configureOptions: options => options.IncludeScopes = true)
    .UseOtlpExporter();
```

- Packages: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http` (1.20.1).
- The endpoint comes from `OTEL_EXPORTER_OTLP_ENDPOINT`. Do not put it in code.
- `UseOtlpExporter()` replaces `AddOtlpExporter()` on each signal. Call it one time.
- Sampling: `tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(ratio)))`, or the environment variables `OTEL_TRACES_SAMPLER` and `OTEL_TRACES_SAMPLER_ARG`.
