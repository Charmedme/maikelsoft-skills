# Compile check: phase 2 of the stack split

Date: 2026-11-19. Status: done. This is decision P2-08 of `concept-phase2.md`. The check compiles the key claims of `research-dotnet.md`, `research-mistakes-dotnet.md`, and the dotnet-mcp parts of the API research. A skill rule can use a claim only when this file marks it "pass".

## Contents

- Setup
- dotnet-11 and csharp
- orleans
- aspire
- blazor
- dotnet-mcp
- Corrections to the research

## Setup

- SDK: .NET SDK 11.0.100 from the official `dotnet-install.sh` (`--channel 11.0`), in a scratch folder. The system SDK (10.0.404) did not change.
- Each project has a `global.json` with `11.0.100` and `rollForward` `latestFeature`.
- Build command: `dotnet build --no-incremental --disable-build-servers -p:UseSharedCompilation=false -nodeReuse:false`, then `dotnet build-server shutdown`. One build at a time.
- `DOTNET_CLI_HOME` and `NUGET_PACKAGES` point to the scratch folder, so the template and package caches of the user did not change.
- Each project targets `net11.0`, except where a row says otherwise.

## dotnet-11 and csharp

Code: one console project with each full example of `plugins/dev-stack/skills/csharp/references/net11.md` in its own file (union, closed hierarchy, collection arguments, extension indexer, labeled break). The project file has `<Features>runtime-async=on</Features>` and no `EnablePreviewFeatures`.

```csharp
public union SaveResult(Saved, Conflict, NotFound);

public static string Result(SaveResult result) => result switch
{
    Saved s => $"saved {s.Id}",
    Conflict c => $"conflict: {c.Reason}",
};

public static HashSet<string> Codes(IEnumerable<string> codes) =>
    [with(StringComparer.OrdinalIgnoreCase), .. codes];
```

| Claim | Result | Evidence |
|-------|--------|----------|
| `union` with an exhaustive `switch` compiles on `net11.0` | pass | Build succeeded, 0 warnings. The run prints `not found` and `conflict: x`. |
| A missing case gives a diagnostic | pass, as a warning | `warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'NotFound' is not covered.` It is not an error. |
| A `_ =>` arm hides a missing case | pass | A switch with all 3 cases and a `_` arm gives no warning. A `_` arm with a missing case also gives no warning. |
| `closed record` switch is exhaustive without a discard | pass | A missing `GateOpen` arm gives CS8509 with "the pattern 'GateOpen' is not covered". |
| `[with(StringComparer.OrdinalIgnoreCase), .. items]` | pass | `Codes(["a", "A", "b"])` gives 2 items, and `Contains("B")` is `True`. |
| `[with(capacity: n), .. values]` | pass | Capacity is 6 for 3 values. |
| Extension indexer in an `extension` block | pass | `seq[1]` returns 6. |
| Labeled `break outer;` | pass | Returns `(1, 0)`. |
| `<Features>runtime-async=on</Features>` builds without `EnablePreviewFeatures` | pass | 0 warnings. Reflection shows `<Main>$` with `MethodImplAttributes.Async`, so the feature is on. |
| `app.UseConcurrencyLimiter()` fails on ASP.NET Core 11 | pass | `error CS1061: 'WebApplication' does not contain a definition for 'UseConcurrencyLimiter'`. `AddConcurrencyLimiter` also gives CS1061 on `IServiceCollection`. |
| Replacement: `AddRateLimiter` with `RateLimitPartition.GetConcurrencyLimiter` and `UseRateLimiter` | pass | Build succeeded, 0 warnings. |
| The examples of `net11.md` compile on SDK 11.0.100 | pass | All 5 code blocks compile. No example failed. |

## orleans

Code: a silo with `Host.CreateApplicationBuilder`, `UseOrleans`, `UseLocalhostClustering()`, and `AddMemoryGrainStorage("store")`. Package `Microsoft.Orleans.Server` 10.4.2. The test project uses `MSTest.Sdk/4.5.3` and `Microsoft.Orleans.TestingHost` 10.4.2.

