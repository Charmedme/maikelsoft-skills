# Test results: backlog-feature

Date: 2026-10-12. Skill: `product-flow/backlog-feature`, versions v1 and v2. The skill calls `backlog-story` for the stories.

## Method

- The same 3 requests as for `backlog-story` (dashboard, search, login in Dutch), with "write the feature with its user stories". 3 runs for each variant, each in a separate agent. Nobody could answer questions.
- Grading as for `backlog-story`: J1 to J8 on the stories, and G1 to G4 on the feature. One blind grader for each run.

## Results

| Assertion | Baseline | v1 | v2 |
|-----------|----------|----|----|
| Language checker 0 errors | 0/9 | 9/9 | 9/9 |
| `backlogcheck.py` 0 errors | - | 9/9 | 9/9 |
| J1 Vertical stories | 6/9 | 9/9 | 9/9 |
| J2 Specific role | 6/9 | 9/9 | 9/9 |
| J3 Checkable criteria | 5/9 | 7/9 | 9/9 |
| J4 Need, not solution | 5/9 | 9/9 | 9/9 |
| J5 Edge case or error in each story | 0/9 | 7/9 | 9/9 |
| J6 Covers the core of the request | 9/9 | 7/9 | 9/9 |
| J7 Lean | 7/9 | 9/9 | 9/9 |
| J8 2 to 8 stories | 8/9 | 9/9 | 9/9 |
| G1 Goal with a measure, no invented target | 0/9 | 9/9 | 9/9 |
| G2 Scope in and out | 6/9 | 9/9 | 9/9 |
| G3 Feature-level criteria, not copies | 6/9 | 8/9 | 8/9 |
| G4 Each scope result has a story | 7/9 | 9/9 | 9/9 |
| Average length (words) | 1,548 | 996 | 920 |
| Trigger: must trigger (5 queries) | - | - | 5/5 |
| Trigger: must not trigger (5 queries) | - | - | 5/5 |

## Chain compared with the direct skill

In v1, the stories in the chain were weaker than the stories of `backlog-story` alone (J5 7/9 against 8/9, J6 7/9 against 8/9). The agent read only `SKILL.md` of `backlog-story`, not its rules.

## Changes from v1 to v2

- Step 4 tells the agent to read `story-rules.md` of `backlog-story` too, and to keep an edge case in each story.
- Rule F4a: each thing that the request names is "In", or "Out" with a reason. In v1, 2 dashboard runs had no overall status.
- The checker warns (A6) when no criterion of a story has a condition word (when, if, no, not, error).

With v2, the stories in the chain pass each story assertion 9/9.
