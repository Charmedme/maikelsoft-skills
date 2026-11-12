## SHOP-12 Free returns

**Type:** User Story
**Parent:** None
**Status:** spec:draft

As a customer, I want to return an order without cost within 30 days, so that I buy with less risk.

### Acceptance criteria

- [ ] AC1: A return within 30 days is free.
- [ ] AC2: After that, the shop charges a return fee.
- [ ] AC3: A return of a sale item follows the same rule.

### Behaviour spec

```gherkin
Rule: R1 A return within 30 days is free (AC1) (limit: 30 days)

  Scenario: return below the limit
    Given an order that was delivered 10 days ago
    When the customer returns the order
    Then the return is free

Rule: R2 A late return costs a fee (AC2)

  Scenario: late return
    Given an order that was delivered 40 days ago
    When the customer returns the order
    Then the shop charges a fee
```
