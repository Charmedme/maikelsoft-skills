# Order confirmation mail (ORD-23)

**Type:** story
**Status:** spec:ready

As a customer, I want a confirmation mail when I place an order, so that I know that the shop has my order.

## Context

A customer places an order with `POST /orders`. The reply is 201 with the order. The service sends no mail today.

The error contract is RFC 9457 problem details (decision API-04). An expected failure is a result value (decision API-05). Authorization is out of scope for this story.

Open points for the team:

- The order has an `id` and no other number. The story does not say which value is the order number in the mail.
- The service has no email address for a customer. The story does not say where the address comes from.
- The story does not say if the service tries the mail again after a failure.

## Acceptance criteria

1. When an order is placed, the customer gets a mail with the order number, the lines, and the total.
2. A failure to send the mail does not fail the order.
3. The mail is sent at most one time per order.

## Behaviour spec

```gherkin
Rule: R1 A placed order gives a mail with the order number, the lines, and the total (AC1)

  Scenario: order with two lines
    Given a customer
    When the customer places an order with the lines 2 x SKU-1 at 2.50 and 1 x SKU-2 at 1.00
    Then the customer gets a mail with the order number of that order
    And the mail has the line SKU-1 with quantity 2 and the line SKU-2 with quantity 1
    And the mail has the total 6.00

Rule: R2 A failure to send the mail does not fail the order (AC2)

  Scenario: the mail server refuses the mail
    Given the mail server refuses each mail
    When the customer places an order with the line 1 x SKU-1 at 2.50
    Then the reply is 201 with the order
    And the order list of the customer has that order

Rule: R3 The mail is sent at most one time per order (AC3)

  Scenario: one order gives one mail
    Given a customer
    When the customer places an order with the line 1 x SKU-1 at 2.50
    Then the customer gets exactly 1 mail for that order

  Scenario: two orders give one mail each
    Given a customer
    When the customer places two orders
    Then the customer gets exactly 1 mail for each order

  Scenario: the mail step runs again for the same order
    Given the customer got the mail for an order
    When the service handles that order again, for example after a restart
    Then the customer still has exactly 1 mail for that order
```
