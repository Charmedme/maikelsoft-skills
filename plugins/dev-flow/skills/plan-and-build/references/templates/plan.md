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

A link to [research.md](research.md).

## Plan

The ticket table and a link to [decisions.md](decisions.md). The acceptance tests are in `<path of the Specs folder>`.

| No | Ticket | AC | Blocked by | Status |
|----|--------|----|------------|--------|
| 01 | [<Title>](tickets/01-<slug>.md) | AC1 | None | draft |

Status: draft | todo | doing | blocked | done

## Waiting for the PO

<Each product question, the link of the comment, and the answer with its date when it comes. Or "None.">

## Gates

- Spec: open
- Done: open

Gate: open | waiting | approved | assumed. Large tier without dev-team also has the gate Design before the gate Spec.

Add the date and one line of reason when a gate changes.

## Build

- Branch: <feat/<slug>, or the name that the project guide gives>
- Type check: `<command>`
- One test file: `<command>`
- All tests: `<command>`
