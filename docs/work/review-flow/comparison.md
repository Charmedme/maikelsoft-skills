# Comparison: baseline, dev-team 0.5.0, and dev-team 0.6.0

Date: 2026-10-31. Part 1 is the first test. An independent review showed errors in it, and the list "Corrections" names them. Part "Second test" has the later result.

Result of part 1: the rules for nits in comments and the round stop fire on their triggers. The test says nothing about more bugs found, fewer rounds, or better use of the attention of the human.

## Corrections

An independent reviewer found these errors in this report. The session checked them against the run files.

| The report said | The facts |
|-----------------|-----------|
| User settings do not reach the runs | Runs wrote 3 rows (API-04, API-05, DATA-15) into `~/.claude/dev-team/profile.md`. `run.py` set `DEV_TEAM_PROFILE` but did not make the file, and the seniors used the home path. So a later arm could read the answers of an earlier arm. The owner approved the removal, and the rows are gone. `run.py` now makes the file, and it stops when the real file changes. |
| The new version handles comments better | 1 run only, and the new arm asked 5 questions against 2 for the baseline. Only the handling of the nit and the older comment shows a difference. |
| The baseline cost a fifth to a third of the team | The range was 9% to 30%. |
| The raise cost 68% more | The security review took 0.8 of 18.4 minutes. Most of the rise came from a choice of the senior to add a concurrency token. One run cannot separate the two. |
| The reviewer removed the migration | The fix pass removed it, in 6 of 7 runs, also in the baseline. The risk check ran after that fix, so it saw nothing. |
| The handler had to change, so the nit rule failed | The lead wrote the fix prompt. It asked for "comments 1, 2, 3, 4, 7, 8, 9 (blocking and cheap)". Version 0.5.0 asks the same. The rule was in the wrong file. |
| A nit no longer adds a commit | One run had the same commit with or without the nit. |

The test also could not answer its question. The planted bugs were too easy. No run reported the older bug in `GetTotalAsync` in the `review` scenario. Cost varied by 32% between runs of one arm.

## Method

- Arms: `baseline` is plain Claude Code with `dev-stack`. `current` is `dev-team` 0.5.0. `new` is `dev-team` 0.6.0.
- Model: Sonnet for each agent. Each arm runs from a copy of its plugins, with each agent file on Sonnet.
- Size: 1 run for each arm in each scenario, on the small order API of the evals. So one result can be chance.
- Scoring: `evals/review-flow/score.py` gives the facts, and the hidden tests of `evals/dev-team` score the story. The author of the change read the results, and no blind grader ran.
- Tools: `evals/review-flow/`. The commands are in `run.py`.
- The runs did not read the settings of the user. The file `CLAUDE.md` in the home folder did reach each arm. The runs also wrote to the real profile of the owner: see "Corrections".

## Scenarios

| Scenario | What the arm gets |
|----------|-------------------|
| `story` | The story "cancel an order", and an answer sheet |
| `review` | A built core with 2 planted bugs, 2 nits, an older bug, and a migration, in a light run |
| `comments` | A pull request with 4 comments: a real bug, a nit, a comment on older code, and a repeat of a rejected comment |
| `cap` | The same comments, after 3 earlier rounds. No baseline, because a plain session has no round rule |

## Results

| Scenario | Fact | Baseline | Current | New |
|----------|------|----------|---------|-----|
| `story` | Build, tests, and hidden tests (D1 to D6 of the eval) | Pass | Pass | Pass |
| `story` | Cost and time | $0.22, 0.8 min | $1.46, 8.2 min | $2.45, 18.4 min |
| `story` | Security review | No | No | Yes, by a raise. 0 findings |
| `review` | Both planted bugs fixed | Yes | Yes | Yes |
| `review` | Cost and time | $0.19, 0.9 min | $1.07, 6.6 min | $0.99, 6.1 min |
| `review` | Nits changed in the fix | Yes | Yes | Yes |
| `review` | Older bug changed | No | No | No |
| `review` | Migration | Removed | Rewritten | Removed |
| `review` | `runcheck.py` findings | Not used | 1 (`RUN-HOOK`) | 0 |
| `comments` | Real bug fixed | Yes | Yes | Yes |
| `comments` | Nit changed | Yes | Yes | No. Reply only |
| `comments` | Older bug changed | No | No | No, and recorded in `older-problems.md` |
| `comments` | Repeat comment | Rejected | Rejected | Rejected, with a proposal for a rule |
| `comments` | Cost and time | $0.19, 0.8 min | $0.64, 4.4 min | $0.70, 4.0 min |
| `cap` | Stops, and gives the 3 options | Not run | No. It fixes and also changes the nit | Yes |
| `cap` | Cost and time | Not run | $0.60, 3.3 min | $0.24, 0.8 min |

