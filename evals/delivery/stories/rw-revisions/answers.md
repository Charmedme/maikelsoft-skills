# Answers of the product owner

- An update that changes nothing (same title, description, body, and tags) does not raise the version and does not make a revision.
- Existing articles start at version 1, with no revisions.
- Updates and deletes work only with the current slug. An old slug gives 404 for an update or a delete.
- Favorites go through the current slug only.
- The revision list has no paging for now.
- `version` in the body of a create is ignored.
- A slug can go back to an earlier title of the same article. The article then simply has that slug again.
- The article list and the feed show the `version` of each article too.
