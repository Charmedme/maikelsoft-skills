# SHOP-12 Free returns and guest checkout

As a customer, I want to return an order without cost, so that I can buy with less risk.

## Behaviour spec

```gherkin
Feature: Free returns and guest checkout

  Rule: R1 Free return within 30 days (limit: 30 days)

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

  Rule: R2 Guest checkout

    Scenario: Guest pays without an account
      Given a guest with 1 product in the cart
      When the guest pays with a valid card
      Then the shop creates the order
      And the shop sends the confirmation to the email address of the guest

    Scenario Outline: Guest must give a valid email address
      Given a guest with 1 product in the cart
      When the guest pays with the email address "<email>"
      Then the shop shows "<message>"

      Examples:
        | email        | message                            |
        | (empty)      | Enter your email address           |
        | anne@example | Enter a valid email address & retry |
```

## Links

Spec copied to docs/work/SHOP-12/spec.md by the agent.
