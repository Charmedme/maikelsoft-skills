# The backlog context file

## Contents

- Why one file
- Template
- Where to keep it

## Why one file

The context file holds the full run. It has the request, the sources, the decisions, the prototype verdict, the spec, and all backlog items in full detail. A later run reads it and adds to it. It does not start again from zero. `backlog-push` can push straight from it.

Name it `backlog-<slug>.md`. The slug is 2 or 3 words of the subject, for example `backlog-product-search.md`.

## Template

```markdown
# Backlog: <subject>

## Status

- Phase: <scan | grill | prototype | spec | backlog | push | done>
- Levels: <2 (Feature, User Story) | 3 (Epic, Feature, User Story)>
- Profile: <Agile | Scrum | SAFe>
- Language: <English | Dutch, je | Dutch, u>
- G1 Shared understanding: <open | approved | assumed> <date, one line of reason>
- G2 Prototype: <open | approved | approved with changes | rejected> <date>
- G3 Backlog review: <open | approved> <date>
- G4 Push: <open | approved> <date>
- Push log: <path>

## Request

<The request of the human, in their words.>

## Sources

| Source | What it gave | Link |
|--------|--------------|------|

## Decisions

| No | Decision | By | Date |
|----|----------|----|------|
| Q1 | <question in short>: <answer> | human / recommended, accepted / assumed | <date> |

## Prototype

- File: <path or link>
- Question: <the question of the prototype>
- Verdict: <yes | yes with changes | no>. Changes: <...>

## Spec

<Goal, users, scope, and story map. See spec.md.>

## Backlog

<The items, as the backlog-* skills write them: each item a "##" heading with "**Type:**" and "**Parent:**".>
```

Start the "Backlog" section with a line that says it holds the items. Each item heading uses `##` or `###`, so that `backlogtree.py` finds it.

## Where to keep it

Save it in the first place of this list that you can reach:

1. **The claude.ai Project of the session.** Write it to the Project with the Projects tool, so that each chat in the Project can read it.
2. **The repository.** Use the place for plans that the project guide gives. Else `docs/work/<slug>/`.
3. **A download.** Give the file to the human, and ask them to attach it when they continue the run.

Tell the human in one line where the file is.
