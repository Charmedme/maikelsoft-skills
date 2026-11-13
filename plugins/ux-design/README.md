# ux-design

ux-design is a Claude Code plugin with a UX designer agent and its practice skills. The agent designs the user side of one story: the problem frame, the flow, the patterns, the screens, and the states. It gives each design decision a source, so the same story gives the same design again. Then it makes mockups from the design system of the project, and a clickable prototype for a test with users. After the build, it reviews the feature against the design with Playwright.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

Status: version 1.0.0. Two commands carry the work: `/ux-design` designs one item before the build, and `/ux-review` checks the built screens against the design. The agent has four modes: design, mockup, prototype, and review. The tool skills connect it to Claude Design, Figma, and OpenPencil. product-flow, dev-flow, and dev-team call the agent when you install this plugin. The plugin works alone.

Read the test results below before you use it on real work.

## Install

You need Claude Code, Python 3.9 or later, and Python Playwright for the mockup checks.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install ux-design@maikelsoft-skills
pip install playwright
```

After the install, run `/reload-plugins` in each open Claude Code session, or start a new session. Until then, the `ux-designer` agent type is not available. The agent file says how a general-purpose agent can follow it in that case.

## Quick start

1. Set up the design facts of the project one time. With the dev-setup plugin, run `/setup`. Without it, `/ux-design` asks the questions at its first run. The answers go in the `## Design` section of `.claude/rules/delivery.md`:

   ```markdown
   ## Design
   - Mockup tool: html
   - Design-system source: design-system/tokens.css, design-system/components.css, DESIGN.md
   - Brand files: DESIGN.md, PRODUCT.md
   - Languages: en
   - Run the app: npm run dev, http://localhost:5173
   ```

2. Design one item with UI work:

   ```
   /ux-design SHOP-12
   ```

   The command reads the ticket, makes the work folder `docs/work/SHOP-12/`, and starts the agent for the design and the mockups. It stops for your choice of a style direction and for your OK on the key screens. It offers a prototype for a test with users.
3. Read `ux-design.md` and the mockups in `mockups/`, and approve the design. After your approval of the text, one message goes to the product owner. It holds the state examples, the proposed decisions, and the opportunities.
4. After the build, check the built screens against the design:

   ```
   /ux-review SHOP-12
   ```

   The review needs the run command of the app and Python Playwright. It writes `ux-review.md` with a verdict.

Each command ends with the next step, for example `Next: /build SHOP-12`.

## Upgrade from 0.x

- The `## Design` section moves from `CLAUDE.md` to `.claude/rules/delivery.md`. The commands and the agent still read it in `CLAUDE.md` when `delivery.md` has no such section. The setup step offers to move it.
- `ux-setup` and `ux-practice` are out of the `/` menu. `/setup` or `/ux-design` starts the setup step, and `/ux-design` starts the method.

## Commands

Claude Code starts a skill by itself when your request matches its trigger. You can also start a skill with its command. Text after the command goes to the skill, for example `/ux-design:ux-review SHOP-12`. When no other plugin has a command with the same name, the short name also works: `/ux-design`, `/ux-review`.

| Command | What it does | Starts by itself when you ask for |
|---------|--------------|-----------------------------------|
| `/ux-design:ux-design` | Designs one item with UI work before the build: the design spec, the mockups of each state, and a prototype for a test with users. Sends the state examples to the product owner. | The UX design, a design spec, or the screens and states of a story before the build |
| `/ux-design:ux-review` | Reviews a built feature against its design in each state and at each size. | A built screen checked against its design, or a UX audit |
| `/ux-design:ux-patterns` | Selects an interaction pattern from the facts of a task. | A pattern for a list, a form, a flow, or a destructive action |
| `/ux-design:ux-states` | Lists the states of each screen and how the user recovers. | The states of a screen, or more than the happy path |
| `/ux-design:ui-craft` | The visual design method, with a rubric and a list of AI tells. | A screen judged on visual quality, or output that looks generic |
| `/ux-design:brand-rules` | Writes and checks `DESIGN.md`, `PRODUCT.md`, and DTCG design tokens. | Brand rules, a style guide, or design tokens |
| `/ux-design:web-accessibility` | WCAG 2.2 AA checks for a design or a built feature. | Accessibility, focus, contrast, or screen reader use |
| `/ux-design:ux-prototype` | Makes a clickable prototype with A/B variants and a test plan, for an existing design. | A flow tested with users, or variants compared |
| `/ux-design:figma` | Reads from and writes to Figma through the official MCP server. | A Figma link, or Figma as the design tool |
| `/ux-design:claude-design` | Works with Claude Design and Design System artifacts. | A Claude Design link, or Claude Design as the design tool |
| `/ux-design:openpencil` | Puts mockups into OpenPencil. | OpenPencil, or a project with `.op` files |

