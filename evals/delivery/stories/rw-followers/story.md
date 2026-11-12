# Followers of a profile

As a member, I want to see who follows a profile, so that I can find people with the same interests.

## Acceptance criteria

1. `GET /api/profiles/{username}/followers` returns `{"profiles": [...], "profilesCount": n}`: the members who follow this profile.
2. The most recent follower comes first.
3. Each profile has `username`, `bio`, `image`, and `following`: whether the current reader follows that member. Without a token, `following` is false.
4. An unknown user name gives 404.
5. Anybody can read the list, also without a token.
