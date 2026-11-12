# Run files

All files of one item are in the work folder `docs/work/<item>/`. It is the same folder as the folder of a solo build of `dev-flow:build`. The files are the memory of the team: a new agent can continue from them.

## Contents

- Folder
- run.md
- log.md
- Status words

## Folder

| File | Written by | Step |
|------|-----------|------|
| `run.md` | main (`runheader.py` writes the header) | each step |
| `log.md` | each agent | each step |
| `ticket.txt` | main (story-intake, or `dev-flow:build` before the hand-over). The raw text of the ticket | 2 |
| `spec.md` | main (story-intake, `specsync.py copy`), then senior (section "## Checks") | 2, 3 |
| `spec-baseline.json` | `specsync.py copy`. The fingerprint of each rule | 2 |
| `story.md` | an old run from dev-team 0.6.0 only, in place of `spec.md` | - |
| `design.md` | senior | 3, 4 |
| `decision-sheet.md` | senior, then main (answers) | 3, 5 |
| `packages/NN-<slug>.md` | senior | 3 |
| `reflection.md` | reflector senior | 4 |
| `core-report.md` | senior | 7 |
| `core-review.md` | reflector senior | 7 |
| `packages/NN-plan.md`, `NN-report.md`, `NN-checks.md` | medior | 8 |
| `packages/NN-plan-feedback.md`, `NN-review.md` | senior | 8 |
| `risk-1.txt`, `risk-2.txt` | main (`riskcheck.py`) | 7, 13 |
| `security-review.md` | security reviewer | 9 |
| `g2-checks.md`, `g2.md` | main | 13 |
| `commands.jsonl` | the plugin hook | each Bash command |
| `older-problems.md` | main | 13, after G2. One row for each older finding |
| `follow-ups/NN-comments.md`, `NN-replies.md`, `NN-review.md`, `NN-risk.txt` | main (process-review), senior | After G2 |
| `follow-ups/NN-change.md` | main. Quotes a change that the human asks for | After G2 |

The test files of the rules are in the repository, and `.dev-flow/spec-lock.json` locks them. The held-back tests are outside the repository, in the data folder of dev-flow.

## run.md

`run.md` starts with the shared header of dev-flow and dev-team. `runheader.py` writes it. Do not write it by hand. The team body follows the header.

```markdown
---
flow: team
item: <ticket key>
size: <the size of dev-flow:build, else large>
step: <n> <step name>
baseline: <set after specsync.py copy, legacy for an old run>
---

# Run: <item>

| Field | Value |
|-------|-------|
| Item | <title> (<ticket key>) |
| Step | <1 to 13> <step name> |
| Status | <last status word> |
| Branch | <the branch rule of the project, else feat/<item> or fix/<item>> |
| Base | <commit hash at the start> |
| G1 | open / approved by <name> on <date> / changed on <date> |
| G2 | open / shown on <date> |
| Answer sheet | <path> / none |
| Mode | full / light (from design.md, as approved at G1) / raised (a light run with high risk: a security review and no reflection) |

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
| heldback-fix | 1 | 0 |
| docs-fix | 1 | 0 |
```

After G2, each follow-up adds two rows: `followup NN fix` and `followup NN review`. Each has limit 1. An example of a follow-up is the comments of a pull request review. A second round is a new follow-up with the next number.

The senior adds three rows for each package when it starts the package: `NN plan-feedback`, `NN review`, and `NN fix`. Each has limit 1. Only the agent of a step changes the counter of that step. `runcheck.py` reads the header and the counters.

## log.md

```markdown
# Log: <item>

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

The hook writes each Bash command to `commands.jsonl`. Before G2, `runcheck.py` fails a row that says a check passed when that agent ran no check command. After G2, it needs one `checks.py` command in each follow-up.

## Status words

| Step | Status words |
|------|--------------|
| main | `STARTED`, `G1_APPROVED`, `G1_CHANGED`, `RISK_CHECKED`, `G2_SHOWN`, `FOLLOWUP_STARTED`, `FOLLOWUP_DONE`, `NEEDS_HUMAN`, `BLOCKED` |
| design, apply-reflection | `DESIGN_READY`, `NEEDS_HUMAN`, `BLOCKED` |
| reflect | `REFLECTION_READY` |
| build-core | `CORE_DONE`, `CORE_DONE_WITH_CONCERNS`, `NEEDS_HUMAN`, `BLOCKED` |
| review-core, NN review | `APPROVED`, `APPROVED_WITH_COMMENTS`, `CHANGES_REQUIRED` |
| fix | `FIX_DONE`, `NEEDS_HUMAN`, `BLOCKED` |
| packages | `PACKAGES_DONE`, `PACKAGES_DONE_WITH_CONCERNS`, `BLOCKED` |
| NN plan | `PLAN_READY`, `NEEDS_CONTEXT`, `BLOCKED` |
| NN plan-feedback | `PLAN_APPROVED`, `PLAN_APPROVED_WITH_CHANGES` |
| NN build, NN fix (medior) | `DONE`, `DONE_WITH_CONCERNS`, `NEEDS_CONTEXT`, `BLOCKED` |
| security-review | `PASS`, `PASS_WITH_FINDINGS`, `BLOCKED` |

Each step file has one line `Status: <WORD>` under its title. The security review uses `Verdict: <WORD>`.
