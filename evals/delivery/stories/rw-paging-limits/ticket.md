# Limits on the article pages

**Type:** story
**Status:** spec:ready

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

## Behaviour spec

```gherkin
Rule: R1 Without limit, a page holds at most 20 articles (AC1) (limit: 20)

  Scenario: article count below the limit
    Given 19 articles exist
    When a visitor lists the articles without limit
    Then the reply holds 19 articles with articlesCount 19

  Scenario: article count at the limit
    Given 20 articles exist
    When a visitor lists the articles without limit
    Then the reply holds 20 articles with articlesCount 20

  Scenario: article count above the limit
    Given 21 articles exist
    When a visitor lists the articles without limit
    Then the reply holds 20 articles with articlesCount 21

Rule: R2 The largest allowed limit is 100 (AC2, AC3) (limit: 100)

  Scenario: limit below the limit
    Given 150 articles exist
    When a visitor lists the articles with limit 99
    Then the reply holds 99 articles with articlesCount 150

  Scenario: limit at the limit
    Given 150 articles exist
    When a visitor lists the articles with limit 100
    Then the reply holds 100 articles with articlesCount 150

  Scenario: limit above the limit
    Given 150 articles exist
    When a visitor lists the articles with limit 101
    Then the reply is 422 with an error on "limit"

  Scenario: very large limit is refused, not lowered
    When a visitor lists the articles with limit 1000000
    Then the reply is 422 with an error on "limit"

Rule: R3 The smallest allowed limit is 1 (AC3) (limit: 1)

  Scenario: limit below the limit
    When a visitor lists the articles with limit 0
    Then the reply is 422 with an error on "limit"

  Scenario: limit at the limit
    Given 3 articles exist
    When a visitor lists the articles with limit 1
    Then the reply holds 1 article with articlesCount 3

  Scenario: limit above the limit
    Given 3 articles exist
    When a visitor lists the articles with limit 2
    Then the reply holds 2 articles with articlesCount 3

  Scenario: negative limit
    When a visitor lists the articles with limit -5
    Then the reply is 422 with an error on "limit"

Rule: R4 The offset is not negative (AC3, AC6) (limit: 0)

  Scenario: offset below the limit
    When a visitor lists the articles with offset -1
    Then the reply is 422 with an error on "offset"

  Scenario: offset at the limit
    Given the articles "trip-c", "trip-b", "trip-a" exist, newest first
    When a visitor lists the articles with limit 1 and offset 0
    Then the reply holds only "trip-c" with articlesCount 3

  Scenario: offset above the limit
    Given the articles "trip-c", "trip-b", "trip-a" exist, newest first
    When a visitor lists the articles with limit 1 and offset 1
    Then the reply holds only "trip-b" with articlesCount 3

  Scenario: offset past the end
    Given 3 articles exist
    When a visitor lists the articles with offset 10
    Then the reply is an empty list with articlesCount 3

Rule: R5 A value that is not a whole number is refused (AC4)

  Scenario Outline: not a whole number
    When a visitor lists the articles with <field> "<value>"
    Then the reply is 422 with the error on "<field>" in the form {"errors": {"<field>": [...]}}

    Examples:
      | field  | value |
      | limit  | abc   |
      | limit  | 2.5   |
      | offset | abc   |
      | offset | 1.5   |

Rule: R6 The same rules apply to the feed (AC5)

  Scenario: feed limit at 100
    Given "ann" follows "bob" and "bob" wrote 150 articles
    When "ann" reads the feed with limit 100
    Then the reply holds 100 articles with articlesCount 150

  Scenario: feed limit above 100
    When "ann" reads the feed with limit 101
    Then the reply is 422 with an error on "limit"

  Scenario: feed negative offset
    When "ann" reads the feed with offset -1
    Then the reply is 422 with an error on "offset"

  Scenario: feed without a token and a wrong limit
    When a visitor without a token reads the feed with limit 101
    Then the reply is 401

Rule: R7 The feed counts all matching articles past the end (AC5, AC6)

  Scenario: feed offset past the end
    Given "ann" follows "bob" and "bob" wrote 3 articles
    When "ann" reads the feed with offset 10
    Then the reply is an empty list with articlesCount 3
```
