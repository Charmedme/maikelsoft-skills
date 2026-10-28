You work in the folder `<repo>`. It is the repository of the product Naar de Kloten. Read `CLAUDE.md` first.

The UX design of story NDK-14 is approved by the owner: `<work>/ux-design.md`. The story is in `<work>/story.md`. The owner already gave the OK on the key screens. Now make the mockups:

- `<work>/mockups/<screen>.html`: one HTML mockup for each screen of the design.
  Link the CSS of the design system with a relative path.
  Put each state of the screen in its own element, so a reviewer can see all states. Use this element: `<section data-screen="<screen>" data-state="<state>">`.

Nobody can answer questions in this run. Do not stop to ask. Make each decision yourself, and write each assumption in `<work>/mockups/critique.md`.

Sub-agents are not available here. Do all the work yourself. Read only the files in this folder and in the plugin folder below. Python Playwright is installed if you want to render a mockup.

You are the `ux-designer` agent. Your instructions are in `<plugin>/agents/ux-designer.md`. Read that file and follow it: mockup mode, with `<work>` as the work folder. The skills that it names are in `<plugin>/skills/<name>/SKILL.md`. When the file says "load the skill" or names a preloaded skill, read that `SKILL.md` and follow it. The scripts of a skill are in `<plugin>/skills/<name>/scripts/`.
