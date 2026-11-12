# Sources: rw-title-trim

| Rule | Value or behaviour | Source |
|------|--------------------|--------|
| R1 | Stored title has no white space at the start or the end; white space = spaces, tabs, line breaks | story.md AC1 |
| R1 | Example `"  My trip  "` | story.md Context |
| R1 | Spaces inside the title stay as they are | answers.md line 3 |
| R2 | Trim on update | story.md AC1 |
| R2 | Update without a title keeps the current title | answers.md line 4 |
| R3 | Slug from the trimmed title | story.md AC2 |
| R3 | Slug form ("My trip" -> "my-trip") | repo/src/Conduit/Infrastructure/Slug.cs (existing) |
| R3 | An update with a new title changes the slug | repo/src/Conduit/Features/Articles/Edit.cs (existing) |
| R4 | Empty after trim: 422, error on `title`, on create and update | story.md AC3, answers.md line 5 |
| R4 | Error format `{"errors": {"title": [...]}}` | answers.md line 6 |
| R5 | Description and body stay as typed | story.md AC4 |
| R6 | No change to the article list or to existing articles | answers.md line 7 |
| all | Example values ("New title", "A short intro", "# Day 1") | test data only |

## Gaps

- The error message text for an empty title: no source. The scenarios check the field only.
- Other white space (for example a no-break space): AC1 names only spaces, tabs, and line breaks. Left out.
