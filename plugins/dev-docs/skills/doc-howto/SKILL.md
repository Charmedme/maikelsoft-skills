---
name: doc-howto
description: How-to guide and runbook. Use when the user wants steps that do one task (install, configure, deploy, migrate, rotate a key, recover from an incident), or wants a how-to guide or runbook written, rewritten, or reviewed.
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/doccheck.py *)
---

# How-to guide

A how-to guide helps a reader who knows the basics to do one task. It is procedural text: the reader acts, step by step. A runbook is a how-to guide for operations.

- Use it for: one task with a clear end state.
- Not for: a lesson for a new user (`doc-tutorial`), facts to look up (`doc-reference`), the reason for a design (`doc-explanation`).

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

~~~markdown
# <Verb> <object>            (example: "Rotate the API key")

<One sentence: what the reader gets at the end.>

## Before you start
- <Prerequisite: tool and version, access, permission, data.>

## Steps
1. <One action. Condition first: "If X, do Y.">
   ```sh
   <command>
   ```
   <Expected result, when the reader must see it.>

## Make sure that it works
<A command or a check with its expected result.>

## Roll back                  (required when a step changes data or production)
1. <Step>

## Troubleshooting           (optional)
| Problem | Cause | Fix |

## Related
- <Links to reference and explanation.>
~~~

Runbook: add `## When to use this runbook` (the alert or the symptom) at the top, and `## Escalate` (who, when) at the end.

## Rules for this type

- The title starts with a verb and names the task.
- One action in each step. Two actions only when the reader does them at the same time.
- Put a warning before the step that it is about: command or condition first, then the risk. "Do not run this on production. The command deletes all rows."
- Give each command in a code block that the reader can copy. Use placeholders in `<angle-brackets>` and say what goes in each one.
- Keep explanations to one sentence. Link to `doc-explanation` content for more.
