# Brief: dev-setup

Status: approved (G1, 2026-11-05)

## Need
In plan version 7, each command first reads the role of the person and the facts of the project (`shared/context.md`). Today each plugin asks its own setup questions. product-flow asks for the tracker and the backlog profile. ux-design asks for the design tools, and dev-team for the route. A person answers the same questions more than once, and no plugin knows the role. dev-setup asks them one time, keeps the role for each repository, and prints it at each session start.

## Form
A new plugin `dev-setup` 1.0.0 with:
- one skill `setup` (the command `/setup`, with the arguments `route` and `starter`),
- a session-start hook that prints the role line,
- a script `rolestore.py` that reads and writes the roles.

A skill is the right form for the questions and the files. A hook is the right form for the role line, because it must be in each session without a command.

## Workflow
1. **`/setup`.** Read what exists: the role store, `.claude/rules/delivery.md`, `REVIEW.md`, `CLAUDE.md`, the git remote, the installed plugins, and the project files. Goes wrong when: it asks for a fact that the repository already shows.
2. **One round of questions**, each with a recommended answer. It asks for the role, the scope (all projects or this project), and the missing facts. Goes wrong when: it asks the route steps of a person who has not seen them work.
3. **Show the list of files**, with a short preview of each, and write them after one approval. Never overwrite: an existing file gets a proposed addition. Goes wrong when: it changes a file of the team without a word.
4. **Run the setup step of each installed plugin** for its own section of `delivery.md` (for example `ux-setup` for `## Design`). Goes wrong when: two plugins ask the same question.
5. **Check the short names** of the installed commands against each other and against the built-in commands. Report each clash with the full name to use.
6. **Report**: the role and the place of the role store, the files, and the clashes. End with `Next:` and the first command of the role.

`/setup route` shows the steps of the route with their values and changes them. `/setup starter` makes the Claude Code basics of the project (decision D7).

## Output
- The role and the route in `roles.json` in the data folder of the plugin.
- Project files, committed by the person: `.claude/rules/delivery.md`, `REVIEW.md`, a pull request template, a CI check, and with `starter` the basics of D7.
- For claude.ai: a block for the Project instructions.

## Triggers
- Must start:
  - "/setup"
  - "set up this project for the delivery flow"
  - "set my role to product owner"
  - "change my route so the team takes large work"
  - "set up the Claude Code basics of this repo"
- Must not start: "write a CLAUDE.md" (built-in `/init`), "set up ESLint", "configure the tracker connection" (MCP setup), "set up the design tools only" (`ux-design:ux-setup`), "add a GitHub Actions build".

## Success criteria
- The role line of the hook comes from the right source: `DELIVERY_ROLE`, else the project entry, else the global entry. The project entry is the same in each worktree of the repository.
- `/setup` asks at most one round of questions, and no question whose answer is in the repository.
- `/setup` never overwrites a file. It shows the list and writes after one approval.
- `/setup route` changes only the steps that the person names.
- `/setup starter` makes each part of D7 that does not exist, and changes no existing file.
- The report names each clash of a short name.

## Limits
- No tracker connection. The MCP server or the CLI of the tracker is a setup of its own. `/setup` says when it does not find one.
- No change to another plugin in this step. The other plugins read the role line from step 5 on. The dev-team hook stops printing its route in step 7.
- When the person removes the plugin, Claude Code deletes `roles.json`. The report says so one time.

## Decisions
| No | Decision | Who |
|----|----------|-----|
| D1 | A new plugin `dev-setup` 1.0.0: one skill `setup` with the arguments `route` and `starter`, one session-start hook, and `rolestore.py`. Python with the standard library, as the other hooks. | recommended, accepted |
| D2 | `/setup` asks for the role: product owner or analyst, designer, developer, tech lead, or all. It asks for the scope: all projects or this project. It asks only for the missing facts. These are the tracker and its ready state, the backlog profile, and the item file format without a tracker. They are also the languages, the design tools and the run command, and the review limits. The route takes the defaults of the role. | plan version 7 |
| D3 | The roles are in `${CLAUDE_PLUGIN_DATA}/roles.json`, with a `global` entry and a `projects` map. The project key is the git remote as host and path, in lower case, without `.git`. Without a remote, it is the path of the git common folder. The hook reads `DELIVERY_ROLE` first, then the project entry, then the global entry. It prints the line of `shared/context.md`, or nothing when it finds no role. | plan version 7 |
| D4 | `/setup` shows each file that it makes or changes, with a short preview, and writes them after one approval. It never overwrites. An existing file gets a proposed addition. | human |
| D5 | The CI check runs `speclock.py check` on each pull request. It is for GitHub Actions and Azure Pipelines. For other CI, `/setup` gives the command. CI cannot reach the plugin folder. So `/setup` copies `speclock.py` into `.dev-flow/` of the repository, and a new `/setup` updates the copy. | human for the CI systems, recommended for the copy |
| D6 | The pull request template has the sections Testing, Risk, and How to undo. `REVIEW.md` comes from `shared/review-template.md`, with the risk tiers. | plan version 7 |
| D7 | `/setup starter` makes a short `CLAUDE.md` from its own scan (at most about 60 lines), `.claude/settings.json`, `.claude/rules/delivery.md`, and the empty folders `.claude/skills/`, `.claude/agents/`, and `.claude/hooks/`. It makes one `.claude/README.md` with a short section for each folder: what it is for, and how to configure it. It adds `.gitignore` lines for `CLAUDE.local.md` and `.claude/settings.local.json`. It makes no `commands/` folder, because skills replace it. It changes no existing file, and only proposes additions to an existing `CLAUDE.md`. | human |
| D8 | In claude.ai, the skill cannot write files. `/setup` in Claude Code prints the block for the Project instructions. The product-flow README gets the same block as a template (step 5). | plan version 7 |
| D9 | Test with the build-skill method: 3 prompts, 3 runs with and 3 runs without the skill, and 10 trigger queries. Unit tests for the hook and `rolestore.py` check four cases. The project entry wins over the global entry. A second worktree gets the same entry. `DELIVERY_ROLE` wins. The hook prints nothing without a role. | human |

## Language and runtime
English, ASD-STE100, as the other plugins. Claude Code in the terminal, the desktop app, and the IDE. A cloud session gets the role from `DELIVERY_ROLE`.
