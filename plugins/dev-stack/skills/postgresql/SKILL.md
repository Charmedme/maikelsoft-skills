---
name: postgresql
description: PostgreSQL engine rules and SQL text for .NET services. Use when code or a review touches SQL for PostgreSQL (a .sql script, FromSql, SqlQuery, ExecuteSql, migrationBuilder.Sql, Dapper, or plain Npgsql), column types, keys, indexes, EXPLAIN, isolation levels, row locks, a queue table with SKIP LOCKED, an upsert, DDL on a live table, NpgsqlDataSource, COPY, or a report per local day. Not for EF Core mapping, LINQ, or generated EF migrations (dev-stack:efcore), SQL Server or T-SQL (dev-stack:sqlserver), or server operations (backup, replication, postgresql.conf tuning).
metadata:
  verified: "2026-10-25"
  versions: "PostgreSQL 18.6 (19 is RC 1), Npgsql 10.0.3, Dapper 2.1.89, EFCore.PG 10.0.3, .NET 10"
---

# PostgreSQL

Engine rules and SQL text for PostgreSQL. EF Core mapping, LINQ, and generated migrations are in `dev-stack:efcore`. Its `references/provider-postgresql.md` has the EF options: `UseNpgsql`, `xmin`, enums, and naming. Test containers are in `dev-stack:dotnet-testing`.

Write all output except code in the rules of `references/language.md`.

## First

1. Read `references/stack-rules.md`.
2. Find the server version: the image tag in the compose file or the Testcontainers builder (`postgres:18.6`), `SetPostgresVersion(...)`, or `SELECT version();`.
   - PostgreSQL 18: use this file.
   - PostgreSQL 17 or older: the rules apply, except `uuidv7()` and `LIKE` on a nondeterministic collation. Both came in 18.
   - PostgreSQL 19 (RC 1 on 2026-10-15): keep 18 in production until 19.1 and EFCore.PG support it (DATA-01).
3. Find the access path and the name style:
   - EF Core (`UseNpgsql`), Dapper, or plain Npgsql.
   - snake_case names (`UseSnakeCaseNamingConvention`, or a hand-written schema), or PascalCase names in quotes (the EF default without the convention).

Done when you can name the server version, the access path, and the name style.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Engine and tag | PostgreSQL 18. Pin the image tag (`postgres:18.6`) in compose and in tests. | DATA-01, DATA-20 |
| Names | snake_case, no quotes. | DATA-09 |
| Keys | `uuid` from the EF provider (UUIDv7). Writers without EF: `DEFAULT uuidv7()`. Small lookup tables: `bigint GENERATED ALWAYS AS IDENTITY`. | DATA-07 |
| Instants | `timestamptz`, written as UTC. | DATA-10 |
| Money | `numeric(19,4)`. Currency as `varchar(3)` with a `CHECK`. | DATA-11 |
| Concurrency | The `xmin` token on edited aggregates. | DATA-15 |
| Bulk insert | Binary `COPY` with `BeginBinaryImportAsync`. | DATA-22 |
| Pooling | One `NpgsqlDataSource` per database. The Npgsql pool only. | DATA-23 |
| Case-insensitive text | A nondeterministic ICU collation on the column. | DATA-24 |
| DDL on live tables | `lock_timeout`, concurrent index builds, `NOT VALID` then `VALIDATE`. | DATA-25 |

## Existing code

In PostgreSQL code, the defects of stack rule 3 include these. Each one gives a wrong result without an error, and tests with midday times or one worker pass.

| Defect | Rule |
|--------|------|
| `::date` or `date_trunc` on a `timestamptz` without `AT TIME ZONE`, in a report per local day, week, or month. With the session zone UTC, it counts UTC days. | 7 |
| `FOR UPDATE SKIP LOCKED` in autocommit, then a second statement. | 11 |
| A read, then a write in a second statement, or `MERGE` as an upsert. | 10, 12 |
| `NOT IN (SELECT ...)` on a nullable column. | 4 |
| A local `DateTime` sent to `timestamptz` through Dapper or Npgsql. | 6 |
| On a live table: an index without `CONCURRENTLY`, a column with a volatile default, a constraint without `NOT VALID`, or no `lock_timeout`. | 16 |
| `BeginBinaryImportAsync` without `CompleteAsync`. | 19 |

New code "like the existing report" depends on that report: fix both, and name the fix under "Changes".

## Rules

### Connections

1. Register one `NpgsqlDataSource` per database: `AddNpgsqlDataSource` (package `Npgsql.DependencyInjection`), or `NpgsqlDataSourceBuilder` and `UseNpgsql(dataSource)` when EF and Dapper share it. Open one connection per operation with `await using`. The data source holds the pool, the type mappings, and the settings.

