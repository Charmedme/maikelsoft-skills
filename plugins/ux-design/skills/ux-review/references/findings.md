# Findings

## Format of ux-review.md

```markdown
# UX review: <story>

Mode: full | light
Verdict: CHANGES_REQUIRED | DONE | DONE_WITH_CONCERNS

## Scope

| Screen | State | In scope because | Screenshots |
|--------|-------|------------------|-------------|

## First look

- <screen>/<state>: <what the user sees, and what is different from the design>

## Heuristics

| ID | Verdict | Reason |
|----|---------|--------|

## Walkthrough

| Step | Result | Reason |
|------|--------|--------|

## Findings

### F1: <short title>
- What: <what is wrong, for the user>
- Where: <screen/state, screenshot name, file:line, or selector>
- Evidence: <tool rule, screenshot, or the line of the design>
- Impact: <what the user cannot do, or does wrong>
- Heuristic: <H1 to H10, or WCAG criterion>
- Severity: P0 | P1 | P2 | P3
- Origin: introduced | regression | pre-existing
- Owner: dev | design | po
- Fix: <the change, in one line>

## Tool results

| Check | Result |
|-------|--------|
| States walked | <n> of <n> |
| Screenshots | <n> |
| axe-core violations | <n>, or "not run" |
| Tells | <n> |
```

## Severity

| Level | Meaning |
|-------|---------|
| P0 | The user cannot do the main task, loses data, or gets a wrong result without a sign. |
| P1 | The user fails a task of an acceptance criterion, or a WCAG A or AA criterion fails on the main task. |
| P2 | The user can continue with effort or a workaround. A difference from the design that the user notices. |
| P3 | A small difference that most users do not notice. |

## Origin

- **introduced**: the diff of this story makes the fault.
- **regression**: it worked before the diff, and the diff breaks it.
- **pre-existing**: it was already there. It goes to the human, and it does not block.

## Owner

- **dev**: the build does not match the design.
- **design**: the design is wrong or incomplete. The build follows it.
- **po**: a product decision is missing. The question goes to the product owner.

## Merge the tool results

- One finding for each cause. "Input without a label" on 14 rows is one finding.
- A fault at one width only says the width in Where.
- An axe-core result that the design already has as a decision (for example a color that the product owner approved) is still a finding. Owner: design.
