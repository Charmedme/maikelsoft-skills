# Defaults catalog: dev-team and dev-stack

Date: 2026-10-16. Status: draft, for review. Brief: `dev-team-brief.md`.

This catalog lists 200 decisions that a team makes one time for new code. The recommendations are not decisions. For each project, the agents ask the human each decision when the work first needs it. They show the recommendation from this catalog. The human answers, and the senior logs the answer as an ADR. Existing code keeps its own style.

Six research agents built the catalog from primary sources. They used NuGet and npm download data, State of JS and State of CSS 2025, JetBrains 2025, and the CNCF 2025 survey. They also used Microsoft Learn, the .NET 10 and Angular 22 templates, eShop, and the license files inside the packages. The full entries (options, evidence, best practice, when to choose another option, revisit signal, sources) are in the archive `dev-team-defaults-research-2026-10-16.zip`.

## Contents

- Standing rules
- Where common practice and best practice differ
- License findings
- How the agents ask and log
- Catalog

## Standing rules

These rules come from the human. The agents do not ask them again.

- MediatR and AutoMapper are not allowed. Use direct handler classes and manual mapping. When a mapping is large, use Mapperly (Apache-2.0, source generator). When a mediator is necessary, use an open source source-generated mediator (for example Mediator by martinothamar, MIT) or Wolverine.
- FluentAssertions 7.2.2 and AwesomeAssertions are both supported. A migration skill moves a project from one to the other.
- Pin `System.Security.Cryptography.Pkcs` 10.0.x with transitive pinning when a package pulls a vulnerable version (SqlClient 6.1 and 7.1, MimeKit 4.18). The agents do not ask again (batch 2, 2026-10-19).

## Where common practice and best practice differ

| ID | Common practice (evidence) | Best practice and the catalog default |
|----|----------------------------|---------------------------------------|
| API-06, API-07 | MediatR (495M downloads) and AutoMapper (1.16B) in eShop and in templates. | No mediator, manual mapping. Both are paid above USD 5M revenue since July 2025. |
| DATA-05, DATA-06 | Generic repository and own unit of work (eShop, templates). | DbContext directly. Microsoft docs: it already is both patterns. |
| DATA-18, OPS-17 | `Migrate()` at startup (eShop in each service). | Migration bundle in a one-shot job with its own database identity. |
| MSG-04 | MassTransit (166M downloads on v8). | Wolverine 6 (MIT, outbox built in). MassTransit v9 is commercial. |
| SEC-03, FE-26 | Tokens in the browser (OIDC libraries, about 3M installs per week). | BFF with an HttpOnly cookie (RFC 10017, Microsoft guidance). |
| TEST-02 | FluentAssertions v5 and v6. | AwesomeAssertions 9 for new repos, FA 7.2.2 where it exists. |
| TEST-03 | Moq (7.2M per week). | NSubstitute and hand-written fakes. New NSubstitute versions now get more downloads than new Moq versions. |
| TEST-11 | Coverage gate of 75 to 80%. | Coverage in the PR, no gate. Microsoft: a target can do harm. |
| FE-24, FE-22 | Reactive Forms, HttpClient with RxJS. | Signal Forms and `httpResource` (stable in v22). |
| FE-31 | Cypress among Angular users (56.8% use). | Playwright (94% satisfaction, first in the official Angular skill). |
| OPS-08 | Helm (81 to 87% in CNCF 2025). | Kustomize for own services, Helm for packages that other teams install. |

## License findings

| Package | Finding |
|---------|---------|
| PrimeNG 22 (2026-07-15) | Left MIT for a paid license. Free only under USD 1M revenue and fewer than 5 developers. |
| Verify 33+ (2026-09-12) | Maintenance fee for companies with USD 10,000 revenue or more. Verify 32 and older are MIT. |
| MassTransit v9 | Commercial (free under USD 1M revenue). |
| MediatR 13+, AutoMapper 15+ | Dual license, paid above USD 5M revenue. Not allowed (standing rule). |
| FluentAssertions 8+ | Paid license for commercial use. |
| Duende.BFF | Paid license in production. The catalog uses plain ASP.NET Core and YARP. |
| Docker Desktop | Free only under 250 employees and under USD 10M revenue. |
| Bitnami charts and images | No longer free and patched. |
| Grafana, Loki, Tempo | AGPLv3. |
| SonarAnalyzer.CSharp | License forbids AI analysis of its output. |

## How the agents ask and log

