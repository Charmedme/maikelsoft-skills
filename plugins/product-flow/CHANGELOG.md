# Changelog

This file records all notable changes to the product-flow plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.4.0] - 2026-10-25

### Changed

- `product-prototype` makes each prototype in the shared `proto-shell/1` standard. The output is still one HTML file that opens offline. It now has the standard header, the toolbar, the variants, the states, the sizes, and Test mode. The checker `protocheck.py` is the shared one.
- `product-prototype` ranks the open points by the cost of a wrong answer, and takes the one with the highest cost as its question.
- `idea-to-backlog` uses the `ux-design:ux-designer` agent in phase 3 when the `ux-design` plugin is installed and the request changes screens. G2 shows the prototype, the mockups, and the proposed decisions. Without the plugin, phase 3 works as before.
- Example mapping adds one example for each state of the approved UX design, with the source `ux: <screen>/<state>`.
- `backlog-story` links the UX design of a story in its notes.

## [1.3.0] - 2026-10-24

### Added

- The skill `example-mapping`, as a business analyst (BA): it turns one approved item into rules, examples, and questions before the build, in `examples.md`. It gives an example below, at, and above each limit. It lists the words with more than one meaning. It searches the existing decisions before it asks the PO. `examplecheck.py` checks the file. `dev-flow:plan-and-build` starts it for each small, medium, or large item.
- Rule A10 of `backlog-story`: each acceptance criterion has one or more concrete examples (Given/When/Then, or a table) in an "Examples" section. A team profile turns it off with `examples: no`.

### Changed

- Rule A5: the criteria stay a checklist of rules, and the examples go under "Examples".

## [1.2.0] - 2026-10-16

### Added

- `idea-to-backlog`: the grill always starts with round 1, the need. It asks 5 questions: current work, where it goes wrong, exceptions, groups that work differently, and success. It also asks them when the documents describe the problem.
- An impact scan in round 2: admin screens, other modules, roles, and the log of each new or changed object.
- The context file has a "Problem" section and an "Open questions" section with an owner and a status. Items refer to an open question by its number (O1, O2).
- A short round with the open questions of each feature, before the stories of that feature.
- An optional gap analysis against an existing backlog, before G3.
- Team profile (`team-profile.md`): the standard of the team wins over the rules of the skills where they conflict. The skills ask one time which template applies.
- `backlogcheck.py --profile <file>`: a team profile can allow Given/When/Then (A5), criteria on an epic (E7), and set a title format (TP).
- New checker rules:
  - A8 (error): an open question in an acceptance criterion.
  - T3 (warning): a part of the screen in a title.
  - OQ (warning): an open question without its number.
- `product-prototype` compares each screen with the decisions before it asks the human.
- Rule A9: a claim without a source is an "Assumption:", also when a human is present.
- The grill test (`evals/product-flow/grill-test/`): a simulated product owner with hidden facts, 2 scenarios, and a score script.

### Changed

- An answer with facts but no choice goes to "Open questions". The skill no longer marks it "assumed".

## [1.1.0] - 2026-10-15

### Changed

- Each feature has its own goal and measure. A new "Supports:" line in the "Goal" section refers to the product goal. Before, each feature had a copy of the product goal.
- The product goal is written one time, in the "Spec" section of the context file. `idea-to-backlog` makes a separate goal item only when the team keeps goals as items.
- `backlog-goal` puts the product goal in the goal place of the ticket system, else in the "Supports:" line of each feature.

### Added

- `backlog-push` always writes each goal to a place that the project has. The order is: the goal object of the system, a goal field of the item type, or the first section "Goal" of the description. The approval summary and the report show the place of each goal.
- The fake tracker for tests can show the fields of a type and set fields.
- Rule F2b and a checker warning: the goal of a feature is the same as the goal of another feature, or as its "Supports:" line.

## [1.0.0] - 2026-10-12

### Added

- First release of the product-flow plugin for Claude Code, for product owners and business analysts.
- The skill `idea-to-backlog`. It takes a request to approved backlog items in six phases: scan and research, grill, prototype, spec, backlog, and push. Four gates where the human decides.
- One context file for each run (`backlog-<slug>.md`), so that a later run can continue.
- The skills `backlog-story`, `backlog-feature`, `backlog-epic`, and `backlog-goal`. Each writes, rewrites, and reviews its item type. `backlog-story` also splits stories.
- Levels: 2 by default (Feature, User Story), 3 on request (Epic, Feature, User Story). Profiles: Agile, Scrum, and SAFe.
- The skill `product-prototype`. It builds a throwaway clickable prototype with the design tokens, design system, or branding that it finds.
- The skill `backlog-push`. It puts the items into Jira, Azure DevOps, GitHub, GitLab, or Linear, or writes them to files. It first does a duplicate check and gets the approval of the human. A push log lets a stopped push continue.
- The checkers `backlogcheck.py`, `backlogtree.py`, and `protocheck.py`.
- The shared language rules: English in ASD-STE100 Simplified Technical English, and Dutch in Begrijpelijk Nederlands (B1).
- Test results for each skill against a baseline without the skill.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/product-flow-v1.3.0...HEAD
[1.3.0]: https://github.com/Charmedme/maikelsoft-skills/compare/product-flow-v1.2.0...product-flow-v1.3.0
[1.2.0]: https://github.com/Charmedme/maikelsoft-skills/compare/product-flow-v1.1.0...product-flow-v1.2.0
[1.1.0]: https://github.com/Charmedme/maikelsoft-skills/compare/product-flow-v1.0.0...product-flow-v1.1.0
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/product-flow-v1.0.0
