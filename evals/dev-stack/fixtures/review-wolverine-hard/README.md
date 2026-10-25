# Orders API

The order service of the webshop. It holds customers, the product catalog, stock, orders, payments, shipments and returns.

| Module | Folder | Endpoints |
|--------|--------|-----------|
| Customers | `src/Orders.Api/Customers` | `POST /customers`, `GET /customers/{id}`, `PUT /customers/{id}/email` |
| Catalog | `src/Orders.Api/Catalog` | `POST /products`, `GET /products/{sku}`, `PUT /products/{sku}/price`, `DELETE /products/{sku}` |
| Inventory | `src/Orders.Api/Inventory` | `GET /inventory/{sku}`, `POST /inventory/{sku}/receipts`, `POST /inventory/{sku}/reservations` |
| Orders | `src/Orders.Api/Orders` | `POST /orders`, `GET /orders?customerId=`, `GET /orders/{id}`, `POST /orders/{id}/cancel`, `PUT /orders/{id}/delivery-address` |
| Payments | `src/Orders.Api/Payments` | `POST /orders/{id}/payments`, `GET /orders/{id}/payments` |
| Shipping | `src/Orders.Api/Shipping` | `POST /shipments`, `GET /shipments/{id}`, `POST /shipments/{id}/book`, `POST /shipments/{id}/dispatch`, `POST /shipments/{id}/delivered` |
| Returns | `src/Orders.Api/Returns` | `POST /orders/{id}/returns`, `POST /returns/{id}/approve`, `POST /returns/{id}/reject` |
| Reporting | `src/Orders.Api/Reporting` | `GET /reports/sales?from=&to=` |

## Messaging

The service uses Wolverine with the RabbitMQ transport and the PostgreSQL message store (schema `wolverine`).

| Direction | Message | Endpoint | Owner |
|-----------|---------|----------|-------|
| In | `PaymentCaptured` | queue `orders-api.payment-captured` | payments team, `docs/payment-events.md` |
| In | `ParcelDelivered` | queue `orders-api.carrier-events` | carrier adapter |
| Out | `OrderPaid`, `OrderCancelled`, `ShipmentBooked` | exchange `orders.events` | this service |

The contracts are in `src/Orders.Api/Contracts`. The parcel platform API is described in `docs/carrier-api.md`, the warehouse interface in `docs/warehouse.md`.

## Build and test

```bash
dotnet tool restore
dotnet build -warnaserror
dotnet test
```

The integration tests start PostgreSQL 18.6 in Docker with Testcontainers. Docker must run. The tests turn off the RabbitMQ transport.

## Configuration

| Key | Content |
|-----|---------|
| `ConnectionStrings:Orders` | PostgreSQL |
| `ConnectionStrings:RabbitMq` | An `amqp://` URI |
| `Carrier:BaseUrl`, `Carrier:ApiKey` | The parcel platform. The key comes from user secrets or the environment (`Carrier__ApiKey`). |
| `Payments:BaseUrl` | The PSP |

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Orders.Api --output-dir Data/Migrations
```

Team decisions are in `docs/dev-team/decisions.md`.