- **Index.** `docs/dev-team/decisions.md`: one row per decision ID (ID, question, answer, status, by, ADR, check, date). Each agent reads it first.
- **ADR.** One MADR 4.0.0 file per decision in the ADR folder of the repo, else `docs/decisions/`. Written with `dev-docs:doc-adr` when it is installed, else with a short MADR template with the same front matter.
- **Ask on first need.** The senior lists the decisions that the story needs in `design.md` and marks the open ones. At G1 the orchestrator asks all open decisions in one message, each with the recommendation. The human answers "ok" or names another option.
- **Reopen.** When a story needs another answer, the senior writes "Reopen: <ID>, because <need>". At G1 the human keeps or changes the decision. A change gives a new ADR. The old ADR becomes "superseded by", with links both ways. An accepted ADR never changes.
- **Checks.** A decision becomes a check only when a tool can decide it: `.editorconfig`, `BannedSymbols.txt`, an architecture test, or a `stackcheck` rule. Else the review checks it.
- **Script.** `decisioncheck.py` checks the index against the ADRs (status, links, supersession) and fails when new code uses a decision that has no accepted row.

## Catalog

The "B" marks of the research files show which decisions block the first story. The first story of a new .NET API meets about 40 of them.

### SOL: Solution and conventions (10)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| SOL-01 | Solution shape | Single project with feature folders (vertical slices). It is the template shape, has the smallest change cost, and a folder can move to its own project later without a rewrite. |
| SOL-02 | Folder organization | By feature. A change to one use case stays in one folder. Type folders spread one change over many folders. |
| SOL-03 | Shared build policy (Directory.Build.props) | `Directory.Build.props` with `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild true`. |
| SOL-04 | Central Package Management | CPM on, `CentralPackageTransitivePinningEnabled` true in apps. |
| SOL-05 | SDK pin (global.json) | `10.0.402`, `rollForward: latestFeature`, `allowPrerelease: false`. |
| SOL-06 | Warnings policy | Nullable as errors always. All warnings as errors in CI. NuGet audit warnings stay errors. |
| SOL-07 | Code style enforcement (.editorconfig) | Template `.editorconfig`. Raise IDE0161 (file-scoped namespace), IDE1006 (naming), IDE0005 (unused usings, needs `GenerateDocumentationFile`) to `warning`. |
| SOL-08 | Naming conventions | Microsoft style: `_camelCase`, `s_camelCase`, PascalCase constants, `Async` suffix on all awaitable methods (endpoint handler methods too, for one rule). |
| SOL-09 | Interfaces for services | Interface only at a seam. |
| SOL-10 | Aspire (AppHost and ServiceDefaults) | ServiceDefaults only for a single service with one database. AppHost + ServiceDefaults when the solution has 2 or more resources or services. |

### API: HTTP APIs (20)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| API-01 | Minimal APIs or controllers | Minimal APIs. |
| API-02 | Endpoint organization | Static `MapXxx` extension per feature, `MapGroup`, named static handler methods returning `TypedResults`. |
| API-03 | Request and response contracts | `sealed record` per request and per response, in the feature folder. |
| API-04 | Error contract | ProblemDetails everywhere, extension `code` (for example `order.not_found`) on every 4xx, `traceId` added in `CustomizeProblemDetails`. |
| API-05 | Expected failures: exceptions or result values | No library. Endpoints return `TypedResults` unions. Services return a small own result (or `null`, or a sealed record hierarchy). Exceptions only for bugs and infrastructure failure, mapped to 500 by `IExceptionHandler`. |
| API-06 | Mediator or direct calls | No mediator. One handler class per use case. MediatR is not allowed (human decision). |
| API-07 | Object mapping | Manual mapping. Mapperly (Apache-2.0, source generator) for large mappings. AutoMapper is not allowed (human decision). |
| API-08 | Request validation | FluentValidation when the project uses it (stack choice). Built-in `AddValidation` for shape rules only. |
| API-09 | OpenAPI generator and UI | Microsoft.AspNetCore.OpenApi + Scalar in Development. Build-time document committed. |
| API-10 | Versioning scheme | No versioning for an internal API with one consumer. URL segment with Asp.Versioning for any external or multi-consumer API, from the first endpoint (adding a path segment later breaks clients). |
| API-11 | JSON settings | Web defaults + `JsonStringEnumConverter` + `NumberHandling = Strict`. `DateTimeOffset` for instants. |
| API-12 | Pagination style | Offset with a hard maximum page size (for example 100) and a unique, stable sort order. |
| API-13 | Idempotency for POST | Natural idempotency (client-supplied id or unique business key). `Idempotency-Key` filter only on POSTs with external side effects. |
| API-14 | Rate limiting | Built-in limiter, `RejectionStatusCode = 429`, named policies on sensitive and anonymous endpoints only. |
| API-15 | CORS policy | Same origin (no CORS) when CFG-08 picks a BFF. Otherwise one named policy with origins from configuration. |
| API-16 | Health endpoint paths | `/healthz/live` and `/healthz/ready`, mapped in all environments, short-circuited, not routed through the public ingress. |
| API-17 | URL style | Lowercase plural nouns, kebab-case, actions as `POST /resource/{id}/action`. |
| API-18 | API version format and deprecation window | Major version `v1`. A version stays at least 6 months after its deprecation, 12 months for partners. |
| API-19 | Breaking-change gate | oasdiff in CI on the committed document: a gate on `ERR` for an API with an external consumer, a report for internal APIs. |
| API-20 | Validation error codes to the client | `code = "validation.failed"` plus an `errorCodes` map per field, the same shape for the built-in and the FluentValidation path. |

