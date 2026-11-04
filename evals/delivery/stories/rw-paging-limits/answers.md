# Answers of the product owner

- `limit=100` is allowed. `limit=101` is refused.
- `offset=0` is allowed. An `offset` past the end gives an empty list with the full `articlesCount`.
- Refuse, do not silently change the value. Clients must learn the limit.
- The error format is the existing one: `{"errors": {"limit": [...]}}`.
- The feed still needs a signed-in reader. The 401 for a missing token comes before the check of the values.
