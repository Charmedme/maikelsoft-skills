# Plan (medium and large tier)

Turn the item and the technical decisions into tickets. The item is the spec: do not write a separate spec, and do not copy the acceptance criteria into a second document. Link to the item.

```
<work folder>/
  plan.md                 the item, the tier, the tickets, the gates, the questions for the PO
  research.md
  decisions.md            technical decisions only
  tickets/NN-<slug>.md    one file for each ticket
  spike/                  only when a spike was needed
```

## Technical questions

Ask the technical questions that the research did not settle, in rounds. One decision in each question, with your recommended answer and the reason in one sentence. Number the questions across all rounds (Q1, Q2, ...), so that the human can answer "Q3: yes". A question that depends on another question goes to the next round. Write each answer in `decisions.md` (`templates/decisions.md`), with the item key and who decided.

Topics: the modules and the interfaces, the data and its migration, the test seams, and the risks that are hard to undo. Prefer test seams that exist, and the highest seam that works. A product question goes to the PO (`po-questions.md`), not into this list.

## Spike

Make a spike only for a technical risk: "Does this library, query, API, or data model work as we need?" A spike is throwaway code in `<work folder>/spike/` that answers that one question. Run it, and write the answer and the reason in `decisions.md`. A question about what the user sees is not a spike: it is a product question.

## The tickets

Use `templates/ticket.md`. Number the tickets from `01`, with the blockers first.

- **Vertical slices.** Each ticket makes one narrow path work through all layers (data, logic, interface, tests).
- **One context window.** A ticket is small enough for one agent to do from the start to the end.
- **Prepare first.** When a change to the existing code makes the work easier, put that change in the first ticket.
- **Blocked by.** Add a blocker only when the ticket really needs it.
- **Wide changes.** A rename or type change over the full codebase takes three steps. Add the new form next to the old form. Move the callers in batches. Remove the old form.
- **Acceptance criteria.** Each ticket names the AC numbers of the item that it covers. Each AC of the item is in one ticket or more. A ticket can add technical criteria, for example "the migration keeps existing rows valid".

Set the status of each ticket to `draft`. After the gate Plan, set it to `todo`.

## Check

Run `workcheck.py` on the work folder and `doccheck.py` on each document. Fix each error.
