# PostgreSQL decisions

The team decisions that PostgreSQL code needs. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

This skill owns DATA-23, DATA-24, and DATA-25. For DATA-07, DATA-10, DATA-11, DATA-15, and DATA-22, the full entry with the EF side is in `dev-stack:efcore` (`references/decisions.md`). The entries below give only the PostgreSQL engine part. DATA-01 (engine) is in `dev-stack:efcore`, and DATA-20 (test database) is in `dev-stack:dotnet-testing`.

## Contents

- DATA-23. Connection pooling on PostgreSQL
- DATA-07. Primary key type (engine part)
- DATA-10. Date and time type (engine part)
- DATA-11. Money and decimal precision (engine part)
- DATA-24. Case-insensitive text on PostgreSQL
- DATA-15. Optimistic concurrency (engine part)
- DATA-25. DDL on live tables
- DATA-22. Bulk operations (engine part)

## DATA-23. Connection pooling on PostgreSQL

- Question: Do we use only the Npgsql pool, or add PgBouncer between the services and PostgreSQL?
- Ask when: the first service that connects to PostgreSQL, and each time the number of replicas goes up.
- Options: Npgsql pool only: one `NpgsqlDataSource` per database in each process, `Maximum Pool Size` 100 by default. PgBouncer in transaction mode: many client connections share few server connections. A managed pooler of the cloud (for example the built-in PgBouncer of Azure Database for PostgreSQL).
- Recommended: The Npgsql pool only. Register one `NpgsqlDataSource` per database. Add PgBouncer in transaction mode when the replicas times `Maximum Pool Size` comes near `max_connections` of the server.
- Choose another option when: many replicas or serverless functions each open a pool. The cloud offers a pooler that the operations team already runs.
- Depends on: DATA-01.
- Revisit when: the server reports "too many connections" or `max_connections` must go up for a new replica.
- Check: judge. With PgBouncer in transaction mode, check that the code uses no session state across transactions (session `SET`, advisory session locks, `LISTEN`).
- Detect: DET-DATA-23.

## DATA-07. Primary key type (engine part)

- Question: How do writers without EF get a key on a `uuid` column?
- Ask when: the first table that Dapper, psql, or another service writes.
- Options: `uuid` with `DEFAULT uuidv7()` (PostgreSQL 18). `uuid` with `DEFAULT gen_random_uuid()` (random, version 4). `bigint GENERATED ALWAYS AS IDENTITY`.
- Recommended: The catalog default stays: `uuid` keys that the EF provider generates (EFCore.PG 9+ makes UUIDv7, not the Npgsql driver). On PostgreSQL 18, also add `DEFAULT uuidv7()` to the column, so that writers without EF get ordered keys. Small lookup tables: `bigint GENERATED ALWAYS AS IDENTITY`.
- Choose another option when: the server is 17 or older: `gen_random_uuid()`, or a key from the application.
- Depends on: DATA-01, DATA-02.
- Revisit when: the server moves to 18.
- Check: tool (PG-SERIAL).
- Detect: none.

## DATA-10. Date and time type (engine part)

- Question: Which column type holds instants, and how does SQL compute a local day?
- Ask when: the first date or time column, or the first report per day.
- Options: `timestamptz` for instants. `timestamp` (no time zone) for a local wall-clock value. `date` for a calendar day.
- Recommended: `timestamptz` for each instant, written as UTC. Compute a local day with `AT TIME ZONE '<IANA zone>'`. Store the IANA zone of a user in its own `text` column when it matters.
- Choose another option when: the value is a local wall-clock time with no instant (for example an opening hour): `time` or `timestamp`.
- Depends on: DATA-01.
- Revisit when: a report is off by one day near midnight.
- Check: tool (PG-TIMESTAMP-NO-TZ). Judge for local `DateTime` parameters.
- Detect: none.

## DATA-11. Money and decimal precision (engine part)

- Question: Which PostgreSQL types hold an amount and a currency?
- Ask when: the first amount column.
- Options: `numeric(19,4)`. `money` (formats with `lc_monetary`). `float8` (binary rounding). `bigint` minor units.
- Recommended: `numeric(19,4)` for an amount. `varchar(3)` with `CHECK (currency ~ '^[A-Z]{3}$')` for an ISO currency code. The PostgreSQL wiki advises against `char(n)` because it pads with spaces.
- Choose another option when: a payment API uses minor units: `bigint` at that boundary only.
- Depends on: DATA-02.
- Revisit when: a currency with more than 4 decimals appears.
- Check: judge.
- Detect: none.

