# Orders API

A small order service. Customers place orders with order lines. An order can be cancelled within a time window.

## Build and test

```bash
dotnet tool restore
dotnet build -warnaserror
dotnet test
```

The integration tests start PostgreSQL 17.6 in Docker with Testcontainers. Docker must run.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Orders.Api --output-dir Data/Migrations
```

## Code analysis

Install the Roslynator extension (VSIX) in Visual Studio to get the Roslynator analyzers. CI runs `ci/check.sh`.

Team decisions are in `docs/dev-team/decisions.md`.