```csharp
builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("Orders")!);

// In a report class that gets NpgsqlDataSource from DI:
await using var connection = await dataSource.OpenConnectionAsync(ct);
```

### Names and SQL text

2. Write each name in SQL as the schema has it. PostgreSQL folds a name without quotes to lowercase. With EF PascalCase names, quote each name (`o."PlacedAt"`). `WHERE Status = 'Placed'` fails with 42703.
3. Send each value as a parameter: `{value}` in `FromSql`, `SqlQuery`, and `ExecuteSqlAsync`, and `@name` in Dapper. Check an identifier against an allow list (`dev-stack:efcore` rule 23).
4. Write `NOT EXISTS (SELECT 1 ...)`, not `NOT IN (SELECT ...)`. One NULL in the subquery makes `NOT IN` return no rows.
5. Raw SQL through EF on an entity with an `xmin` token: select the columns and the token (`SELECT o.*, o.xmin`). `SELECT *` does not return system columns, so EF fails (error 42703). EF cannot compose some SQL: `UPDATE ... RETURNING` and `FOR UPDATE`. Read it with `ToListAsync()` or `AsAsyncEnumerable()`. `SingleAsync()` and `FirstOrDefaultAsync()` put the SQL in a subquery, and PostgreSQL rejects it.

### Types and time

6. Store instants in `timestamptz` and send UTC: a `DateTimeOffset` with offset 0, or a `DateTime` with `Kind = Utc`. Npgsql sends a local `DateTime` as `timestamp`. PostgreSQL then converts it with the session `TimeZone`, without an error. With `America/New_York`, 12:00 local became 16:00Z. EF throws for this value. Dapper and plain Npgsql do not.
7. Compute a local day with `(ts AT TIME ZONE 'Europe/Amsterdam')::date`, and a local week or month with `date_trunc('week', ts AT TIME ZONE 'Europe/Amsterdam')`. `ts::date` and `date_trunc` on a `timestamptz` use the session zone: 2026-07-01 22:30Z is 2026-07-02 in Amsterdam. Filter on the bare column with the UTC instants of local midnight, so that an index can serve the filter.
8. Keys: `bigint GENERATED ALWAYS AS IDENTITY` or `uuid`. Do not use `serial`. On PostgreSQL 18, `DEFAULT uuidv7()` gives ordered keys to writers without EF. It is a built-in function, so it needs no extension.
9. Money: `numeric(19,4)`. Not `money` (it depends on `lc_monetary`) and not `float8`. Text: `text`, or `varchar(n)` when the limit is a rule. Not `char(n)`, because it pads with spaces.

```sql
-- Revenue per day in Amsterdam time. @from and @to_exclusive are dates.
SELECT (o.placed_at AT TIME ZONE 'Europe/Amsterdam')::date AS day,
       count(DISTINCT o.id) AS orders,
       sum(l.quantity * l.unit_price) AS revenue
FROM orders AS o
JOIN order_lines AS l ON l.order_id = o.id
WHERE o.placed_at >= (@from::timestamp AT TIME ZONE 'Europe/Amsterdam')
  AND o.placed_at <  (@to_exclusive::timestamp AT TIME ZONE 'Europe/Amsterdam')
GROUP BY 1
ORDER BY 1;
```

### Concurrency

10. Keep `READ COMMITTED`, the default. Make a change safe with one conditional statement (`UPDATE ... WHERE status = 'Placed'`, then check the row count) or with a concurrency token. When you need `REPEATABLE READ` or `SERIALIZABLE`, retry the whole transaction on 40001 and 40P01. Npgsql sets `PostgresException.IsTransient` for both, so the EF execution strategy retries them.
11. Claim work (a queue, "the next order") with `FOR UPDATE SKIP LOCKED`, in one statement or in one transaction. Without a transaction, the lock ends with the `SELECT`, and two workers get the same row. `FOR UPDATE` without `SKIP LOCKED` makes the workers wait for each other.

```csharp
var ids = await db.Database.SqlQuery<Guid>($"""
    UPDATE orders
    SET "Status" = 'Picking', "PickedBy" = {workerId}, "PickingStartedAt" = {now}
    WHERE "Id" = (
        SELECT "Id" FROM orders
        WHERE "Status" = 'Placed'
        ORDER BY "PlacedAt", "Id"
        LIMIT 1
        FOR UPDATE SKIP LOCKED)
    RETURNING "Id" AS "Value"
    """).ToListAsync(ct); // not SingleOrDefaultAsync: EF cannot compose UPDATE ... RETURNING
```

