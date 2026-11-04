# Answers of the product owner

- This is a hard delete. Keep no copy, no "deleted" flag.
- "At once" means the next request. A token that was issued before the delete must not work for anybody, also not for a new account that is made later.
- Without a token, `DELETE /api/user` gives 401.
- "Each request" includes the pages that need no sign-in: a request that carries the old token gets 401 there too, so the client learns at once that the session ended. Without a token, those pages work as before.
- The rule for the tag list (criterion 10) applies everywhere, also when an author deletes an article or removes a tag from an article. Tags are not shown when no article uses them.
- Other members who favorited a deleted article simply no longer see it. They get no message.
- No soft limit or waiting period. No email confirmation for now.