```csharp
[GenerateSerializer, Alias("Views.ViewCountState")]
public sealed class ViewCountState { [Id(0)] public long Count { get; set; } }

public sealed class ViewCounterGrain(
    [PersistentState("views", "store")] IPersistentState<ViewCountState> state) : Grain, IViewCounterGrain
{
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.RegisterGrainTimer(FlushAsync, new GrainTimerCreationOptions
        {
            DueTime = TimeSpan.FromSeconds(30),
            Period = TimeSpan.FromSeconds(30),
        });
        return Task.CompletedTask;
    }
    // FlushAsync(CancellationToken) calls state.WriteStateAsync(cancellationToken).
}

// Test
var builder = new InProcessTestClusterBuilder(1);
builder.ConfigureSilo((options, silo) => silo.AddMemoryGrainStorage("store"));
await using var cluster = builder.Build();
await cluster.DeployAsync();
var count = await cluster.Client.GetGrain<IViewCounterGrain>("home").AddViewAsync();
```

| Claim | Result | Evidence |
|-------|--------|----------|
| Orleans 10.4.2 builds on `net11.0` (only `net8.0` and `net10.0` assets) | pass | Build succeeded. The silo runs and prints `count=1`. A `net10.0` fallback was not necessary. |
| `[PersistentState]` with `IPersistentState<T>` | pass | Compiles and runs. |
| `RegisterGrainTimer` with `GrainTimerCreationOptions` | pass | Compiles. An overload without a state argument takes `Func<CancellationToken, Task>`. |
| `[GenerateSerializer]`, `[Id]`, `[Alias]` on the state | pass | Compiles. The source generator ran. |
| `InProcessTestClusterBuilder` test activates the grain and passes | pass | `dotnet test`: `total: 1, failed: 0, succeeded: 1` (2.7 s). `InProcessTestCluster` supports `await using`. |
| `RegisterTimer` gives an obsolete warning | pass | `warning CS0618: 'Grain.RegisterTimer(...)' is obsolete: 'Use 'this.RegisterGrainTimer(callback, state, new() { DueTime = dueTime, Period = period, Interleave = true })' instead.'` |
| `Grain<TState>` is obsolete | fail | `class LegacyGrain : Grain<ViewCountState>` compiles with no warning. The XML doc says "Base class for a Grain with declared persistent state". Only the docs call it legacy. |
| `ConfigureAwait(false)` in a grain gives ORLEANS0014 | pass | `warning ORLEANS0014: ConfigureAwait in grain code must not use 'false' and must include ConfigureAwaitOptions.ContinueOnCapturedContext`. |
| A write in `OnDeactivateAsync` saves the last count | fail with memory storage | At host stop, the write fails: `No active nodes are compatible with grain memorystorage`. The memory storage grain stops first. The host still stops with exit code 0. |

## aspire

Code: an AppHost on `Microsoft.NET.Sdk` with `OutputType` `Exe` and `net11.0`. Packages: `Aspire.Hosting.AppHost`, `Aspire.Hosting.Dotnet`, `Aspire.Hosting.PostgreSQL`, and `Aspire.Hosting.Redis`, each 17.0.0. The API project uses `Aspire.Npgsql` 17.0.0.

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var db = builder.AddPostgres("pg").AddDatabase("orders");
var cache = builder.AddRedis("cache");
builder.AddDotnetProject("api", "../Api/Api.csproj")
    .WithReference(db).WaitFor(db)
    .WithReference(cache).WaitFor(cache);
