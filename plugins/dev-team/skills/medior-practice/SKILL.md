---
name: medior-practice
description: Rules and templates of the medior developer in the dev-team flow - plan a work package, build it test first, run the checks, report with real command output, and do one fix pass. Preloaded by the dev-team medior agents. Use when you act as a medior in a dev-team run, or when the user asks for a package plan or report in the dev-team format.
---

# Medior practice

The rules that make a medior plan and build the same on each run. The templates are in `references/templates.md`. Write all text in the language rules of `references/language.md`. Code, identifiers, and code comments are in English.

## Plan (mode `plan`)

1. Read the package, `design.md`, `docs/dev-team/decisions.md`, and `docs/dev-team/conventions.md`.
2. Read the code that the package touches and the tests near it.
3. Restate the goal in one sentence. Quote each acceptance criterion verbatim.
4. List the files that you change or add. Each one is an owned file of the package, or it has a reason.
5. List the tests, core critical behavior first. Each criterion has at least one test. Name each test as it will be in the code.
6. List the risks and the deviations from the design, each with the reason.
7. Run the self-check. Write `packages/NN-plan.md` with `Status: PLAN_READY`.

A missing fact, a decision that `decisions.md` does not hold, or a needed package: stop with `NEEDS_CONTEXT` and name it. Do not guess.

## Build (mode `build`)

1. Apply each change of `packages/NN-plan-feedback.md`.
2. Load the stack skill of each technology that you touch. Read its version gate first.
3. Write the test of the core behavior first and see it fail. Then write the code.
4. Keep the contracts of the package as they are. Follow `conventions.md` and the decisions.
5. Run the checks after your last change: `python3 scripts/checks.py --root . --out <run folder>/packages/NN-checks.md`. Fix each failed check and run them again.
6. Commit with a clear message. Write `packages/NN-report.md`.

## Fix (mode `fix`)

Fix each blocking comment of `packages/NN-review.md` one time. Change only what the comment needs. Run the checks again. Commit. Add the section Fix to `NN-report.md`.

## Report

The report shows real command output, not memory. The Checks section is the table of `NN-checks.md` from the run after your last change. A check that you did not run after the last change gets "not run after the last change". Map each acceptance criterion to its test (`path:line`).

## Self-check

Before a plan or a report goes out, check each item. Fix a failed item first.

1. Each acceptance criterion (verbatim) has a test.
2. The package owns each changed file, or the file has a reason.
3. No new package, decision, or contract change.
4. The checks ran after the last change, and the report shows that run.
5. The status word is one from the list of the mode, on one `Status:` line.
6. You wrote the log row.

## Track record

Add one row per mode to `log.md` (template in `references/templates.md`). Use agent `medior` or `medior-large`, and the step `NN plan`, `NN build`, or `NN fix`. Take the time from `date -u +%H:%M` when you write the row. Write the short hash of each commit of the mode.

## References

- `references/templates.md`: read before you write a file. The templates of `NN-plan.md`, `NN-report.md`, and the log row.
- `references/language.md`: the language rules.
- `scripts/checks.py`: runs the build, test, format, and lint checks that apply.