Two skills have no command. `ux-setup` is the setup step: `/setup` and the commands start it. `ux-practice` is the method of the agent.

The agents have no command. A skill starts them, except where the table says otherwise.

| Agent | What it does | Started by |
|-------|--------------|------------|
| `ux-design:ux-designer` | Designs one story, makes mockups and a prototype, and reviews the build. | `/ux-design`, `/ux-review`, and the build flows of dev-flow and dev-team. You can also ask for it by name, for example "Use the ux-designer agent in design mode for docs/work/SHOP-12" |

## What is in the plugin

| Part | Name | What it does |
|------|------|--------------|
| Agent | `ux-designer` | Design mode: frame, opportunities, flow, patterns, options, design spec, states, copy, accessibility, self-check. Mockup mode: one frame for each state, then a check and a critique. Prototype mode: a clickable prototype with variants and a test plan. Review mode: the built feature against the design. |
| Skill | `ux-design` | The command for the design of one item. It reads the ticket, makes the work folder, and starts the agent in each mode. It holds the stops for the human and sends one message to the product owner. |
| Skill | `ux-practice` | The method: four phases, the design spec with a source for each decision, the decision log, evidence tags, and opportunities. Also the modes light, full, and new-direction, and the `uxcheck.py` checker. |
| Skill | `ux-patterns` | Decision tables that select an interaction pattern from the facts of the task, each rule with an ID. Also the rules for icons and toggles, with their research and the web values. |
| Skill | `ui-craft` | The visual method in a fixed order, the option and mockup rubrics, and the list of AI tells. Also a foundation for a project without a design system, and an audit for an existing one. |
| Skill | `ux-states` | The state inventory: the core states, each error kind, and the business states. Each state says what the user sees, can do, and how the user recovers. |
| Skill | `ux-prototype` | A prototype in the shared `proto-shell/1` standard: the same header, toolbar, variants, states, sizes, and Test mode in each prototype. The question with the highest risk, and a test plan. |
| Skill | `ux-review` | The command after the build. A walk file makes each state of the built app. Screenshots at 3 widths, axe-core, the Tab walk, the tell checks, the heuristics, and findings with a severity and an owner. |
| Skill | `web-accessibility` | WCAG 2.2 AA: a checklist for the design and for the build, and the manual checks that no tool does. Also the status of the EAA and EN 301 549. |
| Skill | `ux-setup` | The setup step. Scans the project and writes the `## Design` section of `.claude/rules/delivery.md` after one round of questions. |
| Skill | `brand-rules` | `DESIGN.md`, `PRODUCT.md` with a Words table, and DTCG tokens with usage notes and contrast pairs. `brandcheck.py` checks and converts the tokens. |
| Skill | `claude-design` | Reads a Claude Design System, turns the checked HTML mockups into a Design canvas, and makes a Design System from DTCG tokens. |
| Skill | `figma` | Adds to the official Figma plugin: a call budget, a read order, and a map from the variables to the tokens. It writes only in a draft file. |
| Skill | `openpencil` | Imports the checked HTML mockups into OpenPencil, one frame for each state (`opimport.py`). Then it uses the `openpencil-design` skill. |

## How it works

- **No empty decisions.** Each screen has 10 decisions (purpose, main task, pattern, layout, hierarchy, components, density, states, copy, motion). Each decision names its source: the product, the design system, a pattern rule, or a decision in `docs/ux/decisions.md`. A decision without a source becomes a proposed decision for the product owner.
- **Opportunities stay visible.** The agent proposes 1 to 3 extras beyond the acceptance criteria, each from a fact of the product. The mockups show each one in its own `proposed-op-<n>` frame.
- **The design system wins, and gets checked.** Without a design system, the agent makes a minimal foundation in the work folder. With one, it audits it first. It reports each fault in `design-system-issues.md` and fixes the serious ones in a marked `ds-fixes.css`. It never changes the design system of the project.
- **Facts by tools.** `uxcheck.py design` checks the spec, the source tags, the states, and the failure paths. `uxcheck.py mockup` renders each mockup and finds tells: colors and sizes outside the tokens, more than one primary action, nested surfaces, unlabeled inputs, and low contrast. `uxcheck.py system` audits a design system. `uxcheck.py review` walks the built app from a walk file, and walks the focus with the Tab key. `brandcheck.py` checks the DTCG tokens and the banned words.
- **HTML first, then the tool.** The HTML mockups are the checked source. The tool skill then puts them in Claude Design, Figma, or OpenPencil, where the team looks and edits.

