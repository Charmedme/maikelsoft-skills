# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Security
- Isolation and locks
- Writes under concurrency
- Types and time
- Indexes and plans
- Procedures and dynamic SQL
- SqlClient and Dapper
- Tests
- Decisions
- Existing code

## Security

| # | Check | Type |
|---|-------|------|
| 1 | No SQL built by concatenation. Dynamic SQL uses `sp_executesql` with parameters. | tool (SQL-EXEC-CONCAT), judge |
| 2 | A dynamic identifier comes from an allow list and goes through `QUOTENAME`. | judge |
| 3 | No password in a connection string in source code. Azure uses managed identity. | judge (`dev-stack:secure-coding`) |
| 4 | The application login has no DDL rights in production. | judge |
| 5 | `TrustServerCertificate=True` only for a local container. | judge |

## Isolation and locks

| # | Check | Type |
|---|-------|------|
| 6 | No `NOLOCK`, `READUNCOMMITTED`, `READ UNCOMMITTED`, or `IsolationLevel.ReadUncommitted`. | tool (SQL-NOLOCK) |
| 7 | RCSI is on for the database, or the report names DATA-26 as an open point. | judge |
| 8 | The test database has the production settings (RCSI, compatibility level). | judge |
| 9 | With optimized locking and RCSI, no code depends on a strict order of transactions. | judge |
| 10 | Lock hints appear only where the logic needs them (`UPDLOCK` before an update, `HOLDLOCK` in an upsert, `READPAST` in a queue). | judge |
| 11 | Deadlock victims (1205) retry the whole transaction. | judge |

## Writes under concurrency

| # | Check | Type |
|---|-------|------|
| 12 | A counter or a stock level changes in one conditional statement with a check of the row count. | judge |
| 13 | A multi-row change that must be all or nothing runs in one transaction. | judge |
| 14 | A concurrent upsert uses `MERGE ... WITH (HOLDLOCK)` or `UPDATE` with `UPDLOCK, HOLDLOCK` then `INSERT`. | tool (SQL-MERGE-NO-HOLDLOCK), judge |
| 15 | A queue claim uses `UPDLOCK, READPAST` in one statement. | judge |
| 16 | 2601 and 2627 map to 409. 547 maps to 409 or 422. | judge |
| 17 | With retries on, explicit EF transactions run inside `CreateExecutionStrategy().ExecuteAsync` (`dev-stack:efcore` rule 20). | judge |
| 18 | Rows and tables change in the same order in each transaction. | judge |

## Types and time

| # | Check | Type |
|---|-------|------|
| 19 | `AT TIME ZONE` uses a Windows zone name. | tool (SQL-IANA-ZONE) |
| 20 | A `datetime2` without offset converts from UTC first (`AT TIME ZONE 'UTC' AT TIME ZONE '<zone>'`). | judge |
| 21 | Instants are `datetimeoffset` or UTC `datetime2`. No `datetime`, no `GETDATE()`. | tool (SQL-DATETIME-TYPE), judge |
| 22 | Amounts are `decimal(p,s)`. No `money`, no `float`. | judge |
| 23 | `nvarchar(n)` for user text, `varchar(n)` for codes. No filter on `nvarchar(max)`. | judge |
| 24 | Clustered `uniqueidentifier` keys are sequential (`NEWSEQUENTIALID()` or EF). No `NEWID()`, no `Guid.CreateVersion7()`: SQL Server sorts the last 6 bytes first. | judge |
| 25 | SQL Server 2025 functions match the compatibility level. No `=` on a `json` column. | judge |

## Indexes and plans

| # | Check | Type |
|---|-------|------|
| 26 | Each new index serves a named query: equality columns, then the range or sort column, then `INCLUDE`. | judge |
| 27 | Each foreign key column that a join or a delete uses has an index. | judge |
| 28 | A filtered index matches a query with a literal condition. | judge |
| 29 | No `CONVERT_IMPLICIT` on an indexed column in the plan of a hot query. | judge |
| 30 | A performance claim comes with an actual plan on data like production. A finding from source code only says "check with the plan". | judge |

## Procedures and dynamic SQL

| # | Check | Type |
|---|-------|------|
| 31 | No new procedures in service code without DATA-29. | judge |
| 32 | A procedure with a transaction starts with `SET XACT_ABORT ON` and `SET NOCOUNT ON`, and uses `TRY`, `CATCH`, and `THROW`. | judge |
| 33 | Procedure parameter types match the columns. | judge |
| 34 | `ALTER DATABASE` steps run outside a transaction (`suppressTransaction: true` in EF). | judge |

## SqlClient and Dapper

| # | Check | Type |
|---|-------|------|
| 35 | `Microsoft.Data.SqlClient`, not `System.Data.SqlClient`. | judge |
| 36 | `System.Security.Cryptography.Pkcs` 10.0.x is pinned with SqlClient 6.1 or 7.1. | tool (`dotnet build -warnaserror`, NU1902, NU1903) |
| 37 | No `AddWithValue`. String parameters for `varchar` columns are `VarChar` or `DbString { IsAnsi = true }`. | tool (SQL-ADDWITHVALUE), judge |
| 38 | Dapper calls pass the `CancellationToken` through `CommandDefinition`. | judge |
| 39 | Long lists use a table-valued parameter or `OPENJSON`, not thousands of parameters. | judge |
| 40 | `SqlBulkCopy` uses column mappings and `CheckConstraints`. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 41 | Tests run against the pinned SQL Server image, not SQLite or the EF InMemory provider. | judge (`dev-stack:dotnet-testing`) |
| 42 | A counter, a claim, or an upsert has a test with parallel calls. | judge |
| 43 | A report per local day has a test with an instant near midnight UTC. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 44 | New code that needs DATA-26, DATA-27, DATA-28, or DATA-29 has an accepted decision, or the report marks it as assumed. | judge |

## Existing code

| # | Check | Type |
|---|-------|------|
| 45 | Existing code that the change edits, calls, or copies has no defect of `SKILL.md` (First, step 5). The change fixes the defect. When the change does not touch that code, the report names the defect as an open point. | judge |
