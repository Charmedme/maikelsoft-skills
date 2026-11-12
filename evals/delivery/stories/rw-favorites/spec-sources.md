# Sources: rw-favorites

| Rule | Value or behaviour | Source |
|------|--------------------|--------|
| R1 | `POST /api/articles/{slug}/favorite` returns `{"article": ...}` with `favorited: true` and the new count | story.md AC1 |
| R1 | Marking twice: 200, count stays the same | answers.md line 3 |
| R1 | Author can mark the own article | answers.md line 5 |
| R2 | `DELETE .../favorite` returns the article with `favorited: false` and the new count | story.md AC2 |
| R2 | Removing a mark that does not exist: 200 with the article | answers.md line 4 |
| R3 | 401 without a token, 404 for an unknown slug, for both endpoints | story.md AC3 |
| R4 | `favorited` true only for the reader who marked; in single article, list, and feed | story.md AC4 |
| R4 | The feed holds articles of followed authors (so the reader follows "bob" in the Given) | repo/src/Conduit/Features/Articles/List.cs (existing) |
| R5 | `favoritesCount` = number of readers who marked, in each response | story.md AC5 |
| R6 | `GET /api/articles?favorited={username}` lists only marked articles | story.md AC6 |
| R6 | Combines with limit, offset; `articlesCount` = all matching | answers.md line 6 |
| R7 | Combines with tag and author | answers.md line 6; repo/src/Conduit/Features/Articles/ArticlesController.cs (existing `tag` and `author` query values) |
| R6 | Unknown user name: empty list, not an error | answers.md line 7 |
| all | Example values (slugs, user names, tags "travel"/"food") | test data only |

## Gaps

- No gaps that block a rule.
