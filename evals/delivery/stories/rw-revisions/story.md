# Revisions and safe edits of an article

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
