---
name: ux-setup
description: Set up a project for UX work - finds the design tools, the design-system source, the brand files, the languages, and the run command, asks the human one round of questions, and writes the "## Design" section of .claude/rules/delivery.md that the ux-design commands and the ux-designer agent read. Moves an older "## Design" section out of CLAUDE.md. Use when /setup, /ux-design, or /ux-review needs the design facts, when the ux-designer agent returns NEEDS_HUMAN because the "## Design" section is missing, or when the user wants to set up or change the design tools of a project.
user-invocable: false
---

# UX setup

The commands `/ux-design` and `/ux-review` and the `ux-designer` agent read the `## Design` section of `.claude/rules/delivery.md`. This skill writes that section. It is the setup step of this plugin. `/setup` of the dev-setup plugin starts it for the design questions, and the ux-design commands start it when the section is missing. Run it in the main session, because a subagent cannot ask the human.

## Steps

1. **Scan.** Run `python3 scripts/setupscan.py <project folder>`. When the session has the Artifact tool, also list the Claude Design Systems of the account (`list` with type "Design System"). Done when you have the facts.
2. **Draft.** Make the answer for each line of the section from the facts (`references/section.md` has the lines and the rules). Mark each line `found` (a fact) or `guess`. Done when each line has a draft value.
3. **Ask.** Ask one round of questions. Ask only for the `guess` lines and for the facts that a scan cannot find. Examples are the Figma plan and seat, and the Claude Design System of this product. Give your recommendation as the first answer. Use the question tool when the session has it. Max 4 questions. Done when each line has an answer, or the human said "use your choice".
4. **Write.** Write the section in `.claude/rules/delivery.md` at the root of the repository. Keep the other sections. Make the file when it does not exist (`references/section.md`). When a `## Design` section exists, change only the lines that the human changed, and show the difference. Done when the section is in `.claude/rules/delivery.md`.
5. **Move.** When `design_section_file` of the scan is `CLAUDE.md`, the project has the section of an older version. Step 4 copies its lines into `.claude/rules/delivery.md`. Now ask the human for the OK to remove the section from `CLAUDE.md`. After the OK, remove it. Claude Code loads `.claude/rules/` at each session start, and the team keeps the facts of all plugins in one file. When nobody can answer, keep `CLAUDE.md` as it is, and say so. Done when `CLAUDE.md` has no `## Design` section, or the reply says why it stays.
6. **Tell.** Give each tool that the project names but the session does not have, with its install command (`references/section.md`). After a plugin install, tell the human to run `/reload-plugins`, or to start a new session. Until then, the `ux-designer` agent type is not available. When the session has the dev-setup plugin, say one time: "`/setup` asks the setup questions of all plugins." Done when the reply lists the missing tools, or says "none". A reply with a plugin install also names `/reload-plugins`.

## Rules

- The mockup tool is the tool that the team reviews in. Recommend Claude Design when the product has a Claude Design System. Else recommend Figma when the team has a Dev or Full seat. Else recommend HTML.
- Do not guess a plan or a seat. Ask, or write `unknown`. The Figma skill uses the seat for its call budget.
- One design-system source is the master. Name it first. The other sources follow it.
- Do not install tools or plugins. Give the commands.

## Files

| File | Read when |
|------|-----------|
| `references/section.md` | Steps 2, 4, and 6: the lines of the section, the recommendation rules, the new file, and the install commands. |
| `references/language.md` | Before you write the section, a question, or text for the human. |
