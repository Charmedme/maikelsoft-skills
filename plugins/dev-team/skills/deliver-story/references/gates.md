# Gates

The team stops two times for the human. Only the main session talks to the human.

## Contents

- Gate G1
- Gate G2

## Gate G1

Show one message. Keep it short: the human reads the full files only when needed.

```markdown
## G1: <story title>

Design: <3 to 5 lines from the summary of design.md>
Core (senior): <one line>
Packages: <NN title (size), one line each, or "none: the senior builds all">
Mode: <light or full, with the reason. Light: no reflection and no separate security review. A later high risk check raises a light run.>

### Decisions to approve

<the table of decision-sheet.md: ID, question, recommendation, reason. Only the open rows.>

### Assumptions to confirm

<the rows of "Assumptions to confirm" in design.md: question, chosen default, other options. Or "None.">

### New packages to approve

<name, version, license, reason, "exists: yes" per package, or "None.">

### Reflection

<the accepted improvements in one line each, and the rejected ones with the reason>

Answer "ok" to approve all, or name the rows that you change. The full design is in <path>.
```

After the answer:

- Write each answer into `decision-sheet.md` (column Answer, column By = human).
- An "ok" approves each recommendation in the sheet, each assumption, and each new package.
- A change to the design goes back to the senior (`mode: design`) one time. Then show G1 again.
- "full" for a light run: write `Mode: full` in `run.md`, run the reflection (step 4), then show G1 again.
- Write the mode of `design.md` in the field Mode of `run.md`.
- Write `G1 | approved by <name> on <date>` in `run.md` and a log row `G1_APPROVED` or `G1_CHANGED`.

With an answer sheet: take each answer from the sheet. A row that the sheet does not answer stops the run at G1.

## Gate G2

Write `g2.md`, then show it.

```markdown
## G2: <story title>

### Warnings

<only points that need an action, one line each. Or "None.">
- <the RUN-HOOK finding>
- <the risk has no cover: why>
- <raised: "The design said light. The build touched <paths>. The section How to undo does not cover these paths.">
- <REVIEW.md changed: "A guide file changed. Read it.">
- <an older Critical or High security finding: file and problem>
- <CI parity "not known": the pipeline can run checks that did not run here>

Result: <DONE / DONE_WITH_CONCERNS / BLOCKED> on branch <branch>, at <hash> (<N> commits since <base>)
Test edits: <the line "test edits" of risk-2.txt>

### What changed and why

<3 lines from design.md. Add "CLAUDE.md changed" when it changed.>

What the checks do not cover: <one line>

| Check | Result |
|-------|--------|
| build, test, format (g2-checks.md) | <pass / fail per check> |
| runcheck.py | <0 findings / N findings> |
| decisioncheck.py | <0 findings / N findings> |
| security review | <verdict, findings per severity> |
| UX review | <verdict, findings per severity, or "no UI work"> |
| risk check (risk-2.txt) | <high or normal, each reason> |
| CI parity (g2-checks.md) | <"declared in conventions.md" / "not known"> |

### Acceptance criteria

| Criterion (verbatim) | Test or evidence |
|----------------------|------------------|

### Open findings

<each open finding of level issue or higher: source file, severity, what to do, what to check. You answer each one here. Or "None.">
Suggestions: <the number of open rows of level suggestion>
Nits: <the number of rows of level nitpick>

### Older problems

<one line for each older finding of level issue, then a count of the others. The list is in older-problems.md. Nobody fixed them in this story. Or "None.">

### Text for the pull request

Testing: <what ran, with the result>
Risk: <high or normal, the reason, the files to read first>
How to revert: <the steps, from "How to undo" in design.md. A schema or data change needs more than git revert.>

### Commits

<git log --oneline base..HEAD>

Next step for you: review the branch, then push it and open the pull request.
```

The hash is the last commit outside the run folder: `git log -1 --format=%h -- ':/' ':!/<run folder>'`. The tree must be clean, because the command ignores uncommitted code. A later commit needs a new report: `FOLLOWUP_DONE` shows the new hash. The risk check is a path and pattern check. Do not say that the change is safe.

The acceptance criteria table takes each criterion from `story.md` verbatim, and the test name or `path:line` from the reports. A criterion without a test is an open finding.
