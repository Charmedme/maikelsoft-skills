# Unique product codes

**Type:** story
**Status:** spec:ready

As a catalog manager, I want each product code to be used only once, so that orders and exports point to one product.

## Context

Codes are typed by hand. Today `ab-100`, `AB-100` and ` AB-100 ` can all exist at the same time.

## Acceptance criteria

1. The code is stored without white space at the start or the end.
2. Two codes are the same when they are equal after the trim, ignoring upper and lower case.
3. Create and update refuse a code that another product already has, with 400 and a validation error on `Code`.
4. A product can keep its own code on update, also when only the case changes.
5. The code of a deleted product can be used again.
6. Also when two people create a product with the same code at the same moment, only one product gets the code.

## Behaviour spec

```gherkin
Rule: R1 The code is stored trimmed, with the case as typed (AC1)

  Scenario: spaces at the start and the end are removed
    When the catalog manager creates a product with the code " Ab-100 "
    Then the product has the code "Ab-100"

  Scenario: trimmed on update too
    Given the product Galaxy S24 with the code PD002
    When the catalog manager updates Galaxy S24 with the code "  PD009  "
    Then Galaxy S24 has the code "PD009"

Rule: R2 An empty code is refused (AC1, AC3)

  Scenario: empty code
    When the catalog manager creates a product with the code ""
    Then the reply is 400 with a validation error on Code

  Scenario: code of only spaces
    When the catalog manager creates a product with the code "   "
    Then the reply is 400 with a validation error on Code

  Scenario: empty code on update
    Given the product Galaxy S24 with the code PD002
    When the catalog manager updates Galaxy S24 with the code "   "
    Then the reply is 400 with a validation error on Code

Rule: R3 Create refuses a code that another product has, after the trim and ignoring case (AC2, AC3)

  Scenario: same code in other case
    Given the product Galaxy S24 with the code PD002
    When the catalog manager creates a product with the code "pd002"
    Then the reply is 400 with a validation error on Code

  Scenario: same code with spaces
    Given the product Galaxy S24 with the code PD002
    When the catalog manager creates a product with the code " PD002 "
    Then the reply is 400 with a validation error on Code

  Scenario: a refused create stores nothing
    Given the product Galaxy S24 with the code PD002
    When the catalog manager creates a product with the code "pd002" and the name Copy
    Then no product with the name Copy exists

  Scenario: a different code is accepted
    Given the product Galaxy S24 with the code PD002
    When the catalog manager creates a product with the code "PD0021"
    Then the reply is 201

Rule: R4 Update refuses the code of another product, but a product can keep its own code (AC3, AC4)

  Scenario: code of another product
    Given the products Galaxy S24 with the code PD002 and Pixel 9 with the code PD003
    When the catalog manager updates Pixel 9 with the code "pd002"
    Then the reply is 400 with a validation error on Code

  Scenario: own code unchanged
    Given the product Galaxy S24 with the code PD002
    When the catalog manager updates the name of Galaxy S24 to Galaxy S25 and keeps the code PD002
    Then the reply is 200

  Scenario: own code with only a case change
    Given the product Galaxy S24 with the code PD002
    When the catalog manager updates Galaxy S24 with the code "pd002"
    Then the reply is 200 and Galaxy S24 has the code "pd002"

Rule: R5 The code of a deleted product can be used again (AC5)

  Scenario: reuse the code of a deleted product
    Given the product ThinkPad X1 with the code PD004 was deleted
    When the catalog manager creates a product with the code "pd004"
    Then the reply is 201

Rule: R6 Also at the same moment, only one product gets a code (AC6)

  Scenario: two creates with the same code at the same moment
    Given no product with the code AB-100
    When two catalog managers create a product with the codes "AB-100" and "ab-100" at the same moment
    Then one reply is 201 and the other reply is 400 with a validation error on Code

  Scenario: only one product has the code after the race
    Given no product with the code AB-100
    When two catalog managers create a product with the codes "AB-100" and "ab-100" at the same moment
    Then exactly one product has the code AB-100, ignoring case
```
