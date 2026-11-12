# Revisions and safe edits of an article

**Type:** story
**Status:** spec:ready

As an author who edits on two devices, I want my edits to never overwrite a newer version without notice, and I want to see the earlier versions of my article.

## Context

Authors lose text: they open an article on a laptop and on a phone, save on the phone, then save the old text on the laptop. Links to an article also break when the author changes the title, because the slug changes.

## Acceptance criteria

1. Each article response has a `version`: 1 after create, and 1 higher after each update that changes the article.
2. `PUT /api/articles/{slug}` needs `version` in the article body: the version that the author started from. When it is not the current version, the update is refused with 409, with an error on `version`, and nothing changes.
3. An update without `version` is refused with 422, with an error on `version`.
4. When two updates from the same version arrive at the same time, exactly one succeeds. The other gets 409.
5. `GET /api/articles/{slug}/revisions` returns `{"revisions": [...], "revisionsCount": n}`: the earlier versions, newest first. Each revision has `version`, `title`, `description`, `body`, `tagList`, and `updatedAt`.
6. Only the author can read the revisions. Another member gets 403. Without a token: 401. Unknown slug: 404.
7. When the title changes, the article gets a new slug. The old slug keeps working for reading: `GET /api/articles/{old slug}` and its comments return the article, with the new slug in the response.
8. An old slug stays reserved for its article: a new article whose title gives the same slug gets another slug.
9. When an article is deleted, its revisions and its old slugs are deleted too. The slugs are free again.

## Behaviour spec

```gherkin
Rule: R1 An article has a version that goes up by 1 with each update that changes it (AC1)

  Scenario: new article
    When "bob" creates the article "My trip" with version 5 in the body
    Then the reply has version 1

  Scenario: update that changes the body
    Given "bob" has the article "my-trip" at version 1
    When "bob" updates the body of "my-trip" from version 1
    Then the reply has version 2

  Scenario: update that changes nothing
    Given "bob" has the article "my-trip" at version 1
    When "bob" sends the same title, description, body, and tags from version 1
    Then the article stays at version 1 with no revisions

  Scenario: article from before this change
    Given the article "old-trip" was created before versions existed
    When "bob" reads "old-trip"
    Then the reply has version 1

Rule: R2 An update needs the current version of the article (AC2, AC3, AC4)

  Scenario: update from an old version
    Given "bob" has the article "my-trip" at version 2 with body "Phone text"
    When "bob" updates the body of "my-trip" to "Laptop text" from version 1
    Then the reply is 409 with an error on "version" and the body stays "Phone text"

  Scenario: update from the current version
    Given "bob" has the article "my-trip" at version 2
    When "bob" updates the body of "my-trip" from version 2
    Then the reply has version 3

  Scenario: update without version
    Given "bob" has the article "my-trip" at version 2
    When "bob" updates the body of "my-trip" without version
    Then the reply is 422 with an error on "version"

  Scenario: two updates from the same version at the same time
    Given "bob" has the article "my-trip" at version 1
    When "bob" sends two body updates from version 1 at the same time
    Then exactly one update succeeds and the other gets 409

Rule: R3 The revision list shows the earlier versions, newest first (AC5)

  Scenario: article updated twice
    Given "bob" created "my-trip" and updated it twice, so it is at version 3
    When "bob" reads the revisions of "my-trip"
    Then the reply holds the revisions 2 and 1 in this order, each with version, title, description, body, tagList, and updatedAt, and revisionsCount 2

  Scenario: article never updated
    Given "bob" created "my-trip" and did not update it
    When "bob" reads the revisions of "my-trip"
    Then the reply is an empty list with revisionsCount 0

  Scenario: no paging
    Given "bob" updated "my-trip" 30 times
    When "bob" reads the revisions of "my-trip"
    Then the reply holds all 30 revisions with revisionsCount 30

Rule: R4 Only the author can read the revisions (AC6)

  Scenario: another member
    When "ann" reads the revisions of the article "my-trip" of "bob"
    Then the reply is 403

  Scenario: visitor without a token
    When a visitor without a token reads the revisions of "my-trip"
    Then the reply is 401

  Scenario: unknown article
    When "bob" reads the revisions of "no-such-trip"
    Then the reply is 404

Rule: R5 A new title gives a new slug, and the old slug keeps working for reading (AC7)

  Scenario: title change
    Given "bob" has the article "my-trip" with title "My trip"
    When "bob" changes the title to "My big trip"
    Then the article has the slug "my-big-trip"

  Scenario: read the article through the old slug
    Given the title of "my-trip" changed and the article now has the slug "my-big-trip"
    When a visitor reads the article "my-trip"
    Then the reply is the article with slug "my-big-trip"

  Scenario: read the comments through the old slug
    Given the article "my-trip" has the comment "Nice" and now has the slug "my-big-trip"
    When a visitor reads the comments of "my-trip"
    Then the reply holds the comment "Nice"

  Scenario: title goes back to an earlier title
    Given the article had the title "My trip", then "My big trip"
    When "bob" changes the title back to "My trip"
    Then the article has the slug "my-trip" again

Rule: R6 Changes go only through the current slug (AC7)

  Scenario Outline: change through an old slug
    Given the article "my-trip" now has the slug "my-big-trip"
    When "bob" sends <action> for "my-trip"
    Then the reply is 404

    Examples:
      | action                 |
      | an update              |
      | a delete               |
      | a favorite mark        |

Rule: R7 An old slug stays reserved until its article is deleted (AC8, AC9)

  Scenario: new article with the title of an old slug
    Given the article "my-trip" now has the slug "my-big-trip"
    When "ann" creates an article with the title "My trip"
    Then the new article gets the slug "my-trip-1"

  Scenario: deleted article frees its old slugs
    Given the article "my-trip" got the slug "my-big-trip", and "bob" deleted it
    When "ann" creates an article with the title "My trip"
    Then the new article gets the slug "my-trip"

  Scenario: deleted article has no revisions left
    Given the article "my-big-trip" has 2 revisions
    When "bob" deletes "my-big-trip"
    Then no revision of that article exists
```
