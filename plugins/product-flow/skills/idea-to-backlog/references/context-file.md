# The backlog context file

## Contents

- Why one file
- Template
- Where to keep it

## Why one file

The context file holds the full run. It has the request, the sources, the problem, the decisions, the open questions, the prototype verdict, the spec, and all backlog items in full detail. A later run reads it and adds to it. It does not start again from zero. `backlog-push` can push straight from it.

Name it `backlog-<slug>.md`. The slug is 2 or 3 words of the subject, for example `backlog-product-search.md`.

## Template

```markdown
# Backlog: <subject>

## Status

- Phase: <scan | grill | prototype | spec | backlog | push | done>
- Levels: <2 (Feature, User Story) | 3 (Epic, Feature, User Story)>
- Profile: <Agile | Scrum | SAFe>
- Language: <English | Dutch, je | Dutch, u>
- Template: <team profile: path | template of the ticket system | plugin default>
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

## Problem

- Current work: <how the users do the task today>
- Where it goes wrong: <...>
- Exceptions: <...>
- Groups that work differently: <...>, and the groups that do not need this
- Success: <the number and its value today, or a reference to an open question>

## Decisions

| No | Decision | By | Date | Source |
|----|----------|----|------|--------|
| Q1 | <question in short>: <answer> | human / recommended, accepted / assumed | <date> | <document, prototype, or "human"> |

## Open questions

| No | Question | Owner | Status | Waits |
|----|----------|-------|--------|-------|
| O1 | <question> | customer / PO / team / expert / human | open / answered: <Q-number> | <the items that wait for the answer> |

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

An item refers to an open question by its number: "Open question: O3". The question itself is only in "Open questions". When the answer comes, change the status to "answered", add the decision, and update the items.

When the scope changes, add a decision for it. When the subject changes, give the file a new name and say so in the "Status" section.

## Where to keep it

Save it in the first place of this list that you can reach:

1. **The claude.ai Project of the session.** Write it to the Project with the Projects tool, so that each chat in the Project can read it.
2. **The repository.** Use the place for plans that the project guide gives. Else `docs/work/<slug>/`.
3. **A download.** Give the file to the human, and ask them to attach it when they continue the run.

Tell the human in one line where the file is.
