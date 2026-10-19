# Orders API

A small order service. Customers place orders with order lines. An order can be cancelled within a time window.

## Build and test

```bash
dotnet tool restore
dotnet build -warnaserror
dotnet test
```

The integration tests start SQL Server 2025 (`mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04`) in Docker with Testcontainers. Docker must run.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Orders.Api --output-dir Data/Migrations
```

Team decisions are in `docs/dev-team/decisions.md`.
