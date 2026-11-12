# Sources: rw-paging-limits

| Rule | Value or behaviour | Source |
|------|--------------------|--------|
| R1 | `limit` is 20 when not sent | story.md AC1 |
| R2 | Largest `limit` 100; 100 allowed, 101 refused | story.md AC2, answers.md line 3 |
| R2 | Above 100: 422 with the error on `limit` | story.md AC3 |
| R2 | `limit=1000000` refused, not lowered | story.md Context, answers.md line 5 |
| R3 | `limit` smaller than 1: 422 on `limit` | story.md AC3 |
| R4 | Negative `offset`: 422 on `offset`; `offset=0` allowed | story.md AC3, answers.md line 4 |
| R4 | Offset past the end: empty list, full `articlesCount` | story.md AC6, answers.md line 4 |
| R4 | List order newest first | repo/src/Conduit/Features/Articles/List.cs (existing order by created time, newest first) |
| R5 | Not a whole number: refused in the same way (422, error on the field) | story.md AC4 |
| R5 | Error format `{"errors": {"limit": [...]}}` | answers.md line 6 |
| R6 | Same rules for the feed | story.md AC5 |
| R6 | Feed: 401 for a missing token comes before the value check | answers.md line 7 |
| R6, R7 | Feed holds articles of followed authors | repo/src/Conduit/Features/Articles/List.cs (existing) |
| R7 | `articlesCount` = all matching, also past the end, for the feed | story.md AC5, AC6 |
| all | Example values (19/20/21/150/3 articles, values "abc", "2.5", "1.5", -5) | test data only |

## Gaps

- None. Each value comes from story.md, answers.md, or existing behaviour.
