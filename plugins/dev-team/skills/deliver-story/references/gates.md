# Gates

The team stops two times for the human. G1 is the plan gate of `dev-flow:build`, and G2 is its final gate. Only the main session talks to the human.

## Contents

- Gate G1
- Gate G2

## Gate G1

Show one message. Keep it short: the human reads the full files only when needed.

```markdown
## G1: <item key>: <title> (plan gate)

Size: <the size line, with the risk>
Design: <3 to 5 lines from the summary of design.md>
Core (senior): <one line>
Packages: <NN title (size), one line each, or "none: the senior builds all">
Mode: <light or full, with the reason. Light: no reflection and no separate security review. A later risk check with `raise: yes` raises a light run.>

### Scenarios

<the table "## Checks" of spec.md: each rule R<n> with its test file. Mark each high-risk rule "high risk: the lock asks you again".>

### Decisions to approve

<the table of decision-sheet.md: ID, question, recommendation, reason. Only the open rows.>

### Assumptions to confirm

<the rows of "Assumptions to confirm" in design.md: question, chosen default, other options. Or "None.">

### New packages to approve

<name, version, license, reason, "exists: yes" per package, or "None.">

### Reflection

<the accepted improvements in one line each, and the rejected ones with the reason>

Answer "ok" to approve the plan and the list of scenarios, or name the rows that you change. The full design is in <path>.
```

After the answer:

- Write each answer into `decision-sheet.md` (column Answer, column By = human).
- An "ok" approves each recommendation in the sheet, each assumption, and each new package. It also approves the list of scenarios, and the lock of each rule that is not high risk. The senior locks these test files with `--by "<name> (G1)"`.
- A high-risk rule is a rule that the risk check raises, or a rule that a risk of the size table touches. These risks are a schema change, sign-in, authorization, secrets, money, and a deletion of data. Its lock asks the human again in step 7, with the test file.
- A change to a rule of the spec is a product question. It goes to the PO, not to the design.
- A change to the design goes back to the senior (`mode: design`) one time. Then show G1 again. Scope that the human adds gets its own row in the acceptance criteria table of `design.md`, with a test.
- "full" for a light run: write `Mode: full` in `run.md`, run the reflection (step 4), then show G1 again.
- Write the mode of `design.md` in the field Mode of `run.md`.
- Write `G1 | approved by <name> on <date>` in `run.md` and a log row `G1_APPROVED` or `G1_CHANGED`.

With an answer sheet: take each answer from the sheet. A row that the sheet does not answer stops the run at G1.

The sheet also approves the lock of a high-risk rule. Without a human and without a sheet, a lock can be "assumed". This is only true when each test case checks one scenario of the spec, word for word. The senior writes `--by "assumed (spec of the ticket)"`. Else the run stops before the lock.

## Gate G2

Write `g2.md`, then show it.

```markdown
## G2: <item key>: <title> (final gate)

### Warnings

<only points that need an action, one line each. Or "None.">
- <the RUN-HOOK finding>
- <the risk has no cover: why>
- <raised: "The design said light. The build touched <paths>. The section How to undo does not cover these paths.">
- <gone reason: "<kind> <path> went away after the build. Check that this is what you want.">
- <migration, new package, or major version: "<kind> <path>. How to undo: <the steps>.">
- <test edits since the last check: "A fix changed a test: <path>. Read it.">
- <a guide file changed: "<path> changed. Read it.">
- <CI parity "not known": the pipeline can run checks that did not run here>
- <a run with baseline: legacy: "This run started before dev-team 1.0.0. It has no locked tests and no held-back tests.">
- <docscheck.py findings that are still open: "<doc> can be out of date.">

Result: <DONE / DONE_WITH_CONCERNS / BLOCKED> on branch <branch>, at <hash> (<N> commits since <base>)
Test edits: <the line "test edits" of risk-2.txt>

### What changed and why

<3 lines from design.md. Add "CLAUDE.md changed" when it changed.>

What the checks do not cover: <one line>

| Check | Result |
|-------|--------|
| locked tests (speclock.py check) | <n files, all pass, 0 changes> |
| held-back tests (heldback.py run) | <passed / total, or "not for a legacy run"> |
| docs (docscheck.py) | <0 findings / N findings, each doc with --ok and its reason> |
| build, test, format (g2-checks.md) | <pass / fail per check> |
| runcheck.py | <0 findings / N findings> |
| decisioncheck.py | <0 findings / N findings> |
| security review | <verdict, findings per severity> |
| UX review | <verdict, findings per severity, or "no UI work"> |
| risk check (risk-2.txt) | <high or normal, each reason> |
| CI parity (g2-checks.md) | <"declared in conventions.md" / "not known"> |

### Rules

| Rule (verbatim) | Locked test |
|-----------------|-------------|

### Open findings

<each open finding of level issue or higher: source file, severity, what to do, what to check. You answer each one here. Or "None.">
Suggestions: <the number of open rows of level suggestion>
Nits: <the number of rows of level nitpick>

### Older problems

<one line for each older finding of level issue, then a count of the others. The list is in older-problems.md. Nobody fixed them in this item. Or "None.">

### Text for the pull request

Testing: <what ran, with the result>
Risk: <high or normal, the reason, the files to read first>
How to revert: <the steps, from "How to undo" in design.md. A schema or data change needs more than git revert.>

### Commits

<git log --oneline base..HEAD>

Next step for you: review the branch, then push it and open the pull request.
```

The hash is the last commit outside the work folder: `git log -1 --format=%h -- ':/' ':!/<work folder>'`. The tree must be clean, because the command ignores uncommitted code. Keep the headings of the template as written, because `runcheck.py --stage report` reads them. A later commit needs a new report: `FOLLOWUP_DONE` shows the new hash. The risk check is a path and pattern check. Do not say that the change is safe.

The rules table takes each rule R<n> from the spec block of `spec.md` verbatim, and its locked test file from the section "## Checks". A chore or a task takes each part of "## Checks". A rule without a locked test that passes is an open finding. An old run with `story.md` uses the table `| Criterion (verbatim) | Test or evidence |` with the criteria of `story.md`.
