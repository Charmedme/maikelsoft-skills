---
name: update-docs
description: Check and update of the whole doc set of a project - an inventory, a plan or an audit with one type for each page, the approval of the human, and then each document written or fixed by the correct doc- skill. Use when the user wants a project or repository documented, asks which docs are missing or out of date, or wants an existing doc set checked and updated. Not for the docs of one change during a build, which the build updates itself. Writes in plain language: ASD-STE100 for English, B1 for Dutch.
argument-hint: "[folder or doc set]"
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/doccheck.py *)
---

# Update the docs

This skill checks the whole doc set of a project and brings it up to date. First it makes a plan: which documents the project needs, of which type, in which file, and which skill writes each one. The human approves the plan. Then the doc- skills write or fix each document.

## Modes

Select the mode from the repository. Do not ask.

- **Plan**: the project has few or no docs (no page other than a README). Make a plan for a new doc set.
- **Audit**: the project has a doc set. Classify each page as one type. Find the pages that mix types, the pages that do not agree with the code, the missing types, and the duplicate content. Grade each page (step 4).

## Steps

1. **Read the language rules.** Read `references/language.md` in this skill folder. Ask the format question one time for all documents, in the same message as the other questions. Done when you know the format, the language of the docs, and (Dutch) "je" or "u", or you asked.
2. **Make an inventory.** Read the repo: the existing docs and their folders, the public API, the CLI, and the configuration. Also read the build and deploy files, and find the readers (users, developers, operators). With `$ARGUMENTS`, limit the inventory to that folder or doc set. Done when you can name each reader and each main task.
3. **Classify.** For each need, select one type with two questions: Does the reader act or learn the facts? Is the reader learning or working?

   | | Acting | Knowing |
   |---|---|---|
   | **Learning** | tutorial (`doc-tutorial`) | explanation (`doc-explanation`) |
   | **Working** | how-to guide (`doc-howto`) | reference (`doc-reference`) |

   Other types: `doc-readme` (entry page), `doc-adr` (one decision), `doc-changelog` (versions), `doc-code-comments` (comments in code), `doc-ui-text` (product text).
   More page types go to these skills: a troubleshooting page and a migration guide to `doc-howto`, a glossary to `doc-reference`. List `CONTRIBUTING.md` and `AGENTS.md` when they are missing, but do not write them. They are out of scope.
   In audit mode, give each page one type, and mark each page that has content of more than one type.
4. **Make the plan.** Give a table with one row for each document. The columns are: file path, type, skill, purpose, source of the facts, format, and status (new, rewrite, update, keep). The format is the answer to the format question. A row for `doc-code-comments`, `CHANGELOG.md`, or `README.md` always has Markdown or the source file. Use the folder layout of the repo. If there is none, use `docs/tutorials/`, `docs/how-to/`, `docs/reference/`, `docs/explanation/`, and `docs/decisions/`. In audit mode, add four columns:
   - **Audience**: the reader of the page.
   - **ROT**: redundant (another page has the same content), outdated (it does not agree with the code), trivial (no reader needs it), or none.
   - **Grade**: A, C, or F. Ask four questions: Does the page have one type? Do the facts agree with the code? Do the links work? Does it follow the template of its type? A has four yes answers, C has one or two no answers, F has more.
   - **Last change**: the date of the last commit of the page.

   An outdated page gets the status update, with the facts that changed. Propose to delete or merge each redundant or trivial page. A small set of correct pages is better than a large set with errors. Done when each reader task has a document and each document has one type.
5. **Get approval.** Show the plan and stop. Write nothing until the human approves it, or changes it.
6. **Run the plan.** After approval, give each skill the format of its row. Use the skills in this order: reference, how-to, tutorial, explanation, ADR, README last (it links to the others). Done when each row has its file and the checker shows 0 errors for each file.
7. **Report.** Give the files that changed, the checker result of each file, and the rows that wait. End with one line: `Next: review the changed docs and commit them.` Done when the human has the report.
