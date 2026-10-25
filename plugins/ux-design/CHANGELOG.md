# Changelog

This file records all notable changes to the ux-design plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.1] - 2026-10-25

### Changed

- The `openpencil` skill imports the checked HTML mockups with `opimport.py`. For each state, the script measures the rendered mockup in a browser with the snapshot extractor of OpenPencil, and imports it with `op import:snapshot`. It names each frame `<screen> / <state>` and exports a PNG of each frame. Before, the skill only loaded `openpencil-design`, and each run made its own workaround. The HTML import of OpenPencil breaks CSS tables.

## [0.3.0] - 2026-10-25

### Added

- The `ux-setup` skill. It scans the project for the design tools, the tokens, the brand files, the languages, and the run command. Then it asks one round of questions and writes the `## Design` section of `CLAUDE.md`. Each line is marked `found` or `guess`.
- The `figma` skill, on top of the official Figma plugin. It reads in a fixed order and keeps a call budget for the plan and the seat (`figmamap.py count`). It maps the Figma variables to the project tokens (`figmamap.py map`). It writes mockups only in a draft or duplicate file.
- The `claude-design` skill. It reads a Claude Design System as the design-system source. It turns checked HTML mockups into a Design canvas with one artboard for each state (`html2dc.py`). It makes a Design System from DTCG tokens.
- The `openpencil` skill: a pointer to the `openpencil-design` skill, with a check of the skill, the `op` CLI, and the running app.
- The `brand-rules` skill: `DESIGN.md` in the section order of the DESIGN.md format, and `PRODUCT.md` with a Words table. Also DTCG 2025.10 tokens with a usage note and contrast pairs. `brandcheck.py` checks the tokens, converts them to CSS and to the Claude Design format, and finds banned words in the UI text.
- The `web-accessibility` skill: a WCAG 2.2 AA checklist for the design and for the build, and the status of the EAA and EN 301 549.
- `uxcheck.py review` walks the focus with the Tab key at 1440 px. New rules: `RV-FOCUS-VISIBLE`, `RV-FOCUS-TRAP`, `RV-FOCUS-OBSCURED`, `RV-FOCUS-ORDER`, `RV-TABINDEX`, and `RV-MOTION`. `--no-keys` skips them.

### Changed

- The `ux-designer` agent inherits the tools of the session, except NotebookEdit. Before, its tool list did not include the Artifact tool and the MCP tools of Figma and OpenPencil.
- Mockup mode always makes and checks the HTML mockups first. For Claude Design, Figma, or OpenPencil, it then loads the tool skill.
- Design mode and review mode use the `web-accessibility` skill. When the `## Design` section is missing, the agent asks for `ux-setup`.

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