The numbers for `review` and `cap` of the new arm come from the last run, after two small fixes. The earlier runs gave the same facts, at $0.75 and $0.21.

## What changed

**Better.**

- A nit in a pull request comment no longer starts a code change. This removes the commit that the post warns about.
- The round cap stops the loop, at about 40% of the cost of the next round.
- A restart now logs its commands, so `RUN-HOOK` no longer fires for this reason.
- Older problems and repeat comments now have a record and a proposal for a rule.
- G2 has the block Warnings and the text for the pull request.

**No change.**

- All arms found and fixed the planted bugs, and all arms passed the hidden tests. The baseline did this at a fifth to a third of the cost of the team. This agrees with the earlier results in the README of `dev-team`.

**Worse.**

- A story that adds a migration now gets a security review. In this run, the senior chose to add a concurrency token, so the risk check fired. The cost rose by 68%, and the time more than doubled. The review found nothing.

**Rules that did not hold.**

- The nit rule failed in the fix pass of the core review. The fix renamed the nits in `review`, also after a tighter rule. The handler had to change for a blocking bug, so this has a low cost, but the rule is not reliable.
- The raise did not fire in `review`, because the reviewer removed the migration first. The fix pass removed it in 2 of 2 runs, although the design said that the human asked for it. So a fix pass can remove work that it does not understand.
- G2 did not always follow the template. One run wrote "at HEAD" and no hash, and one run put Warnings after the open points.

## Problems in the test itself

The first set of 11 runs was not valid, and it cost about $16.

The seniors ran on Opus, because the environment variable does not override an agent file. One run ended early, because a headless run stops agents in the background. The cap fixture had only 2 rounds. A planted nit also failed the build. Each run in the table comes from the corrected tools. The total cost of all runs was about $26.

## Bugs that the test found in the new version

- A restart of `deliver-story` wrote no marker. Fixed.
- The Sort row of `process-review` had no status word, so one run wrote an invalid word. Fixed.

## Second test

Date: 2026-10-31. Model: Sonnet for each agent. The owner approved the cut and each change below.

### The fix pass and the risk check

The `review` scenario ran 3 times with the cut version. The fixture is the same as in part 1.

| Fact | Part 1, version 0.6.0 (3 runs) | Cut version (3 runs) |
|------|--------------------------------|----------------------|
| Fix prompt names only chosen comments | No | Yes, 3 of 3 |
| The human's migration still in the change | 0 of 3 | 3 of 3. In 2 runs, the senior made it again with the EF tool, as a blocking comment asked |
| Risk check before the core review | No | Yes, 3 of 3 |
| Failing test in its own commit, before the fix | Not measured | 3 of 3 |
| Warnings at G2 name the migration | Not measured | 3 of 3. The 2 replaced migrations also show as a `gone reason` |
| Planted bugs fixed | 3 of 3 | 3 of 3 |
| Cost | Not comparable | $1.02 to $1.32 |

The rule was the right one, but this is a small sample with a fixture that the author wrote.

These faults remain:

- `RUN-HOOK`: the marker was missing in 1 of 3 restarts. G2 reported it correctly.
- `RUN-LOG`: 2 of 3 runs wrote the status word `G2_READY`. Step 10 now names `G2_SHOWN`. No run tested this text.
- The older bug in `GetTotalAsync`: the review named it in 2 of 3 runs. The count comes from a search for the name, so it does not prove the cause. `older-problems.md` existed in 0 of 3 runs.
- A nit in a fixed method: one run left the name `o` and changed only the comment.

### The round count

| Scenario | Result | Cost |
|----------|--------|------|
| `cap`: 3 earlier rounds, no cause comes back | Shows `Rounds: 4`, and goes on | $0.79 |
| `recur`: a comment says that an earlier fix broke the behavior | Stops before any change, and gives options a and b | $0.22 |

