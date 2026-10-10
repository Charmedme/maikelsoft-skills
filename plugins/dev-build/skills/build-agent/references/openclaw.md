# OpenClaw agent

An OpenClaw agent is a workspace folder with Markdown files. The files load at the start of each session. There is no frontmatter and no tool list in the files.

## Contents

- Workspace files
- Limits
- Add an agent
- Open points

## Workspace files

| File | Purpose | Notes |
|------|---------|-------|
| `AGENTS.md` | Operating instructions and how to use memory. | Loads each session. Its `## Tools` section holds notes only. It does not turn tools on or off. |
| `SOUL.md` | Persona, tone, boundaries. | Loads each session. This is where the voice goes. |
| `USER.md` | Optional model of the user. | Own budget of 4,000 chars. |
| `IDENTITY.md` | Name, vibe, emoji. | Written in the first-run ritual. |
| `BOOTSTRAP.md` | One-time first-run ritual. | Delete it after the setup. |
| `BOOT.md` | Optional startup checklist. | Runs only when the `boot-md` hook is on. |
| `MEMORY.md` | Curated long-term memory. | Main private session only. |
| `memory/YYYY-MM-DD.md` | Daily log. | One file for each day. |
| `skills/` | Skills for this agent only. | Win over other skills with the same name. |

`TOOLS.md` and `HEARTBEAT.md` show as retired in the docs navigation. Do not create them without a check.

## Limits

Each file: 20,000 chars (`agents.defaults.bootstrapMaxChars`). All files together: 60,000 chars (`bootstrapTotalMaxChars`). OpenClaw cuts the rest. Put instructions in `AGENTS.md` and keep it short. Put the voice in `SOUL.md`.

## Add an agent

```
openclaw agents add work --workspace <dir> --model <id>
openclaw agents list --bindings
```

Per-agent config lives in `agents.entries.<id>` in `~/.openclaw/openclaw.json`: `workspace`, `agentDir`, `skills` (an explicit list replaces the default list). `bindings` route a channel account to an agent. Never share an `agentDir` between agents. The workspace is not a sandbox unless sandboxing is on.

## Open points

I could not read the pages for `tools`, `sandbox`, `bindings` syntax, and routing. Read `docs.openclaw.ai/concepts/multi-agent` and the configuration page before you write those keys. Check the folder with `agentcheck.py --runtime openclaw`.

Source: `docs.openclaw.ai/concepts/agent-workspace` and `.../multi-agent`.