12. Enforce uniqueness with a unique index, and map 23505 to 409. Write an upsert as `INSERT ... ON CONFLICT (key) DO UPDATE SET ...`. It gives one atomic result when two calls run at the same time.

Detail on MVCC, isolation, queues, advisory locks, and deadlocks: `references/concurrency.md`.

### Indexes and plans

13. Design an index from the query: equality columns first, then the range or sort column. Use a partial index for a hot subset (`WHERE status = 'Placed'`). Use GIN for `jsonb`, arrays, `tsvector`, and `pg_trgm`. Each index costs time on each write, so add an index for a query, not for each column.
14. Index each foreign key column that a join or a cascade delete uses. PostgreSQL does not make that index. EF migrations add it. A hand-written schema must add it.
15. Prove a plan with `EXPLAIN (ANALYZE, BUFFERS)` on data like production. A plan on a small test table proves nothing.

Detail and examples: `references/indexes.md`.

### DDL on live tables

16. In each migration that changes a table with live writes (DATA-25):
    - Start with `SET lock_timeout = '5s'`. A DDL statement waits for its lock, and all later queries on the table wait behind it.
    - Build an index with `CREATE INDEX CONCURRENTLY`, outside a transaction. After a failed build, drop the INVALID index and build it again. EF: `IsCreatedConcurrently()`. One idempotent script: `dev-stack:efcore` rule 32a.
    - Add a column with a volatile default (`gen_random_uuid()`, `clock_timestamp()`) in steps: add it without a default, fill it in batches, then set the default. The one-step form writes the table again under an `ACCESS EXCLUSIVE` lock. A constant default does not.
    - Add a foreign key or a check as `NOT VALID`, then run `VALIDATE CONSTRAINT` in a later statement. Without `NOT VALID`, PostgreSQL scans the table while it holds the `ACCESS EXCLUSIVE` lock. `lock_timeout` limits only the wait for the lock, not the scan.
    - Rename or remove a column in expand and contract steps (OPS-15).

Detail, the order of steps, and the lint rules: `references/ddl-live-tables.md`.

### Dapper and COPY

17. Dapper on Npgsql: write `= ANY(@ids)` with an array or a list. Dapper sends a list as one array parameter on Npgsql, so `IN @ids` fails with 42601.
18. Set `DefaultTypeMap.MatchNamesWithUnderscores = true` one time at startup when the schema has snake_case names and the C# types have PascalCase properties.
19. For a large insert, use binary `COPY` with `BeginBinaryImportAsync`, and call `CompleteAsync()` before the dispose. Without it, Npgsql cancels the `COPY` at the dispose. In autocommit mode, PostgreSQL keeps no rows and gives no error. In a transaction the next command fails with 25P02.

```csharp
await using (var writer = await connection.BeginBinaryImportAsync(
    "COPY stock_levels (sku, available) FROM STDIN (FORMAT BINARY)", ct))
{
    foreach (var row in rows)
    {
        await writer.StartRowAsync(ct);
        await writer.WriteAsync(row.Sku, NpgsqlDbType.Varchar, ct);
        await writer.WriteAsync(row.Available, NpgsqlDbType.Integer, ct);
    }

    await writer.CompleteAsync(ct); // without this call, PostgreSQL keeps no rows
}
```

