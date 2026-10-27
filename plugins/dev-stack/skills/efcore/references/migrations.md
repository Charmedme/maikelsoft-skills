# Migrations

How to add, review, merge, check, and deploy EF Core migrations.

## Contents

- Where migrations live
- Add a migration
- Review a migration
- Teams and merge conflicts
- CI checks
- Deployment
- Startup migration
- Seed data and migrations
- Zero-downtime changes

## Where migrations live

- Keep the migrations in the data project, in a `Migrations` folder. For a second context, use `-o Migrations/<Context>` and give each context its own history table (DATA-04).
- Add an `IDesignTimeDbContextFactory<T>` when the startup project cannot build the context for the tools (for example a worker without a host). The tools need no real database for `migrations add`.

```csharp
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost;Database=design;Application Name=ef-design", o => o.UseCompatibilityLevel(170))
                .Options,
            new DesignTenant());

    private sealed class DesignTenant : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
    }
}
```

- Install the tools as a local tool with the same version as EF: `dotnet tool install dotnet-ef --version 10.0.x`, recorded in `.config/dotnet-tools.json`.
- EF Core 10 tools need `--framework` for a project with `<TargetFrameworks>` (more than one target), for example `dotnet ef migrations add AddOrderDeliveryDate --framework net10.0`. Without it, the tools stop with "The project targets multiple frameworks".

## Add a migration

1. Change the model.
2. Add the migration with a name that says what changed:

   ```
   dotnet ef migrations add AddOrderDeliveryDate --project src/App.Data --startup-project src/App.Api
   ```

3. Read the generated `Up` and `Down` methods (next section).
4. Build and run the tests. The test database applies all migrations (see `testing.md`).
5. Do not apply the migration to a shared or production database. Show it to the human. `dotnet ef database update` changes the database: run it only on your local database.

Done when the build passes, `has-pending-model-changes` returns exit code 0, and you read the migration.

## Review a migration

Check each generated migration for these problems:

| Problem | Sign | Fix |
|---------|------|-----|
| Rename as drop and add | `DropColumn` and `AddColumn` for one property | Replace with `RenameColumn`, or do a data copy in expand-contract steps. |
| Data loss on a type change | `AlterColumn` to a smaller type or length | Check the data first. Add a data step. |
| New `NOT NULL` column on a full table | `AddColumn` with `nullable: false` and no default | Add a default, or add it nullable, fill it, then make it `NOT NULL`. |
| Blocking index on a large PostgreSQL table | `CreateIndex` without concurrent creation | `IsCreatedConcurrently()` in the model. |
| JSON column type change on SQL Server | `nvarchar(max)` to `json` after a raise of the compatibility level | Accept it, or keep `nvarchar(max)` with `HasColumnType`. |
| Unexpected changes | Changes in tables that you did not touch | Someone changed the model without a migration, or the snapshot is wrong. Stop and ask. |

## Teams and merge conflicts

- Migrations form one line. Two branches that each add a migration make a fork.
- Each migration changes `<Context>ModelSnapshot.cs`. A merge conflict in the snapshot means that two people changed the model.
- To fix it:
  1. Abort the merge.
  2. Remove your migration: `dotnet ef migrations remove`. Your model changes stay.
  3. Merge the other branch.
  4. Add your migration again.
- Do not rename, reorder, or edit applied migrations. Do not edit the snapshot by hand.
- EF 11 writes the ID of the latest migration in the snapshot, so Git shows a conflict even when the model changes do not overlap.

## CI checks

Run these steps in the pipeline of each pull request:

1. Check for model changes without a migration:

   ```
   dotnet ef migrations has-pending-model-changes --project src/App.Data --startup-project src/App.Api
   ```

   Exit code 0: no changes. Exit code 1: a migration is missing. Fail the build.

2. Run the integration tests. They apply all migrations to an empty container, which proves that the migrations run.
3. Optional: make the idempotent script and keep it as a build artifact, so a reviewer can read the SQL.

EF 9+ also throws at `Migrate` when the model has pending changes (`PendingModelChangesWarning`). Do not hide that warning.

## Deployment

DATA-18 default: a migration bundle that CI builds and a one-shot job runs before the new app version starts.

```
dotnet ef migrations bundle --project src/App.Data --startup-project src/App.Api --self-contained -r linux-x64 -o efbundle
./efbundle --connection "$MIGRATION_CONNECTION_STRING"
```

- The bundle needs no SDK, no source code, and no EF tools on the target.
- It uses the EF migration lock (EF 9+) and runs the seeding delegates.
- Use a deployment identity with DDL rights for the job. The app identity gets only the rights to read and write data.
- Set `ASPNETCORE_ENVIRONMENT` (or `DOTNET_ENVIRONMENT`) when you build and run the bundle. Without it, the bundle can read development settings and user secrets.

Idempotent script, when a DBA must review or approve the SQL:

```
dotnet ef migrations script --idempotent --project src/App.Data --startup-project src/App.Api -o migrate.sql
```

The script checks the history table, so it can run on a database at any migration. It does not use the migration lock and does not run seeding delegates. On PostgreSQL, a concurrent index (`IsCreatedConcurrently()`) fails in the idempotent script, because the script wraps each step in a `DO` block. Use a bundle, or a non-idempotent script for that migration. When the team must keep one idempotent script, use a pre-deploy SQL file for the concurrent index and `CREATE INDEX IF NOT EXISTS` in the migration (SKILL.md rule 32a).

Aspire: use `AddEFMigrations` for local runs and `PublishAsMigrationBundle` or `PublishAsMigrationScript` for publishing (see the `aspire` skill).

## Startup migration

`Database.MigrateAsync()` at startup is the common tutorial pattern. It has costs:

- Each replica tries to migrate. EF 9+ takes a lock, but a long migration blocks startup and health checks.
- The app identity needs DDL rights.
- Nobody reviews the SQL before it runs.

Use it only when DATA-18 accepts it (for example a single-instance internal tool). Then add a suppression with the reason:

```csharp
// stackcheck: ignore EF-STARTUP-MIGRATE single-instance internal tool, decision DATA-18
await db.Database.MigrateAsync(ct);
```

Do not call `EnsureCreated` before or instead of `Migrate`. `EnsureCreated` makes the schema without the history table, so later migrations fail.

## Seed data and migrations

- `HasData` rows become `InsertData`, `UpdateData`, and `DeleteData` operations in migrations. Give each row a fixed key. A changed value makes a new migration.
- `UseSeeding` and `UseAsyncSeeding` run after `Migrate`, `database update`, and a bundle, not after a script. Details in `modeling.md`.

## Zero-downtime changes

When old and new app versions run at the same time, use expand-contract over several deployments:

1. Expand: add the new column or table, nullable or with a default. Deploy.
2. Write both: the app writes the old and the new column. Copy the old data in a batch job or migration.
3. Read new: the app reads the new column. Deploy.
4. Contract: remove the old column in a later migration.

A rename is an add, a copy, and a drop over these steps. A single `RenameColumn` breaks the old app version while it still runs.
