---
name: sqlserver
description: SQL Server and Azure SQL engine rules and T-SQL for .NET services. Use when code or a review touches T-SQL (a .sql script, a stored procedure, FromSql, SqlQuery, ExecuteSql, migrationBuilder.Sql, Dapper, or SqlClient code), column types, keys, indexes, isolation levels, RCSI, optimized locking, lock hints such as NOLOCK or UPDLOCK, deadlocks, MERGE or another upsert, a queue table, AT TIME ZONE, SqlBulkCopy, or SQL Server 2025 features. Not for EF Core mapping, LINQ, or generated EF migrations (dev-stack:efcore), PostgreSQL (dev-stack:postgresql), or server administration (backup, Always On, instance settings).
metadata:
  verified: "2026-10-27"
  versions: "SQL Server 2025 (17.x, CU10), compatibility level 170, Azure SQL Database, Microsoft.Data.SqlClient 6.1.7 and 7.1.1, Dapper 2.1.89, EF Core 10.0.14, .NET 10.0.14"
---

# SQL Server

Engine rules and T-SQL for SQL Server and Azure SQL. EF Core mapping, LINQ, and generated migrations are in `dev-stack:efcore`. Its `references/provider-sqlserver.md` has the EF options: `UseSqlServer`, `UseCompatibilityLevel`, `rowversion`, and collations in the model. Test containers are in `dev-stack:dotnet-testing`.

Write all output except code in the rules of `references/language.md`.

## First

1. Read `references/stack-rules.md`.
2. Find the engine and the compatibility level: the image tag (`mcr.microsoft.com/mssql/server:2025-CU10-ubuntu-24.04`), `UseCompatibilityLevel(...)` or `UseAzureSql`, or `SELECT @@VERSION, compatibility_level FROM sys.databases WHERE name = DB_NAME();`.
   - SQL Server 2025 or Azure SQL at level 170: use this file.
   - Level 160 or lower: no `REGEXP_LIKE` and no regex table functions (rule 17).
   - SQL Server 2022 or older: no optimized locking, no `json` type, no regex functions.
3. Find the database settings. Many rules depend on them:

   ```sql
   SELECT is_read_committed_snapshot_on, is_accelerated_database_recovery_on, is_optimized_locking_on
   FROM sys.databases WHERE name = DB_NAME();
   ```

   A new SQL Server database has all three off. Azure SQL Database has all three on.
4. Find the SqlClient version: 6.1 (with EF Core 10) or 7.1. Both pull a vulnerable `System.Security.Cryptography.Pkcs`. Pin Pkcs 10.0.x (standing rule, stack rule 5). SqlClient 6.1.6 and later also pull `Microsoft.Identity.Client.Broker`. For an app without Entra ID sign-in, Microsoft advises a direct reference to SqlClient `7.1.x` (also with EF Core 10). With Entra ID on SqlClient 7, also add `Microsoft.Data.SqlClient.Extensions.Azure`.
5. Read the existing data code that the change edits, calls, or copies: raw SQL, reports, upserts, and key generators. These defects give wrong results without an error, and the existing tests pass:
   - `AT TIME ZONE '<zone>'` on a `datetime2` column without `AT TIME ZONE 'UTC'` first: the report counts UTC days (rule 12).
   - An interpolated string or `AddWithValue` on a `varchar` column: `nvarchar(4000)`, `CONVERT_IMPLICIT`, and an index scan (rule 10).
   - `Guid.CreateVersion7()` or `NEWID()` for a clustered key: random order in SQL Server, also when a comment or a decision says otherwise (rule 15).
   - `MERGE` without `HOLDLOCK`, or a check before an insert (rule 6).

   Fix the defect when the change edits, calls, or copies that code, and list the fix under Changes. Otherwise report it as an open point. Stack rule 3 keeps the style of existing code, not its defects.

   After you fix a defect, search the repository for the same defect. Use `grep` for the table name and the call. Fix each other site on a table that the change uses when the fix changes no result and needs no migration. A typed parameter is an example. List each site under Changes. Report a site that needs a decision, a migration, or a change of behavior as an open point.

Done when you can name the engine, the compatibility level, the RCSI and optimized locking settings, and the SqlClient version. Also name the defects in the existing code that the change touches.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Engine | The engine that the product runs. SQL Server 2025 or Azure SQL when the contract asks for it. | DATA-01 |
| Isolation | RCSI on for each OLTP database and for the test database. `READ COMMITTED`. No `NOLOCK`. | DATA-26 |
| Optimized locking | On for a new SQL Server 2025 database, with ADR and RCSI. | DATA-27 |
| Keys | `uniqueidentifier` from EF (sequential). Writers without EF: `DEFAULT NEWSEQUENTIALID()`, or `bigint IDENTITY`. | DATA-07 |
| Strings | `nvarchar(n)` for user text, `varchar(n)` for codes, the server default collation. | DATA-28 |
| Instants | `datetimeoffset`, written as UTC. | DATA-10 |
| Money | `decimal(19,4)`. Currency as `char(3)`. | DATA-11 |
| Concurrency | `rowversion` on edited aggregates. | DATA-15 |
| Bulk insert | `SqlBulkCopy`. | DATA-22 |
| Procedures | No new procedures in a service. | DATA-29 |

