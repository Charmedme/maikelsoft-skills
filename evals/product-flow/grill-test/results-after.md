# Grill test: results after the change (2026-10-16)

Skill: `idea-to-backlog` 1.2.0. The same 2 scenarios and the same simulated product owner as in `results-before.md`. 3 runs for each scenario.

| Run | Calls | N1 | N2 | N3 | N4 | N5 | I1 | I2 | I3 | Need | Impact |
|-----|-------|----|----|----|----|----|----|----|----|------|--------|
| a-1 | 22 | 1 | 2 | 3 | 4 | 5 | 15 | 13 | 13 | 5/5 | 3/3 |
| a-2 | 20 | 1 | 2 | 3 | 4 | 5 | - | - | 13 | 5/5 | 1/3 |
| a-3 | 19 | 1 | 2 | 3 | 4 | 5 | 8 | 12 | 13 | 5/5 | 3/3 |
| b-1 | 21 | 1 | 2 | 3 | 4 | 5 | 14 | - | 13 | 5/5 | 2/3 |
| b-2 | 20 | 1 | 2 | 3 | 4 | 5 | 16 | 18 | 13 | 5/5 | 3/3 |
| b-3 | 21 | 1 | 2 | 3 | 4 | 5 | 9 | - | 11 | 5/5 | 2/3 |

## Before and after

| Measure | Before | After |
|---------|--------|-------|
| Need facts found (N1 to N5), all runs | 11 of 30 | 30 of 30 |
| Current work found with a problem statement in the document | 0 of 3 | 3 of 3 |
| Related module found (I1) | 0 of 6 | 5 of 6 |
| Impact facts found (I1 to I3) | 6 of 18 | 14 of 18 |
| Questions to the human, average | 16.5 | 20.5 |

## What changed in the result

- All 6 runs found the real problem: the buyers use their own article numbers. All 6 runs added a way to use these numbers. All 6 runs left out the EDI customers.
- 4 of 6 runs changed the solution definition after round 1, for example .xlsx in scope or a link to the existing article number list.

## Phase 5 (1 run)

- 19 items, 0 checker errors, 4 feature rounds, 5 open questions with an owner (PO 3, team 2).
- All items refer to an open question by its number. No criterion holds an open question.

## Limits

- The 5 questions of round 1 match the 5 need facts of the simulated product owner. The test was made from the same analysis as the change. So the score for N1 to N5 is a check that round 1 runs, not proof that it finds every need. The related module (I1) is not in round 1, and it is the fairer measure.
- The language of the questions is still not good. The question logs have 5 to 26 STE errors after the change, about as many as before.
- The change costs about 4 questions more for each run.
