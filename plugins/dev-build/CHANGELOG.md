# Changelog

This file records all notable changes to the dev-build plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.1] - 2026-10-29

### Changed

- `build-skill` and `build-agent` have the new copies of the shared language checker (`doccheck.py`) and the language rules (`language.md`) from dev-docs 1.3.0 and 1.4.0. The copies in 1.1.0 were older.
- The checker has three new accessibility warnings: `A11Y-ALT`, `A11Y-LINK`, and `A11Y-HEADING`. `HOUSE-WORDS` also finds "see below", "the following command", and "please". A sentence that starts with a lowercase word (`v4`, `npm`) is now a new sentence.
- The language rules have the new sections "Procedures" and "Accessibility".

## [1.1.0] - 2026-10-12

### Added

- A grill phase at the start of `build-skill` and `build-agent`. The skill interviews the human in rounds. Each question has a recommended answer. Round 1 asks three things: is a skill or an agent the right form, which steps the human does today, and what goes wrong today.
- A brief: the record of the decisions. For a skill, it is `brief.md` in the skill folder. For an agent, it is `docs/work/<agent-name>/brief.md`. The test assertions come from the success criteria in the brief.
- Gate G1: the human approves a summary of the brief in its own message. The skill writes no skill or agent file before G1.
- `shared/grill.md`, synced only into the dev-build skills. `sync_shared.py` now supports shared files for one plugin.
- An eval with a simulated human and hidden facts (`evals/dev-build/humans/monday-mail.md`), and the results of three rounds.

### Changed

- Improve mode starts from one bad run, not from all topics.
- The gate question is part of round 1. It is no longer a separate step.
- In a run without a human, the brief records each choice as "recommended, not confirmed", with the status "not approved".

## [1.0.0] - 2026-10-10

### Added

- First release of the dev-build plugin for Claude Code.
- The skill `build-skill`. It creates, improves, or reviews an agent skill in four steps: research, concept, test, and build. It has a gate that checks if a skill is the right form, and a test method with a results table.
- The skill `build-agent`. It creates, improves, or reviews an agent. It starts from a role spec and renders it for Claude Code, the Agent SDK, Managed Agents, or OpenClaw.
- `skillcheck.py`: a checker for a skill folder. It checks the name, the description, the length, the links, the references, the frontmatter fields, and the style.
- `agentcheck.py`: a checker for an agent file or an OpenClaw workspace.
- Rules with IDs (W, P, and A rules) so that a review can cite them.
- The shared language rules: English in ASD-STE100 Simplified Technical English, and Dutch in Begrijpelijk Nederlands (B1).
- An eval set for both skills, and the results of the test rounds against a plain agent.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-build-v1.1.0...HEAD
[1.1.0]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-build-v1.0.0...dev-build-v1.1.0
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/dev-build-v1.0.0
