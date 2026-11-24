#!/usr/bin/env python3
"""Write scenarios-api.json: S9, the hard api-design scenario (review and extend a published contract).

    python3 evals/stack-split/make_api_hard.py

S7 of scenarios.json writes a new contract, and the models do that well without the skill. S9 starts from a
published partner contract with planted defects (E1 to E5) and one decoy (D1), and asks for five additions
that touch the traps of the skill: webhook signatures, deprecation headers, rate-limit headers, a complex
search, and a concurrent partial update. Prepare and score it like the other scenarios:

    python3 evals/stack-split/prepare.py <runs> --scenarios scenarios-api.json
    python3 evals/stack-split/score.py <runs> --scenarios scenarios-api.json --judge <runs>/judge.json
"""
import json
import pathlib

HERE = pathlib.Path(__file__).resolve().parent

SEED = """openapi: 3.1.0
info:
  title: Partner Orders API
  version: 1.4.0
  description: |
    Published API for partners. About 40 partners generate their clients from this file.
servers:
  - url: https://api.example.com
paths:
  /v1/orders:
    get:
      operationId: listOrders
      summary: List the orders of the partner
      parameters:
        - name: offset
          in: query
          schema: { type: integer, minimum: 0 }
        - name: limit
          in: query
          schema: { type: integer, minimum: 1 }
      responses:
        '200':
          description: A page of orders
          content:
            application/json:
              schema:
                type: object
                properties:
                  items: { type: array, items: { $ref: '#/components/schemas/Order' } }
                  total: { type: integer }
        '400':
          $ref: '#/components/responses/Error'
    post:
      operationId: createOrder
      summary: Create an order
      parameters:
        - name: Idempotency-Key
          in: header
          required: true
          schema: { type: string, maxLength: 64 }
      requestBody:
        required: true
        content:
          application/json:
            schema: { $ref: '#/components/schemas/NewOrder' }
      responses:
        '201':
          description: Created
          headers:
            Location: { schema: { type: string } }
          content:
            application/json:
              schema: { $ref: '#/components/schemas/Order' }
        '400':
          $ref: '#/components/responses/Error'
  /v1/orders/{orderId}:
    parameters:
      - name: orderId
        in: path
        required: true
        schema: { type: string, format: uuid }
    get:
      operationId: getOrder
      summary: Get one order
      responses:
        '200':
          description: The order
          content:
            application/json:
              schema: { $ref: '#/components/schemas/Order' }
        '404':
          $ref: '#/components/responses/Error'
    put:
      operationId: replaceOrder
      summary: Replace the order
      description: Partners and our staff both change orders.
      requestBody:
        required: true
        content:
          application/json:
            schema: { $ref: '#/components/schemas/Order' }
      responses:
        '200':
          description: The changed order
          content:
            application/json:
              schema: { $ref: '#/components/schemas/Order' }
        '404':
          $ref: '#/components/responses/Error'
  /v1/orders/{orderId}/cancel:
    parameters:
      - name: orderId
        in: path
        required: true
        schema: { type: string, format: uuid }
    post:
      operationId: cancelOrder
      summary: Cancel the order
      description: Order IDs are random UUIDs, so this operation does not check which partner owns the order.
      responses:
        '200':
          description: Result of the cancel
          content:
            application/json:
              schema:
                type: object
                properties:
                  success: { type: boolean }
                  message: { type: string }
components:
  responses:
    Error:
      description: Error
      content:
        application/json:
          schema: { $ref: '#/components/schemas/ErrorResponse' }
  schemas:
    ErrorResponse:
      type: object
      properties:
        error: { type: string }
        message: { type: string }
    NewOrder:
      type: object
      required: [customerId, lines]
      properties:
        customerId: { type: string }
        lines:
          type: array
          items: { $ref: '#/components/schemas/OrderLine' }
    Order:
      type: object
      properties:
        id: { type: string, format: uuid }
        partnerId: { type: string }
        customerId: { type: string }
        status: { type: string, enum: [open, shipped, cancelled] }
        total: { type: number }
        deliveryAddress: { $ref: '#/components/schemas/Address' }
        note: { type: [string, 'null'] }
        lines:
          type: array
          items: { $ref: '#/components/schemas/OrderLine' }
    OrderLine:
      type: object
      properties:
        sku: { type: string }
        quantity: { type: integer, minimum: 1 }
    Address:
      type: object
      properties:
        street: { type: string }
        city: { type: string }
        postalCode: { type: string }
        country: { type: string }
"""

