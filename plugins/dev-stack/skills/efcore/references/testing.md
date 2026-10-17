# Testing EF Core code

Only the EF part. The test framework, assertions, container lifetime, and Respawn setup are in `dev-stack:dotnet-testing` (decision DATA-20).

## Contents

- Why not the InMemory provider
- Apply migrations one time
- What to test
- What not to test

## Why not the InMemory provider

Microsoft discourages the EF InMemory provider (`UseInMemoryDatabase`, package `Microsoft.EntityFrameworkCore.InMemory`) for tests:

- It is not relational. It has no SQL, no constraints, and no foreign key checks.
- It ignores transactions.
- It does not run raw SQL (`FromSql`, `ExecuteSql`) or provider functions.
- It compares strings in .NET, so case rules differ from the database.
- `ExecuteUpdate` and `ExecuteDelete` do not work.

A test with InMemory can pass while the same query fails or returns other rows on the real database. A mocked `DbSet` is worse: it runs LINQ to Objects and proves nothing about the SQL.

SQLite in-memory is real SQL, but its types, case rules, and translations differ. It does not support `json`, `jsonb`, arrays, `xmin`, `rowversion`, or collations. Use it only when the human accepts these limits.

## Apply migrations one time

Start one container with the production engine (image pinned to the production tag) for the test run. Apply the migrations one time, before the first test:

```csharp
[TestClass]
public static class PgDatabase
{
    // Pin the image tag of the production engine.
    private static readonly PostgreSqlContainer Container = new PostgreSqlBuilder("postgres:18.6").Build();

    public static PgContext CreateContext() => new(PgContext.Options(Container.GetConnectionString()));

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext _)
    {
        await Container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync(); // once per test run: this also proves that the migrations run
    }

    [AssemblyCleanup]
    public static async Task StopAsync() => await Container.DisposeAsync();
}
```

- Use `MigrateAsync`, not `EnsureCreated`. Then the tests run the same schema as production, and CI proves that each migration runs.
- Build the test context with the same provider options as the app (`SetPostgresVersion`, naming convention, compatibility level).
- Reset data between tests with Respawn, and keep `__EFMigrationsHistory` out of the reset (`dev-stack:dotnet-testing`).
- A transaction per test, rolled back at the end, is faster. It breaks when the code under test commits or uses the execution strategy.

## What to test

- Each LINQ query that is not trivial: the translation works and the result is correct.
- Concurrency: two contexts load the same row, both save, the second gets `DbUpdateConcurrencyException`.
- Constraints: unique index violations, foreign keys, cascade delete.
- Round trips of time and decimal values (UTC, precision).
- Query filters: a tenant cannot read rows of another tenant.
- Collations: the case-insensitive column finds a value with another case.

```csharp
[TestMethod]
public async Task SecondWriterGetsConcurrencyException()
{
    var number = $"INV-{Guid.NewGuid():N}";
    await using (var seed = PgDatabase.CreateContext())
    {
        seed.Invoices.Add(new Invoice
        {
            Number = number,
            Email = "a@example.com",
            IssuedAt = DateTimeOffset.UtcNow,
            DueDate = new DateOnly(2026, 12, 1),
        });
        await seed.SaveChangesAsync();
    }

    await using var first = PgDatabase.CreateContext();
    await using var second = PgDatabase.CreateContext();
    var a = await first.Invoices.SingleAsync(i => i.Number == number);
    var b = await second.Invoices.SingleAsync(i => i.Number == number);

    a.Email = "b@example.com";
    await first.SaveChangesAsync();

    b.Email = "c@example.com";
    await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
}
```

Throw on the model warnings in the test context, so that a missing precision or a cartesian `Include` fails a test:

```csharp
.ConfigureWarnings(w => w.Throw(
    SqlServerEventId.DecimalTypeDefaultWarning,
    RelationalEventId.MultipleCollectionIncludeWarning))
```

## What not to test

- EF itself: that `Add` and `SaveChanges` insert a row.
- Query behavior on a mocked `DbSet`.
- When a handler needs a test double, put a small interface in front of the query results (for example `IOrderReader`) and fake that. Do not fake `DbSet` or `DbContext`.
