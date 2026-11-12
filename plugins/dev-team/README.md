# dev-team

dev-team is the team mode of the dev-flow plugin. It builds one large or risky work item with a small team of agents. The main session leads the team. A senior designs the item and builds the critical core. Mediors build the work packages, and a security reviewer checks the diff.

The human approves two times: the plan (G1) and the result (G2). The team works on the same chain as a solo build of `/dev-flow:build`. It uses the work folder `docs/work/<item>/` and the header of `run.md`. It also uses `spec.md` with the Behaviour spec of the ticket, and one locked test file for each rule.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

Status: version 1.0.0. The team has no command of its own. `/dev-flow:build` gives it a large item with a risk. `/dev-flow:review-comments` gives it the comments on a pull request of a team run.

Product questions of the senior go to the product owner as a comment on the item. Read the test results below before you use it on real work. The test results come from version 0.6.0 and earlier.

## Install

You need Claude Code, Python 3.9 or later, the dev-flow plugin 3.1.0 or later, and the dev-stack plugin. For a .NET repository you also need the .NET SDK. dev-team declares dev-flow as a dependency. When dev-flow is missing, Claude Code installs it with dev-team (see [Plugin dependencies in the Claude Code docs](https://code.claude.com/docs/en/plugins/dependencies)). The dependency has no version range, so update dev-flow yourself when you have a version before 3.1.0.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-stack@maikelsoft-skills
claude plugin install dev-team@maikelsoft-skills
```

## Quick start

1. Open Claude Code in your repository.
2. Build the item:

   ```
   /dev-flow:build PROJ-123
   ```

3. `build` copies the Behaviour spec into `spec.md` and sets the size. For a large item with a risk, it asks: "The team of dev-team can build this. Do you want the team?" Answer "yes". With the route `team=on` (for example a tech lead), it takes the team without the question. You can also ask for the team: "Build PROJ-123 with the team."
4. The team designs the item and shows gate G1, the plan gate. G1 shows the design, the scenarios (each rule with its test file), the open decisions, and the new packages. Answer "ok", or name the rows that you change.
5. The team locks the test file of each rule and builds on the branch of the item. It reviews each part one time, and runs a security review and the held-back tests. It updates the docs of the change.
6. Gate G2, the final gate, shows the checks, the rules with their locked tests, and the open findings. You review the branch, push it, and open the pull request.
7. When the pull request has comments, run `/dev-flow:review-comments`. It gives a team run to the team.

## Commands

dev-team has no command in the `/` menu. Use the commands of dev-flow:

| Command of dev-flow | What it does with the team |
|---------------------|----------------------------|
| `/dev-flow:build` | Offers the team for a large item with a risk, or takes the team when your route says `team=on`. Continues a team run when the header of `run.md` says `flow: team`. |
| `/dev-flow:review-comments` | Gives the comments on a pull request of a team run to the team. |

Claude Code also starts the team when you ask for it, for example "let the team do it". The skills of dev-team are out of the menu, and other skills start them. The agents have no command. A skill starts them.

| Agent | What it does | Started by |
|-------|--------------|------------|
| `dev-team:senior` | Designs the story, builds the core, and leads the mediors. | `deliver-story` and `process-review` |
| `dev-team:medior` | Builds one work package of size standard. | The senior agent |
| `dev-team:medior-large` | Builds one work package of size hard, with more reasoning effort. | The senior agent |
| `dev-team:security-reviewer` | Reviews the full diff of a story one time for security. | `deliver-story` |

## What is in the plugin

| Part | Name | What it does |
|------|------|--------------|
| Skill | `deliver-story` | The team mode of `build`. The main session leads the run: spec, design, reflection, G1, held-back tests, locked tests and core, packages, security review, UX review, prove, docs, G2. The UX review runs when the story has UI work and a UX design or a mock-up, and the `ux-design` plugin is installed. After G2, later changes go through the agents. |
| Skill | `process-review` | The team mode of `review-comments`. It sorts the comments into the classes of dev-flow, has the senior fix them, has a second senior review the fix, and writes draft replies. |
| Skill | `story-intake` | Reads the item from Jira, Azure DevOps, GitHub, GitLab, or Linear (read only), and writes `spec.md` with `specsync.py`. After a hand-over from `build`, it adds only the item section. |
| Skill | `senior-practice` | The rules and templates of the senior: design, packages, reflection, plan feedback, review, self-check. |
| Skill | `medior-practice` | The rules and templates of a medior: plan, build test first, report with real output, fix. |
| Skill | `stack-index` | Tells each agent which dev-stack skill to load. |
| Skill | `team-decisions` | The decision sheet, the decision index, one ADR per decision, the profile, and `decisioncheck.py`. |
| Agent | `senior` | Opus, effort high. Designs, reflects, builds the core, leads the mediors. |
| Agent | `medior` | Sonnet, effort medium. Plans and builds a standard package. |
| Agent | `medior-large` | Sonnet, effort high. Plans and builds a hard package. |
| Agent | `security-reviewer` | Opus, effort high. Reviews the diff one time. |
| Hook | `log-command.py` | Writes each Bash command of a run to `commands.jsonl`. It tries `python3`, `python`, and `py -3`. |
| Hook | `session-start.py` | In a project that uses dev-team, gives the rules of the team. It also names the entry skill for each kind of work, unless dev-setup is installed, enabled, and has a role set. Then dev-setup gives the route. |

## Rules of the team

- The agents create a branch and commit on it. They do not push and do not open a pull request.
- Each step writes a file in the work folder `docs/work/<item>/` and a row in `log.md`. The main session can start again from the last good step.
- `run.md` starts with the shared header of dev-flow and dev-team (`flow: team`). `runcheck.py` reads it.
- The Behaviour spec of the ticket is the contract. `spec.md` holds a copy with the fingerprint of each rule. When the PO changes a rule, the team asks you to accept, and unlocks only the tests of that rule.
- The test file of each rule is locked with `speclock.py`, as in a solo build. No agent changes a locked test. A high-risk rule asks you again before the lock.
- The branch follows the branch rules of the project, else `feat/<item>` or `fix/<item>`.
- One reflection, one plan feedback, one review, and one fix per part. No loops.
- A fix changes only the comments that its prompt names, and it keeps the scope that you approved at G1. For a bug, it commits the failing test first.
- `riskcheck.py` runs after the core build and again at G2. It adds a security review for a sign-in or authorization reason. It gives a warning for a migration or a package, for a reason that went away, and for a fix that changed a test. It is a path and pattern check. It cannot say that a change is safe.
- `process-review` shows the number of rounds. It stops at the round cap of the size (3 for a large item). It also stops when a cause comes back, or when an earlier fix broke the behavior.
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

Use dev-team when a wrong guess costs more than the extra time and money, or when you need the record. For a patch, a small, or a medium item, use `dev-flow:build` in one session. In the tests it passed the same hidden acceptance tests at 3 to 5 times lower cost.

## When it helps

Use it when:

- You need a full record: a design, ADRs, a decision index, reviews, and a security review.
- The change is risky, and a separate security review of the diff is worth the cost.

Do not use it when:

- You want a correct result at a low cost. One session with `dev-flow:build` gets the same result. The team costs about 10 times as much as plain Claude Code.
- You want a fast result. A team run takes 15 to 40 minutes.
- The stack is not .NET. The stack skills cover .NET only.

## Move from 0.6.0

- The commands `/dev-team:deliver-story`, `/dev-team:process-review`, and the other skill commands are gone from the `/` menu. Use `/dev-flow:build` and `/dev-flow:review-comments`.
- A new run writes `spec.md` in place of `story.md`. An item needs a Behaviour spec, or it gets the refine pass of `/dev-flow:build` first. A chore or a task needs none.
- A run that you started with 0.6.0 goes on. The team gives its `run.md` the shared header with `baseline: legacy` (`runheader.py adopt`), and it keeps `story.md` and its branch. Such a run has no locked tests and no held-back tests.
- A new branch follows the branch rules of the project, not `dev-team/<slug>`.

## Limits

- The senior starts the mediors as nested agents. This needs `CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH` of 2 or more (default 3).
- Plugin agents do not use hooks, MCP servers, or a permission mode of their own.
- The UX review of an item with only a mock-up passes the mock-up in the prompt. The `ux-design` agent does not read it as a design source yet.
- `checks.py` runs the checks that the project lists under "CI checks" in `docs/dev-team/conventions.md`. It names no analyzer. A project that does not list them gets the note "CI parity: not known".
- The stack skills cover .NET in dev-stack 1.1.0: 18 skills. Angular, TypeScript, Docker, and Kubernetes use the official docs until their skills exist.
