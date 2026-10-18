---
name: efcore
description: Data access code with Entity Framework Core 10 on SQL Server or PostgreSQL. Use when code or a review touches DbContext, DbSet, LINQ queries, Include, SaveChanges, ExecuteUpdate, raw SQL through EF, transactions, concurrency tokens, value objects, query filters, interceptors, migrations (dotnet ef, Migrations folder), seed data, or migration deployment in CI. Not for Dapper or plain ADO.NET code, T-SQL or PostgreSQL engine work (indexes, isolation, locks), or EF6 on .NET Framework.
metadata:
  verified: "2026-10-16"
  versions: "EF Core 10.0 (LTS), EFCore.PG 10.0, SQL Server 2025, PostgreSQL 18, .NET 10. EF Core 11 RC 2 in references/ef11.md"
---

# EF Core

How to use EF Core well, with any relational provider. Engine rules (T-SQL, indexes, isolation, locks) are in `dev-stack:sqlserver` and `dev-stack:postgresql`. Test database setup is in `dev-stack:dotnet-testing`.

## First

1. Read `references/stack-rules.md`.
2. Find the EF version: the `Microsoft.EntityFrameworkCore*` package in `*.csproj` or `Directory.Packages.props`.
   - EF Core 10: use this file.
   - EF Core 11: use this file and read `references/ef11.md`.
   - EF Core 8 or 9: support ends on 2026-11-10. These rules apply, except the EF 10 APIs: complex type JSON, named query filters, `LeftJoin`, and the block lambda in `ExecuteUpdateAsync`. Tell the human about the upgrade. Do not upgrade unless the task asks for it.
3. Find the provider: `UseSqlServer`, `UseAzureSql`, or `UseNpgsql`. Read `references/provider-sqlserver.md` or `references/provider-postgresql.md`.

Done when you can name the EF version, the provider, and the provider version setting.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Library | EF Core only. `Select` projections for reads, `SqlQuery<T>` for the rare query that LINQ cannot express. | DATA-02 |
| Context | One DbContext per service. One per module in a modular monolith, with its own schema. | DATA-03, DATA-04 |
| Data access | The DbContext directly in handlers. No generic repository, no `IUnitOfWork`. | DATA-05, DATA-06 |
| Keys | `Guid` keys that the provider generates. Do not set them in code. | DATA-07 |
| Names | SQL Server: PascalCase. PostgreSQL: snake_case with `EFCore.NamingConventions`. | DATA-09 |
| Time | `DateTimeOffset` (UTC) for instants, `DateOnly` and `TimeOnly`, `TimeProvider` for now. | DATA-10 |
| Decimals | `Properties<decimal>().HavePrecision(18, 4)`. Money as a complex type when there are two or more currencies. | DATA-11 |
| Enums | Strings, set in `ConfigureConventions`. | DATA-12 |
| Audit | `CreatedAt` and `UpdatedAt` from one `SaveChangesInterceptor`. | DATA-14 |
| Concurrency | `rowversion` (SQL Server) or `xmin` (PostgreSQL) on edited aggregates. | DATA-15 |
| Migrations | EF migrations. Deploy with a bundle job. No `Migrate` at startup. | DATA-17, DATA-18 |
| Bulk | `ExecuteUpdateAsync` and `ExecuteDeleteAsync`. `SqlBulkCopy` or Npgsql `COPY` for big inserts. | DATA-22 |

Existing code keeps its style (stack rule 3).

## Rules

### Context and setup

1. Register the context with `AddDbContext` (scoped, one per request). Use `AddDbContextPool` only when profiling shows that setup cost matters and the context has no state per request. A pooled context keeps fields such as a tenant ID from the last request.
2. In background services, singletons, Blazor Server, and parallel work, inject `IDbContextFactory<T>`. Create and dispose one context for each unit of work. A singleton that holds a scoped context keeps it alive forever.
3. Run one operation at a time on a context. Do not use `Task.WhenAll` on queries of one context, because EF throws "A second operation started on this context".
4. Put the model in `IEntityTypeConfiguration<T>` classes with `ApplyConfigurationsFromAssembly`. Put type-wide defaults in `ConfigureConventions`. Then a new property gets the correct precision, length, and enum mapping without extra code.
5. Set the provider version to the real server version. SQL Server 2025: `UseCompatibilityLevel(170)`. PostgreSQL 18: `SetPostgresVersion(18, 0)`. EF writes SQL for the version that it assumes, and the defaults are older.
6. Turn on `EnableRetryOnFailure` for cloud and container databases. Then follow rule 20.

