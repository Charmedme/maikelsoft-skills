# EF Core decisions

The team decisions that EF Core code needs. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4). DATA-20 (test database) is in `dev-stack:dotnet-testing`.

## Contents

- DATA-01. Database engine for a new service
- DATA-02. Data access library: EF Core, Dapper, or both
- DATA-17. Migration tool
- DATA-03. One DbContext or one per module
- DATA-04. Schema per module
- DATA-07. Primary key type
- DATA-09. Table and column naming
- DATA-10. Date and time type
- DATA-11. Money and decimal precision
- DATA-12. Enums in the database
- DATA-05. Repository pattern on top of EF Core
- DATA-06. Unit of work
- DATA-16. Multi-tenancy model
- DATA-14. Audit columns
- DATA-15. Optimistic concurrency
- DATA-08. Strongly typed IDs
- DATA-19. Seed data
- DATA-18. Migrations deployment
- DATA-13. Soft delete
- DATA-21. Read models and projections
- DATA-22. Bulk operations

## DATA-01. Database engine for a new service

- Question: Does this new service use SQL Server 2025 or PostgreSQL 18?
- Ask when: the first new database (a new service, or a new module with its own database).
- Options: SQL Server 2025: commercial engine, free Express and Developer editions, Azure SQL as PaaS. PostgreSQL 18: open source (PostgreSQL License), strong `jsonb`, arrays, and extensions. Same engine as the rest of the product: no new engine to operate.
- Recommended: The engine that the product already runs. For a new product with no database, use PostgreSQL 18 (no license cost). It has the most users in the Stack Overflow 2025 survey. EF Core and Dapper support it fully.
- Choose another option when: a customer or hosting contract requires SQL Server or Azure SQL. The team has DBA skills only for SQL Server. You need a feature that only SQL Server has (temporal tables with EF support, SSIS, SSRS).
- Depends on: none.
- Revisit when: operations must add a second engine. PostgreSQL 19 is GA.
- Check: judge.
- Detect: DET-DATA-01.

## DATA-02. Data access library: EF Core, Dapper, or both

- Question: Do we use EF Core for all data access, Dapper for all, or EF Core for writes and Dapper for some reads?
- Ask when: the first table or the first query.
- Options: EF Core only: LINQ, change tracking, migrations, raw SQL through `FromSql` and `SqlQuery<T>`. Dapper only: hand-written SQL, no tracking, schema through DbUp. Both: EF Core for writes and simple reads, Dapper for some heavy read queries.
- Recommended: EF Core only. Use `Select` projections for reads and `SqlQuery<T>` for the few queries that LINQ cannot express. One library gives one mapping and one migration tool.
- Choose another option when: a measured hot read path or a large report is clearer or faster in SQL. Then add Dapper for those queries only, on the same connection or data source. A service has no domain writes and only runs SQL reports: Dapper only (`dapper-postgres` skill).
- Depends on: DATA-01.
- Revisit when: more than about 10 % of the queries are raw SQL. Profiling shows EF overhead on a hot path.
- Check: judge.
- Detect: DET-DATA-02.

## DATA-17. Migration tool

- Question: Which tool owns schema changes: EF Core migrations, DbUp, or FluentMigrator?
- Ask when: the first table.
- Options: EF Core migrations: C# that EF generates from the model. DbUp: plain `.sql` scripts that run one time, in order. FluentMigrator: C# migration classes, provider-neutral.
- Recommended: EF Core migrations when the service uses EF Core (DATA-02). DbUp when the service has no EF model.
- Choose another option when: a DBA team owns the schema in SQL: DbUp scripts, and EF maps to the tables.
- Depends on: DATA-02.
- Revisit when: a second tool wants to own the same database. Do not allow this.
- Check: judge.
- Detect: DET-DATA-17.

## DATA-03. One DbContext or one per module

