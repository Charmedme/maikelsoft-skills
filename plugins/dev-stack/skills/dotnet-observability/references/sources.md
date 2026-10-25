# Sources

Checked on 2026-10-19, and again on 2026-10-25. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-25 | Source |
|------|-----------------------|--------|
| OpenTelemetry .NET (SDK, Hosting, OTLP, InMemory, ASP.NET Core and Http instrumentation) | 1.20.1 (2026-10-20) | [nuget.org OpenTelemetry](https://www.nuget.org/packages/OpenTelemetry) |
| Microsoft.OpenTelemetry (Microsoft OpenTelemetry Distro) | 1.2.0 | [nuget.org](https://www.nuget.org/packages/Microsoft.OpenTelemetry) |
| Azure.Monitor.OpenTelemetry.AspNetCore | 1.6.0 | [nuget.org](https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.AspNetCore) |
| Azure.Monitor.OpenTelemetry.Exporter | 1.10.0 | [nuget.org](https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.Exporter) |
| Microsoft.ApplicationInsights.AspNetCore | 3.2.0 | [nuget.org](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore) |
| Microsoft.Extensions.Logging.ApplicationInsights | 2.23.0, the last version | [nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights) |
| Microsoft.Extensions.Telemetry, Compliance.Redaction, Diagnostics.Testing | 10.11.0 | [nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.Telemetry) |
| Serilog, Serilog.AspNetCore, Serilog.Settings.Configuration | 4.4.0, 10.0.0, 10.0.1 | [nuget.org](https://www.nuget.org/packages/Serilog.AspNetCore) |
| Serilog.Sinks.OpenTelemetry | 4.2.0 | [nuget.org](https://www.nuget.org/packages/Serilog.Sinks.OpenTelemetry) |
| Serilog.Sinks.ApplicationInsights | 5.0.1, needs Microsoft.ApplicationInsights below 3.0 | [nuget.org](https://www.nuget.org/packages/Serilog.Sinks.ApplicationInsights) |
| .NET 10 runtime and SDK | 10.0.13 and SDK 10.0.402 (2026-10-13) | [release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json) |

All other versions in this table did not change between 2026-10-19 and 2026-10-25 (nuget.org flat container index, stable versions only).

## Documentation

- [Enable Azure Monitor OpenTelemetry for .NET](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable): the Microsoft OpenTelemetry Distro for new apps, no action for existing users of the Azure Monitor distro.
- [AzureMonitorOptions.TracesPerSecond](https://learn.microsoft.com/dotnet/api/azure.monitor.opentelemetry.aspnetcore.azuremonitoroptions.tracespersecond) (Azure.Monitor.OpenTelemetry.AspNetCore 1.6.0, Microsoft.OpenTelemetry): "When both TracesPerSecond and SamplingRatio are specified, TracesPerSecond takes precedence." Checked 2026-10-25.
- [Azure.Monitor.OpenTelemetry.AspNetCore CHANGELOG](https://github.com/Azure/azure-sdk-for-net/blob/Azure.Monitor.OpenTelemetry.AspNetCore_1.6.0/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore/CHANGELOG.md): 1.5.0 (2026-04-30) changed the default sampler to `RateLimitedSampler` with 5 traces per second. [AzureMonitorOptions.cs at 1.6.0](https://github.com/Azure/azure-sdk-for-net/blob/Azure.Monitor.OpenTelemetry.AspNetCore_1.6.0/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore/src/AzureMonitorOptions.cs): `SamplingRatio = 1.0F`, `TracesPerSecond = 5.0`, `EnableTraceBasedLogsSampler = true` as defaults. Checked 2026-10-25.
- [Azure Monitor Distro README 1.6.0, Log Scopes](https://learn.microsoft.com/dotnet/api/overview/azure/monitor.opentelemetry.aspnetcore-readme#examples): scopes are off by default, `Configure<OpenTelemetryLoggerOptions>(o => o.IncludeScopes = true)`. [OpenTelemetryLoggerOptions.cs at core-1.20.1](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.20.1/src/OpenTelemetry/Logs/ILogger/OpenTelemetryLoggerOptions.cs): `IncludeScopes` default false. Checked 2026-10-25.
- [microsoft/opentelemetry-distro-dotnet](https://github.com/microsoft/opentelemetry-distro-dotnet) `MicrosoftOpenTelemetryBuilderExtensions.cs` and `Agent365/Runtime/Agent365OpenTelemetryBuilderExtensions.cs` (main, 2026-10-20): `UseAgent365` runs after `UseAzureMonitor` and calls `SetSampler(new ParentBasedSampler(new AlwaysOnSampler(), ...))`. Checked 2026-10-25.
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

With SDK 10.0.402 on 2026-10-25:

- Azure.Monitor.OpenTelemetry.AspNetCore 1.6.0, 50 root activities in a loop: `SamplingRatio = 1.0F` alone recorded 2 of 50. With `TracesPerSecond = null`, 50 of 50 were recorded (ran).
- Microsoft.OpenTelemetry 1.2.0 with `ExportTarget.AzureMonitor` and a local fake ingestion endpoint: 50 of 50 spans exported with the default, with `TracesPerSecond = 1.0`, and with `SamplingRatio = 0.1F` plus `TracesPerSecond = null`. The tracer provider sampler was `ParentBasedSampler` with root `AlwaysOnSampler` (ran). This disagrees with Learn (default 5 traces per second).
- Microsoft.OpenTelemetry 1.2.0: without `IncludeScopes`, the exported log record has no scope value. With `Configure<OpenTelemetryLoggerOptions>(o => o.IncludeScopes = true)`, it has the value (ran).

## Credits

These sources gave ideas. The text is a paraphrase. The plugin `CREDITS.md` lists them.

- [OpenTelemetry .NET documentation](https://github.com/open-telemetry/opentelemetry-dotnet) (Apache-2.0): the logging do and do-not list.
- [aaronontheweb/dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) `opentelemetry-net-instrumentation` (MIT): low-cardinality names, checks on listeners.
- [microsoft/azure-skills](https://github.com/microsoft/azure-skills) `appinsights-instrumentation` (MIT): the connection string from the environment.
