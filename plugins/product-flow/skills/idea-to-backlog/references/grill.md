# Phase 2: Grill

## Contents

- Rounds
- Topics
- A question for a third person
- Gate G1

## Rounds

Interview the human until you both understand the work in the same way. Each decision opens the decisions that depend on it.

- Ask all questions that do not depend on another open question in one round. Then wait.
- Number the questions across all rounds (Q1, Q2, ...), so the human can answer "Q4: yes".
- One decision in each question. Give the options when there are options.
- Give a recommended answer and the reason in one sentence: "Recommended: <answer>, because <reason>."
- "ok" or "agree" means the recommended answer.
- After each round, add the decisions to the "Decisions" section.

Format:

```
**Q1. <Short title>.** <The question.>
Recommended: <answer>, because <reason>.
```

Keep a round at 7 questions or less. Put the questions that change the most first.

## Topics

Ask only what the research did not settle:

1. **Problem and users.** Who has the problem? Which type of user? How do they see that the problem is gone?
2. **Outcome and measure.** What changes for the users and the business? Which number shows it?
3. **Scope.** What is in, and what is out?
4. **Behaviour.** The normal case, the most important edge cases, and what the user sees when it fails.
5. **Data.** What the work shows, stores, changes, or deletes. Which data is personal?
6. **Quality.** Only the ones that matter for this work: speed, security, privacy, accessibility, availability.
7. **Risks.** Value: do users want it? Usability: can they use it? Feasibility: can the team build it? Viability: does it fit the business, the law, and the budget?
8. **Constraints.** A system, a date, or a technology that the team cannot change. Write a technology as a constraint, not as a story.

## A question for a third person

When only another person can answer (legal, finance, an expert), write the question for that person in the "Decisions" section with the mark "waiting: <person>". Continue with the other questions. A "waiting" decision blocks G1, unless the human tells you to use an assumption.

## Gate G1

When no question is open, give the shared understanding in at most 10 lines. Give the users, the outcome and its measure, the scope, and the most important decisions. Ask: "Is this what you mean?"
