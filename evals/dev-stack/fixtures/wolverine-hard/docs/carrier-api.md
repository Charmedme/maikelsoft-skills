# Parcel platform API

The parcel platform books parcels with PostNL, DHL and DPD for us. Base URL: setting `Carrier:BaseUrl`. Each request has the header `X-Api-Key`.

## POST v2/bookings

Request headers: `Idempotency-Key` (our reference, at most 64 characters). A request with a key that the platform knows returns the same booking (status 200), for 24 hours.

```json
{
  "reference": "0199f3a2c6e47d1b9a3f5e2d7c8b6a41",
  "recipient": { "name": "Ann Example", "street": "Hofstraat 1", "postalCode": "7271 AB", "city": "Borculo", "country": "NL" },
  "service": "standard"
}
```

`service` is `standard` or `express`.

| Status | Body | Meaning |
|--------|------|---------|
| 201, 200 | `{ "bookingId": "...", "carrier": "dhl", "trackingNumber": "JVGL0000012345" }` | Booked |
| 422 | `{ "code": "address_rejected", "detail": "..." }` | The carrier cannot deliver to this address. The same request gives the same answer. |
| 429 | - | Too many requests. Header `Retry-After`. |
| 500, 502, 503 | - | The platform or the carrier is not available. Outages of a few minutes happen several times a month. |
