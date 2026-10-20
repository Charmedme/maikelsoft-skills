# Orders API

A small order service. Customers place orders with order lines. An order can be cancelled within a time window.

## Build and test

```bash
dotnet tool restore
dotnet build -warnaserror
dotnet test
```

The integration tests start PostgreSQL 18.6 in Docker with Testcontainers. Docker must run. The image `rabbitmq:4.3-management-alpine` is in the local cache.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Orders.Api --output-dir Data/Migrations
```

Team decisions are in `docs/dev-team/decisions.md`.
