# Provider: SQL Server and Azure SQL

The EF side of SQL Server: provider options, type mapping, and model APIs. T-SQL, indexes, isolation (RCSI), locks, and deadlocks are in `dev-stack:sqlserver`.

## Contents

- Package and versions
- Registration
- Compatibility level and JSON
- Connection string
- Retries
- Types
- Concurrency token
- Collations and case
- Indexes from the model
- Temporal tables
- Error numbers
- EF 11 changes

## Package and versions

- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.x (EF 10 uses Microsoft.Data.SqlClient 6.x).
- SQL Server 2025 (17.x) is the current version, with compatibility level 170. Azure SQL Database runs the newest engine.
- `dotnet build` with SqlClient 6.1.7 reports NU1902 and NU1903 for the transitive package `System.Security.Cryptography.Pkcs` 9.0.11 (checked 2026-10-16). Add a direct reference to the current 10.0.x version of that package, after the approval of the human (stack rule 5). EF Core SqlServer 10.0.14 still uses SqlClient 6.1.7.
- SqlClient 6.1.6 and later depend on `Microsoft.Identity.Client.Broker`, which makes the published app much larger. When the app does not use Microsoft Entra ID authentication, Microsoft recommends a direct reference to Microsoft.Data.SqlClient 7.1.x (7.1.1 on 2026-10-27). Entra ID with SqlClient 7 needs the package Microsoft.Data.SqlClient.Extensions.Azure. SqlClient 7.1.1 also pulls a vulnerable Pkcs (9.0.18 on `net10.0`), so keep the Pkcs 10.0.x reference. Ask the human before you add a package (stack rule 5).

## Registration

```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        sql.UseCompatibilityLevel(170); // SQL Server 2025: native json type
        sql.EnableRetryOnFailure();
    });
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
    options.EnableSensitiveDataLogging(env.IsDevelopment());
});
```

For Azure SQL Database, use `UseAzureSql(connectionString)`. It sets the options for the newest engine.

## Compatibility level and JSON

Set `UseCompatibilityLevel` to the level of the real database, not to the newest one. EF writes SQL for that level.

| Level | Server | What EF uses |
|-------|--------|--------------|
| 150 | SQL Server 2019 | EF 10 default |
| 160 | SQL Server 2022 | EF 11 default. `LEAST`, `GREATEST`, `JSON_PATH_EXISTS` |
| 170 | SQL Server 2025, Azure SQL | Native `json` column type. EF 11: `JSON_CONTAINS` |

- At level 170 (or with `UseAzureSql`), EF 10 maps a JSON column (complex type with `ToJson()`, primitive collections) to the `json` type.
- When you raise the level in an existing project, the next migration converts existing `nvarchar(max)` JSON columns to `json`. Read that migration. Keep `nvarchar(max)` with `HasColumnType("nvarchar(max)")` when you do not want the change.

## Connection string

- Set `Application Name=<service>`. EF 10 adds its own value when it is missing. That changes the connection pool key and the name in monitoring.
- Keep secrets out of the connection string in source code. Use managed identity (`Authentication=Active Directory Default`) on Azure.

## Retries

`EnableRetryOnFailure()` retries transient errors, also deadlock victim 1205. The default is 6 retries with a maximum delay of 30 seconds. With retries on, run each explicit transaction inside `CreateExecutionStrategy().ExecuteAsync` (see `writes.md`).

## Types

| .NET | SQL Server default | Note |
|------|--------------------|------|
| `string` | `nvarchar(max)` | Set a length (convention). `nvarchar(max)` cannot be an index key. |
| `decimal` | `decimal(18,2)` with a warning | Set the precision (DATA-11). |
| `DateTimeOffset` | `datetimeoffset` | Instants (DATA-10). |
| `DateTime` | `datetime2` | Only with `Kind = Utc` for instants. |
| `DateOnly`, `TimeOnly` | `date`, `time` | |
| `Guid` | `uniqueidentifier` | Sequential values from EF by default. |
| `enum` | `int` | String by convention (DATA-12), with `IsUnicode(false)`. |
| `bool` | `bit` | |

## Concurrency token

```csharp
[Timestamp]
public byte[] RowVersion { get; set; } = [];
```

Or a `ulong` with `IsRowVersion()`: `b.Property(t => t.Version).IsRowVersion();`. The database changes the value on each update. Send it to the client and back for HTTP updates.

## Collations and case

The column collation decides the comparison. The default database collation is usually case-insensitive (`_CI_`). So the same LINQ query can return other rows on PostgreSQL.

```csharp
modelBuilder.UseCollation("Latin1_General_100_CI_AS_SC_UTF8");          // database default
b.Property(t => t.Email).HasMaxLength(320).UseCollation("Latin1_General_100_CI_AS"); // one column
```

Use `EF.Functions.Collate(column, "...")` in a query only when needed. A collation in the query can stop the use of an index with another collation.

## Indexes from the model

```csharp
b.HasIndex(t => t.Title).HasFilter("[IsOpen] = 1").IncludeProperties(t => t.Email);
```

`HasFilter` makes a filtered index. `IncludeProperties` adds covering columns. In a project that references the SQL Server and the PostgreSQL provider, `IncludeProperties` is ambiguous: call `SqlServerIndexBuilderExtensions.IncludeProperties(index, t => t.Email)`. Write the filter in T-SQL with the column names of the database. Index design rules are in `dev-stack:sqlserver`.

## Temporal tables

```csharp
b.ToTable(t => t.IsTemporal());
```

EF creates the period columns and the history table. Query history with `TemporalAll()`, `TemporalAsOf(time)`, and the other `Temporal*` methods. EF 11 can map the period columns to CLR properties.

## Error numbers

| Number | Meaning | Action |
|--------|---------|--------|
| 2601, 2627 | Unique index or key violation | Return 409 or a validation error. |
| 547 | Foreign key or check constraint | Return 409 or 422. |
| 1205 | Deadlock victim | The retry strategy retries it. Repeated deadlocks: see `dev-stack:sqlserver`. |

## EF 11 changes

Only when the project uses EF Core 11 (see `ef11.md`). SqlClient 7 removes the Azure authentication code: add `Microsoft.Data.SqlClient.Extensions.Azure` for Entra ID. The default compatibility level is 160.
