# Concept: dev-stack batch 2

Date: 2026-10-19. Status: approved on 2026-10-19. The owner accepted each recommendation of the decision sheet, except B2-01: build all 11 skills in one batch. The 3 extra stack skills are `aspire`, `keycloak`, and `dapper` (a later batch).

Batch 2 has 11 stack skills. Four agents checked the research of 2026-10-16 again, and compiled or ran each claim that decides a rule. Docker was down, so no real broker, SQL Server, Valkey, or Elasticsearch server ran. A local PostgreSQL 16 and fake SMTP, cache, and search servers stood in.

## Contents

- Version changes since 2026-10-16
- Skills
- Test plan
- Decision sheet
- Catalog changes

## Version changes since 2026-10-16

Few versions changed. These facts change the skills:

- SqlClient 7.1.1 and MimeKit 4.18.1 pull a vulnerable `System.Security.Cryptography.Pkcs`. The build fails with `-warnaserror` until the project references Pkcs 10.0.13 directly. (verified, compiled)
- `Testcontainers.Mailpit` exists since 4.14.0. The research said that there is no .NET module. (verified, docs)
- EFCore.PG 11.0.0-rc.2 came out on 2026-10-19. PostgreSQL 19 RC 1 is out. The skills stay on PostgreSQL 18 and EF Core 10. (verified, docs)
- Learn now recommends the Microsoft OpenTelemetry Distro for new apps on Azure Monitor. (verified, docs)
- The `dotnet-testing` skill pins the image `rabbitmq:4.2`. Community support for 4.2 ended on 2026-07-31. (verified, docs)

## Skills

| Skill | Scope | Top traps (all compiled or run) | Trap case |
|-------|-------|--------------------------------|-----------|
| `postgresql` | Engine, SQL, types, indexes, MVCC, locks, DDL on live tables, Npgsql, Dapper on Npgsql. EF options stay in `efcore`. | A local `DateTime` through Dapper shifts by the session time zone with no error. `SKIP LOCKED` outside a transaction gives one row to two workers. `FromSql("SELECT *")` fails on an entity with `xmin`. | Picking queue for 20 parallel workers, and a daily report in Amsterdam time. |
| `sqlserver` | T-SQL, types, indexes, isolation, RCSI, optimized locking, deadlocks, SqlClient and Dapper. | `AT TIME ZONE` takes only Windows zone names. A raw SQL string parameter is `nvarchar(4000)`, so a `varchar` key loses its index. A parallel `MERGE` needs `HOLDLOCK`. | Stock reservation that never goes below zero, a parallel upsert, and a report without `NOLOCK`. |
| `redis` | HybridCache (L1 in process, Redis or Valkey as L2), StackExchange.Redis 3, keys, TTLs, locks, pub/sub. | After a remove on node A, node B serves its old L1 copy (also for tags). A cached EF entity comes back without its ID and lines. The cache package alone resolves StackExchange.Redis 2.7.27. | Cached GET on three nodes, where a cancel must show on each node at once. |
| `elasticsearch` | Client 9, mappings, aliases and reindex, sync from the database, queries, paging. | NEST and 8.x code. One failed bulk item does not throw. `from` and `size` for a large export. | Order search with typo tolerance, a two-million-row export, and correct order of updates. |
| `rabbitmq` | Messaging principles, broker 4.3, RabbitMQ.Client 7, EasyNetQ 8 as a reference. | Publisher confirms are off by default. Classic queues by default. Publish inline after `SaveChangesAsync` loses events when the broker is down. | Event for a Go service, with no event lost while the broker is down. |
| `wolverine` | Wolverine 6: handlers, outbox and inbox on EF Core, error policies, code generation, the RabbitMQ transport. | A plain `AddDbContext` throws on the first message. An endpoint that calls `SaveChangesAsync`, then `PublishAsync`, is outside the outbox. A handler that returns `int` is not found. | Order and message never out of step, and a business error that is not retried. |
| `mailkit` | MailKit 4 and MimeKit 4: options, TLS, connection, outbox, background sender, failure classes, Mailpit tests. | `SecureSocketOptions.Auto` sends the password in plain text when the relay has no STARTTLS. One rejected recipient stops the whole message. The Pkcs pin. | Order confirmation mail through an outbox, while the relay is down for some minutes. |
| `aspnetcore-openapi` | The OpenAPI document, response metadata, Scalar or Swagger UI, the build-time document, versioning with Asp.Versioning. | A `TypedResults.Problem(404)` and the validation 400 are missing from the document. The build-time document goes to `obj/`. Integers show as `["integer","string"]`. | Document for an external client generator, kept in the repository. |
| `fluentvalidation` | FluentValidation 12: rules, cascade, codes, child and collection rules, async rules, the call from a filter or worker, tests. | With `AddValidation` and DataAnnotations still active, the built-in filter answers first and the validator never runs. `ToDictionary()` drops the error codes. An async rule with sync `Validate` gives a 500. | Move POST /orders to FluentValidation with an async SKU rule and stable codes. |
| `dotnet-observability` | ILogger, `[LoggerMessage]`, OpenTelemetry traces and metrics, the Application Insights client, Serilog as an option, redaction, sampling, tests. | The Microsoft distro throws at start without a connection string, so integration tests fail. Serilog replaces the logger factory and redaction stops. A metric tag per customer. | Logs and traces to Application Insights, with an email address that must not reach the logs. |
| `puppeteersharp` | HTML to PDF with headless Chrome: browser lifetime, pages, concurrency, PDF options, the container image, the sandbox, untrusted HTML. | Free text in the HTML makes requests to internal hosts (SSRF). As root, Chrome needs `--no-sandbox`. A chiseled image cannot hold Chrome. | Invoice PDF with a customer note and a remote logo, in the Docker image. |

