# Templates

`uxcheck.py` reads these formats. Keep the headings and the table columns exactly.

## Contents

- ux-design.md
- Sections for full and new-direction mode
- Decision log
- Research backlog

## ux-design.md

````markdown
# UX design: <story title>

Mode: light | full | new-direction
Surface: operate | read | persuade

## Sources

| Source | Found | Used for |
|--------|-------|----------|
| DESIGN.md | yes | tokens, components |

## Frame

| Question | Answer | Evidence |
|----------|--------|----------|
| Problem | <the problem in the words of the user> | checked: story |
| Users | <who, described by behavior, not by age or job title only> | judgment: ... |
| Value | <what changes for the user when this works> | needs-users |
| Success | <how the user and the team see success> | checked: AC-1 |
| Constraints | <tech, law, time, existing screens> | checked: PRODUCT.md |

## Opportunities

| ID | Opportunity | From | Decision |
|----|-------------|------|----------|
| OP-1 | <what would help beyond the ACs> | product: <fact> | UXD-008 (proposed) |

## Flow

1. <Step>. Screen: `<screen-id>`. AC: AC-1.
2. ...

Decision points: <where the flow branches, and on which fact>.

### Failure paths

| Step | Failure | State |
|------|---------|-------|
| 3 | The assignment fails for some work orders | bulk-result |
| 3 | The undo fails | error-undo |
| 1 | The list does not load | error-load |

## Style direction

| Direction | Color | Type | Shape | Moodboard |
|-----------|-------|------|-------|-----------|
| Neon night | black stage, acid green and pink accents | condensed sans, large numbers | sharp cards, thin outlines | mockups/direction-1.html |
| Paper table | warm off-white, ink blue | serif titles, plain sans body | round cards with a paper shadow | mockups/direction-2.html |
| Arcade | deep blue, yellow, red | pixel display font for titles | chunky tiles, thick borders | mockups/direction-3.html |

Chosen: Paper table (owner, 2026-10-27)

## Options

| Option | Structure | Task fit | Clarity | Consistency | Risk | Cost | Total |
|--------|-----------|----------|---------|-------------|------|------|-------|
| A | <layout, order, main action> | 3 | 2 | 3 | 2 | 3 | 13 |
| B | ... | | | | | | |

Selected: A. Reason: <one or two sentences>.

In new-direction mode, the Options table has a Difference column, and the Total follows the new-direction variant of the `ui-craft` option rubric:

| Option | Structure | Task fit | Clarity | Consistency | Risk | Cost | Difference | Total |
|--------|-----------|----------|---------|-------------|------|------|------------|-------|
| A | <layout, order, main action> | 3 | 2 | 2 | 2 | 1 | 3 | 14.5 |

## Screen: <screen-id>

| Decision | Value | Source |
|----------|-------|--------|
| purpose | <one sentence> | product: AC-1 |
| main-task | <one task> | product: story |
| pattern | <pattern name> | pattern: PAT-LIST-01 |
| layout | <regions in order> | system: page layout "list" |
| hierarchy | 1: <...> 2: <...> 3: <...> | pattern: PAT-ACT-01 |
| components | <component names> | system: DataTable, Button |
| density | compact | decision: UXD-002 |
| states | default, empty, loading, error, ... | product: AC-3 |
| copy | <main labels> | product: domain terms |
| motion | none | system: motion tokens |

### States: <screen-id>

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
| default | ... | ... | n/a |
| empty | ... | ... | ... |
| loading | ... | ... | ... |
| error | ... | ... | ... |
| partial | n/a: <reason> | | |

### Copy: <screen-id>

| Element | Text |
|---------|------|
| Page title | ... |

### Accessibility: <screen-id>

- Keyboard order: ...
- Focus after an action: ...
- Labels and announcements: ...

## Element inventory

| Element | Home | Component | Screens |
|---------|------|-----------|---------|
| music toggle | top-bar | IconToggle | lobby, round, score |
| leave game | top-bar | IconButton with label | lobby, round |
| players | panel | PlayerList | lobby, round, score |