builder.Build().Run();
```

| Claim | Result | Evidence |
|-------|--------|----------|
| AppHost on `Microsoft.NET.Sdk` with `AddDotnetProject`, `WithReference`, and `WaitFor` builds | pass | Build succeeded, 0 warnings. No `ProjectReference` is necessary. |
| `builder.AddNpgsqlDataSource("orders")` compiles in the API | pass | Build succeeded, 0 warnings. `NpgsqlDataSource` injects into an endpoint. |
| Aspire 17 marks `AddProject<Projects.Api>` obsolete | pass | `warning CS0618: 'ProjectResourceBuilderExtensions.AddProject<TProject>(IDistributedApplicationBuilder, string)' is obsolete: 'Use AddDotnetProject(name, path) from the Aspire.Hosting.Dotnet package instead. There is no generic AddDotnetProject<TProject>; pass the project path instead of the TProject type.'` |
| `AddProject<Projects.Api>` on `Microsoft.NET.Sdk` | fails to build | CS0618 and `error CS0234: The type or namespace name 'Api' does not exist in the namespace 'Projects'`, also with a `ProjectReference` to the API. |
| `Sdk="Aspire.AppHost.Sdk/17.0.0"` gives ASPIRE012 | pass | `warning ASPIRE012: AppHostOld uses the Aspire.AppHost.Sdk, which is obsolete but remains available for staged migration. Switch the project's Sdk attribute to Microsoft.NET.Sdk, add explicit PackageReference items for Aspire.Hosting.AppHost and Aspire.Hosting.Dotnet, and migrate C# resource ProjectReference items to AddDotnetProject calls.` It still builds. |
| ASPIRE013 blocks a plain `dotnet build` | fail | ASPIRE013 comes only when `AspireUseCliBundle` is not `true`. The package props set it to `true` for a `.csproj`. See the next rows. |
| ASPIRE013 text | shown | With `-p:AspireUseCliBundle=false`: `error ASPIRE013: AppHost requires the Aspire CLI bundle. Remove AspireUseCliBundle or set it to true, and install the matching Aspire CLI from https://get.aspire.dev. ASPIRE013 cannot be suppressed to disable the bundle.` |
| Build on a machine with no Aspire CLI | pass, with a download | With an empty `HOME` and no `aspire` on `PATH`, the build ran the setup through `dnx` and wrote a bundle of 379 MB to `~/.aspire`. Build succeeded in 14 s. It needs network access. When the setup fails, the targets give ASPIRE009 (bundle not found). |
| The bundle version must match | not checked | With Aspire CLI 13.6.5 on `PATH` (bundle 13.5.0), the 17.0.0 AppHost built and started with DCP 13.5.0. No warning. |

The AppHost started one time for about 60 seconds, because Docker is on the machine. DCP started the Redis container and removed it at the stop. No other run used containers.

## blazor

Code: `dotnet new blazor -n Shop --interactivity Server` from SDK 11.0.100. Then a page `/products` and a static SSR page `/order`.

```razor
@page "/products"
@rendermode InteractiveServer
@code {
    [PersistentState]
    public List<Product>? Products { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Products ??= await ProductService.GetAllAsync();
    }
}
```

```csharp
using Microsoft.Extensions.Validation;

[ValidatableType]
public sealed class OrderForm
{
    [Required] public Address ShippingAddress { get; set; } = new();
    [MinLength(1)] public List<OrderLine> Lines { get; set; } = [];
}
// Program.cs: builder.Services.AddValidation();
```

| Claim | Result | Evidence |
|-------|--------|----------|
| The template targets `net11.0` | pass | `<TargetFramework>net11.0</TargetFramework>`. The project file also sets `<BlazorDisableThrowNavigationException>true`. |
| `app.UseAntiforgery()` is not in the generated `Program.cs` | pass | Not in the file. |
| `app.MapStaticAssets()` is in the generated `Program.cs` | pass | Present, before `MapRazorComponents<App>()`. `UseStaticFiles` is not present. |
| The template router uses `NotFoundPage` | pass | `<Router ... NotFoundPage="typeof(Pages.NotFound)">`, and `UseStatusCodePagesWithReExecute("/not-found", ...)`. |
| `[PersistentState]` on a property of an `InteractiveServer` page builds | pass | Build succeeded, 0 warnings. The prerendered HTML has a `Blazor-Server-Component-State` comment. |
| `[ValidatableType]` in a `.cs` file and `AddValidation()` build without ASP0029 | pass | Build succeeded, 0 warnings. The attribute is in `Microsoft.Extensions.Validation`. |
| Nested and collection validation runs in static SSR | pass | A POST with 4 bad nested fields gives 4 messages: Street, PostalCode, Sku, and Quantity. |
| Forms without `UseAntiforgery()` | observed | The form HTML has no antiforgery token field. A same-origin POST returns 200. A POST with `Origin: https://evil.example` and `Sec-Fetch-Site: cross-site` returns 400 with "A valid antiforgery token was not provided with the request." |

