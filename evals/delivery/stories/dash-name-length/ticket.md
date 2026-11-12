# Limit the length of a product name

**Type:** story
**Status:** spec:ready

As a catalog manager, I want product names to have a maximum length, so that lists and exports stay readable.

## Acceptance criteria

1. A product name can have at most 100 characters.
2. Create (`POST /api/products`) and update (`PUT /api/products/{id}`) refuse a longer name with 400 and a validation error on `Name`.
3. A name of exactly 100 characters is accepted.

## Behaviour spec

```gherkin
Rule: R1 Create accepts a product name of at most 100 characters (AC1, AC2, AC3) (limit: 100 characters)

  Scenario: name below the limit on create
    When the catalog manager creates a product with a name of 99 letters
    Then the reply is 201

  Scenario: name at the limit on create
    When the catalog manager creates a product with a name of 100 letters
    Then the reply is 201

  Scenario: name above the limit on create
    When the catalog manager creates a product with a name of 101 letters
    Then the reply is 400 with a validation error on Name

  Scenario: a refused create stores nothing
    When the catalog manager creates a product with the code PD100 and a name of 101 letters
    Then no product PD100 exists

Rule: R2 Update accepts a product name of at most 100 characters (AC1, AC2, AC3) (limit: 100 characters)

  Scenario: name below the limit on update
    Given the product PD002 with the name Galaxy S24
    When the catalog manager updates PD002 with a name of 99 letters
    Then the reply is 200

  Scenario: name at the limit on update
    Given the product PD002 with the name Galaxy S24
    When the catalog manager updates PD002 with a name of 100 letters
    Then the reply is 200

  Scenario: name above the limit on update
    Given the product PD002 with the name Galaxy S24
    When the catalog manager updates PD002 with a name of 101 letters
    Then the reply is 400 with a validation error on Name

  Scenario: a refused update changes nothing
    Given the product PD002 with the name Galaxy S24
    When the catalog manager updates PD002 with a name of 101 letters
    Then PD002 still has the name Galaxy S24

Rule: R3 Existing products with a longer name stay as they are until somebody edits them (AC1)

  Scenario: existing long name is still shown
    Given an existing product PD100 with a name of 120 letters from before this change
    When the catalog manager gets the product list
    Then PD100 has its name of 120 letters

  Scenario: editing a product with a long name needs a shorter name
    Given an existing product PD100 with a name of 120 letters from before this change
    When the catalog manager updates only the description of PD100 and keeps the name
    Then the reply is 400 with a validation error on Name
```
