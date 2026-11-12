## Return an order free of charge within 30 days of delivery

**Type:** User Story
**Parent:** None
**Status:** spec:ready

As a customer, I want to return an order free of charge within 30 days after delivery, so that a wrong choice costs me nothing.

### Acceptance criteria

- [ ] A customer who returns an order within 30 days after delivery pays no fee.
- [ ] A customer who returns an order more than 30 days after delivery pays the return fee of 4.95 euro.

### Behaviour spec

```gherkin
Rule: R1 A return within 30 days after delivery is free (AC1) (limit: 30 days)

  Scenario: return below the limit
    Given an order that the customer got 29 days ago
    When the customer returns the order
    Then the return is free

  Scenario: return at the limit
    Given an order that the customer got 30 days ago
    When the customer returns the order
    Then the return is free

  Scenario: return above the limit
    Given an order that the customer got 31 days ago
    When the customer returns the order
    Then the return is not free

Rule: R2 A return more than 30 days after delivery costs a fee (AC2) (limit: 30 days)

  Scenario: return below the limit
    Given an order that the customer got 29 days ago
    When the customer returns the order
    Then the shop charges no return fee

  Scenario: return at the limit
    Given an order that the customer got 30 days ago
    When the customer returns the order
    Then the shop charges no return fee

  Scenario: return above the limit
    Given an order that the customer got 31 days ago
    When the customer returns the order
    Then the shop charges a return fee of 4.95 euro
```

### Notes

- Decision (product owner): the 30 days are calendar days after delivery. Day 0 is the day of delivery.
- Decision (product owner): the return fee is 4.95 euro.
- Assumption: the shop charges the same fee for each return, not a percentage of the order.
- Out of scope: showing the fee before the customer confirms the return.
- Out of scope: refund of the shipping cost, and returns of single items of an order.
