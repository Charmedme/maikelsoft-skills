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
    agent.md           optional: tools and scaffold of a simulated-human case
    graders/*.md       assertions that a grader scores
    checks.json        assertions that a command scores
    case.yaml          one-turn case only: the scaffold script
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

An `llm` grader with `focus: trace` sees only the first 12 and the last 12 messages. A file that the agent writes in the middle of a long run is not in that view. With `focus: files`, the judge sees only the file names. To grade the content of a file, give its path in `focus`. When the path is not fixed (for example a date in the name), use a check.

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

`agent.md` sets the tools and the scaffold of the case. The default tools are Read, Write, Edit, Glob, Grep, and Skill. Add Bash only when the skill needs a shell:

```markdown
---
scaffold: fixture.sh
tools: Read Write Edit Glob Grep Skill Bash
---
```

With Bash, the agent runs in the OS sandbox of Claude Code: each command can write only in the work folder. On Linux, the sandbox needs `bubblewrap` and `socat`. Do not put a `case.yaml` in this kind of case, because `claude plugin eval` then loads it as a one-turn case.

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

`case.yaml` of a one-turn case:

```yaml
schema_version: "1.1"
name: release-absent
context:
  scaffold_script: fixture.sh
```

`fixture.sh` starts in the empty work folder. `evalrun.py` runs a copy of the cases, and writes the repository path to `evals/.repo-root` in the copy. The script reads that file first:

```bash
#!/usr/bin/env bash
set -euo pipefail
repo="$(cat "$(dirname "$0")/../.repo-root" 2>/dev/null || (cd "$(dirname "$0")/../../../.." && pwd))"
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
