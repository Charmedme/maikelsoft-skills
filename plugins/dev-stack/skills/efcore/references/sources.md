# Sources

Checked on 2026-10-16. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- EF Core documentation
- Provider documentation
- Checked by compile and test
- Credits

## Versions

| Item | Version on 2026-10-16 | Source |
|------|-----------------------|--------|
| EF Core | 10.0.13 (LTS, support to 2028-11-10) | [nuget.org Microsoft.EntityFrameworkCore](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore), [EF Core releases](https://learn.microsoft.com/ef/core/what-is-new/) |
| EF Core 11 | 11.0.0-rc.2.26504.105 | [What is new in EF Core 11](https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/whatsnew) |
| EF Core 8 and 9 | End of support 2026-11-10 | [EF Core releases](https://learn.microsoft.com/ef/core/what-is-new/) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | [nuget.org](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL) |
| EFCore.NamingConventions | 10.0.1 | [nuget.org](https://www.nuget.org/packages/EFCore.NamingConventions) |
| SQL Server | 2025 (17.x), compatibility level 170 | [What is new in SQL Server 2025](https://learn.microsoft.com/sql/sql-server/what-s-new-in-sql-server-2025) |
| PostgreSQL | 18.6 | [postgresql.org versions](https://www.postgresql.org/versions.json) |

## EF Core documentation

- [What is new in EF Core 10](https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/whatsnew): complex types and JSON, named query filters, `LeftJoin`, block lambda in `ExecuteUpdate`, parameterized collections.
- [Breaking changes in EF Core 10](https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/breaking-changes): `Application Name`, collection parameter mode, JSON column type.
- [Breaking changes in EF Core 11](https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/breaking-changes): checked again on 2026-10-16 for `ef11.md`.
- [DbContext lifetime, configuration, and initialization](https://learn.microsoft.com/ef/core/dbcontext-configuration/)
- [Advanced performance topics](https://learn.microsoft.com/ef/core/performance/advanced-performance-topics): pooling, compiled queries.
- [Efficient querying](https://learn.microsoft.com/ef/core/performance/efficient-querying)
- [Single and split queries](https://learn.microsoft.com/ef/core/querying/single-split-queries)
- [Pagination](https://learn.microsoft.com/ef/core/querying/pagination)
- [SQL queries](https://learn.microsoft.com/ef/core/querying/sql-queries)
- [Global query filters](https://learn.microsoft.com/ef/core/querying/filters)
- [ExecuteUpdate and ExecuteDelete](https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete)
- [Handling concurrency conflicts](https://learn.microsoft.com/ef/core/saving/concurrency)
- [Connection resiliency](https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency)
- [Complex types](https://learn.microsoft.com/ef/core/modeling/complex-types)
- [Model bulk configuration](https://learn.microsoft.com/ef/core/modeling/bulk-configuration)
- [Value conversions](https://learn.microsoft.com/ef/core/modeling/value-conversions)
- [Data seeding](https://learn.microsoft.com/ef/core/modeling/data-seeding)
- [Interceptors](https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors)
- [Applying migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying): bundles, scripts, startup migration, deployment identity.
- [Migrations in team environments](https://learn.microsoft.com/ef/core/managing-schemas/migrations/teams)
- [EF Core tools reference](https://learn.microsoft.com/ef/core/cli/dotnet): `has-pending-model-changes`, `bundle`, `script`.
- [Choosing a testing strategy](https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy): InMemory provider discouraged.
- [SQL Server value generation](https://learn.microsoft.com/ef/core/providers/sql-server/value-generation)
- [Multi-tenancy](https://learn.microsoft.com/ef/core/miscellaneous/multitenancy)
- [dotnet/efcore analyzer releases](https://github.com/dotnet/efcore): EF1002, EF1003, EF1004.

## Provider documentation

- [EFCore.PG 10 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html) and [11 release notes](https://www.npgsql.org/efcore/release-notes/11.0.html)
- [EFCore.PG value generation](https://www.npgsql.org/efcore/modeling/generated-properties.html): UUIDv7 for `Guid` keys from EFCore.PG 9.
- [EFCore.PG concurrency tokens](https://www.npgsql.org/efcore/modeling/concurrency.html): `xmin`.
- [EFCore.PG enum mapping](https://www.npgsql.org/efcore/mapping/enum.html)
- [EFCore.PG indexes](https://www.npgsql.org/efcore/modeling/indexes.html)
- [EFCore.PG collations and case sensitivity](https://www.npgsql.org/efcore/misc/collations-and-case-sensitivity.html)
- [EFCore.PG tables and naming](https://www.npgsql.org/efcore/modeling/tables.html)
- [Npgsql date and time mapping](https://www.npgsql.org/doc/types/datetime.html)
- [Npgsql 10 release notes](https://www.npgsql.org/doc/release-notes/10.0.html)
- [EFCore.BulkExtensions license](https://github.com/borisdj/EFCore.BulkExtensions) (dual license) and [ZZZ Projects license](https://zzzprojects.com/license-agreement/) (commercial).
- [dbi services: UUIDv7 in SQL Server](https://www.dbi-services.com/blog/why-uuidv7-does-not-reduce-fragmentation-in-sql-server/) (secondary source, test-based).

## Checked by compile and test

On 2026-10-16, with .NET SDK 10.0.402, EF Core 10.0.13, EFCore.PG 10.0.3, EFCore.NamingConventions 10.0.1, Testcontainers.PostgreSql 4.16.0, MSTest 4.0.2, and `postgres:18.6`:

- All C# examples compile with `dotnet build -warnaserror`.
- `GenerateCreateScript` shows `json` columns at compatibility level 170, and `jsonb` and snake_case names on PostgreSQL. It shows `numeric(19,4)` and `character(3)` for the `Money` complex type. It also shows a GIN index, a partial concurrent index, a stored generated column, a temporal table, and a filtered index with `INCLUDE`.
- `NpgsqlSequentialGuidValueGenerator` returns UUID version 7. SQL Server uses `SequentialGuidValueGenerator`.
- EF1002 and EF1003 fire on `FromSqlRaw` and `ExecuteSqlRaw` with interpolation or concatenation.
- `UseXminAsConcurrencyToken` does not exist in EFCore.PG 10.0.3.
- `dotnet ef migrations has-pending-model-changes` returns exit code 1 for a missing migration and 0 when the model matches.
- Tests against PostgreSQL 18.6: an `xmin` token gives `DbUpdateConcurrencyException` to the second writer. A nondeterministic ICU collation finds an email with another case. `MigrateAsync` applies a concurrent index. The idempotent script with the same index fails with SQLSTATE 25001.
- `ToQueryString()` throws `NullReferenceException` for `EF.Parameter(list).Contains(...)` on SQL Server (the query itself is fine). Do not use `ToQueryString` to check that form.

## Credits

- **dotnet/skills, `optimizing-ef-core-queries`** ([github.com/dotnet/skills](https://github.com/dotnet/skills/tree/main/plugins/dotnet-data/skills/optimizing-ef-core-queries)), MIT License, Copyright (c) .NET Foundation and Contributors. `queries.md`, `writes.md`, and `migrations.md` paraphrase parts of it. These are the review workflow, the rules for findings from source code only, and the tracking table. Also the `ExecuteUpdate` caveats, the paging rules, the import rules, and the rule to not apply a migration without approval. This skill copies no text.
- The github/awesome-copilot `ef-core` skill is not used. It recommends the InMemory provider and a mocked `DbSet` for tests, which this skill rejects.
