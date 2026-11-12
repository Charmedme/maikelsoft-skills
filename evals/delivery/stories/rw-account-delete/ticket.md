# Delete my account

**Type:** story
**Status:** spec:ready

As a member, I want to delete my account and the content I made, so that my data is gone when I leave.

## Context

Members ask for this by email today, and support removes rows by hand. Privacy rules say that the data must be gone at once, and that the account must stop working at once.

## Acceptance criteria

1. `DELETE /api/user` with `{"user": {"password": "..."}}` deletes the account of the signed-in member. The reply is 204.
2. A wrong or missing password is refused with 422 and an error on `password`. Nothing is deleted.
3. After the delete, the account cannot be used: each request with a token that was issued to the deleted account is refused with 401. Signing in with the old email and password fails.
4. The articles of the member are deleted, with all comments on them.
5. The comments that the member wrote on other articles are deleted.
6. The favorites of the member are removed, so the `favoritesCount` of the other articles goes down.
7. The member no longer follows anybody, and nobody follows the member.
8. `GET /api/profiles/{username}` of the deleted member gives 404.
9. A new member can register with the same user name or email after the delete. The new member starts with nothing: no articles, favorites, follows, or comments of the old account.
10. The tag list (`GET /api/tags`) shows only tags that are used by at least one article.

## Behaviour spec

```gherkin
Rule: R1 A signed-in member deletes the own account with the correct password (AC1)

  Scenario: correct password
    Given the member alice is signed in with the password secret-1
    When alice sends DELETE /api/user with {"user": {"password": "secret-1"}}
    Then the reply is 204

  Scenario: no token
    Given the member alice with the password secret-1
    When somebody sends DELETE /api/user with {"user": {"password": "secret-1"}} and no token
    Then the reply is 401

Rule: R2 A wrong or missing password is refused with 422 and deletes nothing (AC2)

  Scenario: wrong password
    Given the member alice is signed in with the password secret-1
    When alice sends DELETE /api/user with {"user": {"password": "wrong-1"}}
    Then the reply is 422 with an error on password

  Scenario: missing password
    Given the member alice is signed in
    When alice sends DELETE /api/user with {"user": {}}
    Then the reply is 422 with an error on password

  Scenario: a refused delete keeps the account
    Given the member alice is signed in with the password secret-1
    When alice sends DELETE /api/user with {"user": {"password": "wrong-1"}}
    Then GET /api/profiles/alice still gives 200

Rule: R3 The deleted account stops working at the next request (AC3)

  Scenario: old token on a page that needs sign in
    Given alice deleted her account
    When a request to GET /api/user carries the token that was issued to alice
    Then the reply is 401

  Scenario: old token on a page that needs no sign in
    Given alice deleted her account
    When a request to GET /api/articles carries the token that was issued to alice
    Then the reply is 401

  Scenario: the same page without a token works as before
    Given alice deleted her account
    When a visitor sends GET /api/articles without a token
    Then the reply is 200

  Scenario: sign in with the old email and password
    Given alice deleted her account with the email alice@example.test and the password secret-1
    When somebody signs in with alice@example.test and secret-1
    Then the sign in fails with 401

Rule: R4 The articles, comments and profile of the member are gone (AC4, AC5, AC8)

  Scenario: article of the member
    Given alice wrote the article "how-to-train-your-dragon"
    When alice deletes her account
    Then GET /api/articles/how-to-train-your-dragon gives 404

  Scenario: comment of another member on an article of the member
    Given bob wrote a comment on the article "how-to-train-your-dragon" of alice
    When alice deletes her account
    Then GET /api/articles/how-to-train-your-dragon/comments gives 404

  Scenario: comment of the member on another article
    Given alice wrote a comment on the article "bobs-tips" of bob
    When alice deletes her account
    Then GET /api/articles/bobs-tips/comments does not have the comment of alice

  Scenario: profile of the member
    Given the member alice
    When alice deletes her account
    Then GET /api/profiles/alice gives 404

Rule: R5 The favorites and follows of the member are gone (AC6, AC7)

  Scenario: favorite on another article
    Given the article "bobs-tips" of bob has a favoritesCount of 1, from alice
    When alice deletes her account
    Then the favoritesCount of "bobs-tips" is 0

  Scenario: follow by the member
    Given alice follows bob
    When alice deletes her account
    Then bob has no follower alice

  Scenario: follow of the member
    Given bob follows alice
    When alice deletes her account
    Then bob no longer follows alice

  Scenario: a favorite on a deleted article disappears
    Given bob favorited the article "how-to-train-your-dragon" of alice
    When alice deletes her account
    Then GET /api/articles?favorited=bob does not have that article

Rule: R6 The tag list shows only tags that at least one article uses (AC10)

  Scenario: tag used only by an article of the deleted member
    Given only the article of alice has the tag dragons
    When alice deletes her account
    Then GET /api/tags does not have dragons

  Scenario: tag that another article still uses
    Given the article of alice and the article of bob have the tag training
    When alice deletes her account
    Then GET /api/tags still has training

  Scenario: author deletes the last article with a tag
    Given only the article "bobs-tips" of bob has the tag tips
    When bob deletes the article "bobs-tips"
    Then GET /api/tags does not have tips

  Scenario: author removes the last use of a tag from an article
    Given only the article "bobs-tips" of bob has the tag tips
    When bob edits "bobs-tips" and removes the tag tips
    Then GET /api/tags does not have tips

Rule: R7 A new member with the same user name or email starts with nothing (AC9)

  Scenario: register with the same user name and email
    Given alice deleted her account with the email alice@example.test
    When a new member registers with the user name alice and the email alice@example.test
    Then the reply is 201

  Scenario: the new member has none of the old content
    Given alice had articles, favorites, follows and comments, and deleted her account
    When a new member registers as alice and reads her own articles, favorites and follows
    Then she has no articles, no favorites, no follows and no comments

  Scenario: the old token does not work for the new account
    Given alice deleted her account and a new member registered as alice
    When a request to GET /api/user carries the token that was issued to the old account
    Then the reply is 401
```