## Non-text contrast

| Part | Color | Next to | Ratio | Needs |
|------|-------|---------|-------|-------|
| input border | #8a7fb0 | #1a0b3b | 4.9 | 3:1 |
| toggle on-state fill | #f5c542 | #1a0b3b | 11.2 | 3:1 |
| raised surface (decorative card) | #2e1a63 | #1a0b3b | 1.4 | none |

## Decisions

| ID | Decision | Status | Reason |
|----|----------|--------|--------|
| UXD-007 | <new decision> | proposed | <reason> |

## Open questions

- [NEEDS EVIDENCE] <question>, method: <interview, test, data>.
````

Rules for `uxcheck.py`:

- Each `## Screen: <id>` has all 10 decisions, each with a value and a source tag (`product:`, `system:`, `pattern:`, `decision:`).
- A `pattern:` tag names a rule ID that exists in `ux-patterns`. A `decision:` tag names a row in `docs/ux/decisions.md` or in the Decisions section.
- Each screen has a states table with the required states (see `ux-states`). A state that cannot occur gets `n/a: <reason>`.
- The `Mode:` line has one of `light`, `full`, or `new-direction` (`DS-MODE`).
- Full and new-direction mode have an Options table with 2 or 3 options and a "Selected:" line.
- New-direction mode has a Style direction section with 3 or 4 rows and a `Chosen: <name> (owner, <date>)` line. Its Options table has a Difference column with at least one 3 (`DS-DIRECTION`).
- Full and new-direction mode have an Element inventory. It has one row for each element that shows on more than one screen. Home is one region name, the same name as the `data-region` value in the mockups. An element with two homes is an error (`DS-INVENTORY`).
- Full and new-direction mode have a Non-text contrast table. Needs is `3:1` for a control edge, a state indicator, or an icon that the user needs. Needs is `none` for a decorative surface. A row with Needs `3:1` and a ratio below 3 is an error (`DS-NONTEXT`).
- Each failure path names a state that exists in a states table.

## Sections for full and new-direction mode

**Style direction** (new-direction mode only). Each direction is one moodboard: one key screen in HTML, default state only, with the colors, the type, and the shapes of the direction. A moodboard shows the look, not the structure. The directions differ from each other and from the existing look in color, type, and shape.

Show the moodboards to the owner and stop with `NEEDS_HUMAN`. Write the `Chosen:` line only after the owner chose.

**Element inventory.** List each element that shows on more than one screen. Examples: the settings toggles, the leave action, the invite action, the player list, the help. Give each element one home region and one component. The mockups use the same names: `data-element="music toggle"` inside an ancestor with `data-region="top-bar"`.

A control that shows in more than one frame has the same `id` or `data-element` in each frame. The checker compares a control across frames only by these values.

A user learns the place of an element one time. When the element moves, the user must look for it on each screen.

**Non-text contrast.** List each part that WCAG 1.4.11 covers:

- the edge of an input, a checkbox, a switch, a segmented control, or an icon-only control
- the state indicator of a toggle
- an icon without a text label
- a raised surface that holds an interactive tile (a ratio below 1.5:1 against the background makes the tile hard to see)

Give the ratio against the color next to it. A control with a visible text label does not need an edge. Use `uxcheck.py system` for the ratios of the tokens.

## Decision log

`docs/ux/decisions.md`:

```markdown
# UX decisions

| ID | Decision | Status | Date | Story | Replaces |
|----|----------|--------|------|-------|----------|
| UXD-001 | Lists of work items use a data table with sortable columns. | accepted | 2026-10-23 | STORY-12 | |
```

Status: `proposed` (the agent writes it), `accepted` (the human approves it), `replaced`.

## Research backlog

`docs/ux/research-backlog.md`:

```markdown
# Research backlog

| Rank | Assumption | Risk if wrong | Method | Stories |
|------|------------|---------------|--------|---------|
| 1 | Planners check stock before they assign a job. | Wrong main task on the job screen | 5 interviews | STORY-12 |
```

Rank by risk: the assumption that costs the most when it is wrong comes first.
