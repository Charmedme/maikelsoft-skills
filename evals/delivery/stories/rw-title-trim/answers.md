# Answers of the product owner

- Spaces inside the title stay as they are. Only the start and the end are trimmed.
- An update that does not send a title keeps the current title.
- An update that sends a title with only white space is refused, like on create.
- The error format is the existing one: `{"errors": {"title": [...]}}`.
- No change to the article list or to existing articles in the database.
