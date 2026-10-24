# Changelog

This file records all notable changes to the ux-design plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-10-24

### Added

- The `ux-designer` agent with a design mode and a mockup mode.
- The `ux-practice` skill: the four phases, the design spec with a source tag for each decision, and the UX decision log. Also evidence tags, opportunities, light mode, and the checker `uxcheck.py` (modes `design`, `mockup`, and `system`).
- The `ux-patterns` skill: decision tables for 12 needs, from lists and filters to empty states and overview screens.
- The `ui-craft` skill: the visual method in a fixed order, the option and mockup rubrics, and the list of AI tells. Also the foundation for a project without a design system, and the audit of an existing design system.
- The `ux-states` skill: the core states, one state for each error kind, the business states, and a stress list.
- An eval set in `evals/ux-design/` with three fixtures (strong, empty, and faulty design system), a runner, scores, determinism, and blind pairwise graders.
