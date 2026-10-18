# dev-team

dev-team is a Claude Code plugin that delivers one user story with a small team of agents. The main session leads the team. A senior designs the story and builds the critical core. Mediors build the work packages. A security reviewer checks the diff. The human approves two times: the design (G1) and the result (G2).

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

Status: version 0.1.0, not tested end to end yet. It is not in the marketplace yet.

## Install

You need Claude Code, Python 3.9 or later, and the dev-stack plugin. For a .NET repository you also need the .NET SDK.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-stack@maikelsoft-skills
claude --plugin-dir plugins/dev-team
```

## Quick start

1. Open Claude Code in your repository.
2. Give the story:

   ```
   Deliver story PROJ-123 with the dev team.
   ```

3. The team reads the story, designs it, and shows gate G1: the design, the open decisions, and the new packages. Answer "ok", or name the rows that you change.
4. The team builds on the branch `dev-team/<slug>`, reviews each part one time, and runs a security review.
5. Gate G2 shows the checks, the acceptance criteria with their tests, and the open findings. You review the branch, push it, and open the pull request.

## What is in the plugin

| Part | Name | What it does |
|------|------|--------------|
| Skill | `deliver-story` | The main session leads the run: story, design, reflection, G1, core, packages, security review, G2. |
| Skill | `story-intake` | Reads the story from Jira, Azure DevOps, GitHub, GitLab, or Linear (read only) and writes `story.md`. |
| Skill | `senior-practice` | The rules and templates of the senior: design, packages, reflection, plan feedback, review, self-check. |
| Skill | `medior-practice` | The rules and templates of a medior: plan, build test first, report with real output, fix. |
| Skill | `stack-index` | Tells each agent which dev-stack skill to load. |
| Skill | `team-decisions` | The decision sheet, the decision index, one ADR per decision, the profile, and `decisioncheck.py`. |
| Agent | `senior` | Opus, effort high. Designs, reflects, builds the core, leads the mediors. |
| Agent | `medior` | Sonnet, effort medium. Plans and builds a standard package. |
| Agent | `medior-large` | Sonnet, effort high. Plans and builds a hard package. |
| Agent | `security-reviewer` | Opus, effort high. Reviews the diff one time. |
| Hook | `log-command.py` | Writes each Bash command of a run to `commands.jsonl`. |

## Rules of the team

- The agents create a branch and commit on it. They do not push and do not open a pull request.
- Each step writes a file in the run folder `docs/work/<slug>/` and a row in `log.md`. The main session can start again from the last good step.
- One reflection, one plan feedback, one review, and one fix per part. No loops.
- A team decision or a new package needs the approval of the human.
- `runcheck.py` compares the log with the command log. A claimed check without a command is a finding.

## Limits

- The senior starts the mediors as nested agents. This needs `CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH` of 2 or more (default 3).
- Plugin agents do not use hooks, MCP servers, or a permission mode of their own.
- The stack skills cover .NET in dev-stack 1.0.0. Other stacks use the official docs until their skills exist.
