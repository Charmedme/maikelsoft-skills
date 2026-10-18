---
name: stack-index
description: Routing table from a file type, a package, or a technology to the dev-stack skill that holds its rules, with the state of each skill (available or planned). Preloaded by the dev-team agents. Use when a dev-team agent must know which stack skill to load before it designs, builds, or reviews code.
---

# Stack index

Find the stack skill for each technology that the work touches. Load it with the Skill tool before you design, build, or review that part. Read its version gate first. Write all text in the language rules of `references/language.md`.

## How to use

1. List the files that the work touches, and the packages in the project files (`*.csproj`, `Directory.Packages.props`, `package.json`).
2. Find each one in the table. One file can need more than one skill (an endpoint that writes to the database needs `aspnetcore` and `efcore`).
3. Always load `dev-stack:secure-coding` for code that takes input, handles a secret, or adds a package.
4. A skill that is not installed: write one line in your report, for example "No stack skill for X. I used the official docs of version Y." Check the current facts with a look-up. Do not stop the run for it.

## Available skills (dev-stack 1.0.0)

| Signal | Skill |
|--------|-------|
| `*.cs`, `*.csproj`, `Directory.Build.props`, `.editorconfig`, configuration binding | `dev-stack:csharp` |
| `Program.cs`, endpoints, controllers, middleware, DI, options, `HttpClient`, health checks, `BackgroundService` | `dev-stack:aspnetcore` |
| `Microsoft.EntityFrameworkCore*`, `DbContext`, `Migrations/`, `Npgsql.EntityFrameworkCore.PostgreSQL` | `dev-stack:efcore` |
| Test projects, `MSTest*`, `AwesomeAssertions`, `Testcontainers*`, `WebApplicationFactory` | `dev-stack:dotnet-testing` |
| `FluentAssertions` in a test project | `dev-stack:migrate-fluentassertions` |
| Analyzer settings, `.globalconfig`, `Roslynator*`, `Meziantou.Analyzer`, `BannedSymbols.txt`, build warnings | `dev-stack:dotnet-analyzers` |
| Input, authentication, authorization, secrets, file upload, a new package, the security review | `dev-stack:secure-coding` |

## Planned skills (not in dev-stack yet)

| Signal | Skill |
|--------|-------|
| `Microsoft.AspNetCore.OpenApi`, `Swashbuckle*`, `Scalar*`, `Asp.Versioning*` | `aspnetcore-openapi` |
| `FluentValidation*` | `fluentvalidation` |
| `ILogger`, `Serilog*`, `OpenTelemetry*`, `ApplicationInsights*` | `dotnet-observability` |
| `*.sql` for SQL Server, `Microsoft.Data.SqlClient` | `sqlserver` |
| `*.sql` for PostgreSQL, `Npgsql` | `postgresql` |
| `HybridCache`, `StackExchange.Redis` | `redis` |
| `Elastic.Clients.Elasticsearch` | `elasticsearch` |
| `RabbitMQ.Client`, `EasyNetQ` | `rabbitmq` |
| `WolverineFx*` | `wolverine` |
| `MailKit`, `MimeKit` | `mailkit` |
| `PuppeteerSharp` | `puppeteersharp` |
| `@angular/*`, `*.component.ts` | `angular` |
| `angular` 1.x, `*.module.js` of AngularJS | `angularjs` |
| `*.ts`, `*.js`, `tsconfig*.json`, `eslint.config.*` | `typescript` |
| `*.html` templates, accessibility | `html-a11y` |
| `*.scss`, `stylelint` | `scss` |
| `Dockerfile`, `compose*.yml` | `docker` |
| Kubernetes manifests, `kustomization.yaml` | `kubernetes` |

When a planned skill is installed later, its name is `dev-stack:<skill>`.

## References

- `references/language.md`: the language rules.
