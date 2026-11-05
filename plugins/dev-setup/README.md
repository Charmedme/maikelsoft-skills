# dev-setup

dev-setup is a Claude Code plugin that sets up the delivery flow one time, for a person and for a project. `/setup` asks your role and keeps it for all your projects or for one repository. It asks the facts of the project that the repository does not show, and writes them into files that the team shares. A session-start hook then gives each session your role, so each command of product-flow, dev-flow, dev-team, and ux-design knows your route.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-setup@maikelsoft-skills
```

## Quick start

1. Open Claude Code in your repository.
2. Type `/setup`.
3. Answer the questions in one reply: your role, all projects or this project, and the facts that `/setup` cannot find.
4. Approve the list of files.
5. Commit the new files. Your role is not in the repository: it stays on your computer.

## Commands

Claude Code starts a skill by itself when your request matches its trigger. You can also start a skill with its command. Text after the command goes to the skill, for example `/setup route team=on`.

| Command | What it does | Starts by itself when you ask for |
|---------|--------------|-----------------------------------|
| `/dev-setup:setup` | Your role, the facts of the project, and the review and CI files. `/setup route` changes the steps of your route. `/setup starter` makes the Claude Code basics of a project. | Your role or route set or changed, a project set up for the delivery flow, or the Claude Code basics of a repository |

The short form `/setup` works when no other installed plugin uses that name.

## Usage

### Roles

| Role | First command | Route defaults |
|------|---------------|----------------|
| Product owner or analyst | `/to-backlog` | The refine pass after each item. |
| Designer | `/ux-design` | The final gate of the build waits for your UX review. |
| Developer | `/build` | The build asks before it gives large work with high risk to the team. |
| Tech lead | `/build` | The build gives large work with high risk to the team without a question. |
| All of them | `/to-backlog`, then `/build` | Your approval of a ready spec also approves its low-risk scenarios. |

The role sets defaults only. It never stops a command.

### Where the role is stored

| Where you work | Source of the role |
|----------------|--------------------|
| Claude Code on your computer | `roles.json` in the data folder of the plugin. An entry for this repository wins over the entry for all projects. Each worktree and each clone of the repository gets the same entry. |
| Claude Code on the web | The environment variable `DELIVERY_ROLE` of the cloud environment, for example `developer` or `developer; team=on`. |
| claude.ai | A block in the instructions of your Project. `/setup` gives the block. |

When you remove the plugin, Claude Code deletes the data folder and your stored role.

### Files in the repository

`/setup` shows the list first, and writes after your approval. It never overwrites a file. For a file that exists, it adds lines only after your approval.

- `.claude/rules/delivery.md`: the facts of the project. Claude Code loads it at the start of each session.
- `REVIEW.md`: what a reviewer must flag and skip, and the paths of high risk.
- A pull request template with Testing, Risk, and How to undo.
- A CI check for GitHub Actions or Azure Pipelines that fails a pull request when an approved test changed. It runs `.dev-flow/speclock.py`, a copy that `/setup` keeps current.

### The starter

`/setup starter` makes the parts that do not exist:

- a short `CLAUDE.md`,
- `.claude/settings.json` and `.claude/rules/`,
- the empty folders `skills/`, `agents/`, and `hooks/`,
- a `.claude/README.md` with a short instruction for each part.

It also adds `CLAUDE.local.md` and `.claude/settings.local.json` to `.gitignore`. For an existing `CLAUDE.md`, it only proposes additions.

## Limits

- `/setup` does not connect the tracker. Add the MCP server or the CLI of your tracker first.
- The other plugins read the role line from their next major version.

## License

MIT
