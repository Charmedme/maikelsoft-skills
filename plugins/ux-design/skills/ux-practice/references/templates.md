# Templates

`uxcheck.py` reads these formats. Keep the headings and the table columns exactly.

## Contents

- ux-design.md
- Decision log
- Research backlog

## ux-design.md

````markdown
# UX design: <story title>

Mode: full | light
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

## Options

| Option | Structure | Task fit | Clarity | Consistency | Risk | Cost | Total |
|--------|-----------|----------|---------|-------------|------|------|-------|
| A | <layout, order, main action> | 3 | 2 | 3 | 2 | 3 | 13 |
| B | ... | | | | | | |

Selected: A. Reason: <one or two sentences>.

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
- Full mode has an Options table with 2 or 3 options and a "Selected:" line.
- Each failure path names a state that exists in a states table.

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
