# Run files

All files of one story are in the run folder `docs/work/<slug>/`. The files are the memory of the team: a new agent can continue from them.

## Contents

- Folder
- run.md
- log.md
- Status words

## Folder

| File | Written by | Step |
|------|-----------|------|
| `run.md` | main | each step |
| `log.md` | each agent | each step |
| `story.md` | main (story-intake) | 2 |
| `design.md` | senior | 3, 4 |
| `decision-sheet.md` | senior, then main (answers) | 3, 5 |
| `packages/NN-<slug>.md` | senior | 3 |
| `reflection.md` | reflector senior | 4 |
| `core-report.md` | senior | 6 |
| `core-review.md` | reflector senior | 6 |
| `packages/NN-plan.md`, `NN-report.md`, `NN-checks.md` | medior | 7 |
| `packages/NN-plan-feedback.md`, `NN-review.md` | senior | 7 |
| `security-review.md` | security reviewer | 8 |
| `g2-checks.md`, `g2.md` | main | 9 |
| `commands.jsonl` | the plugin hook | each Bash command |
| `follow-ups/NN-comments.md`, `NN-replies.md`, `NN-review.md` | main (process-review), senior | After G2 |

## run.md

```markdown
# Run: <slug>

| Field | Value |
|-------|-------|
| Story | <title> (<ticket ID or "pasted">) |
| Step | <1 to 9> <step name> |
| Status | <last status word> |
| Branch | dev-team/<slug> |
| Base | <commit hash at the start> |
| G1 | open / approved by <name> on <date> / changed on <date> |
| G2 | open / shown on <date> |
| Answer sheet | <path> / none |
| Mode | full / light (from design.md, as approved at G1) |

## Counters

| Counter | Limit | Value |
|---------|-------|-------|
| design-reflection | 1 | 0 |
| core-review | 1 | 0 |
| core-fix | 1 | 0 |
| security-review | 1 | 0 |
| security-fix | 1 | 0 |
| ux-review | 1 | 0 |
| ux-fix | 1 | 0 |
```

After G2, each follow-up adds two rows: `followup NN fix` and `followup NN review`. Each has limit 1. An example of a follow-up is the comments of a pull request review. A second round is a new follow-up with the next number.

The senior adds three rows for each package when it starts the package: `NN plan-feedback`, `NN review`, and `NN fix`. Each has limit 1. Only the agent of a step changes the counter of that step. `runcheck.py` reads the counters.

## log.md

```markdown
# Log: <slug>

| Time | Agent | Step | Status | Files | Commit | Checks | Decisions | Assumptions | Reason |
|------|-------|------|--------|-------|--------|--------|-----------|-------------|--------|
```

One row per step and agent. Rules:

- Time: UTC, `HH:MM`, from `date -u +%H:%M` at the moment that you write the row. Do not write the time from memory. Agent: `main`, `senior`, `reflector`, `medior`, `medior-large`, or `security-reviewer`.
- Step: the step name. Package steps start with the package number: `01 plan`, `01 plan-feedback`, `01 build`, `01 review`, `01 fix`.
- Status: one status word from the list below.
- Files: the files that the step wrote. Commit: the short hash of each commit of the step (`git log --format=%h`), or `-`. Write the row after the commit, so that the hash exists.
- Checks: the result of each check that the agent ran in this step, for example `build pass, test pass, format pass`. Write `-` when the step ran no check. A check that you did not run after your last change is "not run".
- Decisions: decision IDs that the step used or opened. Assumptions: what the step assumed.
- Reason: one sentence. Why the step ended with this status.

The hook writes each Bash command to `commands.jsonl`. `runcheck.py` fails a row that says a check passed when that agent ran no check command.

## Status words

| Step | Status words |
|------|--------------|
| main | `STARTED`, `G1_APPROVED`, `G1_CHANGED`, `G2_SHOWN`, `FOLLOWUP_STARTED`, `FOLLOWUP_DONE`, `NEEDS_HUMAN`, `BLOCKED` |
| design, apply-reflection | `DESIGN_READY`, `NEEDS_HUMAN`, `BLOCKED` |
| reflect | `REFLECTION_READY` |
| build-core | `CORE_DONE`, `CORE_DONE_WITH_CONCERNS`, `NEEDS_HUMAN`, `BLOCKED` |
| review-core, NN review | `APPROVED`, `APPROVED_WITH_COMMENTS`, `CHANGES_REQUIRED` |
| fix | `FIX_DONE`, `BLOCKED` |
| packages | `PACKAGES_DONE`, `PACKAGES_DONE_WITH_CONCERNS`, `BLOCKED` |
| NN plan | `PLAN_READY`, `NEEDS_CONTEXT`, `BLOCKED` |
| NN plan-feedback | `PLAN_APPROVED`, `PLAN_APPROVED_WITH_CHANGES` |
| NN build, NN fix (medior) | `DONE`, `DONE_WITH_CONCERNS`, `NEEDS_CONTEXT`, `BLOCKED` |
| security-review | `PASS`, `PASS_WITH_FINDINGS`, `BLOCKED` |

Each step file has one line `Status: <WORD>` under its title. The security review uses `Verdict: <WORD>`.
