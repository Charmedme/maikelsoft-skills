# Cancel an order (ORD-17)

**Type:** story
**Status:** spec:ready

As a customer, I want to cancel my order within the cancel window. Then I do not pay for an order that I do not want.

## Context

The domain class `Order` has a method `Cancel`, but no endpoint calls it. The setting `Orders:CancelWindowHours` gives the cancel window: 24 hours. An order is inside the window for 24 hours after `placedAt`, and at 24 hours exactly.

The error contract is RFC 9457 problem details (decision API-04). An expected failure is a result value, not an exception (decision API-05). Authorization for the cancel is out of scope for this story. The current setup stays (no authorization), and this is an open point.

## Acceptance criteria

1. `POST /orders/{id}/cancel` cancels a placed order inside the cancel window and returns 200 with the order, status Cancelled.
2. An order that is shipped returns 409 Conflict with a problem details body.
3. An order outside the cancel window returns 409 Conflict with a problem details body that says the window is closed.
4. A second cancel of a cancelled order returns 200 and changes nothing (idempotent).
5. An unknown order id returns 404.

## Behaviour spec

```gherkin
Rule: R1 A placed order inside the cancel window is cancelled (AC1)

  Scenario: cancel soon after the order
    Given an order with status Placed, placed at 2026-10-16 09:00 UTC
    When the customer sends POST /orders/{id}/cancel at 2026-10-16 10:00 UTC
    Then the reply is 200 with the order, status Cancelled

  Scenario: the cancel is stored
    Given the customer cancelled the order at 2026-10-16 10:00 UTC
    When the customer gets the order list of the customer
    Then the order has status Cancelled

Rule: R2 The cancel window closes 24 hours after the order (AC1, AC3) (limit: 24 hours)

  Scenario: cancel below the limit
    Given an order with status Placed, placed at 2026-10-16 09:00:00 UTC
    When the customer cancels the order at 2026-10-17 08:59:59 UTC
    Then the reply is 200 with the order, status Cancelled

  Scenario: cancel at the limit
    Given an order with status Placed, placed at 2026-10-16 09:00:00 UTC
    When the customer cancels the order at 2026-10-17 09:00:00 UTC
    Then the reply is 200 with the order, status Cancelled

  Scenario: cancel above the limit
    Given an order with status Placed, placed at 2026-10-16 09:00:00 UTC
    When the customer cancels the order at 2026-10-17 09:00:01 UTC
    Then the reply is 409 with a problem details body that says the cancel window is closed
    And the order still has status Placed

Rule: R3 A shipped order cannot be cancelled (AC2)

  Scenario: cancel a shipped order
    Given an order with status Shipped, placed at 2026-10-16 09:00 UTC
    When the customer cancels the order at 2026-10-16 10:00 UTC
    Then the reply is 409 with a problem details body
    And the order still has status Shipped

Rule: R4 A second cancel returns 200 and changes nothing (AC4)

  Scenario: second cancel inside the window
    Given the customer cancelled the order at 2026-10-16 10:00 UTC
    When the customer cancels the order again at 2026-10-16 11:00 UTC
    Then the reply is 200 with the order, status Cancelled, and cancelledAt 2026-10-16 10:00 UTC

  Scenario: second cancel after the window closed
    Given an order placed at 2026-10-16 09:00 UTC that the customer cancelled at 2026-10-16 10:00 UTC
    When the customer cancels the order again at 2026-10-18 09:00 UTC
    Then the reply is 200 with the order, status Cancelled, and cancelledAt 2026-10-16 10:00 UTC

Rule: R5 An unknown order id returns 404 (AC5)

  Scenario: no order has the id
    Given no order has the id 0199f0a2-0000-7000-8000-000000000001
    When the customer sends POST /orders/0199f0a2-0000-7000-8000-000000000001/cancel
    Then the reply is 404
```
