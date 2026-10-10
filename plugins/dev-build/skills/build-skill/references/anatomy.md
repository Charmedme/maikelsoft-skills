# Anatomy of a skill

## Contents

- Is a skill the right form?
- Folder layout
- Frontmatter
- Body
- Descriptions
- Context cost

## Is a skill the right form?

A skill is right when at least one reason is true:

- The task repeats and needs the same steps each time.
- The task needs facts or files that the agent does not know and cannot find.
- The task needs a script that gives the same result each time.
- Many tasks share the same instructions, and a person must start them by name.

A skill is wrong when:

- The rule is for every task. Put it in `CLAUDE.md` or `AGENTS.md`.
- The rule must run each time without exception. Use a hook.
- The agent already does the task well without help. Test first (see `testing.md`).
- The task needs its own role, tools, or model. Use an agent (`build-agent`).

## Folder layout

```
skill-name/
  SKILL.md        required: frontmatter + body
  references/     optional: detail, loaded when needed
  scripts/        optional: code that runs, not code to read
  assets/         optional: templates, images, data
```

The folder name equals the `name` field. Link each file from `SKILL.md`. Link one level deep only. A file that links to a file that links to a file gets read in part only.

## Frontmatter

Fields of the open Agent Skills spec (portable):

| Field | Rule |
|-------|------|
| `name` | 1-64 chars, lowercase letters, digits, single hyphens. Same as the folder. No "claude" or "anthropic". |
| `description` | 1-1024 chars. What the skill does and when to use it. Third person. No tags. |
| `license` | Optional. |
| `compatibility` | Optional, up to 500 chars. Tools or network the skill needs. |
| `metadata` | Optional. Free key and value pairs. |
| `allowed-tools` | Optional. Tools that run without a prompt. |

Fields that only Claude Code knows: `when_to_use`, `disable-model-invocation`, `user-invocable`, `argument-hint`, `arguments`, `disallowed-tools`, `model`, `effort`, `context`, `agent`, `background`, `hooks`, `paths`, `shell`. Other runtimes can ignore them. The claude.ai upload can reject them. `skillcheck.py` warns for each one. Ask the human where the skill will run.

Useful Claude Code values:

- `disable-model-invocation: true` for a skill with side effects that only a person can start.
- `${CLAUDE_SKILL_DIR}` in a command gives the path of the skill folder.
- `context: fork` runs the skill in a separate agent. Use it only when the skill has real steps. A list of rules gives no result in a fork.

## Body

The agent loads the body when the skill triggers. It stays in the context for the rest of the session. After a compaction, Claude keeps about the first 5,000 tokens of each skill. So:

- Keep the body under 300 lines. The hard limit is 500.
- Put the most important steps first.
- Move long tables, examples, and rarely used cases to `references/`.
- Start a reference file with a "Contents" list when it has more than 100 lines.

## Descriptions

The description decides if the skill triggers. The agent sees only the name and the description of each skill until it picks one.

- Start with the type of the output or the task.
- Add one "Use when ..." sentence. Name the real situations, in the words a person uses.
- Write in third person: "Checks ...", not "I check ..." or "You can ...".
- Keep it lean. Add more trigger words only when the trigger test shows that the skill does not trigger.
- Do not repeat the steps of the skill in the description. The agent then follows the description and skips the body.

## Context cost

Each skill description is always in the context. Each body costs tokens when it triggers. So a small skill with a clear description is better than a large one. Split a skill when two parts have different triggers.
