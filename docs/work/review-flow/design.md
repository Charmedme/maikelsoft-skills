# Review flow design

> **Status 2026-10-31:** this file records the design as it was before the cut. Origin (Part B), the nit cap (Part C), and `REVIEW.md` (Part E) are on hold. The hard round cap of Part A became a count with a stop on a cause that comes back. `docs/work/review-flow/comparison.md` has the reasons and the results.

Status: the owner approved the first version of the build on 2026-10-31, and asked for two more rounds. The owner approved the decisions of the rounds on 2026-10-31. The file `build-notes.md` holds the small fixes for the builder. No part of this design exists in a plugin yet. Date: 2026-10-31.

This document tells how review must work in the `dev-team`, `dev-flow`, and `dev-stack` plugins. It covers review loops, nits, older code, and risk. The file `evals.md` holds the evals that measure the reviewers. The file `retrospective.md` tells how this design came to be.

The owner is one person. Agents do the work and do not push. So this design treats human attention as the scarce resource, and it does not copy rules that need a large team.

## Contents

- Goals
- Sources
- Terms
- The flow
- Part A: Stop review loops
- Part B: Origin of a finding
- Part C: Nits
- Part D: Risk check
- Part E: REVIEW.md
- Part F: Reports
- Part G: Repeat findings
- Part H: Skill list
- Part I: Stack hook
- Part J: Comments in `dev-flow`
- Rollout
- Not adopted
- Decisions of the owner
- Limits

## Goals

In order of priority, from the Anthropic post:

1. Catch real bugs.
2. Cut review rounds.
3. Send human attention to the changes with the most risk.

## Sources

