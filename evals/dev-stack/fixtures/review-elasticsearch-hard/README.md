# Orders API

The order service of the webshop. It holds customers, the product catalog, stock, orders, payments, shipments and returns.

| Module | Folder | Endpoints |
|--------|--------|-----------|
| Customers | `src/Orders.Api/Customers` | `POST /customers`, `GET /customers/{id}`, `PUT /customers/{id}/email` |
| Catalog | `src/Orders.Api/Catalog` | `POST /products`, `GET /products/{sku}`, `PUT /products/{sku}/price`, `DELETE /products/{sku}` |
| Search | `src/Orders.Api/Search` | `GET /products/search?q=&brand=&category=&page=` |
| Inventory | `src/Orders.Api/Inventory` | `GET /inventory/{sku}`, `POST /inventory/{sku}/receipts`, `POST /inventory/{sku}/reservations` |
| Orders | `src/Orders.Api/Orders` | `POST /orders`, `GET /orders?customerId=`, `GET /orders/{id}`, `POST /orders/{id}/cancel` |
| Payments | `src/Orders.Api/Payments` | `POST /orders/{id}/payments`, `GET /orders/{id}/payments` |
| Shipping | `src/Orders.Api/Shipping` | `POST /shipments`, `GET /shipments/{id}`, `POST /shipments/{id}/dispatch`, `POST /shipments/{id}/delivered` |
| Returns | `src/Orders.Api/Returns` | `POST /orders/{id}/returns`, `POST /returns/{id}/approve`, `POST /returns/{id}/reject` |
| Reporting | `src/Orders.Api/Reporting` | `GET /reports/sales?from=&to=` |

The payment service provider (PSP) API is described in `docs/psp-api.md`.

## Build and test

```bash
dotnet tool restore
dotnet build -warnaserror
dotnet test
```

The integration tests start PostgreSQL 18.6 and Elasticsearch 9.5.5 in Docker with Testcontainers. Docker must run. `compose.yml` starts the same versions for local development.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Orders.Api --output-dir Data/Migrations
```

Team decisions are in `docs/dev-team/decisions.md`.

## Product search

Shoppers search the catalog through `GET /products/search`. The search runs on the Elasticsearch cluster of the platform team (section `Search`, the API key comes from user secrets or `Search__ApiKey`). Each product change writes a row to `product_search_outbox` in the same transaction, and `ProductIndexWorker` indexes the current state of the product. The search shows 20 products per page.

## Mail

The service sends mail through the internal SMTP relay. The settings are in section `Smtp`. The password comes from user secrets or the environment (`Smtp__Password`). Integration tests that need a real relay use a local relay (Mailpit or a small fake server) on `127.0.0.1` with `Smtp:Security` set to `None`.
