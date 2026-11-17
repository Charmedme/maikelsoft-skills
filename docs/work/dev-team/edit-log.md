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

## E3: cut and second test (2026-10-31)

- Change: after an independent review, the owner cut Origin, `REVIEW.md`, the nit cap, proposal rows, and option c of the round stop. The risk check moved to before the core review. New: the fix scope, remedy by reason, removed authorization lines, the check of G2, an advisory round count.
- Test: see `docs/work/review-flow/comparison.md`, part "Second test".
- Observed: the fix pass kept the migration in 3 of 3 runs, and the round stop fired only on its trigger. Bug finding gave no difference between the cut version and the held rules (10 of 10 each).
- Verdict: keep the cut. Each result comes from a small sample with fixtures that the author wrote.
- Evidence: `docs/work/review-flow/comparison.md`

### E3: the team mode of the dev-flow chain
- Date and version: 2026-11-12, dev-team 1.0.0 and dev-flow 3.1.0
- Change: step 7 of plan version 7. `deliver-story` is the team mode of `dev-flow:build`, and `process-review` is the team mode of `dev-flow:review-comments`. The team uses the shared header of `run.md`, `spec.md` with the fingerprints, the lock of one test file for each rule, and the held-back tests. G1 is the plan gate and G2 is the final gate. The skills are out of the `/` menu. The session-start hook gives the route only without dev-setup.
- Fix pass: a review found gaps. One fix pass closed them: the release of dev-flow 3.1.0, old runs without a header, the docs step, and the hook. The decisions are in `team-mode-1.0.md`.
- Test: `tests/test_devteam.py` (new tests for `RUN-HEADER`, `RUN-SPEC`, an old run, the G2 rules table, and the hook with and without dev-setup), `tests/test_sync_shared.py`, `tests/test_readme_commands.py`. The checkers `skillcheck.py` and `doccheck.py` on each changed skill and Markdown file. No run of the team on an item.
- Observed: the scripts and hooks pass their tests. The behaviour of the skills is not measured.
- Verdict: Not tested in the delivery suite yet. Plan step 9 tests it.
- Evidence: `docs/work/dev-team/team-mode-1.0.md`

### E4: the evals of the team mode (plan v7 step 9)
- Date and version: 2026-11-17, dev-team 1.0.0 with dev-flow 3.1.0
- Change: no change to a skill. The dev-team evals and the review-flow evals now give the team the ticket with the Behaviour spec. The review-flow scenario `recur` starts with 2 earlier rounds. That is under the round limit, so only the recurring bug can stop the run.
- Test:
  - The dev-team eval set, team arm, 8 stories, 2 runs each, Opus.
  - The review flow, 0.6.0 against 1.0.0, 4 scenarios with 1 run each, and `recur` with 2 runs each, Sonnet.
  - The delivery suite, 5 large stories, 2 runs for each arm, Sonnet. The arms: `flowspec` (dev-flow 2.1.0 with the spec), `buildspec` (dev-flow 3.1.0 alone), and `team`.
  - A blind grading of the code of `team` against `buildspec`: 10 pairs, 2 graders, with the labels A and B changed for the second grader.
  - The trigger queries of `build` and `deliver-story`: 5 start and 5 skip queries each.
- Observed:
  - dev-team evals: each check passes in 16 of 16 runs. No run stops early. A run costs $9.80 and takes 30 minutes on average.
  - Review flow: `story` gives the same checks in both versions, for $2.64 against $1.64. In `review`, 1.0.0 fixes the 3 blocking comments, then stops on a question about the migration, before the 2 nits. In `cap`, 1.0.0 stops as designed. In `recur`, 1.0.0 stops for the recurring bug in 2 of 2 runs and gives the three options. Version 0.6.0 stops only at its round limit.
  - Delivery suite, hidden tests: `flowspec` 256/256 (10 of 10 runs green, $1.48 and 16 minutes a run, 0.6 agents). `buildspec` 255/256 (9 of 10, $2.56, 21 minutes, 2.5 agents). `team` 256/256 (10 of 10, $8.98, 51 minutes, 9.3 agents). No run read the hidden tests.
  - Blind grading: the two graders agree on 10 of 10 pairs. The team is better in 10 of 10 (sign test p = 0.002). The hidden tests did not find the defects of `buildspec`:
    - A field of acceptance criterion 2 is missing in the list items. A check of the code confirms it.
    - An existing end-to-end test breaks. A check of the code confirms it.
    - A race gives 500 in place of 409 on SQL Server.
    - The import loads a full table in memory.
    - A test driver can delete the developer database.
  - The defects of `team` are minor. An imported code is not trimmed, a delete and an edit can race, and two responses give 404 in place of 401.
  - Triggers: 10 of 10 for `build` and 10 of 10 for `deliver-story`.
- Verdict: helped. The team mode makes better code than the solo build on large items, and it costs 3.5 times as much. Keep it for large or risky items. The sample is small: 5 stories on one model. The owner decides about the default of the team route.
- Evidence: this entry. The run folders are local and not in the repository.
