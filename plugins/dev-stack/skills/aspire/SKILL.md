---
name: aspire
description: Run first for each task that adds, changes, or tests an Aspire AppHost - a resource in it (PostgreSQL, Redis, RabbitMQ, Orleans), WithReference and WaitFor, a connection that comes from Aspire instead of appsettings.json, ServiceDefaults, or an integration test that starts the AppHost. Aspire 17 (2026-11-18) changed the AppHost form, so the form in training data is old. Covers the Aspire CLI, Microsoft.NET.Sdk with AddDotnetProject, parameters and secrets, client integrations, the dashboard, aspire agent mcp, aspire publish and deploy, and the upgrade of a 13.x AppHost. Signals: AppHost.cs, apphost.cs, Aspire.Hosting packages, Aspire.AppHost.Sdk, AddProject, AddServiceDefaults, DistributedApplicationTestingBuilder, an aspire command. Not for compose files or Kubernetes manifests by hand, OpenTelemetry without Aspire (dev-stack:dotnet-observability), health checks without Aspire (dev-stack:aspnetcore), or engine rules (dev-stack:postgresql, dev-stack:redis, dev-stack:rabbitmq, dev-stack:orleans).
metadata:
  verified: "2026-11-19"
  versions: "Aspire 17.0.0 (Aspire.Hosting.*, Aspire.Hosting.Testing, client packages, Aspire CLI), Aspire 13.6.5 (the line before 17), .NET SDK 10.0.404 and 11.0.100"
---

# Aspire

Aspire runs a set of .NET services and their resources (databases, caches, brokers) on a developer machine, in tests, and in a deployment. This skill owns the AppHost, the ServiceDefaults project, the client integrations in each service, the dashboard, the Aspire CLI, `Aspire.Hosting.Testing`, and `aspire publish` and `deploy`. The rules inside each engine stay in `dev-stack:postgresql`, `dev-stack:redis`, `dev-stack:rabbitmq`, and `dev-stack:orleans` (the silo side). The OpenTelemetry rules stay in `dev-stack:dotnet-observability`. The test framework is in `test-stack:dotnet-testing`.

Aspire 17.0.0 came out on 2026-11-18. Models and old guidance write the 13.x form. For the project form, do not trust these sources:

- Your memory.
- Learn pages under `dotnet/aspire`.
- The Microsoft skill `aspire` 0.0.6. It detects an AppHost by `Aspire.AppHost.Sdk`.
- `aspire update`. It writes the deprecated SDK.

To read a current API, run `aspire docs search <topic>`.

## First

