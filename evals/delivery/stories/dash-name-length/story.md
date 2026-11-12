# Limit the length of a product name

As a catalog manager, I want product names to have a maximum length, so that lists and exports stay readable.

## Acceptance criteria

1. A product name can have at most 100 characters.
2. Create (`POST /api/products`) and update (`PUT /api/products/{id}`) refuse a longer name with 400 and a validation error on `Name`.
3. A name of exactly 100 characters is accepted.
