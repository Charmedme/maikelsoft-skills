# Phase 2: Grill

Interview the human until you both have the same understanding of the work. Each decision opens the decisions that depend on it. This is the design tree.

## Rounds

The **frontier** is each open decision that does not depend on another open decision. Ask the full frontier in one round. Then wait for the answers.

Format of a round:

```
**Q1. <Short title>.** <The question. Give the options when there are options.>
Recommended: <your answer, and the reason in one sentence>

**Q2. <Short title>.** <...>
Recommended: <...>
```

- Number the questions across all rounds (Q1, Q2, ... Q12), so the human can answer "Q7: yes".
- One decision in each question.
- A question that depends on another question in the same round goes to the next round.
- When a question needs a fact, find the fact first (phase 1 rules). Ask the other questions while a sub-agent looks for it.
- When the human says "ok" or "agree", the recommended answer is the decision.

After each round, add the decisions to `decisions.md` (use `templates/decisions.md`). Write who decided: the human, or "recommended, accepted". Then make the next frontier.

## Topics to cover

Ask only the topics that the research did not settle:

- The problem and the user. Who has the problem, and how do they see that the work solves it?
- The scope. What is in, and what is out.
- The behaviour. The normal case, the edge cases, the errors. For each error, ask what the user sees and what the program returns (message, exit code, status).
- The data. What the work stores, changes, or deletes.
- The test seams. Where the tests touch the code. Prefer seams that exist. Use the highest seam that works. Fewer seams are better.
- The risks. What is hard to undo.

## A question that the human cannot answer

When the human says that another person knows the answer, read `questionnaire.md`. Mark the decision as "waiting" in `decisions.md`. From then on, do not ask the human about that subject. Put each question about it in the questionnaire. Continue with the questions that do not depend on it.

## Gate G1

When the frontier is empty, give a summary of the decisions in at most 10 lines. Ask: "Is this the shared understanding?" Only the human can approve the gate. A decision that is "waiting" blocks the gate, unless the human tells you to use an assumption.
