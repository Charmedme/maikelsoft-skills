# dev-docs test results, round 4 (release 1.3.0)

This round tests the 10 high-priority items of the improvement analysis of 2026-10-12. It also adds the first eval for five skills that had no eval: tutorial, reference, explanation, ADR, and changelog. The earlier rounds are in [results-2026-10-08.md](results-2026-10-08.md), [round 2](results-2026-10-08-round2.md), and [round 3](results-2026-10-08-round3.md).

## Method

- Each run had its own agent, with 3 runs for each variant. Nobody could answer questions.
- `score.py` scored the `check` assertions. It has two new checks: `contains` and `file_contains`. An output can now be a glob pattern.
- The author scored the `judge` assertions from the output files and the replies. This round used no grader agent.
- 8 new evals (12 to 19), with new fixtures: `orders-api.yaml`, `changes-2.4.0.txt`, `orders-db-alert.yaml`, `adr-repo/`, and `audit-docs/`.

The variants:

- **v4main**: release 1.2.0 (the `main` branch).
- **v5**: release 1.3.0.

## Results: checks

| Eval | Skill | Item | v4main | v5 |
|------|-------|------|--------|----|
| tutorial-clean-up | `doc-tutorial` | T1 | 6/9 | 9/9 |
| reference-api-errors | `doc-reference` | F1 | 6/12 | 12/12 |
| explanation-from-code | `doc-explanation` | none (new eval) | 6/6 | 6/6 |
| adr-supersedes | `doc-adr` | D1 | 13/15 | 15/15 |
| release-notes-security | `doc-changelog` | C1 | 12/12 | 12/12 |
| runbook-from-alert | `doc-howto` | H1 | 6/15 | 15/15 |
| audit-grades | `doc-plan` | P1 | 3/9 | 9/9 |
| english-form-errors | `doc-ui-text` | U1, U2 | 12/15 | 15/15 |

## Results: judge assertions

| Eval | Assertion | v4main | v5 |
|------|-----------|--------|----|
| release-notes-security | The first sentence tells the reader to update because of the security fix | 0/3 | 3/3 |
| release-notes-security | The removed endpoint has an "Action required" section | 3/3 | 3/3 |
| adr-supersedes | The old ADR links to the new ADR | 1/3 | 3/3 |
| adr-supersedes | The new ADR has two options and a reason from the drivers | 3/3 | 3/3 |
| runbook-from-alert | The runbook tells the reader to ask the data team before the drop of the slot | 3/3 | 3/3 |
| audit-grades | The audit proposes to delete `installation-old.md` | 3/3 | 3/3 |
| audit-grades | The audit finds the wrong facts in `install.md` | 3/3 | 3/3 |
| tutorial-clean-up | One path, with real output after the steps | 3/3 | 3/3 |
| reference-api-errors | No invented default: a missing fact is a `TODO:` | 3/3 | 3/3 |
| english-form-errors | Each field error names the field and says what to do | 3/3 | 3/3 |

## Findings

- **Each high-priority item changed the output.** Each v5 run has the new sections and columns:
  - tutorial: "Clean up"
  - errors table: "Client action" and "Retry"
  - runbook: "Impact", "Diagnose", and "Last tested"
  - audit: ROT and grade
  - UI text: the hidden "Error:" prefix
- **Release 1.2.0 was already good at some items.** The model already put the advisory ID and the affected versions in the release notes, and it already set the old ADR to "superseded". 1.3.0 adds the update advice at the top of the release notes and the link from the old ADR to the new ADR.
- **The tests found a gap in 1.3.0.** Each v5 changelog run said that the release-notes template had no "Security" heading, but the rule asked for one. The template now has the heading. This change came after the test.
- **The 1.2.0 skill text had a conflict.** Two v4main ADR runs asked if they could change an accepted ADR, because the skill said "An accepted ADR does not change". The new rule says when the skill can change an accepted ADR.
- **Two checks were too weak at first.** The "Error:" check matched an element name in a 1.2.0 table, and the ADR check did not look for the link. The author made both checks stricter before the final score. The tables show the final score.
- **The explanation skill works.** The new eval passed in each run of both variants. Each run found the same source errors in the gron code.

## Decision

Release 1.3.0.

## Limits

- The author scored the judge assertions. The author also wrote the skills.
- 3 runs for each variant is a small sample.
- Each fixture tests one case. For example, the runbook eval has one alert and the audit eval has four pages.
