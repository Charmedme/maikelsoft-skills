# Comments on articles

**Type:** story
**Status:** spec:ready

As a reader, I want to comment on an article and read the comments of others, so that I can discuss the article.

## Context

The comment feature was removed during a rewrite. The database table `Comments` still exists, with the relation to the article and to the author. The clients use the RealWorld API specification for this feature.

## Acceptance criteria

1. `POST /api/articles/{slug}/comments` with `{"comment": {"body": "..."}}` adds a comment by the signed-in reader and returns `{"comment": {...}}` with `id`, `createdAt`, `updatedAt`, `body`, and `author` (a profile).
2. `GET /api/articles/{slug}/comments` returns `{"comments": [...]}` for each visitor, also without a token.
3. `DELETE /api/articles/{slug}/comments/{id}` removes the comment.
4. Only the author of a comment can remove it.
5. A comment without text is refused with 422 and an error on `body`.
6. In each comment, `author.following` tells whether the current reader follows the comment author. Without a token it is false.
7. When an article is deleted, its comments are deleted too.

## API specification

The endpoints and the response format follow the RealWorld API specification for "Add Comments to an Article", "Get Comments from an Article", and "Delete Comment".

## Behaviour spec

```gherkin
Rule: R1 A signed-in reader adds a comment to an article (AC1)

  Scenario: reader adds a comment
    Given the article "my-trip" has no comments
    When the reader "ann" adds the comment "Great trip!" to "my-trip"
    Then the reply is a comment with body "Great trip!", an id, createdAt, updatedAt, and author "ann"

  Scenario: reader adds a comment to an unknown article
    Given no article has the slug "no-such-trip"
    When the reader "ann" adds the comment "Great trip!" to "no-such-trip"
    Then the reply is 404

Rule: R2 A comment without text is refused (AC5)

  Scenario: empty body
    When the reader "ann" adds the comment "" to "my-trip"
    Then the reply is 422 with an error on "body"

  Scenario: body of only spaces
    When the reader "ann" adds the comment "   " to "my-trip"
    Then the reply is 422 with an error on "body"

  Scenario: body of one letter
    When the reader "ann" adds the comment "a" to "my-trip"
    Then the article "my-trip" has 1 comment with body "a"

Rule: R3 Each visitor can read the comments of an article, oldest first (AC2)

  Scenario: visitor without a token reads the comments
    Given "ann" commented "First" on "my-trip" at 09:00 and "bob" commented "Second" at 10:00
    When a visitor without a token lists the comments of "my-trip"
    Then the reply holds the comments "First" and "Second" in this order

  Scenario: article without comments
    Given the article "my-trip" has no comments
    When a visitor without a token lists the comments of "my-trip"
    Then the reply is an empty list of comments

  Scenario: comments of an unknown article
    Given no article has the slug "no-such-trip"
    When a visitor without a token lists the comments of "no-such-trip"
    Then the reply is 404

Rule: R4 The comment author shows whether the current reader follows that author (AC6)

  Scenario: reader follows the comment author
    Given "carl" follows "bob" and "bob" commented on "my-trip"
    When "carl" lists the comments of "my-trip"
    Then the comment of "bob" has author following true

  Scenario: reader does not follow the comment author
    Given "carl" does not follow "bob" and "bob" commented on "my-trip"
    When "carl" lists the comments of "my-trip"
    Then the comment of "bob" has author following false

  Scenario: visitor without a token
    Given "carl" follows "bob" and "bob" commented on "my-trip"
    When a visitor without a token lists the comments of "my-trip"
    Then the comment of "bob" has author following false

Rule: R5 Only the author of a comment can remove it (AC3, AC4)

  Scenario: author removes the own comment
    Given "ann" commented "Great trip!" on "my-trip"
    When "ann" removes that comment
    Then the reply is 204 with an empty body

  Scenario: removed comment is gone from the list
    Given "ann" removed her comment "Great trip!" on "my-trip"
    When a visitor lists the comments of "my-trip"
    Then the comment "Great trip!" is not in the list

  Scenario: another reader tries to remove the comment
    Given "ann" commented "Great trip!" on "my-trip"
    When "bob" removes that comment
    Then the reply is 403 and the comment "Great trip!" is still on "my-trip"

Rule: R6 Removing a comment that is not on the article gives 404 (AC3)

  Scenario: comment does not exist
    Given the article "my-trip" has no comment with id 999
    When "ann" removes the comment 999 of "my-trip"
    Then the reply is 404

  Scenario: comment belongs to another article
    Given "ann" commented "Nice" on "other-trip"
    When "ann" removes that comment through the article "my-trip"
    Then the reply is 404 and the comment "Nice" is still on "other-trip"

  Scenario: unknown article
    Given no article has the slug "no-such-trip"
    When "ann" removes the comment 1 of "no-such-trip"
    Then the reply is 404

Rule: R7 Deleting an article deletes its comments (AC7)

  Scenario: article with comments is deleted
    Given the article "my-trip" has 2 comments
    When the author deletes the article "my-trip"
    Then the 2 comments no longer exist
```
