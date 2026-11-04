# Limits on the article pages

As the operator, I want a limit on the page size of the article lists, so that one request cannot load the whole database.

## Context

`GET /api/articles` and `GET /api/articles/feed` take `limit` and `offset`. Today any value is accepted. A client asked for `limit=1000000` last week and the API became slow.

## Acceptance criteria

1. `limit` is 20 when the client does not send it.
2. The largest allowed `limit` is 100.
3. A `limit` larger than 100, a `limit` smaller than 1, or a negative `offset` is refused with 422, with the error on the field that is wrong (`limit` or `offset`).
4. A value that is not a whole number is refused in the same way.
5. These rules apply to the article list and to the feed.
6. `articlesCount` stays the number of all matching articles, also when `offset` is past the end.
