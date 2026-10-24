# Payment gateway events

Owner: payments team (#team-payments). Service: `payment-gateway` (Go). Status: live since 2026-10-20.

The payment gateway captures card payments at the PSP and publishes the result of each payment.

## Exchange

`payments.events`, type `topic`, durable. The payments team declares it. Each consumer declares and binds its own queue.

| Routing key | When |
|-------------|------|
| `payment.captured.v1` | The PSP captured the money. |
| `payment.failed.v1` | The PSP declined the card, or the capture failed. |

## Message properties

| Property | Value |
|----------|-------|
| `content_type` | `application/json` |
| `type` | The routing key, for example `payment.captured.v1` |
| `message_id` | A new UUID for each publish |
| `correlation_id` | The order id |
| `delivery_mode` | 2 (persistent) |

## Delivery

The gateway publishes each result at least once. When the gateway does not get a publisher confirm in time, it publishes the same result again, with a new `message_id`. The `paymentId` identifies a payment result.

## Bodies

`payment.captured.v1`:

```json
{
  "paymentId": "pay_01JD7Q4M8ZK3VQ2N5T",
  "orderId": "0199f3c2-6b1e-7c4a-9d2f-3e8a1b5c7d90",
  "amount": 42.50,
  "currency": "EUR",
  "capturedAt": "2026-10-20T09:15:02.481Z"
}
```

`payment.failed.v1`:

```json
{
  "paymentId": "pay_01JD7Q9X2C4MKD8R1P",
  "orderId": "0199f3c2-6b1e-7c4a-9d2f-3e8a1b5c7d90",
  "amount": 42.50,
  "currency": "EUR",
  "reason": "card_declined",
  "failedAt": "2026-10-20T09:16:40.002Z"
}
```

- `paymentId`: a string of at most 64 characters.
- `amount`: a decimal number in the currency, with 2 decimals.
- `reason`: one of `card_declined`, `insufficient_funds`, `expired_card`, `fraud_suspected`, `psp_error`. New values can come.
