# Research: recent AI mistakes and useful options for dotnet-11, orleans, aspire, blazor

Date: 2026-11-19. Status: research round 2 for phase 2 of the stack split. This file adds to `research-dotnet.md`. It does not repeat the versions and traps of round 1.

Marks:

- **verified, source**: the linked page, issue, or file says it. A person or a tool can read it at the URL.
- **verified, probe**: the model wrote it in the probe of this research (see "Probe method").
- **not verified**: from one weak source, or the source does not say it in full. Check it before a rule uses it.

## Contents

- Probe method
- dotnet-11
- orleans
- aspire
- blazor
- New traps for the skills
- Facts that are not verified

## Probe method

- Tool: `claude -p` (headless), one call at a time, from an empty scratch folder.
- The research removed the variables of the parent session (`CLAUDECODE`, each `CLAUDE_*` variable).
- Flags: `--tools ""`, `--disallowedTools` (Skill, Agent, Bash, Read, Edit, Write, WebFetch, WebSearch, Glob, Grep), `--strict-mcp-config`, `--disable-slash-commands`, `--setting-sources ""`, `--no-session-persistence`. No skill, plugin, or MCP server could help the model.
- Models: `haiku` (Haiku 5.5) and `sonnet` (Sonnet 5.5). 8 prompts for each model. Each prompt starts with "Today is 2026-11-19" and asks for the latest stable version and for code only.
- Total cost: $0.21 (16 calls, 2 discarded calls, and 1 repeat).
- Two problems in the first run. The `claude` process read the prompt file on stdin, so 3 Haiku answers mixed several prompts. The research discarded them and ran the calls again with stdin closed. The first Sonnet answer to the AppHost prompt was only a tool call. The repeat added "You have no tools".
- Both models said that they know Aspire 13.0 as the latest version. Both models used `net10.0` when the prompt did not say `net11.0`.

## dotnet-11

