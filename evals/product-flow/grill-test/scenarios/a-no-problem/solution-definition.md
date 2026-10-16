# Solution definition: Bulk order upload

## Solution
Business buyers upload a CSV file with order lines on a new page "Upload order". The shop reads the file, shows the lines in a table, and the buyer confirms the order.

## Screens
1. Upload page with a drop zone for the CSV file and a link to a sample file.
2. Check table: one row for each line, with article number, quantity, and a status icon.
3. Confirm button that places the order.

## File format
Columns: article number; quantity. Separator: semicolon. Maximum 500 lines.

## Out of scope
Excel files (.xlsx).
