# Changelog

This file records all notable changes to the dev-stack plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.0] - 2026-10-25

### Added

- Stack rule 3: the style of existing code wins, its defects do not. The agent checks the existing code that the change calls, copies, or edits. It fixes a defect that the change depends on and reports the others as open points.
- The report names each advisory that the project hides (`NuGetAuditSuppress`, `NoWarn` NU190x, `NuGetAuditLevel`) and says if its reason still holds.
- Skill rules for defects in existing code:
  - `sqlserver`: UTC days in a report, `nvarchar(4000)` on a `varchar` column, unordered GUID keys, and `MERGE` without `HOLDLOCK`.
  - `postgresql`: a table of defects.
  - `redis`: cache defects.
  - `rabbitmq`: consumer base classes.
- New tool rules: `PG-LOCAL-DATE-NO-ZONE`, `MAIL-AUDIT-HIDDEN`, `FV-CLASS-CASCADE-STOP`, `PPTR-PDF-FORMAT`, `PPTR-PDF-BACKGROUND`, `WOLV-TYPED-HTTPCLIENT`, and `RDS-PKG-SER2`.
- `wolverine`: a Review section (follow each handler parameter to its registration, missing forwarders, generated code in `Static` mode).
- `fluentvalidation`: the report gives a table of the error codes of the endpoint.
- Eval set: 22 hard cases (a write case and a review case for each batch 2 skill), with probes, planted mistakes, decoys, and a self-test. Results in `docs/work/dev-stack/hard-cases/results.md`.

### Changed

- Each skill has `verified: 2026-10-25`. New versions:
  - SQL Server 2025 CU10, and RabbitMQ 4.4 for a new broker.
  - PuppeteerSharp 25.15.0, WolverineFx 6.53.0, StackExchange.Redis 3.4.20, and MailKit and MimeKit 4.18.2.
  - Mailpit v1.32.0, OpenTelemetry .NET 1.20.1, Scalar.AspNetCore 2.18.3, and Swashbuckle.AspNetCore 10.3.1.
- Facts that prevent a silent wrong result moved from `references/` into a short rule in `SKILL.md`.
- `mailkit`: MimeKit 4.18.2 depends on Pkcs 10.0.13, so the Pkcs pin is only for MimeKit 4.18.1 and older.
- `elasticsearch`: an 8.x client works with a 9.x server. The 8-to-9 table now covers 8.18 and older.
- `rabbitmq`: the report names the owner of each dead-letter queue.
- `sqlserver`: the report says how a database setting change is deployed.
- `redis`: the report says why each cached type is a DTO, and how a write decision gets its data.

### Fixed

- `mailkit`: MailKit below 4.16.0 has CVE-2026-41319. The version rule and `MAIL-PKG-MIMEKIT-CVE` now check it.
- `dotnet-observability`: `SamplingRatio = 1.0F` alone keeps about 5 traces per second with the Azure Monitor distro 1.5.0 and later, because `TracesPerSecond` wins. Log scopes are off by default with both distros.
- `elasticsearch`: one alias descriptor holds one action, so `RemoveIndex` and `Add` on one descriptor send only the add.
- `puppeteersharp`: `PdfOptions` without `Format` prints Letter, and `PrintBackground` is false by default.
- `fluentvalidation`: `ClassLevelCascadeMode.Stop` stops the whole validator. `SetValidator` skips a null item.
- `aspnetcore-openapi`: a file response needs `Produces<Stream>(200, "application/pdf")`. The `WithOpenApi` delegate does not reach the document in .NET 10.
- `wolverine`: a handler that injects a typed `HttpClient` throws `InvalidServiceLocationException`.
- `rabbitmq`: `basic.nack` with requeue does not count toward the 4.3 delivery limit. Global QoS did not fail on 4.3.6. A consumer timeout cancels the consumer.
- `fluentvalidation` tool rule `FV-CASCADE-DEPENDENT`: `NotEmpty()` then `Matches(` without a cascade.

## [1.1.0] - 2026-10-20

### Added

