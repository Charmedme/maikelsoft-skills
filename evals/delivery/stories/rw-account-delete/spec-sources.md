# Sources: rw-account-delete

Repo paths are under `repo/src/Conduit/` unless written in full.

## Rules

- R1 (AC1): `DELETE /api/user` with `{"user": {"password": "..."}}`, reply 204 — AC1. Without a token 401 — answers.md line 5. Member names, emails and passwords (alice, bob, secret-1) are test data.
- R2 (AC2): wrong or missing password 422 with an error on `password`, nothing deleted — AC2. Error body form `{"errors": {"password": [...]}}` — existing behaviour, Infrastructure/Errors/ErrorHandlingMiddleware.cs. Profile 200 for an existing member — existing behaviour of `GET /api/profiles/{username}`.
- R3 (AC3): old token refused with 401 on each request; sign in with old email and password fails — AC3. "At once" is the next request — answers.md line 4. Pages that need no sign in give 401 with the old token, and work as before without a token — answers.md line 6. Sign-in failure is 401 — existing behaviour, Features/Users/Login.cs (`credentials invalid`). `GET /api/articles` needs no sign in — Features/Articles/ArticlesController.cs.
- R4 (AC4, AC5, AC8): articles with all comments deleted — AC4; comments on other articles deleted — AC5; profile gives 404 — AC8. Hard delete, no copy and no flag — answers.md line 3. Missing article gives 404 — existing behaviour, Features/Articles/Details.cs; the comments of a missing article give 404 too, Features/Comments/List.cs.
- R5 (AC6, AC7): favorites removed, `favoritesCount` goes down — AC6; no follows either way — AC7. Others who favorited a deleted article no longer see it, no message — answers.md line 8 (the "no message" part has no observable step; the scenario checks that the article is gone from the favorited list).
- R6 (AC10): tag list only shows tags used by at least one article — AC10. Also when an author deletes an article or removes a tag — answers.md line 7. Current tag list returns all stored tags — Features/Tags/List.cs.
- R7 (AC9): register again with the same user name and email, starts with nothing — AC9. Register reply 201 — existing behaviour, Features/Users/UsersController.cs. Old token must not work for a later new account — answers.md line 4.
- No waiting period and no email confirmation — answers.md line 9: no scenario, nothing to observe.

## Gaps

- No source gives the error message text on `password` for wrong and missing; scenarios check only the field.
- No source says what happens to a second, concurrent request with the old token that is in flight during the delete; left out.
- No source says if the delete of a member with very many articles has a time limit; left out.
- No source gives the reply when the body has no `user` object at all; left out.
