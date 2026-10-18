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

### Decisions to approve

<the table of decision-sheet.md: ID, question, recommendation, reason. Only the open rows.>

### New packages to approve

<name, version, license, reason, "exists: yes" per package, or "None.">

### Reflection

<the accepted improvements in one line each, and the rejected ones with the reason>

Answer "ok" to approve all, or name the rows that you change. The full design is in <path>.
```

After the answer:

- Write each answer into `decision-sheet.md` (column Answer, column By = human).
- An "ok" approves each recommendation in the sheet and each new package.
- A change to the design goes back to the senior (`mode: design`) one time. Then show G1 again.
- Write `G1 | approved by <name> on <date>` in `run.md` and a log row `G1_APPROVED` or `G1_CHANGED`.

With an answer sheet: take each answer from the sheet. A row that the sheet does not answer stops the run at G1.

## Gate G2

Write `g2.md`, then show it.

```markdown
## G2: <story title>

Result: <DONE / DONE_WITH_CONCERNS / BLOCKED> on branch <branch> (<N> commits since <base>)

| Check | Result |
|-------|--------|
| build, test, format (g2-checks.md) | <pass / fail per check> |
| runcheck.py | <0 findings / N findings> |
| decisioncheck.py | <0 findings / N findings> |
| security review | <verdict, findings per severity> |

### Acceptance criteria

| Criterion (verbatim) | Test or evidence |
|----------------------|------------------|

### Open findings

<each open finding: source file, severity, what to do, what to check. Or "None.">

### Commits

<git log --oneline base..HEAD>

Next step for you: review the branch, then push it and open the pull request.
```

The acceptance criteria table takes each criterion from `story.md` verbatim, and the test name or `path:line` from the reports. A criterion without a test is an open finding.
