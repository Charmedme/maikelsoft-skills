---
name: ux-setup
description: Set up a project for UX work - finds the design tools, the design-system source, the brand files, the languages, and the run command, asks the human one round of questions, and writes the "## Design" section of CLAUDE.md that the ux-designer agent reads. Use when the ux-designer agent returns NEEDS_HUMAN because the "## Design" section is missing, when the user wants to set up or change the design tools of a project, or before the first design of a project.
---

# UX setup

The `ux-designer` agent reads the `## Design` section of `CLAUDE.md` first in each mode. This skill writes that section. Run it in the main session, because a subagent cannot ask the human.

## Steps

1. **Scan.** Run `python3 scripts/setupscan.py <project folder>`. When the session has the Artifact tool, also list the Claude Design Systems of the account (`list` with type "Design System"). Done when you have the facts.
2. **Draft.** Make the answer for each line of the section from the facts (`references/section.md` has the lines and the rules). Mark each line `found` (a fact) or `guess`. Done when each line has a draft value.
3. **Ask.** Ask one round of questions. Ask only for the `guess` lines and for the facts that a scan cannot find. Examples are the Figma plan and seat, and the Claude Design System of this product. Give your recommendation as the first answer. Use the question tool when the session has it. Max 4 questions. Done when each line has an answer, or the human said "use your choice".
4. **Write.** Write the section in `CLAUDE.md`. Keep the other sections. When a `## Design` section exists, change only the lines that the human changed, and show the difference. Done when the section is in `CLAUDE.md`.
5. **Tell.** Give each tool that the project names but the session does not have, with its install command (`references/section.md`). After a plugin install, tell the human to run `/reload-plugins`, or to start a new session. Until then, the `ux-designer` agent type is not available. Done when the reply lists the missing tools, or says "none". A reply with a plugin install also names `/reload-plugins`.

## Rules

- The mockup tool is the tool that the team reviews in. Recommend Claude Design when the product has a Claude Design System. Else recommend Figma when the team has a Dev or Full seat. Else recommend HTML.
- Do not guess a plan or a seat. Ask, or write `unknown`. The Figma skill uses the seat for its call budget.
- One design-system source is the master. Name it first. The other sources follow it.
- Do not install tools or plugins. Give the commands.

## Files

| File | Read when |
|------|-----------|
| `references/section.md` | Steps 2, 4, and 5: the lines of the section, the recommendation rules, and the install commands. |
| `references/language.md` | Before you write the section, a question, or text for the human. |
