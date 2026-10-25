---
name: openpencil
description: Design with OpenPencil (.op files, the op CLI, the OpenPencil MCP server) - put checked HTML mockups into OpenPencil with one frame for each state, then edit them with the openpencil-design skill. Use when CLAUDE.md names OpenPencil as a design tool or the mockup tool, when the project has .op files, or when the user asks for OpenPencil.
---

# OpenPencil

For the work in OpenPencil, call the Skill tool with "openpencil-design" and follow it. This skill adds the import of the checked HTML mockups of the ux-designer agent.

Write text for the human in the language rules of `references/language.md`.

## Before you start

Check the 3 parts. When a part is missing, give the human the command and stop with `NEEDS_HUMAN`.

| Part | Check | Command when it is missing |
|------|-------|----------------------------|
| The skill | The Skill tool knows `openpencil-design`. | `/plugin marketplace add zseven-w/openpencil-skill`, then `/plugin install openpencil-skill@openpencil-skill`. Or `op install`. |
| The CLI | `op --version` gives a version. | `brew install zseven-w/openpencil/op` (macOS), or the install script in the OpenPencil README (Linux) |
| The app | `op status` says `"running": true`. | Start the app, or `op start --headless --file <work folder>/mockups/<story>.op` |

## Mockups

1. **HTML first.** Make and check the HTML mockups as the mockup mode of the agent says. Done when `uxcheck.py mockup` shows 0 errors and the critique is written.
2. **Import.** Run `python3 scripts/opimport.py <work folder>/mockups/*.html --out <work folder>/mockups/openpencil`. For each state, it measures the rendered mockup in a browser and imports it with `op import:snapshot`. It names each frame `<screen> / <state>` and exports a PNG of each frame. Done when the script exits with 0.
3. **Look.** Compare 2 or 3 frame PNGs with the screenshots of the HTML mockups. The import loses checkboxes and radio buttons, `text-transform`, and the text inside an input. When a lost part matters for a state, add it back with the tools of `openpencil-design`. Done when the frames match the mockups, or each difference is listed.
4. **Save.** Save the document (`op save_document filePath=<work folder>/mockups/<story>.op`). Write the file name and the list of frames in `<work folder>/mockups/openpencil.md`. Done when the `.op` file and the list exist.

Do not use `op import:html` for these mockups. It cannot measure CSS tables and wrapped rows, so the columns of a table overlap.

## New designs in OpenPencil

When the human wants a design made in OpenPencil, not imported, follow `openpencil-design`. Use the tokens of the design-system source of `CLAUDE.md` as OpenPencil variables (`op vars:set`). Then check the result as a mockup: export the frames, and look at them as step 3 says.
