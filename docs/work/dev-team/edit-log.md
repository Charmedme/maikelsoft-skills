# Edit log: dev-team

This log records each change to a skill, an agent, or a script of the plugin, the test that measured it, and a verdict.

## Edits

### E1: the field report of 2026-10-28
- Date and version: 2026-10-28, dev-team 0.5.0
- Change: the 10 plugin points of the field report.
  - The entry point and the work after G2 changed. A new skill handles review comments.
  - The senior looks for sibling patterns and aggregates. The project declares its CI checks.
  - A mock-up counts as a design source. The hook has a fallback command.
  - A missing command log gives one finding. `decisioncheck` reads added lines and finds duplicate IDs.
  - The build commands are in one place. A run moves with the branch.
- Scope: the points of one project are not in the plugin. The sort is in `field-report-2026-10-28.md`.
- Test: `tests/test_devteam.py` (26 new tests) for `decisioncheck.py`, `runcheck.py`, `checks.py`, and the hooks. The language checker on each changed Markdown file. No run of the team on a story.
- Observed: the scripts and hooks pass their tests. The fallback command was tested on Linux only. The Windows case is not tested.
- Regression run (story `get-order`, team arm, Opus, 2 runs for each version, same machine). Old 0.4.0: 9/9 and 9/9, $4.34 and $4.14, 2.7 and 2.0 minutes. First 0.5.0: 7/9 and 7/9, both stopped at step 6 with `NEEDS_HUMAN`. The cause was a rule that I wrote: no temporary `global.json`. The repo pins an SDK that the machine does not have, and the old agents got past it with a temporary change. Fixed 0.5.0: 9/9 and 9/9, $3.95 and $4.30, 2.4 and 2.3 minutes, `runcheck.py` 0 findings.
- Not measured: `process-review`, the After G2 flow, and the CI checks section. The eval set has no story that uses them.
- Verdict: proposed
- Evidence: `docs/work/dev-team/field-report-2026-10-28.md`

### E2: the review flow
- Date and version: 2026-10-31, dev-team 0.6.0, dev-stack 1.3.0
- Change: Parts A to G of `docs/work/review-flow/design.md`, for `dev-team` and the security report. The `dev-flow` parts and Part J are not in this version.
- Test: `tests/test_riskcheck.py` and new tests in `tests/test_devteam.py`. A comparison on 4 scenarios, with 1 run for each arm, on Sonnet. See `docs/work/review-flow/comparison.md`.
- Observed: a nit in a comment no longer starts a change, and the round cap stops the loop. The hidden tests gave the same result. A raised story cost 68% more, and its security review found nothing. The nit rule did not hold in the fix pass of the core review.
- Verdict: confounded. One test covers all the parts.
- Evidence: `docs/work/review-flow/comparison.md`
