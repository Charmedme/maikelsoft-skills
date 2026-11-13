# Sources: rw-comments

| Rule | Value or behaviour | Source |
|------|--------------------|--------|
| R1 | Route `POST /api/articles/{slug}/comments`, body `{"comment": {"body": ...}}`, reply fields `id`, `createdAt`, `updatedAt`, `body`, `author` | story.md AC1 |
| R1 | Unknown article gives 404 on add | answers.md line 4 |
| R1 | Adding a comment returns 201 | story.md AC1, answers.md line 9 |
| R2 | 422 with an error on `body` for a comment without text | story.md AC5 |
| R2 | A body of only spaces counts as without text | answers.md line 7 |
| R2 | Error format `{"errors": {"body": [...]}}` (422, field name in lower case) | repo/src/Conduit/Infrastructure/Errors/ErrorHandlingMiddleware.cs (existing) |
| R3 | `GET .../comments` returns `{"comments": [...]}`, also without a token | story.md AC2 |
| R3 | Oldest first | answers.md line 6 |
| R3 | Unknown article gives 404 on list | answers.md line 4 |
| R4 | `author.following` true or false; false without a token | story.md AC6 |
| R5 | `DELETE .../comments/{id}` removes the comment | story.md AC3 |
| R5 | Only the author can remove | story.md AC4 |
| R5 | Other reader gets 403, comment stays | answers.md line 3 |
| R5 | 204 with an empty body | answers.md line 8 |
| R6 | Comment that does not exist or belongs to another article: 404 | answers.md line 5 |
| R6 | Unknown article on remove: 404 | answers.md line 4 |
| R7 | Comments are deleted with the article | story.md AC7 |
| all | Example values ("my-trip", "ann", "Great trip!", id 999, 09:00/10:00) | test data only, no product value |

## Gaps

- Status code of a successful add (200 or 201): no source. The scenario checks the reply content only.
- Add or remove without a token (401 or another code): no source in story.md or answers.md. Left out.
- Maximum length of a comment body: no source. Left out.
