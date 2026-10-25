# Warehouse interface

Owner: warehouse team.

The warehouse picks and packs an order when it gets a `PickOrder` command. The command has the carrier and the tracking number, because the warehouse prints the label of the parcel platform.

| Item | Value |
|------|-------|
| Contract | `warehouse.pick-order`, version 1 (`src/Orders.Api/Contracts/WarehouseMessages.cs`) |
| Queue | `warehouse.pick-orders` on the shared RabbitMQ broker |
| Queue settings | Quorum queue, `x-dead-letter-exchange: warehouse.dlx`. The warehouse service declares the queue and its dead-letter exchange. |
| Send | Straight to the queue (default exchange, routing key = queue name) |
| Permissions | The user `orders` can write to the queue. It cannot change or delete it. |

Send one `PickOrder` for each booked parcel. The warehouse ignores a second `PickOrder` with the same `ShipmentId`.
