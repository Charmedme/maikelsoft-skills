# Payment events

Owner: payments team. This service reads the events from the queue `orders-api.payment-captured`.

## payments.payment-captured

The payments service publishes this event when the PSP has captured the money of an order.

| Version | Since | Fields |
|---------|-------|--------|
| 1 | 2026-05 | `PaymentId`, `OrderId`, `Amount`, `Currency`, `CapturedAt` |
| 2 | 2026-10-20 | Version 1 plus `ShippingSpeed` (`Standard` or `Express`), which the customer picks on the payment page |

- The payments service switches its nodes to version 2 one by one. Until 2026-11-15, version 1 events can still arrive. Treat a version 1 event as `Standard`.
- The contract types are in `src/Orders.Api/Contracts/PaymentMessages.cs`. Version 1 has `IForwardsTo<PaymentCapturedV2>`.
- The payments service sends an event again when it does not get the broker confirm in time. The `PaymentId` is the same in each copy.