Each skill follows the stack skill template. It has a `SKILL.md` under 300 lines, `references/decisions.md`, a review checklist, and sources. Its `rules.json` has 3 to 6 regex rules for `stackcheck.py`, each tested on match and no-match examples.

## Test plan

Each skill gets two cases, as in batch 1: a trap write case and a review case with 6 to 8 planted mistakes. Each case runs 3 times with the skill and 3 times without it (12 runs per skill). Hidden probe tests check the behavior.

- New fixtures: `orders-api-sqlserver`, `puppeteersharp-trap`, and one review fixture per skill.
- New check types: `file_exists` and `transitive_version`.
- The cases need Docker images: PostgreSQL 18, SQL Server 2025, Valkey 9, Elasticsearch 9.5, RabbitMQ 4.3, Mailpit, and Chrome in a CI image.
- Cost estimate: about $1 per run with grading, so about $130 for 132 runs. The fixtures and probes take most of the build time.

## Decision sheet

The recommendation is the answer unless the owner changes the row.

### Structure

| ID | Question | Recommendation |
|----|----------|----------------|
| B2-01 | Build all 11 skills in one batch, or split them? | Split. 2a: postgresql, sqlserver, redis, aspnetcore-openapi, fluentvalidation, dotnet-observability. 2b: rabbitmq, wolverine, mailkit, elasticsearch, puppeteersharp. 2b needs more infrastructure in the tests. |
| B2-02 | One `redis` skill, or `dotnet-caching` plus `dotnet-redis`? | One `redis` skill. Its description names HybridCache, so it also triggers for caching without Redis. |
| B2-03 | One observability skill, or a separate logging skill? | One skill, with references for Serilog, redaction, Application Insights migration, and tests. |
| B2-04 | Where do the messaging principles live? | One shared file `messaging-principles.md`, synced into `rabbitmq` and `wolverine`, like `stack-rules.md`. |
| B2-05 | Wolverine.HTTP in scope? | No. Minimal APIs (API-01) use `IDbContextOutbox<T>` or `InvokeAsync`. |
| B2-06 | Retire the personal skill `valkey-caching` when `redis` ships? | Yes. Its L1 text moves into the `redis` skill. |
| B2-07 | Standing rule for the Pkcs pin? | Yes. "Pin `System.Security.Cryptography.Pkcs` 10.0.x with transitive pinning", so a story does not ask again. |
| B2-08 | Move the test images: `postgres:17.6-alpine` to 18.6, and `rabbitmq:4.2` to 4.3? | Yes, both. DATA-20 says to pin the production tag. |

### Catalog corrections

