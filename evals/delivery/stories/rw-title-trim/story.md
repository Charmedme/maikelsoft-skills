# Trim the spaces around an article title

As an author, I want the spaces around my article title removed, so that titles and links look clean.

## Context

Authors paste titles from other tools. Titles like `"  My trip  "` now keep the spaces in the stored title.

## Acceptance criteria

1. When an author creates or updates an article, the stored title has no white space at the start or the end. White space is spaces, tabs, and line breaks.
2. The slug is made from the trimmed title.
3. A title that is empty after the trim is refused with status 422, and the error is on the field `title`. This applies to create and to update.
4. The description and the body stay exactly as the author typed them.
