# Orders API

The order service of the web shop. Customers place orders, the warehouse picks and ships them, and finance gets the sales reports and the invoices.

| Module | Content |
|--------|---------|
| Orders | Place, read, list, and cancel orders |
| Customers, Products | Master data |
| Stock | Stock levels and the reservation at order time |
| Coupons | Discount codes with a limited number of uses |
| Picking | The next order for a warehouse worker |
| Shipping | Shipments with the carrier and the tracking number |
| Returns | Return requests, and the job that expires old ones |
| Invoices | One invoice for each shipped order, with a gapless number |
| Reports | Sales reports for finance |

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

The deploy runs an EF migration bundle against production, then restarts the three instances one by one. There is no maintenance window.

## Production

- The shop and the warehouse are in the Netherlands. A day or a week in a report is a Dutch calendar day or week (Monday to Sunday).
- Size on 2026-10-01: `orders` 41 million rows, `order_lines` 152 million rows, `shipments` 38 million rows. The shop takes orders day and night, and the warehouse picks from 06:00 to 23:00.
- Up to 25 pickers ask for the next order at the same time at the start of a shift.

Team decisions are in `docs/dev-team/decisions.md`.
