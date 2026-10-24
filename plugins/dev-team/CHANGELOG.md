# Changelog

This file records all notable changes to the dev-team plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
