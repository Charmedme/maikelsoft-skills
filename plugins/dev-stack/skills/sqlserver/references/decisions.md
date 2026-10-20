# SQL Server decisions

The team decisions that SQL Server code needs. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

This skill owns DATA-26, DATA-27, DATA-28, and DATA-29. For DATA-07, DATA-10, DATA-11, DATA-15, and DATA-22, the full entry with the EF side is in `dev-stack:efcore` (`references/decisions.md`). The entries below give only the SQL Server engine part. DATA-01 (engine) is in `dev-stack:efcore`, and DATA-20 (test database) is in `dev-stack:dotnet-testing`.

## Contents

- DATA-26. RCSI on SQL Server
- DATA-27. Optimized locking on SQL Server 2025
- DATA-28. String types on SQL Server
- DATA-07. Primary key type (engine part)
- DATA-10. Date and time type (engine part)
- DATA-11. Money and decimal precision (engine part)
- DATA-15. Optimistic concurrency (engine part)
- DATA-29. Stored procedures
- DATA-22. Bulk operations (engine part)

## DATA-26. RCSI on SQL Server

- Question: Do we turn on READ_COMMITTED_SNAPSHOT (RCSI) for the database?
- Ask when: the first database of a service, the first report that runs next to writes, or the first blocking or deadlock between a reader and a writer.
- Options: RCSI on: `READ COMMITTED` reads the last committed version, and readers and writers do not block each other. RCSI off: `READ COMMITTED` takes shared locks, and readers wait for writers. `SNAPSHOT` isolation for chosen transactions only (`ALLOW_SNAPSHOT_ISOLATION ON`).
- Recommended: RCSI on for each new OLTP database, and in the test database, so that the tests show the production behavior. It is the default of Azure SQL Database. Do not use `NOLOCK` to avoid blocking.
- Choose another option when: an existing database has code that depends on a blocking read (a read, then a change based on that read, without `UPDLOCK`). Fix that code first, then turn RCSI on.
- Depends on: DATA-01.
- Revisit when: `tempdb` (or the persistent version store with ADR) grows because of long transactions.
- Check: the query on `sys.databases` in `SKILL.md` (section Check). Tool (SQL-NOLOCK).
- Detect: DET-DATA-26.

## DATA-27. Optimized locking on SQL Server 2025

- Question: Do we turn on optimized locking for the database?
- Ask when: the first SQL Server 2025 database, or an upgrade to 2025.
- Options: On (needs accelerated database recovery, ADR). Off (the default on SQL Server 2025). Azure SQL Database always has it on.
- Recommended: On for a new database, together with ADR and RCSI (the Azure SQL default). Off for an existing database until the code is checked for updates that depend on a strict order of transactions: with lock after qualification, an `UPDATE` checks its `WHERE` clause on the last committed version and can skip a row that a running transaction changes.
- Choose another option when: the database has many lock hints (`UPDLOCK`, `HOLDLOCK`) or columnstore indexes: the gain is small. The team cannot test the order-dependent code.
- Depends on: DATA-26.
- Revisit when: the database moves to SQL Server 2025, or lock memory and lock escalation show in monitoring.
- Check: the query on `sys.databases` in `SKILL.md` (section Check).
- Detect: DET-DATA-27.

## DATA-28. String types on SQL Server

- Question: Which string types and which collation do new columns use?
- Ask when: the first string column.
- Options: `nvarchar(n)` for user text and `varchar(n)` for codes, with the server default collation (`SQL_Latin1_General_CP1_CI_AS` or `Latin1_General_100_CI_AS`). A `_UTF8` collation with `varchar(n)` for all text. `nvarchar(n)` for all text.
- Recommended: `nvarchar(n)` for text that users type (names, notes), `varchar(n)` with `IsUnicode(false)` in EF for codes (SKU, ISO country and currency codes, status values), and the server default case-insensitive collation. Give each column a maximum length. Type each parameter like its column, or the index cannot seek.
- Choose another option when: the data is mostly Latin text and storage matters: a `_UTF8` collation with `varchar`. A column needs case-sensitive compare: a `_CS_` collation on that column.
- Depends on: DATA-01.
- Revisit when: an implicit conversion (`CONVERT_IMPLICIT`) shows in the plan of a hot query.
- Check: tool (SQL-ADDWITHVALUE), judge.
- Detect: DET-DATA-28.

## DATA-07. Primary key type (engine part)

