# Isolation and locking on SQL Server

Isolation levels, RCSI, optimized locking, lock hints, queues, upserts, deadlocks, and blocking. The EF side (concurrency tokens, the execution strategy) is in `dev-stack:efcore` (`references/writes.md`).

## Contents

- Isolation levels
- RCSI
- Optimized locking
- Pick the tool
- Upsert patterns
- Queue claim
- Lock hints
- Deadlocks
- Find blocking
- Error numbers

## Isolation levels

| Level | Readers | Notes |
|-------|---------|-------|
| `READ UNCOMMITTED` (`NOLOCK`) | Read uncommitted rows | Dirty reads, rows read two times or missed. Do not use. |
| `READ COMMITTED`, RCSI off | Take shared locks, wait for writers | The default of SQL Server. |
| `READ COMMITTED`, RCSI on | Read the last committed version, no shared locks | The default of Azure SQL Database. Recommended (DATA-26). |
| `SNAPSHOT` | One version for the whole transaction | Needs `ALLOW_SNAPSHOT_ISOLATION ON`. An update conflict fails with 3960. |
| `REPEATABLE READ` | Keep shared locks to the end | Blocks writers. Deadlock risk. |
| `SERIALIZABLE` | Key-range locks | The same as `HOLDLOCK` on each table. |

## RCSI

- On: `ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;`. The statement cannot run in a transaction (error 226), and it needs a moment with no other active connection (`WITH ROLLBACK IMMEDIATE` ends them).
- Versions go to `tempdb`, or to the persistent version store in the database when ADR is on. A transaction that stays open for a long time keeps old versions.
- Code that changes behavior: a read, then an update based on that read, now reads a committed value that another transaction is about to change. Put `UPDLOCK` on that read, or write it as one conditional `UPDATE`.
- `READCOMMITTEDLOCK` as a table hint forces the old locking read for one statement.

## Optimized locking

SQL Server 2025 and Azure SQL. Two parts:

- Transaction ID (TID) locking: a changed row is protected by one lock on the transaction, not by a lock on each row until the end. Fewer locks, almost no lock escalation.
- Lock after qualification (LAQ): with RCSI and `READ COMMITTED`, an `UPDATE` or `DELETE` checks its `WHERE` clause on the last committed version without a lock. It locks only the rows that qualify.

Facts (Microsoft Learn, checked 2026-10-19; settings checked on SQL Server 2025 CU9):

- `ALTER DATABASE CURRENT SET OPTIMIZED_LOCKING = ON` fails with error 12133 when ADR is off. Turn on `ACCELERATED_DATABASE_RECOVERY` first.
- LAQ is not used with the hints `UPDLOCK`, `READCOMMITTEDLOCK`, `XLOCK`, or `HOLDLOCK` on the table, at another isolation level, in `MERGE`, with an `OUTPUT` clause that returns rows, with a columnstore index, or with variable assignment.
- Behavior change: with LAQ, `UPDATE t SET b = 3 WHERE b = 2` can skip a row that a running transaction changes from 1 to 2. Without LAQ, it waits and then updates it. Code that needs a strict order uses `REPEATABLE READ` or `SERIALIZABLE`.

## Pick the tool

| Need | Use |
|------|-----|
| Change a counter or a stock level | One conditional `UPDATE ... WHERE Available >= @q`, then check `@@ROWCOUNT` or the EF count |
| Two users edit one aggregate | `rowversion` (DATA-15) and 409 |
| Take the next item of a queue | `UPDATE TOP (1) ... WITH (UPDLOCK, READPAST, ROWLOCK) ... OUTPUT` |
| Insert or replace by key | `MERGE ... WITH (HOLDLOCK)`, or `UPDATE` with `UPDLOCK, HOLDLOCK`, then `INSERT` |
| One job at a time across replicas | `sp_getapplock` with `@LockOwner = 'Transaction'` |
| A report next to writes | RCSI, no hint |

## Upsert patterns

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;

