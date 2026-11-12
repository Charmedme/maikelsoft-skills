# Review flow evals

Status: plan. No eval in this file has run. Date: 2026-10-31. The file `design.md` in this folder holds the rules that these evals test.

## Contents

- The question
- Tools
- Cases
- What the grader scores
- Stages
- How to decide
- Cost
- What this cannot show

## The question

Do the new review rules ask for fewer needless changes and give fewer wrong findings, and do they find as many real bugs?

## Tools

The tools in `evals/dev-stack/` give the method, but the two runners do not fit.

- `evals/dev-stack/run.py` forbids the Agent and Skill tools, and it tells the run to follow `SKILL.md`. For `plan-and-build`, this starts a build.
- `evals/dev-team/run.py` makes a repository with no diff to review, and it writes no `final.md`.

So this work adds a small runner, `evals/review-flow/run.py`. It has three targets.

| Target | How it starts | What the grader reads |
|--------|---------------|-----------------------|
| `flow` | `claude -p --model sonnet`, with the steps 1 to 3 of `review.md`, the item, and the diff | `final.md` |
| `senior` | `claude -p --agent dev-team:senior --plugin-dir <version>`, with `mode: review-core` and a small run folder | `core-review.md`, copied to `final.md` |
| `lead` | `claude -p --plugin-dir <version>`, with a prepared run folder. Each run has a limit of $5 | Files and log rows, scored by a script |

| Tool | Use | Change |
|------|-----|--------|
| `prepare.make_repo`, kind `review` | The repository, with the base on `main` and the change on `feature` | Fill the hash of Base in the fixture `run.md` after the commit |
| `planted/*.json` | The answer sheet, with decoys | The new fields `origin` and `must_block` |
| `grade.py` and `grader.md` | One blind grader for each run | One result row for each planted id, one label for each finding, and no review file in the repository copy |
| `compare.py` | The result for each case, and a sign test | A variant parameter |

**The old version.** The old text must stay available after the change. Use the same method as `DEV_FLOW20_ROOT` in `evals/dev-team/run.py`: a worktree of the commit before the change.

**One variable.** Today, the sub-agent gets only the diff, and the new text gives it read access. A run in a repository has read access in any case. So both variants get read access, and the verdict covers only the rules. The read access itself ships with the verdict `proposed`.

**One output format.** The old version has no levels, so it cannot say what it wants fixed. So the common prompt of both versions asks for a last table with the columns `Location`, `Finding`, and `Fix before merge` (yes or no).

## Cases

There are 20 cases, the low end of the 20 to 50 that the evals guide gives.

| Cases | Kind | What passes |
|-------|------|-------------|
| 9 | A new bug | Named. New version: must fix, with Origin `new` |
| 2 | An exposed bug | Named. New version: must fix, with Origin `exposed` |
| 3 | An older bug in the same file | Named. New version: tagged `pre-existing`, and no fix before merge |
| 6 | A clean change with decoys | No decoy flagged |

- Real bugs come first, from the fix and revert commits of the projects of the owner. Project A has about 375 commits that start with "fix", and only a few that start with "Revert".
- This repository is public. So write each case as a small new example, and copy no private code.
- Label an injected bug as injected.
- A real bug in a clean case is not a wrong finding. It has its own count.
- Each later incident or field report adds a case.

## What the grader scores

| Measure | Who scores it | Versions | Use |
|---------|---------------|----------|-----|
| `named`: the review names the same problem at the same place, with the correct reason | Blind grader | Both | Must not fall by 7 of 42 or more |
| `older_code_flagged`: the review names the older bug | Blind grader | Both | Must not fall by 4 of 9 or more. A fall means that the review hides an older bug |
| Wrong findings: a finding that is wrong, or that leads to a harmful change | Blind grader | Both | A gain |
| `asks_change_before_merge`: the rows with `yes` that the grader labels as a nit or as older code | Grader label, then a script | Both | A gain. This is the row that starts another commit and another round |
| Level and Origin of the matched row | A script, from the review table | New only | A `must_block` bug that is not a must-fix row counts as lost |

The grader gets only the last common table. A test showed that the variant still shows when a script removes columns and headings from the full text. The script for level and Origin reads the full text.

The count of nits is not a measure. Part C of `design.md` still lists each nit on one line, so the cap does not lower the count.

## Stages

1. **Old version, first runs.** Run 2 times for each of the 14 bug cases, and 1 time for each clean case. These 34 runs prove the cases and give the real cost.
2. **Harder cases.** Make a case harder when both runs name its bug. Keep at most 4 easy cases as a regression set. A set that the old version passes in full cannot show a gain.
3. **Cost check.** Add the numbers from stage 1, and stop for the owner when the projection goes over the limit.
4. **Old version, measure.** Run the old version until each case has 3 runs. A harder case gets 3 new runs, and its earlier runs do not count. Those runs selected the case because the old version named the bug, so they bias the result. A case that no run names is too hard. Replace it.
5. **New version.** Run 3 times for each of the 20 cases (60 runs). When stage 3 stopped for cost, the owner can choose 1 run first. Cases that differ then get more runs.
6. **Confirm.** Run each candidate for a lost case 6 more times for each version.
7. **`REVIEW.md`.** Run the rules of Part E on 6 cases, with 3 runs each (18 runs). Each rule that a case tests gets one assertion.
8. **Text rules, target `lead`.** Each eval runs 3 times.
   - A run folder with 3 earlier rounds and a new batch. The agent must stop and give the three options.
   - A light run with a migration in the diff. The agent must raise.
   - A prepared `security-review.md` with a `pre-existing` Critical finding. The block Warnings at G2 must show it.
   - For Part J: the same stop test in `plan-and-build`.