| Source | Use | Strength |
|--------|-----|----------|
| [How the Claude Code team does code review](https://claude.dev/blog/solving-code-review-at-anthropic/) | The round cap, diff-only review, risk tiers, incident evals, clean cases | Primary, from a vendor at a much larger scale |
| [Customize Code Review](https://code.claude.com/docs/en/code-review-customize.md) | The `REVIEW.md` format, and the rule to read it from the base branch | Primary |
| [Severity levels of Code Review](https://code.claude.com/docs/en/code-review-pull-requests.md#severity-levels) | The Pre-existing level, and the count of findings that a review does not post | Primary |
| [Demystifying evals for AI agents](https://www.anthropic.com/engineering/demystifying-evals-for-ai-agents) | 20 to 50 tasks from real failures, cases in both directions | Primary, from a vendor |
| The AI-Native SDLC Playbook (PDF, not in the repository) | The nit cap of 5, the failing test first, a block on test edits | Vendor |
| The video transcript (TXT, not in the repository) | A cap on nits, nits on older code | A summary of the post |
| [Factory benchmark](https://docs.factory.com/benchmarks/review-benchmark), [Qodo benchmark](https://huggingface.co/datasets/Qodo/PR-Review-Bench/blob/main/README.md), [Kodus experiments](https://kodus.io/en/ai-code-review-recall/) | Eval size, noise, and the cost of voting | Vendor results, not independent |
| [Google code review standard](https://google.github.io/eng-practices/review/reviewer/standard.html) | Approve when the change makes the code health better. A nit is a point that the author can ignore | Primary |
| *Vibe Coding Architecture At Scale* (Packt, 2026), from the book library | A loop is only as good as its stop condition and its blast radius. Few trials give bands, not exact numbers | Book |
| *Modern Software Engineering*, David Farley, from the book library | Work in small steps and limit the blast radius | Book |

The Dutch file `volledige_raw_tekst_vertaling_woord_voor_woord.md` is not a source. It says that it is a word-for-word text, but it adds claims that the transcript and the post do not have.

## Terms

| Term | Meaning |
|------|---------|
| Round | A follow-up with one or more rows that were ever of class `fix` or `prove`. A follow-up with no such rows is not a round. |
| Base | The commit in the `Base` field of `run.md`. In `dev-flow`, the result of `git merge-base HEAD <default branch>`, written in the report. |
| Compare point | The commit that each diff of a run starts from. The script `riskcheck.py` gives it. |
| Origin | Where a finding comes from: `new`, `exposed`, or `pre-existing`. Part B defines these words. |
| Blocking | A finding that gets a fix pass before the human gets the work. |
| Nit | A point of polish with no effect on behavior. It is not blocking. |
| Risk | The output of `riskcheck.py`: `high` or `normal`. |

**The compare point.** `Base` records where the run started. It is not always the correct start of a diff. After a merge of the default branch, `git diff <Base>...HEAD` lists files of other people. A test showed the same after a rebase. So the risk check gives false reasons, and a reviewer tags other people's lines as `new`.

The rule: use Base only when the merge base with the default branch is an ancestor of Base. Else use the merge base. So an open stacked branch keeps Base. A story after a merge, a rebase, or a squash merge of its parent gets the newer commit. Tests showed this.

On real merges, the result equals the pull-request diff in 237 of 237 merges of project A, and in 34 of 34 of project C. `Base...HEAD` is wrong on 22 of the 237.

When the result equals HEAD and Base differs, use Base. A branch that is already in the default branch otherwise gives an empty diff and a false `normal`. A test showed this. When Base also equals HEAD, the script exits with code 2.

The script finds the default branch from `--default-branch`, then `origin/HEAD`, then `main`, then `master`. When Base does not exist, it exits with code 2, and "Warn when the raise fails" applies. An amended parent is a known gap, so the script prints a line for it. A stacked story that merges the default branch is a second gap.

The same point goes to the reviewers, to `decisioncheck.py --changed`, and to `git show` for `REVIEW.md`. Pull requests on GitHub use a [merge-base diff](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/about-comparing-branches-in-pull-requests) too.

## The flow

These steps show one `dev-team` story with the changes of this design.

1. The senior designs the story. `design.md` has a section "How to undo".
2. The human approves at gate G1.
3. The agents build. Each review gives each finding an Origin and a level.
4. Before the security review, `riskcheck.py` runs from Base. The team lead saves its reasons in the run folder.
5. When the run is light and the risk is high, the team lead sets `Mode: raised`, and starts the security review.
6. Before G2, `riskcheck.py` runs again from Base. Only the reasons that are new in this run count.
7. G2 shows the block Warnings, then the result, then the text for the pull request.
8. The human pushes. The comments of the pull request go to `process-review`.
9. `process-review` sorts the comments. A follow-up with no `fix` and no `prove` rows ends without a fix pass.
10. When 3 rounds exist and a new batch has `fix` or `prove` rows, `process-review` stops. It gives the human three options.

`dev-flow` follows the same rules with its own steps. Part B, Part C, and Part D name the differences.

## Part A: Stop review loops

**Problem.** One pull request at Anthropic went through more than 50 rounds overnight. Today, `process-review` starts a new follow-up for each round of comments, with no total limit.

**Risk here.** Agents do not push, so a human is in each round. The loop cannot run overnight. So the cap is a text rule in `process-review`, not a checker rule.

**Rules.**

1. Count the rounds after the step Sort, because that step gives the classes of the new batch.
2. Keep a row that changes from `prove` to `reject` as `prove -> reject`, so that the count stays correct.
3. When 3 earlier rounds exist and the new batch has a `fix` or `prove` row, stop. Show the rounds so far, and give these options:
   a. Go on with this round.
   b. Close the threads with a reason. The rows become `fix -> closed`, and no fix or review step runs.
   c. Start a new run with a new design. The new run gets a new branch from the compare point. So the old code is not in its diff. The old branch stays for the human.

   After b or c, write `FOLLOWUP_DONE` with the option as the reason, remove the marker, and commit. Else rule 10 continues the old follow-up.
4. Write the answer as a row in `log.md`, so that a restart does not ask again. Each later round asks again.
5. A follow-up with no `fix` and no `prove` rows skips the fix step and the review step. Today, the skill has no path for this case.
6. A change that the human asks for uses the file `follow-ups/NN-change.md`, which quotes the request. It is not a round. Today, the file name is free.
7. A comment with the same cause as a rejected or decided row gets a reply that cites the earlier row. A comment with a new fact opens the row again.
8. Flag the class of a bug, not one line. The step Group of `process-review` already does this.
9. Print `Rounds: N` at `FOLLOWUP_DONE`. This is the first data for goal 2.
10. When the last `FOLLOWUP_STARTED` has no `FOLLOWUP_DONE`, continue that follow-up with the same number. Today, a restart takes a new number, so one batch counts as two rounds.

**Also fix.** A review of this design found these defects in the current code.

- Today, G2 removes `.dev-team/active-run`, so `commands.jsonl` logs nothing in a follow-up. `process-review` and the route for a human change must write the marker at the start. They must remove it before the commit at `FOLLOWUP_DONE`.
- `RUN-EVIDENCE` accepts a command from any time. A test showed that a follow-up row with `test pass` and only a command from before G2 gives no finding. A rule for each row does not solve this. In the 8 real run folders, the rule reads only 33 of 138 check cells, because the cells are free text and often Dutch. So replace the rule after G2 with one rule for each follow-up:
  - The reason of the `FOLLOWUP_STARTED` row holds the full time from `date -u +%Y-%m-%dT%H:%M:%SZ`, because the column Time has only `HH:MM`. A window ends at the next `FOLLOWUP_STARTED`.
  - Each window needs a `checks.py` command in `commands.jsonl`. Step 7 already runs it, and the text of a row does not matter.
  - A window with no commands gives one `RUN-HOOK` finding. A window with no time gives no finding, so an old run folder stays clean.
  - The rule before G2 stays as it is.
- The route for a human change and the route for a tiny change also open a window. In the real runs, 4 rows after G2 had no `FOLLOWUP_STARTED`. A tiny change is not a round.
- Commit the run folder at `FOLLOWUP_DONE`. Today, no step commits these files, and rule 7 and the windows need them on another worktree.

## Part B: Origin of a finding

**Problem.** A reviewer finds older problems, and a helpful agent fixes them. This makes new commits and new rounds. But the simple rule "only the diff can block" hides real bugs.

Also, the post says that 59% was code "there since the reviewer's first pass". The reviewer had already read this code, and it is not always older than the pull request. So this problem needs two rules: Origin and the scope of a follow-up.

**Rule for Origin.** Each finding has one Origin. Measure it against the compare point in each review, also when a review reads only the diff of a fix.

| Origin | Meaning | Can block |
|--------|---------|-----------|
| `new` | The line is not in the compare point. A line that the change moved counts as `new`. | Yes |
| `exposed` | The line is in the compare point, but this change makes it reachable or wrong. The finding says how. | Yes |
| `pre-existing` | The line is in the compare point, and this change does not affect it. | No |

**Rule for the scope of a follow-up.** The first review reads all of the story. A review in a follow-up gets the earlier review files, and it does not repeat a finding from them.

- A finding blocks only on a line in the fix diff, or on a line that the fix exposes.
- Another finding goes under "Open findings" for the human. No agent fixes it, and it does not change the status. So a bug in the code of the story stays visible, and it does not start a loop.

**Older problems must reach the human.**

- `pre-existing` findings go in the section "Older problems" of the review. Nobody fixes them in this story.
- G2 gets one line for each older finding of level `issue`, and a count of the others.
- The team lead adds each one to `<run folder>/older-problems.md`, so that it stays after the run. One file for each story gives no merge conflict, and the run folder is already outside the hash of the report. The senior reads the older lists of earlier runs in `mode: design`, for the paths that the story touches.
- A `pre-existing` security finding of Critical or High goes in the block Warnings.
- In `dev-flow`, the review sub-agent writes each finding to `docs/work/<slug>/review.md`, in a table with the columns Location, Level, Origin, and Finding. The report gets the count and at most 3 lines. The file also holds Base, because a report is a message that a restart cannot read.
- `process-review` gets the class `older` for a comment on a line in the compare point that this change does not affect. Its action is a reply and a row in the older list, with no fix. The human can change the class.

**Evidence.** Each finding has a `path:line`. A claim about behavior needs a citation of the code, not a guess from a name. Before a finding goes in the report, try to refute it. The security report already has this step, and the other reviews get it too.

**Check.** No checker rule. The `pre-existing` eval cases measure wrong tags (see `evals.md`). An earlier version had a rule `RUN-ORIGIN`. A test showed false findings after a fix moved lines. It also missed the case that it was for.

**Where.**

- `senior-practice/SKILL.md` and `references/templates.md`: a column Origin, and the section "Older problems".
- `process-review/SKILL.md`: the scope of step 6, and the class `older`.
- `security-reviewer.md`: use the four levels of `review-report.md` in place of "blocking, major, or minor".
- `secure-coding/references/review-report.md`: a column Origin. `BLOCKED`, `PASS`, and the Gate column count only `new` and `exposed` findings.
- `plan-and-build/references/review.md`: the file `review.md` of the work folder. The sub-agent gets read access to the repository in addition to the diff, because it cannot judge `exposed` from the diff alone. Step 4 fixes only `new` and `exposed` bugs.

## Part C: Nits

**Problem.** The transcript says to cap nits, and it warns about nits on code that only came along with the pull request. The post gives the reason: "A person can wave off a nit in seconds. An agent fixes it, which means another commit and another round of review."

**Rules.**

1. Show at most 5 nits in full, by default. Put each other nit on one line in the section "More nits". The message to the human gives only the count. `REVIEW.md` can change the number.
2. The cap applies to the label `nitpick` only. `suggestion` and `issue` have no cap, and they are always listed. A hidden `suggestion` can hide a real bug.
3. A nit is only on a line that the change adds or changes.
4. A nit never changes the status, starts a fix pass, or counts as a round. Only the human can make a nit into a fix.
5. Do not report a nit for a tool that ran in this review, or for a generated file. This rule names only tools that ran, because `checks.py` does not run `stackcheck.py`.
6. `process-review` gets the class `nit`. Use it when the author of the comment marks it as a nit. Also use it when the comment has no effect on behavior.
7. A comment of the owner is `nit` only when the owner says so. Step 8 shows the nit rows separately, with a short reply and no fix.
8. `plan-and-build/references/review.md` gets the same levels. Today, it knows only "clearly a bug" and "other".
9. When the same nit comes back, Part G applies.

**Check.** The evals check that no nit becomes a must-fix row. `runcheck.py` does not count nits. It runs at G2, when the review counter is at its limit, so a finding has no remedy there.

The security review keeps its four levels. It has no nits.

## Part D: Risk check

**Problem.** A light run skips the security review because of a claim in `design.md` at design time. Nothing checks that claim against the diff. In `dev-flow`, the tier rule is also text only.

**The script.** New file: `shared/riskcheck.py`, with `tests/test_riskcheck.py`.

```sh
python3 riskcheck.py --root . --base <Base> [--default-branch <name>] [--out <file>]
```

The output has one line for the level, one line for each reason with its path, and one section for test edits. The first line gives the compare point. The exit code is 0 when the script ran, and 2 when it could not run. `--base` is always Base, and the script finds the compare point from it. So `REVIEW.md` never comes from the change itself, and test edits always cover the full story.

| Level | When |
|-------|------|
| `high` | A path or an added line matches the list. |
| `normal` | Each other change. |

The list:

- Paths: migrations, CI workflow files, sign-in and authorization names, and the lines under "Sensitive paths" in `REVIEW.md`.
- Added lines of code, not comments: a package name that the compare point does not hold, and a major version update.
- Added lines, part 2: a new marker for anonymous access such as `AllowAnonymous`.
- The script leaves out `docs/`, test and fixture paths, `artifacts/`, and `.log` files. A minor version update is not high, in line with `tiers.md`.

**How a name matches.** Split each path on `/`, `.`, `-`, `_`, and at each capital letter, then match whole words. So `Shop.Modules.Identity` and `AuthController.cs` match, and `AuthorService.cs` does not.

**Guide files.** A change to `REVIEW.md` or `CLAUDE.md` does not raise the risk, because a security review adds nothing to a change of text. A change to `REVIEW.md` gives one line in Warnings. A change to `CLAUDE.md` gives one line under "What changed and why".

**Measured.** A prototype ran over the last 100 changes of three projects of the owner, on the main line. This cost nothing. A change has 10 to 39 files on average. A raise matters only on a light run, a patch, or a small item. So the table also counts the changes with 5 code files or fewer.

| Project | High, all changes | High, small changes |
|---------|-------------------|---------------------|
| Project A | 4 of 100 | 1 of 12 |
| Project B | 44 of 100 | 11 of 34 |
| Project C | 14 of 35 | 1 of 10 |

The first rule used whole folder names. It fired on 0 of 150 merges of project A, because it missed the identity module. It fired on 73% of the merges of project C, mostly on `CLAUDE.md`.

**Rules that this design cut.** The prototype also had rules for secrets, outbound clients, raw delete statements, and file upload. They added 0 high changes in this set, also on the 56 small changes. The 6 secret hits were all names of variables, and upload had 0 hits. A new first version without them is simpler, and Part G can add a rule when a real run needs it. The review still looks for these risks.

**Test edits.** The script lists each assertion that exists in the compare point and does not exist at HEAD, and each skip marker that the change adds. This is the check that a fix did not make a test weaker. It is a report and it blocks nothing, because a test can be really wrong. `dev-team` has no lock on its tests, and no review follows a fix pass.

**`dev-team`, in `deliver-story`.**

1. Before step 8, run `riskcheck.py` with `--out <run folder>/risk-1.txt`. Add a log row with the new status word `RISK_CHECKED`.
2. When the run is light and the level is high, set `Mode: raised`, and run step 8.
3. Before step 10, run `riskcheck.py` again with `--out <run folder>/risk-2.txt`. Only the reasons that are not in `risk-1.txt` count. Compare by kind and path, with no line number.
4. When a new reason exists and step 8 did not run, set `Mode: raised`, and run step 8.
5. When a new reason exists and step 8 already ran, add a line to Warnings. The line says that the new paths have no security review.

**What `raised` means.**

- It needs `security-review.md`, and it does not need `reflection.md`. `run_mode` in `runcheck.py` must know this word, and `run-files.md` and `gates.md` must list it.
- The UX review runs at the level of a full run. In a follow-up, the security section of `review-core` also runs, as in a light run.
- The design said light and the build disagreed, so the human must know. Warnings gets the line "The design said light. The build touched `<paths>`." The same line says that the section "How to undo" of `design.md` does not cover these paths.

**`process-review`, and a change that the human asks for.** Before the review step, run `riskcheck.py` with `--out follow-ups/NN-risk.txt`, and compare it with the last saved reasons. When a new reason exists and the security counter is 0, start the security reviewer, as a raise. A blocking result goes to the human, because a follow-up has no second fix pass.

When the counter is 1, put a line in the summary. The human can ask for a new review. A review on each high fix is too heavy, because the human reads each round.

**`dev-flow`, in `plan-and-build`.** After the last commit and before the review, run `riskcheck.py`. When a patch or small item is high, keep the tier and add the review sub-agent. Do not change the tier, because `workcheck.py` then fails with no `plan.md`. `SKILL.md` and `tiers.md` say to move the tier up on a risk, so they get this exception.

- The tier line becomes "Tier: small, review added (risk: `<reason>`)".
- The UX level stays. A patch has no `speclock.py` lock, so the sub-agent gets no WATCH lines.
- A new package that the script finds stops at the human, because the skill needs approval before the build.
- `docs/work/<slug>/review.md` holds Base and the reasons, so that a restart and a later comment can read them.

**Warn when the raise fails.** This is the decision of the owner: raise first, and warn when that does not work. It fails when the script cannot run, when an agent fails, or when the counter is at its limit. The report then says that the risk has no cover, and the report is not clean. A second agent failure still stops the run under the current Stops rule, and the stop message says this.

**No approver counts.** The post asks for 0, 1, or 2 people for each tier. One owner cannot give two approvals. So G2 gives the risk level, the reason, and the files to read first.

## Part E: REVIEW.md

**Problem.** Neither review flow reads a `REVIEW.md`. This file is the place where the owner tells the reviewer what matters.

**Rules.**

1. Read the root `REVIEW.md` from the compare point with `git show "<compare point>:REVIEW.md"`. A change then cannot relax its own review. The Code Review service has the same principle, but it reads from a protected branch.
2. Read only the root file. The service also reads files in subfolders, but one owner does not need this yet.
3. When the file does not exist, the defaults of this document apply.
4. A skip rule never silences a security finding of Critical or High.
5. An agent can propose a change to `REVIEW.md`. Only the human approves it.
6. Put a rule for the local `/code-review` command in `CLAUDE.md`, because that command does not read `REVIEW.md`.

**Template.** New file: `shared/review-md.md`. The headings follow the Code Review example, with one new name for the first. The file is freeform, so the last two headings also work with the service.

```markdown
# Review instructions

## What must be fixed before a merge
<what failure looks like in this repository, and what is a nit at most>

## Cap the nits
Show at most five nits in full per review.

## Do not report
<generated paths, and anything that CI enforces>

## Always check
<the rules of this repository>

## Evidence
A claim about behavior needs a file and a line. Start the summary with a count, and say so when there is nothing to fix.

## Sensitive paths
<paths that always give high risk>
```

**Sync.** Add `riskcheck.py` and `review-md.md` to `NAMED_COPIES` in `scripts/sync_shared.py`, and update its docstring and `CLAUDE.md`. A skill gets a copy only when its `SKILL.md` names the file. So `senior-practice` and `plan-and-build` name the template, and they offer it when a project has no `REVIEW.md`. Add `riskcheck.py` to `allowed-tools` of `plan-and-build`.

## Part F: Reports

G2 in `gates.md` gets these parts.

**Warnings.** One block at the top, so that no rule competes for the first line. It holds only points that need an action:

- the `RUN-HOOK` finding
- the risk without cover
- the raise
- a change to `REVIEW.md`
- the older Critical or High security findings
- the CI parity

Test edits get one count line with the file names, under the result. In the block, they fill about half of the changes of project C. It says "None." when it is empty.

**What changed and why.** Three lines from `design.md`. The transcript names two jobs of review: catch problems and spread understanding. These lines serve the second job at a low cost.

**What the checks do not cover.** One line. The post asks the same question of a person who approves a change.

**Open findings.** G2 lists each open finding of level `issue`, with its file. The post has the rule "Every finding has to be answered". The human gives this answer at G2, because a fix pass covers only blocking findings. G2 gives one number for all `suggestion` rows.

**Text for the pull request.** Three lines that the human can copy:

```markdown
Testing: <what ran, with the result>
Risk: <high or normal, the reason, the files to read first>
How to revert: <the steps. A schema or data change needs more than git revert.>
```

The post says that Claude can fill these in, but that the person who asked for the change signs off. In `dev-team`, the source of the last line is the section "How to undo" of `design.md`. In `dev-flow`, the agent writes it from the change.

**The commit of a report.** The report names the last commit outside the run folder. This command gave the correct hash in a test, from the root and from a subfolder:

```sh
git log -1 --format=%h -- ':/' ':!/<run folder>'
```

A hash of HEAD does not work, because G2 commits the run folder itself. The tree must be clean, because the command ignores uncommitted code. A later commit needs a new report.

`FOLLOWUP_DONE` shows the new hash, and G2 does not repeat. In `dev-flow`, `plan.md` gets `Done: approved at <hash>`. With no `plan.md`, the report holds the hash. The Gate Done report can go over its 250 words by these lines.

## Part G: Repeat findings

The Code Review docs say that when the same kind of finding keeps coming back, a maintainer adds a rule to `REVIEW.md`. The playbook says the same for `CLAUDE.md`.

1. In the step Sort of `process-review`, search the earlier reviews and follow-ups for the same cause.
2. On a second time, write a proposal row with a target. Prefer a lint or stack rule, then `conventions.md`, then `REVIEW.md`.
3. Get the approval of the human for the row. The senior then writes the rule in the next fix pass, because the team lead writes no code.
4. In `follow-ups/NN-replies.md`, count the comments by response. The post measures noise in this way.

| Response | Classes |
|----------|---------|
| Fixed | `fix` |
| Wrong | `reject`, `prove -> reject` |
| Intentional | `decided`, `question` |
| For the product owner | `product` |
| Did not matter | `nit` |
| Older code | `older` |

A script for metrics over `docs/work/` waits until 10 runs exist.

## Part H: Skill list

**Cause.** Claude Code gives the skill list 1% of the context window. Over that limit, it drops the descriptions of the [skills that you invoke least](https://code.claude.com/docs/en/skills.md#skill-descriptions-are-cut-short). In the session of 2026-10-30, 16 skills of this marketplace showed no description, also `dev-stack:secure-coding`. The 58 descriptions together have about 26,000 characters.

**Done.** The owner approved this change: `skillListingBudgetFraction` is `0.02` in the settings of the owner. The effect is not confirmed yet.

**To do.** Add a note to the README of the marketplace. Shorter descriptions wait for a trigger test, because a shorter text can stop a skill from starting.

## Part I: Stack hook

The owner chose a hook that only warns. The reviews of this design found no evidence for the need, and they found work that the first plan left out:

- `dev-stack` has no hooks folder.
- `stackcheck.py` has no option for one file.
- A `require` rule fires on a file that is half written.
- Plain output with exit 0 does not reach the model.

**Decision of the owner.** Wait, and do not build the hook now.

**How the count happens.** A wait with no measure never ends, so this design names the measure. No new code collects data. The security reviewer already runs `stackcheck.py` on the changed files, and the report keeps the summary line of each tool. So the errors in `security-review.md` are stack errors that reached the last review of a run.

**One change.** `review-report.md` has `--changed main` for this tool. Use the compare point, so that a stacked branch does not change the count.

**When to decide again.** When 10 run folders with a `security-review.md` exist, count these errors. This is the same moment as the metrics script of Part G. Build the hook when the average is 1 error or more per run. Else close the idea.

## Part J: Comments in `dev-flow`

**Problem.** Most items go through `plan-and-build`, also a large item, unless the human asks for the team. But `process-review` needs a run folder of `dev-team`, and it stops without one. So after Gate Done, a comment on a `dev-flow` pull request has no route. It gets no classes, no round cap, and no nit rule. An agent then fixes each comment, and this is the loop that the post describes.

**Route.** Today, `process-review` stops when no run folder exists, and `session-start.py` sends each comment there. So the stop text and the start text must name this route. The description of `plan-and-build` gets the trigger, and a trigger test must check it, because the two skills then compete.

**Rules.** New file: `plan-and-build/references/comments.md`. The skill loads it when the human brings review comments on a finished item.

1. Write `docs/work/<slug>/comments-NN.md`, with the classes of `process-review`, and the classes `nit` and `older`.
2. The session fixes `fix` and `prove` rows with TDD. The approved tests stay locked, and `speclock.py check` runs at the end.
3. The round rule of Part A applies. So a batch stops after 3 earlier rounds, with the same three options.
4. Draft the replies for the human. Do not post them.
5. A patch or a small item gets no second review. The exception is a new reason from the risk check of Part D.

**Status.** The owner approved this part. It waits for the `dev-flow` work.

## Rollout

Each pull request has unit tests for its scripts, and the evals of `evals.md` that it needs. Each change gets a row in `docs/work/<plugin>/edit-log.md` with its test and a verdict.

| Pull request | Parts | Evals | Version |
|--------------|-------|-------|---------|
| 1 | The target `lead` of the runner. Part A, and the fixes for the marker, `RUN-EVIDENCE`, and the commit of the run folder | Stage 8, the stop | `dev-team` 0.6.0 |
| 2 | Part D with the compare point and `RISK_CHECKED`, the first form of `review.md` in `dev-flow`, Part E sync, the block Warnings, `raised` in `runcheck.py` | Stage 8, the raise and Warnings | `dev-team` 0.7.0, `dev-flow` 2.2.0 |
| 3 | The eval tools and the cases, with the old version run. Then Part B, Part C, and Part E rules for `dev-flow` | Stages 1 to 7 | `dev-flow` 2.3.0 |
| 4 | The classes `older` and `nit` and the scope in `process-review`, the rest of Part F, Part G, the Part H note, Part J | Stage 8, the stop for Part J, and a trigger test | `dev-team` 0.8.0, `dev-flow` 2.4.0 |
| 5 | Part B, Part C, and Part E for the senior and the security reviewer | Stages 9 and 10 | `dev-team` 0.9.0, `dev-stack` 1.3.0 |

## Not adopted

| Idea | Reason |
|------|--------|
| `intent.md` and `spec.md` | The tracker and `story.md` do this job. The playbook accepts a tracker as the source of truth. |
| Voting between reviewer agents | Kodus went from 83 to 52 true findings, for a precision of about 22% to 25% (calculated from its counts). Look at this again only when the evals show too many wrong findings. |
| Merge queue, circuit breaker, Scion | These solve problems of scale that this team does not have. The post says that Anthropic has no circuit breakers yet. |
| Merge without a human | Agents do not push, and the human decides at the gates. |
| 0, 1, or 2 approvers | There is one owner. |
| A new counter for rounds | The rows of the comment files give the number, and a counter can jam on a restart. |
| `RUN-NITS` and `RUN-ORIGIN` in `runcheck.py` | A finding at G2 has no remedy, and a test showed false findings for `RUN-ORIGIN`. |
| Test edits as a `runcheck.py` finding | A correct edit of a test fails the evals that assert `runcheck_clean`. |
| A security review on each high fix after G2 | The human reads each round. A line in the summary is enough. |

## Decisions of the owner

| No | Decision | Answer on 2026-10-31 |
|----|----------|----------------------|
| 1 | Start the build, in the order of Rollout | Yes, after two more rounds |
| 2 | The limit for the evals of `dev-flow` | $150, with a stop when the projection goes over it |
| 3 | Run the evals of the senior | Yes, with no limit in the answer |
| 4 | Stack hook | Wait, and use the count of Part I |

These points came from the two rounds. The owner answered yes to each one on 2026-10-31.

| No | Decision | Answer: yes, with this reason |
|----|----------|-------------------------------|
| 5 | Part J: review comments in `dev-flow` | Yes, in pull request 4. Without it, the round cap misses most items |
| 6 | A lost case stands only after a confirmation, and the owner accepts each reason | Yes. See `evals.md`, "How to decide" |
| 7 | The measures of benefit change to `asks_change_before_merge` and wrong findings | Yes. The earlier measures rewarded hidden bugs |
| 8 | A total counts as a fall at 7 of 42 for `named`, and at 4 of 9 for `older_code_flagged` | Yes. This set cannot separate a smaller fall from noise. Rule 3 still catches a lost single case |
| 9 | A guide file gives a line and no raise. Warnings holds only points that need an action | Yes |
| 10 | The first version of `riskcheck.py` has no rules for secrets, outbound clients, raw deletes, and file upload | Yes. They added no high change in the sample. The sample is small |
| 11 | After G2, `RUN-EVIDENCE` asks for one `checks.py` command in each follow-up, not a proof for each row | Yes. Free text in the rows makes a rule for each row blind |
| 12 | Option c of the round cap starts a new branch from the compare point, and the old branch stays | Yes |
| 13 | The limits for the text rules ($30), the security text ($25), and the senior ($95) | Yes. This adds $150 to the $150, so the plan can cost $300 |

## Limits

- The rules and the evals have not run. The cost, the noise, and the gain are not known yet. Only a prototype of the risk check ran, over past commits, and a build rehearsal ran in a copy. The scripts of the rehearsal passed their tests.
- No independent reviewer has read the changes that came after the rehearsal.
- The evidence from outside comes mostly from vendors, and from much larger teams.
- `riskcheck.py` reads paths and patterns. It misses a risk that shows in neither.
- A false lost case stays possible in the evals, and a real fall below the limit of decision 8 can pass.
- On a stacked `dev-flow` branch, the diff also holds the parent, as it does today.
- A reviewer can still give a wrong Origin. Only the eval measures this, and no field check exists.
- No bad run of these flows has shown these gaps, because `process-review` has no field runs yet. The gaps come from the code, from tests of the code, and from the post.
