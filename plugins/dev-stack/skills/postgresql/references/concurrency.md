# Concurrency on PostgreSQL

MVCC, isolation levels, row locks, queues, upserts, and retries. The EF side (concurrency tokens, the execution strategy) is in `dev-stack:efcore` (`references/writes.md`).

## Contents

- MVCC in one paragraph
- Pick the tool
- Atomic conditional update
- Queue claim with SKIP LOCKED
- Upsert with ON CONFLICT
- Isolation levels and retries
- Advisory locks
- Deadlocks and lock waits
- Error codes

## MVCC in one paragraph

Each `UPDATE` writes a new row version. A reader sees the versions that were committed when its statement started (`READ COMMITTED`) or when its transaction started (`REPEATABLE READ`). Readers never wait for writers, and writers never wait for readers. Writers wait for each other only on the same row. So a report does not block order placement, and no lock hint is necessary for reads. Old versions stay until `VACUUM` removes them. A transaction that stays open for hours stops that cleanup for the whole database.

## Pick the tool

| Need | Use |
|------|-----|
| Change a counter or a stock level | One conditional `UPDATE`, then check the row count |
| Two users edit one aggregate | A concurrency token (`xmin`, DATA-15) and 409 |
| Take the next item of a queue | `FOR UPDATE SKIP LOCKED` in one statement or one transaction |
| Insert or replace by key | `INSERT ... ON CONFLICT (key) DO UPDATE` |
| One job at a time across replicas | An advisory lock (`pg_try_advisory_xact_lock`) |
| A rule over many rows (no two bookings overlap) | An exclusion constraint, or `SERIALIZABLE` with a retry |

## Atomic conditional update

```sql
UPDATE stock_levels
SET available = available - @quantity
WHERE sku = @sku AND available >= @quantity;
-- 0 rows: not enough stock. 1 row: reserved.
```

A read, a check in C#, and a write lose updates when two requests run at the same time. Both read 10, both write 9. EF: `ExecuteUpdateAsync` with the same `Where`, then check the returned count.

## Queue claim with SKIP LOCKED

The lock of `FOR UPDATE` lasts until the end of the transaction. In autocommit mode, the transaction is the one statement. So:

- One statement: `UPDATE ... WHERE id = (SELECT id ... FOR UPDATE SKIP LOCKED LIMIT 1) RETURNING ...`. The lock covers the `UPDATE`.
- Two statements (`SELECT ... FOR UPDATE SKIP LOCKED`, then `UPDATE`): put both in one transaction. Without it, the lock ends after the `SELECT`, and two workers got the same row in a test with two connections.
- `FOR UPDATE` without `SKIP LOCKED`: the second worker waits for the first. After the wait, the subquery can return no row, and the worker gets nothing although other rows are free.

Index the claim query: a partial index `ON orders (placed_at, id) WHERE status = 'Placed'` keeps the scan short when most rows are done.

EF: run the one-statement form with `SqlQuery<Guid>(...).ToListAsync()`. With `FromSql`, select `o.*, o.xmin` when the entity has an `xmin` token, run it in a transaction, and save in the same transaction.

## Upsert with ON CONFLICT

```sql
INSERT INTO stock_levels (sku, available)
VALUES (@sku, @available)
ON CONFLICT (sku) DO UPDATE SET available = EXCLUDED.available;
```

- The conflict target needs a unique index or constraint on exactly those columns.
- A check before the insert (`SELECT`, then `INSERT`) fails with 23505 when two calls insert the same new key.
- `ON CONFLICT DO NOTHING` returns no row for the existing key. Add `RETURNING` and read the row in a second query when you need it.
- `MERGE` exists since PostgreSQL 15. It does not replace `ON CONFLICT` for a concurrent upsert: under `READ COMMITTED`, two `MERGE` statements can both try the insert and one fails with 23505.

## Isolation levels and retries

- `READ COMMITTED` (default): each statement sees a fresh snapshot. Use conditional statements and tokens.
- `REPEATABLE READ`: one snapshot for the transaction. An update of a row that another transaction changed fails with 40001.
- `SERIALIZABLE`: PostgreSQL detects dangerous patterns and fails one transaction with 40001.

A transaction at a stricter level needs a retry of the whole unit: read again, decide again, write again. Npgsql sets `IsTransient = true` for 40001 and 40P01, so `EnableRetryOnFailure` in EF retries them. Run the transaction inside `CreateExecutionStrategy().ExecuteAsync` (`dev-stack:efcore` rule 20). With Dapper, write the loop yourself: 3 tries, a short random delay, a new connection and transaction for each try.

## Advisory locks

```sql
SELECT pg_try_advisory_xact_lock(hashtext('nightly-stock-import'));
```

- The `xact` form ends with the transaction. The session form (`pg_advisory_lock`) ends with the connection, and a pooled connection can keep it. With PgBouncer in transaction mode, use only the `xact` form (DATA-23).
- Use `try` for a job that must run on one replica only. A blocking `pg_advisory_lock` makes the other replicas wait.

## Deadlocks and lock waits

- PostgreSQL ends one transaction of a deadlock with 40P01 after `deadlock_timeout` (1 second by default). Retry it.
- Repeated deadlocks are a design defect. Change rows in the same order in each transaction (for example sorted by key), and keep transactions short.
- Find waits: `SELECT pid, wait_event_type, state, query FROM pg_stat_activity WHERE wait_event_type = 'Lock';` and `pg_blocking_pids(pid)`.
- Set `statement_timeout` and `idle_in_transaction_session_timeout` for the application role, so that a stuck request does not hold locks for ever.

## Error codes

| SQLSTATE | `PostgresErrorCodes` | Action |
|----------|----------------------|--------|
| 23505 | `UniqueViolation` | 409, or treat as "already done" for an idempotent call |
| 23503 | `ForeignKeyViolation` | 409 or 422 |
| 40001 | `SerializationFailure` | Retry the whole transaction |
| 40P01 | `DeadlockDetected` | Retry the whole transaction |
| 55P03 | `LockNotAvailable` | `NOWAIT` or `lock_timeout` hit: retry later or fail the deploy step |
| 57014 | `QueryCanceled` | `statement_timeout` hit |
