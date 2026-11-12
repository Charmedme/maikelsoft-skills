# Schedule an article

As an author, I want to give an article a publish time in the future, so that readers see it only from that moment.

## Context

Authors write articles for events, and want them to appear at the start of the event. Today an article is visible as soon as it is created.

## Acceptance criteria

1. Create and update take an optional field `publishAt`: a date and time in ISO 8601 with an offset or `Z`, for example `2026-11-03T09:00:00+01:00`. Without it, the article is published at once.
2. Each article response contains `publishAt` in UTC, ending in `Z`.
3. Before its publish time, a scheduled article is visible only to its author. For each other reader, and without a token, it does not exist: the single article gives 404, and it is not in the article list, the feed, or a filter by tag, author, or favorite.
4. The author sees the own scheduled article as a single article, and in `GET /api/articles?author={own username}`.
5. Nobody can favorite or comment on a scheduled article before its publish time (404), the author included.
6. From the publish time on, the article is visible to everybody, with no further action.
7. A `publishAt` in the past publishes at once.
8. The tag list (`GET /api/tags`) shows only tags of articles that are visible to everybody.
9. `articlesCount` counts only the articles that the reader can see.
