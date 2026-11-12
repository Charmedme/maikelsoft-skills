# /setup starter

The starter makes the Claude Code basics of a project. It makes only what does not exist. It never writes to an existing `CLAUDE.md`, also not after a "yes": the person adds the proposed lines. `.gitignore` is the only existing file that it changes, and only with new lines.

## Steps

1. **Scan.** Read the README and the project files (`*.sln`, `*.csproj`, `package.json`, `pyproject.toml`, `go.mod`). Also read the CI files, `.gitignore`, and the folders at the root. List what exists of the parts below. Done when you know the stack, the build and test commands, the layout, and the parts that exist.
2. **Show the list.** For each missing part, one line with its path and a preview of a few lines. For `.gitignore`: the lines to add. For an existing `CLAUDE.md`: the proposed lines, marked "for you to add". Done when the person approves the list.
3. **Write.** Write the approved parts. An empty folder gets a `.gitkeep` file, because git does not keep an empty folder. Done when each approved part exists, and `doccheck.py` shows 0 errors on each new Markdown file.
4. **Report.** List each new file, and each proposed addition that the person did not approve. Add `Next: /setup` when the person has no role.

## Parts

| Part | Content |
|------|---------|
| `CLAUDE.md` | From the scan, at most about 60 lines (template below). When it exists: give the proposed lines in the report, for the person to add. |
| `.claude/settings.json` | The template below. |
| `.claude/rules/delivery.md` | The `## Build` section of `references/files.md` with the commands from the scan. |
| `.claude/skills/`, `.claude/agents/`, `.claude/hooks/` | Empty, with `.gitkeep`. |
| `.claude/README.md` | The template below: one short section for each part. |
| `.gitignore` | Add `CLAUDE.local.md` and `.claude/settings.local.json` when they are not in it. Keep each line that exists. |

Do not make `.claude/commands/`: a skill in `.claude/skills/` replaces a command. Do not make `.mcp.json` or `CLAUDE.local.md`: the README explains them.

## CLAUDE.md

```markdown
# CLAUDE.md

<One sentence: what the project is, and for whom.>

## Commands

- Build: `<command>`
- Test: `<command>`
- Run: `<command>`

## Layout

- `<folder>`: <what it holds>

## Rules

- <Each convention that the scan shows clearly. No guess.>
- Topic rules are in `.claude/rules/`.
```

## .claude/settings.json

```json
{
  "$schema": "https://json.schemastore.org/claude-code-settings.json",
  "permissions": {
    "allow": [],
    "deny": ["Read(./.env)", "Read(./.env.*)", "Read(./secrets/**)"]
  }
}
```

Add the build and test commands to `allow` as `Bash(<command>:*)`, for example `Bash(dotnet test:*)`.

## .claude/README.md

```markdown
# The .claude folder

Claude Code reads the files in this folder and at the root of the project. Commit each file, except the files that end in `.local`.

## CLAUDE.md (root)
The instructions that Claude reads at the start of each session: what the project is, the commands, and the rules. Keep it short. Put a rule for one topic in `rules/`.

## CLAUDE.local.md (root, not committed)
Your personal notes for this project. Git ignores it.

## .mcp.json (root)
The MCP servers of the project, for example the connector of the tracker. Add a server with `claude mcp add --scope project <name> ...`.

## settings.json
The shared settings: permissions, hooks, and the environment. `permissions.allow` lists the commands that run without a question. `permissions.deny` blocks files such as `.env`.

## settings.local.json (not committed)
Your personal settings for this project. They win over `settings.json`. Git ignores this file.

## rules/
One Markdown file for each topic, for example `testing.md`. Claude Code loads each file at the start of a session. A file with `paths:` in its frontmatter applies only to these files.

## skills/
One folder for each skill, with a `SKILL.md`. A skill gives the steps for one kind of task. Type `/<name>` to start it, or Claude starts it when your request matches its description.

## agents/
One Markdown file for each subagent, with its role, its tools, and its model in the frontmatter. Claude gives a task to a subagent, and the subagent works in its own context.

## hooks/
The scripts that hooks run. A hook is a command that runs at a fixed moment, for example before a tool runs. Register each hook in the `hooks` section of `settings.json`.
```
