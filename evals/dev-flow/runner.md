# Run prompt

The orchestrator replaces the values in angle brackets and gives this prompt to a new agent. One agent for each run.

## Common part

You work in the git repository `<repo>`. The human wants this:

> <request>

The human is not always in this conversation. When you need an answer, a decision, or an approval from the human, end your turn with your message to the human. The next message that you get is the reply of the human. Never invent a reply of the human.

Keep a log. Before you end a turn for the human, append your message under a heading `## Agent` to `<run>/transcript.md`. When the reply comes, append it under `## Human`.

Sub-agents are not available here. Do all work yourself. Do not push to a remote. Read only files in the repository and in the skill folders below.

When the human gives the final "done", end your turn with the line `RUN COMPLETE`.

## Variant: skill

Use the skill `plan-and-build`. Its folder is `<skill>`. This folder is `${CLAUDE_SKILL_DIR}` in the skill text. Read `<skill>/SKILL.md` and follow it.

## Variant: baseline

The human works with the skills of Matt Pocock. The human calls them one by one, in this order: grill-me, to-spec, to-tickets, prototype, implement. When the human calls a skill (for example "/to-spec"), read its SKILL.md and follow it. The skills are in `<matt>`:

- grill-me: `productivity/grill-me/SKILL.md` (it calls `productivity/grilling/SKILL.md`)
- to-spec: `engineering/to-spec/SKILL.md`
- to-tickets: `engineering/to-tickets/SKILL.md`
- to-questionnaire: `productivity/to-questionnaire/SKILL.md`
- prototype: `engineering/prototype/SKILL.md`
- implement: `engineering/implement/SKILL.md`, with `engineering/tdd/SKILL.md` and `engineering/code-review/SKILL.md`

The human set up the tracker. The tracker is local Markdown files. Put the spec at `.scratch/<feature-slug>/spec.md` and the tickets under `.scratch/<feature-slug>/issues/`. The triage label is `ready-for-agent`.

The human starts with: "/grill-me <request>"
