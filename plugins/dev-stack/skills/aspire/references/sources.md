# Sources

Checked on 2026-11-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile
- Checked in the 17.0.0 source and templates
- Old guidance that agents read
- Not verified

## Versions

| Item | Version on 2026-11-19 | Source |
|------|-----------------------|--------|
| Aspire (`Aspire.Hosting.*`, client packages, `Aspire.Hosting.Testing`, CLI) | 17.0.0 (on aspire.dev 2026-11-17, on NuGet 2026-11-18) | [Aspire releases](https://github.com/microsoft/aspire/releases/tag/v17.0.0), [nuget.org](https://www.nuget.org/packages/Aspire.Hosting.AppHost) |
| Aspire before 17 | 13.6.5 (2026-11-10). No 14, 15, or 16. | [Aspire releases](https://github.com/microsoft/aspire/releases) |
| Minimum .NET for the AppHost | .NET 10. `Aspire.Hosting.AppHost` 17.0.0 has `net10.0` and `net11.0` assets. | [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| Default images | PostgreSQL 18.6, Redis 8.10 | [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/) |
| OpenTelemetry in new ServiceDefaults | 1.18 | [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/) |

## Documentation

- [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/): `AddDotnetProject`, ASPIRE012, the removed packages, the parameter change, the Kubernetes publisher, `aspire agent init`.
- [Build your first Aspire app](https://aspire.dev/get-started/first-app/): the CLI install.
- [Write your first test](https://aspire.dev/testing/write-your-first-test/): `DistributedApplicationTestingBuilder`, `WaitForResourceHealthyAsync`.
- [Aspire Orleans integration](https://aspire.dev/integrations/frameworks/orleans/): `AddOrleans`, `WithClustering`, `AsClient()`.
- [aspire resource command](https://aspire.dev/reference/cli/commands/aspire-resource/): start, stop, and restart one resource.
- [Deploy to Azure Container Apps with azd](https://learn.microsoft.com/dotnet/aspire/deployment/azd/aca-deployment).
- [microsoft/aspire#22383](https://github.com/microsoft/aspire/issues/22383): `aspire update` writes `Aspire.AppHost.Sdk/17.0.0`.
- [microsoft/aspire#21958](https://github.com/microsoft/aspire/issues/21958): Aspire deprecated `aspire mcp start`, and `aspire agent init` cannot repair a config with an extra argument.
- [Aspire 13.3 breaking changes](https://github.com/microsoft/aspire-skills/blob/main/skills/aspire/references/aspire-13-3-breaking-changes.md): Aspire removed the dashboard MCP server and `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL`.

## Checked by compile

On 2026-11-19 with the .NET SDK 11.0.100 and Aspire 17.0.0, in a scratch folder. The record is `docs/work/stack-split/compile-check.md` of the source repository.

| Claim | Result |
|-------|--------|
| AppHost on `Microsoft.NET.Sdk` with `AddDotnetProject`, `WithReference`, and `WaitFor` | Builds with 0 warnings. No `ProjectReference` is necessary. |
| `builder.AddNpgsqlDataSource("orders")` with `Aspire.Npgsql` 17.0.0 | Builds with 0 warnings. |
| `AddProject<Projects.Api>` on `Aspire.AppHost.Sdk/17.0.0` | Builds with CS0618 ("Use AddDotnetProject(name, path)") and ASPIRE012. |
| `AddProject<Projects.Api>` on `Microsoft.NET.Sdk` | Fails: CS0618 and CS0234 (no `Projects` namespace), also with a `ProjectReference`. |
| ASPIRE013 | Only when `AspireUseCliBundle` is not `true`. The package props set `true`. |
| Build without the Aspire CLI | Downloads a bundle of 379 MB through `dnx` into `~/.aspire`. Needs network access. A failed setup gives ASPIRE009. |
| Start with Docker | DCP started the Redis container and removed it at the stop. |

## Checked in the 17.0.0 source and templates

On 2026-11-19, at the tag `v17.0.0` of github.com/microsoft/aspire. Not built.

- Template `aspire-apphost`: `Microsoft.NET.Sdk`, `OutputType` `Exe`, `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`, and an `aspire.config.json` with the AppHost path.
- Template `aspire-apphost-singlefile`: `#:package Aspire.Hosting.AppHost@...` and `#:package Aspire.Hosting.Dotnet@...`, no `#:sdk`.
- Template `aspire-starter`: `AddDotnetProject`, `WithHttpHealthCheck("/health")`, `WithExternalHttpEndpoints()`, `WithReference` with `WaitFor`.
- Template `aspire-servicedefaults`: `MapDefaultEndpoints()` maps `/health` and `/alive` only when `IsDevelopment()`.
- Template `aspire-mstest`: `CreateAsync<Projects.MyAspireApp_AppHost>()`, a `ProjectReference` to the AppHost, `WaitAsync(DefaultTimeout, ct)` on each step, and a `TestContext` property.
- Source, Orleans: `AddOrleans`, `WithClustering`, `WithGrainStorage`, `AsClient()`.
- Source, other resources: `AddRabbitMQ`, `WithManagementPlugin`, `WithDataVolume`, `AddValkey`, `WithImageTag`.
- Source, client packages: `AddRabbitMQClient`, `AddRedisClient`, `AddRedisDistributedCache`.
- Source, parameters: `AddParameter(..., bool secret)`, `AsOptional()`, `AsRequired()`.

## Old guidance that agents read

- `aspire update` (CLI 17.0): writes the obsolete SDK (#22383).
- Microsoft skill `aspire` 0.0.6 (github.com/microsoft/aspire-skills, 2026-11-17): detects a C# AppHost by `Aspire.AppHost.Sdk` and calls `DotnetProjectResource` experimental. Use its CLI commands, not its project form.
- Learn pages under `dotnet/aspire`: describe Aspire 13 and older, with the workload and `AddProject<>`.
- Model memory (Haiku 5.5 and Sonnet 5.5, probe on 2026-11-19): both wrote `Aspire.AppHost.Sdk/13.0.0`, `AddProject<>`, and 13.0.0 packages. One test used `MSTest.Sdk/3.6.4`, `Version="*"`, and no timeout.

## Not verified

- The type `Projects.X_AppHost` in a test project that references a `Microsoft.NET.Sdk` AppHost. The 17.0.0 test template uses it, but no build checked it.
- The silo side of the Orleans integration (`AddKeyedRedisClient` and `UseOrleans()` with Aspire configuration).
- The replacement of `Aspire.Azure.AI.OpenAI`, and of `WithLetsEncryptProduction` and `WithLetsEncryptStaging`.
- The state of `aspire exec` in 17. Its docs page redirects to `aspire resource`.
- A bundle version that differs from the AppHost version. A 17.0.0 AppHost built and started with CLI 13.6.5 and no warning.
