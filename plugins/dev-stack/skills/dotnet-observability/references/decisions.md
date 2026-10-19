# Observability decisions

The team decisions that logs, traces, and metrics need. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4). SEC-14 (data classification) and SEC-23 (security events) are in `dev-stack:secure-coding`. This skill applies them in `redaction.md`.

## Contents

- OPS-18. Observability backend
- OPS-24. Azure Monitor client for new apps
- OPS-23. Logging pipeline
- OPS-19. Log format and log transport
- OPS-20. Trace sampling

## OPS-18. Observability backend

- Question: Where do the logs, traces, and metrics of this service go?
- Ask when: the first change that adds telemetry, or the first deployment.
- Options: Azure Monitor (Application Insights and Log Analytics): managed, part of the Azure subscription. Grafana stack (Loki, Tempo, Prometheus or Mimir): open source, self-hosted or Grafana Cloud. Another OTLP backend (Elastic, Datadog, Honeycomb): commercial.
- Recommended: Azure Monitor when the service runs on Azure. The Grafana stack elsewhere. Code uses only the OpenTelemetry and `ILogger` APIs, so the backend can change without code changes.
- Choose another option when: the company already runs another backend for all services. Data residency rules exclude the managed service.
- Depends on: none.
- Revisit when: the ingestion cost of Azure Monitor grows faster than the traffic.
- Check: judge.
- Detect: OBS-DET-OPS-18.

## OPS-24. Azure Monitor client for new apps

- Question: Which package sends telemetry to Application Insights in a new app?
- Ask when: OPS-18 is Azure Monitor and the app has no Application Insights client yet.
- Options: Microsoft OpenTelemetry Distro (`Microsoft.OpenTelemetry`, `UseMicrosoftOpenTelemetry`): the package that Microsoft Learn recommends for new apps, version 1.2, also covers OTLP and agent telemetry. Azure Monitor OpenTelemetry Distro (`Azure.Monitor.OpenTelemetry.AspNetCore`, `UseAzureMonitor`): mature, version 1.6, Learn says that existing users need no action. Application Insights SDK 3.x (`Microsoft.ApplicationInsights.AspNetCore`): a classic API on top of OpenTelemetry, for apps that use `TelemetryClient`.
- Recommended: The Microsoft OpenTelemetry Distro for a new app, as Learn recommends. Keep `UseAzureMonitor` where it exists. Use one distro per app.
- Choose another option when: the app depends on `TelemetryClient` calls (`TrackEvent`): SDK 3.x. A platform team pins the Azure Monitor distro for all services: keep it.
- Depends on: OPS-18.
- Revisit when: the Microsoft distro reaches a new major version, or Learn changes its recommendation.
- Check: tool (OBS-IKEY, OBS-AI-CLASSIC), judge for two distros.
- Detect: OBS-DET-OPS-24.

## OPS-23. Logging pipeline

- Question: Do we log through Microsoft.Extensions.Logging with OpenTelemetry, or through Serilog?
- Ask when: the first change that configures logging.
- Options: MEL plus OpenTelemetry: built in, works with the distro, compliance redaction, and `FakeLogger`. Serilog: rich sinks and enrichers, but it replaces the logger factory. MEL with a Serilog sink: Serilog formats, MEL stays the factory.
- Recommended: MEL plus OpenTelemetry. Serilog only where it exists or when the human asks for it. New log calls are `[LoggerMessage]` methods in services and libraries. The project setting for CA1848 wins.
- Choose another option when: the project already uses Serilog sinks that OpenTelemetry cannot replace. Then keep Serilog with `writeToProviders: true` and read `serilog.md`.
- Depends on: OPS-18.
- Revisit when: Serilog stops passing events to other providers, or redaction must work in Serilog.
- Check: tool (OBS-USE-SERILOG, CA1873, CA1848), judge.
- Detect: OBS-DET-OPS-23.

## OPS-19. Log format and log transport

- Question: In which format do logs leave the service, and how?
- Ask when: the first deployment to a container platform.
- Options: OTLP export plus a JSON console in containers: structured, the collector or the distro ships them. Plain text console: readable, but not structured. Files: only for a host without a log agent.
- Recommended: OTLP export (or the distro), plus the JSON console formatter in containers (`Logging__Console__FormatterName=Json`). Use the simple console formatter in development.
- Choose another option when: the platform log agent reads only text, or the platform needs a fixed JSON schema.
- Depends on: OPS-18, OPS-23.
- Revisit when: the platform changes its log agent.
- Check: judge.
- Detect: OBS-DET-OPS-19.

## OPS-20. Trace sampling

- Question: Which part of the traces do we keep?
- Ask when: the first change that exports traces.
- Options: Keep all traces: complete data, the highest ingestion cost. Ratio sampling (`SamplingRatio`, or `TraceIdRatioBasedSampler` with a parent-based sampler): a fixed share. Rate-limited sampling (`TracesPerSecond`, the default of the Microsoft distro: 5 per second): a fixed cost.
- Recommended: Keep all traces, set in configuration, so that operations can change it without a release. Use ratio sampling above about 100 requests per second. With the Microsoft distro, set `SamplingRatio = 1.0F` and `TracesPerSecond = null`, because the default silently keeps 5 traces per second and its trace-based log sampler drops the logs of the other traces.
- Choose another option when: the ingestion cost is too high at the current traffic.
- Depends on: OPS-18.
- Revisit when: the traffic passes about 100 requests per second, or the monthly ingestion cost grows.
- Check: judge (the setting is in code or `appsettings*.json`).
- Detect: OBS-DET-OPS-20.