- Question: Does the service have one DbContext, or one DbContext per module?
- Ask when: the first DbContext.
- Options: One DbContext per deployable service. One DbContext per module (bounded context) in a modular monolith, same database. A separate read-only DbContext next to the write DbContext.
- Recommended: One DbContext per deployable service. In a modular monolith, one DbContext per module. A DbContext is the unit of work for one model, so keep one model per consistency boundary.
- Choose another option when: a read replica has its own connection string: add a read-only context for it. Else use projections on the same context.
- Depends on: DATA-02.
- Revisit when: a module moves to its own service. A transaction must span two contexts (a boundary smell: use an outbox, MSG-09).
- Check: judge.
- Detect: DET-DATA-03.

## DATA-04. Schema per module

- Question: Do the tables of each module go in their own database schema?
- Ask when: the first table of a second module.
- Options: Default schema only (`dbo` or `public`). Schema per module, one database. Database per module.
- Recommended: One service with one module: the default schema. Modular monolith: a schema per module with `HasDefaultSchema("<module>")` and `MigrationsHistoryTable("__EFMigrationsHistory", "<module>")`.
- Choose another option when: a module has its own scale, backup, or compliance needs, or will become a separate service soon: database per module.
- Depends on: DATA-03.
- Revisit when: a module moves out.
- Check: judge.
- Detect: DET-DATA-04.

## DATA-07. Primary key type

- Question: Which primary key type do new tables use?
- Ask when: the first table.
- Options: `int` or `bigint` identity. `int` with a HiLo sequence. `Guid` that the EF provider generates (sequential on SQL Server, UUIDv7 on EFCore.PG 9+). Random `Guid.NewGuid()`. A natural key.
- Recommended: `Guid` keys that the EF provider generates. Do not set the value in code. The ID exists before the insert, so the outbox, idempotency keys, and search document IDs need no round trip. Also, nobody can guess IDs in URLs. Use `int` identity for small lookup tables.
- Choose another option when: large SQL Server tables where 16-byte keys in each index matter: `bigint` identity. Dapper inserts on SQL Server: `DEFAULT NEWSEQUENTIALID()` in the database, or `bigint` identity.
- Depends on: DATA-01, DATA-02.
- Revisit when: storage or index size is a measured problem.
- Check: tool (EF-GUID-KEY) for a key set in code.
- Detect: DET-DATA-07.

## DATA-09. Table and column naming

- Question: Do database names use PascalCase or snake_case?
- Ask when: the first table.
- Options: EF default: PascalCase, table name from the `DbSet` property. snake_case with `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`). An explicit `ToTable` for each entity.
- Recommended: SQL Server: the EF default (PascalCase). PostgreSQL: snake_case with `EFCore.NamingConventions`, and `DefaultTypeMap.MatchNamesWithUnderscores = true` when the service also uses Dapper. PostgreSQL folds names that have no quotes to lowercase, so PascalCase names need quotes in each hand-written query.
- Choose another option when: the plugin has no release yet for a new EF major version: use explicit `ToTable` and `HasColumnName`, or wait.
- Depends on: DATA-01.
- Revisit when: EF 11 is GA (check for a plugin release).
- Check: judge.
- Detect: DET-DATA-09.

## DATA-10. Date and time type

- Question: Which .NET type do we use for points in time, and do we add NodaTime?
- Ask when: the first date or time column.
- Options: `DateTimeOffset` for instants (SQL Server `datetimeoffset`, PostgreSQL `timestamptz` with offset zero). UTC `DateTime`. NodaTime with the Npgsql NodaTime plugin. `DateOnly` and `TimeOnly` for calendar values in each option.
- Recommended: `DateTimeOffset` for instants (always UTC on PostgreSQL), `DateOnly` and `TimeOnly` for calendar values, `TimeProvider` for the clock. Store an IANA time zone ID in its own column when the zone of the user matters.
- Choose another option when: the domain has real calendar and time zone rules (for example scheduling, local business days, or recurring events): NodaTime.
- Depends on: DATA-01.
- Revisit when: time zone bugs appear.
- Check: tool (EF-LEGACY-TIMESTAMP). Judge for local times.
- Detect: DET-DATA-10.