```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        sql.UseCompatibilityLevel(170); // SQL Server 2025: native json type
        sql.EnableRetryOnFailure();
    });
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
    options.EnableSensitiveDataLogging(env.IsDevelopment());
});
```

### Queries

7. Project read endpoints to DTOs with `Select`. EF reads only the columns that you need, and it tracks nothing.
8. Add `AsNoTracking()` only to read-only queries that return entities. A projection without entities has nothing to track. EF tracks an entity inside a DTO.
9. Load related data in the same query, with a projection or `Include`. Do not read a navigation inside a loop (N+1). Do not turn on lazy loading.
10. Add `AsSplitQuery()` to a query that includes two or more sibling collections. Keep single queries as the default. A split query sends more statements and can read data that changed between them.
11. Give each paged query a unique, stable order (`OrderBy(...).ThenBy(o => o.Id)`). Use keyset paging for deep pages, because `Skip` reads and discards each skipped row.
12. Compare the bare column: `o.CreatedAt >= start && o.CreatedAt < end`, not `o.CreatedAt.Year == year`. Do not use `ToLower()` for case-insensitive search. Use a collation (see the provider reference).
13. Use the async methods (`ToListAsync`, `SingleAsync`, `SaveChangesAsync`) and pass the `CancellationToken`. Stream large results with `AsAsyncEnumerable()`.
14. Use `EF.CompileAsyncQuery` only for a hot query with a fixed shape that runs many times each second. EF already caches the translation of each query shape.
15. For a `Contains` with a large list, write `EF.Parameter(ids).Contains(o.Id)`. EF 10 sends one parameter for each element by default, and SQL Server allows at most 2100 parameters.

Detail, evidence rules, and examples: `references/queries.md`.

### Writes

16. Call `SaveChangesAsync` one time per request or message. It sends all changes in one transaction.
17. Use `ExecuteUpdateAsync` and `ExecuteDeleteAsync` for set-based changes without logic per entity. They run at once and skip the change tracker, concurrency tokens, and `SaveChanges` interceptors. Do not change the same rows through tracked entities in the same unit of work.
18. Put a concurrency token on each aggregate that two users or two messages can change (DATA-15). Catch `DbUpdateConcurrencyException` and return 409, or retry the message.
19. Put a unique index on a unique column and catch the violation. A check before the insert does not stop two inserts at the same time.
20. When retries are on, run each explicit transaction inside `CreateExecutionStrategy().ExecuteAsync`. Clear the change tracker at the start, because the strategy can run the delegate again. Without the strategy, EF throws.
21. Keep a transaction short. Do not call an HTTP service or wait for a user inside it.
22. When a cache holds the data, save first, then remove the cache key. Do not write the new value into the cache from the write path (`dev-stack:redis`).

```csharp
var strategy = db.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async token =>
{
    db.ChangeTracker.Clear(); // the delegate can run again: start clean
    await using var tx = await db.Database.BeginTransactionAsync(token);
    var order = await db.Orders.SingleAsync(o => o.Id == id, token);
    order.Status = OrderStatus.Shipped;
    await db.SaveChangesAsync(token);
    await db.Orders
        .Where(o => o.CustomerId == order.CustomerId && o.Status == OrderStatus.Draft)
        .ExecuteDeleteAsync(token);
    await tx.CommitAsync(token);
}, ct);
```

Detail on bulk, concurrency, transactions, and cache invalidation: `references/writes.md`.

### Raw SQL

23. Use `FromSql`, `SqlQuery<T>`, and `ExecuteSqlAsync` with an interpolated string. EF sends each value as a parameter. Use `FromSqlRaw` only for an identifier that you check against an allow list, and pass the values as `{0}` parameters.

```csharp
db.Customers.FromSql($"SELECT * FROM Customers WHERE Email LIKE {"%@" + domain}")
```

### Model

