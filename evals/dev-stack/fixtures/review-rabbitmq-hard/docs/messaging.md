# Messaging of the orders service

The orders service uses RabbitMQ 4.3 (run by the platform team, vhost `/`). The production broker has the exchanges and queues below. The service declares them again at start.

## Exchanges

| Exchange | Type | Owner | Content |
|----------|------|-------|---------|
| `orders.events` | topic | orders team | The events of this service |
| `warehouse.events` | topic | warehouse team | The events of the warehouse service |
| `orders.dlx` | fanout | orders team | The dead letters of the queues of this service |

## Events that we publish

All events go through the outbox. Each message has `message_id`, `correlation_id` (the order id), `type` (the wire name), `content_type` `application/json` and delivery mode 2. The body is camelCase JSON.

| Routing key | Body | Consumers |
|-------------|------|-----------|
| `order.placed.v1` | `orderId`, `customerId`, `total`, `lineCount`, `placedAt` | warehouse |
| `order.cancelled.v1` | `orderId`, `cancelledAt` | warehouse |
| `order.paid.v1` | `orderId`, `amount`, `currency`, `paidAt` | warehouse (published at once by `OrderEventsPublisher`) |

## Queues that we consume

| Queue | Binding | Handler |
|-------|---------|---------|
| `orders.stock-received` | `warehouse.events`, `stock.received.v1` | `StockReceivedConsumer` |
| `orders.payment-results` | `payments.events`, `payment.captured.v1` and `payment.failed.v1` | `PaymentResultConsumer` |

The warehouse service is a .NET service. It sends `stock.received.v1` with a PascalCase body: `ReceiptId`, `Sku`, `Quantity`, `ReceivedAt`.

## Dead letters

A message that the service cannot process goes to `orders.dlx` and from there to the queue `orders.dlq`. Operations alerts when `orders.dlq` has a message. The orders team looks at each dead letter, fixes the cause, and publishes the message again to its original exchange.
