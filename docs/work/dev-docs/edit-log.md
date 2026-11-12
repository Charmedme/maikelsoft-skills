# Edit log: dev-docs

Earlier versions have their results in `evals/dev-docs/`. This log starts with version 2.0.0.

| No | Date | Change | Test | Verdict |
|----|------|--------|------|---------|
| E1 | 2026-11-12 | Version 2.0.0. `doc-plan` becomes `update-docs` (the command `/update-docs`), and `doc-plan` forwards to it. The skill selects plan or audit mode itself, gives an outdated page the status "update", and ends with a `Next:` line. Decisions: `update-docs.md`. | Unit tests `tests/test_v7_ux_docs_names.py`. No eval run | Not tested in the delivery suite yet. Plan step 9 tests it |
