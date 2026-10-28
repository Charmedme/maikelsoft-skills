---
name: senior-practice
description: Rules and templates of the senior developer in the dev-team flow - design, work packages, reflection on a design or core code, plan feedback, code review, the self-check, and the track record. Preloaded by the dev-team senior agent. Use when you act as the senior in a dev-team run, or when the user asks for a senior design or review in the dev-team format.
---

# Senior practice

The rules that make a senior design and review the same on each run. The templates are in `references/templates.md`. Write all text in the language rules of `references/language.md`. Code, identifiers, and code comments are in English.

## Design (mode `design`)

1. Read `story.md`, `docs/dev-team/decisions.md`, `docs/dev-team/conventions.md`, and the profile when they exist. The profile is the file in `$DEV_TEAM_PROFILE` when it is set, else `~/.claude/dev-team/profile.md`.
2. Learn the system: the solution layout, the build and test commands, the code style, and the code near the story. When `conventions.md` is missing, write it from what you find. Give the source file of each rule. Also do these three things:
   - Find the sibling code. For each kind of work in the story, find how the neighbour modules do the same job. Examples: a child collection that syncs, a query by specification, a validator, a mapping. Write that pattern in `conventions.md` with its file. Follow it, or name the break and the reason.
   - Write the build and test commands under "Build and test" in `conventions.md`. Include any pin of the SDK or of a tool. Agents use these commands. No agent makes a temporary `global.json`.
   - Find the pipeline of the project (for example `.github/workflows`, or `azure-pipelines.yml`). Write the checks it runs under "CI checks", one command in backticks for each bullet. `checks.py` runs them. When you find no pipeline, leave the section out. The check output then says that parity is not known.
3. Check the current facts of each technology that the story touches. Use the version gate of its stack skill (`stack-index` tells you which).
4. Design the simplest solution that meets each acceptance criterion. Follow the existing conventions. Add a layer, an abstraction, or a package only for a concrete reason, and name it. Name the trade-off of each real choice.
5. Write the threat model in five lines. Name the new input, the callers, and the data. Then name what can go wrong and what stops it.
6. Name the aggregates first. For each entity of the story, name the aggregate root that owns it. Say who may change it. Say what stays consistent in one transaction. Feature folders and packages follow the aggregates, not the tables of a data model. An entity with two possible owners is a decision for the sheet. Use the `domain-modeling` skill when it is installed.
   Then split the work: the critical core (you build it) and work packages (mediors build them). The core is the part where a mistake breaks the story or the data. A package has one goal and a size (`standard` or `hard`). It names its owned files, its acceptance criteria (verbatim), its contracts, and the skills to load. Few packages are better than many. A small story has no packages.
7. List each decision that the story needs. Use the `team-decisions` skill for the decision sheet.
8. An open question with a safe default goes to G1. A default is safe when the story supports it, and when it is easy to change later. Choose the default and write it under "Assumptions to confirm" in `design.md`, with the other options. Then go on with the design.
9. Stop with `NEEDS_HUMAN` only when no safe default exists. Then the work needs a fact that only the human knows.
10. Choose the run mode. `light` when all of these are true: no new package, no schema change, and no work packages. Also no change to sign-in, authorization, secrets, file upload, or an outbound call. Else `full`. Write the mode and the reason in `design.md`. A light run has no reflection and no separate security review. Examples: a provider, an address, a legal rule. Write each question under "Questions for the human". Do not guess.

## Reflection (modes `reflect` and `review-core`)

You check the work of another senior one time. Give at most 10 improvements, ranked by effect. Each one says what to change, how, and why it is better. Look for these problems:

- a criterion without a design part or a test
- a simpler design
- a missing failure case
- a broken convention
- a wrong or outdated API
- a security gap
- a package split that causes conflicts
- a feature folder that follows a table, not an aggregate
- code that does not follow the pattern of the sibling modules

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
- In a light run, the core review also does the security review: check the diff against the review checklist of `dev-stack:secure-coding`. Write the result in a section "Security" of `core-review.md`.

## Build (modes `build-core` and `fix`)

Write the test of the core behavior first, then the code. Follow the stack skills and the decisions. Run `checks.py` after your last change and keep its output. Commit each finished part. In `fix`, change only what the blocking findings need.

Prove a cause before you fix it. A cause is a hypothesis until a test fails for it. This holds for a claim of a reviewer and for your own idea. A sync by key gets a test with two items of the same kind.

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
