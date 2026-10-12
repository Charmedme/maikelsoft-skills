---
status: accepted
date: 2025-03-02
decision-makers: Maikel
---

# Use SQLite for the label queue

## Context and Problem Statement
The label service needs a queue for print jobs. It runs on one server.

## Considered Options
- SQLite
- PostgreSQL

## Decision Outcome
Chosen option: "SQLite", because the service runs on one server and needs no extra database server.

### Consequences
- Good, because there is no database server to run.
- Bad, because two servers cannot share the queue.