TASK = (
    "The work folder holds openapi.yaml, the published contract of our partner API. About 40 partners generate "
    "their clients from it. Do two things.\n\n"
    "1. Add these to the contract:\n"
    "   a. Partners get a webhook call when an order ships. They must be able to trust that the call comes from us.\n"
    "   b. GET /v1/orders stops on 2027-06-30. Tell the partners in the API responses. Add its replacement: a list "
    "that partners use for a nightly sync of up to 3 million orders, while new orders arrive all the time.\n"
    "   c. The gateway allows each partner 100 requests a minute. Put what a partner gets above the limit in the contract.\n"
    "   d. A search on many fields: a list of statuses, a date range, a customer, a minimum and maximum total, and a list of SKUs.\n"
    "   e. Partners must be able to change only the delivery address of an order. Our staff can change the same order at the same time.\n\n"
    "2. Review the existing contract. Write the findings in review.md, with a fix for each one."
)

KEY = (
    "Current facts. Webhooks: the top-level `webhooks:` key of OpenAPI 3.1; Standard Webhooks headers webhook-id, "
    "webhook-timestamp, webhook-signature, HMAC-SHA256 over id.timestamp.body, a tolerance window and dedupe on the "
    "id; a signature over the body only (X-Signature, X-Hub-Signature) allows a replay. End of an operation: "
    "`Deprecation: @<unix seconds>` (RFC 9745 structured date, 2027-06-30 is @1782777600), `Sunset: <HTTP date>` "
    "(RFC 8594), and `Link: <...>; rel=\"deprecation\"`, plus `deprecated: true` on the operation; "
    "`Deprecation: true` is the old draft form. Nightly sync of a large changing set: cursor paging with a "
    "`next` cursor or Link rel=next, a `limit` with a `maximum`. Rate limit: 429 with Retry-After; "
    "X-RateLimit-* and RateLimit-Limit/Remaining/Reset are old forms; draft-11 RateLimit/RateLimit-Policy only "
    "when marked as a draft. Complex search: POST /v1/orders/search documented as safe and idempotent, or the "
    "QUERY method (RFC 10008, OpenAPI 3.2) when supported; not a long GET query string. Partial update: PATCH with "
    "application/merge-patch+json; ETag on GET, If-Match required, 412 on a mismatch, 428 when missing. New "
    "operations use RFC 9457 application/problem+json with a stable code.\n\n"
    "Planted defects of the existing contract that review.md should name:\n"
    "E1. Errors use an own envelope (application/json, error and message), not RFC 9457 problem details.\n"
    "E2. The cancel operation returns 200 with success: false; it has no 4xx responses.\n"
    "E3. The cancel operation skips the object-level (owner) check; random IDs are not access control (BOLA).\n"
    "E4. The list `limit` has no maximum, and offset paging for a large changing set.\n"
    "E5. PUT replaceOrder has no ETag or If-Match, so the last write wins.\n"
    "Decoy D1: `note: { type: [string, 'null'] }` is correct OpenAPI 3.1; flagging it is a false finding. "
    "Idempotency-Key on createOrder is also correct.\n\n"
    "The contract is published to 40 partners: a fix of E1, E2, or a changed existing schema is a breaking "
    "change. A good answer does not silently break the existing operations; it names the breaking fixes as open "
    "points with a new version, or adds them in a backward-compatible way. A fix of E3 (owner check) is a "
    "security fix and may change behaviour. New operations do not copy the defects."
)

SCENARIO = {
    "S9": {
        "skill": "dev-stack/api-design",
        "current": "aspnetcore-openapi",
        "task": TASK,
        "seed": {"openapi.yaml": SEED},
        "must": {
            "problem+json for new operations": r"application/problem\+json",
            "webhooks key": r"(?m)^webhooks:",
            "webhook timestamp header": r"(?i)(?<![\w-])webhook-timestamp",
            "Deprecation as a structured date": r"@1\d{9}",
            "Sunset header": r"(?m)^\s+Sunset:",
            "Link rel deprecation": r"rel=\\?[\"']?deprecation",
            "cursor for the sync": r"(?i)cursor",
            "Retry-After": r"Retry-After",
            "search safe and idempotent": r"(?im)(safe[^\n]{0,80}idempotent|idempotent[^\n]{0,80}safe|^\s+query:)",
            "merge patch": r"merge-patch\+json",
            "428": r"['\"]?428['\"]?\s*:",
            "existing error schema kept": r"(?m)^\s{4}ErrorResponse:",
        },
        "never": {
            "X-RateLimit": r"X-RateLimit",
            "old RateLimit trio": r"RateLimit-(Limit|Remaining|Reset)\b",
            "Deprecation true": r"(?i)deprecation[\"']?\s*:\s*[\"']?true",
            "body-only signature header": r"(?i)X-(Hub-)?Signature",
            "OpenAPI 3.0 nullable": r"nullable:\s*true",
            "RFC 7807": r"7807",
        },
        "judge_key": KEY,
    }
}


def main() -> None:
    out = HERE / "scenarios-api.json"
    out.write_text(json.dumps(SCENARIO, indent=1) + "\n", encoding="utf-8")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
