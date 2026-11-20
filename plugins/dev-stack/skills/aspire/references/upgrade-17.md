# Upgrade an AppHost from 13.x to 17

Do these steps only after the human said "yes" to AHS-01. Aspire 17 follows 13.6.5. There is no 14, 15, or 16. Make the change in one branch. Keep the services on their .NET version, because only the AppHost needs .NET 10 or later.

## Contents

- Why `aspire update` is not enough
- Steps
- Changes in 17 that the build does not show

## Why `aspire update` is not enough

`aspire update` changes the package versions to 17.0.0. It also changes the SDK to `Aspire.AppHost.Sdk/17.0.0`, not to `Microsoft.NET.Sdk` (microsoft/aspire#22383). That SDK is obsolete. The build passes with warning ASPIRE012, and `AddProject<>` gives CS0618. An agent that stops after `aspire update` keeps the old form.

## Steps

1. To update the CLI, run `curl -sSL https://aspire.dev/install.sh | bash`. Done when `aspire --version` shows 17.
2. To change the package versions, run `aspire update` in the folder of the AppHost. Done when each `Aspire.*` package shows 17.0.0.
3. In the AppHost project file, make these changes:
   a. Change the `Sdk` attribute to `Microsoft.NET.Sdk`. Remove each `<Sdk Name="Aspire.AppHost.Sdk" ... />` line.
   b. Remove `<IsAspireHost>true</IsAspireHost>`. Add `<OutputType>Exe</OutputType>`.
   c. Add a `PackageReference` to `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`, both 17.0.0.
   d. Remove each `ProjectReference` to a service project.

   Done when the project file matches `SKILL.md`, rule 2.
4. In `AppHost.cs`, replace each `AddProject<Projects.Orders_Api>("api")` with `AddDotnetProject("api", "../Orders.Api/Orders.Api.csproj")`. Replace each `AddCSharpApp` call too. Keep the resource names, because the services use them for their connections. Done when the AppHost has no `AddProject<` and no `AddCSharpApp`.
5. For a single-file `apphost.cs`, replace `#:sdk Aspire.AppHost.Sdk@13.x` with `#:package Aspire.Hosting.AppHost@17.0.0` and `#:package Aspire.Hosting.Dotnet@17.0.0`. The 17.0.0 template `aspire-apphost-singlefile` uses this form. It was not built. Done when the file has no `#:sdk` line.
6. Replace each package that 17 removed:
   - `Aspire.RabbitMQ.Client.v6`: `Aspire.RabbitMQ.Client`. The service code moves to RabbitMQ.Client 7 (`dev-stack:rabbitmq`, section "Version 6 to 7").
   - `Aspire.MongoDB.Driver.v2`, `Aspire.Azure.AI.OpenAI`, `Aspire.Azure.AI.Inference`: run `aspire integration search <name>`. The 17.0.0 source has `Aspire.Azure.AI.Extensions.OpenAI` and `Aspire.OpenAI`. Which one replaces `Aspire.Azure.AI.OpenAI` is not verified: ask the human.
   - `WithLetsEncryptProduction` and `WithLetsEncryptStaging`: removed. TODO: the replacement is not verified. Ask the human.

   Done when `dotnet restore` shows no NU1101 and no NU1102.
7. Find each required parameter whose value is `""`. Give it a value, or call `AsOptional()`. In 17, `""` throws `MissingParameterValueException` at the start. Done when `aspire run` starts without that exception.
8. Fix the agent configs. Change `"args": ["mcp", "start"]` to `"args": ["agent", "mcp"]` by hand in `.mcp.json`, `.vscode/mcp.json`, and other MCP configs. Remove `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL`. Then run `aspire doctor`. Done when `aspire doctor` reports no deprecated MCP config.
9. Change each script and pipeline step that runs `dotnet run --operation publish` to `aspire publish`. Add the CLI install to each CI job that builds the AppHost (`SKILL.md`, rule 3). Done when no file contains `--operation publish`.
10. To build, run `dotnet build -warnaserror`. Done when the build shows no ASPIRE012 and no CS0618.
11. To run the tests, run `dotnet test`. Done when the summary shows 0 failed. Each test project that calls `CreateAsync<Projects.X_AppHost>()` must build after step 3. Report each build error under "Open points" with its text.

## Changes in 17 that the build does not show

- New ServiceDefaults projects reference OpenTelemetry 1.18. An existing ServiceDefaults project keeps its versions. Update them in the same branch, or name them under "Open points".
- The default images are PostgreSQL 18.6 and Redis 8.10. PostgreSQL 18 does not start on a local volume of PostgreSQL 17 or older. Pin the old tag with `WithImageTag`. Or, after the human agrees, remove the local volume.
- The dashboard home page is now a resource table.
- `aspire agent init` registers the Aspire plugin for the agent. It does not copy skill files. Remove old copies of the Aspire skill from the repository after the human agrees.
