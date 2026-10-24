# Example mapping

Example mapping turns an item into rules, examples, and questions before the build. It is the "three amigos" step of BDD: the product owner (PO), a developer, and a tester look at one item together. You do the preparation as a business analyst (BA). The PO decides.

## Steps

1. **Rules.** For each acceptance criterion, list its rules: each limit, each state, each "only when", each error.
2. **Examples.** Give each rule one or more concrete examples with real values. Use Given/When/Then, or a table with one row for each example. For each limit, give an example on each side and one at the exact limit. Example: 1:59, 2:00, and 2:01 for a time limit of 2 minutes. For each state, give an example in the state and one out of it.
3. **Search the decisions first.** A question about a rule often has an answer already. Look in this order:
   1. The item, its comments, its parent, and its linked items.
   2. The backlog context file of product-flow (`backlog-<slug>.md`, section "Decisions"). Also other decision logs of the project, for example a table of `Q1`, `Q2`.
   3. The decision records (ADRs) and the project guide.

   When a decision answers the question, write the example with that answer, and cite the decision ID and its words. Example: "Q44: Bij een pauze stopt de lopende ronde."
4. **Questions.** Each rule that has no answer yet is a question for the PO. Give each one the options and your recommended answer. Read `po-questions.md` for the way to send them.
5. **Write it down.** Add the examples to the "Examples" section of the item file or the run file. Label each example with its criterion (AC1, AC2). List the answers that came from decisions, with their IDs. List the open questions.

## Rules

- An example adds real values, not a new rule. A new rule is a question.
- An answer that cites an existing decision counts as approved when the PO is the human in the session (solo work). A new decision always needs the PO.
- Do not change the item. A PO answer that changes an acceptance criterion goes back to the PO (`po-questions.md`).
