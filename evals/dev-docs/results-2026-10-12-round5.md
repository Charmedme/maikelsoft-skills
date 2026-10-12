# dev-docs test results, round 5 (release 1.4.0)

This round tests the medium-priority items of the improvement analysis of 2026-10-12. The earlier round is in [results-2026-10-12-round4.md](results-2026-10-12-round4.md).

## Method

- Each run had its own agent, with 3 runs for each variant. Nobody could answer questions.
- `score.py` scored the `check` assertions.
- The author scored the `judge` assertions from the output files and the replies. This round used no grader agent.
- Three new evals: `changelog-entry-commits` (20), `doc-comments-deprecated` (21), and `ui-text-placeholders` (22). Five evals got new assertions: 1, 13, 14, 15, and 17.
- The new checker rules (`A11Y-ALT`, `A11Y-LINK`, `A11Y-HEADING`, and the new `HOUSE-WORDS`) have unit tests.

The variants:

- **v5main**: release 1.3.0 (the `main` branch).
- **v6**: release 1.4.0.

## Results: checks

| Eval | Items | v5main | v6 |
|------|-------|--------|----|
| readme-from-code | R1, R3 | 6/6 | 6/6 |
| reference-api-errors | F2 | 13/18 | 18/18 |
| explanation-from-code | E1, E2 | 6/6 | 6/6 |
| adr-supersedes | D2 | 18/18 | 18/18 |
| runbook-from-alert | A4 | 18/18 | 18/18 |
| changelog-entry-commits | C2, C3, C4 | 18/18 | 18/18 |
| doc-comments-deprecated | K1, K2 | 6/9 | 6/9 |
| ui-text-placeholders | U3 | 12/12 | 12/12 |

## Results: judge assertions

| Eval | Assertion | v5main | v6 |
|------|-----------|--------|----|
| changelog-entry-commits | The reply reports the removal without an earlier deprecation | 0/3 | 3/3 |
| explanation-from-code | The architecture part starts with the context | 1/3 | 2/3 |
| ui-text-placeholders | The translator notes mention longer German text or a length limit | 1/3 | 2/3 |
| doc-comments-deprecated | The reply reports the key in the comment | 3/3 | 3/3 |
| doc-comments-deprecated | The agent changed no code line | 3/3 | 3/3 |
| readme-from-code | The agent ran each quick-start command, or marked it `TODO: verify` | 3/3 | 3/3 |

## Findings

- **Clear effect on two skills.** The reference now has sections for authentication and rate limits in each run (F2). The changelog skill now reports a removal without a deprecation in each run (C3).
- **Small effect on two skills.** The C4 order in the explanation and the translation note in the UI text came in 2 of 3 runs. Before, they came in 1 of 3 runs.
- **No measurable effect on four items.** Release 1.3.0 already updated the ADR index (D2), marked a release as `[YANKED]` (C2), used `@deprecated` (K1), and used named placeholders with plural forms (U3). The new rules make this behavior explicit. They do not change the result on this model.
- **The README status rule works as written.** No run invented a status or a maintainer. Each v6 run said that no file gives these facts and left out the line (R1).
- **A gap in K2.** No run of either variant added a doc comment for the module. The rule says what a module comment holds, but it does not say to add one. The assertion stays, and the result is 0/3 for both variants. This is an open question for the next release.
- **No new checker warnings in the outputs.** The 30 output files of this round have no `A11Y` warning.

## Decision

Release 1.4.0. Decide for the next release if `doc-code-comments` must add a module comment when a file has none.

## Limits

- The author scored the judge assertions. The author also wrote the skills.
- 3 runs for each variant is a small sample. A difference of 1 run in 3 is not a strong signal.
- The checker rules for accessibility are heuristics. They can give false warnings, for example for a decorative image.
