# Carrier gateway API

The carrier gateway books parcels with PostNL, DHL and DPD. One API for all carriers.

Base URL: setting `CarrierGateway:BaseUrl`. Authentication: header `X-Api-Key` (setting `CarrierGateway:ApiKey`, from user secrets or the environment).

## POST v2/bookings

Request:

```json
{
  "carrier": "postnl",
  "reference": "0199f6c2a3b47c4e9d1e2f3a4b5c6d7e",
  "recipient": {
    "name": "Ann Example",
    "street": "Main Street 1",
    "postalCode": "1234 AB",
    "city": "Utrecht",
    "country": "NL",
    "phone": "+31600000000",
    "email": "ann@example.com"
  },
  "instructions": "Ring twice."
}
```

`reference` is our shipment id without dashes. The gateway sends the phone number and the email address to the carrier for the delivery notice.

### 201 Created

```json
{ "trackingNumber": "3SABCD1234567", "labelUrl": "https://labels.gateway.example.com/3SABCD1234567.pdf" }
```

### 422 Unprocessable Entity: the carrier refuses the booking

```json
{
  "code": "ADDRESS_UNDELIVERABLE",
  "message": "The carrier does not deliver to this address.",
  "recipient": {
    "name": "Ann Example",
    "street": "Main Street 1",
    "postalCode": "1234 AB",
    "city": "Utrecht",
    "country": "NL",
    "phone": "+31600000000",
    "email": "ann@example.com"
  }
}
```

`code` is one of `ADDRESS_UNDELIVERABLE`, `ADDRESS_UNKNOWN`, `POSTAL_CODE_INVALID`, `PHONE_REQUIRED`, `SERVICE_NOT_AVAILABLE`. `message` is English text for people. The gateway echoes the recipient as it understood it.

### 429, 500, 502, 503

The gateway or the carrier has a problem. Try again later. The body is not defined.
