# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Install and project form
- Resources and start order
- Parameters and secrets
- ServiceDefaults and services
- Agents and dashboard
- Tests
- Publish and deploy
- Severity
- Do not flag

## Install and project form

| # | Check | Type |
|---|-------|------|
| 1 | The AppHost form matches AHS-01. A new AppHost uses `Microsoft.NET.Sdk`, `OutputType` `Exe`, `Aspire.Hosting.AppHost`, and `Aspire.Hosting.Dotnet`. | tool (AHS-OLD-SDK, ASPIRE012) |
| 2 | An AppHost with `Aspire.AppHost.Sdk/17.0.0` is a half-done move. The change does the steps of `upgrade-17.md`, or lists each open step under "Open points". | tool (AHS-OLD-SDK), judge |
| 3 | A 17 AppHost uses `AddDotnetProject` with a path. No `AddProject<>`, no `AddCSharpApp`, and no `ProjectReference` to a service. | tool (AHS-ADD-PROJECT, CS0618, CS0234) |
| 4 | Each `Aspire.*` package has the same fixed version. No `*` and no 13.x package in a 17 AppHost. | judge |
| 5 | No `dotnet workload install aspire` in a README, a script, a Dockerfile, or a pipeline. | tool (AHS-WORKLOAD) |
| 6 | No `AspireUseCliBundle` set to `false`. Each CI job that builds the AppHost installs the Aspire CLI or caches `~/.aspire`. | judge |

## Resources and start order

| # | Check | Type |
|---|-------|------|
| 7 | Each `WithReference` to a database, a cache, a broker, or a service has a `WaitFor` for the same resource. | judge |
| 8 | Each HTTP service has `WithHttpHealthCheck`. Only the services that users open have `WithExternalHttpEndpoints()`. | judge |
| 9 | Resource names are lowercase and match the connection names in the services. | judge |
| 10 | Databases and brokers follow AHS-03 (`WithDataVolume()`). | judge |
| 11 | The image tag matches the production major version, or the default is on purpose. | judge |

## Parameters and secrets

| # | Check | Type |
|---|-------|------|
| 12 | Each secret is `AddParameter(name, secret: true)`. No secret value is in `appsettings*.json` or in code. | judge |
| 13 | No required parameter has the value `""`. A value that can be missing uses `AsOptional()`. | judge |

## ServiceDefaults and services

| # | Check | Type |
|---|-------|------|
| 14 | Each service calls `AddServiceDefaults()` and `MapDefaultEndpoints()`. | judge |
| 15 | `MapDefaultEndpoints()` keeps the Development check. Probes outside Development follow AHS-02. | judge |
| 16 | No second `AddOpenTelemetry()`, `AddStandardResilienceHandler()`, or `AddServiceDiscovery()` in a service that calls `AddServiceDefaults()`. | judge |
| 17 | Services use the client integration on `builder` with the resource name (`builder.AddNpgsqlDataSource("orders")`). No connection string for an AppHost resource in `appsettings.json`. | judge |

## Agents and dashboard

| # | Check | Type |
|---|-------|------|
| 18 | MCP configs use `aspire agent mcp`. No `"mcp", "start"`. | tool (AHS-MCP-START) |
| 19 | No `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL`, no dashboard MCP server, and no dashboard Copilot setting. | tool (AHS-DASHBOARD-MCP) |

## Tests

| # | Check | Type |
|---|-------|------|
| 20 | No `Task.Delay` in a test that uses `DistributedApplicationTestingBuilder`. | tool (AHS-TEST-DELAY) |
| 21 | `BuildAsync`, `StartAsync`, and `WaitForResourceHealthyAsync` each have `WaitAsync(timeout, ct)`. | judge |
| 22 | The test waits for the resource before its first call, and uses `CreateHttpClient` with the resource name. | judge |
| 23 | The split of AHS-07: the tests of one API do not start the whole AppHost. | judge |

## Publish and deploy

| # | Check | Type |
|---|-------|------|
| 24 | No `dotnet run --operation publish` and no `--publisher manifest`. | judge |
| 25 | The deployment target follows AHS-04. No step runs `aspire deploy` or `azd up` without a person. | judge |

## Severity

- blocking: a secret value in git, an AppHost that does not build, `MapDefaultEndpoints()` without the Development check on a public port, a removed package (`Aspire.RabbitMQ.Client.v6`).
- major: a new AppHost on `Aspire.AppHost.Sdk`, `AddProject<>` in a 17 AppHost, `WithReference` without `WaitFor`, `Task.Delay` or no timeout in a test, a deprecated MCP config.
- minor: an `Aspire.*` package on another 17.0.x patch than the others, a missing `WithDataVolume()`.

## Do not flag

- `Aspire.AppHost.Sdk/13.x` and `AddProject<>` in an existing 13.x AppHost that the human keeps (AHS-01). Name it once under "Open points".
- `WithReference` without `WaitFor` for a resource that the service does not need at its start, when a comment gives the reason.
- `Task.Delay` in a test without `DistributedApplicationTestingBuilder`.
