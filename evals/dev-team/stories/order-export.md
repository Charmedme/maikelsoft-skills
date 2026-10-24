Story ORD-52: Export the orders of a customer as CSV

State: approved

As a customer service agent, I want to download the orders of a customer as CSV, so that I can answer questions in a spreadsheet.

Acceptance criteria:
- [ ] AC1: GET /orders/export?customerId={id} returns a CSV file (content type text/csv). The first line is the header id,placedAt,status,sku,quantity,unitPrice,lineTotal. Each order line is one row.
- [ ] AC2: Amounts have a dot as the decimal separator and always 2 decimals, whatever the language settings of the server.
- [ ] AC3: placedAt is in ISO 8601 UTC with the letter Z, to the second.
- [ ] AC4: A field with a comma, a double quote, or a line break is between double quotes (RFC 4180). A double quote in a field is written two times.
- [ ] AC5: A field that starts with =, +, -, or @ starts with a single quote (') in the file. A spreadsheet then does not run it as a formula.
- [ ] AC6: A customer without orders gets a file with only the header line. An empty customer id returns 400 with a problem details body.

Examples:

AC1, AC2, AC3: one order with two lines, placed at 2026-10-16 09:00 UTC
| sku | quantity | unitPrice | Row in the file |
|-----|----------|-----------|-----------------|
| SKU-1 | 2 | 2.5 | <id>,2026-10-16T09:00:00Z,Placed,SKU-1,2,2.50,5.00 |
| SKU-2 | 1 | 1 | <id>,2026-10-16T09:00:00Z,Placed,SKU-2,1,1.00,1.00 |

AC4: a comma in the sku
- Given an order with the line sku "BOX,SMALL", quantity 1, unit price 3.00
- When the agent exports the orders of the customer
- Then the row has the sku field "BOX,SMALL" between double quotes

AC5: a formula in the sku
- Given an order with the line sku "=1+1", quantity 1, unit price 3.00
- When the agent exports the orders of the customer
- Then the row has the sku field '=1+1

AC6: no orders
- Given a customer without orders
- When the agent exports the orders of the customer
- Then the file has only the header line
