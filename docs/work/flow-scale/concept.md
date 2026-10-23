# Concept: one product front end, and a process that scales to the change

Date: 2026-10-23. Scope: `dev-flow:plan-and-build`, `dev-team:deliver-story`, `product-flow:idea-to-backlog`.

## Findings (verified in the code of the skills)

1. **Double front end: correct.** `idea-to-backlog` and `plan-and-build` both research, grill, and prototype. Both grill the problem, the user, and the scope (`dev-flow/references/grill.md`, topics 1 and 2). Each writes its own decisions: the "Decisions" section of the backlog context file, and `docs/work/<slug>/decisions.md`. No rule links them, and no rule says which wins.
2. **dev-team is mostly clean.** `deliver-story` already starts from a story (`story-intake`, read only). Its decision record (`docs/dev-team/decisions.md`) holds technical decisions. Gap: the senior can still put product questions to the human in the session. Nothing sends them to the PO.
3. **Four gates for every change: correct.** `size.md` says: "All phases and all gates stay. Only the depth changes." A one-line bug gets research.md, a grill, a spec, a prototype, and G1 to G4.
4. **Our own data agrees with the October study.** dev-team light mode on the small story: $4.14 median, 9/9 checks. The baseline without the team: $0.40, same pass result. That is about 10 times the cost for no gain on a small change. Jain measured about 10.5 times the tokens and about 7 times the cost, 10 of 10 in both conditions.

## Gap in the proposed fix

When `dev-flow` starts from the approved item, it does the same job as `dev-team:deliver-story`: two dev back ends from one item. The fix removes the double front end and makes a double back end. Proposal: one dev entry that reads the item, sets the tier, and runs one session (small) or the team (large).

## Proposal A: one source of product decisions

- The approved tracker item is the only source of product decisions: the description, the acceptance criteria, and the comments of the PO. The backlog context file of `idea-to-backlog` stays the PO's work file.
- The dev side starts from the item with `story-intake`. No idea mode: an idea without an item goes to `product-flow:idea-to-backlog` first. That skill can write the items to files when there is no tracker, so solo work still has a path.
- The dev side does technical research and asks technical questions only. Rule: a question about the user, the value, the scope, or the visible behaviour is a product question.
- A product question goes back to the PO as a comment on the item. The run stops with status `WAITING_FOR_PO` for that part, and continues with the parts that do not depend on it.
- The dev decision record holds technical decisions only. Each row names the item. When a technical choice changes visible behaviour, it is a product question, not a dev decision.
- Prototypes: a UI or flow prototype is a product question (`product-prototype`). The dev side makes a technical spike only (does this library, query, or API work).

## Proposal B: tiers by size and type

The agent sets the tier from facts in the code and the item, and tells the human in one line. The human can change it. The agent moves a tier up when the facts show more work. It moves a tier down only with the human's word.

| Tier | Signal | Phases | Gates |
|------|--------|--------|-------|
| Patch | One file, a clear cause, no contract or data change. A typo, a config value, a bug with a repro | Read the code, fix test-first, run the checks | Done (the diff and the checks) |
| Small | One behaviour, up to about 3 files, one layer | Short research in the report, a plan of up to 5 lines, build, checks | Done |
| Medium | One feature over more than one layer | research.md, technical questions, plan and tickets, build, review | Plan (tickets), Done |
| Large | More than one feature, or more than 10 tickets | As medium, plus a spike for each technical risk. Runs with the team (`dev-team`) | Design, Plan, Done |

Risk raises the tier to Medium or higher, whatever the size. The risks: a schema or data migration, authentication or authorization, a public API contract, and a new package. Also money, the deletion of data, or another step that is hard to undo.

The type changes the phases too:

| Type | Change to the phases |
|------|----------------------|
| Bug | Start with a failing test that shows the bug. No spec. |
| Refactor | No new behaviour, so no product question. The existing tests must stay green before and after. |
| Feature | As the table. |
| Chore (package update, config) | Patch or Small. The checks are the gate. |

The final checks (build, tests, the stack rules) run in each tier. They are the one gate that the October study keeps for a strong model.

## Test plan

Eval set of dev-team (stories get-order small, cancel-order medium, ship-history large, order-mail trap) plus one new bug case. 3 runs for each case, current flow against the tiered flow, Opus. Pass: the checks and probes stay equal, and the cost of the small and patch cases goes down by half or more. The trap case must still stop for the human.

## Decisions of the human (2026-10-23)

| No | Decision |
|----|----------|
| D1 | One dev entry with two paths. `dev-flow:plan-and-build` reads the item and sets the tier. Patch, small, and medium run in one session. Large runs with `dev-team:deliver-story`. |
| D2 | An idea without an item goes to `product-flow:idea-to-backlog` first. `plan-and-build` has no idea mode. |
| D3 | A product question goes to the PO as a comment on the item. The agent drafts it, the human approves the exact text, and the agent posts it. |
| D4 | The tiers and gates as proposed. The agent sets the tier, and the human can change it. |
