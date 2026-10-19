# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Pipeline and client
- Log calls
- Personal data and secrets
- Traces and metrics
- Tests
- Decisions

## Pipeline and client

| # | Check | Type |
|---|-------|------|
| 1 | One logging pipeline. Serilog only where it exists, with `writeToProviders: true` when a distro or another provider must get the logs. | tool (OBS-USE-SERILOG), judge |
| 2 | One distro: not `UseAzureMonitor` and `UseMicrosoftOpenTelemetry` together (the host does not start). | tool (OBS-TWO-DISTROS), judge |
| 3 | The distro is registered only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set, so tests and local runs start. | judge |
| 4 | No instrumentation key, in code or in `appsettings*.json`. | tool (OBS-IKEY) |
| 5 | No classic API: `TelemetryConfiguration.Active`, `AddApplicationInsights()` on logging, `WriteTo.ApplicationInsights`. | tool (OBS-AI-CLASSIC) |
| 6 | No `Microsoft.Extensions.Logging.ApplicationInsights`, no SDK 2.x, no `Serilog.Sinks.ApplicationInsights`. | tool (OBS-PKG-MEL-AI, OBS-PKG-AI-2X, OBS-PKG-SERILOG-AI-SINK) |
| 7 | Sampling is set in code or configuration (OPS-20). | judge |
| 8 | Own sources and meters are registered by name (`AddSource`, `AddMeter`, or the `ConfigureOpenTelemetry...Provider` calls). | judge |
| 9 | One OTLP exporter call (`UseOtlpExporter`), no endpoint in code. | judge |
| 10 | No console exporter outside development. | tool (OBS-CONSOLE-EXPORTER) |

## Log calls

| # | Check | Type |
|---|-------|------|
| 11 | Message templates only: no interpolation or concatenation in a log call. | tool (CA2254, CA2017, CA2253, CA1727) |
| 12 | New log calls follow the project style: `[LoggerMessage]` methods with unique event IDs, or the existing style. | tool (CA1873, CA1848), judge |
| 13 | The exception is the exception argument, not `ex.Message`. | judge |
| 14 | A failure is logged one time, at the boundary that handles it. No log and rethrow. | judge |
| 15 | Levels fit: Error for a failure that needs action, Warning for a recovered problem, Information for business events, Debug for detail. | judge |
| 16 | `IncludeScopes = true` when the code uses `BeginScope` with the OpenTelemetry log exporter. | judge |

## Personal data and secrets

| # | Check | Type |
|---|-------|------|
| 17 | No secrets, tokens, connection strings, bodies, or headers in a log or a span. | tool (SC-LOG-SECRET in `dev-stack:secure-coding`), judge |
| 18 | Personal data is classified and redacted (`EnableRedaction`, `AddRedaction`), with the MEL logger factory. | judge |
| 19 | A value that support must search is HMAC-redacted, with the key from the secret store. | judge |
| 20 | A claim in a comment or in the pull request ("emails are redacted") matches the code. | judge |
| 21 | Security events have their own `[LoggerMessage]` methods and stable event IDs (SEC-23). | judge |

## Traces and metrics

| # | Check | Type |
|---|-------|------|
| 22 | One `static readonly ActivitySource` per component. `StartActivity` results use `activity?.`. | tool (CS8602), judge |
| 23 | Meters come from `IMeterFactory`. | tool (OBS-NEW-METER) |
| 24 | Metric tags are low-cardinality. No customer ID, email, or order ID on a metric. | judge |
| 25 | Span names are stable (no IDs in the name). IDs go in attributes. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 26 | A `FakeLogger` test checks the event ID and that no raw personal data is in the record. The logger comes from a provider with redaction. | judge |
| 27 | A `MetricCollector<T>` test checks the value and the tags of each new instrument. | judge |
| 28 | The tests start without a connection string. | tool (`dotnet test`) |

## Decisions

| # | Check | Type |
|---|-------|------|
| 29 | The backend (OPS-18), the client (OPS-24), the pipeline (OPS-23), the log format (OPS-19), and sampling (OPS-20) follow `docs/dev-team/decisions.md`, or the report marks them as assumed. | judge |
| 30 | Logging personal data has an accepted SEC-14, or the report asks it. | judge |
