# Favorite articles

As a reader, I want to mark articles as a favorite, so that I can find them again and the author sees which articles readers like.

## Context

The favorite feature was removed during a rewrite. The database table `ArticleFavorites` still exists. The article responses still have the fields `favorited` and `favoritesCount`, but nothing sets them correctly. The clients use the RealWorld API specification for this feature.

## Acceptance criteria

1. `POST /api/articles/{slug}/favorite` marks the article as a favorite of the signed-in reader and returns the article (`{"article": {...}}`) with `favorited: true` and the new `favoritesCount`.
2. `DELETE /api/articles/{slug}/favorite` removes the mark and returns the article with `favorited: false` and the new count.
3. Both need a signed-in reader (401 without a token). An unknown slug gives 404.
4. `favorited` is true only for the reader who marked the article. It is correct in each response that has articles: the single article, the article list, and the feed.
5. `favoritesCount` is the number of readers who marked the article, in each of these responses.
6. `GET /api/articles?favorited={username}` lists only the articles that this user marked.

## API specification

The endpoints and the response format follow the RealWorld API specification for "Favorite Article" and "Unfavorite Article", and the `favorited` filter of "List Articles".
