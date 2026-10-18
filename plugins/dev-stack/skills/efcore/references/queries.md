# Queries

Rules for LINQ queries with EF Core 10. Part of this file paraphrases the MIT-licensed `optimizing-ef-core-queries` skill of github.com/dotnet/skills (see `sources.md`).

## Contents

- Review on evidence
- Find the execution boundary
- Projection and tracking
- N+1 and related data
- Single or split query
- Paging
- Predicates that can use an index
- Contains with a list
- Joins
- Streaming
- Compiled queries
- See the SQL

## Review on evidence

A review of source code only cannot prove a slow query. Separate the two kinds of findings:

- **Defect.** The code shows it: a query inside a loop, a filter after `ToListAsync`, `Skip` without `OrderBy`, two operations at the same time on one context.
- **Possible optimization.** It needs a plan, a log, or data sizes: a missing index, a split query, a compiled query.

Report a possible optimization as "check with the plan". Do not write that an index is missing when you saw only one configuration file: the migrations or the database can have it. When all patterns are correct, say so. Do not invent a finding.

A loop that changes loaded entities and then calls `SaveChangesAsync` one time is not "SaveChanges in a loop". Suggest `ExecuteUpdateAsync` only when the row count is large and the work has no per-entity logic.

## Find the execution boundary

EF sends SQL when the code enumerates the query: `ToListAsync`, `FirstAsync`, `SingleAsync`, `CountAsync`, `AnyAsync`, `await foreach`. Each operator after `AsEnumerable()`, `ToList()`, or a method that returns a list runs in memory.

- Follow helper methods and repositories to the real code. A method that returns `List<T>` loaded all rows. A method that returns `IEnumerable<T>` can run the query again each time the code enumerates it.
- Keep `Where`, `Select`, `OrderBy`, and aggregates on `IQueryable<T>` until the end.
- Use `AnyAsync` to test for rows. Keep `CountAsync` only when the code needs the number.

```csharp
// Loads all rows, then filters in memory.
var all = await db.Orders.ToListAsync(ct);
var oldInMemory = all.Where(o => o.CreatedAt < cutoff).ToList();

// Filters in SQL.
var oldInSql = await db.Orders.Where(o => o.CreatedAt < cutoff).ToListAsync(ct);
```

## Projection and tracking

Project to a DTO for each read endpoint. EF reads only the columns of the projection.

```csharp
var items = await query
    .OrderBy(o => o.CreatedAt).ThenBy(o => o.Id)
    .Take(pageSize)
    .Select(o => new OrderListItem(
        o.Id,
        o.CreatedAt,
        o.Reference,
        o.Customer.Name,
        o.Lines.Count,
        o.Lines.Sum(l => l.Quantity * l.UnitPrice)))
    .ToListAsync(ct);
```

Select the tracking mode from the result:

| Result | Tracking |
|--------|----------|
| DTO or scalar values, no entities | Nothing to track. Do not add `AsNoTracking`. |
| Entities that the code changes and saves | Keep tracking. |
| Entities for read only | `AsNoTracking()`. Make the projection smaller first, when the full entity is not necessary. |
| Read-only graph where the same entity occurs many times, and the code compares references | `AsNoTrackingWithIdentityResolution()`. |

`AsNoTracking` gives a new object for each row, also for the same key. A filtered `Include` on a tracking context can show children that a previous query loaded, because EF connects tracked entities. Use a projection or a no-tracking query when the filtered subset matters.

## N+1 and related data

A navigation read inside a loop sends one query for each row. Load the data in one query:

```csharp
var summaries = await db.Orders
    .Select(o => new OrderSummary(o.Id, o.Lines.Count, o.Lines.Sum(l => l.UnitPrice)))
    .ToListAsync(ct);
```

Do not turn on lazy loading (`UseLazyLoadingProxies`, the `Microsoft.EntityFrameworkCore.Proxies` package) in a server app. It hides the N+1 queries and does synchronous I/O. Pass: a fixed number of queries, whatever the row count.

## Single or split query

Two or more sibling collection `Include`s multiply the rows (a cartesian product). A chain (`Include(a => a.B).ThenInclude(b => b.C)`) and reference navigations do not multiply in the same way.

```csharp
db.Orders
    .AsNoTracking()
    .Include(o => o.Lines)
    .Include(o => o.Notes)
    .AsSplitQuery()
    .SingleOrDefaultAsync(o => o.Id == id, ct);
```

- First try a projection that reads only what the caller needs.
- `AsSplitQuery()` sends one statement for each collection. Data can change between the statements. When the result must be consistent, run the query in a snapshot or serializable transaction. EF 11 throws `DbQueryConcurrencyException` when it cannot match the split results.
- A split query with `Skip` and `Take` needs a unique order.
- Do not set `UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` for all queries. Decide per query.
- In tests, set `RelationalEventId.MultipleCollectionIncludeWarning` to throw. Then each query with sibling collections must choose `AsSplitQuery()` or `AsSingleQuery()`.

