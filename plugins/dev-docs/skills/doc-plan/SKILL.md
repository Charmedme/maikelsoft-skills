---
name: doc-plan
description: Documentation plan and documentation audit for a whole project. Use when the user wants a project or repository documented, asks which docs are missing, or wants an existing doc set checked for structure. Selects the document types and sends each document to the correct doc- skill.
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/doccheck.py *)
---

# Documentation plan

This skill does not write documents. It makes a plan: which documents the project needs, of which type, in which file, and which skill writes each one. The human approves the plan before you write a document.

## Modes

- **Plan**: make a plan for a project with few or no docs.
- **Audit**: check an existing doc set. Classify each page as one type. Find the pages that mix types. Find the missing types and the duplicate content.

## Steps

1. **Read the language rules.** Read `references/language.md` in this skill folder. Ask the format question one time for all documents, in the same message as the other questions. Done when you know the format, the language of the docs, and (Dutch) "je" or "u", or you asked.
2. **Make an inventory.** Read the repo: the existing docs and their folders, the public API, the CLI, and the configuration. Also read the build and deploy files, and find the readers (users, developers, operators). Done when you can name each reader and each main task.
3. **Classify.** For each need, select one type with two questions: Does the reader act or learn the facts? Is the reader learning or working?

   | | Acting | Knowing |
   |---|---|---|
   | **Learning** | tutorial (`doc-tutorial`) | explanation (`doc-explanation`) |
   | **Working** | how-to guide (`doc-howto`) | reference (`doc-reference`) |

   Other types: `doc-readme` (entry page), `doc-adr` (one decision), `doc-changelog` (versions), `doc-code-comments` (comments in code), `doc-ui-text` (product text).
   In audit mode, give each page one type, and mark each page that has content of more than one type.
4. **Make the plan.** Give a table with one row for each document. The columns are: file path, type, skill, purpose, source of the facts, format, and status (new, rewrite, keep). The format is the answer to the format question. A row for `doc-code-comments`, `CHANGELOG.md`, or `README.md` always has Markdown or the source file. Use the folder layout of the repo. If there is none, use `docs/tutorials/`, `docs/how-to/`, `docs/reference/`, `docs/explanation/`, and `docs/decisions/`. Done when each reader task has a document and each document has one type.
5. **Get approval.** Show the plan and stop. Write nothing until the human approves it, or changes it.
6. **Run the plan.** After approval, give each skill the format of its row. Use the skills in this order: reference, how-to, tutorial, explanation, ADR, README last (it links to the others). Done when each row has its file and the checker shows 0 errors for each file.
