# Changelog

This file records all notable changes to the ux-design plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-10-25

### Added

- Prototype mode of the `ux-designer` agent, and the `ux-prototype` skill. A prototype uses the shared `proto-shell/1` standard: the same header, toolbar, variants, states, sizes, and Test mode in each prototype. The skill ranks the open questions by risk, adds the failure states of the path, and writes a test plan from a template.
- Review mode of the agent, and the `ux-review` skill. A walk file makes each state of the built app with Playwright and fake server answers. The checker makes screenshots at 390, 820, and 1440 px, and runs axe-core and the tell checks. Each finding has a severity, an origin, and an owner.
- `uxcheck.py review` for the walk file.

### Changed

- The tell checks skip elements that the user cannot see (opacity 0, `aria-hidden`).

## [0.1.0] - 2026-10-24

### Added

- The `ux-designer` agent with a design mode and a mockup mode.
- The `ux-practice` skill: the four phases, the design spec with a source tag for each decision, and the UX decision log. Also evidence tags, opportunities, light mode, and the checker `uxcheck.py` (modes `design`, `mockup`, and `system`).
- The `ux-patterns` skill: decision tables for 12 needs, from lists and filters to empty states and overview screens.
- The `ui-craft` skill: the visual method in a fixed order, the option and mockup rubrics, and the list of AI tells. Also the foundation for a project without a design system, and the audit of an existing design system.
- The `ux-states` skill: the core states, one state for each error kind, the business states, and a stress list.
- An eval set in `evals/ux-design/` with three fixtures (strong, empty, and faulty design system), a runner, scores, determinism, and blind pairwise graders.
