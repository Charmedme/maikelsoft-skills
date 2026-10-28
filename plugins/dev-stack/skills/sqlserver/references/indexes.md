# Indexes and plans on SQL Server

How to choose an index and prove that a query uses it. EF index APIs (`HasIndex`, `HasFilter`, `IncludeProperties`) are in `dev-stack:efcore` (`references/provider-sqlserver.md`).

## Contents

- Clustered key
- Nonclustered column order
- Filtered indexes
- Implicit conversion
- When a seek is not the answer
- Foreign keys
- Prove the plan
- Find missing and unused indexes
- Online index operations

## Clustered key

The clustered index is the table. Each nonclustered index carries the clustered key. So the clustered key is:

- narrow (`bigint`, or a 16-byte `uniqueidentifier`),
- unique,
- static (never updated),
- growing (`IDENTITY`, `NEWSEQUENTIALID()`, the EF sequential generator).

A random key (`NEWID()`, `Guid.NewGuid()`, `Guid.CreateVersion7()`) inserts into the middle of the index: page splits, fragmentation, and more log. SQL Server sorts a `uniqueidentifier` by its last 6 bytes first, so a UUIDv7 is not in order there.

## Nonclustered column order

1. Columns compared with `=` first.
2. Then one range or sort column.
3. `INCLUDE` for the columns that the query only reads, so that the plan needs no key lookup.

Example: `WHERE CustomerId = @c ORDER BY PlacedAt DESC` with `Total` in the select list needs `(CustomerId, PlacedAt) INCLUDE (Total)`.

Compare the bare column. `WHERE CAST(PlacedAt AS date) = @day` can still seek on `datetime2` in some cases, but `WHERE YEAR(PlacedAt) = @y` and `WHERE PlacedAt AT TIME ZONE '...'` cannot. Write a range on the column.

## Filtered indexes

```sql
CREATE INDEX IX_Orders_Placed ON dbo.Orders (PlacedAt, Id) WHERE Status = 'Placed';
```

- The query must have the same condition, with a literal, not a parameter. A parameterized query (`WHERE Status = @status`) does not use the filtered index unless the plan is compiled for the value (`OPTION (RECOMPILE)`).
- The session needs the standard `SET` options (`ANSI_NULLS`, `QUOTED_IDENTIFIER` on). SqlClient sets them.

## Implicit conversion

When a parameter type has a higher precedence than the column type, SQL Server converts the column. Then it cannot seek.

| Column | Parameter | Result (checked on SQL Server 2025 CU9) |
|--------|-----------|-----------------------------------------|
| `varchar(32)`, collation `SQL_Latin1_General_CP1_CI_AS` | `nvarchar(4000)` | Clustered index scan with `CONVERT_IMPLICIT` |
| `varchar(32)`, collation `Latin1_General_100_CI_AS` | `nvarchar(4000)` | Range seek (more work than a plain seek) |
| `varchar(32)` | `varchar(32)` | Seek |

The sources of `nvarchar`: `AddWithValue`, a Dapper `string` without `DbString`, and an interpolated string in EF `SqlQuery` or `FromSql`. LINQ uses the type of the mapping (`IsUnicode(false)` gives `varchar`). The plan shows the problem as a warning on the `SELECT` operator.

## When a seek is not the answer

A `WHERE` on an indexed column does not promise an index seek. Check the estimated and the actual rows in the plan before you add an index.

- Rowstore indexes fit a seek on one value or a small range. They fit transactional work.
- A nonclustered seek costs one key lookup for each row. When the query reads a large share of the table, the optimizer picks a scan. This is correct, not a defect.
- Remove the lookups with `INCLUDE` for each column that the query reads. The index then covers the query.
- Statistics keep a histogram of 200 steps or fewer, on the first key column only. A skewed column or a stale statistic gives a wrong row estimate and a wrong plan. Update the statistics, or add a filtered index for the hot subset.
- A filtered index on `WHERE Col IS NULL` is not used when `Col` is not a key or an included column of that index. Add `Col` with `INCLUDE`.

A report or an aggregate that scans a large range of a big table is an analytic query. For it, use a columnstore index:

- A columnstore index reads only the columns of the query. It skips row groups by their minimum and maximum values. It compresses about 10 times and uses batch mode.
- On a rowstore table, add one nonclustered columnstore index. Transactions keep the rowstore indexes, and reports use the columnstore. Plan extra space and extra cost for each write.
- Use it for stable data. Queries that update or delete more than 10% of the rows suit it less.
- Use few partitions. A partition with fewer than one million rows keeps its rows in the delta store, without the full gain.
- Keep a rowstore index for the lookup by key. A columnstore index is not for the seek on one value.

Prove the gain with the actual plan and the real row count. A small table in a test shows no difference.

## Foreign keys

SQL Server does not make an index for a foreign key column. A join from the parent and a delete of a parent row then scan the child table. EF migrations add the index. A hand-written schema must add it.

## Prove the plan

- Use the actual plan, not the estimated plan: `SET STATISTICS XML ON;` in a query window, or Query Store (`sys.query_store_plan`).
- Read: seek or scan, estimated against actual rows, key lookups, `CONVERT_IMPLICIT` warnings, and memory grant warnings.
- Run the query with the real parameter values and the real data volume. A plan on 100 rows proves nothing.
- EF: `query.ToQueryString()` shows the SQL and the parameter types.
- Parameter sniffing: one plan for all values. When a value has very different row counts, consider `OPTION (RECOMPILE)` for that query, or the parameter-sensitive plan optimization (compatibility level 160 and later).

## Find missing and unused indexes

```sql
-- Index use since the last restart
SELECT OBJECT_NAME(s.object_id) AS table_name, i.name, s.user_seeks, s.user_scans, s.user_updates
FROM sys.dm_db_index_usage_stats AS s
JOIN sys.indexes AS i ON i.object_id = s.object_id AND i.index_id = s.index_id
WHERE s.database_id = DB_ID()
ORDER BY s.user_updates DESC;
```

- `sys.dm_db_missing_index_details` gives suggestions. Treat them as hints: they ignore existing indexes and column order.
- `sp_BlitzIndex` from the First Responder Kit (MIT) gives a full report. Ask the DBA before you install it.
- Drop an unused index only after a full business cycle.

## Online index operations

- `CREATE INDEX ... WITH (ONLINE = ON)` lets writes continue during the build. Developer and Enterprise editions, and Azure SQL. Standard edition builds offline.
- `RESUMABLE = ON` (with `ONLINE = ON`) lets a long build pause and continue.
- `WAIT_AT_LOW_PRIORITY` limits the wait for the short schema lock at the start and the end.
- EF: `HasIndex(...).IsCreatedOnline()` for SQL Server.
