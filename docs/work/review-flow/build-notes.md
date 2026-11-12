# Build notes for the review flow

These are small fixes from the build rehearsal of 2026-10-31. Each one keeps the intent of `design.md`, so the builder applies them with no new decision. The numbers are the IDs of the rehearsal report.

## Risk check

- MINOR-2: when a local default branch exists, use the newer of it and `origin/HEAD`. Exit with code 2 when no default branch exists.
- MINOR-3: use fixed output lines `reason: <kind> <path>`, and an option `--since <file>` that prints only new reasons.
- MINOR-4: when a raise starts at the second check, copy `risk-2.txt` over `risk-1.txt`. Else a restart writes a false line in Warnings.
- MINOR-5: keep a run of capitals as one word, so that `JWTService` gives `JWT` and `Service`.
- MINOR-6: compare assertions as text with no whitespace, and count duplicates.
- MINOR-7: write the mode first, then the `RISK_CHECKED` row. Else a restart from that row skips step 8.
- MINOR-13: the column Checks of a `RISK_CHECKED` row holds `-`.

## Rounds and restart

- MINOR-8: the answer row of the round cap uses the status `NEEDS_HUMAN`, with NN in the step name.
- MINOR-9: add a section Stops to `process-review`. A restart resumes at the first step with no row. It does not write the classes again, and it does not add a second `FOLLOWUP_STARTED` row. A lost agent in a step uses the same counter value. Record the commit before the fix.
- MINOR-9: a new batch after a crash starts a new follow-up after the open one ends.
- MINOR-11: use `git rev-parse --git-path info/exclude`, because `.git` is a file on a linked worktree.
- MINOR-12: remove the marker before the commit at G2 too. Five `commands.jsonl` files in a real project are dirty because of this order.
- MINOR-15: write `Rounds: N` in the reason of the `FOLLOWUP_DONE` row, so that it stays as data.
- MINOR-17: match `^01 fix` in `RUN-FIX`, because a row `followup 01 fix` now satisfies package 01.

## Files that the design does not name

- MINOR-14: `agents/senior.md` (mode `fix` names only `NN-comments.md`), `senior-practice/SKILL.md`, and `templates.md` ("light run only", also for `raised`).

## Evals

- MINOR-19: `grade.py` skips a case that `cases.load` does not know, with no message and exit 0. `prepare.FIXTURES` points only to `dev-stack`. Give the runner its own source for cases, fixtures, and planted files. Do not put the cases in `evals/dev-stack/cases/`.
- MINOR-20: add `evals/review-flow/decide.py` for the candidate rule, the total of 9, the limits, and the win count for the new measures. Keep the runs of stage 6 in a separate folder. Adapt `grader.md`, which names .NET 10.
- MINOR-22: `flow` writes only `review.md`. The script reads its table for level and Origin.
- MINOR-23: the prompt of `flow` gives the whole `review.md` without the fix and run steps, plus the compare point.
- MINOR-24: `--plugin-dir` names both `dev-team` and `dev-stack`, because the agent preloads `dev-stack:secure-coding`. Keep the fixture run folder out of `main...feature`.
- MINOR-26: a replacement case and a candidate add runs. Count about 190 runs for `flow`.

## Before each commit

- MINOR-18: run `doccheck.py` on each changed Markdown file. CI also reads `docs/work/`.
