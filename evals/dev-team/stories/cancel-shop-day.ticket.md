# Cancel an order until the end of the next shop day (ORD-41)

**Type:** story
**Status:** spec:ready

As a customer, I want to cancel my order until the end of the day after I ordered. Then I have one full day to change my mind.

## Context

The domain class `Order` has a method `Cancel` with a cancel window of 24 hours, but no endpoint calls it. The new cancel rule replaces the 24-hour cancel window. The setting `Orders:CancelWindowHours` can go.

The shop time zone is Europe/Amsterdam. In 2026, summer time ends on 25 October: the offset goes from UTC+2 to UTC+1.

The error contract is RFC 9457 problem details (decision API-04). An expected failure is a result value, not an exception (decision API-05). No new NuGet package. Authorization for the cancel is out of scope for this story. The current setup stays (no authorization), and this is an open point.

## Acceptance criteria

1. `POST /orders/{id}/cancel` cancels a placed order and returns 200 with the order, status Cancelled.
2. A customer can cancel until 23:59:59 shop time on the calendar day after the order day. The shop time zone is Europe/Amsterdam. The order day is the date of the order in shop time.
3. After that moment, the cancel returns 409 Conflict with a problem details body.
4. An order that is shipped returns 409 Conflict with a problem details body.
5. A second cancel of a cancelled order returns 200 and changes nothing.
6. An unknown order id returns 404.

## Behaviour spec

```gherkin
Rule: R1 A placed order is cancelled (AC1)

  Scenario: cancel on the order day
    Given an order with status Placed, placed at 2026-10-16 10:00 shop time
    When the customer sends POST /orders/{id}/cancel at 2026-10-16 11:00 shop time
    Then the reply is 200 with the order, status Cancelled

Rule: R2 The cancel is possible until 23:59:59 shop time on the day after the order day (AC2, AC3) (limit: 23:59:59 shop time on the next day)

  Scenario: cancel below the limit, more than 24 hours after the order
    Given an order placed at 2026-10-16 10:00 shop time (08:00 UTC)
    When the customer cancels the order at 2026-10-17 23:59:58 shop time (21:59:58 UTC)
    Then the reply is 200 with the order, status Cancelled

  Scenario: cancel at the limit
    Given an order placed at 2026-10-16 10:00 shop time (08:00 UTC)
    When the customer cancels the order at 2026-10-17 23:59:59 shop time (21:59:59 UTC)
    Then the reply is 200 with the order, status Cancelled

  Scenario: cancel above the limit
    Given an order placed at 2026-10-16 10:00 shop time (08:00 UTC)
    When the customer cancels the order at 2026-10-18 00:00:00 shop time (2026-10-17 22:00:00 UTC)
    Then the reply is 409 with a problem details body
    And the order still has status Placed

Rule: R3 The order day is the date of the order in shop time (AC2)

  Scenario: order before midnight UTC and after midnight shop time
    Given an order placed at 2026-10-16 23:30 UTC, which is 2026-10-17 01:30 shop time
    When the customer cancels the order at 2026-10-18 23:00 shop time (21:00 UTC)
    Then the reply is 200 with the order, status Cancelled

  Scenario: next day ends after the change from summer time
    Given an order placed at 2026-10-24 12:00 shop time (10:00 UTC)
    When the customer cancels the order at 2026-10-25 23:30 shop time (22:30 UTC)
    Then the reply is 200 with the order, status Cancelled

  Scenario: after the end of the next day, after the change from summer time
    Given an order placed at 2026-10-24 12:00 shop time (10:00 UTC)
    When the customer cancels the order at 2026-10-26 00:00:00 shop time (2026-10-25 23:00:00 UTC)
    Then the reply is 409 with a problem details body

Rule: R4 A shipped order cannot be cancelled (AC4)

  Scenario: cancel a shipped order
    Given an order with status Shipped, placed at 2026-10-16 10:00 shop time
    When the customer cancels the order at 2026-10-16 11:00 shop time
    Then the reply is 409 with a problem details body
    And the order still has status Shipped

Rule: R5 A second cancel returns 200 and changes nothing (AC5)

  Scenario: second cancel on the next day
    Given the customer cancelled the order at 2026-10-16 11:00 shop time
    When the customer cancels the order again at 2026-10-17 09:00 shop time
    Then the reply is 200 with the order, status Cancelled, and the cancelledAt of the first cancel

  Scenario: second cancel after the end of the next day
    Given an order placed at 2026-10-16 10:00 shop time that the customer cancelled at 2026-10-16 11:00 shop time
    When the customer cancels the order again at 2026-10-19 09:00 shop time
    Then the reply is 200 with the order, status Cancelled, and the cancelledAt of the first cancel

Rule: R6 An unknown order id returns 404 (AC6)

  Scenario: no order has the id
    Given no order has the id 0199f0a2-0000-7000-8000-000000000001
    When the customer sends POST /orders/0199f0a2-0000-7000-8000-000000000001/cancel
    Then the reply is 404
