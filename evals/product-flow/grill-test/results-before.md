# Grill test: results before the change (2026-10-16)

Skill: `idea-to-backlog` 1.1.0. 3 runs for each scenario. The number is the question that found the fact. "-" means not found.

| Run | Calls | N1 current work | N2 where it goes wrong | N3 exceptions | N4 groups of customers | N5 measure | I1 related module | I2 reporting | I3 roles | Need | Impact |
|-----|-------|----|----|----|----|----|----|----|----|------|--------|
| a-1 | 13 | 1 | - | - | - | 3 | - | - | - | 2/5 | 0/3 |
| a-2 | 17 | 1 | - | - | - | 3 | - | - | - | 2/5 | 0/3 |
| a-3 | 14 | - | - | - | - | 3 | - | - | - | 1/5 | 0/3 |
| b-1 | 19 | - | - | - | 4 | 5 | - | 5 | 4 | 2/5 | 2/3 |
| b-2 | 18 | - | - | - | 18 (G1) | 5 | - | 5 | 1 | 2/5 | 2/3 |
| b-3 | 18 | - | - | - | 16 (G1) | 3 | - | 3 | 1 | 2/5 | 2/3 |

## Findings

- N2 (where it goes wrong) and N3 (exceptions): 0 of 6. The skill asks which problem the solution solves, not where the user gets stuck today.
- N1 (current work): 2 of 3 without a problem statement, 0 of 3 with one. A stated problem stops the question about the current work.
- N4 (groups of customers): 0 of 3 without a problem statement. With one, 2 of 3 runs found it only at G1, when the summary said "all customers".
- I1 (related module): 0 of 6. I2 and I3 came up through questions about the measure and about who can use the page.

## Limits

- One product, a simulated product owner, and keyword matching. The matching can count a question that touches a topic by accident.
