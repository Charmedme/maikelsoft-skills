---
name: doc-adr
description: Architecture decision record (ADR) in MADR 4.0.0 format. Use when the user wants to record, compare, or review a technical decision (choose between technologies, patterns, or designs), or wants an ADR written, rewritten, or reviewed.
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/doccheck.py *)
---

# Architecture decision record

An ADR records one decision: the context, the options, the chosen option, and the consequences. The format is MADR 4.0.0 (https://adr.github.io/madr/). An accepted ADR does not change. A new ADR supersedes it.

- Use it for: one decision with real alternatives.
- Not for: a system overview (`doc-explanation`), a task list (use the issue tracker).

<!-- BEGIN shared/workflow.md -->
## Modes

- **Write**: make a new document.
- **Rewrite**: improve an existing document. Keep each fact, code sample, command, link, and heading anchor. Change the structure and the language.
- **Review**: report the findings. Change nothing.

## Steps

1. **Read the language rules.** Read `references/language.md` in this skill folder. Done when you know the language, the level, and (Dutch) "je" or "u". If one is not clear, ask the human before you continue.
2. **Collect the facts.** Read the request, the code, and the existing docs. Done when each section of the template below has its facts. A section with a missing fact has a question for the human or a `TODO:`.
3. **Find the source errors.** Rewrite and review mode. Look for commands that cannot work and tools that are old. Also look for two names for one item, and facts that do not agree with the code. Done when you have a list of source errors. Do not fix a source error silently, and do not copy it silently.
4. **Make the outline.** Use the template below. Give each part of the content one document type. If content of a different type has its own document, move the content there and add a link. If not, keep the content in its own section, and suggest the new document in the report. A rewrite never deletes content. Done when each heading has one purpose.
5. **Write.** Write or rewrite mode only. In a rewrite, keep each heading that other pages can link to. If a heading must change, list the old anchor in the report. An explicit ID (`{#old-anchor}`) keeps the anchor on kramdown, MkDocs, and Hugo sites, but not on GitHub. Done when each section of the outline has its text.
6. **Check.** Run `python3 ${CLAUDE_SKILL_DIR}/scripts/doccheck.py --level <level> <file>`. In rewrite mode, add `--source <original file>`. Outside Claude Code, use the path of the `scripts` folder of this skill. Done when the checker shows 0 errors, and you fixed each warning or gave a reason to keep it.
7. **Report.** Give the file path, the checker summary line, the open questions, the `TODO:` items, and the source errors. In rewrite mode, also give the main changes and the changed anchors. In review mode, give each finding as: location, rule (a checker rule ID, an STE section, or "Source error"), problem, fix.
<!-- END shared/workflow.md -->

## Template

File: `docs/decisions/NNNN-<title-with-dashes>.md`. Use the next free number. A path that the human gives, or the ADR folder of the repo, comes first.

~~~markdown
---
status: proposed            # proposed | accepted | rejected | deprecated | superseded by ADR-NNNN
date: YYYY-MM-DD
decision-makers: <names>
consulted: <names>          # optional
informed: <names>           # optional
---

# <Short title: the problem and the solution>

## Context and Problem Statement
<Two or three sentences. You can write the problem as a question.>

## Decision Drivers            (optional)
- <Force or concern.>

## Considered Options
- <Option 1>
- <Option 2>

## Decision Outcome
Chosen option: "<option>", because <reason, linked to the decision drivers>.

### Consequences
- Good, because <positive result>.
- Bad, because <negative result>.

### Confirmation               (optional)
<How the team makes sure that the implementation follows the decision: a review, a test, a fitness function.>

## Pros and Cons of the Options   (optional)

### <Option 1>
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low / Medium / High |
| Cost | |
| Scalability | |
| Team familiarity | |

- Good, because <argument>.
- Neutral, because <argument>.
- Bad, because <argument>.

## More Information             (optional)
<Links, the date to revisit the decision, related ADRs.>
~~~

## Rules for this type

- One decision in each ADR.
- Give at least two real options. "Do nothing" is a valid option.
- Base the "because" on the decision drivers, not on taste. Never choose an option because the team already uses it, unless familiarity is a decision driver that you state.
- The human makes the decision. If the human did not choose, write `status: proposed` and give your recommendation in "More Information".
- Put action items in the issue tracker, not in the ADR.
- In the table of an option, use only the dimensions that have facts. Do not invent a rating. Remove the "(optional)" markers from the output.