### CFG: Configuration, clients, jobs, auth (10)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| CFG-01 | Options pattern and validation | Typed options, DataAnnotations, `ValidateOnStart`. |
| CFG-02 | Secrets | User-secrets + environment variables (or orchestrator secrets). Add a vault provider when the platform already has one. |
| CFG-03 | Feature flags | Plain options for a few static on/off switches. Microsoft.FeatureManagement when targeting, percentage rollout or runtime refresh is needed. |
| CFG-04 | Time source | `TimeProvider`, UTC instants as `DateTimeOffset`. |
| CFG-05 | Outbound HTTP clients | Typed client. |
| CFG-06 | HTTP resilience defaults | Standard handler on all clients through `ConfigureHttpClientDefaults`. Retries off for POST/PATCH unless the call is idempotent (API-13). |
| CFG-07 | Background jobs | `BackgroundService` for in-process periodic work that may be lost on restart. Hangfire when a job must survive a restart or needs retries and a dashboard. |
| CFG-08 | Authentication model | APIs accept JWT bearer from the organization's IdP (Entra ID with Microsoft.Identity.Web, or Keycloak with plain `AddJwtBearer`). A browser SPA goes through a BFF with a cookie (own small BFF with YARP + OIDC cookie, or Duende.BFF). |
| CFG-09 | Authorization model | Fallback policy = authenticated user. Named policies mapped to scopes or roles from the IdP. Resource checks in the handler via `IAuthorizationService`. |
| CFG-10 | Localization of backend messages | Stable `code` in ProblemDetails (API-04) + English `detail`. Server-side localization only for text the server renders (mail, PDF) or when no own UI exists. |

### DATA: Data (29)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| DATA-01 | Database engine for a new service | The engine the product already runs. For a new product without a database: PostgreSQL 18. It has no license cost and is the most used database in the 2025 survey. EF Core and Dapper support it fully. |
| DATA-02 | Data access library: EF Core, Dapper, or both | EF Core only. Use `Select` projections for reads and `SqlQuery<T>` for the few queries LINQ cannot express. |
| DATA-03 | One DbContext or one per module | One DbContext per deployable service. In a modular monolith, one DbContext per module. |
| DATA-04 | Schema per module | One service, one module: default schema. Modular monolith: schema per module with `HasDefaultSchema` and `MigrationsHistoryTable("__EFMigrationsHistory", "<module>")`. |
| DATA-05 | Repository pattern on top of EF Core | DbContext directly. No generic repository. |
| DATA-06 | Unit of work | One `SaveChangesAsync` call per request or message. No `IUnitOfWork` interface. |
| DATA-07 | Primary key type | `Guid` keys generated by the EF provider (do not set the value in code). On PostgreSQL 18, also `DEFAULT uuidv7()` on the column for writers without EF. |
| DATA-08 | Strongly typed IDs | Plain primitives. |
| DATA-09 | Table and column naming | SQL Server: EF default PascalCase. PostgreSQL: snake_case with `EFCore.NamingConventions`, and `MatchNamesWithUnderscores = true` if Dapper is used. |
| DATA-10 | Date and time type | `DateTimeOffset` for instants (always UTC on PostgreSQL), `DateOnly` / `TimeOnly` for calendar values, `TimeProvider` for the clock. |
| DATA-11 | Money and decimal precision | `ConfigureConventions`: `Properties<decimal>().HavePrecision(18, 4)`. Money as a `Money` complex type (`Amount` decimal(19,4), `Currency` char(3) on SQL Server, `varchar(3)` with a CHECK on PostgreSQL) when more than one currency exists. A plain `decimal(19,4)` column when there is one currency. |
| DATA-12 | Enums in the database | String, set once per enum type in `ConfigureConventions` with `HaveConversion<string>()` and a max length (`IsUnicode(false)` on SQL Server). |
| DATA-13 | Soft delete | Hard delete. Decide per entity when a real need appears (undo, legal hold). |
| DATA-14 | Audit columns | `CreatedAt` and `UpdatedAt` (`DateTimeOffset`) on edited aggregates, filled by one `SaveChangesInterceptor`. |
| DATA-15 | Optimistic concurrency | A concurrency token on every aggregate that two users or two messages can change: `rowversion` on SQL Server, `xmin` on PostgreSQL. |
| DATA-16 | Multi-tenancy model | Single tenant until the product needs more. When it does: tenant column with a named query filter (`"Tenant"`), a composite index starting with the tenant column, and no DbContext pooling. |
| DATA-17 | Migration tool | EF Core migrations when EF Core is used (DATA-02). DbUp when the service has no EF model. |
| DATA-18 | Migrations deployment | Migration bundle built in CI and run as a one-shot job with a DDL-capable identity. |
| DATA-19 | Seed data | Static reference data that never changes per environment (countries, statuses): `HasData`. |
| DATA-20 | Test database strategy | One Testcontainers container per test run (image pinned to the production tag), migrations applied once, Respawn reset before each test. |
| DATA-21 | Read models and projections | Query the write tables with projections. Add a view for a complex, reused query. |
| DATA-22 | Bulk operations | `ExecuteUpdateAsync`/`ExecuteDeleteAsync` for set-based changes. `SqlBulkCopy` or Npgsql `COPY` for large inserts. |
| DATA-23 | Connection pooling on PostgreSQL | The Npgsql pool only. PgBouncer in transaction mode when replicas times `Max Pool Size` comes near `max_connections`. |
| DATA-24 | Case-insensitive text on PostgreSQL | A nondeterministic ICU collation on the column. `citext` only in existing schemas. |
| DATA-25 | DDL on live tables | `lock_timeout` in each migration that touches a table with live writes, indexes built concurrently, expand and contract (OPS-15). |
| DATA-26 | RCSI on SQL Server | On for each new OLTP database and in the test container. |
| DATA-27 | Optimized locking on SQL Server 2025 | On for new databases, with RCSI. Off for an existing database until the code is checked for order-dependent updates. |
| DATA-28 | String types on SQL Server | `nvarchar(n)` for user text, `varchar(n)` with `IsUnicode(false)` for codes, the server default `_CI_AS` collation. |
| DATA-29 | Stored procedures | No new procedures in services. LINQ or parameterized SQL in code. Procedures only for jobs that a DBA owns. |