One more observation: a POST with no `Lines` fields gave a null `Lines` list, so the page threw `NullReferenceException`. The initializer `= []` did not keep the list. A skill rule for forms with lists must handle null after binding. This is from one run only.

## dotnet-mcp

Code, stdio: `ModelContextProtocol` 2.4.0 on `net11.0` (the package has `net8.0`, `net9.0`, and `net10.0` assets).

```csharp
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();

[McpServerToolType]
public sealed class OrderTools(IOrderStore store)
{
    [McpServerTool(Name = "get_order_status", ReadOnly = true, Idempotent = true, OpenWorld = false), Description("Get the status of an order.")]
    public async Task<string> GetOrderStatus([Description("The order ID.")] string orderId, CancellationToken cancellationToken) => ...;
}
```

Code, HTTP: `ModelContextProtocol.AspNetCore` 2.4.0 and `Microsoft.AspNetCore.Authentication.JwtBearer` 11.0.0.

```csharp
builder.Services.AddAuthentication(o =>
{
    o.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o => { o.Authority = authority; o.TokenValidationParameters = new() { ValidAudience = serverUrl, ValidIssuer = authority }; })
.AddMcp(o => o.ResourceMetadata = new() { AuthorizationServers = { authority }, ScopesSupported = ["orders.read"] });
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless)
    .WithToolsFromAssembly();
app.MapMcp("/mcp").RequireAuthorization();
```

| Claim | Result | Evidence |
|-------|--------|----------|
| Stdio server with `WithStdioServerTransport().WithToolsFromAssembly()` builds and runs | pass | 0 warnings. `initialize`, `tools/list`, and `tools/call` over stdin work. Each line on stdout is JSON-RPC. The logs (38 lines) went to stderr. |
| `[McpServerTool(ReadOnly = true)]` sets the hint | pass | `tools/list` gives `readOnlyHint: true`, `idempotentHint: true`, `openWorldHint: false`. The write tool with `Destructive = true` gives `destructiveHint: true`. |
| An `McpException` message reaches the client | pass | Result: `isError: true`, text "An error occurred invoking 'get_order_status': Order 9 not found." |
| HTTP server with `WithHttpTransport()`, `MapMcp().RequireAuthorization()`, and `.AddMcp(...)` builds | pass | Build succeeded, 0 warnings. |
| 401 with resource metadata | pass | `WWW-Authenticate: Bearer resource_metadata="http://127.0.0.1:5191/.well-known/oauth-protected-resource/mcp"`. The header has no `scope` parameter by default. |
| Resource metadata document | pass | `/.well-known/oauth-protected-resource/mcp` returns `resource` (the request URL with `/mcp`), `authorization_servers`, `bearer_methods_supported`, and `scopes_supported`. |
| Property `SessionMode` exists, default `Stateless` | pass | `HttpServerTransportOptions.SessionMode`. Enum `HttpServerSessionMode`: `Stateless`, `Stateful`, `StatefulForInitializeClients`. The XML doc gives the default `Stateless` "as of the 2026-07-28 protocol revision". |
| Property `Stateless` exists | pass | `bool`, default `true`. Not obsolete: `o.Stateless = true;` gives no warning. |
| Property `EnableLegacySse` is obsolete | pass | `warning MCP9004: 'HttpServerTransportOptions.EnableLegacySse' is obsolete: 'Legacy SSE transport has no built-in request backpressure and should only be used with completely trusted clients in isolated processes. Use Streamable HTTP instead.'` |
| Stateful-only options are obsolete | pass | `IdleTimeout` gives `warning MCP9006: ... Stateful Streamable HTTP mode is a back-compat-only escape hatch for 2025-11-25 protocol revision clients and earlier.` `MaxIdleSessionCount`, `EventStreamStore`, and `SessionMigrationHandler` are on the same type. |
| Other names on 2.4.0 | listed | `McpAuthenticationOptions`: `ResourceMetadata`, `ResourceMetadataUri`, `Events`. `ProtectedResourceMetadata`: `Resource`, `AuthorizationServers`, `ScopesSupported`, `WwwAuthenticateScope`, and others. |
| The `mcpserver` template is in SDK 11 | pass | `dotnet new list` shows "MCP Server App, mcpserver" with no install. The SDK holds `microsoft.mcpserver.projecttemplates.11.0.11.0.0.nupkg`. |
| The template references SDK 1.2.0 | fail on SDK 11 | The SDK 11 template references `ModelContextProtocol` 2.1.0 (local) and `ModelContextProtocol.AspNetCore` 2.1.0 (remote), with `net11.0`. The remote `Program.cs` sets `options.Stateless = true`. `server.json` uses schema 2025-10-17. |
| `dotnet new install Microsoft.McpServer.ProjectTemplates` | installs 1.2.1, but SDK 11 does not use it | The NuGet package 1.2.1 references 1.2.0 and `net10.0`, with precedence 1021. The SDK template has precedence 11000, so `dotnet new mcpserver` still gives 2.1.0. |

