You work in the repository <repo>. The story is in `<work>/story.md`. The approved UX design is in `<work>/ux-design.md`, and the approved mockups are in `<work>/mockups/`. The project facts are in `CLAUDE.md`, `PRODUCT.md`, and `DESIGN.md`. The design system is in `design-system/`.

Task: make a clickable prototype for a test with real users. It must answer the most important open question of the design. It must show the states that the question needs, and it must be ready for a moderated test session (tasks for the participants).

Write the result in `<work>/prototype/`:

1. `prototype.html`: one self-contained HTML file that opens offline with a double-click.
2. `test-plan.md`: the question, what you compare, the tasks, and the participants.

You cannot ask questions. When you must choose, take the most reasonable answer and write it in `test-plan.md`. Python Playwright is installed if you want to render the prototype. Do not change files outside `<work>/prototype/`.
