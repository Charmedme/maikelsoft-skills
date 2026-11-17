# The order list accepts an empty customer id (ORD-45)

**Type:** bug
**Status:** spec:ready

The order list returns 200 with an empty list for the empty customer id. A client with a broken customer lookup does not see its error.

## Steps to reproduce

1. Call `GET /orders?customerId=00000000-0000-0000-0000-000000000000`.

Expected result: 400 Bad Request with a validation problem body that names the field `customerId`.

Actual result: 200 OK with an empty list.

## Context

`GET /orders?customerId={id}` returns at most 50 orders of the customer, newest first. The endpoint does not check the customer id. `POST /orders` already refuses an order for the empty customer id. The human is also the product owner of this item.

## Acceptance criteria

1. `GET /orders?customerId=00000000-0000-0000-0000-000000000000` returns 400 Bad Request with a validation problem body that names the field `customerId`.
2. Unchanged: a customer id that is not empty gives the same reply as today.

## Behaviour spec

```gherkin
Rule: R1 The order list refuses the empty customer id (AC1)
  Now: GET /orders?customerId=00000000-0000-0000-0000-000000000000 returns 200 OK with an empty list.

  Scenario: empty customer id
    Given the customer id 00000000-0000-0000-0000-000000000000
    When the client sends GET /orders?customerId=00000000-0000-0000-0000-000000000000
    Then the reply is 400 with a validation problem body that names the field customerId

Rule: R2 Unchanged (AC2)

  Scenario: customer with orders
    Given the customer 0199f0a2-0000-7000-8000-000000000002 has 2 orders
    When the client sends GET /orders?customerId=0199f0a2-0000-7000-8000-000000000002
    Then the reply is 200 with the 2 orders, newest first

  Scenario: customer without orders
    Given the customer 0199f0a2-0000-7000-8000-000000000003 has no orders
    When the client sends GET /orders?customerId=0199f0a2-0000-7000-8000-000000000003
    Then the reply is 200 with an empty list
```
