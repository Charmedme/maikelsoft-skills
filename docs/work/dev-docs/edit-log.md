# Edit log: dev-docs

Earlier versions have their results in `evals/dev-docs/`. This log starts with version 2.0.0.

| No | Date | Change | Test | Verdict |
|----|------|--------|------|---------|
| E1 | 2026-11-12 | Version 2.0.0. `doc-plan` becomes `update-docs` (the command `/update-docs`), and `doc-plan` forwards to it. The skill selects plan or audit mode itself, gives an outdated page the status "update", and ends with a `Next:` line. Decisions: `update-docs.md`. | Unit tests `tests/test_v7_ux_docs_names.py`. No eval run | Not tested in the delivery suite yet. Plan step 9 tests it |
| E2 | 2026-11-17 | Plan v7 step 9: a trigger check of `update-docs`. | `evals/dev-docs/triggers.json`: 5 start and 5 skip queries, 1 run each, Sonnet. Start 4 of 5: the query "Update the documentation of this project after the last release" starts no skill. Skip 5 of 5 | The skill triggers as designed. Keep the description |
