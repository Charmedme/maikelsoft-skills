# Run prompt

run.py gives this prompt to a new headless session for each run. The values in angle brackets come from `evals.json`.

## Common part

You work in the git repository `<repo>`. The human wants this:

> <request>

The human reads your messages. When you need an answer, a decision, or an approval from the human, end your turn with your message to the human. The next message that you get is the reply of the human. Never invent a reply of the human.

Sub-agents are not available here. Do all work yourself. Do not push to a remote. Do not change files outside the repository, except the data folder below.

## Variant: skill

Use the skill `setup` of the plugin `dev-setup`. Its folder is `<skill>`. In the skill text, `${CLAUDE_SKILL_DIR}` is `<skill>`, `${CLAUDE_PLUGIN_ROOT}` is `<plugin>`, and `${CLAUDE_PLUGIN_DATA}` is `<data>`. Read `<skill>/SKILL.md` and follow it.
