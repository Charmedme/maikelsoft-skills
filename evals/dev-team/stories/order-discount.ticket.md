# Discount code on a new order (ORD-50)

**Type:** story
**Status:** spec:ready

As a customer, I want to enter a discount code when I place an order, so that I pay the lower price of a promotion.

## Context

For this release the shop has two codes:

- WELCOME10: 10 percent, valid until 2026-12-31 (to the end of that day, UTC).
- SPRING25: 25 percent, valid until 2026-05-31 (to the end of that day, UTC).

The order in a response has the field `total` today, and no fields `subtotal` and `discount`. The product owner approved a schema change with an EF Core migration.

The error contract is RFC 9457 problem details (decision API-04). An expected failure is a result value (decision API-05). No new NuGet package. Authorization is out of scope for this story. The human is also the product owner of this item.

## Acceptance criteria

1. `POST /orders` accepts an optional field `discountCode`. A valid code lowers the total of the order by the percentage of the code.
2. The discount is a percentage of the subtotal (the sum of the line totals). The discount is rounded to 2 decimals, half away from zero.
3. A code is not case-sensitive, and spaces at the start and the end do not count.
4. An unknown code or a code after its end date returns 422. The problem details body names the field `discountCode`. The order is not stored.
5. A code works only when the subtotal is 50.00 or more. A lower subtotal returns 422 with a problem details body that names the field `discountCode`.
6. The order in the response has the fields `subtotal`, `discount`, and `total`. Without a code, `discount` is 0.00 and `total` equals `subtotal`.
7. The order list (`GET /orders?customerId=...`) shows the same `subtotal`, `discount`, and `total` as the response of `POST /orders`.

## Behaviour spec

```gherkin
Rule: R1 A valid code lowers the total by its percentage of the subtotal, rounded half away from zero (AC1, AC2)

  Scenario: discount on the subtotal, rounded up from half a cent
    Given the date is 2026-11-13
    And a new order with the lines 2 x 24.99 and 1 x 0.07 (subtotal 50.05)
    When the customer places it with the code WELCOME10
    Then the reply is 201 with subtotal 50.05, discount 5.01, and total 45.04

Rule: R2 A code is not case-sensitive, and spaces at the start and the end do not count (AC3)

  Scenario: code in lower case with spaces
    Given the date is 2026-11-13
    And a new order with the line 1 x 80.00
    When the customer places it with the code " welcome10 "
    Then the reply is 201 with subtotal 80.00, discount 8.00, and total 72.00

Rule: R3 An unknown or expired code is refused and the order is not stored (AC4)

  Scenario: expired code
    Given the date is 2026-11-13
    And a new order with the line 1 x 80.00
    When the customer places it with the code SPRING25
    Then the reply is 422 with a problem details body that names the field discountCode
    And the order list of the customer has no new order

  Scenario: unknown code
    Given the date is 2026-11-13
    And a new order with the line 1 x 80.00
    When the customer places it with the code NOPE
    Then the reply is 422 with a problem details body that names the field discountCode
    And the order list of the customer has no new order

Rule: R4 A code works to the end of its last day, UTC (AC4) (limit: 2026-12-31 23:59:59 UTC for WELCOME10)

  Scenario: order below the limit
    Given the time is 2026-12-31 12:00:00 UTC
    When the customer places an order with the line 1 x 80.00 and the code WELCOME10
    Then the reply is 201 with discount 8.00 and total 72.00

  Scenario: order at the limit
    Given the time is 2026-12-31 23:59:59 UTC
    When the customer places an order with the line 1 x 80.00 and the code WELCOME10
    Then the reply is 201 with discount 8.00 and total 72.00

  Scenario: order above the limit
    Given the time is 2027-01-01 00:00:00 UTC
    When the customer places an order with the line 1 x 80.00 and the code WELCOME10
    Then the reply is 422 with a problem details body that names the field discountCode
    And the order list of the customer has no new order

Rule: R5 A code works only for a subtotal of 50.00 or more (AC5) (limit: 50.00)

  Scenario: subtotal below the limit
    Given the date is 2026-11-13
    When the customer places an order with the line 1 x 49.99 and the code WELCOME10
    Then the reply is 422 with a problem details body that names the field discountCode

  Scenario: subtotal at the limit
    Given the date is 2026-11-13
    When the customer places an order with the line 1 x 50.00 and the code WELCOME10
    Then the reply is 201 with subtotal 50.00, discount 5.00, and total 45.00

  Scenario: subtotal above the limit
    Given the date is 2026-11-13
    When the customer places an order with the line 1 x 50.01 and the code WELCOME10
    Then the reply is 201 with subtotal 50.01, discount 5.00, and total 45.01

Rule: R6 The order shows subtotal, discount, and total, also without a code (AC6)

  Scenario: no code
    Given a new order with the line 3 x 10.00
    When the customer places it without a code
    Then the reply is 201 with subtotal 30.00, discount 0.00, and total 30.00

Rule: R7 The order list shows the same subtotal, discount, and total (AC7)

  Scenario: order with a code
    Given the customer placed the order of the lines 2 x 24.99 and 1 x 0.07 with the code WELCOME10 on 2026-11-13
    When the customer gets the order list
    Then the order in the list has subtotal 50.05, discount 5.01, and total 45.04

  Scenario: order without a code
    Given the customer placed an order with the line 3 x 10.00 without a code
    When the customer gets the order list
    Then the order in the list has subtotal 30.00, discount 0.00, and total 30.00
```
