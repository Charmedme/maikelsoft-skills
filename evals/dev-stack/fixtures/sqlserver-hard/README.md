# Orders API

The order service of the webshop. It holds customers, the product catalog, stock, orders, payments, shipments and returns.

| Module | Folder | Endpoints |
|--------|--------|-----------|
| Customers | `src/Orders.Api/Customers` | `POST /customers`, `GET /customers/{id}`, `PUT /customers/{id}/email` |
| Catalog | `src/Orders.Api/Catalog` | `POST /products`, `GET /products/{sku}`, `PUT /products/{sku}/price`, `DELETE /products/{sku}` |
| Inventory | `src/Orders.Api/Inventory` | `GET /inventory/{sku}`, `POST /inventory/{sku}/receipts`, `POST /inventory/{sku}/reservations` |
| Orders | `src/Orders.Api/Orders` | `POST /orders`, `GET /orders?customerId=`, `GET /orders/{id}`, `POST /orders/{id}/cancel` |
| Payments | `src/Orders.Api/Payments` | `POST /orders/{id}/payments`, `GET /orders/{id}/payments` |
| Shipping | `src/Orders.Api/Shipping` | `POST /shipments`, `GET /shipments/{id}`, `POST /shipments/{id}/dispatch`, `POST /shipments/{id}/delivered` |
| Returns | `src/Orders.Api/Returns` | `POST /orders/{id}/returns`, `POST /returns/{id}/approve`, `POST /returns/{id}/reject` |
| Reporting | `src/Orders.Api/Reporting` | `GET /reports/sales?from=&to=` (one row per day in Amsterdam time) |

The payment service provider (PSP) API is described in `docs/psp-api.md`.

## Build and test

```bash
dotnet tool restore
dotnet build -warnaserror
dotnet test
```

The database is SQL Server 2025 (`mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04`, compatibility level 170). The integration tests start it in Docker with Testcontainers. Docker must run.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Orders.Api --output-dir Data/Migrations
```

Team decisions are in `docs/dev-team/decisions.md`.

## Database

The BI team reads the `Orders` and `OrderLines` tables directly. Do not rename their columns. The time columns of `Orders` are `datetime2` in UTC.
