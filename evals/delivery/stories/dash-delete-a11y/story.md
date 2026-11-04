# Delete a product with the keyboard and a screen reader

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
