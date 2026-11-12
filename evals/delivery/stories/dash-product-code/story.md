# Unique product codes

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
