# Sources: dash-delete-a11y

Repo paths are under `repo/src/UIs/angular/src/app/` unless written in full.

## Rules

- R1 (AC1): accessible name "Delete <product name>" — story.md AC1. Product "Galaxy S24" — seed data, repo/src/Monolith/ClassifiedAds.WebAPI/Testing/TestDataSeeder.cs. Admin user — answers.md line 3. Page `/products` — answers.md line 6.
- R2 (AC2): role `dialog`, name "Delete product", focus on "Cancel" — AC2. Heading and button texts change as the criteria say, nothing else of the look — answers.md line 5. Current dialog has heading "Delete Product" and buttons "Yes"/"No" — products/delete-product/delete-product.component.html. Button texts "Cancel" and "Delete" — AC2, AC3, AC4.
- R3 (AC3): Escape and "Cancel" close without a delete, focus back to the delete button that opened it — AC3.
- R4 (AC4): row gone, role `status` message "<product name> deleted." — AC4. The message can stay until the next action (answers.md line 4): this permits, it does not demand, so no scenario.
- R5 (AC5): focus on next row, else the new last row, else the "Add Product" link — AC5. Link text "Add Product" — products/list-products/list-products.component.html. Products Pixel 9 and ThinkPad X1 — TestDataSeeder.cs. The list order in the scenarios is set by the Given (test data).
- R6 (AC6): 390 px, 100 characters without spaces, no sideways scroll — AC6.

## Gaps

- No source says where the focus goes when the delete fails (server error); left out.
- No source says what the status message says on a failed delete; left out.
- No source says if the list filter ("Filter by") changes which row is "next"; left out.