## When it helps

Use it when:

- A story changes screens, and a developer or an agent builds them. The design spec gives each screen 10 decisions with a source, so the build does not guess.
- The states matter: errors, empty lists, slow or failed saves, no permission. The plugin designs each state and makes a mockup frame for it.
- The project has no design system, or a design system with faults. The plugin makes a foundation, or it reports the faults and keeps them out of the mockups.
- The same kind of story comes back often. The same story gives the same main decisions in 89% to 100% of the run pairs.
- You use Sonnet. On Sonnet, the plugin wins each criterion against Sonnet without the plugin.
- A UI story goes to a build. The states of the design become examples, so the locked acceptance tests cover each state.
- The review must be a gate. Review mode gives a verdict, a severity for each finding, and a walk file that runs again after the fix.
- Keyboard and screen reader use matter. The Tab walk and the checklist found 18 of 18 planted faults that axe-core does not find. Sonnet without the plugin found 12 of 18.
- The project has no brand files, or its tokens are only CSS. `brand-rules` makes DTCG tokens with checks, and `ux-setup` records the tools.

Do not use it when:

- The story has no user interface, for example backend work or boilerplate.
- You only want one quick visual idea. On Opus, the mockups without the plugin have as much product detail and clarity, at about half the cost.
- Nobody reads the proposed decisions and the opportunities. They wait for the product owner, and without a reader they add cost and no value.
- You want a review that finds more UX defects than a strong model. In the tests, review mode found the same UX defects as the model without the plugin. For accessibility, see below.

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

Version 0.3.0 adds the tool skills, the brand files, accessibility, and the setup. The full results are in [`evals/ux-design/results-2026-10-25-b3.md`](../../evals/ux-design/results-2026-10-25-b3.md). All runs on Sonnet.

| Part | Result |
|------|--------|
| Accessibility | On an app with 6 faults that axe-core does not find, the review finds 18 of 18 (3 runs). Version 0.2.0 finds 14 of 18, Sonnet without the plugin 12 of 18. No more false findings on the clean app. |
| Brand files | Each token has a usage note and each text color has contrast pairs (20 to 22, against 0). `DESIGN.md` has all 8 sections. The banned words are a check, not prose. |
| Figma | With a View seat, the skill keeps to the budget (4 to 5 read calls) and says which frames it did not read. Without the skill: 7 to 9 calls, and no word about the budget. A fake Figma server was used. |
| Setup | 31 of 32 facts right, against 28 of 32. No invented stack or run command. |
| OpenPencil | With a real headless OpenPencil 0.8.4 server: 22 of 22 states as frames in each run. The frames differ from the mockups in about 5% of the pixels. Cost $0.32 to $0.60, against $1.01 to $1.43 without the import script. |
| Calls between the plugins | `idea-to-backlog` starts the agent in design, mockup, and prototype mode when the plugin is installed, and uses `product-prototype` when it is not. |

Version 0.2.0 adds the prototype, the review, and the handoff. The full results are in [`evals/ux-design/results-2026-10-25.md`](../../evals/ux-design/results-2026-10-25.md).

| Part | Result |
|------|--------|
| Prototype | Against Opus without the plugin, prototype mode wins the question (4-1), the states (6-0), test readiness (3-2), fidelity (4-2), and clarity (4-0). |
| Review | All variants find the same defects. Review mode adds a verdict, severities, 48 to 84 screenshots, and a walk file. On Sonnet it costs $0.70 to $0.90 for each run. |
| Handoff | The examples cover 100% of the design states, against 53% to 76% without the handoff rule. |

On Opus, version 0.1.0 wins each design criterion and the states against Opus without the plugin. It does not win the other mockup criteria: Opus without the plugin is strong on product detail. Opus with the plugin beats Sonnet with the plugin on 6 of 9 criteria, at about 2.2 times the cost. Thus the agent uses Opus by default. To use Sonnet, set `model: sonnet` in the agent file.

## Limits

- The agent cannot do user research. It writes the questions and the research backlog. Real users answer them.
- Claude Design needs a claude.ai login. In a `claude -p` run or in CI, the agent makes HTML mockups only.
- The Figma skill was tested with a fake Figma server, not with a real Figma file. The write tools of Figma are in beta.
- The tests used one fictional product and three stories. Results on other products can differ.
- Review mode does not find more defects than a strong model. Its value is the gate: a verdict, the evidence, and a walk file that runs again.
- Only the human runs a prototype test with users. The agent does not invent results.

## Changelog

Refer to [CHANGELOG.md](CHANGELOG.md).
