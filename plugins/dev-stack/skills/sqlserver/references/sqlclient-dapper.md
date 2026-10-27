# SqlClient, Dapper, and procedures

Plain Microsoft.Data.SqlClient and Dapper code against SQL Server, the rules for a stored procedure that a DBA owns, and bulk copy. EF raw SQL (`FromSql`, `SqlQuery`) follows `dev-stack:efcore` rule 23 and the rules in `SKILL.md`.

## Contents

- Packages
- Connections
- Parameters
- Dapper
- Procedures
- Dynamic SQL
- Retries
- Bulk copy

## Packages

| Package | Version on 2026-10-27 | Note |
|---------|-----------------------|------|
| `Microsoft.Data.SqlClient` | 7.1.1, and 6.1.7 with EF Core 10.0.14 | Not `System.Data.SqlClient` (old, no new features). SqlClient 6.1.6 and later depend on `Microsoft.Identity.Client.Broker`. Without Entra ID, reference 7.1.x directly, also with EF Core 10 (EF Core 10 breaking changes). |
| `Microsoft.Data.SqlClient.Extensions.Azure` | 7.1.1 | Entra ID sign-in with SqlClient 7. SqlClient 6 has it built in. |
| `System.Security.Cryptography.Pkcs` | 10.0.14 (10.0.13 or later) | Pin it: SqlClient 6.1.7 pulls 9.0.11 and 7.1.1 pulls 9.0.18, both with NU1902 and NU1903 (standing rule, stack rule 5). |
| `Dapper` | 2.1.89 (Apache-2.0) | Ask the human before you add it (stack rule 5, DATA-02). |

```xml
<PropertyGroup>
  <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
</PropertyGroup>
<ItemGroup>
  <PackageVersion Include="System.Security.Cryptography.Pkcs" Version="10.0.14" />
</ItemGroup>
```

## Connections

- `new SqlConnection(connectionString)` and `await using`. SqlClient pools by connection string, so a new object for each operation is correct.
- Set `Application Name=<service>` in the connection string. It shows in monitoring and keeps the pool key stable. EF Core 10 adds its own `Application Name` to a connection string without one. Then EF Core and Dapper on the same string use two pools, and a `TransactionScope` over both can become a distributed transaction.
- Azure: `Authentication=Active Directory Default` (managed identity) instead of a password.
- `Encrypt=True` is the default since SqlClient 4. Use `TrustServerCertificate=True` only for a local container.

## Parameters

Type each parameter like its column (`SKILL.md` rule 10):

```csharp
await using var command = new SqlCommand("SELECT Available FROM dbo.StockLevels WHERE Sku = @sku", connection);
command.Parameters.Add("@sku", SqlDbType.VarChar, 32).Value = sku;
var available = (int?)await command.ExecuteScalarAsync(ct);
```

- `AddWithValue` guesses the type: a string becomes `nvarchar(<length of the value>)`. The column is converted, and each length makes a new plan in the plan cache.
- `decimal`: set `Precision` and `Scale`. `datetimeoffset`: `SqlDbType.DateTimeOffset`.
- A list: a table-valued parameter (`SqlDbType.Structured` with a table type), or `OPENJSON(@json)` with a JSON array. More than 2100 parameters fail.

## Dapper

- A `string` parameter is `nvarchar(4000)`. For a `varchar` column, wrap it: `new DbString { Value = sku, IsAnsi = true, Length = 32 }`, or set the type with `DynamicParameters.Add("@sku", sku, DbType.AnsiString, size: 32)`.
- `WHERE Id IN @ids` with a list works on SQL Server: Dapper expands it to `(@ids1, @ids2, ...)`. A long list makes many plans and can pass 2100 parameters. Use a table-valued parameter for long lists.
- Pass the `CancellationToken` through `CommandDefinition`.
- `QueryMultipleAsync` reads several result sets of one batch or procedure.

```csharp
var parameters = new DynamicParameters();
parameters.Add("@Sku", sku, DbType.AnsiString, size: 32);
parameters.Add("@Quantity", quantity);
parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
await connection.ExecuteAsync(new CommandDefinition(
    "dbo.PickStock", parameters, commandType: CommandType.StoredProcedure, cancellationToken: ct));
var picked = parameters.Get<int>("@Result") == 0;
```

## Procedures

New service code does not add procedures (DATA-29). When a DBA owns them, each procedure follows these rules:

```sql
CREATE OR ALTER PROCEDURE dbo.PickStock
    @Sku varchar(32),
    @Quantity int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.StockLevels
        SET Available = Available - @Quantity
        WHERE Sku = @Sku AND Available >= @Quantity;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            RETURN 1;
        END;

        INSERT dbo.StockAudit (Sku, Change, Reason, ChangedAt)
        VALUES (@Sku, -@Quantity, N'pick', SYSDATETIMEOFFSET());

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
```

- `SET XACT_ABORT ON`: a client timeout or an error then rolls back the transaction. Without it, the transaction stays open with its locks on the pooled connection.
- `SET NOCOUNT ON`: no row count messages for each statement.
- `THROW` keeps the error number. `RAISERROR` is the old form.
- Parameter types match the columns (`varchar(32)` for a `varchar(32)` column).
- Schema-qualify each object (`dbo.StockLevels`).

## Dynamic SQL

```sql
DECLARE @sort sysname = CASE @SortColumn WHEN N'Sku' THEN N'Sku' WHEN N'Available' THEN N'Available' END;
IF @sort IS NULL THROW 50001, N'Unknown sort column.', 1;

DECLARE @sql nvarchar(max) =
    N'SELECT TOP (100) Sku, Available FROM dbo.StockLevels WHERE Sku LIKE @prefix + ''%'' ORDER BY ' + QUOTENAME(@sort) + N';';
EXEC sys.sp_executesql @sql, N'@prefix varchar(32)', @prefix = @SkuPrefix;
```

- Values go in as parameters of `sp_executesql`.
- An identifier comes from an allow list and goes through `QUOTENAME`.
- `EXEC (@sql)` with a value in the text is SQL injection.

## Retries

- EF: `EnableRetryOnFailure` retries transient errors (also 1205) inside the execution strategy.
- Dapper and SqlClient: retry the whole unit of work on 1205 (and on transient connection errors). Use 3 tries with a short random delay, and a new connection and a new transaction for each try. Make the unit idempotent, or check its result before a new try.
- SqlClient has configurable retry logic (`SqlRetryLogicBaseProvider`). It retries one command or one open, not a transaction. Do not use it for a unit of work with a transaction.

## Bulk copy

```csharp
using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints, transaction)
{
    DestinationTableName = "dbo.StockStaging",
    BatchSize = 5_000,
    EnableStreaming = true,
};
bulk.ColumnMappings.Add(nameof(StockRow.Sku), "Sku");
bulk.ColumnMappings.Add(nameof(StockRow.Available), "Available");
await bulk.WriteToServerAsync(reader, ct); // a DbDataReader over the source rows
```

- Without `CheckConstraints`, SQL Server marks the foreign keys and checks of the table as not trusted.
- Map the columns by name. Without mappings, SqlBulkCopy maps by position.
- For an upsert: copy into a staging table, then one `MERGE ... WITH (HOLDLOCK)` in the same transaction.
- `TABLOCK` (option `SqlBulkCopyOptions.TableLock`) is fast for a staging table, and blocks other writers on that table.
