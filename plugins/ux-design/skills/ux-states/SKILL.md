---
name: ux-states
description: State inventory for each screen - default, empty, loading, partial, error, success, disabled, overflow, offline, and the business states (no permission, stale data, locked by another user, waiting on an external system, bulk result with failures, read only) - with what the user sees, can do, and how the user recovers. Use when a screen or flow needs its states designed or checked, or when a design shows only the happy path.
---

# UX states

Most UI defects live outside the happy path. A screen that has no design for "empty", "error", or "slow" gets one improvised in the build. This skill makes each state a design decision.

## Steps

1. **List the required states.** Each screen has the 9 core states. "Default" is the start of the main task, not a filled-in screen. Show a filled screen as the optional state "in-progress". Give each error kind that can occur its own state: `error-load`, `error-save`, `error-undo`, `error-external`, `conflict`, `session-expired`. One generic "error" row hides the other kinds. Add each business state that the story or the domain makes possible. Read `references/states.md` for the meaning and the questions of each state. Done when each screen has its list.
2. **Answer the 3 questions.** Answer for each state: what does the user see? What can the user do? How does the user recover or continue? Use the pattern rules of `ux-patterns` where they apply (for example `PAT-FB-02` for loading, `PAT-EMPTY-02` for no results). Done when each state has 3 answers, or `n/a: <reason>` when it cannot occur.
3. **Check the stress cases.** Read the stress list in `references/states.md`. Write the result of each case that applies in the "Overflow" or "Partial" row. Done when each stress case that applies has an answer.
4. **Check language readiness.** Each text fits when it is 35% longer. Dates, numbers, and money use the format of the locale. The code does not build sentences from parts. Done when each risk is in the states table or the copy table.

Write the result in the states table of `ux-design.md` (format in the `ux-practice` skill, `references/templates.md`).

## Example

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
| loading | Skeleton rows in the table, the filters stay usable (PAT-FB-02) | Change a filter | Not needed |
| error | "The work orders did not load. The server did not answer." above the table | Try again | "Try again" reloads the list. After 3 failures: the support contact. |
| locked | "Anna edits this order. You can read it." | Read, send a message | The lock ends after 15 minutes without an action. |

## Files

| File | Read when |
|------|-----------|
| `references/states.md` | Steps 1 and 3: each state, its questions, and the stress list. |
| `references/language.md` | Before you write text for the human or the user. |
