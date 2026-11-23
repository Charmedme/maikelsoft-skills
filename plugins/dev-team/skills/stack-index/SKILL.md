---
name: stack-index
description: Routing table from a file type, a package, or a technology to the stack skill (dev-stack, frontend-stack, or test-stack) that holds its rules, with the state of each skill (available or planned). Preloaded by the dev-team agents. Use when a dev-team agent must know which stack skill to load before it designs, builds, or reviews code.
user-invocable: false
---

# Stack index

Find the stack skill for each technology that the work touches. Load it with the Skill tool before you design, build, or review that part. Read its version gate first. Write all text in the language rules of `references/language.md`.

## How to use

1. List the files that the work touches, and the packages in the project files (`*.csproj`, `Directory.Packages.props`, `package.json`).
2. Find each one in the table. One file can need more than one skill (an endpoint that writes to the database needs `aspnetcore` and `efcore`).
3. For Angular, load the official skill `angular-developer` first, then `frontend-stack:angular`. When the rules differ, `frontend-stack:angular` wins. When `angular-developer` is not installed, give the human the command `npx skills add https://github.com/angular/skills`, and go on with `frontend-stack:angular`.
4. Always load `dev-stack:secure-coding` for code that takes input, handles a secret, or adds a package. For browser code, also load `frontend-stack:web-security`.
5. For HTML, CSS, or client-side JavaScript, also load `frontend-stack:modern-web`. It sends the work to Modern Web Guidance of Google Chrome.
6. A skill of a plugin that is not installed: give the human the install command once, for example `claude plugin install frontend-stack@maikelsoft-skills`. Write one line in your report, for example "No stack skill for X. I used the official docs of version Y." Check the current facts with a look-up. Do not stop the run for it.

## Available skills (dev-stack 2.1.0, frontend-stack 1.1.1, test-stack 1.1.0)

| Signal | Skill |
|--------|-------|
| `*.cs`, `*.csproj`, `Directory.Build.props`, `.editorconfig`, configuration binding | `dev-stack:csharp` |
| `Program.cs`, endpoints, controllers, middleware, DI, options, `HttpClient`, health checks, `BackgroundService` | `dev-stack:aspnetcore` |
| A move to `net11.0`, `global.json` with an SDK 11 version, the .NET 11 breaking changes | `dev-stack:dotnet-11` |
| An HTTP API contract: URLs, status codes, errors, paging, idempotency, versions, an OpenAPI design | `dev-stack:api-design` |
| `Microsoft.AspNetCore.OpenApi`, `Swashbuckle*`, `Scalar*`, `Asp.Versioning*`, an OpenAPI document | `dev-stack:aspnetcore-openapi` |
| `FluentValidation*`, `AbstractValidator`, `RuleFor` | `dev-stack:fluentvalidation` |
| `Microsoft.EntityFrameworkCore*`, `DbContext`, `Migrations/`, `Npgsql.EntityFrameworkCore.PostgreSQL` | `dev-stack:efcore` |
| `*.sql` for SQL Server, `Microsoft.Data.SqlClient`, Dapper on SQL Server, raw SQL in EF Core on SQL Server | `dev-stack:sqlserver` |
| `*.sql` for PostgreSQL, `Npgsql`, Dapper on PostgreSQL, raw SQL in EF Core on PostgreSQL | `dev-stack:postgresql` |
| `ILogger`, `LoggerMessage`, `Serilog*`, `OpenTelemetry*`, `Azure.Monitor.OpenTelemetry*`, `ApplicationInsights*`, `Meter`, `ActivitySource` | `dev-stack:dotnet-observability` |
| `HybridCache`, `IDistributedCache`, `StackExchange.Redis`, Valkey | `dev-stack:redis` |
| `Elastic.Clients.Elasticsearch`, `NEST` | `dev-stack:elasticsearch` |
| `Microsoft.Orleans.*`, grains, `UseOrleans`, silo, `InProcessTestCluster` | `dev-stack:orleans` |
| `Aspire.Hosting.*`, an AppHost, ServiceDefaults, `Aspire.*` client integrations, `aspire` CLI | `dev-stack:aspire` |
| `*.razor`, `Microsoft.AspNetCore.Components*`, `QuickGrid`, render modes | `dev-stack:blazor` |
| `ModelContextProtocol*`, an MCP server in .NET, `.mcp/server.json` | `dev-stack:dotnet-mcp` |
| `RabbitMQ.Client`, `EasyNetQ`, an outbox table with a broker | `dev-stack:rabbitmq` |
| `WolverineFx*` | `dev-stack:wolverine` |
| `MailKit`, `MimeKit`, SMTP | `dev-stack:mailkit` |
| `PuppeteerSharp`, HTML to PDF, headless Chrome | `dev-stack:puppeteersharp` |
| Test projects, `MSTest*`, `xunit.v3*`, `xunit`, `AwesomeAssertions`, `Testcontainers*`, `WebApplicationFactory` | `test-stack:dotnet-testing` (MSTest 4 or xUnit v3) |
| `@playwright/test`, `playwright.config.*`, `Microsoft.Playwright*`, `@playwright/mcp`, `@playwright/cli`, end-to-end browser tests | `test-stack:playwright` |
| `*.feature`, `Reqnroll*`, `SpecFlow*`, Gherkin, step definitions | `test-stack:reqnroll` |
| `FluentAssertions` in a test project | `test-stack:migrate-fluentassertions` |
| Analyzer settings, `.globalconfig`, `Roslynator*`, `Meziantou.Analyzer`, `BannedSymbols.txt`, build warnings | `dev-stack:dotnet-analyzers` |
| `@angular/*`, `*.component.ts`, Angular `*.spec.ts`, `@jsverse/transloco`, `@ngrx/*` | `angular-developer` (the official skill), then `frontend-stack:angular` |
| Input, authentication, authorization, secrets, file upload, a new package, the security review | `dev-stack:secure-coding` |
| `bypassSecurityTrust*`, `innerHTML`, a token in the browser, CSP, Trusted Types, AngularJS templates | `frontend-stack:web-security` |
| `*.html`, `*.css`, `*.scss`, browser APIs, UI components, client-side JavaScript | `frontend-stack:modern-web` |
| `document.modelContext`, `registerTool`, WebMCP, tools for browser agents | `frontend-stack:webmcp` (preview) |

## Planned skills (not in a stack plugin yet)

| Signal | Skill |
|--------|-------|
| `angular` 1.x, `*.module.js` of AngularJS | `angularjs` |
| `*.ts`, `*.js`, `tsconfig*.json`, `eslint.config.*` | `typescript` |
| `*.html` templates, accessibility | `html-a11y` |
| `*.scss`, `stylelint` | `scss` |
| `Dockerfile`, `compose*.yml` | `docker` |
| Kubernetes manifests, `kustomization.yaml` | `kubernetes` |

When a planned skill is installed later, a front-end skill is in frontend-stack, and a .NET or platform skill is in dev-stack.

## References

- `references/language.md`: the language rules.
