# Testing a skill

Test before you write long text. A skill without a test is a guess.

## Contents

- What to test
- Prompt tests
- Assertions
- Trigger tests
- Compare versions
- Results table

## What to test

Two things: does the skill make the output better, and does it trigger at the right time.

## Prompt tests

1. Write 3 prompts a real person types. Use different sizes: one short, one with detail, one with a trap (a missing fact, a bad input).
2. Run each prompt 3 times with the skill (variant A) and 3 times without it (baseline). One separate agent for each run. A separate agent has no memory of your hopes.
3. Use the same tools and the same input files for all runs.
4. When the human can answer, the agent must stop at the first question. When no human is there, the agent makes a choice and says which.

If the baseline is as good as the skill, the skill adds nothing. Cut it or change it.

## Assertions

Each assertion is a yes or no question about the output.

- **Check**: a script decides. Example: `skillcheck.py` shows 0 errors. Use scripts for everything that a script can decide.
- **Judge**: one grader agent decides with a written question. Example: "Did the agent ask all questions in one message?" The grader sees the output and the question only. It does not see which variant made the output.

Write 4 to 8 assertions per prompt. Avoid vague ones ("is good"). A passing result for a bad output means the assertion is weak: fix it.

## Trigger tests

1. Write 10 queries: 5 that must trigger the skill and 5 that must not. Make the "must not" queries close to the skill, not far away. Example: for `build-skill`, "write a README" is far. "Add a rule to my CLAUDE.md" is close.
2. Give a cheap agent (Haiku) the list of skill names and descriptions, and one query. Ask which skill it picks, or none.
3. Count correct picks. Run each query 3 times.
4. If a "must trigger" query fails, add the user's words from that query to the description. If a "must not" query triggers, make the description narrower.

## Compare versions

Change one thing between versions. Run the same prompts again. Compare the pass rate for each assertion, not only the total.

Stop when two versions in a row give the same result, or when all assertions pass 3 of 3.

## Results table

Show this table to the human:

| Assertion | Baseline | v1 | v2 |
|-----------|----------|----|----|
| Asks questions in one message | 0/3 | 2/3 | 3/3 |
| `skillcheck.py` 0 errors | 1/3 | 3/3 | 3/3 |
| Trigger: must-trigger queries | - | 4/5 | 5/5 |

Add one line for each change you made between versions and why.
