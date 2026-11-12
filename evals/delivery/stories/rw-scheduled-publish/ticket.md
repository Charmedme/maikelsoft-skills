# Schedule an article

**Type:** story
**Status:** spec:ready

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

## Behaviour spec

```gherkin
Rule: R1 publishAt is an ISO 8601 date and time with an offset, and the reply gives it in UTC (AC1, AC2)

  Scenario: publish time with an offset
    When "bob" creates an article with publishAt "2026-11-03T09:00:00+01:00"
    Then the reply has publishAt "2026-11-03T08:00:00Z"

  Scenario: publish time without an offset
    When "bob" creates an article with publishAt "2026-11-03T09:00:00"
    Then the reply is 422 with an error on "publishAt"

  Scenario: publish time that is not a date and time
    When "bob" creates an article with publishAt "next Monday"
    Then the reply is 422 with an error on "publishAt"

  Scenario: no publish time
    Given "bob" created the article "my-trip" without publishAt
    When a visitor without a token reads "my-trip"
    Then the reply is the article "my-trip"

Rule: R2 From the publish time on, by the server clock, the article is visible to everybody (AC3, AC6) (limit: 2026-11-03T08:00:00Z)

  Scenario: server clock below the limit
    Given the article "my-trip" of "bob" has publishAt "2026-11-03T08:00:00Z" and the server clock is 2026-11-03T07:59:59Z
    When "ann" reads "my-trip"
    Then the reply is 404

  Scenario: server clock at the limit
    Given the article "my-trip" of "bob" has publishAt "2026-11-03T08:00:00Z" and the server clock is 2026-11-03T08:00:00Z
    When "ann" reads "my-trip"
    Then the reply is the article "my-trip"

  Scenario: server clock above the limit
    Given the article "my-trip" of "bob" has publishAt "2026-11-03T08:00:00Z" and the server clock is 2026-11-03T08:00:01Z
    When a visitor without a token reads "my-trip"
    Then the reply is the article "my-trip"

  Scenario: client clock does not matter
    Given the article "my-trip" has publishAt "2026-11-03T08:00:00Z", the server clock is 2026-11-03T07:59:59Z, and the clock of the client is 2026-11-03T09:00:00Z
    When "ann" reads "my-trip"
    Then the reply is 404

Rule: R3 Before its publish time, a scheduled article is in no list of another reader (AC3, AC9)

  Scenario Outline: scheduled article missing from a list
    Given "ann" follows "bob", and "bob" has the published article "trip-a" with tag "travel" and the scheduled article "trip-b" with tag "travel"
    When <reader> reads <list>
    Then the reply holds only "trip-a" with articlesCount 1

    Examples:
      | reader                    | list                              |
      | a visitor without a token | the article list                  |
      | "ann"                     | the feed                          |
      | "ann"                     | the articles with tag "travel"    |
      | "ann"                     | the articles with author "bob"    |

Rule: R4 The author sees the own scheduled article (AC4)

  Scenario: author reads the single article
    Given "bob" has the scheduled article "trip-b"
    When "bob" reads "trip-b"
    Then the reply is the article "trip-b"

  Scenario: author filters by the own user name
    Given "bob" has the published article "trip-a" and the scheduled article "trip-b"
    When "bob" lists the articles with author "bob"
    Then the reply holds "trip-a" and "trip-b" with articlesCount 2

Rule: R5 Nobody can favorite or comment on a scheduled article before its publish time (AC5)

  Scenario Outline: favorite or comment before the publish time
    Given "bob" has the scheduled article "trip-b"
    When <who> sends <action> for "trip-b"
    Then the reply is 404

    Examples:
      | who   | action          |
      | "bob" | a favorite mark |
      | "ann" | a favorite mark |
      | "bob" | a comment       |
      | "ann" | a comment       |

Rule: R6 A publish time in the past publishes at once, and a new publish time in the future hides the article again (AC6, AC7)

  Scenario: publish time in the past
    Given the server clock is 2026-11-03T08:00:00Z
    When "bob" creates the article "my-trip" with publishAt "2026-11-01T09:00:00Z"
    Then a visitor without a token can read "my-trip"

  Scenario: visible article moved into the future
    Given the server clock is 2026-11-03T08:00:00Z and the article "my-trip" of "bob" is visible
    When "bob" updates "my-trip" with publishAt "2026-11-10T08:00:00Z"
    Then "ann" gets 404 for "my-trip"

  Scenario: favorite list after the move into the future
    Given "ann" marked the visible article "my-trip" as a favorite, and "bob" then moved its publishAt into the future
    When a visitor lists the articles with favorited "ann"
    Then the reply is an empty list with articlesCount 0

Rule: R7 Tags and order follow the articles that everybody can see (AC8)

  Scenario: tag used only by a scheduled article
    Given the only article with tag "festival" is scheduled for later
    When "bob", its author, reads the tag list
    Then the tag list does not contain "festival"

  Scenario: tag after the publish time
    Given the only article with tag "festival" has a publishAt that is now past
    When a visitor reads the tag list
    Then the tag list contains "festival"

  Scenario: order stays newest created first
    Given "bob" created "trip-a" with publishAt "2026-11-03T10:00:00Z", then "trip-b" without publishAt, and the server clock is now 2026-11-03T11:00:00Z
    When a visitor lists the articles
    Then the articles are "trip-b", "trip-a" in this order
```
