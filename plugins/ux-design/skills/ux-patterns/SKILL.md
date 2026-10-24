---
name: ux-patterns
description: Decision tables that select an interaction pattern from the facts of a task - table or cards, page or modal or side panel, inline edit or form, pagination, filters, bulk and destructive actions, feedback for slow work, empty states, and input controls. Use when a screen, a list, a form, or a flow needs a pattern, or when a design choice must be repeatable and has to name its rule.
---

# UX patterns

A pattern choice follows from the facts of the task, not from taste. With the same facts, this skill gives the same pattern each time, with a rule ID that the design spec can name.

## Steps

1. **Collect the task facts.** For each screen, write these facts. Take them from the story, `PRODUCT.md`, and the existing screens. Done when each fact has a value or `unknown`.

   | Fact | Values |
   |------|--------|
   | items | how many items the user sees: 1, up to 20, 20 to 100, more than 100 |
   | job | scan (find one), compare (on attributes), act (change items), read (understand one item) |
   | frequency | daily, weekly, rare |
   | expertise | expert (trained, daily user), occasional |
   | context | must the user see the page behind? yes, no |
   | risk | reversible, irreversible, high impact (money, many items, other people) |
   | wait | the time that the system needs: under 1 s, 1 to 10 s, 10 to 30 s, longer, or an external party |
   | device | desktop first, mobile first, both |

2. **Check the decision log.** Read `docs/ux/decisions.md`. A logged decision for the same situation wins. Done when you know which decisions apply.
3. **Select the patterns.** Read `references/patterns.md`. For each need of the screen, go to its table and take the first rule whose condition matches the facts. Done when each need has one rule ID.
4. **Handle no match.** When no rule matches, or a fact is `unknown` and it changes the result, do not guess. Write a `proposed` decision with your recommendation and the reason, and list the fact as an open question. Done when each need has a rule ID or a proposed decision.
5. **Write the result.** In the design spec, put the pattern name in the value and `pattern: <rule ID>` in the source. Done when each pattern row names its rule.

## Example

Facts: 140 work orders, job compare and act, daily, expert, desktop first.
Result: data table (`PAT-LIST-01`), numbered pages (`PAT-PAGE-02`), filter panel with chips (`PAT-FILTER-02`). Also: bulk actions with a count (`PAT-ACT-02`), detail in a side panel (`PAT-OVL-02`).

## Files

| File | Read when |
|------|-----------|
| `references/patterns.md` | Step 3: the decision tables with the rule IDs. |
| `references/language.md` | Before you write text for the human. |