9. **Security text.** Run the existing case `secure-coding-review` of `evals/dev-stack` 3 times for each version of the text. It has 12 planted mistakes, so it shows if the Origin column makes the reviewer lose findings. Add 2 cases with a `pre-existing` Critical finding, on the new version. They assert on the row in "Older problems" and on the verdict word.
10. **Senior.** Use 8 of the 20 cases: 4 new, 1 exposed, 1 older, and 2 clean. Add a small run folder to each case, and run 3 times for each version. The senior already has labels and a confidence rule, so the result of `flow` does not carry over.

Test `flow` first. It is stateless and cheap, and it has no levels today. This test covers Part B and Part C together, so its verdict on each one is `confounded`. Separate runs double the cost, and the two parts share one template.

A baseline of the rules in stage 8 has no value, because the old text has no such rule. The build-skill method asks for a baseline, so the edit log gives this reason.

## How to decide

1. **A candidate for a lost case.** The old version names the bug in 2 or 3 of 3 runs. The new version names it in 1 or fewer.
2. **A lost case.** After stage 6, a candidate stands when the old total minus the new total is 4 or more of 9.
3. **What a lost case does.** The change goes out, unless the owner accepts a reason. Real bugs come first.
4. **A gain in a count of cases.** A difference of 3 cases or more. Few trials give bands, not exact numbers. Kodus saw swings of about 6 bugs out of 136 between runs.
5. **A gain in a count of rows.** At least 25% fewer rows over the set, and more cases better than worse in `compare.py`.
6. **Keep the change** when these three points are true:
   - No lost case stands without a reason.
   - No total falls by the limit of its row above.
   - `asks_change_before_merge` or wrong findings show a gain.
7. Read the transcripts of the failed cases, as the evals guide says.
8. Run the set again when the model changes, as `CONTRIBUTING.md` asks.

**Why the lost-case rule changed.** The earlier rule said that no size of noise excuses a lost case. A simulation with no real difference between the versions gave these results. It uses 4 easy cases, and an assumed rate for the 10 harder cases.

| Rate of naming the bug | Mean false lost cases, no confirmation | Chance of 1 or more | Mean with confirmation | Chance of 1 or more |
|------------------------|----------------------------------------|---------------------|------------------------|---------------------|
| 0.5 | 2.5 | 94% | 0.3 | 29% |
| 0.7 | 1.7 | 85% | 0.2 | 19% |

So the earlier rule almost always rejects the change, also when nothing changed. Real rates come from stage 1. A false lost case stays possible, so the owner reads the transcripts of each one.

**Why a total has a limit.** "Must not fall" has the same fault. With no real difference, `named` falls by 1 or more in 42% to 46% of the sets.

The limit of 7 of 42 gives a false fall in 1% to 8% of the sets. The limit of 4 of 9 for `older_code_flagged` gives one in 0.4% to 5%. So this set cannot see a smaller fall in a total. A fall in one case stays visible through the lost-case rule.

## Cost

Batch 2 of `dev-stack` cost $160 for 132 Opus runs, about $1.21 for each run (`docs/work/dev-stack/batch2/results.md`). That file does not say if the amount includes the graders. Sonnet costs about one third of Opus (`docs/work/flow-scale/spec-results.md`).

| Work | Runs | Estimate | Limit |
|------|------|----------|-------|
| `flow`, stages 1 to 7 | 138, plus up to 20 for harder cases, plus 12 for each candidate. About 180 runs and 180 gradings | $100 to $187, by scale from a review estimate of $76 to $142 for 138 runs. Stage 1 measures it | $150, approved by the owner |
| Text rules, stage 8 | 12 | About $24, at $2 for each run. The order-mail stop run cost $1.05 | $30, proposed |
| Security text, stage 9 | 12, with gradings | $17 to $23 | $25, proposed |
| Senior, stage 10 | 48, with gradings | $67 to $91. A full agent can cost more | $95, proposed. The first 12 runs measure it |

The estimate for `flow` is over the limit at its high end. Stage 3 stops when it projects this. The total of the limits is $300.

The first paid run must check three points that the docs do not settle:

- Does `skills:` preload when the senior is the main agent?
- Does `--plugin-dir` win over the installed plugin?
- Does the router in the user settings change the model of a sub-agent?

## What this cannot show

- The runs review only, so an eval cannot show an agent that fixes a nit or older code. Rule 4 of Part C and the scope rule of Part B stay text rules with no measure until field runs exist.
- A single review cannot show rounds to pass. Today, the owner's projects have 8 run folders and 0 follow-up files, because `process-review` is new. So no baseline exists. `Rounds: N` of Part A gives the first data.
- The planted cases are small. A result here does not prove the same result on a large diff.
