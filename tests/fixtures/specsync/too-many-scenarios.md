## Behaviour spec

```gherkin
Rule: R1 Guest checkout
  Scenario Outline: Guest pays with <card>
    Given a guest with 1 product in the cart
    When the guest pays with a "<card>" card
    Then the shop creates the order
    Examples:
      | card |
      | Visa |
      | Mastercard |
      | Maestro |
      | Amex |
      | iDEAL |
```
