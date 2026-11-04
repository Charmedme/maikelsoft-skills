# Answers of the product owner

- Marking an article twice is not an error. The count stays the same, and the reply is 200 with the article.
- Removing a mark that does not exist is not an error. The reply is 200 with the article.
- An author can mark the own article.
- The `favorited` filter combines with the other filters (tag, author) and with `limit` and `offset`. The `articlesCount` is the number of matching articles, not the page size.
- An unknown user name in the `favorited` filter gives an empty list, not an error.