24. Map value objects as complex types: `ComplexProperty(o => o.Address)` for columns, `ComplexProperty(o => o.Details, b => b.ToJson())` for a JSON column, `ComplexCollection(..., b => b.ToJson())` for a list. Owned types are entity types with a hidden key.
25. Let the provider generate `Guid` keys (sequential on SQL Server, UUIDv7 on EFCore.PG 9+). Do not set `Id = Guid.NewGuid()`.
26. Store instants as `DateTimeOffset` in UTC. Read the time from `TimeProvider`. Use `DateOnly` and `TimeOnly` for calendar values.
27. Register an interceptor one time as a singleton and add that instance. A new instance for each context makes EF build a new internal service provider each time.
28. Give each query filter a name (`HasQueryFilter("Tenant", ...)`). Then a query can switch off one filter with `IgnoreQueryFilters(["SoftDelete"])` and keep the tenant filter.

Detail on conventions, converters, interceptors, filters, seed data, and time: `references/modeling.md`.

### Migrations

29. Read each generated migration before you commit it. EF writes a column rename as a drop and an add, and that deletes the data.
30. Run `dotnet ef migrations has-pending-model-changes` in CI. EF 9+ also throws at `Migrate` when the model has changes without a migration.
31. Deploy migrations with a bundle job or an idempotent script (DATA-18). Do not call `Database.Migrate` at startup without an accepted decision. Do not call `EnsureCreated` outside tests.
32. On a merge conflict in the model snapshot, remove your migration, merge, and add your migration again. Do not edit the snapshot by hand.
32a. Some teams deploy with one idempotent script (`migrations script --idempotent`). When a PostgreSQL index must not block writes there, keep that one script. Do not use `IsCreatedConcurrently()`: PostgreSQL rejects `CREATE INDEX CONCURRENTLY` in the `DO` block of the script (SQLSTATE 25001). Do these three steps:
    - Add a pre-deploy SQL file that the DBA runs with psql before the script, outside a transaction. It drops an INVALID index with the same name, then runs `CREATE INDEX CONCURRENTLY IF NOT EXISTS`.
    - In the migration, replace the generated `CreateIndex` with `migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ...")` with the same index name. Keep `HasIndex` in the model, so the snapshot stays correct.
    - Run the idempotent script after the pre-deploy file, and on a new database. Both runs must end without an error.

Detail on teams, CI, bundles, scripts, seed data, and expand-contract: `references/migrations.md`.

### Logging

33. Turn on `EnableSensitiveDataLogging` only in development. It writes parameter values to the log.

## Never write

| Wrong | Correct |
|-------|---------|
| `FromSqlRaw($"... {name}")` or `"... " + name` | `FromSql($"... {name}")` |
| `FromSqlInterpolated(...)` | `FromSql(...)` |
| `options.UseInMemoryDatabase("test")` | Testcontainers with the production engine (`dev-stack:dotnet-testing`) |
| `new Mock<DbSet<Order>>()` | A query test against the real engine |
| `db.Database.EnsureCreated()` in app code | Migrations, deployed by a bundle or a script |
| `app.Services...Database.Migrate()` in `Program.cs` | Bundle job (`dotnet ef migrations bundle`), or a startup call with a reason (DATA-18) |
| `OwnsOne(o => o.Address)`, `OwnsMany(..., b => b.ToJson())` | `ComplexProperty(o => o.Address)`, `ComplexCollection(..., b => b.ToJson())` |
| `UseXminAsConcurrencyToken()` | `[Timestamp] public uint Version { get; set; }` |
| `public Guid Id { get; set; } = Guid.NewGuid();` | `public Guid Id { get; set; }` (the provider generates it) |
| `Guid.CreateVersion7()` for a SQL Server clustered key | The EF sequential GUID generator (default) |
| `CreatedAt = DateTime.Now` | `CreatedAt = clock.GetUtcNow()` |
| `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` | UTC values |
| `Where(u => u.Email.ToLower() == email.ToLower())` | A case-insensitive collation, or `EF.Functions.ILike` on PostgreSQL |
| `await Task.WhenAll(QueryA(db), QueryB(db))` | Two awaits, or one context per task from `IDbContextFactory<T>` |
| `BeginTransactionAsync()` with retries on, outside a strategy | `CreateExecutionStrategy().ExecuteAsync(...)` |
| `Expression.Lambda<...>` to build `SetPropertyCalls` | Block lambda: `ExecuteUpdateAsync(s => { if (x) s.SetProperty(...); })` |
| `GroupJoin(...).SelectMany(... DefaultIfEmpty())` | `LeftJoin(...)` (.NET 10) |
| `.AsNoTracking().Select(o => new Dto(...))` | `.Select(o => new Dto(...))` |
| `UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` for all queries | `AsSplitQuery()` on the query that needs it |
| `IRepository<T>` over `DbSet<T>` | The DbContext directly (DATA-05) |
| `db.Orders.BulkInsertAsync(rows)` | `SqlBulkCopy` or Npgsql `COPY`. The BulkExtensions package has a dual license. |
| `ToAsyncEnumerable()` on an EF query | `AsAsyncEnumerable()` |
| Package `System.Linq.Async` on .NET 10 | Nothing: async LINQ is in the base library |
| "Npgsql 10 generates UUIDv7 keys" | The EF provider EFCore.PG 9+ generates them. Npgsql (ADO.NET) does not. |