1. Read `references/stack-rules.md`.
2. Find the Aspire version and the AppHost form. Read the `Sdk` attribute of the AppHost project and the `Aspire.Hosting.*` versions. Also read the `#:sdk` or `#:package` lines of an `apphost.cs`, and run `aspire --version`.
   - `Microsoft.NET.Sdk` with `Aspire.Hosting.AppHost` 17.x: use this file.
   - `Aspire.AppHost.Sdk/13.x`, or `IsAspireHost`: an existing 13.x AppHost keeps its form (AHS-01). Write new resources in the 13.x form (`AddProject<Projects.X>`). The move to 17 is a decision for the human. After a "yes", follow `references/upgrade-17.md`.
   - `Aspire.AppHost.Sdk/17.0.0`: somebody ran `aspire update`, and the move is half done (microsoft/aspire#22383). Tell the human, and finish it with `references/upgrade-17.md`.
   - No AppHost: make one only when the task asks for it.
3. List each service project, each resource, and each test project that uses `Aspire.Hosting.Testing`.

Done when you can name the Aspire version, the AppHost form, each resource with its name, and the answer to AHS-01.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Version and form | Aspire 17 with `Microsoft.NET.Sdk` for a new AppHost. A 13.x AppHost keeps its form. | AHS-01 |
| Health endpoints | `MapDefaultEndpoints()` only in Development. Production probes come from `dev-stack:aspnetcore` (API-16). | AHS-02 |
| Local data | `WithDataVolume()` on each database and broker | AHS-03 |
| Deployment | The AppHost is for local runs and tests. A deploy target only after a decision of the team. | AHS-04 |
| Secrets | `AddParameter(name, secret: true)`, with values in user secrets or `aspire secret set` | AHS-05 |
| Agent access | `aspire agent init --mcp` with project scope, on the developer machine only | AHS-06 |
| Tests | `WebApplicationFactory` and Testcontainers for one API. `Aspire.Hosting.Testing` for a few smoke tests across services. | AHS-07 |

## Existing code

In Aspire code, the defects of stack rule 3 include these. Each one runs on the machine of the author and fails later or on another machine.

| Defect | Rule |
|--------|------|
| `WithReference(x)` without `WaitFor(x)`, and a retry loop in the service | 5 |
| A connection string in `appsettings.json` of a service that the AppHost also gives | 10 |
| `MapDefaultEndpoints()` without the `IsDevelopment()` check, on a public port | 9 |
| `Task.Delay` in an Aspire test | 13 |
| `<AspireUseCliBundle>false</AspireUseCliBundle>` | 3 |
| `"args": ["mcp", "start"]` in an MCP config | 12 |

## Rules

### Install and project form

1. Install the Aspire CLI: `curl -sSL https://aspire.dev/install.sh | bash`. Aspire does not use a .NET workload since version 9. The NuGet packages and the CLI replace it. Done when `aspire --version` shows 17.
2. A new AppHost uses `Microsoft.NET.Sdk`, `OutputType` `Exe`, and two packages. It has no `ProjectReference` to the services. A build on SDK 11.0.100 proved this form:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <OutputType>Exe</OutputType>
       <TargetFramework>net10.0</TargetFramework>
       <Nullable>enable</Nullable>
       <ImplicitUsings>enable</ImplicitUsings>
     </PropertyGroup>
     <ItemGroup>
       <PackageReference Include="Aspire.Hosting.AppHost" Version="17.0.0" />
       <PackageReference Include="Aspire.Hosting.Dotnet" Version="17.0.0" />
       <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="17.0.0" />
       <PackageReference Include="Aspire.Hosting.Redis" Version="17.0.0" />
     </ItemGroup>
   </Project>
   ```

   Use a fixed version for each `Aspire.*` package, in `Directory.Packages.props` when the repository has one. All `Aspire.*` packages have the same version. A single-file `apphost.cs` uses `#:package Aspire.Hosting.AppHost@17.0.0` and `#:package Aspire.Hosting.Dotnet@17.0.0`, not `#:sdk`.
3. Do not set `AspireUseCliBundle`. With `false`, the build stops with ASPIRE013. On a machine without the Aspire CLI, the build downloads a bundle of about 380 MB through `dnx` into `~/.aspire`. Without network access, it fails with ASPIRE009. So install the CLI in each CI job that builds the AppHost, or cache `~/.aspire`.
4. Add each .NET service with `AddDotnetProject("name", "../Service/Service.csproj")`. On `Microsoft.NET.Sdk`, `AddProject<Projects.X>` does not compile (CS0234), because that SDK makes no `Projects` types. `AddCSharpApp` is obsolete (CS0618). Use a lowercase resource name. The service uses the same name for its connection.

### Resources, references, and start order

5. Give each dependency both `WithReference(x)` and `WaitFor(x)`. `WithReference` gives the connection string or the endpoint. `WaitFor` starts the service only when `x` is healthy. One does not replace the other. Without `WaitFor`, the service starts before the database accepts connections.
6. Give each HTTP service `WithHttpHealthCheck("/health")`, so that `WaitFor` on that service waits for its health. Give `WithExternalHttpEndpoints()` only to a service that users open.

   ```csharp
   var builder = DistributedApplication.CreateBuilder(args);

   var postgres = builder.AddPostgres("postgres").WithDataVolume();
   var orders = postgres.AddDatabase("orders");
   var cache = builder.AddRedis("cache");

   var api = builder.AddDotnetProject("api", "../Orders.Api/Orders.Api.csproj")
       .WithHttpHealthCheck("/health")
       .WithExternalHttpEndpoints()
       .WithReference(orders).WaitFor(orders)
       .WithReference(cache).WaitFor(cache);

   builder.AddDotnetProject("worker", "../Orders.Worker/Orders.Worker.csproj")
       .WithReference(orders).WaitFor(orders)
       .WithReference(api).WaitFor(api);

   builder.Build().Run();
   ```

7. For RabbitMQ, Orleans, and the other resources, read `references/integrations.md`. To find a package, run `aspire integration search <name>`, then `aspire add <package>`. A new package needs the approval of the human (stack rule 5).

### Parameters and secrets

8. Add each secret with `builder.AddParameter("smtp-password", secret: true)` and pass it with `WithEnvironment`. Put the local value in the user secrets of the AppHost (`Parameters:smtp-password`) or set it with `aspire secret set`. Never put the value in an `appsettings*.json` file in git. In Aspire 17, an empty string does not satisfy a required parameter: the AppHost throws `MissingParameterValueException`. Use `AsOptional()` for a value that can be missing. `AddPostgres` and `AddRedis` make their own password parameter. Do not write a password for them.

### ServiceDefaults and the services

9. Each service calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`. ServiceDefaults adds service discovery, `AddStandardResilienceHandler()`, OpenTelemetry with `UseOtlpExporter()`, and the health checks. Do not add these a second time in the service. `MapDefaultEndpoints()` maps `/health` and `/alive` only in Development. Keep that check: the endpoints show the state of each dependency. Production probes follow AHS-02.
10. In each service, use the client integration with the resource name of the AppHost. The connection string comes from `WithReference`. A build proved the first call.

    | Resource | Client package | Call in the service |
    |----------|----------------|---------------------|
    | PostgreSQL database | `Aspire.Npgsql` | `builder.AddNpgsqlDataSource("orders");` |
    | PostgreSQL with EF Core | `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | `builder.AddNpgsqlDbContext<OrdersDb>("orders");` |
    | Redis | `Aspire.StackExchange.Redis` | `builder.AddRedisClient("cache");` |
    | Redis as `IDistributedCache` | `Aspire.StackExchange.Redis.DistributedCaching` | `builder.AddRedisDistributedCache("cache");` |
    | RabbitMQ | `Aspire.RabbitMQ.Client` (RabbitMQ.Client 7) | `builder.AddRabbitMQClient("messaging");` |

    The client call is on `builder`, not on `builder.Services`. `builder.Services.AddNpgsqlDataSource(...)` is the plain Npgsql call: it gets no Aspire health check and no Aspire telemetry. The rules for the client object (one multiplexer, one data source, the cache layer) are in `dev-stack:redis` and `dev-stack:postgresql`.

### Dashboard and agents

11. `aspire run` starts the AppHost and prints the dashboard URL with a login token. The home page is a resource table with the state, the logs, and the traces of each resource. Read the logs and the traces there before you change code for a failure. The dashboard has no MCP server and no Copilot or AI Assistant: Aspire removed them in 13.3 and 13.5.
12. To give an agent the live state of the resources, run `aspire agent init --mcp` (AHS-06). The MCP server command is `aspire agent mcp`. Aspire deprecated a config with `"args": ["mcp", "start"]`, and `aspire agent init` cannot repair it. Change it by hand. Remove `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL`. Without MCP, the agent loop is `aspire start --non-interactive --isolated --apphost <path>`, `aspire wait <resource>`, then `aspire describe`, `aspire logs <resource>`, and `aspire otel traces`. `aspire doctor` finds stale configs.

### Tests

13. Use `Aspire.Hosting.Testing` for a few smoke tests that start the whole AppHost (AHS-07). The tests need Docker. Give each wait a timeout with `WaitAsync`. Then a resource that never gets healthy fails the test, and the run does not hang. Do not wait with `Task.Delay`. A fixed wait is too short on a slow CI agent and wastes time on a fast one. The project setup (`MSTest.Sdk`, the runner) is in `test-stack:dotnet-testing`. Read `references/integrations.md`, section "Tests", for the full test.

    ```csharp
    var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Orders_AppHost>();
    await using var app = await appHost.BuildAsync(ct).WaitAsync(StartTimeout, ct);
    await app.StartAsync(ct).WaitAsync(StartTimeout, ct);
    await app.ResourceNotifications.WaitForResourceHealthyAsync("api", ct).WaitAsync(StartTimeout, ct);
    using var http = app.CreateHttpClient("api");
    ```

### Publish and deploy

14. Make deployment files with `aspire publish`, and deploy with `aspire deploy`. `dotnet run --operation publish` fails in 17 and points to `aspire publish`. Docker Compose (`Aspire.Hosting.Docker`), Kubernetes (`Aspire.Hosting.Kubernetes`, needs Helm 4), and Azure Container Apps (azd or `Aspire.Hosting.Azure.AppContainers`) are targets. The target is decision AHS-04. Run `aspire deploy` or `azd up` only when the human asks for it, because it changes a live environment. Review the files of `aspire publish` like code.

### Upgrade from 13.x

15. Upgrade only after the human said "yes" to AHS-01. Read `references/upgrade-17.md` before you start. `aspire update` changes the versions but writes `Sdk="Aspire.AppHost.Sdk/17.0.0"`, which is obsolete (ASPIRE012). The manual steps after it are in that file.

## Never write

| Wrong | Correct |
|-------|---------|
| `dotnet workload install aspire` | `curl -sSL https://aspire.dev/install.sh \| bash` |
| `<Project Sdk="Aspire.AppHost.Sdk/13.0.0">` or `/17.0.0` in a new AppHost | `<Project Sdk="Microsoft.NET.Sdk">` with `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet` |
| `<Sdk Name="Aspire.AppHost.Sdk" Version="..." />` and `<IsAspireHost>true</IsAspireHost>` | The same `Microsoft.NET.Sdk` form |
| `builder.AddProject<Projects.Orders_Api>("api")` with a `ProjectReference` in a 17 AppHost | `builder.AddDotnetProject("api", "../Orders.Api/Orders.Api.csproj")` and no `ProjectReference` |
| `Version="13.0.0"` or `Version="*"` on an `Aspire.*` package of a new project | `Version="17.0.0"` on each `Aspire.*` package |
| `.WithReference(db)` alone | `.WithReference(db).WaitFor(db)` |
| An AppHost without `WithHttpHealthCheck` and services without `AddServiceDefaults()` | Rules 6 and 9 |
| `"ConnectionStrings": { "orders": "Host=localhost;..." }` in a service | `WithReference(orders)` in the AppHost |
| `builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("orders")!)` in an Aspire service | `builder.AddNpgsqlDataSource("orders");` |
| `if (true) app.MapDefaultEndpoints();` or the check removed "for Kubernetes probes" | The Development check, and the probes of API-16 |
| `await Task.Delay(TimeSpan.FromSeconds(10));` before the first call in a test | `WaitForResourceHealthyAsync("api", ct).WaitAsync(timeout, ct)` |
| `WaitForResourceHealthyAsync("api")` with no token and no timeout | `.WaitAsync(timeout, ct)` on each wait |
| `"args": ["mcp", "start"]` | `"args": ["agent", "mcp"]`, or run `aspire agent init --mcp` |
| `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL`, the dashboard MCP server, or the dashboard Copilot | `aspire agent init --mcp` |
| `dotnet run --operation publish` or `--publisher manifest` | `aspire publish` |
| `Aspire.RabbitMQ.Client.v6`, `Aspire.MongoDB.Driver.v2`, `Aspire.Azure.AI.OpenAI` | Removed in 17. `Aspire.RabbitMQ.Client`, or `aspire integration search` |
| `<AspireUseCliBundle>false</AspireUseCliBundle>` to build in CI without the CLI | Install the CLI in CI (rule 3) |
| A password in `appsettings.json` of the AppHost, or `""` for a required parameter | `AddParameter(name, secret: true)` and a user secret, or `AsOptional()` |

## Check

1. To run the rules of this skill, run `python3 <this skill folder>/scripts/stackcheck.py --root <repo>`. Done when the summary shows 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>` (for example AHS-OLD-SDK on a 13.x AppHost that keeps its form).
2. To build, run `dotnet build -warnaserror` on the AppHost and each service. Done when the build shows 0 warnings. ASPIRE012 and CS0618 show the old form.
3. Optional: to see the start order, run `aspire start --non-interactive --isolated --apphost <path>`, then `aspire wait api`. Done when each resource is healthy. Stop the AppHost after the check.
4. To run the tests, run `dotnet test`. Done when the summary shows 0 failed.

## Review checklist

The full list is in `references/review-checklist.md`.

1. The AppHost form matches AHS-01: 17 with `Microsoft.NET.Sdk`, or an untouched 13.x form. tool (AHS-OLD-SDK, ASPIRE012)
2. `AddDotnetProject`, not `AddProject<>` or `AddCSharpApp`, in a 17 AppHost. tool (AHS-ADD-PROJECT, CS0618, CS0234)
3. Each `WithReference` to a resource that can be unhealthy has a `WaitFor`. judge
4. Each HTTP service has `WithHttpHealthCheck`, `AddServiceDefaults()`, and `MapDefaultEndpoints()` with the Development check. judge
5. Secrets are secret parameters, not values in a file in git. judge
6. Services use the client integrations with the resource name, and no connection string in `appsettings.json`. judge
7. No install step with the workload. tool (AHS-WORKLOAD)
8. No `Task.Delay` in an Aspire test, and each wait has a timeout. tool (AHS-TEST-DELAY), judge
9. MCP configs use `aspire agent mcp`, and no dashboard MCP variable. tool (AHS-MCP-START, AHS-DASHBOARD-MCP)
10. Each `Aspire.*` package has the same fixed version. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Also give:

- Under "Decisions": AHS-01 for each change, and each other AHS decision that the change uses.
- Under "Packages": each `Aspire.*` package and its version, and each package that 17 removed.
- Under "Open points": each manual step of `references/upgrade-17.md` that you could not do, and each fact that you could not check by build.

## References

- `references/decisions.md`: read before new code needs an AHS decision.
- `references/integrations.md`: read when you add RabbitMQ, Orleans, EF Core migrations, or another resource, or write an Aspire test.
- `references/upgrade-17.md`: read before you move a 13.x AppHost to 17, or when an AppHost has `Aspire.AppHost.Sdk/17.0.0`.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources, the compile check, and the old guidance that agents read.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
