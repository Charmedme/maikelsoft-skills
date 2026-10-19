# Serilog

Serilog is an option, not the default (OPS-23). Read this file when the project already has Serilog, or when the human asks for it.

## Contents

- What Serilog changes
- Setup where Serilog stays
- Application Insights with Serilog
- Move from Serilog to MEL

## What Serilog changes

- `builder.Services.AddSerilog(...)` replaces the `ILoggerFactory` with `SerilogLoggerFactory`. Checked: the factory type changes from `ExtendedLoggerFactory` to `SerilogLoggerFactory`.
- Other logger providers (the OpenTelemetry provider of a distro, a test provider) get nothing, unless you pass `writeToProviders: true`.
- Compliance redaction (`EnableRedaction`) does not run. A classified value goes to the Serilog sinks as it is.
- `using Serilog;` and `Microsoft.Extensions.Logging` in one file make `ILogger` ambiguous (CS0104). Keep the Serilog setup in its own file, or write `Microsoft.Extensions.Logging.ILogger`.

## Setup where Serilog stays

```csharp
builder.Services.AddSerilog(
    (services, configuration) => configuration.ReadFrom.Configuration(builder.Configuration),
    writeToProviders: true);
```

- Use `builder.Services.AddSerilog(...)`, not `builder.Host.UseSerilog(...)`. The service collection form is the current API of Serilog.AspNetCore 10.
- `writeToProviders: true` passes each event to the MEL providers too, so the distro still exports logs.
- Read the levels and sinks from configuration (`Serilog.Settings.Configuration`).
- Keep the redaction rule by hand: do not pass personal data to a Serilog log call. Compliance redaction does not help here.

## Application Insights with Serilog

- `Serilog.Sinks.ApplicationInsights` 5.0.1 needs `Microsoft.ApplicationInsights` below 3.0, and its examples use `TelemetryConfiguration.Active`, which SDK 3.x removed (CS0117). Do not add it.
- Use `writeToProviders: true` with the distro, or `Serilog.Sinks.OpenTelemetry` (4.2) to an OTLP endpoint.

## Move from Serilog to MEL

A move is a separate change. Do it only when the task asks for it.

1. Replace the Serilog sinks with OpenTelemetry exporters or the distro.
2. Replace `Log.ForContext<T>()` and static `Log.Information(...)` with an injected `ILogger<T>`.
3. Replace enrichers with OpenTelemetry resource attributes or `BeginScope` (set `IncludeScopes = true`).
4. Remove `AddSerilog`, `UseSerilogRequestLogging`, and the Serilog packages.
5. Run the tests. Check that a log test with `FakeLogger` still sees each event.
