# Phase 2: Grill

## Contents

- Rounds
- Round 1: the need
- Round 2 and later: the decisions
- Impact scan
- Answers without a choice
- A question for a third person
- Gate G1

## Rounds

Interview the human until you both understand the work in the same way. Each decision opens the decisions that depend on it.

- Ask all questions that do not depend on another open question in one round. Then wait.
- Number the questions across all rounds (Q1, Q2, ...), so the human can answer "Q4: yes".
- One decision or one fact in each question. Give the options when there are options.
- Give a recommended answer and the reason in one sentence: "Recommended: <answer>, because <reason>."
- "ok" or "agree" means the recommended answer.
- Each sentence of a question has at most 20 words. Follow `language.md`.
- After each round, add the decisions to the "Decisions" section, and the facts to the "Problem" section.

Format:

```
**Q1. <Short title>.** <The question.>
Recommended: <answer>, because <reason>.
```

Keep a round at 7 questions or less. Put the questions that change the most first. When much is open, ask more rounds. Do not make a round longer.

## Round 1: the need

Always ask round 1, also when the documents describe the problem. A document often describes the solution, or the problem as the writer sees it. The human confirms the need in their own words.

Ask these 5 questions. When a source gives an answer, put it in the question as the recommended answer, with the source.

1. **Current work.** How do the users do this task today, step by step? Which tools or workarounds do they use?
2. **Where it goes wrong.** Where do the users get stuck today? What goes wrong most often?
3. **Exceptions.** Which cases are unusual? Which cases does the current way not handle?
4. **Groups that work differently.** Which groups of users or customers work differently? Which groups do not need this?
5. **Success.** How do we know that the problem is gone? Which number shows it, and what is the value today?

Write the answers in the "Problem" section. An answer that nobody can give goes to "Open questions", with its owner (for example "customer").

## Round 2 and later: the decisions

Ask only what round 1 and the research did not settle:

1. **Outcome.** What changes for the users and the business?
2. **Scope.** What is in, and what is out? Check each group from round 1 question 4.
3. **Behaviour.** The normal case, the exceptions from round 1, and what the user sees when it fails.
4. **Data.** What the work shows, stores, changes, or deletes. Which data is personal?
5. **Quality.** Only the ones that matter for this work: speed, security, privacy, accessibility, availability.
6. **Risks.** Value: do users want it? Usability: can they use it? Feasibility: can the team build it? Viability: does it fit the business, the law, and the budget?
7. **Constraints.** A system, a date, or a technology that the team cannot change. Write a technology as a constraint, not as a story.

## Impact scan

Do the impact scan in round 2. Find each new or changed object, for example an order, a document, or an address. For each object, ask about the parts of the product outside the main flow:

- Where do users create, change, or delete it: in the user interface, in an admin screen, or through an integration?
- Which other modules use or keep this data (master data, settings, reports)?
- Who can see or change it (roles, approval)?
- What must the log or the audit trail show?

When the team profile has a checklist for a new module, use that checklist in place of this list, or in addition to it.

## Answers without a choice

When an answer gives facts but no choice, write the facts in the "Problem" section. Then ask the choice one more time, in short form. When the second answer also gives no choice, write it in "Open questions" with the owner "human". Do not mark it "assumed".

## A question for a third person

When only another person can answer (legal, finance, an expert), write the question in "Open questions" with that person as the owner. Continue with the other questions. An open question that blocks the scope also blocks G1, unless the human tells you to use an assumption.

## Gate G1

G1 can start when the human answered round 1, and no question that blocks the scope is open.

Give the shared understanding in at most 10 lines. Give the problem in the words of the human, and the users. Name the groups that are out. Give the outcome and its measure, the scope, and the most important decisions. Give the number of open questions for each owner. Ask: "Is this what you mean?"
