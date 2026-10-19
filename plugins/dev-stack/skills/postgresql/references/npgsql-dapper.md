# Npgsql and Dapper

Plain Npgsql and Dapper code against PostgreSQL. Use it when DATA-02 allows Dapper (for example for reports), or for code without EF. EF raw SQL (`FromSql`, `SqlQuery`) follows `dev-stack:efcore` rule 23 and the rules in `SKILL.md`.

## Contents

- Packages
- One data source
- Share the data source with EF
- Connections and commands
- Dapper mapping
- Lists and arrays
- Several result sets
- Dates and times
- Binary COPY
- Errors

## Packages

| Package | Version on 2026-10-19 | Note |
|---------|-----------------------|------|
| `Npgsql` | 10.0.3 | Comes with EFCore.PG. Reference it directly only without EF. |
| `Npgsql.DependencyInjection` | 10.0.3 | `AddNpgsqlDataSource`. |
| `Dapper` | 2.1.89 (Apache-2.0) | Ask the human before you add it (stack rule 5, DATA-02). |

## One data source

```csharp
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Orders")!,
    dataSource => dataSource.EnableParameterLogging(builder.Environment.IsDevelopment()));
```

- `AddNpgsqlDataSource` registers `NpgsqlDataSource` as a singleton, and `NpgsqlConnection` as a scoped service.
- The data source owns the pool. `new NpgsqlConnection(cs)` also uses a pool, keyed by the connection string, but the type mappings and the settings then live in each call (DATA-23).
- Keep `Maximum Pool Size` at the default (100) until a measurement says otherwise. The sum over all replicas must stay below `max_connections` of the server.

## Share the data source with EF

```csharp
var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
builder.Services.AddSingleton(dataSource);
builder.Services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(dataSource));
```

Map enums and plugins on the data source builder and in the EF options both (`dev-stack:efcore`, `references/provider-postgresql.md`).

## Connections and commands

- Open a connection for each operation with `await using`, and close it fast. The pool keeps the physical connection.
- Pass the `CancellationToken`: Dapper takes it through `CommandDefinition`.
- Use the async methods (`QueryAsync`, `ExecuteAsync`, `OpenConnectionAsync`).
- One connection runs one command at a time. Do not use `Task.WhenAll` on one connection.

```csharp
await using var connection = await dataSource.OpenConnectionAsync(ct);
var rows = await connection.QueryAsync<SkuSales>(new CommandDefinition(
    """
    SELECT l.sku, sum(l.quantity)::int AS quantity
    FROM order_lines AS l
    WHERE l.order_id = ANY(@orderIds)
    GROUP BY l.sku
    """,
    new { orderIds },
    cancellationToken: ct));
```

## Dapper mapping

- Dapper maps a column to a property or a constructor parameter by name, without case.
- snake_case columns and PascalCase properties: set `DefaultTypeMap.MatchNamesWithUnderscores = true;` one time at startup, or alias each column (`placed_at AS PlacedAt`).
- With PascalCase names in quotes (EF default), alias with quotes when needed: `o."PlacedAt" AS "PlacedAt"`.
- A positional record needs the columns in the order and the types of its constructor. `count(*)` is `bigint`: cast it (`count(*)::int`) or use `long`.
- `date` maps to `DateOnly` with Npgsql 10 and Dapper 2.1.89 (checked on 2026-10-19).

## Lists and arrays

- Npgsql sends a .NET array or list as a PostgreSQL array. Dapper sends a list as one array parameter on Npgsql, so write `= ANY(@ids)`. `IN @ids` fails with 42601 (checked on 2026-10-19).
- `NOT IN` with a list: write `<> ALL(@ids)`.
- Insert many rows in one statement: `INSERT INTO t (a, b) SELECT * FROM unnest(@a, @b)` with two arrays.

## Several result sets

```csharp
using var results = await connection.QueryMultipleAsync(new CommandDefinition(
    "SELECT * FROM orders WHERE id = @id; SELECT * FROM order_lines WHERE order_id = @id;",
    new { id },
    cancellationToken: ct));
var order = await results.ReadSingleAsync<OrderRow>();
var lines = (await results.ReadAsync<OrderLineRow>()).AsList();
```

## Dates and times

| .NET value | Sent as | Result |
|------------|---------|--------|
| `DateTimeOffset`, offset 0 | `timestamptz` | Correct |
| `DateTimeOffset`, other offset | error | Npgsql throws |
| `DateTime`, `Kind = Utc` | `timestamptz` | Correct |
| `DateTime`, `Kind = Local` or `Unspecified` | `timestamp` | PostgreSQL converts it with the session `TimeZone`, without an error |
| `DateOnly` | `date` | Correct |

So read the time from `TimeProvider.GetUtcNow()`. A `DateTime.Now` in a Dapper parameter for a `timestamptz` column gives a wrong instant when the session zone differs from the machine zone (checked on 2026-10-19: 12:00 local became 16:00Z with `Timezone=America/New_York`).

## Binary COPY

- `BeginBinaryImportAsync("COPY t (a, b) FROM STDIN (FORMAT BINARY)")`, then for each row `StartRowAsync` and one `WriteAsync` for each column, with the `NpgsqlDbType`.
- Call `CompleteAsync()` before the dispose. It returns the row count. Without it, Npgsql cancels the `COPY`: in autocommit mode no row stays and no error comes, and in a transaction the next command fails with 25P02 (both checked on 2026-10-19).
- The importer joins the open transaction of the connection.
- For an upsert: `CREATE TEMP TABLE stage (LIKE stock_levels) ON COMMIT DROP`, `COPY` into it, then `INSERT INTO stock_levels SELECT * FROM stage ON CONFLICT (sku) DO UPDATE ...`, all in one transaction.
- Text formats (`BeginTextImport`, CSV) exist. The binary format needs no escaping and is faster.

## Errors

Catch `PostgresException` and switch on `SqlState` with the constants of `PostgresErrorCodes` (`UniqueViolation`, `SerializationFailure`, `DeadlockDetected`). `IsTransient` is true for errors that a retry can fix. The common codes: 23505 (unique violation, map to 409), 23503 (foreign key), 40001 and 40P01 (retry the whole transaction), 25P02 (an earlier command of the transaction failed).
