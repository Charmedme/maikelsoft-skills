# Ship an order and show its history (ORD-40)

**Type:** story
**Status:** spec:ready

As a warehouse clerk, I want to ship an order with a carrier and a tracking code. As a customer, I want to see what happened to my order.

## Context

The domain class `Order` has a method `MarkShipped`, but no endpoint calls it. The order has no carrier, no tracking code, and no events today. `GET /orders?customerId={id}` returns at most 50 orders of the customer, newest first.

The error contract is RFC 9457 problem details (decision API-04). An expected failure is a result value, not an exception (decision API-05). No new NuGet package. Authorization is out of scope for this story. The current setup stays, and this is an open point.

## Acceptance criteria

1. `POST /orders/{id}/ship` with the body `{ "carrier": "DHL", "trackingCode": "JD014600006751" }` ships a placed order. It returns 200 with the order, status Shipped, and the fields `carrier` and `trackingCode`.
2. The carrier is one of PostNL, DHL, or UPS. The tracking code has 8 to 30 letters or digits. Other values return 400 with validation problem details.
3. An order that is not placed (for example an order that is already shipped) cannot ship. The ship request returns 409 with problem details.
4. `GET /orders/{id}/history` returns the events of the order, oldest first. Each event has the fields `type` (Placed or Shipped) and `occurredAt` (UTC).
5. `GET /orders?customerId={id}&status=Shipped` returns only the orders of that customer with that status. Without status, the list stays as it is today.
6. An unknown order id returns 404 on ship and on history.

## Behaviour spec

```gherkin
Rule: R1 A placed order ships with its carrier and tracking code (AC1)

  Scenario: ship with DHL
    Given an order with status Placed
    When the clerk ships the order with the carrier DHL and the tracking code JD014600006751
    Then the reply is 200 with the order, status Shipped, carrier DHL, and trackingCode JD014600006751

Rule: R2 The carrier is PostNL, DHL, or UPS, and the tracking code has only letters or digits (AC2)

  Scenario: carrier UPS
    Given an order with status Placed
    When the clerk ships the order with the carrier UPS and the tracking code 1Z999AA10123456784
    Then the reply is 200 with the order, status Shipped, and carrier UPS

  Scenario: unknown carrier
    Given an order with status Placed
    When the clerk ships the order with the carrier FedEx and the tracking code JD014600006751
    Then the reply is 400 with validation problem details for carrier
    And the order still has status Placed

  Scenario: tracking code with a sign that is not a letter or a digit
    Given an order with status Placed
    When the clerk ships the order with the carrier DHL and the tracking code JD0146-00006751
    Then the reply is 400 with validation problem details for trackingCode

Rule: R3 The tracking code has 8 to 30 characters (AC2) (limit: 8 to 30 characters)

  Scenario: tracking code below the limit of 8
    Given an order with status Placed
    When the clerk ships the order with the carrier PostNL and the tracking code ABC1234 (7 characters)
    Then the reply is 400 with validation problem details for trackingCode

  Scenario: tracking code at the limit of 8
    Given an order with status Placed
    When the clerk ships the order with the carrier PostNL and the tracking code ABC12345 (8 characters)
    Then the reply is 200 with the order, status Shipped

  Scenario: tracking code at the limit of 30
    Given an order with status Placed
    When the clerk ships the order with the carrier PostNL and a tracking code of 30 letters and digits
    Then the reply is 200 with the order, status Shipped

  Scenario: tracking code above the limit of 30
    Given an order with status Placed
    When the clerk ships the order with the carrier PostNL and a tracking code of 31 letters and digits
    Then the reply is 400 with validation problem details for trackingCode

Rule: R4 Only a placed order can ship (AC3)

  Scenario: ship an order that is already shipped
    Given an order with status Shipped
    When the clerk ships the order with the carrier DHL and the tracking code JD014600006751
    Then the reply is 409 with a problem details body

  Scenario: ship a cancelled order
    Given an order with status Cancelled
    When the clerk ships the order with the carrier DHL and the tracking code JD014600006751
    Then the reply is 409 with a problem details body
    And the order still has status Cancelled

Rule: R5 The history shows the events of the order, oldest first (AC4)

  Scenario: placed order
    Given an order placed at 2026-10-16 09:00:00 UTC
    When the customer sends GET /orders/{id}/history
    Then the reply is 200 with 1 event: type Placed, occurredAt 2026-10-16T09:00:00Z

  Scenario: shipped order
    Given an order placed at 2026-10-16 09:00:00 UTC and shipped at 2026-10-17 14:30:00 UTC
    When the customer sends GET /orders/{id}/history
    Then the reply is 200 with 2 events in this order: Placed at 2026-10-16T09:00:00Z, Shipped at 2026-10-17T14:30:00Z

Rule: R6 The order list can filter on status (AC5)

  Scenario: filter on Shipped
    Given the customer has an order with status Placed and an order with status Shipped
    When the customer sends GET /orders?customerId={id}&status=Shipped
    Then the reply is 200 with only the order with status Shipped

  Scenario: the filter shows no orders of another customer
    Given another customer has an order with status Shipped
    When the customer sends GET /orders?customerId={id}&status=Shipped
    Then the reply does not have the order of the other customer

  Scenario: no status filter
    Given the customer has an order with status Placed and an order with status Shipped
    When the customer sends GET /orders?customerId={id} without status
    Then the reply is 200 with both orders, newest first

Rule: R7 An unknown order id returns 404 on ship and on history (AC6)

  Scenario: ship an unknown order
    Given no order has the id 0199f0a2-0000-7000-8000-000000000001
    When the clerk ships the order 0199f0a2-0000-7000-8000-000000000001 with the carrier DHL and the tracking code JD014600006751
    Then the reply is 404

  Scenario: history of an unknown order
    Given no order has the id 0199f0a2-0000-7000-8000-000000000001
    When the customer sends GET /orders/0199f0a2-0000-7000-8000-000000000001/history
    Then the reply is 404
