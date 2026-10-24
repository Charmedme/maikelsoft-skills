# dev-team

dev-team is a Claude Code plugin that delivers one user story with a small team of agents. The main session leads the team. A senior designs the story and builds the critical core. Mediors build the work packages. A security reviewer checks the diff. The human approves two times: the design (G1) and the result (G2).

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

Status: version 0.3.0, preview. Since 0.3.0, `dev-flow:plan-and-build` builds each item in one session and starts dev-team only when you ask for the team. Product questions of the senior go to the product owner as a comment on the item. Read the test results below before you use it on real work.

## Install

You need Claude Code, Python 3.9 or later, and the dev-stack plugin. For a .NET repository you also need the .NET SDK.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-stack@maikelsoft-skills
claude plugin install dev-team@maikelsoft-skills
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

## Modes

The senior chooses the mode in the design. G1 shows it, and you can ask for `full`.

| Mode | When | Steps |
|------|------|-------|
| `light` | No new package, no schema change, no work packages, and no change to sign-in, authorization, secrets, file upload, or an outbound call. | Design, G1, core build, core review with the security checklist, G2. |
| `full` | All other stories. | Design, reflection, G1, core build, core review, work packages, security review, G2. |

## Test results

Two test rounds and a light-mode check on 2026-10-18, with Claude Opus, on a .NET 10 order API. Each story ran 3 times with dev-team and 3 times with plain Claude Code plus dev-stack (the baseline). Hidden acceptance tests checked the result.

| Story | dev-team | Baseline |
|-------|----------|----------|
| Small (read one order), light mode | 3 of 3 pass, $4.10, 15 min | 3 of 3 pass, $0.40, 1 min |
| Medium (cancel an order) | 3 of 3 pass, $8.00, 26 min | 3 of 3 pass, $0.60, 2 min |
| Medium with a time zone risk | 3 of 3 pass, $11.10, 39 min | 3 of 3 pass, $0.80, 3 min |
| Large (ship and history) | 2 of 3 pass, $9.70, 32 min. One run stopped too early. 0.2.0 fixes it. | 3 of 3 pass, $1.10, 4 min |
| Trap (a fact of the human is missing) | 3 of 3 stop and ask | 0 of 3 stop: all guess and build |

The cost and time are medians. What this means:

- The code of the team was not more correct than the baseline: both passed all hidden acceptance tests.
- The team stops when a fact of the human is missing. The baseline guesses.
- The team writes more tests on medium and large stories (median 15 against 10).
- The team leaves a full record: design, ADRs, decision index, reviews, security review, and a G2 report.
- The team costs about 10 times as much (light mode) to 13 times as much (full mode), and it takes much longer.

Use dev-team when a wrong guess costs more than the extra time and money, or when you need the record. For a patch, a small, or a medium item, use `dev-flow:plan-and-build`. In the tests it passed the same hidden acceptance tests at 3 to 5 times lower cost.

## When it helps

Use it when:

- You need a full record: a design, ADRs, a decision index, reviews, and a security review.
- The change is risky, and a separate security review of the diff is worth the cost.

Do not use it when:

- You want a correct result at a low cost. One session with `dev-flow:plan-and-build` gets the same result. The team costs about 10 times as much as plain Claude Code.
- You want a fast result. A team run takes 15 to 40 minutes.
- The stack is not .NET. The stack skills cover .NET only.

## Limits

- The senior starts the mediors as nested agents. This needs `CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH` of 2 or more (default 3).
- Plugin agents do not use hooks, MCP servers, or a permission mode of their own.
- The stack skills cover .NET in dev-stack 1.1.0: 18 skills. Angular, TypeScript, Docker, and Kubernetes use the official docs until their skills exist.
