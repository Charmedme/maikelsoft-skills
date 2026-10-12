# Test results: backlog-epic and backlog-goal

Date: 2026-10-12. Skills: `product-flow/backlog-epic` (calls `backlog-feature`) and `product-flow/backlog-goal`, version v1.

## Method

- **Epic:** 2 requests, 3 runs for each variant. "account" asks for an epic for the self-service account of business customers. "tracking" is in Dutch and asks for an epic for shipment tracking with a delay message.
- **Goal:** 2 requests, 3 runs for each variant. "productgoal" is a product goal for fewer phone orders. "sprintgoal" is a sprint goal for 4 items, and one of the items is a bug fix that does not fit the goal.
- Nobody could answer questions. One blind grader for each run.

## Results: epic

| Assertion | Baseline | v1 |
|-----------|----------|----|
| Language checker 0 errors | 0/6 | 6/6 |
| X1 Outcome with a measure, no invented target | 5/6 | 6/6 |
| X2 Scope in and out, request covered | 6/6 | 6/6 |
| X3 2 to 6 features in delivery order | 5/6 | 6/6 |
| X4 Each feature has goal, scope, criteria | 5/6 | 6/6 |
| X5 Each feature gives user value | 4/6 | 6/6 |
| X6 Lean | 1/6 | 5/6 |

## Results: goal

| Assertion | Baseline | v1 |
|-----------|----------|----|
| Language checker 0 errors | 3/6 | 6/6 |
| Y1 One sentence, max 25 words | 5/6 | 6/6 |
| Y2 Outcome, not output | 4/6 | 6/6 |
| Y3 Measure or check | 6/6 | 6/6 |
| Y4 Supporting items, bug fix not in the goal | 6/6 | 6/6 |
| Y5 Time frame, or "proposal" for a sprint goal | 3/6 | 6/6 |
| Y6 Lean | 3/6 | 6/6 |
| Average length (words) | 351 | 140 |

## Triggers

10 queries (5 must trigger `backlog-epic` or `backlog-goal`, 5 must not), with 11 skills to select from: 10/10.

## Changes after v1

- `backlog-epic`: one run listed story titles that nobody asked for. Step 4 now says to leave out the "Stories" section of each feature.
- `backlog-goal`: rule PG1 now says to give a proposal for the target in the open question.

These are small text changes. They were not tested again.
