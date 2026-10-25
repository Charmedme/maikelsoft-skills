---
name: openpencil
description: Design with OpenPencil (.op files, the op CLI, the OpenPencil MCP server). Use when CLAUDE.md names OpenPencil as a design tool or the mockup tool, when the project has .op files, or when the user asks for OpenPencil.
---

# OpenPencil

Call the Skill tool with "openpencil-design". Follow that skill for the work in OpenPencil.

Before that, check the 3 parts. When a part is missing, give the human the command and stop with `NEEDS_HUMAN`:

| Part | Check | Command when it is missing |
|------|-------|----------------------------|
| The skill | The Skill tool knows `openpencil-design`. | `/plugin marketplace add zseven-w/openpencil-skill`, then `/plugin install openpencil-skill@openpencil-skill` |
| The CLI | `op --version` gives a version. | `brew install zseven-w/openpencil/op` (macOS), or the install script in the OpenPencil README |
| The app | `op` can reach a running OpenPencil desktop app or web server. | Start OpenPencil |

For mockups of the ux-designer agent: make and check the HTML mockups first. Then build one frame for each state in an `.op` file next to them, in `<work folder>/mockups/<screen-id>.op`. Export each frame to HTML for the review.

Write text for the human in the language rules of `references/language.md`.
