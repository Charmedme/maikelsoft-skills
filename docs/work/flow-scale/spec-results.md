# Results: spec-first flow and the model matrix (2026-10-24)

Test of the spec-first dev-flow (`executable-specs-concept.md`) against dev-flow 2.0 and the baseline, on Opus 5.5 and on Sonnet 5.5. Fixture: orders-api. Headless runs with an answer sheet, 3 runs for each cell.

## The cases

- ORD-50, a discount code: 7 criteria with examples. A schema change.
- ORD-52, a CSV export: 6 criteria with examples.
- Each case has two groups of hidden probe tests. The E tests check the examples of the item (6 and 4 tests). The H tests check the rules with values that no example gives (4 and 4 tests). They test rounding half away from zero, the end of the last valid day in UTC, and a stored discount after a restart. They also test a Dutch server culture, a doubled quote, and formula injection with a comma.
- A reference solution passes 18 of 18. A naive solution (default rounding, the culture of the server, no quote escaping) fails 6.

## Result

Checks and median cost for each run. Each arm passes the build, the tests, the package policy, and each E test and each H test, in each run. The spec-first arm has two extra checks: the tier and the lock (9 checks against 7).

| Case | Model | Baseline | dev-flow 2.0 | Spec-first |
|------|-------|----------|--------------|------------|
| ORD-50 | Opus | 7/7 x3, $0.73 | 7/7 x3, $3.23 | 9/9 x3, $3.21 |
| ORD-52 | Opus | 7/7 x3, $0.77 | 7/7 x3, $3.78 | 9/9 x3, $2.77 |
| ORD-50 | Sonnet | 7/7 x3, $0.26 | 7/7 x3, $0.75 | 9/9 x3, $0.81 |
| ORD-52 | Sonnet | 7/7 x3, $0.24 | 7/7 x3, $0.65 | 9/9 x3, $0.42 |
| order-mail (must stop) | Opus | not run | not run | stops 3/3, $1.05 |

- **Correctness.** No arm and no model fails a hidden test. The examples on the item were enough for each arm. These cases do not show a gain of the flows.
- **Model.** Sonnet gives the same result as Opus at about one third of the cost, in each arm. The spec-first flow on Sonnet costs about the same as the baseline on Opus. In the spec-first runs, Opus made 47 main calls and Sonnet 20, for the same work.
- **Process cost.** The spec-first flow costs 1.6 to 3 times the baseline on Sonnet, and 3.5 to 4 times on Opus.
- **Spec-first against dev-flow 2.0.** The same cost on ORD-50, and lower on ORD-52 (27% on Opus, 35% on Sonnet). The lean small path (no `plan.md`) is the likely cause, but this test does not isolate it.
- **The lock.** Each spec-first run locked its test cases, and `speclock.py check` passed at the end. One Opus run hit a false block of the hook: a commit message named a locked file. Fixed after the first 3 runs. The Sonnet runs and the later Opus runs used the fixed hook.
- **The Sonnet reviewer.** In the Opus spec-first runs, the review sub-agent ran on Sonnet, as the skill says.

## Conclusions

1. On stories with clear examples, Claude Code with Opus or Sonnet writes correct code without a workflow. Neither flow made the code more correct in any of our 8 cases so far.
2. The model is the largest cost lever: Sonnet costs one third of Opus here, at the same result. An independent benchmark (Artificial Analysis) found the reverse at a matched score on harder tasks. So this result holds for this kind of work, not in general.
3. The flows give value that the hidden probes do not measure. They stop when a fact of the human is missing. They keep approved tests that the agent cannot change. They keep a record of the decisions. The team decides if that value is worth 1.6 to 4 times the cost. This test cannot decide it.
4. Synthetic stories do not separate the arms any more. The next useful test uses real items from a real backlog. The probes are the tests that the team wrote later.

## Gaps

- The judge assertion J10 (an acceptance test for each example) is not graded.
- 3 runs for each cell. With 0 failures in each arm, the test cannot show a difference.
- The baseline still loads the account plugins and skills of this machine. It did not call them in the runs that were checked.
- The trap case ran only with the spec-first flow on Opus.
