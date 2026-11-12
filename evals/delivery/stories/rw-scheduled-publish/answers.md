# Answers of the product owner

- The clock of the server decides. The clock of the client does not matter.
- "From the publish time on" includes the exact moment: at `publishAt` the article is visible.
- An author can move the publish time with an update, also back into the future for an article that is already visible. The article is then hidden again until the new time.
- A `publishAt` that is not a valid date and time is refused with 422 and an error on `publishAt`.
- A date and time without an offset (no `Z`, no `+01:00`) is refused with 422. We do not guess the time zone.
- The list order stays "newest created first".