- 11 new skills. Each has version-specific rules, a review checklist, a check step with `stackcheck.py` and `rules.json`, and the shared report format:
  - `aspnetcore-openapi`: the built-in OpenAPI generator, response metadata for client generators, Scalar, the build-time document, Asp.Versioning, and breaking-change checks with oasdiff.
  - `fluentvalidation`: FluentValidation 12 validators, the call from an endpoint filter, a controller, or a worker, and stable error codes.
  - `sqlserver`: T-SQL, isolation levels and RCSI, lock hints, deadlocks and retries, upserts, queue tables, and SqlClient with Dapper.
  - `postgresql`: SQL for PostgreSQL, row locks, a queue table with `SKIP LOCKED`, upserts, DDL on a live table, `NpgsqlDataSource`, and a report per local day.
  - `dotnet-observability`: `ILogger` and `LoggerMessage`, personal data in logs and redaction, OpenTelemetry traces and metrics, and Azure Monitor with sampling.
  - `redis`: HybridCache, keys and TTLs, invalidation over more than one node, and StackExchange.Redis 3 on Redis or Valkey.
  - `elasticsearch`: Elasticsearch 9 with the .NET client 9, mappings and aliases, bulk indexing item by item, the sync from the database, and point-in-time exports.
  - `rabbitmq`: RabbitMQ 4.3 with RabbitMQ.Client 7, quorum queues, publisher confirms, consumers, and dead letters.
  - `wolverine`: Wolverine 6 handlers, the durable outbox on EF Core, error policies, and RabbitMQ transport with quorum queues.
  - `mailkit`: MailKit 4 and MimeKit 4, the TLS setting, the SMTP password, a mail outbox with retries, and OAuth2 SMTP.
  - `puppeteersharp`: HTML to PDF with PuppeteerSharp and headless Chrome, the Chrome sandbox in a container, SSRF, and user text in the HTML.
- Shared messaging principles (`shared/messaging-principles.md`) in `rabbitmq` and `wolverine`: at-least-once delivery, idempotent consumers, the outbox, contracts, names, and dead letters. `scripts/sync_shared.py` keeps the copies equal.
- Stack rules: one package pin has standing approval. SqlClient 6.1 and 7.1 and MimeKit 4.18 pull a vulnerable `System.Security.Cryptography.Pkcs`, so the agent pins Pkcs 10.0.x and names the pin in the report.
- Eval set: 22 new cases, a trap case and a review case for each new skill. Probe tests, planted review mistakes, `run.py` for headless runs, `grade.py` for blind graders, `compare.py`, and `selftest.py`. The selftest checks that each probe passes a good solution and fails a naive one.

### Changed

- `dotnet-testing`: the examples pin `postgres:18.6-alpine`.

### Fixed

- `stackcheck.py`: a `PackageReference` without a version under Central Package Management no longer matches a version rule. The `PackageVersion` in `Directory.Packages.props` carries the version, and the check reads it there.

## [1.0.0] - 2026-10-18

### Added

- First release of the dev-stack plugin for Claude Code, with seven skills: `csharp`, `aspnetcore`, `efcore`, `dotnet-testing`, `dotnet-analyzers`, `secure-coding`, and `migrate-fluentassertions`.
- Shared stack rules in each skill (`references/stack-rules.md`): package approval, no MediatR and no AutoMapper, branch and commit without push, team decisions, and one report format.
- A check step in each skill with `stackcheck.py` and the tool rules in `rules.json`. `dotnet-analyzers` also has `sarif.py`.
- The report rules: run the checks again after the last change, and show only the results of that last run. A "Cause:" line for a fix of a problem that is not obvious. Each recommendation says what to change, how, and why.
- `secure-coding` reviews use the common review template, with the verdicts PASS, PASS_WITH_FINDINGS, and BLOCKED.
- `secure-coding` rule 10: antiforgery only for an endpoint with cookie sign-in. An API with bearer or API-key sign-in uses `.DisableAntiforgery()` with a reason. Rule 10a: when the sign-in decision is open, keep the current sign-in setup and put the missing sign-in as a blocking open point.
- `dotnet-analyzers` rule 18a: the report explains each rule that the agent raises, lowers, or turns off. It says what the rule prevents and why the change fits the project.
- `efcore` rule 32a: with one idempotent migration script on PostgreSQL, build a non-blocking index from a pre-deploy SQL file, and use `CREATE INDEX IF NOT EXISTS` in the migration.
- `csharp` rule 36a: bind a map or an enum from configuration as `Dictionary<string, string>` and convert each entry. The configuration binder skips entries that it cannot convert, without an error.
- An eval set in `evals/dev-stack/`: 18 cases, fixtures, probe tests, `score.py`, `prepare.py`, `consistency.py`, and the grader prompt. The efcore concurrency probe changes the order through the change tracker, so it does not depend on a domain method that an agent can rename.