## DATA-11. Money and decimal precision

- Question: Which precision do decimal columns get, and how do we store money?
- Ask when: the first `decimal` property.
- Options: EF default (SQL Server `decimal(18,2)` with a warning, PostgreSQL `numeric` without precision). A convention in `ConfigureConventions` with overrides per property. Money as a complex type (`Amount` and `Currency`). Integer minor units (`long` cents).
- Recommended: `Properties<decimal>().HavePrecision(18, 4)` in `ConfigureConventions`. A `Money` complex type (`Amount` decimal(19,4), `Currency` char(3)) when there is more than one currency. A plain `decimal(19,4)` column when there is one currency. Throw on `SqlServerEventId.DecimalTypeDefaultWarning` in tests.
- Choose another option when: amounts go to a payment API that uses minor units: `long` cents at that boundary only.
- Depends on: DATA-02.
- Revisit when: a currency with 3 decimals or crypto amounts appear.
- Check: tool (`DecimalTypeDefaultWarning` set to throw).
- Detect: DET-DATA-11.

## DATA-12. Enums in the database

- Question: Do enums go to the database as numbers or as strings?
- Ask when: the first enum property.
- Options: `int` (EF default). String with a max length. PostgreSQL enum type (`MapEnum`). Lookup table with a foreign key.
- Recommended: String. Set it one time for each enum type in `ConfigureConventions` with `HaveConversion<string>()` and a max length (`IsUnicode(false)` on SQL Server). A string stays correct when someone changes the order of the enum members, and it is readable in SQL and reports.
- Choose another option when: a large table where the size of each row matters: `int` with explicit member values. A value set that changes at run time or needs more attributes: a lookup table.
- Depends on: DATA-02.
- Revisit when: someone renames an enum member (strings need a data migration).
- Check: judge.
- Detect: DET-DATA-12.

## DATA-05. Repository pattern on top of EF Core

- Question: Do handlers use the DbContext directly, or do we add repositories?
- Ask when: the first handler or service that reads or writes data.
- Options: DbContext directly in handlers. A repository per aggregate root (write-side methods only: `GetAsync`, `Add`). A generic repository (`IRepository<T>`) with specifications.
- Recommended: DbContext directly. No generic repository. The DbContext already is a repository and a unit of work, and a generic wrapper hides `Include`, projections, and tracking choices.
- Choose another option when: the write side uses DDD aggregates, and code must load and save whole aggregates only. Then add a small repository per aggregate root. Do not add a generic repository over `DbSet`.
- Depends on: DATA-02.
- Revisit when: a second persistence technology must sit behind the same interface.
- Check: judge.
- Detect: DET-DATA-05.

## DATA-06. Unit of work

- Question: Is `SaveChangesAsync` the unit of work, or do we add an `IUnitOfWork` abstraction?
- Ask when: the first command that changes more than one entity.
- Options: `SaveChangesAsync` per request or message (the scoped DbContext is the unit of work). An own `IUnitOfWork` interface. Framework transaction middleware (Wolverine `AutoApplyTransactions()`).
- Recommended: One `SaveChangesAsync` call per request or message. No `IUnitOfWork` interface. When Wolverine handles the message, use `AutoApplyTransactions()`.
- Choose another option when: a command must combine `SaveChangesAsync` with `ExecuteUpdateAsync` or raw SQL in one atomic step. Then use an explicit transaction inside the execution strategy.
- Depends on: DATA-05, MSG-04.
- Revisit when: no signal. Keep it.
- Check: judge.
- Detect: DET-DATA-06.

## DATA-16. Multi-tenancy model