## DATA-24. Case-insensitive text on PostgreSQL

- Question: How do we compare text such as an email address without case?
- Ask when: the first unique or searched text column that users type (email, user name, SKU from a scanner).
- Options: A nondeterministic ICU collation on the column (`locale 'und-u-ks-level2'`, `deterministic = false`). The `citext` extension. A normalized copy (`lower(email)`) with a unique index. `ILIKE` in each query.
- Recommended: A nondeterministic ICU collation on the column. The unique index then refuses `A@x.nl` next to `a@x.nl`, and each comparison works without `lower()`. PostgreSQL 18 also supports `LIKE` on such a column. Keep `citext` only in schemas that already use it.
- Choose another option when: the server is 17 or older and the column needs `LIKE`: a normalized copy with an index. One search query only: `ILIKE` or `EF.Functions.ILike` with a `pg_trgm` index.
- Depends on: DATA-01, DATA-09.
- Revisit when: a query needs pattern match on the column on a server older than 18.
- Check: judge. The EF mapping is in `dev-stack:efcore` (`references/provider-postgresql.md`).
- Detect: DET-DATA-24.

## DATA-15. Optimistic concurrency (engine part)

- Question: Which value is the concurrency token on PostgreSQL?
- Ask when: the first update endpoint or command.
- Options: The `xmin` system column. An own `bigint version` column that the code increments.
- Recommended: `xmin` for the EF token (`[Timestamp] uint Version`). It changes on each update and needs no column. In raw SQL through EF, select it (`SELECT o.*, o.xmin`). Do not use `xmin` as an external version (a search index, an API ETag that a client stores long): it is 32 bits and wraps around. Use an own `bigint` column there (SEARCH-02).
- Choose another option when: other services read the version: an own `bigint version` column.
- Depends on: DATA-01.
- Revisit when: no signal. Keep it.
- Check: tool (PG-SELECT-STAR-FROMSQL), judge.
- Detect: none.

## DATA-25. DDL on live tables

- Question: Which rules does a migration follow when it changes a table that gets writes all day?
- Ask when: the first migration on a table with data in production, or the first index on a big table.
- Options: Plain DDL in the migration (simple, blocks writes). `lock_timeout`, concurrent index builds, `NOT VALID` constraints, and expand and contract steps. An online schema change tool (for example pg_osc or pgroll).
- Recommended: Each migration that touches a table with live writes sets `lock_timeout` (for example 5 seconds), builds indexes with `CREATE INDEX CONCURRENTLY` (EF: `IsCreatedConcurrently()`, one idempotent script: `dev-stack:efcore` rule 32a), adds constraints as `NOT VALID` and validates them later, and follows expand and contract for renames and type changes (OPS-15).
- Choose another option when: the table is small or has no traffic during the deploy: plain DDL. A large type change on a hot table: a tool such as pgroll, after a license check and the approval of the human.
- Depends on: DATA-17, DATA-18.
- Revisit when: a deploy blocks traffic or a migration times out on its lock.
- Check: tool (squawk, when the team approved it), judge.
- Detect: DET-DATA-25.

## DATA-22. Bulk operations (engine part)

- Question: How do we insert many rows into PostgreSQL?
- Ask when: the first import or batch job with more than a few thousand rows.
- Options: Binary `COPY` (`BeginBinaryImportAsync`). Multi-row `INSERT` with arrays (`INSERT ... SELECT * FROM unnest(@a, @b)`). `SaveChangesAsync` in batches.
- Recommended: Binary `COPY`, with `CompleteAsync()` before the dispose. For an upsert, `COPY` into a temporary table, then `INSERT ... SELECT ... ON CONFLICT DO UPDATE`.
- Choose another option when: a few hundred rows: one `INSERT ... SELECT * FROM unnest(...)` or batched `SaveChangesAsync`.
- Depends on: DATA-02.
- Revisit when: the import must merge into a table with live writes (then mind DATA-25).
- Check: judge (`CompleteAsync` present).
- Detect: DET-PG-COPY.
