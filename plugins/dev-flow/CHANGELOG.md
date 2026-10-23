# Changelog

This file records all notable changes to the dev-flow plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-10-23

### Changed

- **Breaking.** `plan-and-build` starts from an approved work item (user story, bug, task, or chore), not from an idea. An idea without an item goes to `product-flow:idea-to-backlog` first. The item is the only source of product decisions.
- **Breaking.** Four tiers replace the size check: patch, small, medium, and large. Each tier has its own steps and gates: Done for patch and small, Plan and Done for medium. A large item runs with `dev-team:deliver-story`. A risk sets the tier to medium or higher. Examples: a schema change, authorization, a new package, money.
- The type of the change (bug, refactor, feature, chore) changes the steps of the tier.
- The gates Design (large without dev-team), Plan, and Done replace G1 to G4.
- `decisions.md` holds only technical decisions, with the item key on each row.
- A new team decision goes through `dev-team:team-decisions` when the repository has a team decision index.
- `workcheck.py` checks the tier, the gates of the tier, and the AC coverage of the tickets. It also checks that the gate Plan is not "assumed" while a question waits for the PO.

### Added

- Product questions go to the PO as a comment on the item (`references/po-questions.md`). The skill drafts the comment, the human approves the exact text, and the skill posts it.
- In an unattended run, the skill never assumes a new package or a new external service. It also never assumes a fact that only the human or the PO knows.
- A technical spike replaces the prototype. A UI or flow prototype is a product question (`product-flow:product-prototype`).

### Removed

- The product grill (the need, the users, the scope), the questionnaire for a third person, the spec files, and the UI prototype. These are the job of `product-flow`.

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

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-flow-v2.0.0...HEAD
[2.0.0]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-flow-v1.0.0...dev-flow-v2.0.0
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/dev-flow-v1.0.0