### Recent mistakes

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| The official skill `migrate-dotnet10-to-dotnet11` says ".NET 11 is in preview" and covers breaking changes "through Preview 3". Its `global.json` example sets `"version": "11.0.100-preview.3"`. An agent that loads this skill writes a preview SDK after GA. | dotnet/skills (Microsoft agent skill, for Copilot, Claude, and Codex) | Last change 2026-05-11, still the state on 2026-11-19 | [SKILL.md](https://github.com/dotnet/skills/blob/main/plugins/dotnet-upgrade/skills/migrate-dotnet10-to-dotnet11/SKILL.md) | verified, source |
| The Copilot upgrade instructions tell the agent to run `dotnet tool install -g upgrade-assistant`. Microsoft deprecated .NET Upgrade Assistant. The target is "the latest LTS (e.g. `net10.0`)". | github/awesome-copilot `dotnet-upgrade.instructions.md` | Last change 2026-02-03, still the state on 2026-11-19 | [instructions](https://github.com/github/awesome-copilot/blob/main/instructions/dotnet-upgrade.instructions.md), [deprecation](https://learn.microsoft.com/dotnet/core/porting/upgrade-assistant-overview) | verified, source |
| A C# 15 task gets an abstract record with a private constructor and a `_ => throw` arm, not a `union`. | Haiku 5.5, Sonnet 5.5 | 2026-11-19 | Probe d11b | verified, probe |
| The model calls `UseConcurrencyLimiter` "deprecated" or "maybe still compiles". ASP.NET Core 11 removed it. | Haiku 5.5, Sonnet 5.5 | 2026-11-19 | Probe d11a | verified, probe |

No GitHub issue in dotnet/runtime or dotnet/sdk after 2026-05-19 reports a wrong upgrade by an AI tool. The searches used "Copilot", "Claude", "LLM", "AI generated", and "hallucinat".

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku 5.5 | d11a: move a `net10.0` API with EF Core 10 and `UseConcurrencyLimiter` to .NET 11 | `global.json`, TFM, and packages correct (11.0.100, 11.0.0). It wrote the rate limiter only "if `UseConcurrencyLimiter()` no longer compiles". It did not name the removal, STS support, the `x86-64-v2` baseline, the `BackgroundService` change, or the EF Core Design package. |
| Sonnet 5.5 | d11a | Same correct versions. It called the concurrency limiter "deprecated", not removed. It did not name STS support, the CPU baseline, or the `BackgroundService` change. It did name `dotnet-ef` and Dockerfile updates. |
| Haiku 5.5 | d11b: `SaveOrder` returns saved, conflict, or not found in `net11.0` | Abstract record hierarchy with a private constructor. No `union`. |
| Sonnet 5.5 | d11b | Abstract record hierarchy, `_ => throw new UnreachableException()`. No `union`. It also put the wrong `using` and then corrected it in a note. |

### Useful options

| Option | Task | Status | Source |
|--------|------|--------|--------|
| File-based apps: `dotnet run app.cs`, `dotnet app.cs`, `#:package`, `#:project`, `#:sdk`. New in 11: `#:include` for more files and DLLs, `dotnet format app.cs`, `dotnet reference add --file`. | Scripts, one-file tools, small repro cases | GA | [SDK 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/sdk), [file-based apps](https://learn.microsoft.com/dotnet/core/sdk/file-based-apps) |
| `dnx` and `dotnet tool exec` run a tool without install. In 11 they do not ask for an extra approval. | One-time tool runs in CI and by agents | GA | [SDK 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/sdk) |
| `dotnet run -e KEY=VALUE` | Run with environment values, no launch profile edit | GA | [SDK 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/sdk) |
| `dotnet watch` with Aspire AppHosts, and a restart after a crash | Hot reload of a full Aspire app | GA | [SDK 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/sdk) |
| `dotnet nuget add source --min-publish-age-hours`, NU1020 | Supply chain: do not take a package that is only hours old | GA | [SDK 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/sdk) |
| `dotnet sln` edits solution filters (`.slnf`) | Build a subset of a large solution | GA | [SDK 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/sdk) |
| `dotnet test` on MTP: `--timeout`, `--maximum-failed-tests`, `--no-dependencies`, `--list-tests` as JSON | Faster test loops for agents | GA (affected-test workflow is experimental) | [.NET 11 overview](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview) |
| GitHub Copilot app modernization agent (`@Modernize`, `@modernize-dotnet`) replaces Upgrade Assistant | Large upgrades with a plan in `.github/upgrades/` | GA. The docs list .NET 5 to 7 to ".NET 8 or later". A .NET 10 to 11 path: not verified | [porting overview](https://learn.microsoft.com/dotnet/core/porting/) |
| Microsoft agent skill `migrate-dotnet10-to-dotnet11` | Checklist of breaking changes by area (compiler, libraries, SDK, ASP.NET Core, EF Core, crypto, JIT) | Stale (Preview 3). Use its reference list, not its versions | [dotnet/skills](https://github.com/dotnet/skills/tree/main/plugins/dotnet-upgrade/skills/migrate-dotnet10-to-dotnet11) |

That skill names breaking changes that round 1 did not list (verified, source: the same SKILL.md, which stops at Preview 3):

- `Microsoft.EntityFrameworkCore.Tools` no longer brings `.Design`.
- EF Core 11 removes the `EFOptimizeContext` property.
- `SqlVector<T>` is not in `SELECT` by default.
- SYSLIB0063 on the `NamedPipeClientStream` constructor with `isConnected`.
- The `(X.Y) when` parse change and `nameof(this.P)` in attributes.
- ZIP reads check CRC32.

## orleans

### Recent mistakes

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| Test with `TestClusterBuilder`, `ISiloBuilderConfigurator`, and `ConfigureApplicationParts`. The last two are Orleans 3.x APIs. The code does not compile on Orleans 7 or later. | Haiku 5.5 | 2026-11-19 | Probe orlb | verified, probe |
| Test with `TestClusterBuilder` and an `ISiloConfigurator` class, not `InProcessTestClusterBuilder` | Sonnet 5.5 | 2026-11-19 | Probe orlb | verified, probe |
| Floating package versions `Version="*"` and `Version="10.*"` for `Microsoft.Orleans.Server` | Haiku 5.5, Sonnet 5.5 | 2026-11-19 | Probe orla | verified, probe |

No public report after 2026-05-19 names an AI mistake with Orleans. The searches covered dotnet/orleans issues ("Copilot", "Claude", "Cursor", "LLM", "AI generated") and the web. Orleans has no official agent skill in dotnet/skills. Community skills exist (managedcode `dotnet-skills`). This research did not read them.

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku 5.5 | orla: counter grain, storage `store`, write each 30 seconds, silo setup | Current: `IPersistentState<T>`, `[PersistentState]`, `[GenerateSerializer]`, `[Id]`, `RegisterGrainTimer`. Old: `Host.CreateDefaultBuilder`. No `[Alias]`. `Version="*"`. `WriteStateAsync()` without the token. |
| Sonnet 5.5 | orla | Current: `RegisterGrainTimer` with `GrainTimerCreationOptions`, the token passed to `WriteStateAsync`, `Host.CreateApplicationBuilder`. No `[Alias]`. `Version="10.*"`. |
| Haiku 5.5 | orlb: MSTest test with a test cluster | Orleans 3.x test setup (does not compile). |
| Sonnet 5.5 | orlb | `TestClusterBuilder` with a configurator class. It compiles, but it is the older form. |

Both models used a write on deactivation as the only safe point. Neither model said that a silo crash loses the views since the last timer tick. A skill must state this trade-off.

### Useful options

| Option | Task | Status | Source |
|--------|------|--------|--------|
| Orleans Dashboard: `Microsoft.Orleans.Dashboard`, `siloBuilder.AddDashboard()`, `app.MapOrleansDashboard()`, and `.RequireAuthorization()` | Watch silos, grains, method latency, reminders | Docs: preview. NuGet: 10.4.2 without a preview tag. The old `OrleansDashboard` package (OrleansContrib) stops at 8.2.0 | [dashboard](https://learn.microsoft.com/dotnet/orleans/dashboard/) |
| Redis providers: `Microsoft.Orleans.Clustering.Redis`, `.Persistence.Redis`, `.Reminders.Redis`, `.GrainDirectory.Redis` | Clustering and state on Redis | GA 10.4.2 | [overview](https://learn.microsoft.com/dotnet/orleans/overview), NuGet |
| `ResourceOptimizedPlacement` is the default since 9.2. Others: `[PreferLocalPlacement]`, `[HashBasedPlacement]`, `[ActivationCountBasedPlacement]`, a custom `IPlacementDirector` | Control where a grain activates | GA | [grain placement](https://learn.microsoft.com/dotnet/orleans/grains/grain-placement) |
| Activation repartitioning: `AddActivationRepartitioner()` | Move grains near the grains they call most | Experimental (ORLEANSEXP001) | [grain placement](https://learn.microsoft.com/dotnet/orleans/grains/grain-placement#activation-repartitioning) |
| Activation rebalancing | Balance memory and activation count across silos | Experimental (ORLEANSEXP002), Orleans 10.0 and later | [grain placement](https://learn.microsoft.com/dotnet/orleans/grains/grain-placement#activation-rebalancing) |
| `[Immovable]` and `[Immovable(ImmovableKind.Repartitioner)]`, `this.MigrateOnIdle()`, `IGrainMigrationParticipant` | Stop automatic moves, or ask for a move | GA (API in 10.0) | [grain lifecycle](https://learn.microsoft.com/dotnet/orleans/grains/grain-lifecycle#grain-migration) |
| `IAsyncEnumerable<T>` return from a grain method, `.WithBatchSize(n)` | Stream a large result to the caller | GA | [develop a grain](https://learn.microsoft.com/dotnet/orleans/grains/#iasyncenumerable-return-values) |
| Durable Jobs (`Microsoft.Orleans.DurableJobs`) | Scheduled jobs for grains | Alpha (10.4.2-alpha.1) | NuGet, round 1 |
| Journaling, `JournaledGrain` (`Microsoft.Orleans.EventSourcing` 10.4.2) | Event sourcing | EventSourcing: GA. Journaling: alpha | NuGet, round 1 |
| NATS streams (`Microsoft.Orleans.Streaming.NATS`) | Streams on NATS | Alpha (10.4.2-alpha.1) | NuGet |
| System.Text.Json grain storage serializer (10.3) | State without Newtonsoft.Json | GA | round 1 |
| Grain observers (`IGrainObserver`, `ObserverManager<T>`) | Push from a grain to a client | GA in older versions. Current API: not verified in this round | not verified |

## aspire

### Recent mistakes

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| `aspire update` on a 13.6.5 AppHost changes the SDK to `Aspire.AppHost.Sdk/17.0.0`, which is obsolete (ASPIRE012). It does not move to `Microsoft.NET.Sdk`. An agent that trusts `aspire update` keeps the old form. | Aspire CLI 17.0 | 2026-11-18 | [microsoft/aspire#22383](https://github.com/microsoft/aspire/issues/22383) | verified, source |
| Aspire deprecates agent MCP configs with `aspire mcp start`. The current command is `aspire agent mcp`. `aspire agent init` cannot repair a config with an extra argument. | Claude Code, VS Code, Copilot CLI, OpenCode configs | 2026-11-09 | [microsoft/aspire#21958](https://github.com/microsoft/aspire/issues/21958), [McpConfigFileHelper.cs](https://github.com/microsoft/aspire/blob/main/src/Aspire.Cli/Agents/McpConfigFileHelper.cs) | verified, source |
| Agents set up the dashboard MCP server and `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL` (removed in 13.3), or the dashboard Copilot or AI Assistant (removed in 13.3 and 13.5). Microsoft's skill tells agents to "scrub" these. | All agents (Microsoft skill text) | Skill 0.0.6, 2026-11-17 | [13.3 breaking changes](https://github.com/microsoft/aspire-skills/blob/main/skills/aspire/references/aspire-13-3-breaking-changes.md) | verified, source |
| The official Aspire skill detects a C# AppHost by `Aspire.AppHost.Sdk` and calls `DotnetProjectResource` "experimental". The Aspire 17 page says that Aspire deprecates `AddProject` and that `Aspire.Hosting.Dotnet` is stable. | microsoft/aspire-skills 0.0.6 | 2026-11-17 | [aspire SKILL.md](https://github.com/microsoft/aspire-skills/blob/main/skills/aspire/SKILL.md), [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/) | verified, source |
| AppHost with `Sdk="Aspire.AppHost.Sdk/13.0.0"`, `AddProject<Projects.X>`, and 13.0.0 packages | Haiku 5.5, Sonnet 5.5 | 2026-11-19 | Probe aspa | verified, probe |
| `Aspire.Hosting.Testing` 13.0.0 or `Version="*"`, and `MSTest.Sdk/3.6.4` | Sonnet 5.5, Haiku 5.5 | 2026-11-19 | Probe aspb | verified, probe |

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku 5.5 | aspa: AppHost with PostgreSQL `orders`, Redis, API, and worker. Start after the database and Redis | `Aspire.AppHost.Sdk/13.0.0`, `AddProject<>`, `ProjectReference`. No `WithHttpHealthCheck`, no ServiceDefaults. `WaitFor` correct. |
| Sonnet 5.5 | aspa | Same old SDK and `AddProject<>`. Packages at 13.0.0. No ServiceDefaults. `WaitFor` and `AddNpgsqlDataSource("orders")` correct. |
| Haiku 5.5 | aspb: MSTest test, GET /health on `api` | `MSTest.Sdk/3.6.4`, `Version="*"`. `WaitForResourceHealthyAsync` correct, but no timeout. It uses `TestContext` in a test without the property (compile error). |
| Sonnet 5.5 | aspb | Current pattern with a 120-second timeout. `Aspire.Hosting.Testing` 13.0.0, `net10.0`. |

### Useful options

| Option | Task | Status | Source |
|--------|------|--------|--------|
| `aspire agent init --agents <copilot\|claude\|opencode> --scope <project\|global> --skills aspire` | Give the agent the official Aspire plugin. In 17 it registers a plugin, it does not copy skill files | GA in 17 | [Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| `aspire agent init --mcp` (MCP is opt-in since 13.6). The MCP server command is `aspire agent mcp` | Live resource state, logs, and traces for an agent | GA | [aspire SKILL.md](https://github.com/microsoft/aspire-skills/blob/main/skills/aspire/SKILL.md) |
| `aspire doctor` | Find stale local skill copies and deprecated MCP configs | GA | [aspire SKILL.md](https://github.com/microsoft/aspire-skills/blob/main/skills/aspire/SKILL.md) |
| `aspire docs search <topic>` and `aspire docs api search <query> --language csharp` | Read the current API before an edit, not from memory | GA | [aspire SKILL.md](https://github.com/microsoft/aspire-skills/blob/main/skills/aspire/SKILL.md) |
| `aspire integration search <query> --format Json`, then `aspire add <package>` | Find the correct integration package | GA | [aspire SKILL.md](https://github.com/microsoft/aspire-skills/blob/main/skills/aspire/SKILL.md) |
| `aspire start --non-interactive --isolated --apphost <path>`, `aspire wait <resource>`, `aspire describe`, `aspire logs`, `aspire otel logs`, `aspire otel traces`, `aspire export` | Agent loop: start, wait, read telemetry before a code change | GA | [aspire SKILL.md](https://github.com/microsoft/aspire-skills/blob/main/skills/aspire/SKILL.md) |
| `aspire resource <resource> <command>` (start, stop, restart, set-parameter) | Restart one resource, not the whole AppHost | GA | [aspire resource](https://aspire.dev/reference/cli/commands/aspire-resource/) |
| Parameters: `AsOptional()`, `AsRequired()`. An empty string no longer satisfies a required parameter. `aspire secret set --environment` | Secrets and settings without prompts | GA in 17 | [Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| Kubernetes and AKS publishers (`Aspire.Hosting.Kubernetes` 17.0.0), Helm 4. Persistent volumes are experimental (ASPIRECOMPUTE002) | `aspire publish` and `aspire deploy` to Kubernetes | GA in 17 | [Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| Go AppHost (`apphost.go`), `AddGoApp` | Polyglot repos | GA in 17 | [Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| CommunityToolkit.Aspire 17 | Extra integrations (for example Ollama) | Beta (17.0.0-beta) | NuGet |
| `aspire exec` | Run a command in the context of a resource | The docs page now redirects to `aspire resource`. State: not verified | [aspire.dev redirect](https://aspire.dev/reference/cli/commands/aspire-exec/) |

Removed in Aspire 17 (verified, source: [Aspire 17](https://aspire.dev/whats-new/aspire-17/)): the packages `Aspire.Azure.AI.OpenAI`, `Aspire.Azure.AI.Inference`, `Aspire.MongoDB.Driver.v2`, and `Aspire.RabbitMQ.Client.v6`, and `WithLetsEncryptProduction` and `WithLetsEncryptStaging`. The 17.0.0 source tree has `Aspire.Azure.AI.Extensions.OpenAI` and `Aspire.OpenAI`. Which one replaces `Aspire.Azure.AI.OpenAI`: not verified.

## blazor

### Recent mistakes

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| Microsoft opens work for a Blazor agent benchmark. The reason: public agent benchmarks have no Blazor tasks, and the team expects that agents do worse on Blazor than on other UI frameworks. | Agents in general | 2026-11-19 | [aspnetcore#70726](https://github.com/dotnet/aspnetcore/issues/70726), [#70727](https://github.com/dotnet/aspnetcore/issues/70727), [#70731](https://github.com/dotnet/aspnetcore/issues/70731) | verified, source |
| Copilot replaced a `Debug.Assert` in `EndpointHtmlRenderer` with a runtime exception and a test for it. The team now asks for a rule in the Copilot instructions. | GitHub Copilot (aspnetcore repo) | 2026-09-29 | [aspnetcore#69586](https://github.com/dotnet/aspnetcore/issues/69586) | verified, source |
| The Copilot Blazor instructions name "C# 14" as current, say nothing about render modes or prerendering, and recommend Blazored.LocalStorage, Fluxor, and Swagger | github/awesome-copilot `blazor.instructions.md` | 2026-06-29 | [instructions](https://github.com/github/awesome-copilot/blob/main/instructions/blazor.instructions.md) | verified, source |
| The Microsoft Blazor skills list the agent mistakes that they fix. The list after this table gives them. | dotnet/skills `dotnet-blazor` plugin | Last change 2026-11-02 | [dotnet-blazor skills](https://github.com/dotnet/skills/tree/main/plugins/dotnet-blazor/skills) | verified, source |
| Copilot, Claude Sonnet 4.5, and ChatGPT wrote Blazor code that did not compile, crashed, or had a wrong architecture | Copilot, Claude Sonnet 4.5, ChatGPT | 2026-03-11 (before the 6-month window) | [HackerNoon](https://hackernoon.com/razor-pages-beat-blazor-for-ai) | not verified (no code in the post) |
| `RegisterOnPersisting` and `TryTakeFromJson`, not `[PersistentState]` | Haiku 5.5 | 2026-11-19 | Probe blza | verified, probe |
| `[ValidateComplexType]` with only `DataAnnotationsValidator`. The nested fields do not validate, and nothing reports an error. | Haiku 5.5 | 2026-11-19 | Probe blzb | verified, probe |
| `app.UseStaticFiles()`, not `app.MapStaticAssets()` | Haiku 5.5, Sonnet 5.5 | 2026-11-19 | Probes blza, blzb | verified, probe |

The mistakes that the Microsoft Blazor skills fix (verified, source: [dotnet-blazor skills](https://github.com/dotnet/skills/tree/main/plugins/dotnet-blazor/skills)):

- JS interop in `OnInitializedAsync`.
- `InvokeAsync<object>` for a void call.
- A private `[JSInvokable]` method.
- `HttpContext` or `SignInManager` in an interactive component.
- No `AddAuthenticationStateSerialization` for WebAssembly or Auto.
- `required` or `init` on a `[Parameter]` (BL0007).
- `_ = InvokeAsync(...)`, which hides exceptions.
- Prerender off as the first fix for a double load.
- `dotnet new blazorwasm` for a Blazor Web App.

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku 5.5 | blza: `/products` with Interactive Server, loads one time, no flicker | Old `PersistentComponentState.RegisterOnPersisting`, `TryTakeFromJson`, and `IDisposable`. `UseStaticFiles`. |
| Sonnet 5.5 | blza | Current: `[PersistentState]`, `Items ??= await ...`, `MapStaticAssets`. No mistake found. |
| Haiku 5.5 | blzb: static SSR order form, nested address and lines, each field validates | `[ValidateComplexType]` without `ObjectGraphDataAnnotationsValidator` (nested validation does not run). No `AddValidation`. `UseStaticFiles`. `FormName` correct. |
| Sonnet 5.5 | blzb | Current: `AddValidation()`, `[ValidatableType]`, `FormName`, `Model ??= new()`. `UseStaticFiles`. `OnInitialized` adds empty lines on each POST too, so a posted form can get new empty lines that fail validation. |

### Useful options

| Option | Task | Status | Source |
|--------|------|--------|--------|
| Render mode per page from a decision table. Static SSR for content. Server for most interactive apps. WebAssembly only with a reason. Auto only when Server and WebAssembly both fail. | Choose the mode before the code | GA (guidance) | [create-blazor-project](https://github.com/dotnet/skills/blob/main/plugins/dotnet-blazor/skills/create-blazor-project/SKILL.md) |
| `[PersistentState]`, `RegisterPersistentService<T>` | No double load after prerender | GA (.NET 10) | round 1 |
| `AddValidation()` and `[ValidatableType]` | Nested and collection validation in forms | GA (.NET 10, not experimental in 11) | round 1 |
| QuickGrid 11: sort and page as links in the query string, `OnRowClick` | Data grids that work in static SSR | GA (11.0.0) | round 1, NuGet |
| `Virtualize` with items of different heights, `OverscanCount` default 15 | Long lists | GA. Two .NET 11 regressions are open: fractional row heights leave the view blank, and Home or End in a text input moves the list | [aspnetcore#70553](https://github.com/dotnet/aspnetcore/issues/70553), [#70554](https://github.com/dotnet/aspnetcore/issues/70554) |
| bUnit 2.x (2.15.2): `BunitContext`, one `Render` method, `DisposeComponentsAsync`, `BunitJSRuntime` | Component tests | GA | [bUnit 1 to 2 migration](https://bunit.dev/docs/migrations/1to2.html), NuGet |
| `ErrorBoundary`. `ComponentBase` swallows each `OperationCanceledException`, so a timeout leaves the component in its loading state | Error UI for a component | GA | [fetch-and-send-data](https://github.com/dotnet/skills/blob/main/plugins/dotnet-blazor/skills/fetch-and-send-data/SKILL.md) |
| `dotnet watch` refreshes static SSR pages in an app with WebAssembly or Auto | Inner loop | GA (.NET 11 SDK) | [.NET 11 overview](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview) |
| The Microsoft `dotnet-blazor` agent plugin (author-component, collect-user-input, configure-auth, support-prerendering, use-js-interop, and others) | A second source of rules to compare | GA (repository), content changes often | [dotnet/skills](https://github.com/dotnet/skills/tree/main/plugins/dotnet-blazor/skills) |
| Components.AI and agent-generated UI from a build-time component catalog | UI that an agent selects, not writes | Prototype (issue only) | [aspnetcore#70732](https://github.com/dotnet/aspnetcore/issues/70732) |

## New traps for the skills

These traps add to the trap tables of round 1.

| Old or wrong | Correct | Skill |
|--------------|---------|-------|
| `global.json` with `11.0.100-preview.3` (from the stale Microsoft skill) | `11.0.100` or later GA, with `rollForward` `latestFeature` | dotnet-11 |
| `dotnet tool install -g upgrade-assistant` | Edit the project files and `global.json` by hand, or use the Copilot modernization agent. Microsoft deprecated Upgrade Assistant | dotnet-11 |
| `UseConcurrencyLimiter` kept "if it still compiles" | ASP.NET Core 11 removed it. Use `AddRateLimiter` with a concurrency limiter and `UseRateLimiter` | dotnet-11 |
| `Microsoft.EntityFrameworkCore.Tools` alone for migrations | Add `Microsoft.EntityFrameworkCore.Design` explicitly on EF Core 11 | dotnet-11 |
| `<EFOptimizeContext>` | `<EFScaffoldModelStage>` and `<EFPrecompileQueriesStage>` | dotnet-11 |
| `net8.0` or `net9.0` for a new project | Both are out of support since 2026-11-10. Use `net10.0` (LTS) or `net11.0` (STS) | dotnet-11 |
| An abstract record with a `_ => throw` arm in a `net11.0` project | `union` and an exhaustive `switch` (round 1, now probe-backed) | dotnet-11 (points to csharp) |
| `ISiloBuilderConfigurator` and `ConfigureApplicationParts` in a test | `InProcessTestClusterBuilder` with delegates. Orleans 7 and later find grains without application parts | orleans |
| `PackageReference Version="*"` or `"10.*"` for Orleans | A fixed version, for example `10.4.2`, in `Directory.Packages.props` | orleans |
| `OrleansDashboard` package with `UseDashboard()` | `Microsoft.Orleans.Dashboard`, `AddDashboard()`, `MapOrleansDashboard().RequireAuthorization()` | orleans |
| A flush timer with no note on data loss | Say that a crash loses the count since the last write. Write on deactivation too | orleans |
| `ORLEANSEXP001` or `ORLEANSEXP002` features as the default | Experimental: only with a reason and a `NoWarn` for that ID | orleans |
| `Aspire.AppHost.Sdk/13.x` or `/17.0.0` after `aspire update` | `Sdk="Microsoft.NET.Sdk"` with `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`. Check the SDK after `aspire update` | aspire |
| `"args": ["mcp", "start"]` in `.mcp.json` | `"args": ["agent", "mcp"]`, or run `aspire agent init --mcp` | aspire |
| `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL`, the dashboard MCP server, the dashboard Copilot or AI Assistant | `aspire agent init` (skills) and `aspire agent init --mcp` | aspire |
| `Aspire.Azure.AI.OpenAI`, `Aspire.Azure.AI.Inference`, `Aspire.MongoDB.Driver.v2` | Removed in 17. Use `aspire integration search` to find the current package | aspire |
| `WithLetsEncryptProduction` or `WithLetsEncryptStaging` | Removed in 17. TODO: the replacement is not verified | aspire |
| An AppHost without ServiceDefaults and without `WithHttpHealthCheck` | `AddServiceDefaults()` in each service, and `.WithHttpHealthCheck("/health")` on each HTTP resource | aspire |
| HTTP polling or `Task.Delay` to wait for a resource | `aspire wait <resource>` in the CLI, `WaitForResourceHealthyAsync` in a test | aspire |
| A required parameter set to `""` | Throws `MissingParameterValueException` in 17. Use `AsOptional()` when a value can be missing | aspire |
| `[ValidateComplexType]` with `DataAnnotationsValidator` only | `AddValidation()` and `[ValidatableType]` on the root model | blazor |
| `app.UseStaticFiles()` in a Blazor Web App | `app.MapStaticAssets()` | blazor |
| `OnInitialized` that adds default rows on each request | Add the default rows only when the form model is new (`Model is null` before `??=`) | blazor |
| bUnit 1.x: `TestContext`, `RenderComponent<T>`, `SetParametersAndRender` | bUnit 2.x: `BunitContext`, `Render<T>` | blazor |
| `Blazored.LocalStorage` or Fluxor as the default state solution | Built-in: cascading values, scoped services, `[PersistentState]`. Add a library only with a reason | blazor |
| `Virtualize` with fractional row heights on .NET 11 | Use whole-pixel row heights until the team fixes aspnetcore#70553 | blazor |

## Facts that are not verified

- Whether the Copilot modernization agent supports a .NET 10 to .NET 11 upgrade. The docs name paths to ".NET 8 or later" only.
- The replacement of `Aspire.Azure.AI.OpenAI` in Aspire 17 (`Aspire.Azure.AI.Extensions.OpenAI` or `Aspire.OpenAI`).
- The replacement of `WithLetsEncryptProduction` and `WithLetsEncryptStaging`.
- The state of `aspire exec` in Aspire 17. Its docs page redirects to `aspire resource`.
- Whether the Orleans Dashboard is still preview. The docs say preview, and the NuGet version 10.4.2 has no preview tag.
- The current grain observer API in Orleans 10.
- Public reports of AI mistakes with Orleans after 2026-05-19. This research found none.
- Reddit threads (r/dotnet, r/Blazor) about AI mistakes in this window. The web search returned none. Reddit was not searched directly.
- The HackerNoon report gives no code. It is from before the window.
- This research did not compile a probe answer. The marks "does not compile" come from the API history, not from a build.
