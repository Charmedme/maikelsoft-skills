# Sources

Checked on 2026-10-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-19 | Source |
|------|-----------------------|--------|
| OpenTelemetry .NET (SDK, Hosting, OTLP, InMemory, ASP.NET Core and Http instrumentation) | 1.20.0 | [nuget.org OpenTelemetry](https://www.nuget.org/packages/OpenTelemetry) |
| Microsoft.OpenTelemetry (Microsoft OpenTelemetry Distro) | 1.2.0 | [nuget.org](https://www.nuget.org/packages/Microsoft.OpenTelemetry) |
| Azure.Monitor.OpenTelemetry.AspNetCore | 1.6.0 | [nuget.org](https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.AspNetCore) |
| Azure.Monitor.OpenTelemetry.Exporter | 1.10.0 | [nuget.org](https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.Exporter) |
| Microsoft.ApplicationInsights.AspNetCore | 3.2.0 | [nuget.org](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore) |
| Microsoft.Extensions.Logging.ApplicationInsights | 2.23.0, the last version | [nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights) |
| Microsoft.Extensions.Telemetry, Compliance.Redaction, Diagnostics.Testing | 10.11.0 | [nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.Telemetry) |
| Serilog, Serilog.AspNetCore, Serilog.Settings.Configuration | 4.4.0, 10.0.0, 10.0.1 | [nuget.org](https://www.nuget.org/packages/Serilog.AspNetCore) |
| Serilog.Sinks.OpenTelemetry | 4.2.0 | [nuget.org](https://www.nuget.org/packages/Serilog.Sinks.OpenTelemetry) |
| Serilog.Sinks.ApplicationInsights | 5.0.1, needs Microsoft.ApplicationInsights below 3.0 | [nuget.org](https://www.nuget.org/packages/Serilog.Sinks.ApplicationInsights) |

## Documentation

- [Enable Azure Monitor OpenTelemetry for .NET](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable): the Microsoft OpenTelemetry Distro for new apps, no action for existing users of the Azure Monitor distro.
- [Sampling in Azure Monitor OpenTelemetry](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-sampling): the default of 5 traces per second, `SamplingRatio`, `TracesPerSecond`, trace-based log sampling.
- [Connection strings](https://learn.microsoft.com/azure/azure-monitor/app/connection-strings): the end of instrumentation key ingestion on 2025-03-31.
- [Logging in .NET](https://learn.microsoft.com/dotnet/core/extensions/logging) and [compile-time logging source generation](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator).
- [Data redaction in .NET](https://learn.microsoft.com/dotnet/core/extensions/data-redaction): taxonomy, `EnableRedaction`, HMAC redactor.
- [Code analysis rules CA1848, CA1873, CA2254, CA2017, CA2253, CA1727](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/).
- [.NET metrics: instrumentation and testing](https://learn.microsoft.com/dotnet/core/diagnostics/metrics-instrumentation): `IMeterFactory`, `MetricCollector<T>`.
- [OpenTelemetry .NET logs and traces best practices](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs): `UseOtlpExporter`, scopes off by default.
- [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/): attribute names and cardinality.
- [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore): `AddSerilog`, `writeToProviders`.

## Checked by compile and run

With SDK 10.0.402 on 2026-10-19:

- Each C# example in `SKILL.md`, `appinsights.md`, `redaction.md`, `serilog.md`, and `testing.md` compiles with `TreatWarningsAsErrors` and `AnalysisLevel` `latest-recommended`.
- `UseMicrosoftOpenTelemetry` without a connection string: `InvalidOperationException` at host start (ran).
- `UseAzureMonitor` plus `UseMicrosoftOpenTelemetry`: `NotSupportedException: Multiple calls to UseAzureMonitorExporter on the same IServiceCollection are not supported` (ran).
- `ConfigureOpenTelemetryTracerProvider(t => t.AddSource(name))` with the distro: `HasListeners()` is true (ran).
- CA1873 warns on `LogTrace`, `LogDebug`, and `LogInformation` calls with arguments (also a string argument), not on `LogWarning` or `LogError`, with CA1848 off (compiled).
- `ErasingRedactor` gives "Customer  signed in". The HMAC redactor gives `1:<hash>`. `AddSerilog` changes the factory to `SerilogLoggerFactory` and skips redaction (ran).
- A bare `new FakeLogger<T>()` shows classified values as `(null)`. Through a provider with `EnableRedaction().AddFakeLogging()`, the values are redacted (ran).
- `TelemetryConfiguration.Active` does not exist in Microsoft.ApplicationInsights 3.2.0 (CS0117).
- `using Serilog;` with `ILogger` in the same file: CS0104 (compiled).
- `HasAzureMonitorConnectionString` is not public (CS1061).

## Credits

These sources gave ideas. The text is a paraphrase. The plugin `CREDITS.md` lists them.

- [OpenTelemetry .NET documentation](https://github.com/open-telemetry/opentelemetry-dotnet) (Apache-2.0): the logging do and do-not list.
- [aaronontheweb/dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) `opentelemetry-net-instrumentation` (MIT): low-cardinality names, checks on listeners.
- [microsoft/azure-skills](https://github.com/microsoft/azure-skills) `appinsights-instrumentation` (MIT): the connection string from the environment.