## Corrections to the research

1. **mcpserver template (concept, P2-05).** On SDK 11, the template comes with the SDK and references `ModelContextProtocol` 2.1.0, not 1.2.0. No `dotnet new install` is necessary. The NuGet template 1.2.1 (1.2.0 references) applies only on SDK 10. The rule stays: after `dotnet new mcpserver`, update to 2.4.0. The rule must name both start versions.
2. **ASPIRE013 (aspire, not verified before).** It does not block a plain `dotnet build`. It is an error only when a project sets `AspireUseCliBundle` to a value other than `true`. When the machine has no Aspire CLI, the build downloads a bundle of about 380 MB through `dnx` into `~/.aspire`. A build without network access can fail with ASPIRE009.
3. **`AddProject<Projects.X>` (aspire).** On a `Microsoft.NET.Sdk` AppHost it does not compile (CS0234), because that SDK does not generate the `Projects` types. On `Aspire.AppHost.Sdk/17.0.0` it compiles with CS0618 and ASPIRE012.
4. **`AddNpgsqlDataSource` in Aspire 17 (not verified before).** It compiles with `Aspire.Npgsql` 17.0.0. Mark it verified.
5. **Orleans on `net11.0` (P2-02, not verified before).** A silo on `net11.0` builds, runs, and passes an `InProcessTestCluster` test with Orleans 10.4.2. The skill can say "verified by build" and drop "not verified".
6. **`Grain<TState>` (orleans).** It has no obsolete attribute and no analyzer warning. A rule can call it legacy from the docs, but no build diagnostic shows it. A `stackcheck` regex is the only automatic check.
7. **Write on deactivation (orleans).** With memory storage, the write in `OnDeactivateAsync` fails at host stop. The rule "write on deactivation too" needs a real storage provider. Do not use memory storage to test it.
8. **Missing union case (dotnet-11, csharp).** It is warning CS8509, not an error. A rule that depends on it must make CS8509 an error (`<WarningsAsErrors>` or `TreatWarningsAsErrors`). It must also forbid a `_` arm.
9. **`csharp/references/net11.md`.** Each example compiles on SDK 11.0.100. Change the status line, and change "preview opt-in" for runtime async: the build proves that `<Features>runtime-async=on</Features>` works without `EnablePreviewFeatures`.
10. **MCP `Stateless` (dotnet-mcp).** The `Stateless` property is not obsolete and gives no warning on 2.4.0. A check for `Stateless = true` is not an old pattern. Only `Stateless = false`, `SessionMode = HttpServerSessionMode.Stateful`, and `EnableLegacySse` (MCP9004) are old patterns.
11. **MCP 401 header (dotnet-mcp).** By default the `WWW-Authenticate` header has no `scope` parameter. The spec recommends one. `ProtectedResourceMetadata.WwwAuthenticateScope` exists for it. The `resource` value is the request URL with the MCP path. `ValidAudience` must match that value, or the skill must set `Resource` explicitly.
12. **Blazor antiforgery (blazor).** Without `UseAntiforgery()`, a same-origin form POST without a token succeeds. A cross-site POST gets 400. The research says "forms get no antiforgery token". This is correct, but the forms are not unprotected.
