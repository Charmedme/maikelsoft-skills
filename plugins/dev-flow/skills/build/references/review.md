# Reviews

Each review is a new sub-agent that did not write the code and did not share your context. Read `review-rules.md` first: it gives what the reviewer gets, the level of each finding, and the review file `review.md`.

## Code review

| Size | Full passes | Round cap |
|------|-------------|-----------|
| Patch | 1 (quick: the diff and the test only) | 1 |
| Small | 1 | 2 |
| Medium | 1 | 3 |
| Large | 2 | 3 |

A pass is one reviewer that reads the full diff from the base. Large: the second pass is a new reviewer with the findings of the first pass. A round is a fix of the blocking findings and a new reviewer of the fix diff.

Give each reviewer:

- `spec.md`, `plan.md`, `decisions.md`, and the diff `git diff <base>...HEAD`,
- `review.md` with the findings of the earlier rounds and their status,
- the WATCH lines of `speclock.py check`, so it checks that no DSL check got weaker,
- `REVIEW.md` of the base (`git show "<base>:REVIEW.md"`), when it exists,
- these questions: Does each part of `spec.md` have its code and its check? Does the change follow the project guide and the code around it? Is there input without a check, an error without a handler, data that a change can lose, or a query in a loop?

Fix each blocking finding with a test first. Write each finding in `review.md`. Stop at the cap of the size, and give the options of `cycle.md`.

## Security review

| Size | When |
|------|------|
| Patch, small | When `riskcheck.py --root . --base <base>` gives `raise: yes` |
| Medium, large | Always |

Start a separate sub-agent. Give it the diff, `spec.md`, and the output of `riskcheck.py`. It loads the skill `dev-stack:secure-coding` when the session has it. Without dev-stack, give it a checklist at the level of the OWASP Top 10:

- access control and tenant isolation,
- injection,
- authentication and sessions,
- secrets in code or logs,
- unsafe deserialization and SSRF,
- file paths and uploads,
- crypto and vulnerable packages.

Then the report says: "The full secure-coding rules were not there. The review used a short checklist."

Fix each finding of Critical or High. At most 2 rounds. A finding of Critical or High that stays open stops the final gate.

## UX review

Only when the item has UI work. Read `ux-review.md` after the code review.
