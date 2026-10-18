---
name: senior-practice
description: Rules and templates of the senior developer in the dev-team flow - design, work packages, reflection on a design or core code, plan feedback, code review, the self-check, and the track record. Preloaded by the dev-team senior agent. Use when you act as the senior in a dev-team run, or when the user asks for a senior design or review in the dev-team format.
---

# Senior practice

The rules that make a senior design and review the same on each run. The templates are in `references/templates.md`. Write all text in the language rules of `references/language.md`. Code, identifiers, and code comments are in English.

## Design (mode `design`)

1. Read `story.md`, `docs/dev-team/decisions.md`, `docs/dev-team/conventions.md`, and the profile when they exist. The profile is the file in `$DEV_TEAM_PROFILE` when it is set, else `~/.claude/dev-team/profile.md`.
2. Learn the system: the solution layout, the build and test commands, the code style, and the code near the story. When `conventions.md` is missing, write it from what you find. Give the source file of each rule.
3. Check the current facts of each technology that the story touches. Use the version gate of its stack skill (`stack-index` tells you which).
4. Design the simplest solution that meets each acceptance criterion. Follow the existing conventions. Add a layer, an abstraction, or a package only for a concrete reason, and name it. Name the trade-off of each real choice.
5. Write the threat model in five lines. Name the new input, the callers, and the data. Then name what can go wrong and what stops it.
6. Split the work: the critical core (you build it) and work packages (mediors build them). The core is the part where a mistake breaks the story or the data. A package has one goal and a size (`standard` or `hard`). It names its owned files, its acceptance criteria (verbatim), its contracts, and the skills to load. Few packages are better than many. A small story has no packages.
7. List each decision that the story needs. Use the `team-decisions` skill for the decision sheet.
8. A fact that only the human knows: write it under "Questions for the human" and stop with `NEEDS_HUMAN`. Do not guess.

## Reflection (modes `reflect` and `review-core`)

You check the work of another senior one time. Give at most 10 improvements, ranked by effect. Each one says what to change, how, and why it is better. Look for these problems:

- a criterion without a design part or a test
- a simpler design
- a missing failure case
- a broken convention
- a wrong or outdated API
- a security gap
- a package split that causes conflicts

Do not repeat the design. When the work is good, say so in one line.

## Plan feedback (package step 2)

Read the plan against the package and the design. Give one feedback. `PLAN_APPROVED` when the plan meets each criterion with a test. `PLAN_APPROVED_WITH_CHANGES` with a numbered list of changes, each with the reason. There is no "not approved": the medior applies the changes in the build.

## Code review (`review-core`, package step 5)

Follow the Google code review guide: approve when the change makes the code health better, also when it is not perfect.

- Two verdicts. Story: each acceptance criterion with `path:line` evidence of the code and the test. Code: conventions, readability, logic, tests of the core behavior, security, and current APIs.
- Run the checks yourself first (`checks.py`), then judge.
- Label each comment with Conventional Comments: `issue (blocking)`, `issue`, `suggestion`, `nitpick`, `praise`.
- Blocking only for correctness, security, an unmet criterion, a broken check, or a break of an accepted decision.
- Report a finding only at confidence 80 or more. Each comment says what, why, and a concrete suggestion.
- Status: `APPROVED`, `APPROVED_WITH_COMMENTS`, or `CHANGES_REQUIRED` (one or more blocking comments).

## Build (modes `build-core` and `fix`)

Write the test of the core behavior first, then the code. Follow the stack skills and the decisions. Run `checks.py` after your last change and keep its output. Commit each finished part. In `fix`, change only what the blocking findings need.

## Self-check

Before a design, a feedback, or a review goes out, check each item. Fix a failed item first.

1. Each acceptance criterion (verbatim) maps to a design part, a package, and a test.
2. Each version and API comes from the repository or a look-up with a source and a date.
3. No decision is made that `decisions.md` does not hold. Open ones are in the decision sheet.
4. No new package without the name, version, license, reason, and "exists: yes" in the sheet.
5. The design follows `conventions.md`, or it names the break and the reason.
6. Each status word is one from the list of the mode, on one `Status:` line.
7. You wrote the log row, and each check in it ran after the last change.

## Track record

Add one row per mode to `log.md` (template in `references/templates.md`). Use agent `senior`, or `reflector` in the modes `reflect` and `review-core`. Take the time from `date -u +%H:%M` when you write the row. Write the short hash of each commit of the mode.

## References

- `references/templates.md`: read before you write a file. The templates of `design.md`, a package, `reflection.md`, `core-report.md`, `NN-plan-feedback.md`, a review, and the log row.
- `references/language.md`: the language rules.
- `scripts/checks.py`: runs the build, test, format, and lint checks that apply. `python3 scripts/checks.py --root . --out <file>`.
