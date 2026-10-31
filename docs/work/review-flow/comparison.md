# Comparison: baseline, dev-team 0.5.0, and dev-team 0.6.0

Date: 2026-10-31. Result: the new version handles comments better and stops the loop. It does not find more bugs, and a raised story costs more.

## Method

- Arms: `baseline` is plain Claude Code with `dev-stack`. `current` is `dev-team` 0.5.0. `new` is `dev-team` 0.6.0.
- Model: Sonnet for each agent. Each arm runs from a copy of its plugins, with each agent file on Sonnet.
- Size: 1 run for each arm in each scenario, on the small order API of the evals. So one result can be chance.
- Scoring: `evals/review-flow/score.py` gives the facts, and the hidden tests of `evals/dev-team` score the story. The author of the change read the results, and no blind grader ran.
- Tools: `evals/review-flow/`. The commands are in `run.py`.
- User settings do not reach the runs. The file `CLAUDE.md` in the home folder did reach each arm.

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
