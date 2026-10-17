# Stack skill template

Each dev-stack skill has the same shape. The agents then find the same parts in each skill. This file is the contract for the authors of the skills.

## Folder

```
plugins/dev-stack/skills/<name>/
  SKILL.md                 required, under 300 lines (hard limit 500)
  rules.json               the rules for stackcheck.py
  references/
    decisions.md           the team decisions of this skill (catalog entries)
    review-checklist.md    the full review checklist
    <topic>.md             detail, one topic per file, a Contents list when over 100 lines
    <version>.md           rules for one major version, for example net11.md (only when needed)
    sources.md             the primary sources, with the date checked
    stack-rules.md         synced copy, do not edit
    language.md            synced copy, do not edit
  scripts/
    stackcheck.py          synced copy, do not edit
    doccheck.py, convert.py  synced copies, do not edit
```

`scripts/sync_shared.py` copies `stack-rules.md` and `stackcheck.py` into a skill when its `SKILL.md` names the file.

## Frontmatter

```yaml
---
name: <name>
description: <Type of task or output>. Use when <real situations, in the words of a user>. Not for <close cases>.
metadata:
  verified: "2026-10-16"
  versions: "<the versions that the facts are for, for example .NET 10, C# 14>"
---
```

- The description starts with the task. Third person. Under 1024 characters. Name the close cases that must not trigger the skill.
- `metadata.verified` is the date of the last fact check (stack rule 2).

## SKILL.md body

Use these sections in this order. Keep the headings, so the agents find them.

1. `# <Title>`: one or two sentences: what the skill covers and the line with the next skill (for example "Language rules only. Host and endpoints: `dev-stack:aspnetcore`.").
2. `## First`: "Read `references/stack-rules.md`." Then the version gate of this technology: which file shows the version, and which reference to read for which version.
3. `## Defaults for new code`: a short table: area, default, decision ID. Existing code keeps its style.
4. `## Rules`: the rules that change code. Numbered. Each rule has the reason in one sentence. Group by area. Only rules that a senior applies in a review.
5. `## Never write`: a table: the old, wrong, or invented code that an LLM writes, and the correct code. One line each.
6. `## Check`: the tool commands with the exact flags and what a pass looks like. Always the stackcheck command.
7. `## Review checklist`: the 10 to 15 most important checks, each marked `tool (<rule ID>)` or `judge`. The full list is in `references/review-checklist.md`.
8. `## References`: one line per reference file: when to read it.

## Writing rules

- Follow `plugins/dev-build/skills/build-skill/references/writing-rules.md` (W1 to W18, P1 to P5).
- Write in ASD-STE100 style: short sentences, active voice, one term for one item, no em-dash.
- Code must be correct for the version in `metadata.versions`. Compile each C# example with the .NET SDK 10.0.402 (`/root/.dotnet/dotnet`). Mark an example that you could not compile.
- Say what to do. Give the reason when it is not obvious.
- No copyrighted text. Paraphrase sources and link them in `references/sources.md`.
- Material from MIT or Apache-2.0 sources (for example github.com/dotnet/skills): paraphrase it, and add a line to `plugins/dev-stack/CREDITS.md`.

## references/decisions.md

One block per decision ID, in the order that a project meets them:

```markdown
## API-01. Minimal APIs or controllers

- Question: <one sentence for the human>
- Ask when: <the first change that needs it>
- Options: <option>: <one line>. <option>: <one line>.
- Recommended: <option>. <reason in one or two sentences>
- Choose another option when: <conditions>
- Depends on: <IDs or "none">
- Revisit when: <signal>
- Check: <tool and rule ID, or "judge">
- Detect: <rule ID in rules.json, or "none">
```

## rules.json

```json
{
  "skill": "<name>",
  "rules": [
    {"id": "CS-UTCNOW", "kind": "regex", "severity": "error",
     "files": ["**/*.cs"], "exclude": ["tests/**", "**/*Tests/**"],
     "pattern": "DateTime(Offset)?\\.UtcNow",
     "message": "Read the time from TimeProvider, so that tests can control it.",
     "fix": "Inject TimeProvider and call GetUtcNow().",
     "match": ["var now = DateTime.UtcNow;"],
     "nomatch": ["var now = timeProvider.GetUtcNow();"]},
    {"id": "PKG-MEDIATR", "kind": "package", "ecosystem": "nuget", "package": "MediatR*",
     "versions": "*", "severity": "error", "message": "MediatR is not allowed."},
    {"id": "PROPS-NULLABLE", "kind": "require", "files": ["Directory.Build.props"],
     "pattern": "<Nullable>enable</Nullable>", "severity": "warning", "message": "Turn on nullable for all projects."},
    {"id": "DET-API-01", "kind": "detect", "decision": "API-01",
     "files": ["**/*.cs"], "pattern": ":\\s*(Controller|ControllerBase)\\b"}
  ]
}
```

- `kind`: `regex` (a match is a finding), `require` (the file must contain the pattern), `package` (a package reference that matches), `detect` (code that uses a decision; no finding).
- Rule IDs: a short prefix of the skill plus a name, uppercase with hyphens.
- `severity`: `error` only when the rule has almost no false positives. Else `warning`.
- A rule that an analyzer already enforces does not go in `rules.json`. Name the analyzer rule in the checklist instead.
- Each `regex` and `detect` rule has `match` (bad lines that must match) and `nomatch` (good lines that must not match), at least one of each. `tests/test_stack_rules.py` checks them for each skill.
