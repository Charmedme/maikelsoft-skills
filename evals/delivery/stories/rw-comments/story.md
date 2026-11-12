# Comments on articles

As a reader, I want to comment on an article and read the comments of others, so that I can discuss the article.

## Context

The comment feature was removed during a rewrite. The database table `Comments` still exists, with the relation to the article and to the author. The clients use the RealWorld API specification for this feature.

## Acceptance criteria

1. `POST /api/articles/{slug}/comments` with `{"comment": {"body": "..."}}` adds a comment by the signed-in reader and returns 201 with `{"comment": {...}}` with `id`, `createdAt`, `updatedAt`, `body`, and `author` (a profile).
2. `GET /api/articles/{slug}/comments` returns `{"comments": [...]}` for each visitor, also without a token.
3. `DELETE /api/articles/{slug}/comments/{id}` removes the comment.
4. Only the author of a comment can remove it.
5. A comment without text is refused with 422 and an error on `body`.
6. In each comment, `author.following` tells whether the current reader follows the comment author. Without a token it is false.
7. When an article is deleted, its comments are deleted too.

## API specification

The endpoints and the response format follow the RealWorld API specification for "Add Comments to an Article", "Get Comments from an Article", and "Delete Comment".
