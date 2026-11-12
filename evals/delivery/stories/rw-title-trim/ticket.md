# Trim the spaces around an article title

**Type:** story
**Status:** spec:ready

As an author, I want the spaces around my article title removed, so that titles and links look clean.

## Context

Authors paste titles from other tools. Titles like `"  My trip  "` now keep the spaces in the stored title.

## Acceptance criteria

1. When an author creates or updates an article, the stored title has no white space at the start or the end. White space is spaces, tabs, and line breaks.
2. The slug is made from the trimmed title.
3. A title that is empty after the trim is refused with status 422, and the error is on the field `title`. This applies to create and to update.
4. The description and the body stay exactly as the author typed them.

## Behaviour spec

```gherkin
Rule: R1 A new article stores the title without white space at the start or the end (AC1)

  Scenario: spaces around the title
    When "bob" creates an article with the title "  My trip  "
    Then the stored title is "My trip"

  Scenario: tab and line break around the title
    When "bob" creates an article with the title "\tMy trip\n", a tab before and a line break after
    Then the stored title is "My trip"

  Scenario: spaces inside the title
    When "bob" creates an article with the title " My   big trip "
    Then the stored title is "My   big trip"

Rule: R2 An update stores the title without white space at the start or the end (AC1)

  Scenario: update with spaces around the title
    Given "bob" has the article "my-trip" with the title "My trip"
    When "bob" updates the title to "  New title  "
    Then the stored title is "New title"

  Scenario: update without a title
    Given "bob" has the article "my-trip" with the title "My trip"
    When "bob" updates only the body of "my-trip"
    Then the stored title stays "My trip"

Rule: R3 The slug is made from the trimmed title (AC2)

  Scenario: slug on create
    When "bob" creates an article with the title "  My trip  "
    Then the article gets the slug "my-trip"

  Scenario: slug on update
    Given "bob" has the article "my-trip"
    When "bob" updates the title to "\nNew title\t", a line break before and a tab after
    Then the article gets the slug "new-title"

Rule: R4 A title that is empty after the trim is refused (AC3)

  Scenario Outline: empty title after the trim
    When "bob" <action> with a title of <title>
    Then the reply is 422 with the error on "title" in the form {"errors": {"title": [...]}}

    Examples:
      | action                        | title                     |
      | creates an article            | 3 spaces                  |
      | creates an article            | a tab and a line break    |
      | updates the article "my-trip" | 3 spaces                  |

  Scenario: refused update keeps the title
    Given "bob" has the article "my-trip" with the title "My trip"
    When "bob" updates the title to "   "
    Then the stored title stays "My trip"

Rule: R5 The description and the body stay as the author typed them (AC4)

  Scenario: description with spaces around it
    When "bob" creates an article with the title "  My trip  " and the description "  A short intro  "
    Then the stored description is "  A short intro  "

  Scenario: body with line breaks around it
    When "bob" creates an article with the title "  My trip  " and the body "\n# Day 1\n"
    Then the stored body is "\n# Day 1\n"

Rule: R6 Existing articles stay as they are (AC1)

  Scenario: article stored before this change
    Given the article "old-trip" was stored before this change with the title "  Old trip  "
    When a visitor lists the articles
    Then "old-trip" has the title "  Old trip  "
```
