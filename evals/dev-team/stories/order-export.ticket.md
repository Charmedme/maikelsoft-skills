# Export the orders of a customer as CSV (ORD-52)

**Type:** story
**Status:** spec:ready

As a customer service agent, I want to download the orders of a customer as CSV, so that I can answer questions in a spreadsheet.

## Context

`GET /orders?customerId={id}` returns at most 50 orders of the customer as JSON. No CSV export exists. An order has the status Placed, Cancelled, or Shipped. Each order line has a `sku`, a `quantity`, a `unitPrice`, and a `lineTotal`.

The error contract is RFC 9457 problem details (decision API-04). An expected failure is a result value (decision API-05). No new NuGet package. Authorization is out of scope for this story. The human is also the product owner of this item.

## Acceptance criteria

1. `GET /orders/export?customerId={id}` returns a CSV file (content type `text/csv`). The first line is the header `id,placedAt,status,sku,quantity,unitPrice,lineTotal`. Each order line is one row.
2. Amounts have a dot as the decimal separator and always 2 decimals, whatever the language settings of the server.
3. `placedAt` is in ISO 8601 UTC with the letter Z, to the second.
4. A field with a comma, a double quote, or a line break is between double quotes (RFC 4180). A double quote in a field is written two times.
5. A field that starts with =, +, -, or @ starts with a single quote (') in the file. A spreadsheet then does not run it as a formula.
6. A customer without orders gets a file with only the header line. An empty customer id returns 400 with a problem details body.

## Behaviour spec

```gherkin
Rule: R1 The export is a CSV file with a header and one row for each order line (AC1)

  Scenario: one order with two lines
    Given an order of the customer, placed at 2026-10-16 09:00 UTC, with the lines 2 x SKU-1 at 2.5 and 1 x SKU-2 at 1
    When the agent exports the orders of the customer
    Then the reply is 200 with the content type text/csv
    And line 1 of the file is "id,placedAt,status,sku,quantity,unitPrice,lineTotal"
    And the file has the row "<id>,2026-10-16T09:00:00Z,Placed,SKU-1,2,2.50,5.00"
    And the file has the row "<id>,2026-10-16T09:00:00Z,Placed,SKU-2,1,1.00,1.00"

  Scenario: two orders
    Given the customer has 2 orders with 1 line each
    When the agent exports the orders of the customer
    Then the file has the header line and 2 rows

Rule: R2 Amounts have a dot and 2 decimals, whatever the language settings of the server (AC2)

  Scenario: server with Dutch language settings
    Given the language settings of the server are nl-NL
    And an order of the customer with the line 2 x SKU-1 at 2.5
    When the agent exports the orders of the customer
    Then the row has unitPrice 2.50 and lineTotal 5.00

  Scenario: whole amount
    Given an order of the customer with the line 1 x SKU-2 at 1
    When the agent exports the orders of the customer
    Then the row has unitPrice 1.00 and lineTotal 1.00

Rule: R3 placedAt is ISO 8601 UTC with the letter Z, to the second (AC3)

  Scenario: time with milliseconds
    Given an order of the customer, placed at 2026-10-16 09:00:00.400 UTC
    When the agent exports the orders of the customer
    Then the row has placedAt 2026-10-16T09:00:00Z

  Scenario: server in another time zone
    Given the time zone of the server is Europe/Amsterdam
    And an order of the customer, placed at 2026-10-16 09:00:00 UTC
    When the agent exports the orders of the customer
    Then the row has placedAt 2026-10-16T09:00:00Z

Rule: R4 A field with a comma, a double quote, or a line break is between double quotes (AC4)

  Scenario: comma in the sku
    Given an order of the customer with the line sku "BOX,SMALL", quantity 1, unit price 3.00
    When the agent exports the orders of the customer
    Then the row has the sku field "BOX,SMALL" between double quotes

  Scenario: double quote in the sku
    Given an order of the customer with the line sku BOX"1, quantity 1, unit price 3.00
    When the agent exports the orders of the customer
    Then the row has the sku field between double quotes, with the double quote written two times: "BOX""1"

  Scenario: line break in the sku
    Given an order of the customer with a line with a line break in the sku, quantity 1, unit price 3.00
    When the agent exports the orders of the customer
    Then the row has the sku field between double quotes, with the line break in it

Rule: R5 A field that starts with =, +, -, or @ starts with a single quote (AC5)

  Scenario Outline: formula sign at the start of the sku
    Given an order of the customer with the line sku "<sku>", quantity 1, unit price 3.00
    When the agent exports the orders of the customer
    Then the row has the sku field <field>

    Examples:
      | sku     | field    |
      | =1+1    | '=1+1    |
      | +1      | '+1      |
      | -1      | '-1      |
      | @SUM(1) | '@SUM(1) |

Rule: R6 A customer without orders gets only the header, and an empty customer id is refused (AC6)

  Scenario: customer without orders
    Given a customer without orders
    When the agent exports the orders of the customer
    Then the reply is 200 and the file has only the header line

  Scenario: empty customer id of zeros
    When the agent sends GET /orders/export?customerId=00000000-0000-0000-0000-000000000000
    Then the reply is 400 with a problem details body

  Scenario: customer id without a value
    When the agent sends GET /orders/export?customerId=
    Then the reply is 400 with a problem details body
```