Existing code keeps its style (stack rule 3).

## Rules

### Isolation and locks

1. Turn on RCSI for each OLTP database (DATA-26). Readers then read the last committed version, and readers and writers do not block each other. RCSI is off in a new SQL Server database and on in Azure SQL Database. Turn it on in the test database too, or the tests prove other behavior. In an existing database, first find code that reads a row and then changes it based on that read. Such code needs `UPDLOCK` with RCSI.

```sql
-- Not in a transaction (error 226). In an EF migration: migrationBuilder.Sql(sql, suppressTransaction: true).
ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
```

2. Do not use `NOLOCK`, `READUNCOMMITTED`, or `READ UNCOMMITTED`. They read uncommitted rows, and a scan can read a row two times or miss it while pages split. RCSI removes the blocking without these errors.
3. Optimized locking (SQL Server 2025, DATA-27) needs ADR. Its lock after qualification (LAQ) works only with RCSI and `READ COMMITTED`. LAQ checks the `WHERE` clause on the last committed version. So an `UPDATE` can skip a row that a running transaction changes. Code that needs a strict order of transactions uses `REPEATABLE READ` or `SERIALIZABLE`. Lock hints (`UPDLOCK`, `HOLDLOCK`, `XLOCK`) turn LAQ off for their table, so use them only where the logic needs them.
4. Retry a deadlock (1205) by running the whole transaction again. `EnableRetryOnFailure` does it in EF. Outside EF, retry the whole unit of work 3 times with a short random delay. Repeated deadlocks are a design defect. Read the deadlock graph in the `system_health` session. Add the missing index, and change tables and rows in the same order.

Detail on isolation levels, RCSI, LAQ, deadlocks, and lock waits: `references/isolation-and-locking.md`.

### Writes under concurrency

5. Change a counter or a stock level in one statement, then check the row count. A read, a check in C#, and a write lose updates under load.

```csharp
var reserved = await db.StockLevels
    .Where(s => s.Sku == sku && s.Available >= quantity)
    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Available, x => x.Available - quantity), ct);
if (reserved == 0)
{
    // not enough stock: roll back the transaction and return 409
}
```

6. An upsert under concurrency needs a key-range lock. Use `MERGE ... WITH (HOLDLOCK)`. Or use `UPDATE ... WITH (UPDLOCK, HOLDLOCK)`, then `INSERT` when no row changed, in one transaction. Without the lock, two sessions insert the same new key, and one fails (error 2627). A check in C# before the insert has the same problem.

```csharp
var skuParameter = new SqlParameter("@sku", SqlDbType.VarChar, 32) { Value = sku };
await db.Database.ExecuteSqlAsync($"""
    MERGE dbo.StockLevels WITH (HOLDLOCK) AS t
    USING (VALUES ({skuParameter}, {available})) AS s (Sku, Available)
        ON t.Sku = s.Sku
    WHEN MATCHED THEN UPDATE SET Available = s.Available
    WHEN NOT MATCHED THEN INSERT (Sku, Available) VALUES (s.Sku, s.Available);
    """, ct);
```

7. Take a work item with one statement: `UPDATE TOP (1) ... WITH (UPDLOCK, READPAST, ROWLOCK) ... OUTPUT inserted.Id`. The statement locks the row, and other workers skip it.
8. Put a unique index on each unique column. Map 2601 and 2627 (unique key) to 409. Map 547 (foreign key or check) to 409 or 422.
9. In a procedure that opens a transaction, write `SET XACT_ABORT ON; SET NOCOUNT ON;` first. Without `XACT_ABORT`, a client timeout stops the batch, but the transaction stays open with its locks. It stays open until the pool reuses or closes the connection.

### Parameters and dynamic SQL

10. Type each parameter like its column, so that the index can seek:
    - A .NET string goes as `nvarchar`. EF raw SQL sends an interpolated string as `nvarchar(4000)`. LINQ sends `varchar(32)` from the mapping.
    - On a `varchar` column with a `SQL_` collation, SQL Server then converts the column (`CONVERT_IMPLICIT`) and scans the index. A `SQL_` collation is the default of a US install.
    - Use LINQ, a `SqlParameter` with `SqlDbType.VarChar` and a size, or Dapper `DbString { IsAnsi = true, Length = 32 }`. Do not use `AddWithValue`.
