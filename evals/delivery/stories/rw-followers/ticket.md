# Followers of a profile

**Type:** story
**Status:** spec:ready

As a member, I want to see who follows a profile, so that I can find people with the same interests.

## Acceptance criteria

1. `GET /api/profiles/{username}/followers` returns `{"profiles": [...], "profilesCount": n}`: the members who follow this profile.
2. The most recent follower comes first.
3. Each profile has `username`, `bio`, `image`, and `following`: whether the current reader follows that member. Without a token, `following` is false.
4. An unknown user name gives 404.
5. Anybody can read the list, also without a token.

## Behaviour spec

```gherkin
Rule: R1 Anybody can read the followers of a profile (AC1, AC5)

  Scenario: visitor without a token reads the followers
    Given "ann" and "bob" follow "carl"
    When a visitor without a token reads the followers of "carl"
    Then the reply holds the profiles "ann" and "bob" with profilesCount 2

  Scenario: profile without followers
    Given nobody follows "carl"
    When a visitor without a token reads the followers of "carl"
    Then the reply is an empty list with profilesCount 0

Rule: R2 The most recent follower comes first (AC2)

  Scenario: two followers
    Given "ann" followed "carl" at 09:00 and "bob" followed "carl" at 10:00
    When a visitor reads the followers of "carl"
    Then the profiles are "bob", "ann" in this order

  Scenario: follower who unfollows and follows again moves to the top
    Given "ann" followed "carl" at 09:00 and "bob" at 10:00, and "ann" unfollowed and followed again at 11:00
    When a visitor reads the followers of "carl"
    Then the profiles are "ann", "bob" in this order

  Scenario: follow without a follow moment comes after the new ones
    Given "dave" follows "carl" from before this change, with no follow moment, and "ann" followed "carl" at 09:00
    When a visitor reads the followers of "carl"
    Then the profiles are "ann", "dave" in this order

Rule: R3 Each profile shows whether the current reader follows that member (AC3)

  Scenario: reader follows the follower
    Given "ann" follows "carl" and "eve" follows "ann"
    When "eve" reads the followers of "carl"
    Then the profile "ann" has following true

  Scenario: reader does not follow the follower
    Given "ann" follows "carl" and "eve" does not follow "ann"
    When "eve" reads the followers of "carl"
    Then the profile "ann" has following false

  Scenario: visitor without a token
    Given "ann" follows "carl" and "eve" follows "ann"
    When a visitor without a token reads the followers of "carl"
    Then the profile "ann" has following false

  Scenario: profile fields
    Given "ann" with bio "Hiker" follows "carl"
    When a visitor reads the followers of "carl"
    Then the profile "ann" has only username, bio "Hiker", image, and following, and no email address

Rule: R4 An unknown user name gives 404 (AC4)

  Scenario: unknown user name
    Given no member has the user name "nobody"
    When a visitor reads the followers of "nobody"
    Then the reply is 404 with an error on "profile"

Rule: R5 The page size is 20 by default and at most 100 (AC1) (limit: 100)

  Scenario: no limit given
    Given 101 members follow "carl"
    When a visitor reads the followers of "carl" without limit
    Then the reply holds 20 profiles with profilesCount 101

  Scenario: limit below the limit
    Given 101 members follow "carl"
    When a visitor reads the followers of "carl" with limit 99
    Then the reply holds 99 profiles with profilesCount 101

  Scenario: limit at the limit
    Given 101 members follow "carl"
    When a visitor reads the followers of "carl" with limit 100
    Then the reply holds 100 profiles with profilesCount 101

  Scenario: limit above the limit
    When a visitor reads the followers of "carl" with limit 101
    Then the reply is 422 with an error on "limit"

Rule: R6 The page size is at least 1 (AC1) (limit: 1)

  Scenario: limit below the limit
    When a visitor reads the followers of "carl" with limit 0
    Then the reply is 422 with an error on "limit"

  Scenario: limit at the limit
    Given 3 members follow "carl"
    When a visitor reads the followers of "carl" with limit 1
    Then the reply holds 1 profile with profilesCount 3

  Scenario: limit above the limit
    Given 3 members follow "carl"
    When a visitor reads the followers of "carl" with limit 2
    Then the reply holds 2 profiles with profilesCount 3

Rule: R7 The offset is not negative (AC1) (limit: 0)

  Scenario: offset below the limit
    When a visitor reads the followers of "carl" with offset -1
    Then the reply is 422 with an error on "offset"

  Scenario: offset at the limit
    Given "ann" followed "carl" at 09:00 and "bob" at 10:00
    When a visitor reads the followers of "carl" with limit 1 and offset 0
    Then the reply holds only "bob" with profilesCount 2

  Scenario: offset above the limit
    Given "ann" followed "carl" at 09:00 and "bob" at 10:00
    When a visitor reads the followers of "carl" with limit 1 and offset 1
    Then the reply holds only "ann" with profilesCount 2

  Scenario: offset past the end
    Given "ann" and "bob" follow "carl"
    When a visitor reads the followers of "carl" with offset 5
    Then the reply is an empty list with profilesCount 2
```
