# Refine pass

The refine pass makes a draft item ready to build. You act as a business analyst (BA). The product owner (PO) decides. Each item command ends with this pass. `/refine` runs it for an item that is still a draft.

## Ready

An item is ready when each of these is true:

1. No question is open.
2. `specsync.py check` shows 0 errors on the Behaviour spec (`--kind bug` for a bug).
3. `backlogcheck.py` shows 0 errors on the item. This check applies only when the skill has `backlogcheck.py` (product-flow).
4. The PO approved the exact text.

Then the status is `spec:ready`. Else it is `spec:draft`. Only this pass sets `spec:ready`. When a person set `spec:ready` by hand, check the item again. When a check fails, set it back to `spec:draft` and say why.

## Steps

1. **Read.** Read the item, its comments, its parent, and its linked items. Read the backlog folder of the item (`docs/backlog/<slug>/intent.md`) when it exists, and the decisions of the project. Done when you know the criteria, the spec, and the decisions that apply.
2. **Review.** Start a new sub-agent as the reviewer (`review-rules.md`). Give it the item, `behaviour-spec.md`, and the findings of earlier rounds. When sub-agents are not available, do the review yourself as a separate step, and say so. The reviewer checks:
   - INVEST: independent, negotiable, valuable, estimable, small, testable.
   - Each criterion has a rule, and each rule names its criterion.
   - Each limit has a scenario below, at, and above it.
   - Each word that a reader can read in two ways. Example: "30 days" can be calendar days or working days, and can start at the order or at the delivery.
   - Each value that the item does not give. Example: "a fee" without the amount.

   Done when each finding has a level (`review-rules.md`).
3. **Search the decisions.** For each finding, look for an answer in the item and its parent. Also look in the backlog folder and the decision records of the project. An answer that a decision gives goes into the spec, with the decision ID in the report. Done when each finding has an answer or is a question.
4. **Ask the PO.** Read `po-questions.md`. Put all open questions in one message. Give each question the options and your recommended answer. Do not answer a product question yourself: a word with two meanings, a value, a scope, or a rule. Done when the PO answered.
5. **Change the item.** Put each answer into the criteria and the spec. Run `specsync.py check`, and `backlogcheck.py` when the skill has it. Done when each check shows 0 errors.
6. **Approve.** Show the changed item, or the changes when the item is long. Ask: "Is this item ready to build?" After step 5, ask it in a message of its own. The questions of step 4 go in an earlier message. An answer to them ("ok", "take your recommended answers") is not the approval. Done when the PO says yes to this question. Then set `spec:ready`.

At most 3 rounds of steps 2 to 5 (`cycle.md`). At the cap, stop and give the options of `cycle.md`.

## No human to ask

Do steps 1 to 3. Write the questions with your recommended answers in the report. Keep the status `spec:draft`. Never set `spec:ready` without the approval of a human.
