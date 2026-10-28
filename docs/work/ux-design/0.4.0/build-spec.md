# Build spec: ux-design 0.4.0

This spec is the contract between the checker build and the text build. The decisions are in `brief.md` (approved at G1). The icon research is in `plugins/ux-design/skills/ux-patterns/references/icons.md`. The field evidence is in `evals/ux-design/fixtures/field-2026-10-27/`.

## Contract: names and IDs

### Design file (`ux-design.md`, template in `ux-practice/references/templates.md`)

- Mode line: `Mode: light | full | new-direction`. The mode `new-direction` is full mode plus the style direction step. Use it when the request asks for something new ("new approach", "totally different", "redesign", "new look"), or when no design system exists.
- New section `## Style direction` (new-direction mode only). A table of 3 or 4 directions (name, color, type, shape, one-screen moodboard file) and a line `Chosen: <name> (owner, <date>)`.
- The Options table gets a column `Difference` in new-direction mode. At least one option has Difference 3.
- New section `## Element inventory`. Columns: `Element | Home | Component | Screens`. One row for each element that shows on more than one screen. Home is one region name.
- New section `## Non-text contrast`. Columns: `Part | Color | Next to | Ratio | Needs`. Needs is `3:1` for a control edge, a state indicator, or a meaningful icon, and `none` for a decorative surface.

### Mockup markup

- A shared element has `data-element="<inventory name>"`.
- A region has `data-region="<region name>"` on an ancestor (for example `top-bar`, `bottom-bar`, `panel`, `main`).

### Checker IDs (`ux-practice/scripts/uxcheck.py`)

| ID | Mode | Level | Test |
|----|------|-------|------|
| SYS-NONTEXT | system | error | A border, outline, or focus-ring token against the background token it sits on, below 3:1. |
| SYS-SURFACE | system | warning | A surface or raised token against the background token, below 1.5:1. |
| DS-MODE | design | error | No valid `Mode:` line. |
| DS-DIRECTION | design | error | Mode new-direction, and a part is missing. The parts: a Style direction section with 3 or 4 rows, a `Chosen:` line, and a Difference column with one 3 or more. |
| DS-INVENTORY | design | error | Full or new-direction mode without an Element inventory, or an element with more than one home. |
| DS-NONTEXT | design | error | Full or new-direction mode without a Non-text contrast table, or a row with Needs 3:1 and a ratio below 3. |
| MK-WIDTH | mockup | error | Horizontal overflow of a frame at 390, 820, or 1440 px. Screenshots at each width. |
| MK-NONTEXT | mockup | error | Rendered: the edge or state indicator of a control is below 3:1 against the adjacent color. Controls: an input, a checkbox, a switch, a segmented control, or an icon-only control. WCAG 1.4.11: a control with a visible text label does not need an edge. |
| MK-SURFACE | mockup | warning | Rendered: an interactive tile, chip, or row whose fill and border are both below 1.5:1 against the adjacent color. |
| MK-REGION | mockup | error | One `data-element` value in a different `data-region` on 2 screens. |
| MK-REGION-MISSING | mockup | warning | A `data-element` without a `data-region` ancestor. |
| TELL-08 | mockup | error | Existing rule. Extend: an emoji or an icon-font ligature does not count as a name. |
| TELL-21 | mockup | error | An icon-only control whose toggle name changes between frames of one screen (`aria-pressed` differs and the name differs), or a name that matches `/^(un|turn (on|off)|enable|disable)/i`. |
| TELL-22 | mockup | error | A control whose icon changes between frames of one screen, with no `aria-pressed`, `aria-checked`, or `role=switch`. |
| TELL-23 | mockup | error | A toggle (`aria-pressed`) whose two states have the same icon markup and differ only in color. |
| TELL-24 | mockup | error | An icon-only control whose icon color is below 3:1 against its background. |
| TELL-25 | mockup | error | An icon-only control that matches the primary selector. |
| TELL-26 | mockup | error | An element with `role=status`, `role=alert`, or a status class, with an icon and no visible text. |
| TELL-27 | mockup | warning | An icon-only control whose only name is `title`. |
| TELL-28 | mockup | warning | A control below 24 x 24 CSS px without the spacing exception of WCAG 2.5.8. |
| TELL-29 | mockup | warning | Icon-only items in `nav`, `role=navigation`, `role=tablist`, or `role=menubar`, or an icon-only menu button. |
| TELL-30 | mockup | warning | One icon (same path data, font class and ligature, or `data-icon`) with 2 or more different names across all frames. |

TELL-21 to TELL-30 also get a row in `ui-craft/references/tells.md` (tool: uxcheck).

### Pattern IDs (`ux-patterns/references/patterns.md`, new section "Icons and toggles")

Universal rules only (decision Q14). About 10 rows: PAT-ICON-01 to PAT-ICON-0n and PAT-TOGGLE-01, PAT-TOGGLE-02. Take them from the icon research. Leave out the PAT-GAME rules. Move the web values (24 CSS px, `aria-pressed` markup, the `title` tooltip) to `ux-patterns/references/platform-web.md`. The full research becomes `ux-patterns/references/icons.md` (loaded only when a design has icons).

## Who builds what

- Checker build: `uxcheck.py`, its unit tests in `tests/test_ux_040.py`, and the new rows in `tells.md`.
- Text build: each other file.
