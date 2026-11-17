# Read one order (ORD-12)

**Type:** story
**Status:** spec:ready

As a customer, I want to see one of my orders, so that I can check its lines and its total.

## Context

The Orders API has `POST /orders` and `GET /orders?customerId={id}` today. No endpoint returns one order. The order in a response has the fields `id`, `customerId`, `status`, `placedAt`, `cancelledAt`, `total`, and `lines`. Each line has `sku`, `quantity`, `unitPrice`, and `lineTotal`.

The error contract is RFC 9457 problem details (decision API-04). Authorization is out of scope for this story. The current setup stays (no authorization), and this is an open point.

## Acceptance criteria

1. `GET /orders/{id}` returns 200 with the order, its lines, and its total.
2. An unknown order id returns 404.

## Behaviour spec

```gherkin
Rule: R1 A known order returns with its lines and its total (AC1)

  Scenario: order with two lines
    Given the customer placed an order with the lines 2 x SKU-1 at 2.50 and 1 x SKU-2 at 1.00
    When the customer sends GET /orders/{id} with the id of that order
    Then the reply is 200 with the order, status Placed, and total 6.00
    And the order has the line SKU-1 with quantity 2, unitPrice 2.50, and lineTotal 5.00
    And the order has the line SKU-2 with quantity 1, unitPrice 1.00, and lineTotal 1.00

  Scenario: order with one line
    Given the customer placed an order with the line 3 x SKU-3 at 10.00
    When the customer sends GET /orders/{id} with the id of that order
    Then the reply is 200 with the order, 1 line, and total 30.00

Rule: R2 An unknown order id returns 404 (AC2)

  Scenario: no order has the id
    Given no order has the id 0199f0a2-0000-7000-8000-000000000001
    When the customer sends GET /orders/0199f0a2-0000-7000-8000-000000000001
    Then the reply is 404 with a problem details body
```
