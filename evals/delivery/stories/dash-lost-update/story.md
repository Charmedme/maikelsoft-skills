# No lost updates on products

As a catalog manager, I want to be told when somebody else changed a product while I was editing it, so that I do not overwrite their change.

## Acceptance criteria

1. `GET /api/products/{id}` and the product list return a `rowVersion` for each product.
2. `PUT /api/products/{id}` needs the `rowVersion` that the editor started from.
3. When the product changed since that version, the update is refused with 409 and nothing changes.
4. An update without `rowVersion` is refused with 400 and a validation error on `RowVersion`.
5. After a successful update, the response or a new GET gives the new `rowVersion`.
6. When two updates from the same version arrive at the same moment, exactly one succeeds and the other gets 409.
