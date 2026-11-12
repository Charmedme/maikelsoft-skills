# Example mapping

Example mapping turns an item into rules, examples, and questions before the build. It is the "three amigos" step of BDD: the product owner (PO), a developer, and a tester look at one item together. You do the preparation as a business analyst (BA). The PO decides.

The result is the file `examples.md`. Write it before any test or code. The gate Spec shows it first.

## Steps

1. **Rules.** For each acceptance criterion, list its rules: each limit, each state, each "only when", each error. Give each rule an ID (`AC1 R1`). Mark a rule with a limit (a number, a time, a count, a date) with `(limit: <value>)`.
2. **Examples.** Give each rule one or more concrete examples with real values. Give each rule with a limit three examples: `below`, `at`, and `above` the limit. Example: 29, 30, and 31 days for a return period of 30 days.
3. **Words with more than one meaning.** Read each criterion again, word by word. List each word that a reader can read in two ways, with the two readings. Examples: "binnen 5 dagen" can be 5 calendar days or 5 working days. "De lijst" can be all items or the first page.
4. **Search the decisions first.** A question often has an answer already. Look in this order:
   1. The item, its comments, its parent, and its linked items.
   2. The backlog context file of product-flow (`backlog-<slug>.md`, section "Decisions"). Also other decision logs of the project, for example a table of `Q1`, `Q2`.
   3. The decision records (ADRs) and the project guide.

   When a decision answers a question, use that answer in the example, and cite the decision ID and its words.
5. **UI states.** When the item has an approved UX design (`ux-design.md`), add one example for each state of each screen in scope. Take the Given, When, and Then from the states table, and the text from the copy table. The source is `ux: <screen>/<state>`. Then the acceptance tests cover each state. A state that the design marks `n/a` gets no row.
6. **Questions.** Each rule, limit, or word that has no answer yet is a question for the PO. Give each one the options and your recommended answer. Read `po-questions.md` for the way to send them.
7. **Check.** Run `examplecheck.py examples.md` and fix each error. When `ux-design.md` is in the same folder, the script also checks each `ux:` source against its states tables.

## The file

```markdown
# Examples: <item key> <item title>

## Rules

- AC1 R1: <rule>
- AC1 R2 (limit: 30 days): <rule>

## Examples

| Rule | Case | Given | When | Then | Source |
|------|------|-------|------|------|--------|
| AC1 R1 | normal | <state> | <action> | <result> | item |
| AC1 R2 | below | <state> | <day 29> | <result> | item |
| AC1 R2 | at | <state> | <day 30> | <result> | Q12 |
| AC1 R2 | above | <state> | <day 31> | <result> | item |
| AC2 R1 | error-save | <the save fails> | <action> | <the text of the state> | ux: list/error-save |

## Words with more than one meaning

- "<word>" (AC1): <reading 1> or <reading 2>. Answer: <the answer and its source, or "open: P1">

## Answers from decisions

- Q44: "<the words of the decision>" (rule AC5 R1)

## Open questions

- P1 (AC1 R2): <question>. Recommended: <answer>, because <reason>.
```

Write "None." under a section without content. The Source column is `item`, a decision ID, `PO` with the date of the answer, `ux: <screen>/<state>`, or `open: P<n>`.

## Rules

- An example adds real values, not a new rule. A new rule is a question.
- An example with the source `open: P<n>` is not ready. The build of that rule waits for the answer.
- In solo work, the PO is the human in the session. Then an answer that cites an existing decision counts as approved. A new decision always needs the PO.
- A word with more than one meaning in a criterion is a product question. Do not answer it as a technical choice or an assumption. This is also true when an answer sheet lets you take your own answer for technical choices. Only the item, a decision that answers that word, or the PO can answer it. A word of a technical contract is not a criterion: mark it `(contract)`.
- Do not change the item. A PO answer that changes an acceptance criterion goes back to the PO (`po-questions.md`).
