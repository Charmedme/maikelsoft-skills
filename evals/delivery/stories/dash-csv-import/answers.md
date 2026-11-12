# Answers of the product owner

- The column order is always `Code,Name,Description`. Another header is refused with 400 as a whole.
- Empty lines are skipped and are not counted.
- The description can be empty.
- The code is stored trimmed, the name and the description as written.
- Importing the same file twice: the second time everything is unchanged.
- The reply for a wrong file is a problem details object with an `errors` list; each item has `line` and `message`.