11. Never build SQL by concatenation. Dynamic SQL uses `sp_executesql` with parameters. An identifier (a sort column) comes from an allow list and goes through `QUOTENAME`.

### Types and time

12. `AT TIME ZONE` takes Windows zone names from `sys.time_zone_info` (`'W. Europe Standard Time'`), not IANA names. `'Europe/Amsterdam'` fails with error 9820. Convert a UTC `datetimeoffset`, then cast to `date` for a local day. SQL Server reads a `datetime2` without an offset as a time in the named zone. So convert it from UTC first: `x AT TIME ZONE 'UTC' AT TIME ZONE 'W. Europe Standard Time'`.

```sql
-- Revenue per day in Amsterdam time. @From and @ToExclusive are dates.
SELECT CAST(o.PlacedAt AT TIME ZONE 'W. Europe Standard Time' AS date) AS Day,
       COUNT(DISTINCT o.Id) AS Orders,
       SUM(l.Quantity * l.UnitPrice) AS Revenue
FROM dbo.Orders AS o
JOIN dbo.OrderLines AS l ON l.OrderId = o.Id
WHERE o.PlacedAt >= (CAST(@From AS datetime2) AT TIME ZONE 'W. Europe Standard Time')
  AND o.PlacedAt <  (CAST(@ToExclusive AS datetime2) AT TIME ZONE 'W. Europe Standard Time')
GROUP BY CAST(o.PlacedAt AT TIME ZONE 'W. Europe Standard Time' AS date)
ORDER BY Day;
```

13. Instants: `datetimeoffset` (or `datetime2` in UTC), not `datetime`. Amounts: `decimal(p,s)`, not `money` or `float`.
14. Strings (DATA-28): `nvarchar(n)` for text that users type, `varchar(n)` for codes (SKU, ISO codes). Do not filter or index an `nvarchar(max)` column: it cannot be an index key.
15. Clustered key: narrow, unique, static, and growing. A `uniqueidentifier` key needs sequential values (the EF generator or `NEWSEQUENTIALID()`), never `NEWID()`. `Guid.CreateVersion7()` is not ordered in SQL Server, because SQL Server sorts `uniqueidentifier` bytes in another order.

### Indexes and plans

16. Design an index from the query. Put the equality columns first, then the range or sort column. Add the columns that the query only reads with `INCLUDE`. Index each foreign key column that a join or a delete uses. Use a filtered index for a hot subset (`WHERE Status = 'Placed'`). Prove a plan with the actual execution plan on data like production. A seek is not always the goal. A query that reads much of a big table can need a covering, filtered, or columnstore index. Refer to "When a seek is not the answer" in `references/indexes.md`.

Detail and diagnostic queries: `references/indexes.md`.

### SQL Server 2025 features

17. Check the compatibility level before you use a new function. `REGEXP_LIKE` and the regex table functions need level 170. The other regex scalar functions (`REGEXP_REPLACE`, `REGEXP_SUBSTR`) work at each level. SQL Server cannot compare a `json` value with `=` or sort it (error 13636).

### SqlClient, Dapper, and procedures

18. New service code uses LINQ or parameterized SQL in code, not new stored procedures (DATA-29). When the project already has procedures that a DBA owns, call them with `CommandType.StoredProcedure` and typed parameters.
19. For a large insert, use `SqlBulkCopy` with `EnableStreaming`, a `BatchSize`, and explicit column mappings.

