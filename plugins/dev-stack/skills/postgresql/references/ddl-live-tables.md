# DDL on live tables

How to change a table that gets writes all day without blocking the application (DATA-25). EF migration workflow, bundles, and the idempotent script are in `dev-stack:efcore` (`references/migrations.md`, rule 32a).

## Contents

- Why DDL blocks
- lock_timeout first
- Indexes
- Columns
- Constraints
- Renames and type changes
- Order of a release
- Lint with Squawk
- EF migrations

## Why DDL blocks

Most `ALTER TABLE` forms take an `ACCESS EXCLUSIVE` lock. The statement waits until each open transaction on the table ends. While it waits, each new query on the table waits behind it, also plain `SELECT`s. One long report can so stop all writes for minutes. The DDL itself can be fast. The wait is the problem.

## lock_timeout first

```sql
SET lock_timeout = '5s';
```

Put it at the start of each migration that touches a live table. The statement then fails with 55P03 after 5 seconds instead of queueing traffic behind it. Run the migration again later. In EF, add `migrationBuilder.Sql("SET lock_timeout = '5s';")` as the first operation.

## Indexes

| Form | Effect |
|------|--------|
| `CREATE INDEX` | Blocks writes on the table for the whole build |
| `CREATE INDEX CONCURRENTLY` | Allows writes. Slower. Cannot run in a transaction block (25001) |
| `DROP INDEX CONCURRENTLY` | Drops without blocking reads and writes |
| `REINDEX INDEX CONCURRENTLY` | Builds the index again without blocking writes |

A failed concurrent build leaves an INVALID index. Find it with `SELECT indexrelid::regclass FROM pg_index WHERE NOT indisvalid;`, drop it, and build it again. A safe pre-deploy file:

```sql
SET lock_timeout = '5s';
DROP INDEX CONCURRENTLY IF EXISTS ix_order_lines_sku; -- only when it is INVALID
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_order_lines_sku ON order_lines (sku);
```

Run that file with psql outside a transaction (no `--single-transaction`).

## Columns

| Change | Safe form |
|--------|-----------|
| Add a nullable column | `ADD COLUMN note text` is fast. |
| Add a column with a constant default | `ADD COLUMN status text NOT NULL DEFAULT 'new'` is fast since PostgreSQL 11 (no table write). |
| Add a column with a volatile default | `gen_random_uuid()`, `clock_timestamp()`, `now()` in a generated value: the table is written again under `ACCESS EXCLUSIVE`. Add the column without a default, fill it in batches (`UPDATE ... WHERE id BETWEEN ...`, commit each batch), then `SET DEFAULT`. |
| Make a column NOT NULL | Add `CHECK (col IS NOT NULL) NOT VALID`, `VALIDATE CONSTRAINT`, then `SET NOT NULL` (PostgreSQL 12+ uses the valid check and skips the scan), then drop the check. |
| Change a type | Most type changes write the table again. Use expand and contract (below). `varchar(n)` to a larger `n` or to `text` is fast. |
| Drop a column | Fast, but deploy the code that no longer reads it first. |

## Constraints

```sql
ALTER TABLE order_lines
    ADD CONSTRAINT fk_order_lines_batches FOREIGN KEY (batch_id) REFERENCES batches (id) NOT VALID;
ALTER TABLE order_lines VALIDATE CONSTRAINT fk_order_lines_batches;
```

`NOT VALID` takes a short lock and checks only new rows. `VALIDATE CONSTRAINT` checks the old rows with a lock that allows writes. Put them in two migrations, or two transactions. A unique constraint: build a unique index concurrently first, then `ADD CONSTRAINT ... UNIQUE USING INDEX`.

## Renames and type changes

Use expand and contract (OPS-15) when old and new code run at the same time during a rolling deploy:

1. Expand: add the new column. The new code writes both columns and reads the old one.
2. Fill the new column in batches.
3. Switch: the code reads the new column.
4. Contract: a later release stops writing the old column and drops it.

A `RENAME COLUMN` in one step breaks each running instance of the old code.

## Order of a release

1. Pre-deploy SQL (concurrent indexes, `NOT VALID` constraints), outside a transaction.
2. The migration (expand steps), with `lock_timeout`.
3. The new application version.
4. Post-deploy SQL (`VALIDATE CONSTRAINT`, batches that fill columns).
5. A later release: the contract steps.

## Lint with Squawk

Squawk (`squawk-cli` on npm, 2.70.0 on 2026-10-15, Apache-2.0 or MIT) checks a SQL file for these risks. Ask the human before the first use (stack rule 5).

```
npx squawk-cli@2.70.0 db/0004_stock.sql
```

The rules for live tables: `require-lock-timeout`, `require-concurrent-index-creation`, `adding-field-with-default`, `adding-foreign-key-constraint`, `constraint-missing-not-valid`, `adding-not-nullable-field`, `renaming-column`, and `changing-column-type`. Type rules: `prefer-timestamptz`, `prefer-identity`, `prefer-bigint-over-int`. Use the rule names as the review checklist for a migration.

Squawk exits with code 1 on any warning. On the review fixture of this skill (2026-10-19) it found the blocking index, the volatile default, the missing `lock_timeout`, `timestamp`, and `serial`. It also reported style rules (`prefer-text-field` for each `varchar(n)`, `prefer-robust-stmts` for each statement without `IF NOT EXISTS`, `require-statement-timeout`). Turn off the style rules that the team does not want in `.squawk.toml`.

## EF migrations

- `IsCreatedConcurrently()` on an index: `Migrate` and a bundle run the statement without a transaction.
- The idempotent script puts each step in a `DO` block, where `CREATE INDEX CONCURRENTLY` fails (25001). Follow `dev-stack:efcore` rule 32a.
- EF writes a column with a default as one `ADD COLUMN ... DEFAULT`. For a volatile default on a live table, change the migration to the three steps above.
- Read the SQL of each migration before the release: `dotnet ef migrations script <from> <to>`.
