# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Security
- Context lifetime
- Queries
- Writes and transactions
- Model
- Migrations and deployment
- Tests
- Decisions

## Security

| # | Check | Type |
|---|-------|------|
| 1 | No interpolation or concatenation inside `FromSqlRaw`, `SqlQueryRaw`, or `ExecuteSqlRaw`. | tool (EF1002, EF1003, EF-RAW-SQL-INTERP, EF-RAW-SQL-CONCAT) |
| 2 | A dynamic identifier in raw SQL comes from an allow list. Values go in as parameters. | judge |
| 3 | `EnableSensitiveDataLogging` is on only in development. | tool (EF-SENSITIVE-LOGGING) |
| 4 | No connection string with a password in source code. | judge (`dev-stack:secure-coding`) |
| 5 | Tenant tables have a named tenant filter. No `IgnoreQueryFilters()` without names in tenant code. | judge |

## Context lifetime

| # | Check | Type |
|---|-------|------|
| 6 | Web code uses a scoped context (`AddDbContext`). Pooling only with a measured reason and no state per request. | judge |
| 7 | Background services, singletons, and parallel work use `IDbContextFactory<T>`. | judge |
| 8 | No two operations at the same time on one context (`Task.WhenAll`, a second query while a stream is open). | judge |
| 9 | Interceptors are singletons, or scoped without pooling when they have state. | judge |

## Queries

| # | Check | Type |
|---|-------|------|
| 10 | Read endpoints project to DTOs. | judge |
| 11 | `AsNoTracking` only on entity results for read only. Not on DTO projections. | judge |
| 12 | No navigation access in a loop. No lazy loading. | tool (EF-LAZY-PROXIES, EF-PKG-PROXIES), judge |
| 13 | Two or more sibling collection `Include`s: a projection, `AsSplitQuery()`, or a reasoned `AsSingleQuery()`. | tool (`MultipleCollectionIncludeWarning` throws in tests), judge |
| 14 | No global split query behavior. | tool (EF-GLOBAL-SPLIT) |
| 15 | Filters, projections, and aggregates run in SQL, not after `ToListAsync` or `AsEnumerable`. | judge |
| 16 | Each paged query has a unique, stable order. Deep pages use keyset paging. | judge |
| 17 | Predicates compare the bare column. No `ToLower()` for case-insensitive search. | judge |
| 18 | Large `Contains` lists use `EF.Parameter` or a fitting collection mode. | judge |
| 19 | Async EF methods in async code, with the `CancellationToken`. | tool (CA1849, CA2016) |
| 20 | No `ToAsyncEnumerable()` on EF queries. No `System.Linq.Async` package on .NET 10. | tool (EF1004 on EF 11, EF-PKG-LINQ-ASYNC) |
| 21 | Compiled queries only for hot queries with a fixed shape. | judge |
| 22 | A performance finding from source code only says "check with the plan". | judge |

## Writes and transactions

| # | Check | Type |
|---|-------|------|
| 23 | One `SaveChangesAsync` per request or message. No hidden save in a loop. | judge |
| 24 | `ExecuteUpdate` and `ExecuteDelete` callers do not need tracked state, concurrency tokens, interceptors, or client cascades. | judge |
| 25 | No tracked change of the same rows after an `ExecuteUpdate` in the same unit of work. | judge |
| 26 | Edited aggregates have a concurrency token. `DbUpdateConcurrencyException` maps to 409 or a retry. | judge |
| 27 | Uniqueness comes from a unique index and a caught violation, not from a check before the insert. | judge |
| 28 | With retries on, explicit transactions run inside `CreateExecutionStrategy().ExecuteAsync`, and the delegate clears the tracker and loads its data. | judge |
| 29 | Transactions are short. No HTTP call, message send, or user wait inside. | judge |
| 30 | A retried unit is idempotent, or uses `ExecuteInTransactionAsync` with a check. | judge |
| 31 | No third-party bulk library without a license check and approval. | tool (EF-PKG-BULKEXT, EF-PKG-ZZZ) |
| 32 | After a write, the code removes the cache key. It does not write the new value into the cache. | judge |

## Model

| # | Check | Type |
|---|-------|------|
| 33 | Each `decimal` has a precision (convention or property). | tool (`DecimalTypeDefaultWarning` throws in tests) |
| 34 | SQL Server strings in an index or a filter have a max length. | judge |
| 35 | Value objects are complex types in new code. | tool (EF-OWNED-TYPE) |
| 36 | `Guid` keys come from the provider. No `Guid.NewGuid()` or `Guid.CreateVersion7()` for keys. | tool (EF-GUID-KEY) |
| 37 | PostgreSQL concurrency uses `[Timestamp] uint` (xmin), not `UseXminAsConcurrencyToken`. | tool (EF-XMIN-OLD) |
| 38 | Instants are UTC `DateTimeOffset` from `TimeProvider`. No `DateTime.Now`. No legacy timestamp switch. | tool (EF-LEGACY-TIMESTAMP, CS rules of `dev-stack:csharp`), judge |
| 39 | Enums use the convention of DATA-12. | judge |
| 40 | Case-insensitive columns use a collation (or `ILike` on PostgreSQL). | judge |
| 41 | Query filters have names. | judge |
| 42 | The code sets the provider version (`UseCompatibilityLevel`, `SetPostgresVersion`). | judge |

## Migrations and deployment

| # | Check | Type |
|---|-------|------|
| 43 | Each model change has a migration. | tool (`dotnet ef migrations has-pending-model-changes`) |
| 44 | The new migration has no drop and add for a rename and no unwanted type change. A new `NOT NULL` column on a full table has a default. | judge |
| 45 | Large PostgreSQL tables get concurrent indexes, and that migration does not go through an idempotent script. | judge |
| 46 | No `EnsureCreated` in app code. | tool (EF-ENSURE-CREATED) |
| 47 | No startup `Migrate` without an accepted DATA-18 decision and a reason on the line. | tool (EF-STARTUP-MIGRATE) |
| 48 | The bundle job uses a deployment identity with DDL rights. The app identity has DML rights only. | judge |
| 49 | No edits to applied migrations or to the snapshot by hand. | judge |
| 50 | Seeding delegates are idempotent, and the sync and async versions both exist. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 51 | No InMemory provider and no mocked `DbSet`. | tool (EF-INMEMORY, EF-PKG-INMEMORY, EF-MOCK-DBSET) |
| 52 | Tests use the production engine in a container with a pinned image tag, and apply the migrations one time. | judge (`dev-stack:dotnet-testing`) |
| 53 | Non-trivial queries, concurrency, constraints, and filters have tests against the real engine. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 54 | Code that uses a DATA decision follows the accepted answer in `docs/dev-team/decisions.md`. | tool (DET-DATA-* with `decisioncheck.py`), judge |
| 55 | No generic repository or `IUnitOfWork` without an accepted DATA-05 or DATA-06 decision. | tool (DET-DATA-05, DET-DATA-06), judge |