Detail on the data source, Dapper mapping, multiple results, and COPY: `references/npgsql-dapper.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `new NpgsqlConnection(connectionString)` in each method | `await using var c = await dataSource.OpenConnectionAsync(ct);` |
| `"... WHERE id IN @ids"` (Dapper on Npgsql) | `"... WHERE id = ANY(@ids)"` |
| `WHERE Status = 'Placed'` on an EF table with PascalCase names | `WHERE "Status" = 'Placed'` |
| `new { importedAt = DateTime.Now }` | `new { importedAt = time.GetUtcNow() }` |
| `GROUP BY placed_at::date` for a local day | `GROUP BY (placed_at AT TIME ZONE 'Europe/Amsterdam')::date` |
| `date_trunc('week', placed_at)` for a local week | `date_trunc('week', placed_at AT TIME ZONE 'Europe/Amsterdam')` |
| `placed_at timestamp NOT NULL` | `placed_at timestamptz NOT NULL` |
| `id serial PRIMARY KEY` | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `amount money` or `amount float8` | `amount numeric(19,4)` |
| `WHERE sku NOT IN (SELECT sku FROM discontinued)` | `WHERE NOT EXISTS (SELECT 1 FROM discontinued d WHERE d.sku = s.sku)` |
| `SELECT ... FOR UPDATE SKIP LOCKED` in autocommit, then a second `UPDATE` | One `UPDATE ... WHERE id = (SELECT ... FOR UPDATE SKIP LOCKED) RETURNING`, or both in one transaction |
| `FromSql($"SELECT * FROM orders ...")` on an entity with `xmin` | `FromSql($"SELECT o.*, o.xmin FROM orders AS o ...")` |
| `SqlQuery<Guid>($"UPDATE ... RETURNING ...").SingleOrDefaultAsync()` | `(await ...ToListAsync(ct)).SingleOrDefault()` |
| `BeginBinaryImportAsync(...)` without `CompleteAsync` | `await writer.CompleteAsync(ct);` before the dispose |
| `CREATE INDEX ix ON order_lines (sku);` on a live table | `CREATE INDEX CONCURRENTLY ix ON order_lines (sku);` outside a transaction |
| `ADD COLUMN batch_id uuid NOT NULL DEFAULT gen_random_uuid()` on a live table | Add it without a default, fill it in batches, then set the default |
| `lower(email) = lower(@email)` | A nondeterministic ICU collation on the column (DATA-24) |

## Check

1. Build with the analyzers as errors:

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings.

2. Run the tests against the pinned production image (`postgres:18.6`, DATA-20). Pass: all tests green. A test of a claim query runs parallel calls, because one call cannot show a double claim.
3. Lint each migration SQL file, when the team approved the tool (stack rule 5). For EF migrations, make the SQL first with `dotnet ef migrations script <from> <to> -o migration.sql`.

   ```
   npx squawk-cli@2.70.0 migration.sql
   ```

   Pass: no finding of the lock rules (`require-concurrent-index-creation`, `adding-field-with-default`, `constraint-missing-not-valid`, `require-lock-timeout`, `prefer-timestamp-tz`, `prefer-identity`). Squawk exits with 1 on any warning. Style rules such as `prefer-text-field` and `prefer-robust-stmts` are team choices: turn them off in `.squawk.toml`, or give the reason in the report.
4. Check a new hot query: `EXPLAIN (ANALYZE, BUFFERS) <query>` on data like production. Pass: an index scan or a bitmap scan on the hot path, no sequential scan of a big table.
5. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

## Review checklist

The full list is in `references/review-checklist.md`.

1. Dapper on Npgsql uses `= ANY(@ids)`, not `IN @ids`. tool (PG-DAPPER-IN-LIST)
2. Each binary `COPY` calls `CompleteAsync`. judge
3. Instants are `timestamptz`. Parameters are UTC values, not a local `DateTime`. tool (PG-TIMESTAMP-NO-TZ), judge
4. A local day, week, or month uses `AT TIME ZONE` with an IANA zone. Check the existing report that the change copies too. The `WHERE` clause compares the bare column. tool (PG-LOCAL-DATE-NO-ZONE), judge
5. A claim query uses `SKIP LOCKED` in one statement or one transaction. judge
6. No `NOT IN (SELECT ...)`. tool (PG-NOT-IN-SUBQUERY)
7. Raw SQL quotes PascalCase names. `FromSql` on an entity with `xmin` selects `xmin`. EF does not compose `UPDATE ... RETURNING`. tool (PG-SELECT-STAR-FROMSQL), judge
8. A migration on a live table sets `lock_timeout`, builds indexes concurrently, and uses `NOT VALID`. tool (squawk), judge
9. Keys are identity or `uuid`, not `serial`. tool (PG-SERIAL)
10. One `NpgsqlDataSource`. No new `NpgsqlConnection` in each call. tool (PG-NEW-CONNECTION)
11. Uniqueness comes from a unique index. An upsert uses `ON CONFLICT`. Code maps 23505. judge
12. A `REPEATABLE READ` or `SERIALIZABLE` transaction retries on 40001 and 40P01. judge
13. Each index serves a query. A hand-written schema indexes its foreign keys. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/decisions.md`: read before new code needs DATA-23, DATA-24, DATA-25, or the PostgreSQL part of DATA-07, DATA-10, DATA-11, DATA-15, or DATA-22.
- `references/concurrency.md`: read when you write a queue, an upsert, a counter, or a transaction with an isolation level. Also read it for 40001, 40P01, or a lock wait.
- `references/indexes.md`: read when you add or review an index, or a query is slow.
- `references/ddl-live-tables.md`: read before you write or review a migration that changes a table with data.
- `references/npgsql-dapper.md`: read when you write Dapper or plain Npgsql code, a data source, or a bulk import.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources, the evidence, and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
