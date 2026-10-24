# ux-design

ux-design is a Claude Code plugin with a UX designer agent and its practice skills. The agent designs the user side of one story: the problem frame, the flow, the patterns, the screens, and the states. It gives each design decision a source, so the same story gives the same design again. Then it makes mockups from the design system of the project.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

Status: version 0.1.0, preview. This version has the design mode and the mockup mode. The prototype mode, the review mode with Playwright, and the calls from product-flow and dev-flow come in a later version. Read the test results below before you use it on real work.

## Install

You need Claude Code, Python 3.9 or later, and Python Playwright for the mockup checks.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install ux-design@maikelsoft-skills
pip install playwright
```

## Quick start

1. Add a `## Design` section to the `CLAUDE.md` of your project:

   ```markdown
   ## Design
   - Mockup tool: html
   - Design-system source: design-system/tokens.css, design-system/components.css, DESIGN.md
   - Brand files: DESIGN.md, PRODUCT.md
   - Languages: en
   ```

2. Put the story in a work folder, for example `docs/work/fl-31-assign/story.md`.
3. Ask Claude Code:

   ```
   Use the ux-designer agent in design mode, then in mockup mode, for docs/work/fl-31-assign.
   ```

4. Read `ux-design.md` and the mockups in `mockups/`. Each proposed decision and each opportunity waits for the product owner.

## What is in the plugin

| Part | Name | What it does |
|------|------|--------------|
| Agent | `ux-designer` | Design mode: frame, opportunities, flow, patterns, options, design spec, states, copy, accessibility, self-check. Mockup mode: one frame for each state, then a check and a critique. |
| Skill | `ux-practice` | The method: four phases, the design spec with a source for each decision, the decision log, evidence tags, opportunities, light mode, and the `uxcheck.py` checker. |
| Skill | `ux-patterns` | Decision tables that select an interaction pattern from the facts of the task, each rule with an ID. |
| Skill | `ui-craft` | The visual method in a fixed order, the option and mockup rubrics, and the list of AI tells. Also a foundation for a project without a design system, and an audit for an existing one. |
| Skill | `ux-states` | The state inventory: the core states, each error kind, and the business states. Each state says what the user sees, can do, and how the user recovers. |

## How it works

- **No empty decisions.** Each screen has 10 decisions (purpose, main task, pattern, layout, hierarchy, components, density, states, copy, motion). Each decision names its source: the product, the design system, a pattern rule, or a decision in `docs/ux/decisions.md`. A decision without a source becomes a proposed decision for the product owner.
- **Opportunities stay visible.** The agent proposes 1 to 3 extras beyond the acceptance criteria, each from a fact of the product. The mockups show each one in its own `proposed-op-<n>` frame.
- **The design system wins, and gets checked.** Without a design system, the agent makes a minimal foundation in the work folder. With one, it audits it first. It reports each fault in `design-system-issues.md` and fixes the serious ones in a marked `ds-fixes.css`. It never changes the design system of the project.
- **Facts by tools.** `uxcheck.py design` checks the spec, the source tags, the states, and the failure paths. `uxcheck.py mockup` renders each mockup and finds tells: colors and sizes outside the tokens, more than one primary action, nested surfaces, unlabeled inputs, and low contrast. `uxcheck.py system` audits a design system.

## Test results

We tested the plugin in four iterations on a fictional field-service product. There were three fixtures: a strong design system, no design system, and a design system with 9 planted faults. Each variant ran 3 times for each case. Blind graders compared the results in pairs. The full results are in [`evals/ux-design/results-2026-10-24.md`](../../evals/ux-design/results-2026-10-24.md).

Version 0.1.0 on Sonnet, against Sonnet without the plugin (30 votes for each criterion):

| Criterion | Plugin | Without | Tie |
|-----------|--------|---------|-----|
| Design: thought through | 30 | 0 | 0 |
| Design: buildable | 30 | 0 | 0 |
| Design: grounded | 27 | 2 | 1 |
| Mockups: states | 30 | 0 | 0 |
| Mockups: specific to the product | 22 | 7 | 1 |
| Mockups: design system | 20 | 6 | 4 |
| Mockups: clarity | 20 | 10 | 0 |

- The same story gives the same main decisions in 89% to 100% of the run pairs (without the plugin: 75% to 90%).
- With a faulty design system, the plugin names 8 or 9 of the 9 faults and copies none into the mockups. Without the plugin, 2 to 6 faults reach the mockups.
- Without a design system, the plugin always makes a shared foundation (token and component files). Without the plugin, the styles stay inside each mockup.
- A run costs about 2.4 times a run without the plugin ($1.47 against $0.60 on Sonnet).

## Limits

- The agent cannot do user research. It writes the questions and the research backlog. Real users answer them.
- The mockup tool is HTML in this version. The skills for Claude Design, Figma, and OpenPencil come later.
- The tests used one fictional product and three stories. Results on other products can differ.

## Changelog

Refer to [CHANGELOG.md](CHANGELOG.md).
