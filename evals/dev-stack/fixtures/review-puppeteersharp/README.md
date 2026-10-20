# Orders API

A small order service. Customers place orders with order lines and an optional note (free text from the webshop). An order can be cancelled within a time window.

## Build and test

```bash
dotnet tool restore
dotnet build -warnaserror
dotnet test
```

The integration tests start PostgreSQL 18.6 in Docker with Testcontainers. Docker must run.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Orders.Api --output-dir Data/Migrations
```

Team decisions are in `docs/dev-team/decisions.md`.

## Container

```bash
docker build -t orders-api .
docker run -p 8080:8080 orders-api
```
