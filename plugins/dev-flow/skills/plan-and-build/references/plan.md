# Phase 3: Plan

Turn the decisions into separate documents. Do not ask new questions here. If a decision is missing, go back to phase 2 for that decision only.

Each item is a separate file:

```
<work folder>/
  plan.md                 the goal, the order, the status, the gates
  research.md
  decisions.md
  specs/<name>.md         one or more
  tickets/NN-<slug>.md    one file for each ticket
  questionnaires/<slug>.md
  prototype/
```

## The plan

Use `templates/plan.md`. The plan is the index of the run. It has the goal in one or two sentences, a link to each spec, the ticket table, and the gates. Do not copy text from a spec or a ticket into the plan.

## The specs

Use `templates/spec.md`. Write one spec for each feature (see `size.md`). A spec says what the work does and why, from the view of the user. It has the decisions of phase 2, the test seams, and what is out of scope.

- Do not put file paths or code in a spec. They change too fast.
- Exception: a type, a state machine, or a schema from the prototype that gives a decision more exactly than text. Keep only the part with the decision, and say that it comes from the prototype.

## The tickets

Use `templates/ticket.md`. Number the tickets from `01`, with the blockers first.

- **Vertical slices.** Each ticket makes one narrow path work through all layers (data, logic, interface, tests). A person can show or check a finished ticket alone.
- **One context window.** A ticket is small enough for one agent to do from the start to the end.
- **Prepare first.** When a change to the existing code makes the work easier, put that change in the first ticket.
- **Blocked by.** Each ticket lists the tickets that it needs before it can start. Add a blocker only when the ticket really needs it.
- **Wide changes.** A rename or type change that touches the full codebase cannot be a vertical slice. Do it in three steps. First, add the new form next to the old form. Then move the callers in batches, one ticket for each batch. Last, remove the old form.
- **Acceptance criteria.** Each criterion is a behaviour that a test or a person can check.

Set the status of each ticket to `draft`. Phase 5 changes it to `todo` after gate G3.

## Check

Run `workcheck.py` on the work folder and `doccheck.py` on each document. Fix each error.
