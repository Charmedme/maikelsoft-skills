# Concept: stack split phase 2

Date: 2026-11-19. Status: approved on 2026-11-19. The owner accepted each recommendation. The owner added one task: more research for each skill (`research-mistakes-*.md`). It covers useful options and the mistakes of AI tools in the last 6 months. The skills fix these mistakes.

Phase 2 adds 6 skills to dev-stack and 1 skill to frontend-stack. The research is in `research-dotnet.md` and `research-api-mcp-webmcp.md` in this folder.

## Contents

- Version facts that decide the rules
- Skills
- Changes to existing skills
- Research round 2
- Behaviour test
- Decision sheet
- Releases

## Version facts that decide the rules

- .NET 11 is GA since 2026-11-10 (SDK 11.0.100, C# 15). It is STS. Its support ends on 2028-11-14, the same day as .NET 10 LTS.
- Aspire 17.0.0 came out on 2026-11-18. It deprecates `Aspire.AppHost.Sdk` (ASPIRE012) and `AddProject<Projects.X>`. The new call is `AddDotnetProject("name", "path.csproj")`.
- Orleans 10.4.2 has `net8.0` and `net10.0` assets, and no `net11.0` asset.
- The C# MCP SDK `ModelContextProtocol` is 2.4.0. The MCP spec is 2026-07-28: stateless HTTP by default, no `initialize` handshake. The `mcpserver` template still references SDK 1.2.0.
- WebMCP is a Community Group draft. The API is on `document.modelContext`, not on `navigator`. Chrome and Edge have origin trials. Firefox and Safari have no implementation.
- RFC 9457 obsoletes RFC 7807. The Idempotency-Key header is draft -08. The RateLimit header fields are draft -11.

## Skills

| Skill | Plugin | Scope | Top traps |
|-------|--------|-------|-----------|
| `dotnet-11` | dev-stack | The move to `net11.0`: the version gate, `global.json`, TFM and package versions together, breaking changes, the upgrade checklist. Points to `csharp` (`references/net11.md`) for C# 15. | A TFM change without `global.json` and EF Core 11. `EnablePreviewFeatures` for runtime async. `UseConcurrencyLimiter`. A move for "longer support". |
| `orleans` | dev-stack | Grains, silo and client setup, serialization, state, timers, reminders, streams, test cluster, Orleans metrics. | `Grain<TState>`, `RegisterTimer`, `SiloHostBuilder`, `[Serializable]`, `TestClusterBuilder`, `ConfigureAwait(false)` in a grain, `CancellationToken` not last. |
| `aspire` | dev-stack | AppHost, ServiceDefaults, client integrations, `WaitFor`, dashboard, `aspire publish` and `deploy`, `Aspire.Hosting.Testing`. Existing AppHosts on 13.x keep their form until the human decides on 17. | `workload install aspire`, `Aspire.AppHost.Sdk`, `AddProject<>`, `WithReference` without `WaitFor`, `Task.Delay` in a test. |
| `blazor` | dev-stack | Render modes with a decision table, prerendering and state, forms and validation, navigation, the circuit, auth state per mode, QuickGrid, JS interop. | `_Host.cshtml`, hosted WebAssembly, `RegisterOnPersisting`, a double load, `ObjectGraphDataAnnotationsValidator`, `<NotFound>`, JS interop in `OnInitializedAsync`, no `FormName`. |
| `dotnet-mcp` | dev-stack | MCP servers with the C# SDK: transports, tools, DI, stateless HTTP, auth metadata, tool hints, NuGet packaging. | SDK 0.x or 1.x, the HTTP+SSE transport, stateful sessions, stdout in stdio, `ElicitAsync` from a tool, no audience check, no tool hints. |
| `api-design` | dev-stack | The HTTP contract, independent of the framework. It covers resources, methods, status codes, RFC 9457 errors, conditional requests, idempotency, pagination, partial updates, long-running work, version and deprecation policy. | RFC 7807, 200 with an error flag, verbs in URLs, offset paging on a large set, no idempotency, last write wins, `X-RateLimit-*`. |
| `webmcp` | frontend-stack | Browser tools for agents: the feature check, `registerTool` with an `AbortSignal`, tool design, the lifecycle in a SPA, the polyfill. Label: preview. | `navigator.modelContext`, `provideContext`, `unregisterTool`, no feature check, no `await`, `@mcp-b/webmcp-polyfill`. |

Each skill follows the stack skill template. It has a `SKILL.md` under 300 lines, `references/decisions.md`, a review checklist, and sources. Its `rules.json` has 3 to 6 rules for `stackcheck.py`.

## Changes to existing skills

- `aspnetcore`: minimum 10.0.15 (security release). Decisions API-04, API-12, API-13, and API-17 move to `api-design`. `aspnetcore` keeps a pointer and the wiring.
- `csharp` `references/net11.md`: status GA, runtime async without "preview", the CPU baseline `x86-64-v2`, 3 behavior changes, EF Core 11 needs `net11.0`.
- .NET 11 column (brief Q8): in `csharp`, `aspnetcore`, `aspnetcore-openapi` (OpenAPI 3.2 default), and `efcore` (EF Core 11). The other skills get it later, when a .NET 11 fact changes their rules.
- `dev-team:stack-index`: 7 new rows.

## Research round 2

`research-mistakes-dotnet.md` and `research-mistakes-api-mcp-webmcp.md` hold the mistakes of the last 6 months, the probe results, and the useful options. Each skill takes the rows of "New traps for the skills" for its name into its trap table and its checklist.

Facts from round 2 that change the plan:

- Some guidance that agents read is old. The .NET 10 to 11 skill of Microsoft says "preview". `aspire update` writes the deprecated SDK. The C# MCP SDK instructions name removed types. The skills give the current form and tell the agent not to trust such a source.
- A "user confirms" step that the agent can complete alone is no confirmation. This applies to `dotnet-mcp` and `webmcp`.
- Angular 22 has a built-in WebMCP API (`provideExperimentalWebMcpTools`). The `webmcp` skill covers it, and scenario S8 stays on plain JavaScript.

## Behaviour test

The workflow runs 8 scenarios with 6 agents (2 Haiku, 2 Sonnet, 2 Opus) and 3 arms: 144 runs. Each run gets a small task in a temporary folder, with no build. The agent writes files. A script checks each file for the current pattern and for the old pattern. One blind judge grades each result.

| No | Scenario | Skill | Arm "current" (dev-stack 1.3.0) |
|----|----------|-------|---------------------------------|
| S1 | Upgrade a `net10.0` API with EF Core 10 to .NET 11 | `dotnet-11` | `csharp` |
| S2 | A grain that counts views, with storage and a flush each 30 seconds | `orleans` | `csharp` |
| S3 | An AppHost with PostgreSQL, Redis, an API, and a worker | `aspire` | `aspnetcore` |
| S4 | A `/products` page with Interactive Server that loads one time | `blazor` | `aspnetcore` |
| S5 | A static SSR order form with a nested address and lines | `blazor` | `aspnetcore` |
| S6 | A stdio MCP server with a read tool and a cancel tool, for NuGet | `dotnet-mcp` | `aspnetcore` |
| S7 | An OpenAPI contract for an orders API (large list, retries, address change, cancel) | `api-design` | `aspnetcore-openapi` |
| S8 | WebMCP tools on a plain JavaScript todo page | `webmcp` | `angular` |

- Success (brief): the new arm passes more checks than the baseline on each model, and writes no old pattern.
- The checks are regex on the written files. The research gives the first list for each scenario.
- Cost estimate: about $70 for 144 runs and 144 judge calls. Cap: $150. At most 4 agents at the same time.

Before the build, one agent compiles the key traps on the .NET 11 SDK in a scratch folder. It runs one build at a time, because the machine has about 1 GB of free memory. The traps are the Orleans 10.4.2 grain on `net11.0`, an Aspire 17 AppHost, a Blazor `[PersistentState]` page, and an MCP server on SDK 2.4.0.

## Decision sheet

The recommendation is the answer unless the owner changes the row.

| ID | Question | Recommendation |
|----|----------|----------------|
| P2-01 | Aspire 17 is one day old. Which version do the rules use? | Aspire 17 for new AppHosts. An existing 13.x AppHost keeps its form (stack rule 3). The move to 17 is a decision for the human. |
| P2-02 | Orleans has no `net11.0` asset. Can an Orleans silo target `net11.0`? | Yes, after a build and the test cluster pass. The compile check gives the evidence. Until then, the skill says "not verified". |
| P2-03 | Where do the REST decisions API-04, 12, 13, and 17 live? | In `api-design`. `aspnetcore` keeps the wiring and a pointer. |
| P2-04 | Does `dotnet-11` copy the C# 15 rules? | No. `csharp/references/net11.md` keeps them. `dotnet-11` owns the upgrade. |
| P2-05 | The `mcpserver` template references SDK 1.2.0. | The skill says: after `dotnet new mcpserver`, update to 2.4.0. |
| P2-06 | WebMCP polyfill? | `@w3c/webmcp-polyfill` only when the human wants agents in browsers without WebMCP. The default is the feature check with no polyfill. |
| P2-07 | Which skills get the .NET 11 column now? | `csharp`, `aspnetcore`, `aspnetcore-openapi`, `efcore`. |
| P2-08 | Compile check on the .NET 11 SDK? | Yes, in a scratch folder, one build at a time. |

## Releases

- dev-stack 2.1.0: 6 new skills, and the changes to `aspnetcore`, `csharp`, `aspnetcore-openapi`, and `efcore`.
- frontend-stack 1.1.0: `webmcp`.
- dev-team 1.1.1: the new rows in `stack-index`.