### CACHE: Caching (8)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| CACHE-01 | Caching layer | No cache until a measured need. Then `HybridCache`: in-process only for one instance, with Redis/Valkey as L2 for more than one instance. |
| CACHE-02 | Cache server: Redis or Valkey | Valkey 9 for self-hosted. Write code server-neutral with StackExchange.Redis 3.x. |
| CACHE-03 | Cache key naming | `InstanceName = "{service}:"` and key `v{n}:{entity}:{id}[:{variant}]`, so the full key is `{service}:v{n}:{entity}:{id}`. Lowercase, internal IDs only. |
| CACHE-04 | Default TTLs | Global `Expiration` 5 minutes, `LocalCacheExpiration` 1 minute. Per entry overrides for data with a known change rate. |
| CACHE-05 | Invalidation style | Explicit `RemoveAsync` after commit, tags for groups (for example `tenant:{id}`), short L1 TTL. |
| CACHE-06 | Cache serialization | System.Text.Json with a source-generated context, DTO records only. |
| CACHE-07 | Fresh L1 on all nodes | No backplane. `DisableLocalCache` for data that must be fresh on each node. FusionCache with its backplane when many cached types need it. |
| CACHE-08 | Distributed lock | `LockTakeAsync` and `LockReleaseAsync` with a token for short work. DistributedLock.Redis when work can outlive the TTL. |

### SEARCH: Search (6)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| SEARCH-01 | When to add a search engine | Database full text first (PostgreSQL `tsvector`, SQL Server full-text index). |
| SEARCH-02 | Sync model from database to search index | Transactional outbox (same mechanism as MSG-09) with an indexing worker. The external version is a `long Version` column that the DATA-14 interceptor increments, or the outbox sequence number. Not `xmin` (32 bits, it wraps around). |
| SEARCH-03 | Index naming and aliases | `{entity}_v{n}` physical, alias `{entity}` for read and write (`is_write_index`). |
| SEARCH-04 | Search server target | Elasticsearch 9.5 with client 9.5.x. OpenSearch only when the license or AWS requires it. |
| SEARCH-05 | Client error handling | No throw by default, one `EnsureValid()` helper, item-by-item handling for bulk responses. |
| SEARCH-06 | Dutch and German text | Built-in `dutch` and `german` analyzers on language sub-fields, decided per index, checked with `_analyze` in a test. |

