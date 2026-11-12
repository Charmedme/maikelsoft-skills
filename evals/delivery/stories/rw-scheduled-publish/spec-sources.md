# Sources: rw-scheduled-publish

| Rule | Value or behaviour | Source |
|------|--------------------|--------|
| R1 | Optional `publishAt` on create and update, ISO 8601 with offset or `Z`, example `2026-11-03T09:00:00+01:00` | story.md AC1 |
| R1 | Without `publishAt`: published at once | story.md AC1 |
| R1 | Response `publishAt` in UTC, ending in `Z` (`2026-11-03T08:00:00Z`) | story.md AC2 |
| R1 | Not a valid date and time: 422 on `publishAt` | answers.md line 6 |
| R1 | Without an offset: 422 | answers.md line 7 |
| R2 | Before the publish time: 404 for another reader and without a token | story.md AC3 |
| R2 | Visible from the publish time on, without further action | story.md AC6 |
| R2 | At the exact moment the article is visible | answers.md line 4 |
| R2 | The server clock decides, not the client clock | answers.md line 3 |
| R3 | Not in the list, the feed, or a filter by tag, author, or favorite | story.md AC3 |
| R3 | `articlesCount` counts only visible articles | story.md AC9 |
| R4 | Author sees the own scheduled article as a single article and with `author={own username}` | story.md AC4 |
| R5 | Nobody can favorite or comment before the publish time (404), author included | story.md AC5 |
| R6 | `publishAt` in the past publishes at once | story.md AC7 |
| R6 | Author can move the publish time into the future again; hidden until the new time | answers.md line 5 |
| R6 | Hidden article not in a favorite filter | story.md AC3 |
| R7 | Tag list shows only tags of articles visible to everybody | story.md AC8 |
| R7 | List order stays newest created first | answers.md line 8 |
| all | Example times (2026-11-01, 2026-11-03, 2026-11-10) | test data only |

## Gaps

- The `publishAt` value in the response of an article created without `publishAt` (creation time or another value): no source. Left out.
- Whether the author sees the own scheduled article in the general list without the author filter, or in the feed: AC4 names only the single article and the author filter. Left out.
- The value of `favoritesCount` or the comments of an article that was favorited or commented before it was moved back into the future: no source. Left out.
