# Grill

Interview the human until you both have the same understanding of the skill or the agent. Do this before you write a concept or a file. Most bad skills come from a short interview, not from bad writing.

## Contents

- Rules
- Rounds
- Topics for a skill
- Topics for an agent
- Improve mode
- The brief
- Gate G1

## Rules

- **Facts are your job. Decisions are the human's job.** Find a fact in the conversation, the files, or the docs. Do not ask the human for it. Put each decision to the human, with your recommended answer.
- **Start from what you have.** If the conversation already shows the workflow ("turn this into a skill"), take the steps, the inputs, the outputs, and the corrections from it. Ask only about the gaps.
- **Ask for a walkthrough.** Ask: "Which steps do you do today, in order, from the input to the result?" An example of the output alone is not enough. A real run shows steps that a description leaves out.
- **Ask about the bad case.** Ask where the task goes wrong today, and what an agent without help gets wrong. These answers become the most important rules.

## Rounds

A decision can depend on another decision. This is the design tree. The **frontier** is each open decision that does not depend on another open decision. Ask the full frontier in one round. Then stop and wait for the answers.

Format of a round:

```
**Q1. <Short title>.** <The question. Give the options when there are options.>
Recommended: <your answer, and the reason in one sentence>

**Q2. <Short title>.** <...>
Recommended: <...>
```

- Number the questions across all rounds (Q1, Q2, ... Q14), so the human can answer "Q7: no".
- Put one decision in each question.
- A question that depends on another question in the same round goes to the next round.
- When the human says "ok", the recommended answers are the decisions.
- After each round, write the decisions in the brief. Then make the next frontier.

Round 1 always has three questions:

- The gate: is a skill or an agent the right form?
- The walkthrough: which steps do you do today?
- The bad case: what goes wrong today?

For the gate, read the reasons in `anatomy.md` (skills) or in the "Gate" section of the `build-agent` skill (agents). If no reason fits, recommend the other form and stop when the human agrees.

## Topics for a skill

Ask only the topics that the conversation and the files do not settle. Done when each topic has a decision or the human said "not needed".

1. **The need.** Who uses the skill, and what problem does it solve? What happens today without it?
2. **The workflow.** The steps in order, as the human does them today. The input of each step. The decision points. The step that goes wrong most.
3. **The output.** The form (file, message, change), the format, and who reads it.
4. **The triggers.** 3 real requests that must start the skill. 2 close requests that must not.
5. **Success.** How the human sees that a result is good. Each answer becomes a yes or no assertion for the test.
6. **Limits.** What the skill must not do, and why. Side effects (send, delete, deploy) that need approval.
7. **Language.** The language of the output, the level, and (Dutch) "je" or "u".
8. **Runtime.** Claude Code only, or also claude.ai and other tools. This sets the frontmatter.

## Topics for an agent

Use the topics for a skill, with these changes:

- In topic 2, ask what the agent receives from its caller and what it returns.
- Add **tools**: which tools the job needs, and which it must not have.
- Add **model**: start with the smallest model, unless the human has a reason.
- Add **caller**: who starts the agent (a parent agent, a person, a schedule), and what the agent does when a fact is missing.

## Improve mode

Start from the failure, not from the topics. Ask for one bad run: the request, the output, and what the human expected. Ask what changed since the author wrote the skill. Read the existing brief first, if there is one. Then ask only the topics that the failure touches.

## The brief

Write the decisions in the brief after each round. Use this form. Keep it under 60 lines.

```markdown
# Brief: <name>

Status: draft | approved (G1, <date>) | not approved (no human)

## Need
<Who, what problem, what happens today.>

## Workflow
1. <Step>. Input: <...>. Goes wrong when: <...>.

## Output
<Form, format, reader.>

## Triggers
- Must start: <request>
- Must not start: <request>

## Success criteria
- <A yes or no check.>

## Limits
- <Limit>, because <reason>.

## Language and runtime
<Language, level, runtime.>

## Decisions
| No | Decision | Who |
|----|----------|-----|
| Q1 | <...> | human / recommended, accepted |
```

Where the brief goes:

- A skill: `brief.md` in the skill folder. Do not link it from `SKILL.md`, so it does not load with the skill. Improve mode reads it.
- An agent: `docs/work/<agent-name>/brief.md`. Do not put it in an `agents/` folder, because Claude Code reads each Markdown file there as an agent.

## Gate G1

When the frontier is empty, give a summary of the brief in at most 10 lines. Ask: "Is this the shared understanding?" Ask this in its own message, after the last round. An answer to a question in a round is not the G1 approval. Do not write the concept, the tests, or any skill file before the human says yes. Then set the status of the brief to "approved".