### MSG: Messaging (18)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| MSG-01 | In-process events or a broker | In process (direct call, or a domain event in the same transaction) inside one deployable. |
| MSG-02 | In-process dispatch library | None. Inject handler services directly. |
| MSG-03 | Broker choice (context note) | RabbitMQ 4.3 with quorum queues. |
| MSG-04 | Messaging library | Wolverine 6 for new services that write to a database and publish (the common case). |
| MSG-05 | Message contract location and versioning | One `<Service>.Contracts` project per publishing service, records only, no dependencies. Pinned wire names. Additive changes. Contract tests that compare serialized JSON with stored samples. |
| MSG-06 | Exchange and queue naming | Lowercase, dot-separated, kebab-case words. Exchange `{publisher}.events` (topic) and `{service}.commands`. Routing key `{entity}.{event}.v{n}`. Queue `{consumer}.{handler-or-purpose}`. Dead-letter queue `{service}.dlq` per service (Wolverine `CustomizeDeadLetterQueueing`), a queue per listener only when another team owns it. |
| MSG-07 | Queue type | Quorum queues for all durable work queues, declared explicitly in code, plus `default_queue_type = quorum` on each vhost. |
| MSG-08 | Retry and dead-letter policy | Wolverine, for transient exceptions: 3 immediate retries with cooldown (50 ms, 250 ms, 1 s). Then 2 scheduled retries with jitter (10 s, 1 min). Then the error queue. Validation errors go to the error queue at once. |
| MSG-09 | Outbox and inbox | Wolverine durable outbox on all sending endpoints with `AutoApplyTransactions()`, durable inbox on business listeners. |
| MSG-10 | Idempotency key strategy | Make the effect naturally idempotent (unique index on the business key, or upsert with a version check). |
| MSG-11 | Message serialization and envelope | System.Text.Json, `application/json`, camelCase, stable type name in the AMQP `type` property (or Wolverine `message-type` header), the library's own envelope. |
| MSG-12 | Correlation and tracing | OpenTelemetry tracing with the library's built-in propagation (`AddSource("Wolverine")`, `RabbitMQ.Client.*`). Set `MessageId` and `CorrelationId` on every message. Log both. |
| MSG-13 | Wolverine code generation | Static in production, Dynamic in development, the `WolverineFx.RuntimeCompilation` package in all builds. |
| MAIL-01 | SMTP transport security | `SslOnConnect` on port 465 or `StartTls` on port 587. Never `Auto` or `StartTlsWhenAvailable`. |
| MAIL-02 | Mail send path | An own small outbox table and a `BackgroundService` sender. The Wolverine durable local queue when the service already uses Wolverine. |
| MAIL-03 | Mail templates | Razor components rendered with `HtmlRenderer` (first party, encodes by default). |
| MAIL-04 | Mail tests | Mailpit through `Testcontainers.Mailpit` for integration tests. |
| MAIL-05 | Microsoft 365 mail | OAuth2 SMTP when the code uses MailKit. Graph `sendMail` and Azure Communication Services Email are the alternatives. |

### TEST: Tests (18)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| TEST-01 | Unit test framework | MSTest 4 with `MSTest.Sdk`. It is already the stack, it has analyzers and coverage in one SDK, and a second framework adds cost with no gain. |
| TEST-02 | Assertion library | AwesomeAssertions 9 for new repos. FluentAssertions 7.2.2 pinned where it exists (both supported). |
| TEST-03 | Mocking library | NSubstitute 6 plus `NSubstitute.Analyzers.CSharp`, and hand-written fakes for repositories, clocks (`FakeTimeProvider`) and simple ports. |
| TEST-04 | Test project layout | `src/` and `tests/` folders. One `X.UnitTests` per production project that has logic. One `X.IntegrationTests` per web host. |
| TEST-05 | Test naming convention | `Method_Scenario_ExpectedResult`, and the test class name `<ClassUnderTest>Tests`. |
| TEST-06 | Test data creation | Inline `new` for one-off data. A hand-written builder or factory method per aggregate when the same setup appears twice. |
| TEST-07 | Integration test strategy | `WebApplicationFactory<Program>` plus Testcontainers with a pinned image, one container per test assembly, replace the connection string in `ConfigureWebHost`. |
| TEST-08 | Database state between tests | Respawn reset plus sequential database classes. It is milliseconds per reset, works through the full HTTP pipeline, and needs no special code in production. |
| TEST-09 | Snapshot testing | No snapshot library. Use explicit assertions. For one large file, commit an expected file and compare. |
| TEST-10 | Architecture tests | Project references only. Zero tooling, and the build already fails on a wrong reference. |
| TEST-11 | Coverage tool and gate | The Microsoft collector through `MSTest.Sdk` (`dotnet test --coverage --coverage-output-format cobertura`), report in the PR, no gate. |
| TEST-12 | Mutation testing | On demand only, on the domain project, with a short note of the score in the ADR or PR. |
| TEST-13 | API contract tests | OpenAPI document per version plus oasdiff `breaking` in CI, plus the integration tests from TEST-07. |
| TEST-14 | Frontend unit test runner and DOM environment | Vitest with jsdom, the CLI default. No extra config. |
| TEST-15 | Frontend component test style | `TestBed` plus harnesses where they exist. It is the first-party path, needs no extra package, and harnesses also work in E2E. |
| TEST-16 | End-to-end tests | Playwright Test in TypeScript, 3 to 10 smoke flows, run against a deployed test environment or a Compose stack in CI. |
| TEST-17 | Test parallelization | Method level for unit tests. Class level plus `[DoNotParallelize]` on database classes for integration tests. |
| TEST-18 | CI test stages and flaky test policy | Unit plus integration on every PR. E2E after merge. No automatic retry. Quarantine with a reason and an issue. |

