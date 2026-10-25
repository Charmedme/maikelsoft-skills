# Sources

Checked on 2026-10-19. On 2026-10-25, a second audit checked the versions again and added the section "Checked on 2026-10-25". The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Checked on 2026-10-25
- Microsoft Learn: engine
- Microsoft Learn: T-SQL
- SqlClient and Dapper
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-25 | Source |
|------|-----------------------|--------|
| SQL Server 2025 (17.x) | CU10 17.0.5035.5 (2026-10-22), compatibility level 170. Before: CU9 17.0.5005.3, CU9 + GDR 17.0.5015.4 (2026-10-13) | [SQL Server 2025 build versions](https://learn.microsoft.com/troubleshoot/sql/releases/sqlserver-2025/build-versions) |
| Container image | `mcr.microsoft.com/mssql/server:2025-CU10-ubuntu-24.04` (tag in the MCR tag list on 2026-10-25). The runs below used `2025-CU9-ubuntu-24.04` | [mcr.microsoft.com](https://mcr.microsoft.com/product/mssql/server/about) |
| Microsoft.Data.SqlClient | 7.1.1, and 6.1.7 (the version of EF Core 10.0.13) | [nuget.org](https://www.nuget.org/packages/Microsoft.Data.SqlClient) |
| Microsoft.Data.SqlClient.Extensions.Azure | 7.1.1 | [nuget.org](https://www.nuget.org/packages/Microsoft.Data.SqlClient.Extensions.Azure) |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.13 | [nuget.org](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer) |
| System.Security.Cryptography.Pkcs | 10.0.13 (the pin) | [nuget.org](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs) |
| Dapper | 2.1.89 (Apache-2.0) | [nuget.org](https://www.nuget.org/packages/Dapper) |
| Testcontainers.MsSql | 4.16.0 | [nuget.org](https://www.nuget.org/packages/Testcontainers.MsSql) |

## Checked on 2026-10-25

| Fact | Version | Source |
|------|---------|--------|
| Latest stable packages: SqlClient 7.1.1, and 6.1.7 for EF Core 10.0.13. Extensions.Azure 7.1.1, EF Core SqlServer 10.0.13, Pkcs 10.0.13. Dapper 2.1.89, Testcontainers.MsSql 4.16.0. No change since 2026-10-19 | nuget.org | [nuget.org](https://www.nuget.org/packages/Microsoft.Data.SqlClient) |
| .NET 10 latest runtime 10.0.13 (2026-10-13), SDK 10.0.402 | .NET 10 | [releases.json](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json) |
| SQL Server 2025 CU10 is 17.0.5035.5, released 2026-10-22 | SQL Server 2025 | [Build versions](https://learn.microsoft.com/troubleshoot/sql/releases/sqlserver-2025/build-versions) |
| SQL Server sorts `uniqueidentifier` with the last 6 bytes as most significant. `SqlGuid.CompareTo` has the same order | SQL Server, .NET 10 | [Comparing GUID and uniqueidentifier values](https://learn.microsoft.com/sql/connect/ado-net/sql/compare-guid-uniqueidentifier-values), [SqlGuid.CompareTo](https://learn.microsoft.com/dotnet/api/system.data.sqltypes.sqlguid.compareto) |
| 1000 values of `Guid.CreateVersion7()` with growing timestamps: in order with `Guid` sort, not in order with `SqlGuid` sort (run with SDK 10.0.402) | .NET 10.0.13 | Run in the workspace |
| Three defects in existing code: UTC days with `AT TIME ZONE` on `datetime2`, `nvarchar(4000)` on `varchar`, and UUIDv7 keys. The skill runs fixed or reported them in 0 of 3 runs, because the rules were only for new code. `SKILL.md` now has First, step 5 | Hard-case round 1.1.0 | `docs/work/dev-stack/hard-cases/results.md` (C10, C11, J4) |

## Microsoft Learn: engine

- [Transaction locking and row versioning guide](https://learn.microsoft.com/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide): isolation levels, RCSI, lock modes, key-range locks.
- [Optimized locking](https://learn.microsoft.com/sql/relational-databases/performance/optimized-locking): TID locking, LAQ, the need for ADR and RCSI, the LAQ limits (hints, `MERGE`, `OUTPUT`), the change of behavior, defaults per platform.
- [Accelerated database recovery](https://learn.microsoft.com/sql/relational-databases/accelerated-database-recovery-concepts)
- [Deadlocks guide](https://learn.microsoft.com/sql/relational-databases/sql-server-deadlocks-guide): 1205, the deadlock graph, `system_health`, optimized locking and deadlocks.
- [ALTER DATABASE SET options](https://learn.microsoft.com/sql/t-sql/statements/alter-database-transact-sql-set-options): `READ_COMMITTED_SNAPSHOT`, `OPTIMIZED_LOCKING`, `ACCELERATED_DATABASE_RECOVERY`.
- [ALTER DATABASE compatibility level](https://learn.microsoft.com/sql/t-sql/statements/alter-database-transact-sql-compatibility-level): level 170.
- [Clustered and nonclustered indexes](https://learn.microsoft.com/sql/relational-databases/indexes/clustered-and-nonclustered-indexes-described) and the [index architecture and design guide](https://learn.microsoft.com/sql/relational-databases/sql-server-index-design-guide)
- [Create filtered indexes](https://learn.microsoft.com/sql/relational-databases/indexes/create-filtered-indexes)
- [Data type precedence](https://learn.microsoft.com/sql/t-sql/data-types/data-type-precedence-transact-sql): why `nvarchar` converts a `varchar` column.

## Microsoft Learn: T-SQL

- [Table hints](https://learn.microsoft.com/sql/t-sql/queries/hints-transact-sql-table): `NOLOCK`, `UPDLOCK`, `HOLDLOCK`, `READPAST`.
- [MERGE](https://learn.microsoft.com/sql/t-sql/statements/merge-transact-sql): `HOLDLOCK` for a concurrent upsert, hints that `MERGE` refuses.
- [AT TIME ZONE](https://learn.microsoft.com/sql/t-sql/queries/at-time-zone-transact-sql) and [sys.time_zone_info](https://learn.microsoft.com/sql/relational-databases/system-catalog-views/sys-time-zone-info-transact-sql): Windows zone names.
- [SET XACT_ABORT](https://learn.microsoft.com/sql/t-sql/statements/set-xact-abort-transact-sql)
- [sp_executesql](https://learn.microsoft.com/sql/relational-databases/system-stored-procedures/sp-executesql-transact-sql) and [QUOTENAME](https://learn.microsoft.com/sql/t-sql/functions/quotename-transact-sql)
- [Regular expression functions](https://learn.microsoft.com/sql/t-sql/functions/regular-expressions-functions-transact-sql): compatibility level 170 for `REGEXP_LIKE` and the table functions.
- [JSON data type](https://learn.microsoft.com/sql/t-sql/data-types/json-data-type)
- [NEWSEQUENTIALID](https://learn.microsoft.com/sql/t-sql/functions/newsequentialid-transact-sql)

## SqlClient and Dapper

- [Microsoft.Data.SqlClient release notes](https://github.com/dotnet/SqlClient/tree/main/release-notes)
- [SqlBulkCopy](https://learn.microsoft.com/sql/connect/ado-net/sql/bulk-copy-operations-sql-server)
- [Configurable retry logic in SqlClient](https://learn.microsoft.com/sql/connect/ado-net/configurable-retry-logic)
- [Dapper README](https://github.com/DapperLib/Dapper): `DbString`, list expansion, `DynamicParameters`.
- Background for procedures and dynamic SQL (link only, copyrighted): Erland Sommarskog, [Error and transaction handling](https://www.sommarskog.se/error_handling/Part1.html) and [The curse and blessings of dynamic SQL](https://www.sommarskog.se/dynamic_sql.html).

## Checked by compile and run

On 2026-10-19, with .NET SDK 10.0.402, EF Core 10.0.13, SqlClient 6.1.7 and 7.1.1, Dapper 2.1.89, and `mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04` (17.0.5005.3):

- A new database has `is_read_committed_snapshot_on`, `is_accelerated_database_recovery_on`, and `is_optimized_locking_on` at 0, compatibility level 170, collation `SQL_Latin1_General_CP1_CI_AS`.
- `OPTIMIZED_LOCKING = ON` without ADR fails with 12133. With ADR it works. `ALTER DATABASE` in a transaction fails with 226.
- `AT TIME ZONE 'Europe/Amsterdam'` fails with 9820. `'W. Europe Standard Time'` puts 2026-07-01 22:30Z on 2026-07-02.
- `REGEXP_LIKE` fails at level 160 (error 195) and works at 170. `REGEXP_REPLACE` works at 160. `=` on `json` fails with 13636.
- An `nvarchar(4000)` parameter on a `varchar(32)` key: a clustered index scan with `CONVERT_IMPLICIT` under `SQL_Latin1_General_CP1_CI_AS`, a range seek under `Latin1_General_100_CI_AS`. A `varchar(32)` parameter: a seek.
- `UPDATE TOP (1) ... WITH (UPDLOCK, READPAST, ROWLOCK)` gave another row to a second session while the first held its claim.
- Probe `mssql-stock` on the eval fixture: a conditional `ExecuteUpdateAsync` in a transaction in the execution strategy sold 10 of 10 units to 50 parallel orders. A tracked read and save created 50 orders for 10 units. `MERGE` without `HOLDLOCK` failed 3 of 120 parallel calls. `MERGE ... WITH (HOLDLOCK)` failed none. A check-then-insert failed 75 of 120.
- SqlClient 6.1.7 and 7.1.1 give NU1902 and NU1903 through `System.Security.Cryptography.Pkcs` until it is pinned to 10.0.13.
- The C# examples in `SKILL.md` and the references compile with `dotnet build -warnaserror` (SqlClient 7.1.1 with the Pkcs pin). The T-SQL examples (the procedure, the dynamic SQL, the upsert, the queue claim, the daily report, the blocking query) ran on the image above.

## Credits

- Microsoft Learn SQL documentation (CC BY 4.0): paraphrased, not copied.
- Brent Ozar Unlimited, First Responder Kit (MIT): `sp_BlitzLock` and `sp_BlitzIndex` are named as tools. No text is copied.
- github/awesome-copilot `sql-code-review` and `ms-sql-dba.instructions.md` (MIT): read, nothing taken.