The first `recur` run was not valid: the fixture named a log line that the code did not have. The lead rejected the comment with a search as evidence, and this was correct. The run cost $0.81. After the fix of the fixture, the run stopped. Each scenario ran 1 time.

In that invalid run, the lead rewrote two commits of an agent to remove a trailer, because of the `CLAUDE.md` of the home folder. The skills now forbid this. No run tested this text.

### Bug finding

The test compares two versions of the core review on 6 cases, with 2 runs for each pair (24 runs, $8.13). The version `trimmed` is the cut version. The version `bundle` also has Origin, the nit cap, and the `REVIEW.md` rules.

Five cases hold one bug, and a test proved 4 of these bugs. The fifth is a race, and the fixture has no concurrency token. The tests of each case pass. One case is clean, with an old bug on main.

| Case | Kind of bug | Trimmed: found and blocking | Bundle: found and blocking |
|------|-------------|-----------------------------|----------------------------|
| `c1-total` | Old helper, new use | 2 of 2 | 2 of 2 |
| `c2-deadline` | Old helper, new use | 2 of 2 | 2 of 2 |
| `c3-moved` | Moved check, `>` became `>=` | 2 of 2 | 2 of 2 |
| `c4-race` | A catch that never fires | 2 of 2 | 2 of 2 |
| `c5-rounding` | Rounding to even | 2 of 2 | 2 of 2 |
| `c6-clean` | No bug, so no block | 2 of 2 not blocked | 2 of 2 not blocked |

| Measure | Trimmed | Bundle |
|---------|---------|--------|
| Bugs found | 10 of 10 | 10 of 10 |
| False blocking comments | 0 | 0 |
| Nitpicks | 9 | 9 |
| Old bug in `c6-clean` reported | 0 of 2 | 1 of 2 |
| Cost | $4.02 | $4.11 |

Result: no difference. The test does not show that Origin hides a bug: both old-helper cases stayed blocking in the bundle. It also does not show that Origin helps. The tags were not stable: the same `c2-deadline` bug had the tag `new` in both runs, at the line of the new caller, and `c1-total` had `exposed` in the run that was checked. Each arm found each bug, so the test has no room to show a difference. Each bug also breaks a written criterion, so the review had a pointer to it. A later test needs bugs that no criterion names.

The grader saw the reviews with no name of the version. It scored `false_blocks` only from the code that the reviews quote. A fault in `bugfind.py` showed the version names to the first grader. The session discarded that grade and fixed the fault with a test. A new agent then graded again. The layout of a review can still show the version.

Cost of the second test: about $13.40, with no cost for the graders.

## Source check

The owner asked to check the playbook of Anthropic and the premise of the earlier work.

**The premise.** The work of Farley, Martin, and others is in `dev-flow` 2.0.0. It has examples, acceptance tests that fail first, a lock on these tests, one scenario at a time, and a mutation check. See `docs/work/flow-scale/executable-specs-concept.md`. `dev-team` 0.5.0 has only one part of this: prove a cause with a failing test. So the largest stories get less of this protection than a small item in `dev-flow`.

**The playbook.** These points agree with the changes of this version:

| Playbook | Change |
|----------|--------|
| A bug fix commits a failing test first, and the fix does not edit it (Stage 4) | The fix pass does this |
| An agent that fixes code must not weaken the check (Stage 4). The alternative to a hook is to check the diff | `riskcheck.py` shows the test edits since the last check |
| A skill is advisory, and a policy that must hold needs something deterministic (Stage 3) | The test row for added scope, the `gone reason` line, `RUN-G2` |
| Rehearse the rollback (Stage 5) | The review runs the Down step of a migration |
| Each incident gets an eval (Stage 4) | The removed migration is now a fact in `score.py` |

These points do not agree with this version, or wait:

- The playbook names `REVIEW.md` and a cap of five nits. This version holds them, because the local flow showed no value in a test. The managed review service reads `REVIEW.md`, so a team with that service can still use one.
- The playbook writes `intent.md`, `spec.md`, and `plan.md`. The earlier decision stays: Markdown specifications drift, and the executable test is the specification.
- The playbook asks for 20 to 50 real tasks in an eval suite. This test has 6 small cases.
