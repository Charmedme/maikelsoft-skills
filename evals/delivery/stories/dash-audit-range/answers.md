# Answers of the product owner

- `from` and `to` are optional. A missing one means no limit on that side.
- The validation error uses the name of the query parameter as the field, for example `pageSize`.
- `from` after `to` gives an error on `to`.
- A range of more than 366 days, counting both days, is refused with 400 and an error on `to`.
- `userName` is the user name of the person who made the change, as in the existing audit log entries.
- `totalItems` counts all matching entries, not only the page.
- A page past the end gives an empty `items` list with the full `totalItems`.
- Entries with the same time can come in any order between them.
