## Behaviour spec

```gherkin
Feature: Too many rules
  Rule: R1 Guest checkout part 1
    Scenario: Guest pays part 1
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
  Rule: R2 Guest checkout part 2
    Scenario: Guest pays part 2
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
  Rule: R3 Guest checkout part 3
    Scenario: Guest pays part 3
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
  Rule: R4 Guest checkout part 4
    Scenario: Guest pays part 4
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
  Rule: R5 Guest checkout part 5
    Scenario: Guest pays part 5
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
  Rule: R6 Guest checkout part 6
    Scenario: Guest pays part 6
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
  Rule: R7 Guest checkout part 7
    Scenario: Guest pays part 7
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
  Rule: R8 Guest checkout part 8
    Scenario: Guest pays part 8
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
```
