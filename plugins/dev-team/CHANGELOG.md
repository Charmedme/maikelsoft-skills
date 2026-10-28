# Changelog

This file records all notable changes to the dev-team plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.0] - 2026-10-28

The changes come from the field report of a real run. Each point of the report is sorted in `docs/work/dev-team/field-report-2026-10-28.md`. Facts of one project (analyzer rules, an SDK version, a code pattern) are not in the plugin.

### Added

- Skill `process-review`: handles the comments of a pull request review. It sorts each comment, sends the fixes to the senior, has a second senior review the fix, runs the checks, and writes draft replies.
- Section "After G2" in `deliver-story`: later changes of the story go through the agents, not inline. The status words `FOLLOWUP_STARTED` and `FOLLOWUP_DONE` and the counters `followup NN fix` and `followup NN review` are new.
- A SessionStart hook. In a project with `docs/dev-team/conventions.md` or `decisions.md`, it names the entry skill for each kind of work.
- `checks.py` runs the commands of the section "CI checks" in `conventions.md`. Without the section, its output says that the parity with the pipeline is not known.
- `decisioncheck.py` has the rule `DEC-ADR-DUPLICATE`: two accepted ADR files with one decision ID.
- `runcheck.py` has the rule `RUN-HOOK`: one finding for the cause when `commands.jsonl` is missing or empty.
- `senior-practice` looks at how sibling modules do the same job. It names the aggregates before the split of the work. It writes the build commands and the CI checks in `conventions.md`.
- `story-intake` records a mock-up of the story as a `Mock-up:` line.

### Changed

- Both hook commands try `python3`, then `python`, then `py -3`. On Windows, `python3` can be a Store alias that is not Python, and the hook failed without a message.
- Step 9 of `deliver-story` is skipped only when the story has no UI work. A mock-up in the story counts as a design source. `runcheck.py` needs the UX review for it.
- `deliver-story` finds an open run by the path, by `.dev-team/active-run`, or by the branch. It commits the run folder at G1 and at G2. It does not show G2 as clean when `RUN-HOOK` has a finding.
- `decisioncheck.py` compares IDs without case, space, and dash differences. With `--changed`, it reads only the lines that the branch added, so old code in a touched file is no finding. It sorts the findings by rule and counts them for each rule.
- The senior proves a cause with a failing test before the fix. A sync by key gets a test with two items of the same kind.

### Known limit

- The `ux-design` agent reads `ux-design.md` as the design. For a story with only a mock-up, `deliver-story` passes the mock-up in the prompt as `Design source:`. The `ux-design` plugin does not read it as a design source yet.

## [0.4.0] - 2026-10-25

### Added

- Step 9 of `deliver-story`: a UX review when the run folder has `ux-design.md`, with the counters `ux-review` and `ux-fix` (limit 1 each). G2 shows the result.
- `runcheck.py` needs the UX review before G2 when the run has `ux-design.md`.

### Changed

- `story-intake` of the run copies the approved UX design of the story. The senior fix mode also takes `ux-review.md`.

## [0.3.0] - 2026-10-24

### Changed

- `deliver-story` is for an item that the human wants the team to build. `dev-flow:plan-and-build` builds each item in one session, and starts the team only when the human asks.
- At the design step, the team lead sorts the questions of the senior. Product questions go to the product owner as a comment on the item (`references/po-questions.md`). Technical questions go to the human.
- No agent and no team decision changes an acceptance criterion or a decision of the PO.

## [0.2.1] - 2026-10-20

### Changed

- `stack-index` routes to the 11 new skills of dev-stack 1.1.0: `aspnetcore-openapi`, `fluentvalidation`, `sqlserver`, `postgresql`, `dotnet-observability`, `redis`, `elasticsearch`, `rabbitmq`, `wolverine`, `mailkit`, and `puppeteersharp`. The planned list keeps only the front-end and container skills.

## [0.2.0] - 2026-10-18

### Added

- Light mode for small stories: no reflection and no separate security review. The core review checks the diff against the secure-coding checklist. G1 shows the mode.
- "Assumptions to confirm" in the design and at G1, for open questions with a safe default.
- The environment variable `DEV_TEAM_PROFILE` sets the path of the profile.
- The plugin is in the marketplace as a preview.

### Changed

- The senior stops with `NEEDS_HUMAN` only when no safe default exists.
- deliver-story loads story-intake with the Skill tool, also for a story in a file.
- Log rows take the time from `date -u` and the commit hash from `git log`.

### Fixed

- `runcheck.py`: a reflector row counts the commands of the senior agent. The verdict under a "## Verdict" heading counts.
- `decisioncheck.py`: DEC-USED skips test code, the run files in `docs/work`, and the `exclude` paths of a rule. A supersede link in plain text counts.

## [0.1.0] - 2026-10-18

### Added

- Agents `senior`, `medior`, `medior-large`, and `security-reviewer`.
- Skills `deliver-story`, `story-intake`, `senior-practice`, `medior-practice`, `stack-index`, and `team-decisions`.
- Scripts `checks.py`, `runcheck.py`, and `decisioncheck.py`, and the hook `log-command.py`.
