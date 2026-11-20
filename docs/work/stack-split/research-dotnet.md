# Research: dotnet-11, orleans, aspire, blazor

Date: 2026-11-19. Status: research for phase 2 of the stack split. Input for the concept of the 4 new dev-stack skills.

Each fact has a source and a mark:

- **verified, docs**: Microsoft Learn, aspire.dev, or the GitHub release notes say it.
- **verified, package metadata**: the NuGet API or the .NET release metadata says it.
- **not verified**: from memory or from one weak source. Check it before a rule uses it.

No fact in this file comes from a build. Only the .NET 10 SDK is on the research machine.

## Contents

- Summary of versions
- dotnet-11
- orleans
- aspire
- blazor
- Facts that are not verified

## Summary of versions

| Item | Version | Date | Mark |
|------|---------|------|------|
| .NET 11 runtime and ASP.NET Core 11 | 11.0.0 | 2026-11-10 | verified, package metadata |
| .NET 11 SDK | 11.0.100 (C# 15) | 2026-11-10 | verified, package metadata |
| .NET 11 support | STS, end of support 2028-11-14 | 2026-11-10 | verified, package metadata |
| .NET 10 runtime and SDK | 10.0.15 and SDK 10.0.404, LTS, end of support 2028-11-14 | 2026-11-10 | verified, package metadata |
| EF Core 11 | 11.0.0 | 2026-11-10 | verified, package metadata |
| Microsoft.Orleans.* | 10.4.2 (net8.0 and net10.0 assets) | 2026-10-30 | verified, package metadata |
| Microsoft.Orleans.Journaling, DurableJobs | 10.4.2-alpha.1 (preview) | 2026-10-30 | verified, package metadata |
| Aspire (Aspire.Hosting.*, Aspire.Cli) | 17.0.0 | 2026-11-18 | verified, package metadata |
| Aspire before 17 | 13.6.5 | 2026-11-10 | verified, package metadata |

Sources: [.NET 11 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/11.0/releases.json), [.NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json), the NuGet flat container index for each package, [Orleans releases](https://github.com/dotnet/orleans/releases), [Aspire releases](https://github.com/microsoft/aspire/releases).

Two facts change the existing skills:

- .NET 11 is GA since 2026-11-10. The 11.0.0 release is also a security release with 22 CVEs. (verified, package metadata)
- .NET 10.0.15 is a security release of 2026-11-10. `dev-stack:aspnetcore` names 10.0.14 as the minimum. Change it to 10.0.15. (verified, package metadata)

## dotnet-11

### Versions and support

- .NET 11.0.0 is GA since 2026-11-10. The SDK is 11.0.100. The C# version of this SDK is 15.0. (verified, package metadata, [releases.json](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/11.0/releases.json))
- .NET 11 is STS. Its end of support is 2028-11-14. (verified, package metadata)
- .NET 10 is LTS. Its end of support is also 2028-11-14. A move to .NET 11 does not give a longer support time. (verified, package metadata)
- The .NET 11 release notes page on Learn says that it is up to date for the GA release. (verified, docs, [What's new in .NET 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview))
- EF Core 11 needs the .NET 11 SDK and the .NET 11 runtime. It does not run on .NET 10. (verified, docs, [What's new in EF Core 11](https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/whatsnew))

### C# 15 at GA

The final list is the same as the list in `net11.md` at RC 3. Nothing was cut. (verified, docs, [What's new in C# 15](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15))

| Feature | State at GA |
|---------|-------------|
| Collection expression arguments `[with(...), ..]` | Shipped |
| Union types `union Pet(Cat, Dog, Bird);` | Shipped |
| Closed hierarchies `closed record` | Shipped |
| Extension indexers | Shipped |
| Labeled `break` and `continue` (IDE0410) | Shipped |
| Non-virtual static interface members on older runtimes | Shipped, for libraries |
| Memory safety (`unsafe(...)` expression, `safe`) | Preview only, needs `LangVersion preview` |

The compiler breaking changes for `with`, `closed`, and `union` are the same as in `net11.md`. The list also names C# 16 changes (`safe` keyword, pointer types). They do not apply to `net11.0`. (verified, docs, [C# compiler breaking changes since C# 14](https://learn.microsoft.com/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011))

### Breaking changes for app code

From the [.NET 11 breaking changes list](https://learn.microsoft.com/dotnet/core/compatibility/11) (verified, docs):

- The x86 and x64 baseline is now `x86-64-v2`. The runtime does not start on an older CPU. ([minimum hardware](https://learn.microsoft.com/dotnet/core/compatibility/jit/11/minimum-hardware-requirements))
- `IHost.RunAsync` and `IHost.StopAsync` throw when a `BackgroundService` fails.
- DI scope disposal aggregates the exceptions of each service.
- An empty string in a later configuration provider now overrides the value of an earlier provider.
- Some Microsoft.Extensions packages are now in the shared framework.
- `DateOnly.TryParse` and `TimeOnly.TryParse` throw for invalid input.
- `Math.Round` with digits gives correctly rounded results. The `decimal` and `BigInteger` conversions do too.
- Floating-point conversions to small integer types saturate.
- `ChangeToken.OnChange` binds an async lambda to the new `Task` overload.
- HttpClient metrics with many values use observable counters.

From the [ASP.NET Core 11 breaking changes list](https://learn.microsoft.com/aspnet/core/breaking-changes/11/overview) (verified, docs):

- ASP.NET Core 11 removes the `ConcurrencyLimiter` middleware.
- `Microsoft.OpenApi` is now 3.x, and `OpenApiVersion` defaults to `OpenApi3_2`.
- ASP.NET Core 11 removes the MVC compatibility options.
- Hosting emits the OpenTelemetry HTTP semantic convention tags by default.
- Kestrel is stricter about the HTTP protocol.
- SqlClient Active Directory authentication moved to a separate package.
- The Blazor changes are in the blazor section of this file.

### Library, ASP.NET Core 11, and EF Core 11 headlines

- Runtime async: Microsoft compiles the .NET libraries and the ASP.NET Core shared framework with it. For an own project, `<Features>runtime-async=on</Features>` turns it on. It no longer needs `EnablePreviewFeatures`. `<UseRuntimeAsync>false</UseRuntimeAsync>` turns it off. .NET 11 removes the `DOTNET_RuntimeAsync` variable. (verified, docs, [runtime](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/runtime#runtime-async))
- Libraries: System.Text.Json supports C# unions and JSON Lines. LINQ has `FullJoin` and tuple `Join`. Zstandard is in `System.IO.Compression`.
- More libraries: DataAnnotations has async validation (`AsyncValidationAttribute`, `IAsyncValidatableObject`). Options have `IAsyncStartupValidator`. `EqualityComparer<T>.Create` takes a key selector. `Process` has run-and-capture helpers. (verified, docs, [overview](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview))
- ASP.NET Core 11: union types in minimal API JSON bodies (shown as `anyOf` in OpenAPI), async validation with `AddValidation()`, and the `[ShortCircuit]` attribute. Also: automatic cross-origin CSRF protection in `WebApplication.CreateBuilder`, SignalR authentication refresh, Zstandard response compression, and an `mcpserver` template. (verified, docs, [What's new in ASP.NET Core 11](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11))
- EF Core 11: `FullJoin` translation, `MaxByAsync` and `MinByAsync`, complex types with TPT and TPC, and keys and indexes on complex type properties. Also: SQL Server vector search, `dotnet ef database update --add`, and `.config/dotnet-ef.json`. `UseSqlServer` defaults to compatibility level 160. (verified, docs, [What's new in EF Core 11](https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/whatsnew))

### Changes for `plugins/dev-stack/skills/csharp/references/net11.md`

1. Change the status line: GA on 2026-11-10, runtime 11.0.0, SDK 11.0.100. Remove "GA is expected".
2. Add: .NET 10 and .NET 11 both end support on 2028-11-14. The decision to move is about features, not support time.
3. Runtime async: remove "preview opt-in". Write "opt-in with `<Features>runtime-async=on</Features>`, no preview flag".
4. Add the CPU baseline `x86-64-v2` to "Before you use .NET 11".
5. Add 3 behavior changes: DI scope disposal, empty configuration strings, and Microsoft.Extensions packages in the shared framework.
6. Add: EF Core 11 needs `net11.0`. A project on `net10.0` stays on EF Core 10.
7. The C# 15 sections stay as they are. The examples compiled on RC 2. Compile them again on SDK 11.0.100 before the release (stack rule 2).

### Scope boundary

- `dotnet-11` owns the move to `net11.0`: the version gate, `global.json`, `TargetFramework`, the package versions, the breaking changes, and the checklist of an upgrade.
- `csharp` keeps the C# 15 language rules in `references/net11.md`. `dotnet-11` points to that file and does not copy it.
- `aspnetcore`, `aspnetcore-openapi`, and `efcore` keep their framework rules. Each one gets a .NET 11 column (brief Q8). `dotnet-11` lists only what changes at the upgrade.

### Top traps

| Old pattern | Current pattern | Source |
|-------------|-----------------|--------|
| A result library (OneOf) or an abstract record with a `_ =>` discard arm | `union SaveResult(Saved, NotFound);` and an exhaustive `switch` | [C# 15](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15) |
| `new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ... }` | `[with(StringComparer.OrdinalIgnoreCase), .. items]` | [C# 15](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15) |
| A Boolean flag or `goto` to leave nested loops | `break outer;` on a labeled loop | [C# 15](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15) |
| `<EnablePreviewFeatures>` or `DOTNET_RuntimeAsync` for runtime async | `<Features>runtime-async=on</Features>`, or nothing | [runtime](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/runtime#runtime-async) |
| `TargetFramework` changed, `global.json` and EF Core 10 left as they are | Change `global.json` (11.0.100), the TFM, and the Microsoft.* packages to 11.0.0 together | [EF Core 11](https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/whatsnew) |
| `app.UseConcurrencyLimiter()` | The rate limiter middleware (`AddRateLimiter`) | [ASP.NET Core 11 breaking changes](https://learn.microsoft.com/aspnet/core/breaking-changes/11/overview) |
| A test that expects `TryParse` to return `false` for a bad `DateTimeStyles` value | Expect `ArgumentException` | [.NET 11 breaking changes](https://learn.microsoft.com/dotnet/core/compatibility/11) |

### Candidate eval scenarios

1. **Save result in a `net11.0` project.** Task: "The project targets net11.0. Add a `SaveOrder` method. It returns saved, conflict, or not found. Add a method that maps the result to a message." Script checks: `union ` and a `switch` over the case types. The text does not contain `OneOf`, `ErrorOr`, or a `_ =>` arm in that switch.
2. **Upgrade a project file.** Task: "Move this net10.0 API with EF Core 10 to .NET 11." Give a `.csproj` and a `global.json`. Script checks: `net11.0`, an SDK version that starts with `11.0.`, and `Microsoft.EntityFrameworkCore` at `11.0.`. The text does not contain `EnablePreviewFeatures` or `LangVersion`.

## orleans

### Versions

- The current release is 10.4.2 of 2026-10-30. 10.4.0 came out on 2026-10-03. (verified, package metadata, [Orleans releases](https://github.com/dotnet/orleans/releases))
- The 10.4.2 packages have `net8.0` and `net10.0` assets. There is no `net11.0` asset. A `net11.0` app uses the `net10.0` assets. (verified, package metadata. That this works on .NET 11: not verified)
- Learn says that Orleans 10.0 targets .NET 8, 9, and 10. The 10.4.2 package has no `net9.0` asset. (verified, docs, [Orleans overview](https://learn.microsoft.com/dotnet/orleans/overview))
- Journaling and Durable Jobs are still preview (`10.4.2-alpha.1`). (verified, package metadata)
- Orleans 10.0 added the Orleans Dashboard and stable Redis providers for clustering, persistence, and reminders. (verified, docs, [Orleans overview](https://learn.microsoft.com/dotnet/orleans/overview))
- Orleans 9 added full `CancellationToken` support, the strong-consistency grain directory, memory-based activation shedding, `InProcessTestCluster`, and failure detection in 90 seconds. Since 9.2 the default placement is `ResourceOptimizedPlacement`. (verified, docs, [migration guide](https://learn.microsoft.com/dotnet/orleans/migration-guide))

### Changes in 10.1 to 10.4 that affect code

All from the GitHub release notes (verified, docs):

- 10.1.0: analyzer ORLEANS0014 warns about `ConfigureAwait` in grain code. ADO.NET persistence supports SQLite.
- 10.2.0: Orleans removes the static meter `Orleans.Runtime.Instruments.Meter`. Resolve `OrleansInstruments` from DI. JSON Lines is the default journaling format.
- 10.3.0: the default Newtonsoft.Json grain storage serializer resolves `$type` only for allowed types. A System.Text.Json grain storage serializer exists. Activities use the OpenTelemetry key `rpc.system.name`. `IConfigureGrainContext` runs before the grain constructor.
- 10.4.0: put a `CancellationToken` parameter last in a grain method. Parameter `[Id]` attributes now set the wire IDs. The latency metric is one histogram `orleans-app-requests-latency`. The `orleans-grains` metric uses the tag `grain_type`. Reapply the SQLite scripts.

### Current API

- Hosting: `builder.UseOrleans(...)` on the host builder and `UseOrleansClient` for a client. `SiloHostBuilder` and `ClientBuilder` are Orleans 3.x. (verified, docs, [Orleans overview](https://learn.microsoft.com/dotnet/orleans/overview#orleans-3x-legacy))
- Grain base: `Grain` with a grain interface (`IGrainWithStringKey` and others). A POCO grain implements `IGrainBase`. (verified, docs, [timers and reminders](https://learn.microsoft.com/dotnet/orleans/grains/timers-and-reminders) names POCO grains)
- Serialization: `[GenerateSerializer]` on the type, `[Id(n)]` on each member, `[Alias("...")]` for a stable type name. (verified, docs, [serialization](https://learn.microsoft.com/dotnet/orleans/host/configuration-guide/serialization))
- State: inject `[PersistentState("state", "store")] IPersistentState<T>` in the constructor. `Grain<TState>` is legacy. (verified, docs, [grain persistence](https://learn.microsoft.com/dotnet/orleans/grains/grain-persistence/))
- Timers: `this.RegisterGrainTimer(callback, state, new GrainTimerCreationOptions { DueTime, Period, Interleave, KeepAlive })` returns `IGrainTimer`. The callback gets a `CancellationToken`. `Interleave` is `false` by default. `RegisterTimer` is obsolete. (verified, docs, [timers and reminders](https://learn.microsoft.com/dotnet/orleans/grains/timers-and-reminders))
- Reminders: persistent, a period of minutes or more, not for high-frequency work. (verified, docs, [timers and reminders](https://learn.microsoft.com/dotnet/orleans/grains/timers-and-reminders))
- Streams: `this.GetStreamProvider("name").GetStream<T>(StreamId.Create("ns", key))`. The old form is `GetStream<T>(guid, "ns")`. 10.4 adds `StreamSubscriptionStartPosition.Latest` and `EarliestAvailable`. (verified, docs, [streaming APIs](https://learn.microsoft.com/dotnet/orleans/streaming/streams-programming-apis))
- Testing: `InProcessTestCluster` with `InProcessTestClusterBuilder` is the recommended test host. `TestCluster` with `ISiloConfigurator` classes is the older form. (verified, docs, [unit testing](https://learn.microsoft.com/dotnet/orleans/implementation/testing))
- Aspire: `builder.AddOrleans("default").WithClustering(...).WithGrainStorage("Default", ...)` in the AppHost. The silo calls `builder.UseOrleans()`. A client resource uses `.WithReference(orleans.AsClient())`. The integration needs Orleans 8.1 or later. (verified, docs, [Aspire Orleans integration](https://aspire.dev/integrations/frameworks/orleans/))
- Transactions (`Microsoft.Orleans.Transactions` 10.4.2) and journaled grains (`JournaledGrain`, `ClearLogAsync` since 10.1) exist. (verified, package metadata and release notes. Their current API detail: not verified)

### Scope boundary

- `orleans` owns grains, silo and client setup, serialization, state, timers, reminders, streams, and transactions. It also owns clustering providers, the Orleans Aspire wiring in the silo, Orleans metrics, and Orleans tests.
- `aspire` owns the AppHost side (`AddOrleans`, `WithClustering`). `orleans` points to it.
- `redis`, `efcore`, and `postgresql` own the stores. `orleans` only says which provider package to use.
- `dotnet-testing` owns the test framework. `orleans` owns the test cluster.
- `dotnet-observability` owns OpenTelemetry setup. `orleans` names the Orleans meters and activity sources.

### Top traps

| Old pattern | Current pattern | Source |
|-------------|-----------------|--------|
| `new SiloHostBuilder()` or `new ClientBuilder()` with `ConfigureApplicationParts` | `builder.UseOrleans(silo => ...)` or `UseOrleansClient` on the host builder | [overview](https://learn.microsoft.com/dotnet/orleans/overview#orleans-3x-legacy) |
| `class CounterGrain : Grain<CounterState>` with `[StorageProvider]` | `Grain` plus a constructor parameter `[PersistentState("counter", "store")] IPersistentState<CounterState>` | [grain persistence](https://learn.microsoft.com/dotnet/orleans/grains/grain-persistence/) |
| `[Serializable]` or no attribute on a message type | `[GenerateSerializer]` with `[Id(0)]`, `[Id(1)]` on each member, and `[Alias]` | [serialization](https://learn.microsoft.com/dotnet/orleans/host/configuration-guide/serialization) |
| `RegisterTimer(cb, null, due, period)` | `this.RegisterGrainTimer(cb, state, new GrainTimerCreationOptions { ... })`, with `Interleave = true` only on purpose | [timers](https://learn.microsoft.com/dotnet/orleans/grains/timers-and-reminders#migrate-from-registertimer-to-registergraintimer) |
| A reminder for a tick of seconds | A grain timer, or a reminder with a period of minutes or more | [timers](https://learn.microsoft.com/dotnet/orleans/grains/timers-and-reminders) |
| `GetStream<T>(guid, "ns")` | `GetStream<T>(StreamId.Create("ns", key))` | [streaming APIs](https://learn.microsoft.com/dotnet/orleans/streaming/streams-programming-apis) |
| `new TestClusterBuilder()` with `ISiloConfigurator` classes | `new InProcessTestClusterBuilder()` with delegates | [unit testing](https://learn.microsoft.com/dotnet/orleans/implementation/testing) |
| `.ConfigureAwait(false)` in a grain method | A plain `await`, so the code stays on the grain scheduler (ORLEANS0014) | [10.1.0 release](https://github.com/dotnet/orleans/releases/tag/v10.1.0) |
| `AddGrainCallFilter`, `[Unordered]`, `OrleansConstructorAttribute` | `AddIncomingGrainCallFilter`, no attribute, `GeneratedActivatorConstructorAttribute` | [migration guide](https://learn.microsoft.com/dotnet/orleans/migration-guide) |
| `CancellationToken` not last in a grain method | `CancellationToken` as the last parameter | [10.4.0 release](https://github.com/dotnet/orleans/releases/tag/v10.4.0) |

### Candidate eval scenarios

1. **Counter grain with a flush.** Task: "Write a grain that counts page views for a key. Keep the count in storage `store`. Write the count each 30 seconds. Write the state type." Script checks: `IPersistentState<`, `[PersistentState(`, `RegisterGrainTimer`, `[GenerateSerializer]`, and `[Id(`. The text does not contain `: Grain<`, `RegisterTimer(`, or `ConfigureAwait(false)`.
2. **Test for the grain.** Task: "Write a test that activates the grain in a test cluster and checks the count." Script checks: `InProcessTestCluster`. The text does not contain `TestClusterBuilder` or `ISiloConfigurator`.

## aspire

### Versions

- Aspire 17.0.0 came out on 2026-11-17 (aspire.dev) and is on NuGet since 2026-11-18. The version before it is 13.6.5. There is no 14, 15, or 16. (verified, package metadata, [Aspire releases](https://github.com/microsoft/aspire/releases/tag/v17.0.0))
- The repository is now `microsoft/aspire`. The docs are on aspire.dev. Learn has older pages under `dotnet/aspire`. (verified, docs)
- The minimum .NET version of Aspire 17 is .NET 10. The orchestrated services can target other versions. `Aspire.Hosting.AppHost` 17.0.0 has `net10.0` and `net11.0` assets. (verified, docs and package metadata, [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/))
- The name is "Aspire", not ".NET Aspire". The CLI installs with `curl -sSL https://aspire.dev/install.sh | bash`. (verified, docs, [first app](https://aspire.dev/get-started/first-app/))

### Aspire 17 changes that models do not know

- Aspire 17 deprecates `AddProject<Projects.X>("name")`. The current call is `builder.AddDotnetProject("name", "../X/X.csproj")`. The method also takes a `.cs` file or a folder. It returns `DotnetProjectResource`. (verified, docs, [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/))
- `AddCSharpApp` is obsolete (CS0618). Use `AddDotnetProject`. (verified, docs)
- Aspire 17 deprecates the `Aspire.AppHost.Sdk` (ASPIRE012). A new AppHost uses `Sdk="Microsoft.NET.Sdk"`, `OutputType` `Exe`, and package references to `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`. (verified, docs and the 17.0.0 [starter template](https://github.com/microsoft/aspire/tree/v17.0.0/src/Aspire.ProjectTemplates/templates/aspire-starter))
- C# AppHosts need the Aspire CLI bundle (ASPIRE013). (verified, docs. The detail of ASPIRE013: not verified)
- `dotnet run --operation publish` fails and points to `aspire publish`. (verified, docs)
- Aspire 17 removes `Aspire.RabbitMQ.Client.v6`. `Aspire.RabbitMQ.Client` uses RabbitMQ.Client 7. (verified, docs)
- `aspire update` does not remove the old SDK. (verified, docs)

### Current model

- Starter AppHost of 17.0.0: `AddDotnetProject(...)`, `.WithHttpHealthCheck("/health")`, `.WithExternalHttpEndpoints()`, `.WithReference(x)`, and `.WaitFor(x)`. (verified, docs, [starter AppHost.cs](https://github.com/microsoft/aspire/blob/v17.0.0/src/Aspire.ProjectTemplates/templates/aspire-starter/Aspire-StarterApplication.1.AppHost/AppHost.cs))
- `WithReference` gives the connection information. `WaitFor` delays the start until the resource is healthy. One does not replace the other. (verified, docs, same template)
- ServiceDefaults of 17.0.0: `AddServiceDefaults()` adds service discovery, `AddStandardResilienceHandler()`, OpenTelemetry with `UseOtlpExporter()`, and health checks. `MapDefaultEndpoints()` maps `/health` and `/alive` only in Development. (verified, docs, [ServiceDefaults template](https://github.com/microsoft/aspire/tree/v17.0.0/src/Aspire.ProjectTemplates/templates/aspire-servicedefaults))
- New ServiceDefaults projects reference OpenTelemetry 1.18. The default OTLP request size limit is 64 MiB. (verified, docs, [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/))
- Default images in 17.0: PostgreSQL 18.6, Redis 8.10. (verified, docs)
- Single-file AppHost: Aspire 13.0 supports `apphost.cs` with `#:sdk Aspire.AppHost.Sdk@13.0.0`. The Aspire 17 page does not say how a single-file AppHost works without that SDK. (verified, docs, [upgrade to Aspire 13](https://learn.microsoft.com/dotnet/aspire/get-started/upgrade-to-aspire-13). State in 17: not verified)
- Testing: `Aspire.Hosting.Testing` 17.0.0. `DistributedApplicationTestingBuilder.CreateAsync<Projects.X_AppHost>()`, then `app.ResourceNotifications.WaitForResourceHealthyAsync("name", token)`, then `app.CreateHttpClient("name")`. Use a timeout on each step. Templates: `aspire-mstest`, `aspire-xunit`, `aspire-nunit`. (verified, docs, [first test](https://aspire.dev/testing/write-your-first-test/))
- Deployment: `aspire publish` and `aspire deploy`. Kubernetes and AKS packages are stable in 17 and need Helm 4. azd still deploys to Azure Container Apps. (verified, docs, [Aspire 17 release](https://github.com/microsoft/aspire/releases/tag/v17.0.0) and [ACA deployment](https://learn.microsoft.com/dotnet/aspire/deployment/azd/aca-deployment))
- Dashboard 17: the home page is a resource table. (verified, docs)

### Scope boundary

- `aspire` owns the AppHost, the ServiceDefaults project, and the client integrations (`Aspire.Npgsql`, `Aspire.StackExchange.Redis`, `Aspire.RabbitMQ.Client`). It also owns `WaitFor` and health in the AppHost, the dashboard, `aspire publish` and `deploy`, and `Aspire.Hosting.Testing`.
- `aspnetcore` already sends ServiceDefaults to `aspire`. It keeps health checks and resilience in a service without Aspire.
- `dotnet-observability` keeps the OpenTelemetry rules. `aspire` only says that ServiceDefaults has done the setup.
- `orleans`, `redis`, `postgresql`, and `rabbitmq` keep their engine rules. `aspire` owns only the resource and the connection.

### Top traps

| Old pattern | Current pattern | Source |
|-------------|-----------------|--------|
| `dotnet workload install aspire` | The Aspire CLI (`aspire.dev/install.sh`), and NuGet packages | [upgrade to Aspire 13](https://learn.microsoft.com/dotnet/aspire/get-started/upgrade-to-aspire-13) |
| `<Sdk Name="Aspire.AppHost.Sdk" />`, `<IsAspireHost>true</IsAspireHost>`, or `Sdk="Aspire.AppHost.Sdk/13.x"` | `Sdk="Microsoft.NET.Sdk"` with `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet` packages | [Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| `builder.AddProject<Projects.Api>("api")` with a `ProjectReference` | `builder.AddDotnetProject("api", "../Api/Api.csproj")` | [Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| `WithReference(db)` alone, and a retry loop in the service | `WithReference(db).WaitFor(db)` | [starter template](https://github.com/microsoft/aspire/blob/v17.0.0/src/Aspire.ProjectTemplates/templates/aspire-starter/Aspire-StarterApplication.1.AppHost/AppHost.cs) |
| Connection strings in `appsettings.json` of each service | `builder.AddNpgsqlDataSource("db")` with the resource name from the AppHost | not verified for 17 |
| `Aspire.RabbitMQ.Client.v6` | `Aspire.RabbitMQ.Client` (RabbitMQ.Client 7) | [Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| `dotnet run --operation publish` | `aspire publish` or `aspire deploy` | [Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| An integration test with `Task.Delay` before the first call | `WaitForResourceHealthyAsync` with a timeout | [first test](https://aspire.dev/testing/write-your-first-test/) |

### Candidate eval scenarios

1. **AppHost for an API and a worker.** Task: "Write the AppHost project file and AppHost.cs. Resources: PostgreSQL with database `orders`, Redis, the API project `Orders.Api`, and the worker `Orders.Worker`. Start the API and the worker after the database is ready." Script checks: `Microsoft.NET.Sdk`, `Aspire.Hosting.AppHost`, `AddDotnetProject(`, `AddPostgres(`, and `WaitFor(`. The text does not contain `Aspire.AppHost.Sdk`, `IsAspireHost`, `AddProject<`, or `workload install`.
2. **Integration test.** Task: "Write a test that starts the AppHost. The test checks that GET /health of the API returns 200." Script checks: `DistributedApplicationTestingBuilder`, `WaitForResourceHealthyAsync`, and `CreateHttpClient`. The text does not contain `Task.Delay` or `WebApplicationFactory`.

## blazor

### Versions

- Blazor ships with ASP.NET Core. `Microsoft.AspNetCore.Components.Web` and `Microsoft.AspNetCore.Components.QuickGrid` are 11.0.0 since 2026-11-10. (verified, package metadata)
- Microsoft updated the ASP.NET Core 11 release notes page on 2026-11-18. Some sections still name preview builds. (verified, docs, [ASP.NET Core 11](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11))

### Render modes

The 4 modes of a Blazor Web App (verified, docs, [render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)):

| Mode | Where it runs | Interactive | Input for the decision table |
|------|---------------|-------------|------------------------------|
| Static SSR (no `@rendermode`) | Server | No | Content pages, forms with a full post, pages that read or write cookies |
| `InteractiveServer` | Server, over a SignalR circuit | Yes | Fast start, data and secrets stay on the server, a stable connection, server memory for each user |
| `InteractiveWebAssembly` | Browser | Yes | Offline or high latency, no server state, a larger first download, all code is visible |
| `InteractiveAuto` | Server first, then browser | Yes | Fast first page and client work later, but code must run on both sides |

- Prerendering is on by default for each interactive mode. Disable it with `new InteractiveServerRenderMode(prerender: false)`. (verified, docs, [prerender](https://learn.microsoft.com/aspnet/core/blazor/components/prerender))
- `RendererInfo.Name`, `RendererInfo.IsInteractive`, and `AssignedRenderMode` tell where a component runs. (verified, docs, [render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes))
- `[ExcludeFromInteractiveRouting]` forces a static SSR page in an app with global interactivity. (verified, docs)

### .NET 10 features that models often miss

- `[PersistentState]` on a public property persists the prerendered value. It replaces the code with `PersistentComponentState.RegisterOnPersisting`. `RegisterPersistentService<T>(renderMode)` does the same for a service. With Interactive Server, the same attribute also keeps state across circuits. (verified, docs, [prerendered state persistence](https://learn.microsoft.com/aspnet/core/blazor/state-management/prerendered-state-persistence) and [server state](https://learn.microsoft.com/aspnet/core/blazor/state-management/server))
- Without persisted state, `OnInitializedAsync` runs twice: once for prerendering and once when the component becomes interactive. The UI flickers. (verified, docs, same page)
- Persisted state goes into the HTML. With WebAssembly and Auto, the browser can read it. Do not put secrets in it. (verified, docs, same page)
- Nested and collection validation: call `AddValidation()` and put `[ValidatableType]` on the root form model. Declare the model in a `.cs` file, not in a `.razor` file. Without this, Blazor validates only the top level. There is no error when the metadata is missing. (verified, docs, [validation](https://learn.microsoft.com/aspnet/core/fundamentals/validation))
- For models in the `.Client` project, call `AddValidation()` in an extension method of that project too. (verified, docs, same page)
- `Router` has a `NotFoundPage` parameter, and `NavigationManager.NotFound()` exists. The `<NotFound>` render fragment is not supported in .NET 10. (verified, docs, [ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))
- The template has a `ReconnectModal` component with collocated CSS and JS. The `components-reconnect-state-changed` event and the `retrying` state exist. (verified, docs, same page)
- Auth state for WebAssembly and Auto: the server calls `AddAuthenticationStateSerialization()`, and the `.Client` project calls `AddAuthenticationStateDeserialization()`. Only name and role claims go to the browser unless `SerializeAllClaims = true`. The state does not change for the life of the WebAssembly app. (verified, docs, [Blazor security](https://learn.microsoft.com/aspnet/core/blazor/security/))

### Streaming rendering, enhanced navigation, and forms

- `[StreamRendering]` (since .NET 9 without an argument) sends placeholder content first and the final content later, for static SSR and prerendering. (verified, docs, [rendering](https://learn.microsoft.com/aspnet/core/blazor/components/rendering#streaming-rendering))
- Enhanced navigation is on by default. `data-enhance-nav="false"` turns it off for a link. `Enhance` on `EditForm` or `data-enhance` on `<form>` turns on enhanced form posts. Enhanced form handling does not flow to child forms. (verified, docs, [navigation](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/navigation))
- A static SSR form needs a unique `FormName` and a `[SupplyParameterFromForm]` property. (verified, docs, [forms binding](https://learn.microsoft.com/aspnet/core/blazor/forms/binding))
- `[SupplyParameterFromForm]` does not support `[BindNever]`. Use a dedicated form model against overposting. (verified, docs, same page)

### What changed in .NET 11

From [What's new in ASP.NET Core 11](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11) and the [breaking changes list](https://learn.microsoft.com/aspnet/core/breaking-changes/11/overview) (verified, docs):

- Static SSR forms with `DataAnnotationsValidator` validate in the browser by default.
- Async form validation: `EditContext.RegisterAsyncFieldValidator`, `IsValidationPending`, and `IsValidationFaulted`.
- `[ValidatableType]` and `[SkipValidation]` are no longer experimental. Remove an `ASP0029` suppression.
- `[SupplyParameterFromTempData]` and `[SupplyParameterFromSession]` for static SSR.
- `app.UseAntiforgery()` is no longer in the template. The new cross-origin CSRF middleware protects forms. Without `UseAntiforgery()`, forms get no antiforgery token. ([antiforgery change](https://learn.microsoft.com/aspnet/core/breaking-changes/11/blazor-server-side-rendering-deferred-cross-site-request-forgery-protection))
- Removed APIs: `Router.PreferExactMatches`, `AddDataAnnotationsValidation(EditContext)`, `EnableDataAnnotationsValidation(EditContext)` without `IServiceProvider`, `SignOutSessionStateManager`, and others. ([removed APIs](https://learn.microsoft.com/aspnet/core/breaking-changes/11/blazor-obsolete-apis-removed))
- QuickGrid sort and page controls render as links. The page, the sort, and the direction go into the query string. `OnRowClick` exists.
- `Virtualize` supports items of different heights. `OverscanCount` default goes from 3 to 15.
- Interactive Server: `Circuit.RequestCircuitPauseAsync`, and the `AutoPause` package pauses a hidden tab after 2 minutes. The circuit gets a new `ClaimsPrincipal` without a reconnect.
- WebAssembly: `IHostedService` runs in the browser. Environment variables go into `IConfiguration`. The `Microsoft.AspNetCore.Components.Gateway` dev server replaces `WebAssembly.DevServer`.
- Analyzer BL0010 recommends `InvokeVoidAsync` instead of `InvokeAsync<object>`.
- Enhanced navigation no longer preloads resources.

### Scope boundary

- `blazor` owns components and render modes. It owns prerendering, state, and forms with their validation. It also owns navigation, the circuit, auth state in each mode, QuickGrid, `Virtualize`, and JS interop.
- `aspnetcore` owns `Program.cs` outside Blazor, endpoints, and middleware order. `blazor` owns only `AddRazorComponents`, `MapRazorComponents`, and the Blazor parts of the pipeline.
- `frontend-stack:web-security` owns XSS, CSP, and token storage in the browser. `secure-coding` owns the server-side authentication.
- `test-stack:dotnet-testing` (bUnit, when the owner wants it) and `test-stack:playwright` own the tests.
- `fluentvalidation` owns FluentValidation. `blazor` owns `DataAnnotationsValidator` and `AddValidation` in a form.

### Top traps

| Old pattern | Current pattern | Source |
|-------------|-----------------|--------|
| `Pages/_Host.cshtml`, `AddServerSideBlazor()`, `MapBlazorHub()`, `blazor.server.js` | Blazor Web App: `AddRazorComponents().AddInteractiveServerComponents()`, `MapRazorComponents<App>()`, `blazor.web.js` | [render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes), [integration](https://learn.microsoft.com/aspnet/core/blazor/components/integration) |
| Hosted Blazor WebAssembly (Client, Server, Shared) | Blazor Web App with a `.Client` project and `InteractiveWebAssembly` or `InteractiveAuto` | [fundamentals](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/) |
| `PersistentComponentState.RegisterOnPersisting` and `TryTakeFromJson` | `[PersistentState] public T? Data { get; set; }` and `Data ??= await ...` | [state persistence](https://learn.microsoft.com/aspnet/core/blazor/state-management/prerendered-state-persistence) |
| A data load in `OnInitializedAsync` without persisted state (loads twice) | `[PersistentState]`, or `prerender: false` when the page needs no prerender | [prerender](https://learn.microsoft.com/aspnet/core/blazor/components/prerender) |
| `ObjectGraphDataAnnotationsValidator` or `[ValidateComplexType]` | `AddValidation()` plus `[ValidatableType]` on a model in a `.cs` file | [validation](https://learn.microsoft.com/aspnet/core/fundamentals/validation) |
| `<NotFound>...</NotFound>` in `Router` | `NotFoundPage="typeof(Pages.NotFound)"` and `NavigationManager.NotFound()` | [ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0) |
| JS interop in `OnInitializedAsync` (fails during prerender) | JS interop in `OnAfterRenderAsync(firstRender)`, or check `RendererInfo.IsInteractive` | [render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes) |
| A static SSR form without `FormName` | `FormName` on `EditForm` (or `@formname`) and `[SupplyParameterFromForm]` | [forms binding](https://learn.microsoft.com/aspnet/core/blazor/forms/binding) |
| On .NET 11, a removed `app.UseAntiforgery()` while the app needs tokens | Keep `app.UseAntiforgery()` when forms or APIs need tokens | [antiforgery change](https://learn.microsoft.com/aspnet/core/breaking-changes/11/blazor-server-side-rendering-deferred-cross-site-request-forgery-protection) |

### Candidate eval scenarios

1. **Product list with prerendering.** Task: "Write a routable component `/products` with Interactive Server. It loads the products from `IProductService` one time. It must not load them twice or flicker." Script checks: `[PersistentState]` and `@rendermode InteractiveServer` (or a global mode). The text does not contain `RegisterOnPersisting`, `_Host.cshtml`, or `AddServerSideBlazor`.
2. **Order form with an address.** Task: "Write an order form for static SSR with a nested shipping address and a list of lines. Each nested field must validate." Script checks: `AddValidation(`, `[ValidatableType]`, `FormName`, and `[SupplyParameterFromForm]`. The model is in a `.cs` file. The text does not contain `ObjectGraphDataAnnotationsValidator` or `ValidateComplexType`.

## Facts that are not verified

- Orleans 10.4.2 on a `net11.0` app. The package has no `net11.0` asset. No build ran.
- The Orleans transaction and journaled grain API detail of 10.4.
- The detail of the Aspire CLI bundle requirement (ASPIRE013).
- The single-file AppHost (`apphost.cs`) in Aspire 17, because the SDK that it uses is now deprecated.
- The client integration call `AddNpgsqlDataSource` in Aspire 17. It was not on a page that this research read.
- `AddServerSideBlazor` and `MapBlazorHub` as the old Blazor Server calls. The Learn pages read here name `_Host.cshtml`, but not these calls.
- The C# 15 examples of `net11.md` on SDK 11.0.100. They compiled on RC 2 only.
