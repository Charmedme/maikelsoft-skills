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
| Reporting | `src/Orders.Api/Reporting` | `GET /reports/sales?from=&to=` |

The payment service provider (PSP) API is described in `docs/psp-api.md`.

## Build and test

```bash
dotnet tool restore
dotnet build -warnaserror
dotnet test
```

The integration tests start PostgreSQL 18.6 and RabbitMQ 4.3 in Docker with Testcontainers. Docker must run.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Orders.Api --output-dir Data/Migrations
```

Team decisions are in `docs/dev-team/decisions.md`.

## Messaging

The service talks to other services through RabbitMQ 4.3. The platform team runs the broker. The connection string is `RabbitMq` (an `amqp://` URI). The service uses RabbitMQ.Client 7. The code is in `src/Orders.Api/Messaging`, and `docs/messaging.md` describes the exchanges and queues.

- Events of this service go to the exchange `orders.events` through the outbox: a row in `outbox_messages` in the same `SaveChangesAsync`, sent by `OutboxPublisher`.
- Consumers derive from `RabbitMqConsumer<TMessage>`. `StockReceivedConsumer` books the goods receipts of the warehouse.
- The integration tests start RabbitMQ 4.3 in Docker with Testcontainers, next to PostgreSQL.
