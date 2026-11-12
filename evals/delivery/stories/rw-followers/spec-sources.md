# Sources: rw-followers

| Rule | Value or behaviour | Source |
|------|--------------------|--------|
| R1 | `GET /api/profiles/{username}/followers` returns `{"profiles": [...], "profilesCount": n}` | story.md AC1 |
| R1 | Readable without a token | story.md AC5 |
| R2 | Most recent follower first | story.md AC2 |
| R2 | "Most recent" = moment of the follow; unfollow and follow again moves to the top | answers.md line 3 |
| R2 | Follows without a follow moment come after the new ones, in any order | answers.md line 4 |
| R3 | Profile fields `username`, `bio`, `image`, `following`; false without a token | story.md AC3 |
| R3 | No email address in a response | repo/REVIEW.md, Must fix 2 (applies by answers.md line 5) |
| R4 | Unknown user name gives 404 | story.md AC4 |
| R4 | Error field `profile` for an unknown profile | repo/src/Conduit/Features/Profiles/ProfileReader.cs (existing `{"errors": {"profile": ["not found"]}}`) |
| R5 | `limit` 20 by default, at most 100; above 100 gives 422 on `limit`; count of all matching items | repo/REVIEW.md, Must fix 1 (applies by answers.md line 5) |
| R6 | `limit` below 1 gives 422 on `limit` | repo/REVIEW.md, Must fix 1 |
| R7 | Negative `offset` gives 422 on `offset`; count of all matching items next to the list | repo/REVIEW.md, Must fix 1 |
| R5-R7 | Rules cite (AC1) because they shape the list endpoint of AC1; the values come from REVIEW.md | answers.md line 5 |
| all | Example values (user names, bio "Hiker", 09:00/10:00/11:00, 101 and 3 followers) | test data only |

## Gaps

- Order between two follows of the same moment: no source. Left out.
- A `limit` or `offset` that is not a whole number: REVIEW.md does not say. Left out.
