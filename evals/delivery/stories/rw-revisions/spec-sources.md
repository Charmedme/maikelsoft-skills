# Sources: rw-revisions

| Rule | Value or behaviour | Source |
|------|--------------------|--------|
| R1 | `version` 1 after create, +1 after each changing update | story.md AC1 |
| R1 | `version` in a create body is ignored | answers.md line 8 |
| R1 | An update that changes nothing: no new version, no revision | answers.md line 3 |
| R1 | Existing articles start at version 1 with no revisions | answers.md line 4 |
| R1 | The list and the feed show `version` | story.md AC1, answers.md line 10 |
| R2 | `PUT /api/articles/{slug}` needs `version`; not current: 409, error on `version`, nothing changes | story.md AC2 |
| R2 | Without `version`: 422, error on `version` | story.md AC3 |
| R2 | Two updates from the same version: exactly one succeeds, the other 409 | story.md AC4 |
| R3 | `GET .../revisions` returns `{"revisions": [...], "revisionsCount": n}`, newest first, fields `version`, `title`, `description`, `body`, `tagList`, `updatedAt` | story.md AC5 |
| R3 | No paging | answers.md line 7 |
| R4 | Another member 403, no token 401, unknown slug 404 | story.md AC6 |
| R5 | New title gives new slug; old slug reads the article and its comments, response has the new slug | story.md AC7 |
| R5 | Slug from title ("My big trip" -> "my-big-trip") | repo/src/Conduit/Infrastructure/Slug.cs (existing) |
| R5 | A slug can go back to an earlier title of the same article | answers.md line 9 |
| R6 | Update and delete with an old slug: 404 | answers.md line 5 |
| R6 | Favorite with an old slug: 404 (only the current slug) | answers.md line 6 |
| R7 | Old slug reserved: a new article gets another slug | story.md AC8 |
| R7 | The other slug is `<slug>-1` | repo/src/Conduit/Features/Articles/Create.cs (existing suffix `-1`, `-2`, ...) |
| R7 | Delete removes revisions and old slugs; slugs are free again | story.md AC9 |
| all | Example values ("Phone text", "Laptop text", 30 updates) | test data only |

## Gaps

- Comments on an old slug: AC7 covers reading the comments only. Adding or removing a comment through an old slug has no source. Left out.
- Unfavorite (`DELETE .../favorite`) through an old slug: answers.md says "favorites go through the current slug only"; the scenario covers the mark only to keep 3 rows.
- Status code of a successful update: existing behaviour (200), not pinned in a scenario.
