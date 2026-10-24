# Research (medium and large tier)

Learn the technical side of the item before you ask the human a question. A good question needs the facts first. The item gives the product side: do not research the need, the users, or the scope again.

## Steps

1. **List the questions.** Write down what you must know to plan the work. Use these views:
   - **The code:** the modules that the change touches, the current behaviour, the tests, the test seams.
   - **The project:** the project guide, the glossary (for example `CONTEXT.md`), the decision records, the conventions for branches and folders.
   - **The domain:** the terms and the technical standards that the item uses (for example a file format or a protocol).
   - **The tools:** the libraries and the APIs that the work uses, and their current versions.
   - **Prior art:** how other code in the project, or a primary source, solves the same technical problem.
2. **Find the answers.** Use sub-agents when you have them, one for each view, so that they work at the same time. Use primary sources: the code, the official docs, the specifications. Follow each claim to the source that owns it.
3. **Write `research.md`.** Use `templates/research.md`. Give the source of each fact. Mark each fact that you cannot confirm as `UNCONFIRMED:`.

Done when each question of step 1 has an answer with a source, or a mark. Do not write the plan here. Do not make decisions here. A finding that conflicts with the item is a product question (`po-questions.md`).