## Paging

- Each paged query has a unique, stable order. Add the key as the last `ThenBy`. A second `OrderBy` replaces the first order: use `ThenBy`.
- Offset paging (`Skip`, `Take`) is fine for the first pages and for "go to page N". It gets slower on deep pages, and rows move when other users insert or delete.
- Keyset paging is constant in speed. It cannot jump to page N. For a descending `(CreatedAt, Id)` cursor, the filter is `CreatedAt < lastDate || (CreatedAt == lastDate && Id < lastId)`. Two independent bounds are wrong.

```csharp
if (afterCreatedAt is { } createdAt && afterId is { } id)
{
    query = query.Where(o => o.CreatedAt > createdAt || (o.CreatedAt == createdAt && o.Id.CompareTo(id) > 0));
}
```

A matching index starts with the filter columns and ends with the order columns. Index design is in `dev-stack:sqlserver` and `dev-stack:postgresql`.

## Predicates that can use an index

A function on a column usually stops an index seek. Compare the bare column:

```csharp
// The database calculates the year for each row.
db.Orders.Where(o => o.CreatedAt.Year == year);

// A range that an index on CreatedAt can serve.
var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
db.Orders.Where(o => o.CreatedAt >= start && o.CreatedAt < start.AddYears(1));
```

- Case-insensitive text: use a column collation or a normalized column with an index, not `ToLower()`. EF does not translate `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`, and it throws.
- `Contains(term)` on a string is a `LIKE '%term%'`. A normal B-tree index cannot serve it. A prefix search (`StartsWith`) can. Full text search is a different feature with other results. Do not change the search mode without telling the human.
- Arithmetic on a column (`o.Total * 1.21m > x`): move the arithmetic to the constant.

## Contains with a list

EF 10 sends `ids.Contains(o.Id)` as one parameter for each element (`IN (@ids1, @ids2, ...)`). It adds extra parameters to round up the count, so that fewer SQL variants exist. SQL Server allows at most 2100 parameters in one command.

| Need | Code |
|------|------|
| Short lists (default) | `ids.Contains(o.Id)` |
| Long lists, one query | `EF.Parameter(ids).Contains(o.Id)`: one JSON array parameter (SQL Server `OPENJSON`, PostgreSQL `= ANY`) |
| Values that the plan must see | `EF.Constant(ids).Contains(o.Id)`. This makes a new plan for each list. |
| Change the default for the context | `UseSqlServer(cs, o => o.UseParameterizedCollectionMode(ParameterTranslationMode.Parameter))` |

The same choice exists for one value: `EF.Parameter(value)` and `EF.Constant(value)`. Keep the defaults until a plan or a measurement shows a problem.

## Joins

.NET 10 has `LeftJoin` and `RightJoin`. EF 10 translates them to `LEFT JOIN` and `RIGHT JOIN`.

```csharp
db.Orders
    .LeftJoin(db.Customers, o => o.CustomerId, c => c.Id,
        (o, c) => new { o.Reference, CustomerName = c == null ? null : c.Name })
```

Use a navigation property when one exists. It is shorter than a join.

## Streaming

`AsAsyncEnumerable()` streams rows without a list in memory. Use it for exports and large batch reads. Do not call `ToAsyncEnumerable()` on an EF query (EF 11 analyzer EF1004). On .NET 10, the base library has async LINQ, so the `System.Linq.Async` package is not necessary.

A long stream keeps the connection open. Do not run a second query on the same context while the stream is open.

## Compiled queries

EF caches the translation of each query shape. `EF.CompileAsyncQuery` also skips the cache lookup. It does not make the database work faster. Use it only for a hot query with a fixed shape that runs many times each second.

```csharp
private static readonly Func<AppDbContext, string, CancellationToken, Task<Guid>> IdByReference =
    EF.CompileAsyncQuery((AppDbContext ctx, string reference, CancellationToken ct) =>
        ctx.Orders.Where(o => o.Reference == reference).Select(o => o.Id).FirstOrDefault());
```

The delegate is `static`. It takes the context and each parameter as arguments. Precompiled queries for NativeAOT are still experimental.

## See the SQL

- `query.ToQueryString()` shows the SQL and the parameters of one query.
- `.TagWith("orders: list page")` puts a comment in the SQL, so you can find the query in the log or in the query store.
- Log the commands: set `"Microsoft.EntityFrameworkCore.Database.Command": "Information"` in `appsettings.Development.json`.
- Count the statements and the rows before and after a change. Compare plans when the human can give them. A reasoned change is not a measured speed-up: say which one you have.
