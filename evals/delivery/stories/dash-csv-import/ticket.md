# Import products from a CSV file

**Type:** story
**Status:** spec:ready

As a catalog manager, I want to import products from a CSV file, so that I can load a supplier list without typing.

## Context

The screen already has an "Import CSV" button and `POST /api/products/importcsv` takes the file (multipart form, field `formFile`), but the endpoint stores nothing yet.

## Acceptance criteria

1. The file has a header line `Code,Name,Description` and one product on each next line. Standard CSV quoting applies: a field with a comma, a quote, or a line break is in double quotes, and a quote inside is doubled. A UTF-8 byte order mark at the start is allowed.
2. A line whose code (trimmed, ignoring case) matches an existing product updates that product. Other lines create a new product.
3. The import is all or nothing. When one line is wrong, nothing is stored and the reply is 400 with a list of the errors, each with the line number in the file (the header is line 1) and the reason.
4. A line is wrong when the code or the name is empty, when the name has more than 100 characters, or when the same code (trimmed, ignoring case) appears on two lines of the file. For a duplicate, both lines are reported.
5. A file with more than 1000 product lines is refused with 400 as a whole.
6. On success the reply is 200 with `{"created": n, "updated": n, "unchanged": n}`. A line that would not change an existing product counts as unchanged and changes nothing.
7. Each product that the import creates or changes gets the same audit log entries as a create or an update on the product screen. Unchanged products get no entry.
8. Only a user with both the permission to add products and the permission to update products can import. Others get 403.

## Behaviour spec

```gherkin
Rule: R1 The file is standard CSV with the header Code,Name,Description (AC1)

  Scenario: quoted field with a comma and a doubled quote
    Given a file with the header Code,Name,Description and the line PD100,"Phone, ""Pro""",Big phone
    When the catalog manager imports the file
    Then the product PD100 has the name Phone, "Pro"

  Scenario: UTF-8 byte order mark at the start
    Given a file that starts with a UTF-8 byte order mark, then the header Code,Name,Description and the line PD100,Phone,Big phone
    When the catalog manager imports the file
    Then the reply is 200 with created 1

  Scenario: other header refused as a whole
    Given a file with the header Name,Code,Description and the line Phone,PD100,Big phone
    When the catalog manager imports the file
    Then the reply is 400 for the file as a whole

  Scenario: empty lines are skipped
    Given a file with the header, the line PD100,Phone,Big phone, an empty line, and the line PD101,Tablet,Small tablet
    When the catalog manager imports the file
    Then the reply is 200 with created 2

Rule: R2 A line with the code of an existing product updates it, other lines create a product (AC2)

  Scenario: code in other case and with spaces updates the existing product
    Given the product PD002 with the name Galaxy S24
    When the catalog manager imports the line " pd002 ,Galaxy S25,Samsung phone"
    Then the product PD002 has the name Galaxy S25 and no new product is created

  Scenario: new code creates a product
    Given no product with the code PD100
    When the catalog manager imports the line PD100,Phone,Big phone
    Then a product PD100 with the name Phone exists

  Scenario: only the code is stored trimmed
    Given no product with the code PD100
    When the catalog manager imports the line " PD100 , Phone ,Big phone"
    Then the new product has the code "PD100" and the name " Phone "

  Scenario: description can be empty
    Given no product with the code PD100
    When the catalog manager imports the line PD100,Phone,
    Then a product PD100 with an empty description exists

Rule: R3 When one line is wrong, nothing is stored and the reply lists the errors by line (AC3, AC4)

  Scenario: empty code
    Given a file with the header, the line PD100,Phone,Big phone, and the line ",Tablet,Small tablet"
    When the catalog manager imports the file
    Then the reply is 400 with a problem details errors list that has an item with line 3

  Scenario: empty name
    Given a file with the header, the line PD100,Phone,Big phone, and the line PD101,,Small tablet
    When the catalog manager imports the file
    Then the reply is 400 with a problem details errors list that has an item with line 3

  Scenario: the correct lines of a wrong file are not stored
    Given a file with the header, the line PD100,Phone,Big phone, and the line PD101,,Small tablet
    When the catalog manager imports the file
    Then no product PD100 exists

  Scenario: duplicate code on two lines reports both lines
    Given a file with the header, the line PD100,Phone,Big phone, the line PD101,Tablet,Small tablet, and the line " pd100 ",Phone 2,Other phone
    When the catalog manager imports the file
    Then the reply is 400 with errors for line 2 and line 4

Rule: R4 A name has at most 100 characters (AC4) (limit: 100 characters)

  Scenario: name below the limit
    When the catalog manager imports a line with a name of 99 characters
    Then the reply is 200 with created 1

  Scenario: name at the limit
    When the catalog manager imports a line with a name of 100 characters
    Then the reply is 200 with created 1

  Scenario: name above the limit
    Given a file whose line 2 has a name of 101 characters
    When the catalog manager imports the file
    Then the reply is 400 with an error for line 2

Rule: R5 A file has at most 1000 product lines (AC5) (limit: 1000 lines)

  Scenario: file below the limit
    When the catalog manager imports a file with 999 product lines
    Then the reply is 200 with created 999

  Scenario: file at the limit
    When the catalog manager imports a file with 1000 product lines
    Then the reply is 200 with created 1000

  Scenario: file above the limit
    When the catalog manager imports a file with 1001 product lines
    Then the reply is 400 and no product is stored

  Scenario: empty lines do not count for the limit
    When the catalog manager imports a file with 1000 product lines and 5 empty lines
    Then the reply is 200 with created 1000

Rule: R6 The reply counts created, updated and unchanged products, and only changes get audit log entries (AC6, AC7)

  Scenario: one product of each kind
    Given the products PD002 "Galaxy S24, Samsung phone" and PD003 "Pixel 9, Google phone"
    When the catalog manager imports the lines PD100,Phone,Big phone and PD002,Galaxy S25,Samsung phone and PD003,Pixel 9,Google phone
    Then the reply is 200 with {"created": 1, "updated": 1, "unchanged": 1}

  Scenario: the same file twice
    Given the catalog manager imported the lines PD100,Phone,Big phone and PD002,Galaxy S25,Samsung phone
    When the catalog manager imports the same file again
    Then the reply is 200 with {"created": 0, "updated": 0, "unchanged": 2}

  Scenario: created and updated products get audit log entries
    Given the product PD002 "Galaxy S24, Samsung phone"
    When the catalog manager imports the lines PD100,Phone,Big phone and PD002,Galaxy S25,Samsung phone
    Then PD100 has a CREATED_PRODUCT audit log entry and PD002 an UPDATED_PRODUCT entry by that catalog manager

  Scenario: unchanged product gets no audit log entry
    Given the product PD003 "Pixel 9, Google phone"
    When the catalog manager imports the line PD003,Pixel 9,Google phone
    Then PD003 gets no new audit log entry

Rule: R7 Only a user with the permissions to add and to update products can import (AC8)

  Scenario: user who can only read products
    Given the user "user", who can only read products
    When the user imports a correct file
    Then the reply is 403

  Scenario: user who can add but not update products
    Given a user with the permission to add products and without the permission to update products
    When the user imports a correct file
    Then the reply is 403

  Scenario: user who can update but not add products
    Given a user with the permission to update products and without the permission to add products
    When the user imports a correct file
    Then the reply is 403

  Scenario: user with both permissions
    Given the user "admin", who has all permissions
    When the user imports a correct file
    Then the reply is 200
```