- Question: Does this service serve more than one tenant, and how do we isolate their data?
- Ask when: the first table with customer data, when the product is multi-tenant.
- Options: Single tenant. Tenant column with a named query filter (shared database). Database per tenant. Schema per tenant (EF docs: not supported, not recommended).
- Recommended: Single tenant until the product needs more. Then: a tenant column with a named query filter (`"Tenant"`), a composite index that starts with the tenant column, and no DbContext pooling.
- Choose another option when: contracts or regulation require physical separation, tenants differ much in size, or a tenant needs its own restore: database per tenant.
- Depends on: DATA-03, DATA-07.
- Revisit when: one tenant dominates the load. A customer asks for a dedicated database.
- Check: judge (pooling plus a tenant field leaks data).
- Detect: DET-DATA-16.

## DATA-14. Audit columns

- Question: Do tables get audit columns (created, updated, by whom), and what fills them?
- Ask when: the first entity that users edit.
- Options: None. `CreatedAt` and `UpdatedAt` (plus `CreatedBy` and `UpdatedBy`) from a `SaveChangesInterceptor`. Full change history: Audit.EntityFramework.Core, SQL Server temporal tables, or CDC.
- Recommended: `CreatedAt` and `UpdatedAt` (`DateTimeOffset`) on edited aggregates, filled by one singleton `SaveChangesInterceptor` that reads `TimeProvider`. Add `CreatedBy` and `UpdatedBy` only when the product shows or needs them.
- Choose another option when: compliance needs a who-changed-what history: temporal tables (SQL Server) or Audit.NET.
- Depends on: DATA-10.
- Revisit when: an auditor asks for history.
- Check: judge (`ExecuteUpdate` skips the interceptor).
- Detect: DET-DATA-14.

## DATA-15. Optimistic concurrency

- Question: Do edited aggregates get a concurrency token?
- Ask when: the first update endpoint or command.
- Options: None (the last write wins). SQL Server `rowversion` (`[Timestamp] byte[]`). PostgreSQL `xmin` (`[Timestamp] uint Version`). An own version column that code increments.
- Recommended: A concurrency token on each aggregate that two users or two messages can change: `rowversion` on SQL Server, `xmin` on PostgreSQL. Map `DbUpdateConcurrencyException` to HTTP 409, or retry the message.
- Choose another option when: append-only or single-writer tables: none.
- Depends on: DATA-01.
- Revisit when: no signal. Keep it.
- Check: tool (EF-XMIN-OLD for the old API). Judge for missing tokens.
- Detect: DET-DATA-15.

## DATA-08. Strongly typed IDs

- Question: Do entity IDs use their own types (`OrderId`) or plain `Guid` and `int`?
- Ask when: the first entity with a foreign key to another entity.
- Options: Plain primitives. An own `readonly record struct OrderId(Guid Value)` with one value converter for each type in `ConfigureConventions`. A source generator: Vogen (active) or StronglyTypedId (1.0 still in beta).
- Recommended: Plain primitives. The EF docs show the pattern but say that simple code can be the better choice.
- Choose another option when: the domain has many ID types that are easy to mix up, for example several `Guid` foreign keys in one method. Then use own record structs with a converter. Vogen only when you have many such types and want generated validation and serializer support. Avoid StronglyTypedId until 1.0 is stable.
- Depends on: DATA-07.
- Revisit when: a bug from a swapped ID gets to production.
- Check: judge.
- Detect: DET-DATA-08.

## DATA-19. Seed data

- Question: How do we load reference data and demo data?
- Ask when: the first lookup table or the first request for demo data.
- Options: `HasData` in the model (becomes migration operations). `UseSeeding` and `UseAsyncSeeding` (EF 9+, run after migrations). An own seeder class at startup. SQL scripts.
- Recommended: Static reference data that is the same in each environment (countries, statuses): `HasData`. Environment data (an admin user, demo data): idempotent `UseSeeding` plus `UseAsyncSeeding`, only in the environments that need it. Test data: inside the tests only.
- Choose another option when: deployment uses SQL scripts (DATA-18): put reference data in migrations (`HasData` or `InsertData`), because seeding delegates do not run for scripts.
- Depends on: DATA-18.
- Revisit when: seed data grows large (move it to a data load job).
- Check: judge.
- Detect: DET-DATA-19.

