# Indexes and plans on PostgreSQL

How to choose an index and prove that a query uses it. EF index APIs (`HasIndex`, `HasFilter`, `HasMethod`, `IsCreatedConcurrently`) are in `dev-stack:efcore` (`references/provider-postgresql.md`).

## Contents

- Start from the query
- B-tree column order
- Partial indexes
- Covering indexes
- GIN, GiST, BRIN
- Foreign keys
- Prove the plan
- Find missing and unused indexes

## Start from the query

Write the hot query first. Then design one index that serves its `WHERE`, `JOIN`, and `ORDER BY`. An index on each column does not help: PostgreSQL uses one index per table scan in most plans, and each index costs time on each write and space in memory.

## B-tree column order

1. Columns compared with `=` first.
2. Then one range or sort column (`<`, `>`, `BETWEEN`, `ORDER BY`).
3. Columns after the first range column help less. PostgreSQL 18 can skip a leading column with few distinct values (skip scan), but do not design for it.

Example: `WHERE customer_id = @c ORDER BY placed_at DESC LIMIT 50` needs `(customer_id, placed_at)`.

Compare the bare column. `WHERE placed_at::date = @day` cannot use an index on `placed_at`. Write `placed_at >= @start AND placed_at < @end`.

## Partial indexes

```sql
CREATE INDEX CONCURRENTLY ix_orders_picking_queue
    ON orders (placed_at, id)
    WHERE status = 'Placed';
```

Use a partial index when the query always filters on a small subset (open orders, unsent outbox rows). The index stays small when most rows leave the subset. The query must repeat the same condition, with the same literal, so that the planner can match it.

## Covering indexes

`INCLUDE (total)` adds columns that the query reads but does not filter on. An index-only scan then skips the table, as long as `VACUUM` keeps the visibility map current.

## GIN, GiST, BRIN

| Type | Use for |
|------|---------|
| GIN | `jsonb` containment (`@>`), arrays, `tsvector` full-text search, `pg_trgm` for `LIKE '%term%'` and `ILIKE` |
| GiST | ranges, exclusion constraints (no overlap), geometry, nearest neighbor |
| BRIN | very large tables where the column follows the insert order (an append-only log by time) |

`pg_trgm` is an extension: `CREATE EXTENSION IF NOT EXISTS pg_trgm;` needs the right on the database. Ask the DBA.

## Foreign keys

PostgreSQL makes an index for a primary key and a unique constraint, not for a foreign key column. Without it:

- A join from the parent to the children scans the child table.
- A delete of a parent row scans the child table for each row, under a lock.

EF migrations add the index (`IX_order_lines_OrderId`). A hand-written schema must add it.

## Prove the plan

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id FROM orders
WHERE status = 'Placed'
ORDER BY placed_at, id
LIMIT 1
FOR UPDATE SKIP LOCKED;
```

- Run it on data like production. On a table with 100 rows, a sequential scan is the best plan, and the result proves nothing.
- `ANALYZE` runs the query. Wrap a write in `BEGIN; ... ROLLBACK;`.
- Read: the scan type of each table, the estimated rows against the actual rows, and `Buffers: shared read` (disk) against `shared hit` (memory).
- A big gap between estimated and actual rows: run `ANALYZE <table>`, or add extended statistics for related columns.
- EF: `query.ToQueryString()` gives the SQL. Run `EXPLAIN` on it with the real parameter values.

## Find missing and unused indexes

```sql
-- Tables with many sequential scans
SELECT relname, seq_scan, seq_tup_read, idx_scan
FROM pg_stat_user_tables
ORDER BY seq_tup_read DESC
LIMIT 20;

-- Indexes that no query used since the last statistics reset
SELECT indexrelname, idx_scan, pg_size_pretty(pg_relation_size(indexrelid))
FROM pg_stat_user_indexes
WHERE idx_scan = 0;
```

`pg_stat_statements` (an extension) shows the queries with the most total time. Drop an unused index only after a full business cycle (month end, year end), and with `DROP INDEX CONCURRENTLY`.
