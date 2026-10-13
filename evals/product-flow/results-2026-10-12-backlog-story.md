# Test results: backlog-story

Date: 2026-10-12. Skill: `product-flow/backlog-story`, versions v1 and v2.

## Method

- 3 requests, each run 3 times for each variant, each run in a separate agent. Nobody could answer questions, so each agent took its own assumptions.
  - **dashboard** (short): "A user wants to see the status of the system inside the app on a dashboard."
  - **search** (detail): office supplies, 12,000 products, search by name, article number or brand, contract prices.
  - **login** (trap, Dutch): "Elke gebruiker moet inloggen ... Gebruik hiervoor Azure AD B2C met een login API." The request has a solution in it and the generic role "gebruiker".
- **Baseline:** the same request without the skill.
- **Check** assertions: a script decides. **Judge** assertions (J1 to J8): one grader agent for each run. The grader did not know which variant made the file.

## Results

| Assertion | Baseline | v1 | v2 |
|-----------|----------|----|----|
| Language checker (`doccheck.py`) 0 errors | 0/9 | 9/9 | 9/9 |
| No Given/When/Then | 4/9 | 9/9 | 9/9 |
| J1 Each story is vertical (no layer story) | 7/9 | 9/9 | 9/9 |
| J2 Each story has a specific role | 3/9 | 7/9 | 9/9 |
| J3 Each criterion is checkable | 2/9 | 7/9 | 8/9 |
| J4 Need, not solution | 5/9 | 9/9 | 9/9 |
| J5 Each story has an edge case or error | 0/9 | 9/9 | 8/9 |
| J6 Covers the core of the request | 9/9 | 9/9 | 8/9 |
| J7 Lean (no extra sections) | 4/9 | 9/9 | 9/9 |
| J8 2 to 8 stories, max 8 criteria each | 3/9 | 9/9 | 9/9 |
| Average length (words) | 1,534 | 552 | 531 |
| Trigger: must trigger (5 queries) | - | - | 5/5 |
| Trigger: must not trigger (5 queries) | - | - | 5/5 |

The trigger test ran each query once (Haiku, 8 skills to select from), not 3 times.

## Changes from v1 to v2

- **Role.** v1 used "signed-in app user" in 2 runs. v2: the rule and the checker also reject a word before "user" ("signed-in user", "app user").
- **Vague words.** v1 used "short", "main parts", "clear". v2: the checker warns for these words. The rule says to replace "the main parts" with a list or a question.
- **Checker bug.** v1 read the English "of" as the Dutch "of" (or). v2 uses the conjunctions of the language of the sentence.

## Observations

- The baseline wrote 7 to 11 stories with release plans, risk sections, and Given/When/Then. It also made technical stories ("B2C-tenant inrichten", "Login API controleert elke aanvraag").
- All skill runs kept Azure AD B2C as a constraint in the notes or a "Security:" criterion, and asked about the B2C end of sale.
- The 3 failures in v2 are single misses in different runs. One duration has no number, one run has no overall status, and one story has no error case. They show no pattern, so there is no v3.
