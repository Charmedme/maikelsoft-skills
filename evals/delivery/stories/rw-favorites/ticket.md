# Favorite articles

**Type:** story
**Status:** spec:ready

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

## Behaviour spec

```gherkin
Rule: R1 A signed-in reader marks an article as a favorite (AC1)

  Scenario: reader marks an article
    Given the article "my-trip" has a favoritesCount of 0
    When "ann" marks "my-trip" as a favorite
    Then the reply is the article with favorited true and favoritesCount 1

  Scenario: reader marks the same article twice
    Given "ann" marked "my-trip" as a favorite and its favoritesCount is 1
    When "ann" marks "my-trip" as a favorite again
    Then the reply is 200 with the article with favorited true and favoritesCount 1

  Scenario: author marks the own article
    Given "bob" is the author of "my-trip" and its favoritesCount is 0
    When "bob" marks "my-trip" as a favorite
    Then the reply is the article with favorited true and favoritesCount 1

Rule: R2 A signed-in reader removes the favorite mark (AC2)

  Scenario: reader removes the mark
    Given "ann" marked "my-trip" as a favorite and its favoritesCount is 1
    When "ann" removes the favorite mark of "my-trip"
    Then the reply is the article with favorited false and favoritesCount 0

  Scenario: reader removes a mark that does not exist
    Given "carl" marked "my-trip" and "ann" did not, so its favoritesCount is 1
    When "ann" removes the favorite mark of "my-trip"
    Then the reply is 200 with the article with favorited false and favoritesCount 1

Rule: R3 Marking needs a signed-in reader and a known article (AC3)

  Scenario Outline: refused mark or unmark
    When <who> sends <action> for the article "<slug>"
    Then the reply is <status>

    Examples:
      | who                       | action             | slug         | status |
      | a visitor without a token | a favorite mark    | my-trip      | 401    |
      | a visitor without a token | a mark removal     | my-trip      | 401    |
      | "ann"                     | a favorite mark    | no-such-trip | 404    |
      | "ann"                     | a mark removal     | no-such-trip | 404    |

Rule: R4 favorited is true only for the reader who marked the article (AC4)

  Scenario Outline: the reader who marked sees favorited true
    Given "ann" follows "bob" and marked the article "my-trip" of "bob"
    When "ann" reads <response>
    Then "my-trip" has favorited true

    Examples:
      | response               |
      | the single article     |
      | the article list       |
      | the feed               |

  Scenario: another reader sees favorited false
    Given "ann" marked "my-trip" and "carl" did not
    When "carl" reads the article list
    Then "my-trip" has favorited false

Rule: R5 favoritesCount is the number of readers who marked the article (AC5)

  Scenario Outline: count in each response with articles
    Given "ann" and "carl" marked the article "my-trip" of "bob", and "dave" follows "bob"
    When "dave" reads <response>
    Then "my-trip" has favoritesCount 2

    Examples:
      | response               |
      | the single article     |
      | the article list       |
      | the feed               |

Rule: R6 The favorited filter lists only the articles that the user marked (AC6)

  Scenario: filter by a user who marked 2 of 3 articles
    Given "ann" marked "trip-a" and "trip-b" and not "trip-c"
    When a visitor lists the articles with favorited "ann"
    Then the reply holds "trip-a" and "trip-b" with articlesCount 2

  Scenario: filter by an unknown user name
    Given no member has the user name "nobody"
    When a visitor lists the articles with favorited "nobody"
    Then the reply is an empty list with articlesCount 0

  Scenario: filter combined with limit and offset
    Given "ann" marked "trip-a" and "trip-b"
    When a visitor lists the articles with favorited "ann", limit 1 and offset 0
    Then the reply holds 1 article with articlesCount 2

Rule: R7 The favorited filter combines with the tag and author filters (AC6)

  Scenario: filter combined with a tag
    Given "ann" marked "trip-a" with tag "travel" and "trip-b" with tag "food"
    When a visitor lists the articles with favorited "ann" and tag "travel"
    Then the reply holds only "trip-a" with articlesCount 1

  Scenario: filter combined with an author
    Given "ann" marked "trip-a" of "bob" and "trip-b" of "carl"
    When a visitor lists the articles with favorited "ann" and author "bob"
    Then the reply holds only "trip-a" with articlesCount 1
```