| ID | Today | Recommendation |
|----|-------|----------------|
| B2-09 | SEARCH-02: the row version is the external version. On PostgreSQL that is `xmin`, which wraps around. | A `long Version` column that the DATA-14 interceptor increments, or the outbox sequence number. |
| B2-10 | DATA-11: `char(3)` for the currency. The PostgreSQL wiki advises against `char(n)`. | `varchar(3)` with a CHECK on PostgreSQL. `char(3)` on SQL Server. |
| B2-11 | CACHE-03: the key starts with `{service}`, and `InstanceName` adds it again. | `InstanceName = "{service}:"`, key `v{n}:{entity}:{id}`. |
| B2-12 | MSG-06: `{queue}.dlq`. Wolverine declares one shared dead-letter queue. | One `{service}.dlq` per service. A queue per listener only when another team owns it. |
| B2-13 | OPS-03: chiseled base image. Chrome cannot go in it. | The renderer gets its own image on full `aspnet:10.0`. The API keeps chiseled. |
| B2-14 | DATA-07: the fixture says "UUIDv7 in Npgsql". The EF provider generates it. | Fix the text. On PostgreSQL 18, add `DEFAULT uuidv7()` for writers without EF. |

### New decisions

| ID | Question | Recommendation |
|----|----------|----------------|
| DATA-23 | Connection pooling on PostgreSQL | The Npgsql pool only. PgBouncer in transaction mode when replicas times `Max Pool Size` comes near `max_connections`. |
| DATA-24 | Case-insensitive text on PostgreSQL | A nondeterministic ICU collation on the column. `citext` only in existing schemas. |
| DATA-25 | DDL on live tables | `lock_timeout` in each migration, indexes built concurrently, expand and contract. |
| DATA-26 | RCSI on SQL Server | On for each new OLTP database and in the test container. |
| DATA-27 | Optimized locking on SQL Server 2025 | On for new databases. Off for an existing database until the code is checked. |
| DATA-28 | String types on SQL Server | `nvarchar(n)` for user text, `varchar(n)` for codes, the server default collation. |
| DATA-29 | Stored procedures | No new procedures in services. Procedures only for jobs that a DBA owns. |
| CACHE-07 | Fresh L1 on all nodes | `DisableLocalCache` for data that must be fresh. FusionCache when many types need a backplane. |
| CACHE-08 | Lock library | `LockTakeAsync` with a token for short work. DistributedLock.Redis when work can outlive the TTL. |
| SEARCH-04 | Search server target | Elasticsearch 9.5 with client 9.5.x. OpenSearch only when the license or AWS requires it. |
| SEARCH-05 | Error handling of the client | No throw by default, one `EnsureValid()` helper, item-by-item handling for bulk. |
| SEARCH-06 | Dutch and German text | Built-in `dutch` and `german` analyzers on language sub-fields, checked with `_analyze` in a test. |
| MSG-13 | Wolverine code generation in production | Static in production, Dynamic in development, the RuntimeCompilation package in all builds. |
| MAIL-01 to MAIL-05 | Mail group: TLS, send path, templates, provider, Microsoft 365 | `SslOnConnect` on 465 or `StartTls` on 587 (never `Auto`). Own small outbox, or the Wolverine local queue when the service uses Wolverine. Razor components with `HtmlRenderer`. OAuth2 SMTP for Microsoft 365. |
| API-18 | API version format and deprecation window | Major version `v1`, 6 months minimum, 12 months for partners. |
| API-19 | Breaking-change gate | oasdiff in CI: a gate for an API with an external consumer, a report for internal APIs. |
| API-20 | Validation error codes to the client | `code = "validation.failed"` plus an `errorCodes` map, the same shape for both validation paths. |
| OPS-23 | Logging pipeline | Microsoft.Extensions.Logging plus OpenTelemetry. Serilog only where it exists or on request. |
| OPS-24 | Azure Monitor distro for new apps | The Microsoft OpenTelemetry Distro (Learn). Keep `UseAzureMonitor` where it exists. |
| SEC-24 | `--no-sandbox` for Chrome | Allowed only with an ADR that names the isolation, and only for trusted templates. |

## Catalog changes

When the owner accepts the sheet, the new and changed rows go into `docs/work/dev-team/defaults-catalog.md`, and each skill gets them in `references/decisions.md`.
