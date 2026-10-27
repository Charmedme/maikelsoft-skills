# Sources

Checked on 2026-10-19. Versions and the facts below "Checked on 2026-10-25" checked again on 2026-10-25. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- PostgreSQL documentation
- Npgsql and Dapper
- Tools
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-27 | Source |
|------|-----------------------|--------|
| PostgreSQL | 18.6 (2026-08-13), supported to 2030-11-14 | [postgresql.org versions](https://www.postgresql.org/versions.json) |
| PostgreSQL 19 | RC 1 (2026-10-15), not GA | [postgresql.org](https://www.postgresql.org/) |
| Npgsql | 10.0.3 | [nuget.org](https://www.nuget.org/packages/Npgsql) |
| Npgsql.DependencyInjection | 10.0.3 | [nuget.org](https://www.nuget.org/packages/Npgsql.DependencyInjection) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3, and 11.0.0-rc.2 | [nuget.org](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL) |
| Dapper | 2.1.89 (Apache-2.0) | [nuget.org](https://www.nuget.org/packages/Dapper) |
| .NET 10 runtime | 10.0.14 (SDK 10.0.403, a security release of 2026-10-25) | [nuget.org](https://www.nuget.org/packages/Microsoft.NETCore.App.Ref) |
| Testcontainers.PostgreSql | 4.16.0 | [nuget.org](https://www.nuget.org/packages/Testcontainers.PostgreSql) |
| squawk-cli | 2.70.0 (Apache-2.0 OR MIT) | [npmjs.com](https://www.npmjs.com/package/squawk-cli) |

## PostgreSQL documentation

- [Transaction isolation](https://www.postgresql.org/docs/18/transaction-iso.html): `READ COMMITTED`, `REPEATABLE READ`, `SERIALIZABLE`, 40001, the note on `MERGE` and unique violations.
- [Explicit locking](https://www.postgresql.org/docs/18/explicit-locking.html): lock modes, `ACCESS EXCLUSIVE`, advisory locks, deadlocks.
- [SELECT, the locking clause](https://www.postgresql.org/docs/18/sql-select.html#SQL-FOR-UPDATE-SHARE): `FOR UPDATE`, `SKIP LOCKED`, `NOWAIT`.
- [INSERT ON CONFLICT](https://www.postgresql.org/docs/18/sql-insert.html#SQL-ON-CONFLICT)
- [CREATE INDEX, building indexes concurrently](https://www.postgresql.org/docs/18/sql-createindex.html#SQL-CREATEINDEX-CONCURRENTLY): no transaction block, INVALID index after a failure.
- [ALTER TABLE](https://www.postgresql.org/docs/18/sql-altertable.html): `NOT VALID`, `VALIDATE CONSTRAINT`, table rewrite for a volatile default, `SET NOT NULL` with a valid check.
- [Client connection defaults](https://www.postgresql.org/docs/18/runtime-config-client.html): `lock_timeout`, `statement_timeout`, `idle_in_transaction_session_timeout`, `TimeZone`.
- [Date and time types](https://www.postgresql.org/docs/18/datatype-datetime.html) and [AT TIME ZONE](https://www.postgresql.org/docs/18/functions-datetime.html#FUNCTIONS-DATETIME-ZONECONVERT)
- [Identifiers and key words](https://www.postgresql.org/docs/18/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS): names without quotes fold to lowercase.
- [UUID functions](https://www.postgresql.org/docs/18/functions-uuid.html): `uuidv7()` (18).
- [Collations, nondeterministic collations](https://www.postgresql.org/docs/18/collation.html#COLLATION-NONDETERMINISTIC)
- [Index types](https://www.postgresql.org/docs/18/indexes-types.html), [partial indexes](https://www.postgresql.org/docs/18/indexes-partial.html), [multicolumn indexes](https://www.postgresql.org/docs/18/indexes-multicolumn.html)
- [Using EXPLAIN](https://www.postgresql.org/docs/18/using-explain.html)
- [Appendix A, error codes](https://www.postgresql.org/docs/18/errcodes-appendix.html)
- [PostgreSQL wiki: Don't Do This](https://wiki.postgresql.org/wiki/Don%27t_Do_This): `NOT IN`, `money`, `char(n)`, `timestamp` without zone, `serial`.

## Npgsql and Dapper

- [Npgsql basic usage and data sources](https://www.npgsql.org/doc/basic-usage.html)
- [Npgsql dependency injection](https://www.npgsql.org/doc/basic-usage.html#dependency-injection)
- [Npgsql date and time handling](https://www.npgsql.org/doc/types/datetime.html)
- [Npgsql COPY](https://www.npgsql.org/doc/copy.html)
- [Npgsql connection string parameters](https://www.npgsql.org/doc/connection-string-parameters.html): `Maximum Pool Size`, `Timezone`, `No Reset On Close`.
- [Npgsql 10 release notes](https://www.npgsql.org/doc/release-notes/10.0.html)
- [Dapper README](https://github.com/DapperLib/Dapper): list parameters, `CommandDefinition`, `QueryMultiple`. Dapper source `FeatureSupport.cs`: arrays on Npgsql.

## Checked on 2026-10-25 and 2026-10-27

| Fact | Version | Source |
|------|---------|--------|
| `date_trunc` on a `timestamptz` truncates in the session `TimeZone`. The optional third argument names another zone. | PostgreSQL 18 | [Date/time functions, date_trunc](https://www.postgresql.org/docs/18/functions-datetime.html#FUNCTIONS-DATETIME-TRUNC) |
| `ALTER TABLE` takes an `ACCESS EXCLUSIVE` lock unless the form says other. `VALIDATE CONSTRAINT` takes only `SHARE UPDATE EXCLUSIVE`. | PostgreSQL 18 | [ALTER TABLE](https://www.postgresql.org/docs/18/sql-altertable.html) |
| `lock_timeout` stops a statement that waits for a lock longer than the limit. It does not limit how long a statement holds a lock. | PostgreSQL 18 | [Client connection defaults](https://www.postgresql.org/docs/18/runtime-config-client.html#GUC-LOCK-TIMEOUT) |
| No new GA major: PostgreSQL 19 is RC 1. The newest minor is 18.6. | 2026-10-27 | [versions.json](https://www.postgresql.org/versions.json) |
| Rule `PG-LOCAL-DATE-NO-ZONE` finds `o."PlacedAt"::date` and `date_trunc('month', o."PlacedAt")` in the review fixture, and nothing in the good selftest overlay. | stackcheck | run on 2026-10-25 |

## Tools

- [Squawk rules](https://squawkhq.com/docs/rules): rule names checked on 2026-10-19 and on 2026-10-27. The type rule is `prefer-timestamptz` (the skill had `prefer-timestamp-tz` before 2026-10-27).

## Checked by compile and run

On 2026-10-19, with .NET SDK 10.0.402, Npgsql 10.0.3, EFCore.PG 10.0.3, and Dapper 2.1.89:

- On PostgreSQL 16.15 (concept lab): unquoted PascalCase names fail with 42703. `FromSql("SELECT * ...")` on an entity with `xmin` fails with 42703, and `SELECT o.*, o.xmin` works. `SqlQuery` over `UPDATE ... RETURNING` works with `ToListAsync` and fails with `SingleOrDefaultAsync`. `SKIP LOCKED` in autocommit gave one row to two connections. In a transaction it did not. 40001 has `IsTransient = true`. `CREATE INDEX CONCURRENTLY` in a transaction fails with 25001.
- On `postgres:18.6-alpine` (eval fixtures): Dapper `IN @ids` fails with 42601. `NOT IN` with a NULL in the subquery returns no rows. A binary `COPY` without `CompleteAsync` in a transaction makes the next command fail with 25P02. A local `DateTime` parameter with `Timezone=America/New_York` was stored 4 hours later. `date` maps to `DateOnly` in Dapper.
- Probe `pg-picking`: the one-statement claim with `SKIP LOCKED` gave 10 orders to 20 parallel workers, each order once. A LINQ read and save gave 20 picks for 10 orders. `AT TIME ZONE 'Europe/Amsterdam'` put an order at 22:30Z on the next day. `::date` did not.
- Squawk 2.70.0 found the lock risks of the review fixture SQL file.
- All C# examples in `SKILL.md` and `references/npgsql-dapper.md` compile with `dotnet build -warnaserror`. The SQL examples of `SKILL.md` and the references ran on `postgres:18.6-alpine`.

## Credits

- The PostgreSQL documentation and the PostgreSQL wiki (PostgreSQL License): paraphrased, not copied.
- The Npgsql documentation (github.com/npgsql/doc, PostgreSQL License): paraphrased.
- The Squawk rule names (Apache-2.0 OR MIT) are the checklist for migrations. No text is copied.
- github/awesome-copilot `postgresql-code-review` (MIT): read, nothing taken. It is not version-aware.
