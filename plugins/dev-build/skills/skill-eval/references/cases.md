# Eval cases

Read this file before you write or move a case.

## Contents

- Folder layout
- One-turn case
- Case with a simulated human
- Checks
- Fixtures
- Move an old eval set

## Folder layout

```
plugins/<plugin>/evals/
  <case>/
    prompt.md          one-turn case (claude plugin eval)
    request.md         or: the first message of a case with a simulated human
    human.md           the answer sheet of the simulated human
    agent.md           optional: the tools of the agent in a simulated-human case
    graders/*.md       assertions that a grader scores
    checks.json        assertions that a command scores
    case.yaml          optional: the scaffold script
  results/             results of each run (do not commit)
```

Give each case a name that tells the task, for example `create-absent-human`. Do not put a variant name in it.

## One-turn case

`prompt.md` has frontmatter and the request:

```markdown
---
max_turns: 30
allowed_tools: [Read, Write, Edit, Glob, Grep, Skill]
---
Make a skill that checks supplier invoices for missing fields. Put it in skills/invoice-check. Nobody can answer questions. Decide yourself and say what you decided.
```

Each file in `graders/` is one assertion. The file name is the assertion ID.

| Type | Frontmatter | Passes when |
|------|-------------|-------------|
| `regex` | `pattern`, `match` (`not_contains` or `count:N`), `flags`, `target` | the pattern is in the target |
| `file_exists` | `path` (a glob) | the run made a file that matches |
| `tool_used` | `tool`, `input_match`, `min`, `max` | the tool calls are in range |
| `llm` | `focus` (optional), body = the pass condition | a judge model says yes |

`target` and `focus` take `last_message` (default), `trace`, `files`, or `{ source: file, path: <path> }` for the content of one file.

A good `llm` grader names one thing to see: "The final report lists each decision the agent made for the human." A bad one asks for quality: "The skill is good."

A regex sees the words of the reply. A reply that says "there is no X" also contains X. Test each pattern on a real reply.

## Case with a simulated human

Use this form when the skill asks the human questions. `request.md` holds the first message. `human.md` holds the answer sheet:

```markdown
# Simulated human: monday-mail

## Hidden facts
Give a fact only when a question touches its topic.
- H1 Input: "Each Monday I export declarations.csv with the columns MRN, status, office."
- H2 Limit: "The skill must never send the mail."

## Gates
- A summary with a request for approval: "Yes, build it."
- A request to publish: "Not now, stop here."
```

`agent.md` sets the tools of the agent. The default is Read, Write, Edit, Glob, Grep, and Skill. Add a shell command only when the skill needs it:

```markdown
---
tools: Read Write Edit Glob Grep Skill
allow: Bash(python3 *)
---
```

The agent runs without settings, skills, or project text of this computer. It sees only the plugin under test.

## Checks

`checks.json` lists commands. A command that exits 0 is a pass. Use it for the Python checkers of the repository.

```json
[
  {"id": "C1", "cmd": "python3 -I {repo}/plugins/dev-build/skills/build-skill/scripts/skillcheck.py {cwd}/skills/invoice-check"},
  {"id": "C2", "cmd": "! grep -rqP '\\x{2014}' {cwd}/skills/invoice-check"}
]
```

Placeholders:

- `{cwd}`: the copy of the work folder of the run.
- `{repo}`: the repository root.
- `{case}`: the case folder.
- `{run}`: the run folder, with `transcript.md` or `trace.jsonl`.

Run the checker from `{repo}`, not from the output. The output can contain code that the agent wrote.

## Fixtures

A case that needs files at the start uses a scaffold script. Large fixtures stay in the root `evals/<plugin>/fixtures/` folder of the repository, so they do not go to the users with the plugin.

`case.yaml`:

```yaml
context:
  scaffold_script: fixture.sh
```

`fixture.sh` starts in the empty work folder. It finds the repository from its own real path, because the runner links the cases into a copy of the plugin:

```bash
#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$(readlink -f "$0")")/../../../.." && pwd)"
cp -r "$repo/evals/dev-build/fixtures/bad-skill" ./skill
```

Run with `--scaffold`. The script runs as you, so use it only for cases that you wrote.

## Move an old eval set

An old set is an `evals.json` with a `score.py`. Move each eval to one case folder:

1. `request` goes to `prompt.md`, or to `request.md` when a human sheet exists.
2. Each `judge` assertion goes to `graders/<id>.md` with `type: llm`.
3. Each `check` assertion goes to a regex grader when a pattern can decide it. Else it goes to `checks.json` with a checker command.
4. Each fixture goes to a scaffold script.
5. Run the new case once against the old results. When a result is very different, find the cause before you remove the old set.

Keep the old result files. They are the history of the plugin.
