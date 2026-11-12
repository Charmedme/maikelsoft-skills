# Delete a product with the keyboard and a screen reader

**Type:** story
**Status:** spec:ready

As a catalog manager who uses the keyboard and a screen reader, I want to delete a product from the list without losing my place, so that I can clean up the catalog quickly.

## Context

The product list in the Angular app has a delete action in each row that opens a confirmation dialog. After a delete, the focus falls back to the top of the page, and a screen reader says nothing.

## Acceptance criteria

1. The delete button of each row has the accessible name "Delete <product name>".
2. The confirmation is a dialog (role `dialog`) with the accessible name "Delete product". When it opens, the focus is on its "Cancel" button.
3. Escape and "Cancel" close the dialog without a delete. The focus goes back to the delete button that opened it.
4. After "Delete" in the dialog, the row is gone, and a status message (role `status`) says "<product name> deleted.".
5. After the delete, the focus is on the delete button of the next row. When the deleted row was the last row, it is on the delete button of the new last row. When the list is empty, it is on the "Add Product" link.
6. At a window width of 390 px, a product name of 100 characters without spaces does not make the page scroll sideways.

## Behaviour spec

```gherkin
Rule: R1 The delete button of each row is named after its product (AC1)

  Scenario: name of the delete button
    Given the admin user is on the product list at /products with the product Galaxy S24
    When a screen reader reads the delete button in the row of Galaxy S24
    Then its accessible name is "Delete Galaxy S24"

Rule: R2 The confirmation is a dialog named "Delete product" with the focus on "Cancel" (AC2)

  Scenario: dialog role and name
    Given the admin user is on the product list at /products
    When the user activates "Delete Galaxy S24" with the keyboard
    Then a dialog with the role dialog and the accessible name "Delete product" opens

  Scenario: focus on Cancel when the dialog opens
    Given the admin user is on the product list at /products
    When the user activates "Delete Galaxy S24" with the keyboard
    Then the focus is on the "Cancel" button of the dialog

  Scenario: only the heading and the button texts change
    Given the admin user is on the product list at /products
    When the user opens the dialog of "Delete Galaxy S24"
    Then the dialog has the heading "Delete product", the buttons "Delete" and "Cancel", and otherwise the current look

Rule: R3 Escape and "Cancel" close the dialog without a delete and give the focus back (AC3)

  Scenario: Escape deletes nothing
    Given the dialog of "Delete Galaxy S24" is open
    When the user presses Escape
    Then the dialog is closed and Galaxy S24 is still in the list

  Scenario: focus back after Escape
    Given the dialog of "Delete Galaxy S24" is open
    When the user presses Escape
    Then the focus is on the button "Delete Galaxy S24"

  Scenario: Cancel deletes nothing
    Given the dialog of "Delete Galaxy S24" is open
    When the user activates "Cancel"
    Then the dialog is closed and Galaxy S24 is still in the list

  Scenario: focus back after Cancel
    Given the dialog of "Delete Galaxy S24" is open
    When the user activates "Cancel"
    Then the focus is on the button "Delete Galaxy S24"

Rule: R4 After "Delete" the row is gone and a status message tells it (AC4)

  Scenario: row is gone
    Given the dialog of "Delete Galaxy S24" is open
    When the user activates "Delete"
    Then the list has no row for Galaxy S24

  Scenario: status message for the screen reader
    Given the dialog of "Delete Galaxy S24" is open
    When the user activates "Delete"
    Then a message with the role status says "Galaxy S24 deleted."

Rule: R5 After a delete the focus goes to the next row, else the new last row, else "Add Product" (AC5)

  Scenario: delete the first row
    Given the list shows Galaxy S24, Pixel 9 and ThinkPad X1 in this order
    When the user deletes Galaxy S24
    Then the focus is on the button "Delete Pixel 9"

  Scenario: delete a row in the middle
    Given the list shows Galaxy S24, Pixel 9 and ThinkPad X1 in this order
    When the user deletes Pixel 9
    Then the focus is on the button "Delete ThinkPad X1"

  Scenario: delete the last row
    Given the list shows Galaxy S24, Pixel 9 and ThinkPad X1 in this order
    When the user deletes ThinkPad X1
    Then the focus is on the button "Delete Pixel 9"

  Scenario: delete the only row
    Given the list shows only Galaxy S24
    When the user deletes Galaxy S24
    Then the focus is on the link "Add Product"

Rule: R6 A long product name does not make the page scroll sideways on a narrow window (AC6)

  Scenario: 100 characters without spaces at 390 px
    Given a product with a name of 100 letters "A" without spaces
    When the admin user opens /products in a window 390 px wide
    Then the page does not scroll sideways
```
