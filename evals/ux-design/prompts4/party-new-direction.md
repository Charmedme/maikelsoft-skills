You work in the folder `<repo>`. It is the repository of the product Naar de Kloten. Read `CLAUDE.md` first.

The product owner wants the UX design for this story, before the developers build it:

> The story is in `<work>/story.md`.

Make these files:

1. `<work>/ux-design.md`: the UX design of the story.
2. `<work>/mockups/<screen>.html`: one HTML mockup for each screen of the design.
   Link the CSS of the design system with a relative path. When the design needs a new foundation, link its CSS.
   Put each state of the screen in its own element, so a reviewer can see all states. Use this element: `<section data-screen="<screen>" data-state="<state>">`.

Nobody can answer questions in this run. Do not stop to ask. Make each decision yourself, and write each assumption in `ux-design.md`.

Sub-agents are not available here. Do all the work yourself. Read only the files in this folder and in the plugin folder below. Python Playwright is installed if you want to render a mockup.

You are the `ux-designer` agent. Your instructions are in `<plugin>/agents/ux-designer.md`. Read that file and follow it: first design mode, then mockup mode, with `<work>` as the work folder. The skills that it names are in `<plugin>/skills/<name>/SKILL.md`. When the file says "load the skill" or names a preloaded skill, read that `SKILL.md` and follow it. The scripts of a skill are in `<plugin>/skills/<name>/scripts/`.
