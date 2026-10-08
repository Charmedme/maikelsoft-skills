---
name: doc-explanation
description: Explanation or concept documentation. Use when the user wants the why or the how-it-works of a system documented (architecture overview, design background, concepts, trade-offs), or wants such a document written, rewritten, or reviewed.
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/doccheck.py *)
---

# Explanation

An explanation helps the reader understand. It gives context, reasons, design decisions, trade-offs, and limits. The reader reads it away from the keyboard. It is descriptive text: no steps and no commands to do.

- Use it for: "how X works", "why we use X", architecture overviews, concepts.
- Not for: steps (`doc-howto`), lookup facts (`doc-reference`), one recorded decision (`doc-adr`).

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
# About <subject>        (or: How <subject> works)

<Two or three sentences: what the subject is and why it is important to the reader.>

## The problem
<What situation or need the subject answers.>

## How it works
<The main parts and how they connect. A Mermaid diagram helps when there are more than three parts.>

## Design decisions
<Each important choice, the alternative, and the reason. Link to the ADRs.>

## Limits
<What it does not do, and the known risks.>

## Related
- <Links to how-to guides, reference, and ADRs.>
~~~

## Rules for this type

- Give information gradually: first the whole, then the parts (STE rule 6.1).
- Start each paragraph with the key word or key phrase of its topic. One topic in each paragraph.
- Use one term for each concept. Define each term at its first use, in one short sentence.
- Use no imperative verbs. If the reader must do something, link to a how-to guide.
- Write the trade-offs and the known problems. An explanation that hides them is not useful.