### QUAL: Code quality (13)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| QUAL-01 | Analysis level and warnings-as-errors policy | `latest-recommended`. Nullable as errors always. All warnings as errors in CI. |
| QUAL-02 | Third-party analyzers | SDK plus Roslynator plus Meziantou plus VS Threading, with the `analyzers.md` baseline. |
| QUAL-03 | C# formatter | `dotnet format` plus `.editorconfig`. No extra tool, and the same `.editorconfig` drives IDE, build and CI. |
| QUAL-04 | `.editorconfig` and `.gitattributes` baseline | Baseline `.editorconfig` from `dotnet-analyzers`, `* text=auto eol=lf`. |
| QUAL-05 | Banned APIs | BannedApiAnalyzers with a list of 3 to 5 entries (`DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, `Thread.Sleep` in production code). |
| QUAL-06 | XML documentation policy | XML docs on public APIs of libraries. CS1591 off in apps. |
| QUAL-07 | Frontend formatter | Prettier as generated by `ng new`, plus `prettier --check .` in CI. |
| QUAL-08 | Frontend lint | Angular-eslint plus `recommended-type-checked` plus `stylistic`, with the angular-eslint rules from `angular.md` section 6. |
| QUAL-09 | Stylesheet lint | No Stylelint at start. |
| QUAL-10 | Pre-commit hooks | No hooks. Simplest. CI catches the same issues. |
| QUAL-11 | Package version management | CPM plus `GlobalPackageReference` for analyzers. Npm lock file plus `npm ci`. |
| QUAL-12 | Dependency update bot | Dependabot, weekly, one group for minor and patch per ecosystem, majors as single PRs. |
| QUAL-13 | Vulnerability audit | NuGetAudit (transitive) and `npm audit` in CI, built-in tools only. |

### GIT: Git and release (10)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| GIT-01 | Branching model | Trunk-based with short branches and PRs to `main`. Unfinished work behind a flag or not yet wired in. |
| GIT-02 | Branch naming | `<type>/<issue-id>-<short-description>`, lower case with hyphens. Without an issue, `<type>/<short-description>`. |
| GIT-03 | Commit message convention | Conventional Commits on the PR title and so on the squash commit. Types `feat`, `fix`, `refactor`, `test`, `docs`, `build`, `ci`, `chore`. English, imperative, at most 72 characters. |
| GIT-04 | Commit size | One logical change per commit. A pure rename or format change in a separate commit. |
| GIT-05 | PR size and template | Soft limit about 400 changed lines excluding generated files. Template with four headings: What, Why (issue link), How tested, Risks and rollback. |
| GIT-06 | Merge strategy | Squash merge only, linear history on, delete branch after merge. One revertable commit per PR, and only the PR title must follow GIT-03. |
| GIT-07 | Branch protection and review rule | Protected `main`, required CI checks, one approval, dismiss approvals on new pushes. |
| GIT-08 | Versioning and release tags | MinVer, a `vX.Y.Z` tag at release, and CI fetches tags (full fetch depth). |
| GIT-09 | Changelog | Keep a Changelog, one line per user-facing PR, moved under a version at release. |
| GIT-10 | CI system | The CI of the repo host. This is a fact to record, not a preference to argue. |

### FE: Frontend (34)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| FE-01 | Workspace layout | Single app. It is the simplest layout, every Angular guide assumes it, and you can move to a CLI multi-project or Nx later without a rewrite. |
| FE-02 | Package manager | Npm, with `min-release-age=3` in `.npmrc` and `npm ci` in CI. It needs no extra install, the whole Angular toolchain is tested with it, and the release-age setting closes most of the supply-chain gap to pnpm. |
| FE-03 | Node version pin | Node 26 LTS (from 2026-10-28), pinned in `.nvmrc` and in `package.json` `engines.node` (`^26`). |
| FE-04 | TypeScript version and strictness flags | CLI strict plus `noUncheckedIndexedAccess` and `erasableSyntaxOnly`. Pin `typescript` to `~6.0.x`. |
| FE-05 | Enums policy | No enums in new code. Use `type Status = 'open' / 'closed'` or an `as const` object when you need to loop over the values. |
| FE-06 | Lint configuration | ESLint 10 flat config with `angular-eslint` `tsRecommended`, `templateRecommended`, `templateAccessibility`, typescript-eslint `strictTypeChecked` plus `stylisticTypeChecked`, and the v22 rules `prefer-signals`, `prefer-service-decorator`, ... |
| FE-07 | Formatting | Prettier with the CLI config, `prettier --check .` in CI, format-on-save in the editor. |
| FE-08 | Folder structure | `src/app/<feature>/` for each route area, flat inside the feature (no type subfolders), `src/app/shared/` only for UI and helpers that two or more features use. |
| FE-09 | File naming style and selector prefix | 2025 style in new workspaces. A 2 to 4 letter product prefix (for example `shp`) so selectors do not collide with libraries. |
| FE-10 | Standalone everywhere | Standalone only, `strictStandalone: true` in `angularCompilerOptions`. |
| FE-11 | Change detection | Zoneless, no explicit `changeDetection` in components. |
| FE-12 | Browser support baseline | Angular's set, no `.browserslistrc`, no polyfills. |
| FE-13 | Bundle budgets | CLI strict budgets. CI fails on the error level. |
| FE-14 | SSR or client-side rendering | CSR only for logged-in business apps. |
| FE-15 | Hosting and origin | Same origin, served by the ASP.NET Core app (or by a reverse proxy in front of it). |
| FE-16 | Runtime configuration | Runtime `config.json` loaded in `provideAppInitializer()`, typed through an `InjectionToken`. |
| FE-17 | UI component library | Angular Material + CDK. It is MIT, ships in lockstep with Angular, has harnesses for tests and supports Signal Forms. |
| FE-18 | Styling approach | Component-scoped SCSS, tokens as CSS custom properties, short kebab-case class names inside components, BEM only for global styles. |
| FE-19 | Icons | Material Symbols, self-hosted through the `material-symbols` package and used with `<mat-icon>`. |
| FE-20 | Routing patterns | Lazy routes per feature, functional guards, `withComponentInputBinding()`. |
| FE-21 | State management | Signal services with `@Service()`. No store library. |
| FE-22 | Data fetching | `httpResource()` for reads, `HttpClient` for writes, both through the same functional interceptors. |
| FE-23 | API client | Orval with `client: 'angular'` and `retrievalClient: 'both'`, output to `src/app/api/` (generated, committed), regenerated in CI and diffed. |
| FE-24 | Forms | Signal Forms for every new form. |
| FE-25 | Error handling and global error UI | Call-site handling with resource `error()` states in the template. One functional interceptor for auth redirects and server errors. A custom `ErrorHandler` that sends errors to the backend log or telemetry. `MatSnackBar` (or the design system toast) as the ... |
| FE-26 | Authentication in the SPA | BFF on the ASP.NET Core backend (OIDC + cookie auth, YARP or plain controllers as the API proxy), same origin (FE-15), Angular XSRF on. |
| FE-27 | Internationalization (i18n) | Angular built-in i18n with a locale in the URL (`/nl/`, `/de/`), `i18nMissingTranslation: 'error'`, XLIFF 2 files in the repo. |
| FE-28 | Date handling | Native `Date` with `DatePipe` / `Intl` for display. Date-fns for arithmetic and the Material datepicker adapter. ISO 8601 with offset on the API. |
| FE-29 | Unit test runner | Vitest with jsdom via `ng test`. |
| FE-30 | Component tests | `TestBed` with harnesses for library components and role-based DOM queries for your own markup. |
| FE-31 | End-to-end tests | Playwright, against a real backend in CI for the critical flows. |
| FE-32 | Accessibility target and tooling | WCAG 2.2 AA. `templateAccessibility` lint. `@axe-core/playwright` scan in each E2E flow (fail on serious and critical). Material or Aria for complex widgets. Manual keyboard and screen reader check per release. |
| FE-33 | AngularJS policy for new code in hybrid apps | New code in Angular only. AngularJS: fixes and security hardening only, each change in `.component()` style. |
| FE-34 | Angular upgrade cadence | Upgrade within 3 months of each major, in one planned story. Patch and minor updates monthly. |

### OPS: Operations (24)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| OPS-01 | Environment names | `Development`, `Staging`, `Production`, with OTAP mapped in the docs. `Test` only for a shared test environment. |
| OPS-02 | Local development setup | .NET Aspire AppHost. |
| OPS-03 | Container base image family | `aspnet:10.0-noble-chiseled-extra` (ICU and tzdata for Dutch and German formats). A headless Chrome renderer gets its own image on full `aspnet:10.0`. |
| OPS-04 | Dockerfile or SDK container publish | Multi-stage Dockerfile with BuildKit. |
| OPS-05 | Container registry | The registry of the cloud that runs the cluster, tags immutable. |
| OPS-06 | Image tag and digest policy | Immutable tag (Git SHA, SemVer for releases) plus digest in the manifests. |
| OPS-07 | Image scanning tool and SBOM | Trivy pinned by SHA, gate on fixable High and Critical, SBOM with BuildKit. |
| OPS-08 | Kubernetes packaging | Plain manifests plus Kustomize overlays. |
| OPS-09 | Configuration delivery | Environment variables from a ConfigMap (`configMapGenerator`, hash suffix). |
| OPS-10 | Secrets in Kubernetes | External Secrets Operator with the vault of the platform. |
| OPS-11 | Traffic entry (Gateway API implementation) | The managed Gateway API of the cloud. Envoy Gateway on a self-managed cluster. |
| OPS-12 | Health endpoint paths and probe policy | `/healthz/live` and `/healthz/ready` in all environments. |
| OPS-13 | Resource requests and limits policy | Memory request equal to the limit. CPU request only, no CPU limit. |
| OPS-14 | Autoscaling | Fixed replicas (2 or more) with a PDB. HPA when load varies. KEDA for bursty queue consumers. |
| OPS-15 | Deployment strategy | Rolling update with expand-and-contract migrations. |
| OPS-16 | Deploy mechanism: GitOps or pipeline deploy | Pipeline deploy with OIDC federation. Argo CD when the platform team runs it. |
| OPS-17 | Database migration execution in a release | EF migration bundle in a Kubernetes Job before the rollout, own database identity. |
| OPS-18 | Observability backend | Azure Monitor / Application Insights on Azure. Grafana stack elsewhere. |
| OPS-19 | Log format and log transport | OTLP log export plus JSON console in containers. |
| OPS-20 | Trace sampling | Keep all traces, set in configuration. Ratio sampling above about 100 requests per second. |
| OPS-21 | Alerting basics | Four symptom alerts per service: 5xx rate, p95 latency, restarts, oldest message age. |
| OPS-22 | TLS certificate management | Cloud-managed certificates on a cloud gateway. Else cert-manager. |
| OPS-23 | Logging pipeline | Microsoft.Extensions.Logging plus OpenTelemetry. Serilog only where it exists or on request. `[LoggerMessage]` in services and libraries, the project setting wins. |
| OPS-24 | Azure Monitor client for new apps | The Microsoft OpenTelemetry Distro, as Learn recommends. Keep `UseAzureMonitor` where it exists. |

### SEC: Security (24)

| ID | Decision | Recommended default |
|----|----------|---------------------|
| SEC-01 | Identity provider | Keycloak for customer-facing multi-tenant products. Entra ID for workforce apps. |
| SEC-02 | API token model | JWT bearer, validated locally, one audience per API, short-lived tokens. |
| SEC-03 | Sign-in model for Angular apps | BFF on ASP.NET Core (OIDC confidential client, cookie, antiforgery, YARP). |
| SEC-04 | Authorization model | Fallback policy plus named permission policies. Resource checks in the handler. |
| SEC-05 | Multi-tenant isolation check | `TenantId` column, tenant from the token, EF query filter, plus a test that proves isolation. |
| SEC-06 | Password storage | No passwords in the app: the IdP owns them. |
| SEC-07 | Secrets in development and CI | Dev: user-secrets or Aspire parameters. |
| SEC-08 | HTTPS, HSTS and TLS termination | TLS ends at the gateway, HSTS one year with subdomains, no plain HTTP on APIs. |
| SEC-09 | CORS defaults | No CORS: one origin through the BFF. |
| SEC-10 | Security headers set | OWASP header set: nginx for static files, small middleware for APIs, CSP for Angular. |
| SEC-11 | Rate limiting defaults | ASP.NET Core rate limiter on sensitive and expensive endpoints, partitioned by user. |
| SEC-12 | Input size limits | Global body limit about 1 MB, raised per endpoint. Page size cap 100. |
| SEC-13 | File upload policy | Extension allowlist, signature check (FileSignatures), size limit, server name, private storage, malware scan. |
| SEC-14 | Data classification and PII logging policy | Small taxonomy (Public, Internal, Personal, Secret) with Compliance redaction. Company taxonomy if one exists. |
| SEC-15 | Dependency policy | Nuget.org and npmjs.org plus Package Source Mapping, lock files, 3-day cooldown. |
| SEC-16 | SAST tool | .NET security analyzers on, plus Semgrep CE. CodeQL when licensed. |
| SEC-17 | Secrets scanning | Gitleaks in CI. GitHub push protection when licensed. |
| SEC-18 | Threat model practice | STRIDE light: five questions in each design. |
| SEC-19 | Security review gate | Block fixable Critical and High. Medium gets a ticket. Low is advice. Target ASVS 5.0 level 2. |
| SEC-20 | Data Protection key ring storage | Data Protection keys in the SQL database, protected with a certificate or vault key. |
| SEC-21 | Kubernetes workload hardening baseline | Pod Security `restricted`, default-deny NetworkPolicy. |
| SEC-22 | Service-to-service authentication | Client credentials JWT for HTTP. One RabbitMQ user and vhost per service. |
| SEC-23 | Security event and audit logging | Security events as structured logs with stable event IDs, retention agreed with the business. |
| SEC-24 | Chrome `--no-sandbox` | Not allowed by default. Allowed only with an ADR that names the isolation (non-root, read-only file system, no secrets, egress blocked), and only for trusted templates. |
