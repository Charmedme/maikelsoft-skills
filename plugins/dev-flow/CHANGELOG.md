# Changelog

This file records all notable changes to the dev-flow plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [3.0.0] - 2026-11-12

### Changed

- **Breaking.** The skill `plan-and-build` has the new name `build`. Start it with `/dev-flow:build`. The old command still works: it sends you to `build`.
- **Breaking.** The ticket is the contract. `build` reads the section "Behaviour spec" of the ticket and copies it into `spec.md`. Each rule of the spec gets one test file, and the skill locks each test file before the build. This spec replaces example mapping in the build.
- A ticket without a Behaviour spec, or with a draft spec, first gets the refine pass. Then `build` waits for the approval of the product owner. This works without the product-flow plugin.
- When there is no ticket, `build` drafts the item and pushes it after your approval.
- The size of the change (patch, small, medium, large) replaces the word "tier". `run.md` holds the header of the run.
- `workcheck.py` checks the new work folder. Each part of `spec.md` needs a check: a test file, or "review only". A medium or large change also needs the order of the scenarios in `plan.md`.
- The mutation check `mutate.py` runs one time on the locked test files, with a limit, while the reviews run.
- A scenario has no step review of its own. The code review checks that each change stays inside its rule.

### Added

- `review-comments`: handles the review comments on a pull request of a solo build. It sorts each comment, fixes it, has a fresh agent review the fix, pushes without force, and replies in each thread. It never resolves a thread.
- Held-back tests for a large change, or for a medium change with a risk. The plan reviewer writes them from the spec before the code exists. `heldback.py` keeps them outside the repository and runs them in the proof step. The guard hook blocks each direct read of their folder, so the builder cannot see them.
- A security review at each size from medium up. At medium, the one code reviewer also does the security review. At large, the second review pass does it. At patch and small, a separate agent does it when `riskcheck.py` finds a risk in the change.
- The docs step: `docscheck.py` names each doc that the change makes old.
- The final report gives the testing, the risk, how to undo the change, and the commits.

### Deprecated

- The command `/dev-flow:plan-and-build`. Use `/dev-flow:build`. A later major version removes the old command.

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

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-flow-v3.0.0...HEAD
[3.0.0]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-flow-v2.1.0...dev-flow-v3.0.0
[2.1.0]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-flow-v2.0.0...dev-flow-v2.1.0
[2.0.0]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-flow-v1.0.0...dev-flow-v2.0.0
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/dev-flow-v1.0.0
