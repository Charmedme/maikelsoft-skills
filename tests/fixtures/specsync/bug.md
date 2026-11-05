## Behaviour spec

```gherkin
Feature: Return on day 30

  Rule: R1 A return on day 30 is free (limit: 30 days)
    Now: the shop charges a return fee of 4.95 for a return on day 30

    Scenario: Return below the limit
      Given an order that was delivered 29 days ago
      When the customer returns the order
      Then the shop charges "0.00" for the return

    Scenario: Return at the limit
      Given an order that was delivered 30 days ago
      When the customer returns the order
      Then the shop charges "0.00" for the return

    Scenario: Return above the limit
      Given an order that was delivered 31 days ago
      When the customer returns the order
      Then the shop charges a return fee of 4.95

  Rule: R2 Unchanged

    Scenario: Return on day 10 stays free
      Given an order that was delivered 10 days ago
      When the customer returns the order
      Then the shop charges "0.00" for the return
```