UPDATE dbo.StockLevels WITH (UPDLOCK, HOLDLOCK)
SET Available = @Available
WHERE Sku = @Sku;

IF @@ROWCOUNT = 0
    INSERT dbo.StockLevels (Sku, Available) VALUES (@Sku, @Available);

COMMIT TRANSACTION;
```

- `HOLDLOCK` keeps a key-range lock on the missing key, so a second session waits instead of inserting the same key. Without it, two sessions both see no row and both insert: error 2627. A test with 4 parallel calls for each of 30 new keys got 2627 three times with `MERGE` and no `HOLDLOCK`, and never with `HOLDLOCK` (2026-10-19).
- `MERGE ... WITH (HOLDLOCK) AS t USING ...` is the one-statement form. `MERGE` refuses `NOLOCK` and `READUNCOMMITTED`.
- Insert first and catch 2627 is valid when inserts are much more common than updates.

## Queue claim

```sql
UPDATE TOP (1) j
SET Status = 'Taken', TakenBy = @Worker, TakenAt = SYSUTCDATETIME()
OUTPUT inserted.Id
FROM dbo.Jobs AS j WITH (UPDLOCK, READPAST, ROWLOCK)
WHERE j.Status = 'New';
```

- `READPAST` skips rows that another transaction locks. A test with one claim held open got the next row in 0.2 seconds for the second session (2026-10-19).
- `TOP (1)` without `ORDER BY` takes any row. For the oldest first, use a CTE with `SELECT TOP (1) ... ORDER BY CreatedAt` and update the CTE.
- An index on `(Status, CreatedAt)` (or a filtered index `WHERE Status = 'New'`) keeps the scan short.

## Lock hints

| Hint | Use |
|------|-----|
| `UPDLOCK` | A read that the same transaction then updates |
| `HOLDLOCK` (`SERIALIZABLE`) | The check for a missing key in an upsert |
| `READPAST` | A queue claim |
| `ROWLOCK` | A hint against page locks in a queue. SQL Server can still escalate. |
| `NOLOCK`, `READUNCOMMITTED` | Do not use. |
| `TABLOCK` | Bulk loads into a staging table |

Hints turn LAQ off for their table. Use them only where the logic needs them.

## Deadlocks

- SQL Server ends one transaction with error 1205. Retry the whole transaction: EF `EnableRetryOnFailure` retries 1205 inside the execution strategy. Outside EF, retry the whole unit of work up to 3 times with a short random delay, on a new transaction.
- Read the graph: the `system_health` Extended Events session keeps recent `xml_deadlock_report` events. With optimized locking, the graph shows `xactlock` resources.
- Fix the cause: add the index that the `UPDATE` or `DELETE` needs (a scan locks more rows), change tables and rows in the same order (sort keys), keep transactions short, and use RCSI so that readers take no part.
- `sp_BlitzLock` from the First Responder Kit (MIT) summarizes deadlocks. Ask the DBA before you install it.

## Find blocking

```sql
SELECT r.session_id, r.blocking_session_id, r.wait_type, r.wait_time, t.text
FROM sys.dm_exec_requests AS r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) AS t
WHERE r.blocking_session_id <> 0;
```

A session in `sys.dm_exec_sessions` with `open_transaction_count > 0` and status `sleeping` holds locks without work: often a client timeout in a procedure without `SET XACT_ABORT ON`.

## Error numbers

| Number | Meaning | Action |
|--------|---------|--------|
| 2601, 2627 | Unique index or key | 409, or "already done" for an idempotent call |
| 547 | Foreign key or check | 409 or 422 |
| 1205 | Deadlock victim | Retry the whole transaction |
| 1222 | Lock request timeout (`SET LOCK_TIMEOUT`) | Retry later |
| 3960 | Snapshot update conflict | Retry the whole transaction |
| 9820 | Invalid time zone name in `AT TIME ZONE` | Use the Windows name |
| -2 (SqlClient) | Command timeout | Check blocking, then the plan |
