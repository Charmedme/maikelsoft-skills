---
name: stack-index
description: Routing table from a file type, a package, or a technology to the dev-stack skill that holds its rules, with the state of each skill (available or planned). Preloaded by the dev-team agents. Use when a dev-team agent must know which stack skill to load before it designs, builds, or reviews code.
user-invocable: false
---

# Stack index

Find the stack skill for each technology that the work touches. Load it with the Skill tool before you design, build, or review that part. Read its version gate first. Write all text in the language rules of `references/language.md`.

## How to use

1. List the files that the work touches, and the packages in the project files (`*.csproj`, `Directory.Packages.props`, `package.json`).
2. Find each one in the table. One file can need more than one skill (an endpoint that writes to the database needs `aspnetcore` and `efcore`).
3. For Angular, load the official skill `angular-developer` first, then `dev-stack:angular`. When the rules differ, `dev-stack:angular` wins. When `angular-developer` is not installed, give the human the command `npx skills add https://github.com/angular/skills`, and go on with `dev-stack:angular`.
4. Always load `dev-stack:secure-coding` for code that takes input, handles a secret, or adds a package.
5. A skill that is not installed: write one line in your report, for example "No stack skill for X. I used the official docs of version Y." Check the current facts with a look-up. Do not stop the run for it.

## Available skills (dev-stack 1.3.0)

| Signal | Skill |
|--------|-------|
| `*.cs`, `*.csproj`, `Directory.Build.props`, `.editorconfig`, configuration binding | `dev-stack:csharp` |
| `Program.cs`, endpoints, controllers, middleware, DI, options, `HttpClient`, health checks, `BackgroundService` | `dev-stack:aspnetcore` |
| `Microsoft.AspNetCore.OpenApi`, `Swashbuckle*`, `Scalar*`, `Asp.Versioning*`, an OpenAPI document | `dev-stack:aspnetcore-openapi` |
| `FluentValidation*`, `AbstractValidator`, `RuleFor` | `dev-stack:fluentvalidation` |
| `Microsoft.EntityFrameworkCore*`, `DbContext`, `Migrations/`, `Npgsql.EntityFrameworkCore.PostgreSQL` | `dev-stack:efcore` |
| `*.sql` for SQL Server, `Microsoft.Data.SqlClient`, Dapper on SQL Server, raw SQL in EF Core on SQL Server | `dev-stack:sqlserver` |
| `*.sql` for PostgreSQL, `Npgsql`, Dapper on PostgreSQL, raw SQL in EF Core on PostgreSQL | `dev-stack:postgresql` |
| `ILogger`, `LoggerMessage`, `Serilog*`, `OpenTelemetry*`, `Azure.Monitor.OpenTelemetry*`, `ApplicationInsights*`, `Meter`, `ActivitySource` | `dev-stack:dotnet-observability` |
| `HybridCache`, `IDistributedCache`, `StackExchange.Redis`, Valkey | `dev-stack:redis` |
| `Elastic.Clients.Elasticsearch`, `NEST` | `dev-stack:elasticsearch` |
| `RabbitMQ.Client`, `EasyNetQ`, an outbox table with a broker | `dev-stack:rabbitmq` |
| `WolverineFx*` | `dev-stack:wolverine` |
| `MailKit`, `MimeKit`, SMTP | `dev-stack:mailkit` |
| `PuppeteerSharp`, HTML to PDF, headless Chrome | `dev-stack:puppeteersharp` |
| Test projects, `MSTest*`, `AwesomeAssertions`, `Testcontainers*`, `WebApplicationFactory` | `dev-stack:dotnet-testing` |
| `FluentAssertions` in a test project | `dev-stack:migrate-fluentassertions` |
| Analyzer settings, `.globalconfig`, `Roslynator*`, `Meziantou.Analyzer`, `BannedSymbols.txt`, build warnings | `dev-stack:dotnet-analyzers` |
| `@angular/*`, `*.component.ts`, Angular `*.spec.ts`, `@jsverse/transloco`, `@ngrx/*` | `angular-developer` (the official skill), then `dev-stack:angular` |
| Input, authentication, authorization, secrets, file upload, a new package, the security review | `dev-stack:secure-coding` |

## Planned skills (not in dev-stack yet)

| Signal | Skill |
|--------|-------|
| `angular` 1.x, `*.module.js` of AngularJS | `angularjs` |
| `*.ts`, `*.js`, `tsconfig*.json`, `eslint.config.*` | `typescript` |
| `*.html` templates, accessibility | `html-a11y` |
| `*.scss`, `stylelint` | `scss` |
| `Dockerfile`, `compose*.yml` | `docker` |
| Kubernetes manifests, `kustomization.yaml` | `kubernetes` |

When a planned skill is installed later, its name is `dev-stack:<skill>`.

## References

- `references/language.md`: the language rules.
