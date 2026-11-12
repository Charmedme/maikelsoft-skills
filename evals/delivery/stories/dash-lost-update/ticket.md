# No lost updates on products

**Type:** story
**Status:** spec:ready

As a catalog manager, I want to be told when somebody else changed a product while I was editing it, so that I do not overwrite their change.

## Acceptance criteria

1. `GET /api/products/{id}` and the product list return a `rowVersion` for each product.
2. `PUT /api/products/{id}` needs the `rowVersion` that the editor started from.
3. When the product changed since that version, the update is refused with 409 and nothing changes.
4. An update without `rowVersion` is refused with 400 and a validation error on `RowVersion`.
5. After a successful update, the response or a new GET gives the new `rowVersion`.
6. When two updates from the same version arrive at the same moment, exactly one succeeds and the other gets 409.

## Behaviour spec

```gherkin
Rule: R1 Each product comes with its rowVersion (AC1)

  Scenario: one product
    Given the product PD002
    When the catalog manager gets /api/products/{id} of PD002
    Then the reply has a rowVersion

  Scenario: the product list
    Given the products PD002, PD003 and PD004
    When the catalog manager gets the product list
    Then each product in the list has a rowVersion

Rule: R2 An update needs the rowVersion that the editor started from (AC2, AC4)

  Scenario: update with the current rowVersion
    Given the catalog manager got PD002 with rowVersion "v1" and nobody changed it since
    When the catalog manager updates the name of PD002 to Galaxy S25 and sends back "v1" exactly as received
    Then the reply is 200 and PD002 has the name Galaxy S25

  Scenario: update without rowVersion
    Given the product PD002 with the name Galaxy S24
    When the catalog manager updates the name of PD002 to Galaxy S25 without a rowVersion
    Then the reply is 400 with a validation error on RowVersion

  Scenario: an update without rowVersion changes nothing
    Given the product PD002 with the name Galaxy S24
    When the catalog manager updates the name of PD002 to Galaxy S25 without a rowVersion
    Then PD002 still has the name Galaxy S24

  Scenario: delete needs no rowVersion
    Given the product PD004
    When the catalog manager deletes PD004 without a rowVersion
    Then the reply is 200 and PD004 is gone

Rule: R3 An update from an old version is refused with 409 and changes nothing (AC3)

  Scenario: somebody else changed the product
    Given manager A and manager B got PD002 with rowVersion "v1", and manager B then changed its name to Galaxy S25
    When manager A updates the name of PD002 to Galaxy S26 with rowVersion "v1"
    Then the reply is 409 with a problem details body

  Scenario: the change of the other manager stays
    Given manager A and manager B got PD002 with rowVersion "v1", and manager B then changed its name to Galaxy S25
    When manager A updates the name of PD002 to Galaxy S26 with rowVersion "v1"
    Then PD002 still has the name Galaxy S25

  Scenario: a refused update gets no audit log entry
    Given manager A and manager B got PD002 with rowVersion "v1", and manager B then changed its name to Galaxy S25
    When manager A updates the name of PD002 to Galaxy S26 with rowVersion "v1"
    Then the audit log of PD002 has no entry for the change to Galaxy S26

Rule: R4 After a successful update the editor gets the new rowVersion (AC5)

  Scenario: new version after the update
    Given the catalog manager got PD002 with rowVersion "v1"
    When the catalog manager updates PD002 with rowVersion "v1"
    Then the rowVersion of PD002 in the reply or in a new GET differs from "v1"

  Scenario: next update with the new version
    Given the catalog manager updated PD002 and got the new rowVersion "v2"
    When the catalog manager updates PD002 again with rowVersion "v2"
    Then the reply is 200

Rule: R5 Of two updates from the same version at the same moment, exactly one succeeds (AC6)

  Scenario: two updates at the same moment
    Given manager A and manager B got PD002 with rowVersion "v1"
    When both send an update of PD002 with rowVersion "v1" at the same moment
    Then one reply is 200 and the other reply is 409

  Scenario: only the successful update is in the audit log
    Given manager A and manager B got PD002 with rowVersion "v1"
    When both send an update of PD002 with rowVersion "v1" at the same moment
    Then the audit log of PD002 has exactly one new UPDATED_PRODUCT entry
```
