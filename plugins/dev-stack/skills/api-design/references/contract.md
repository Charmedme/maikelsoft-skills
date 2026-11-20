# Contract examples

OpenAPI 3.1 examples for the rules in `SKILL.md`. Each example is a part of one orders API. Copy the form, and change the names to the names of the project.

## Contents

- Shared parts
- Errors
- Lists with a cursor
- Create with retries
- Conditional update with merge patch
- Actions and object-level checks
- Long-running work
- Complex search and QUERY
- Rate limits
- Deprecation
- Webhooks
- OpenAPI 3.2 features
- Agent-ready operations

## Shared parts

```yaml
openapi: 3.1.2
info:
  title: Orders API
  version: 1.0.0
components:
  schemas:
    Problem:
      type: object
      required: [type, title, status, code]
      properties:
        type: { type: string, format: uri-reference }
        title: { type: string }
        status: { type: integer, minimum: 400, maximum: 599 }
        detail: { type: string }
        instance: { type: string, format: uri-reference }
        code: { type: string, examples: [order.not_found] }
        traceId: { type: string }
        errors:
          type: object
          additionalProperties:
            type: array
            maxItems: 20
            items: { type: string }
  responses:
    Problem:
      description: The request failed. The body says why.
      content:
        application/problem+json:
          schema: { $ref: '#/components/schemas/Problem' }
  headers:
    ETag:
      description: Strong ETag from the row version.
      schema: { type: string }
  parameters:
    IfMatch:
      name: If-Match
      in: header
      required: true
      schema: { type: string }
```

A field that clients can clear gets `type: [string, "null"]`. OpenAPI 3.1 has no `nullable`.

## Errors

- Use the shared `Problem` response for each 4xx and 5xx, with the media type `application/problem+json` (RFC 9457).
- Keep one list of `code` values, each with its `type` URI and one page that explains it. RFC 9457 section 4 defines an IANA registry for common problem types.
- 422 is for a valid request that breaks a business rule (an order that is already shipped cannot change its address). 409 is for a state conflict with another request. 400 is for a request that the server cannot read or that breaks the schema.

## Lists with a cursor

```yaml
paths:
  /customers/{customerId}/orders:
    get:
      operationId: listCustomerOrders
      summary: List the orders of a customer, newest first.
      parameters:
        - name: cursor
          in: query
          description: Opaque. Take it from `next`. It is bound to the filter and the sort order.
          schema: { type: string, maxLength: 512 }
        - name: limit
          in: query
          schema: { type: integer, minimum: 1, maximum: 100, default: 50 }
      responses:
        '200':
          description: One page of orders.
          headers:
            Link:
              description: '`<...>; rel="next"` when more orders exist (RFC 8288).'
              schema: { type: string }
          content:
            application/json:
              schema:
                type: object
                required: [items]
                properties:
                  items: { type: array, maxItems: 100, items: { $ref: '#/components/schemas/Order' } }
                  next: { type: [string, "null"], description: 'Cursor of the next page, or null at the end.' }
        '400': { $ref: '#/components/responses/Problem' }
```

The server builds the cursor from the last sort key and the ID. A client never builds or reads a cursor.

## Create with retries

Natural idempotency: the client makes the ID.

```yaml
  /orders/{orderId}:
    put:
      operationId: createOrder
      summary: Create the order with this client ID. A retry with the same body returns the same order.
      responses:
        '201': { description: Created. }
        '200': { description: The order exists with the same content. }
        '409': { $ref: '#/components/responses/Problem' }
```

With the `Idempotency-Key` header (IETF draft -08):

```yaml
  /payments:
    post:
      operationId: createPayment
      summary: Pay an order. Send the same Idempotency-Key on each retry.
      description: The server keeps a key for 24 hours. Idempotency-Key is IETF draft -08, not an RFC.
      parameters:
        - name: Idempotency-Key
          in: header
          required: true
          schema: { type: string, maxLength: 255 }
      responses:
        '201': { description: Paid. A retry with the same key and body returns this response again. }
        '400': { description: The key is missing., $ref: '#/components/responses/Problem' }
        '409': { description: The first request with this key still runs., $ref: '#/components/responses/Problem' }
        '422': { description: The key came before with another body., $ref: '#/components/responses/Problem' }
```

## Conditional update with merge patch

```yaml
  /orders/{orderId}/delivery-address:
    patch:
      operationId: changeDeliveryAddress
      summary: Change some fields of the delivery address.
      parameters:
        - $ref: '#/components/parameters/IfMatch'
      requestBody:
        required: true
        content:
          application/merge-patch+json:
            schema: { $ref: '#/components/schemas/AddressPatch' }
      responses:
        '200':
          description: Changed.
          headers: { ETag: { $ref: '#/components/headers/ETag' } }
        '412': { description: The ETag does not match. Read the order again., $ref: '#/components/responses/Problem' }
        '422': { description: The order is shipped., $ref: '#/components/responses/Problem' }
        '428': { description: If-Match is missing., $ref: '#/components/responses/Problem' }
```