- Question: How do writers without EF get a key, and which key type is the clustered index?
- Ask when: the first table that Dapper, a procedure, or another service writes.
- Options: `uniqueidentifier DEFAULT NEWSEQUENTIALID()`. `bigint IDENTITY`. `uniqueidentifier DEFAULT NEWID()` (random).
- Recommended: The catalog default stays: `Guid` keys from the EF sequential generator. Writers without EF: `DEFAULT NEWSEQUENTIALID()`, or `bigint IDENTITY` for a very large table. Never a clustered key on `NEWID()`: random inserts split pages. `Guid.CreateVersion7()` is not ordered in SQL Server.
- Choose another option when: a table has hundreds of millions of rows and many indexes: `bigint IDENTITY`, because each nonclustered index carries the clustered key.
- Depends on: DATA-01, DATA-02.
- Revisit when: index size or page splits are a measured problem.
- Check: judge.
- Detect: none.

## DATA-10. Date and time type (engine part)

- Question: Which column type holds instants, and how does T-SQL compute a local day?
- Ask when: the first date or time column, or the first report per day.
- Options: `datetimeoffset(7)`. `datetime2(7)` in UTC. `datetime`.
- Recommended: `datetimeoffset` for instants, written as UTC. Compute a local day with `AT TIME ZONE '<Windows zone name>'` (`'W. Europe Standard Time'`). Keep a mapping from the IANA zone of a user to the Windows name in code (`TimeZoneInfo.TryConvertIanaIdToWindowsId`).
- Choose another option when: an existing schema uses `datetime2` in UTC: keep it, and convert with `AT TIME ZONE 'UTC' AT TIME ZONE '<zone>'`.
- Depends on: DATA-01.
- Revisit when: a report is off by one day near midnight.
- Check: tool (SQL-DATETIME-TYPE, SQL-IANA-ZONE).
- Detect: none.

## DATA-11. Money and decimal precision (engine part)

- Question: Which SQL Server types hold an amount and a currency?
- Ask when: the first amount column.
- Options: `decimal(19,4)`. `money` (4 decimals, rounding in multiply and divide). `float` (binary rounding).
- Recommended: `decimal(19,4)` for an amount, `char(3)` for an ISO currency code.
- Choose another option when: a payment API uses minor units: `bigint` at that boundary only.
- Depends on: DATA-02.
- Revisit when: a currency with more than 4 decimals appears.
- Check: judge.
- Detect: none.

## DATA-15. Optimistic concurrency (engine part)

- Question: Which value is the concurrency token on SQL Server?
- Ask when: the first update endpoint or command.
- Options: A `rowversion` column. An own `bigint` version column.
- Recommended: `rowversion`. SQL Server changes it on each update. It is unique in the database and is not a time. In raw SQL, compare it in the `WHERE` clause of the `UPDATE` and check the row count.
- Choose another option when: other systems copy the rows and need a version that stays the same after a copy: an own version column.
- Depends on: DATA-01.
- Revisit when: no signal. Keep it.
- Check: judge.
- Detect: none.

## DATA-29. Stored procedures

- Question: Do new services put logic in stored procedures?
- Ask when: the first query that someone wants to write as a procedure, or the first database that a DBA team owns.
- Options: No new procedures: LINQ or parameterized SQL in code, versioned with the service. Procedures for each data access. Procedures only for jobs that a DBA owns (maintenance, ETL).
- Recommended: No new procedures in services. Code and SQL change in one commit, one review, and one test run. Procedures only for jobs that a DBA owns, or in a database that another team owns.
- Choose another option when: a DBA team owns the database and gives access only through procedures. A batch job moves much data inside the database.
- Depends on: DATA-02.
- Revisit when: the same logic is needed by several applications in other languages.
- Check: judge. A procedure follows the rules in `SKILL.md` (rule 9 for `XACT_ABORT`, rule 11 for dynamic SQL).
- Detect: DET-DATA-29.

## DATA-22. Bulk operations (engine part)

- Question: How do we insert many rows into SQL Server?
- Ask when: the first import or batch job with more than a few thousand rows.
- Options: `SqlBulkCopy`. A table-valued parameter with one `INSERT ... SELECT`. Batched `SaveChangesAsync`.
- Recommended: `SqlBulkCopy` with column mappings, `EnableStreaming`, and a `BatchSize`. For an upsert, copy into a staging table, then one `MERGE ... WITH (HOLDLOCK)` or `UPDATE` and `INSERT` in one transaction.
- Choose another option when: a few hundred rows: a table-valued parameter or batched `SaveChangesAsync`.
- Depends on: DATA-02.
- Revisit when: the import must run next to heavy writes on the same table (lock escalation).
- Check: judge.
- Detect: DET-MSSQL-BULKCOPY.