Detail on SqlClient, Dapper, procedures, and bulk copy: `references/sqlclient-dapper.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `SELECT ... FROM dbo.Orders WITH (NOLOCK)` | RCSI on the database, no hint |
| `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` | RCSI on the database, `READ COMMITTED` |
| `MERGE dbo.StockLevels AS t USING ...` (concurrent upsert) | `MERGE dbo.StockLevels WITH (HOLDLOCK) AS t USING ...` |
| `if (await db.StockLevels.FindAsync(sku) is null) db.Add(...)` for an upsert | `MERGE ... WITH (HOLDLOCK)`, or `UPDATE ... WITH (UPDLOCK, HOLDLOCK)` then `INSERT` |
| `var s = await Get(sku); if (s.Available >= q) { s.Available -= q; Save(); }` | One conditional `UPDATE` or `ExecuteUpdateAsync`, then check the row count |
| `cmd.Parameters.AddWithValue("@sku", sku)` on a `varchar` column | `cmd.Parameters.Add("@sku", SqlDbType.VarChar, 32).Value = sku;` |
| `SqlQuery<int>($"... WHERE Sku = {sku}")` on a `varchar` column | LINQ, or `{new SqlParameter("@sku", SqlDbType.VarChar, 32) { Value = sku }}` |
| `AT TIME ZONE 'Europe/Amsterdam'` | `AT TIME ZONE 'W. Europe Standard Time'` |
| `Id uniqueidentifier DEFAULT NEWID() PRIMARY KEY CLUSTERED` | `DEFAULT NEWSEQUENTIALID()`, or the EF generator |
| `BEGIN TRAN` in a procedure without `SET XACT_ABORT ON` | `SET XACT_ABORT ON; SET NOCOUNT ON;` at the start |
| `EXEC('SELECT ... WHERE Name = ''' + @name + '''')` | `EXEC sp_executesql N'... WHERE Name = @name', N'@name nvarchar(100)', @name = @name;` |
| `PlacedAt datetime NOT NULL` | `PlacedAt datetimeoffset(7) NOT NULL` |
| `Amount money` or `Amount float` | `Amount decimal(19,4)` |
| `ALTER DATABASE ... SET READ_COMMITTED_SNAPSHOT ON` in an EF migration with a transaction | `migrationBuilder.Sql(sql, suppressTransaction: true)`, or a DBA step |
| `BeginTransactionAsync()` with `EnableRetryOnFailure`, outside a strategy | `CreateExecutionStrategy().ExecuteAsync(...)` (`dev-stack:efcore` rule 20) |

## Check

1. Build with the analyzers as errors:

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings. With SqlClient 6.1.7 or 7.1.1, the build fails with NU1902 and NU1903 until the project pins `System.Security.Cryptography.Pkcs` 10.0.x.
2. Run the tests against `mcr.microsoft.com/mssql/server:2025-CU10-ubuntu-24.04`, with the production settings of the database (RCSI, compatibility level). Pass: all tests green. A test of a counter, a claim, or an upsert runs parallel calls.
3. Check the database settings in each environment:

   ```sql
   SELECT is_read_committed_snapshot_on, is_accelerated_database_recovery_on,
          is_optimized_locking_on, compatibility_level
   FROM sys.databases WHERE name = DB_NAME();
   ```

   Pass: the values of DATA-26, DATA-27, and DATA-01.
4. For a new hot query, read the actual execution plan (`SET STATISTICS XML ON`, or Query Store). Pass: a seek on the hot path, and no `CONVERT_IMPLICIT` on an indexed column.
5. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>` (in SQL: `-- stackcheck: ignore <RULE-ID> <reason>`).

## Review checklist

The full list is in `references/review-checklist.md`.

1. No `NOLOCK`, `READUNCOMMITTED`, or `READ UNCOMMITTED`. tool (SQL-NOLOCK)
2. A concurrent upsert uses `MERGE ... WITH (HOLDLOCK)` or `UPDLOCK, HOLDLOCK`. tool (SQL-MERGE-NO-HOLDLOCK), judge
3. A counter or a stock level changes in one conditional statement with a check of the row count. judge
4. `AT TIME ZONE` uses a Windows zone name. tool (SQL-IANA-ZONE)
5. No dynamic SQL by concatenation. `sp_executesql` with parameters, `QUOTENAME` for identifiers. tool (SQL-EXEC-CONCAT), judge
6. Parameters match the column type. No `AddWithValue`. tool (SQL-ADDWITHVALUE), judge
7. Procedures with a transaction start with `SET XACT_ABORT ON`. judge
8. Instants are `datetimeoffset` or UTC `datetime2`, not `datetime`. tool (SQL-DATETIME-TYPE)
9. Clustered `uniqueidentifier` keys are sequential, not `NEWID()`. judge
10. RCSI is on, or the report names DATA-26 as an open point. judge
11. Explicit transactions with retries on run inside the execution strategy. Deadlocks retry the whole unit. judge
12. Each index serves a query. Foreign keys have an index. No filter on `nvarchar(max)`. judge
13. New functions of SQL Server 2025 match the compatibility level. judge
14. Existing code that the change edits, calls, or copies has none of the defects of First, step 5. judge
15. Each other site of a fixed defect on a table of the change is fixed or reported. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

The change can need a database setting: RCSI, optimized locking, or a compatibility level. For each one, state how it is deployed (a migration with `suppressTransaction: true`, or a DBA step) and when it can run.

## References

- `references/decisions.md`: read before new code needs DATA-26, DATA-27, DATA-28, DATA-29, or the SQL Server part of DATA-07, DATA-10, DATA-11, DATA-15, or DATA-22.
- `references/isolation-and-locking.md`: read when you write a transaction, a queue, an upsert, or a counter. Also read it for blocking, a deadlock (1205), or a lock hint.
- `references/indexes.md`: read when you add or review an index, or a query is slow.
- `references/sqlclient-dapper.md`: read when you write SqlClient or Dapper code, call a procedure, write a procedure, or bulk copy rows.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources, the evidence, and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
