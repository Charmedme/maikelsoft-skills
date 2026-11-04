# Delete my account

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
