# Run prompt

run.py gives this prompt to a new headless session for each run. The values in angle brackets come from `evals.json`.

## Common part

You work in the git repository `<repo>`. The human is the product owner. The human wants this:

> <request>

The human reads your messages. When you need an answer, a decision, or an approval from the human, end your turn with your message to the human. The next message that you get is the reply of the human. Never invent a reply of the human.

Sub-agents are not available here. Do all work yourself. Do not push to a remote. Do not change files outside the repository.

## Variant: skill

Use the skill `<name>` of the plugin `product-flow`. Its folder is `<skill>`. The other skills of the plugin are in `<plugin>/skills/`. In the skill text, `${CLAUDE_SKILL_DIR}` is the folder of the skill, and `${CLAUDE_PLUGIN_ROOT}` is `<plugin>`. Read `<skill>/SKILL.md` and follow it. When it starts another skill of the plugin, read that `SKILL.md` and follow it.