## DATA-18. Migrations deployment

- Question: How do migrations get to the test and production databases?
- Ask when: the first deployment pipeline, or the first `Migrate` call that an agent wants to add to `Program.cs`.
- Options: Migration bundle as a one-shot job before the app rolls out. Idempotent SQL script, reviewed and run by a pipeline step or a DBA. `Database.MigrateAsync()` at app startup. Aspire `AddEFMigrations` (local) with `PublishAsMigrationBundle` or `PublishAsMigrationScript`.
- Recommended: A migration bundle that CI builds and a one-shot job runs with a DDL-capable identity. The app identity gets DML rights only. In Aspire, use `AddEFMigrations` locally and `PublishAsMigrationBundle`.
- Choose another option when: a DBA must approve the SQL: idempotent script. A single-instance internal tool: startup migration, with a comment that gives the reason.
- Depends on: DATA-17.
- Revisit when: zero-downtime deployment becomes a requirement (add expand-contract rules).
- Check: tool (EF-STARTUP-MIGRATE, EF-ENSURE-CREATED).
- Detect: DET-DATA-18.

## DATA-13. Soft delete

- Question: When a user deletes a record, do we delete the row, flag it, or move it?
- Ask when: the first delete feature.
- Options: Hard delete. A flag column with a named query filter (`HasQueryFilter("SoftDelete", ...)`) and an interceptor that turns deletes into updates. An archive or history table (SQL Server temporal tables, or an own archive table).
- Recommended: Hard delete. Decide per entity when a real need appears (undo, legal hold). Soft delete has costs. Unique indexes need the flag, and cascades do not run. `ExecuteDelete` skips the filter logic, and privacy law can require a real delete.
- Choose another option when: users need undo or a trash: a flag column with a named filter, filtered unique indexes, and an interceptor. You need a full history: temporal tables or an audit trail (DATA-14).
- Depends on: DATA-02.
- Revisit when: a restore request comes in. A GDPR erasure request comes in.
- Check: judge.
- Detect: DET-DATA-13.

## DATA-21. Read models and projections

- Question: Do reads use the write tables directly, or do we build separate read models?
- Ask when: the first list or report screen that joins several aggregates.
- Options: EF `Select` projections to DTOs on the write tables. Database views mapped as keyless entities. Separate read tables that handlers or events update. A search index (SEARCH-01).
- Recommended: Projections on the write tables. Add a view for a complex query that many places use.
- Choose another option when: a read needs data from several services, or a query is too slow after index tuning. Then use separate read tables fed by events, with a rebuild job. Full text or faceted search: SEARCH-01.
- Depends on: DATA-02.
- Revisit when: a read query misses its latency budget after index tuning.
- Check: judge.
- Detect: DET-DATA-21.

## DATA-22. Bulk operations

- Question: How do we do bulk inserts and set-based updates?
- Ask when: the first import, batch job, or update of many rows.
- Options: `ExecuteUpdateAsync` and `ExecuteDeleteAsync` plus batched `SaveChangesAsync`. Provider bulk copy: `SqlBulkCopy` (SQL Server), Npgsql binary `COPY` (PostgreSQL). EFCore.BulkExtensions (dual license). Z.EntityFramework.Extensions (commercial).
- Recommended: `ExecuteUpdateAsync` and `ExecuteDeleteAsync` for set-based changes. `SqlBulkCopy` or Npgsql `COPY` for large inserts.
- Choose another option when: you often need bulk upserts of tracked graphs and have a budget. Then use a commercial library, after a license check and the approval of the human.
- Depends on: DATA-02.
- Revisit when: an import job needs a bulk merge on both engines.
- Check: tool (EF-PKG-BULKEXT, EF-PKG-ZZZ).
- Detect: DET-DATA-22.
