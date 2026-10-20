# dev-stack

dev-stack is a Claude Code plugin for a .NET 10 team. It has 18 stack skills. Each skill tells the agent what is true for this version of the stack and what the agent usually gets wrong. Each skill also has a check step with a tool and one report format.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code, the .NET 10 SDK, and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-stack@maikelsoft-skills
```

## Quick start

1. Open Claude Code in a .NET repository.
2. Ask for a change or a review in your own words:

   ```
   Add an endpoint that cancels an order. Answer 409 when the order is shipped.
   ```

   ```
   Review the changes on this branch before we merge.
   ```

3. Claude Code loads the skills that match the work. One request can load more than one skill, for example `aspnetcore` and `efcore`.
4. The agent builds, runs the checks again after its last change, and writes a report. The report has a verdict, the checks with their results, the decisions, the assumptions, and the open points.

## Skills

| Skill | Use it for |
|-------|------------|
| `csharp` | C# 14 code and project files: nullability, records, async and cancellation, disposal, exceptions, `TimeProvider`, collections, `Directory.Build.props`, and Central Package Management. |
| `aspnetcore` | The host: DI lifetimes, options, middleware order, minimal API endpoints, ProblemDetails, typed HTTP clients, health checks, and OpenTelemetry. |
| `aspnetcore-openapi` | The built-in OpenAPI document, response metadata for client generators, Scalar, the build-time document, Asp.Versioning, and breaking-change checks with oasdiff. |
| `fluentvalidation` | FluentValidation 12: rules, cascade mode, error codes, async rules, the call from an endpoint filter, a controller, or a worker, and validator tests. |
| `efcore` | EF Core 10 on SQL Server or PostgreSQL: queries, `SaveChanges`, concurrency tokens, value objects, migrations, and the deploy of migrations. |
| `sqlserver` | SQL Server and Azure SQL: T-SQL, column types, indexes, isolation levels and RCSI, lock hints, deadlocks, upserts, queue tables, and SqlClient with Dapper. |
| `postgresql` | PostgreSQL: SQL text, column types, indexes, row locks, and a queue table with `SKIP LOCKED`. Also upserts, DDL on a live table, `NpgsqlDataSource`, and a report per local day. |
| `dotnet-observability` | Logs, traces, and metrics: `ILogger`, `LoggerMessage`, personal data and redaction, OpenTelemetry, and Azure Monitor with sampling. |
| `redis` | Caching with HybridCache, cache keys and TTLs, invalidation over more than one node, and StackExchange.Redis 3 on Redis or Valkey. |
| `elasticsearch` | Elasticsearch 9 with the .NET client 9: mappings, aliases, reindex, bulk indexing, the sync from the database, search, and exports with point in time. |
| `rabbitmq` | RabbitMQ 4.3 with RabbitMQ.Client 7: exchanges, quorum queues, publisher confirms, consumers, dead letters, and the shared messaging principles. |
| `wolverine` | Wolverine 6: handlers, the durable outbox and inbox on EF Core, error policies, local queues, and the RabbitMQ transport. |
| `mailkit` | Outgoing mail with MailKit 4 and MimeKit 4: TLS, the SMTP password, a mail outbox with retries, templates, and OAuth2 SMTP. |
| `puppeteersharp` | HTML to PDF and screenshots with PuppeteerSharp and headless Chrome: the Chrome sandbox, Chrome in a container, SSRF, and user text in the HTML. |
| `dotnet-testing` | MSTest 4 on Microsoft.Testing.Platform, FluentAssertions 7.2.2 or AwesomeAssertions 9, fakes, `FakeTimeProvider`, `WebApplicationFactory`, and Testcontainers. |
| `dotnet-analyzers` | SDK analyzers, warnings as errors, Roslynator, Meziantou, `BannedSymbols.txt`, and the CI gate. |
| `secure-coding` | Security rules for writing and reviewing code: sign-in, access control, input, secrets, supply chain, and containers. |
| `migrate-fluentassertions` | The step-by-step move of a test project from FluentAssertions 5, 6, or 7 to AwesomeAssertions 9. |

## Team rules in each skill

- Each new package needs the approval of the human. The agent writes a proposal for other packages.
- MediatR and AutoMapper are not allowed.
- The agent can make a branch and commit on it. It does not push and does not open a pull request.
- `docs/dev-team/decisions.md` holds the accepted team decisions. When a decision is missing, the agent asks. In a run without a human, it marks the answer as an assumption.

## Checkers

| Script | What it checks |
|--------|----------------|
| `stackcheck.py` | The code against the tool rules of the skill (`rules.json`): banned APIs, old patterns, and package policy. |
| `sarif.py` | The SARIF output of the analyzers (`dotnet-analyzers` only). |
| `doccheck.py` | The report text, against ASD-STE100 or Begrijpelijk Nederlands. |

## Test results

Method, cases, and results are in `evals/dev-stack/`.

### Version 1.1.0: the 11 new skills

Each new skill has two cases. A trap case builds a change with a hidden trap. A review case reviews a change with planted mistakes. Each case ran 3 times without the skill and 3 times with it: 132 runs on Opus. Blind graders scored the reports. Scripts ran the build, the tests, and hidden probe tests.

| Measure | No skill | dev-stack 1.1.0 |
|---------|----------|-----------------|
| Checks and grader items passed, 11 trap cases | 85% | 98% |
| Failed checks, 33 trap runs | 29 | 1 |
| Planted mistakes found per review (8 or 9 planted) | 7.5 | 8.1 |
| False findings per review | 0.15 | 0 |
| Cost per run | 100% | about 110% |

Per case, the skill variant wins 13 cases, ties 8, and loses 1 (sign test p = 0.002). The largest gains: `mailkit` (the outbox while the relay is down, TLS, encoding), `wolverine` (quorum queues), `puppeteersharp` (no `--no-sandbox`), and `dotnet-observability` (sampling). `postgresql` ties: the agent already solves its trap without the skill.

### Version 1.0.0: the first seven skills

Each version ran 18 cases: 12 normal cases and 6 cases with a hidden trap. The capability cases had 5 runs for each version, the other cases 3 runs. Blind graders scored the reports. Scripts ran the build, the tests, the format check, and probe tests.

| Measure | No skill | dev-stack 1.0.0 |
|---------|----------|-----------------|
| Checks and grader items passed, 18 cases | 90.1% | 99.0% |
| Runs without a failure, 8 capability cases | 29% | 92% |
| Each changed analyzer rule says what it prevents | 6 of 10 runs | 10 of 10 runs |
| Same report headings over runs (overlap) | 0.23 | 0.98 |

The last two fixes had a test of their own: `efcore` rule 32a and `csharp` rule 36a. Then efcore-trap and csharp-trap were clean in 5 of 5 runs each. Before the fixes, efcore-trap was clean in 1 of 3 runs and csharp-trap in 2 of 5 runs.

## Limits

- On cases that the agent already does well, the skills add little. The gain is on version traps and quiet defaults. Examples: MSTest 4 with FluentAssertions 7, analyzer gates, classic queues, the Chrome sandbox, and trace sampling.
- The review cases are near the ceiling: without the skill, the agent already finds most planted mistakes.
- The runs used one model (Opus). The gain can be different on other models.
- `migrate-fluentassertions` has no eval case yet.
- The report limit of 700 words is not met. The mean is about 935 words.
- The skills do not replace the build and the tests. The agent must run them, and the report shows only the results of the last run.
