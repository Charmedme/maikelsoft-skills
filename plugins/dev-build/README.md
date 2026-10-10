# dev-build

dev-build is a Claude Code plugin for people who write agent skills and agents. It has two skills. `build-skill` makes a skill. `build-agent` makes an agent. Each skill follows four steps: research, concept, test, and build. You approve the concept before the skill builds anything.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-build@maikelsoft-skills
```

## Quick start

1. Open Claude Code.
2. Ask for a skill or an agent in your own words:

   ```
   Make a skill that checks supplier invoices for missing fields.
   ```

   ```
   Make a subagent that reviews pull requests for security problems.
   ```

3. The skill checks that a skill or agent is the right form. Then it asks all questions in one message.
4. Answer the questions and approve the concept. The skill builds the files and runs its checkers.

## Skills

| Skill | Use it for |
|-------|------------|
| `build-skill` | A new skill, a change to a skill, or a review of a skill. Includes a gate (is a skill the right form?), a test method, and the steps to publish in a plugin. |
| `build-agent` | A new agent, a change to an agent, or a review of an agent. Starts from a role spec that does not depend on the runtime. Renders it for Claude Code, the Agent SDK, Managed Agents, or OpenClaw. |

Each skill has three modes: Create, Improve, and Review. Review changes nothing. Each finding cites a rule ID.

## Checkers

| Script | What it checks |
|--------|----------------|
| `skillcheck.py` | A skill folder: the name, the description, the length, the links, the references, the frontmatter fields, and the style. |
| `agentcheck.py` | An agent file or folder: the frontmatter fields and values, the tools, the model, the prompt, and the OpenClaw workspace limits. |
| `doccheck.py` | The instruction text, against ASD-STE100 or Begrijpelijk Nederlands. |

Run them on your own files:

```sh
python3 plugins/dev-build/skills/build-skill/scripts/skillcheck.py <skill folder>
python3 plugins/dev-build/skills/build-agent/scripts/agentcheck.py <file or folder> --runtime claude-code
```

## Test results

Method and results are in `evals/dev-build/`. In short:

- The skills make the agent stop and ask before it builds (3 of 3 runs, against 0 of 3 for a plain agent).
- The review mode cites rule IDs (3 of 3 runs, against 0 of 3).
- For `improve`, a plain agent did as well on the checks. The value is the checker and the rules.
- Nobody ran an agent that the skills built on real work. The Managed Agents and OpenClaw parts have static checks only.

## Limits

- Agent files work in Claude Code. They do not work in the claude.ai chat.
- The OpenClaw reference does not cover the `tools`, `sandbox`, and `bindings` keys. Read the OpenClaw docs before you write them.
- Managed Agents is in beta. Check the docs for changes.
