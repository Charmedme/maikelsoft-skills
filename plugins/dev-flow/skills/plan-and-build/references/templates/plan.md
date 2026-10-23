# Plan: <item key> <item title>

**Item:** <link to the item, or the path of the item file>

**Tier:** <small | medium | large> <type: bug | refactor | feature | chore>. <reason in one line>

**Step:** <research | questions | plan | build | review | done>

## Item

Acceptance criteria (verbatim from the item):

- AC1: <...>
- AC2: <...>

PO decisions that the work touches:

- <decision, with the link to the comment or the record. Or "None.">

## Research

<Small tier: the code that the change touches and the facts that you found, max 5 lines, each with its source. Medium tier: a link to [research.md](research.md).>

## Plan

<Small tier: max 5 lines: the change, the test that shows it, the checks. Medium tier: the ticket table below and a link to [decisions.md](decisions.md).>

| No | Ticket | AC | Blocked by | Status |
|----|--------|----|------------|--------|
| 01 | [<Title>](tickets/01-<slug>.md) | AC1 | None | draft |

Status: draft | todo | doing | blocked | done

## Waiting for the PO

<Each product question, the link of the comment, and the answer with its date when it comes. Or "None.">

## Gates

- Plan: open
- Done: open

Gate: open | waiting | approved | assumed. Small tier has the gate Done only. Large tier without dev-team also has the gate Design before the gate Plan.

Add the date and one line of reason when a gate changes.

## Build

- Branch: <feat/<slug>, or the name that the project guide gives>
- Type check: `<command>`
- One test file: `<command>`
- All tests: `<command>`
