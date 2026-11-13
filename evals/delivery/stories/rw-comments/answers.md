# Answers of the product owner

- A reader who is not the author and tries to remove a comment gets 403, and the comment stays.
- Adding, listing, or removing comments on an unknown article gives 404.
- Removing a comment that does not exist, or a comment that belongs to another article, gives 404.
- Comments are listed oldest first.
- A body of only spaces counts as "without text".
- Removing a comment returns 204 with an empty body, as in the RealWorld specification.
- Adding a comment returns 201 with the new comment, as in the RealWorld specification.
