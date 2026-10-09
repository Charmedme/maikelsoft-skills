# Changelog

This file records all notable changes to the dev-flow plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-10-09

### Added

- First release of the dev-flow plugin for Claude Code.
- The skill `plan-and-build`. It takes an idea to working code in six phases: research, grill, plan, prototype, build, and review.
- Four gates where the human decides: G1 shared understanding, G2 prototype validated, G3 tickets approved, G4 done.
- One file for each item in a work folder, by default `docs/work/<slug>/`. The items are the plan, the research, the decisions, the specs, the tickets, and the questionnaires.
- A questionnaire for a question that only a third person can answer. After that point, the skill does not ask the human about that subject.
- A size check (small, medium, large) that sets the depth of each phase.
- `workcheck.py`: a checker for the work folder. It checks the ticket format, the blockers, cycles, the gate order, and the agreement between the plan and the tickets. With `--next`, it gives the tickets that can start now.
- The shared language rules: English in ASD-STE100 Simplified Technical English, and Dutch in Begrijpelijk Nederlands (B1). The format is fixed: Markdown.
- An eval set with three evals, and the results of two test rounds against the six source skills.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-flow-v1.0.0...HEAD
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/dev-flow-v1.0.0
