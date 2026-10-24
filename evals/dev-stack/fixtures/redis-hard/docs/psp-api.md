# PSP API (summary)

The payment service provider (PSP) takes card payments for the webshop. The base URL is in `Payments:BaseUrl`.

## POST v1/authorizations

Request:

```json
{ "reference": "<order id, 32 hex>", "amount_minor": 1234, "currency": "EUR" }
```

Response 200:

```json
{ "id": "auth_123", "status": "authorized" }
{ "id": "auth_124", "status": "declined", "reason": "insufficient_funds" }
```

Reasons: `insufficient_funds`, `card_expired`, `suspected_fraud`, `do_not_honor`.
