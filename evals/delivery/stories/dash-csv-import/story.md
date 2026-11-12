# Import products from a CSV file

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
