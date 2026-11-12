# Answers of the product owner

- Store the code as typed, only trimmed. Do not change the case.
- An empty code (or only spaces) is refused with 400 and an error on `Code`.
- Existing duplicates in production were cleaned up last week; there are none.
- When two requests race, the loser gets the same 400 as a normal duplicate.
