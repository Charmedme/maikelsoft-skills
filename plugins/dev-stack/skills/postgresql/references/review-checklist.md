# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Security
- SQL text
- Types and time
- Concurrency
- Indexes and plans
- Migrations on live tables
- Npgsql and Dapper
- Tests
- Decisions

## Security

| # | Check | Type |
|---|-------|------|
| 1 | Each value goes in as a parameter. No string concatenation or interpolation into SQL text that Npgsql or Dapper runs. | judge (`dev-stack:secure-coding`) |
| 2 | A dynamic identifier (sort column, table) comes from an allow list and is quoted. | judge |
| 3 | The application role has no DDL rights in production. Migrations run with their own identity (DATA-18). | judge |
| 4 | `EnableParameterLogging` and `Include Error Detail` are on only in development. | judge |

## SQL text

| # | Check | Type |
|---|-------|------|
| 5 | Names match the schema: PascalCase names in quotes, or snake_case without quotes. | judge |
| 6 | No `NOT IN (SELECT ...)`. `NOT EXISTS` instead. | tool (PG-NOT-IN-SUBQUERY) |
| 7 | `FromSql` on an entity with an `xmin` token selects `xmin`. | tool (PG-SELECT-STAR-FROMSQL), judge |
| 8 | SQL that EF cannot compose (`UPDATE ... RETURNING`, `FOR UPDATE`) is read with `ToListAsync` or `AsAsyncEnumerable`, not `SingleAsync` or `FirstOrDefaultAsync`. | judge |
| 9 | The `WHERE` clause compares the bare column (no cast, no function on an indexed column). | judge |

## Types and time

| # | Check | Type |
|---|-------|------|
| 10 | Instants are `timestamptz`. | tool (PG-TIMESTAMP-NO-TZ) |
| 11 | Parameters for instants are UTC (`DateTimeOffset` with offset 0, or `DateTime` with `Kind = Utc`). No `DateTime.Now`. | judge, `dev-stack:csharp` rules |
| 12 | A local day, week, or month uses `AT TIME ZONE '<IANA zone>'`, not `::date` or `date_trunc` in the session zone. Check the existing report that new code copies too. | tool (PG-LOCAL-DATE-NO-ZONE), judge |
| 13 | Keys are `bigint GENERATED ALWAYS AS IDENTITY` or `uuid`. No `serial`. | tool (PG-SERIAL) |
| 14 | Amounts are `numeric(p,s)`. No `money`, no `float8`. No `char(n)`. | judge |
| 15 | Case-insensitive columns use a collation (DATA-24), not `lower()` in each query. | judge |

## Concurrency

| # | Check | Type |
|---|-------|------|
| 16 | A counter or a stock level changes in one conditional `UPDATE`, with a check of the row count. | judge |
| 17 | A claim query uses `FOR UPDATE SKIP LOCKED` in one statement or in one transaction. | judge |
| 18 | Uniqueness comes from a unique index. An upsert uses `ON CONFLICT`. 23505 maps to 409. | judge |
| 19 | A `REPEATABLE READ` or `SERIALIZABLE` transaction retries the whole unit on 40001 and 40P01. | judge |
| 20 | Transactions are short. No HTTP call or user wait inside. No transaction open across requests. | judge |
| 21 | Advisory locks use the `xact` form, or the session form without PgBouncer in transaction mode. | judge |
| 22 | Rows change in the same order in each transaction (no deadlock by order). | judge |

## Indexes and plans

| # | Check | Type |
|---|-------|------|
| 23 | Each new index serves a named query. Equality columns first, then the range or sort column. | judge |
| 24 | A hot subset uses a partial index. `jsonb`, arrays, and full-text use GIN. | judge |
| 25 | A hand-written schema indexes each foreign key column that a join or a delete uses. | judge |
| 26 | A performance claim comes with `EXPLAIN (ANALYZE, BUFFERS)` on data like production. A finding from source code only says "check with the plan". | judge |

## Migrations on live tables

| # | Check | Type |
|---|-------|------|
| 27 | The migration sets `lock_timeout` before DDL on a live table. | tool (squawk `require-lock-timeout`), judge |
| 28 | Indexes on live tables use `CREATE INDEX CONCURRENTLY`, outside a transaction. | tool (squawk `require-concurrent-index-creation`), judge |
| 29 | No column with a volatile default in one step on a live table. | tool (squawk `adding-field-with-default`) |
| 30 | Foreign keys and checks on live tables use `NOT VALID`, then `VALIDATE CONSTRAINT`. `lock_timeout` does not make a check without `NOT VALID` safe: the scan runs under the `ACCESS EXCLUSIVE` lock. | tool (squawk `constraint-missing-not-valid`), judge |
| 31 | Renames and type changes follow expand and contract (OPS-15). | judge |
| 32 | An idempotent EF script has no `CREATE INDEX CONCURRENTLY` in a `DO` block (`dev-stack:efcore` rule 32a). | judge |

## Npgsql and Dapper

| # | Check | Type |
|---|-------|------|
| 33 | One `NpgsqlDataSource` per database. No new `NpgsqlConnection` in each call. | tool (PG-NEW-CONNECTION) |
| 34 | Dapper lists use `= ANY(@ids)`. | tool (PG-DAPPER-IN-LIST) |
| 35 | Each binary `COPY` calls `CompleteAsync` before the dispose. | judge |
| 36 | Dapper calls pass the `CancellationToken` through `CommandDefinition`. | judge |
| 37 | snake_case columns map with `MatchNamesWithUnderscores` or aliases. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 38 | Tests run against the pinned production image (DATA-20), not SQLite or the EF InMemory provider. | judge (`dev-stack:dotnet-testing`) |
| 39 | A claim, a counter, or an upsert has a test with parallel calls. | judge |
| 40 | A report per local day has a test with an instant near midnight UTC. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 41 | New code that needs DATA-23, DATA-24, or DATA-25 has an accepted decision, or the report marks it as assumed. | judge |