In merge patch, `{ "line2": null }` removes `line2`, and a field that the body does not name stays the same. So `AddressPatch` has no `required` list, and `line2` allows `null`.

## Actions and object-level checks

```yaml
  /orders/{orderId}/cancel:
    post:
      operationId: cancelOrder
      summary: 'Cancel an order of the caller. Safe to retry: a cancelled order stays cancelled.'
      responses:
        '204': { description: Cancelled. }
        '404': { description: No order with this ID for the caller., $ref: '#/components/responses/Problem' }
        '409': { description: The order is shipped., $ref: '#/components/responses/Problem' }
```

Return 404, not 403, when the order belongs to another user, so the API does not tell that the ID exists. The check is the same as on `GET /orders/{orderId}`. The rules for the check are in `dev-stack:secure-coding`.

## Long-running work

```yaml
  /orders/exports:
    post:
      operationId: startOrderExport
      responses:
        '202':
          description: The export started.
          headers:
            Location: { description: URL of the export status., schema: { type: string } }
  /orders/exports/{exportId}:
    get:
      operationId: getOrderExport
      responses:
        '200':
          description: The state of the export.
          headers:
            Retry-After: { description: 'Seconds until the next poll, while it runs.', schema: { type: integer } }
          content:
            application/json:
              schema:
                type: object
                required: [status]
                properties:
                  status: { type: string, enum: [running, succeeded, failed] }
                  result: { type: string, format: uri, description: Link to the file when it succeeded. }
                  error: { $ref: '#/components/schemas/Problem' }
```

## Complex search and QUERY

```yaml
  /products/search:
    post:
      operationId: searchProducts
      summary: Search products with nested filters.
      description: Safe and idempotent. This operation changes nothing, and a client can retry it.
```

OpenAPI 3.2 has the `query:` operation key for the `QUERY` method (RFC 10008). Use it only after you checked that the server, the proxies, the client generator, and the lint tool support it (APD-11).

## Rate limits

```yaml
        '429':
          description: Too many requests.
          headers:
            Retry-After: { description: Seconds to wait., schema: { type: integer } }
          content:
            application/problem+json:
              schema: { $ref: '#/components/schemas/Problem' }
```

Optional, when the team accepts a draft (APD-09): `RateLimit-Policy: "default";q=100;w=60` and `RateLimit: "default";r=50;t=30` (IETF draft -11). Write "draft -11" in the header description. The fields `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`, and `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset` are old forms.

## Deprecation

```yaml
  /v1/orders:
    get:
      deprecated: true
      responses:
        '200':
          headers:
            Deprecation: { description: 'RFC 9745 structured date, for example @1767225600.', schema: { type: string } }
            Sunset: { description: 'RFC 8594 HTTP date, for example Thu, 31 Dec 2026 23:59:59 GMT.', schema: { type: string } }
            Link: { description: '<https://example.com/docs/v2-migration>; rel="deprecation"', schema: { type: string } }
```

`Deprecation: true` is an old draft form. `Warning: 299` is obsolete.

## Webhooks

```yaml
webhooks:
  orderShipped:
    post:
      summary: An order shipped. Delivered at least once. Drop a repeated webhook-id.
      parameters:
        - { name: webhook-id, in: header, required: true, schema: { type: string } }
        - { name: webhook-timestamp, in: header, required: true, schema: { type: integer }, description: Unix seconds. }
        - { name: webhook-signature, in: header, required: true, schema: { type: string }, description: 'v1,<base64 HMAC-SHA256 of id.timestamp.body>. More than one value during a secret rotation.' }
      responses:
        '204': { description: Received. }
```

The receiver rejects a timestamp more than 5 minutes from its clock. The header names are from the Standard Webhooks spec. Check them against the version that the receivers use (not verified for 1.3.0). A webhook URL that a customer registers is an outbound call: apply the SSRF rule of `dev-stack:secure-coding`.

## OpenAPI 3.2 features

Use these only with APD-08 on 3.2, and after you checked the tool chain:

- `query:` for the `QUERY` method, and `additionalOperations` for other methods.
- `in: querystring`: the full query string as one object.
- `itemSchema` for `text/event-stream` and `application/jsonl` streams.
- Tag `parent` and `kind`, and `$self`.

Spectral needs `@stoplight/spectral-formats` 1.9.0 or later for `oas3_2`. Its example rules give false positives on `dataValue` and `serializedValue` (Spectral issues 3100 and 3110).

## Agent-ready operations

Agents that call an API read only the contract. A study of 600 endpoints found that action URIs, POST for reads, loose parameters, and undocumented status codes or error shapes confuse an agent. For each operation, give:

- an `operationId` and a `summary` that says what the operation changes
- each status code, with the problem schema for each error
- a `maximum` on each number and array, and an `enum` for each closed list
- the security scheme