## Check

1. Build with the analyzers as errors. EF1002 and EF1003 (Raw SQL with interpolation or concatenation) come with the EF package.

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings.

2. Check that the model has no changes without a migration:

   ```
   dotnet ef migrations has-pending-model-changes --project <data project> --startup-project <app project>
   ```

   Pass: exit code 0. Exit code 1 means that a migration is missing.

3. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

4. Run the tests with `ConfigureWarnings(w => w.Throw(...))` for `SqlServerEventId.DecimalTypeDefaultWarning` and `RelationalEventId.MultipleCollectionIncludeWarning`. Pass: all tests green.

## Review checklist

The full list is in `references/review-checklist.md`.

1. No interpolation or concatenation in `FromSqlRaw`, `SqlQueryRaw`, or `ExecuteSqlRaw`. tool (EF1002, EF1003, EF-RAW-SQL-INTERP, EF-RAW-SQL-CONCAT)
2. No InMemory provider and no mocked `DbSet`. tool (EF-INMEMORY, EF-PKG-INMEMORY, EF-MOCK-DBSET)
3. No `EnsureCreated` in app code, no startup `Migrate` without a decision. tool (EF-ENSURE-CREATED, EF-STARTUP-MIGRATE)
4. CI runs `has-pending-model-changes`, and each new migration has no drop and add for a rename. tool (pipeline), judge
5. Each `decimal` has a precision. tool (`DecimalTypeDefaultWarning` throws in tests)
6. A query with two or more sibling collection `Include`s uses `AsSplitQuery()` or a projection. tool (`MultipleCollectionIncludeWarning`), judge
7. No navigation access in a loop, no lazy loading. tool (EF-LAZY-PROXIES, EF-PKG-PROXIES), judge
8. Async EF calls in async code, with the `CancellationToken`. tool (CA1849, CA2016)
9. No two operations at the same time on one context. Background services use `IDbContextFactory<T>`. judge
10. Read endpoints project to DTOs. `AsNoTracking` only on entity reads. judge
11. Paged queries have a unique order. judge
12. Edited aggregates have a concurrency token, and the code handles `DbUpdateConcurrencyException`. judge
13. Explicit transactions run inside the execution strategy when retries are on. judge
14. `ExecuteUpdate` and `ExecuteDelete` callers do not need tracked state, tokens, or interceptors. judge
15. Value objects are complex types. GUID keys come from the provider. tool (EF-OWNED-TYPE, EF-GUID-KEY)

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/decisions.md`: read before new code needs a DATA decision. It has the question, the options, and the recommendation.
- `references/queries.md`: read when you write or review a query (projections, N+1, split queries, paging, `Contains` lists).
- `references/writes.md`: read when you write `SaveChanges`, `ExecuteUpdate`, a transaction, a concurrency token, bulk work, or cache invalidation.
- `references/modeling.md`: read when you add an entity, a value object, a converter, an interceptor, a query filter, seed data, or a date column.
- `references/migrations.md`: read before you add, merge, review, or deploy a migration.
- `references/testing.md`: read before you write a test that uses a DbContext.
- `references/provider-sqlserver.md`: read when the project uses `UseSqlServer` or `UseAzureSql`.
- `references/provider-postgresql.md`: read when the project uses `UseNpgsql`.
- `references/ef11.md`: read only when the project uses EF Core 11.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
