# Changelog

This file records all notable changes to the dev-flow plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.1.0] - 2026-10-25

### Added

- A UX review after the code review, for an item with UI work. It needs the `ux-design` plugin. The small tier gets a light review, the medium and large tier a full review. The agent runs on Sonnet, and its walk file stays in the repository.

### Changed

- Intake copies the approved UX design of an item into the work folder. An item with UI work but no approved design is a question for the product owner.
- Example mapping adds one example for each state of the UX design, with the source `ux: <screen>/<state>`. `examplecheck.py` checks each such source against the states of `ux-design.md`.

## [2.0.0] - 2026-10-24

### Changed

- **Breaking.** `plan-and-build` starts from an approved work item (user story, bug, task, or chore), not from an idea. An idea without an item goes to `product-flow:idea-to-backlog` first. The item is the only source of product decisions.
- **Breaking.** Four tiers replace the size check: patch, small, medium, and large.
- A risk sets the tier to medium or higher, for example a schema change, authorization, a new package, or money. A large item needs two counted signals. Each item runs in one session. `dev-team:deliver-story` starts only when the human asks for the team.
- **Breaking.** The specification is executable. Example mapping writes `examples.md`. It holds the rules, the examples below, at, and above each limit, and the words with more than one meaning. It also holds the answers from existing decisions and the open questions. The examples become acceptance tests in four layers (test case, DSL, protocol driver, system).
- The acceptance tests fail before the build starts.
- The gates Spec and Done replace G1 to G4. At the gate Spec, the human approves `examples.md` and the acceptance tests. Large items also have a gate Design. Small items write no `plan.md`.
- After the gate Spec, the skill builds one scenario at a time: red, green, a review of the step, a commit.
- `decisions.md` holds only technical decisions. A new team decision goes through `dev-team:team-decisions` in the medium and large tier.
- The review sub-agent runs on Sonnet and gets only the diff. The skill asks for quiet build and test output.

### Added

- `speclock.py` locks the approved test cases and watches the DSL. A PreToolUse hook (`hooks/guard.py`) blocks each edit of a locked test, also through Bash.
- `mutate.py`, a boundary mutation check: it flips each comparison in the changed code and runs the acceptance tests. A mutation that survives is a rule without an example. When the project has Stryker, the skill runs it too.
- `examplecheck.py` checks `examples.md`: each rule has an example, each limit has the cases below, at, and above, and each open source has a question. A word with more than one meaning in a criterion cannot have a technical or assumed answer.
- Product questions go to the PO as a comment on the item (`references/po-questions.md`), after the human approves the exact text. The skill first searches the existing decisions. In solo work, an answer that cites an existing decision counts as approved.
- In an unattended run, the skill never assumes a new package or a new external service. It also never assumes a fact that only the human or the PO knows. While a question waits, it does not start the build.

### Removed

- The product grill, the questionnaire for a third person, the spec files, and the UI prototype. These are the job of `product-flow`.

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
