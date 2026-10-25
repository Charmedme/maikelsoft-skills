# Orders API

The order service of the webshop. It holds customers, the product catalog, stock, orders, payments, shipments and returns.

| Module | Folder | Endpoints |
|--------|--------|-----------|
| Customers | `src/Orders.Api/Customers` | `POST /customers`, `GET /customers/{id}`, `PUT /customers/{id}/email` |
| Catalog | `src/Orders.Api/Catalog` | `POST /products`, `GET /products/{sku}`, `PUT /products/{sku}/price`, `DELETE /products/{sku}` |
| Inventory | `src/Orders.Api/Inventory` | `GET /inventory/{sku}`, `POST /inventory/{sku}/receipts`, `POST /inventory/{sku}/reservations` |
| Orders | `src/Orders.Api/Orders` | `POST /orders`, `GET /orders?customerId=`, `GET /orders/{id}`, `POST /orders/{id}/cancel` |
| Payments | `src/Orders.Api/Payments` | `POST /orders/{id}/payments`, `GET /orders/{id}/payments` |
| Shipping | `src/Orders.Api/Shipping` | `POST /shipments`, `GET /shipments/{id}`, `POST /shipments/{id}/dispatch` (books the parcel with the carrier gateway), `POST /shipments/{id}/delivered` |
| Returns | `src/Orders.Api/Returns` | `POST /orders/{id}/returns`, `POST /returns/{id}/approve`, `POST /returns/{id}/reject` |
| Reporting | `src/Orders.Api/Reporting` | `GET /reports/sales?from=&to=` |

The payment service provider (PSP) API is described in `docs/psp-api.md`. The carrier gateway API is described in `docs/carrier-gateway-api.md`.

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

## Telemetry

Logs, traces and metrics go to Application Insights through the Azure Monitor OpenTelemetry Distro. The connection string comes from the environment (`APPLICATIONINSIGHTS_CONNECTION_STRING`). Without it (local runs, tests), the service runs without the exporter.

The operations dashboard reads the metrics of the meter `Orders.Shipping`. Support filters the logs of a shipment on `customDimensions.ShipmentId`.
