# Phase 4: Prototype

A prototype is throwaway code that answers one question. Let the human see and use the result before you write the real code.

## Select the question

Take the riskiest open point of the specs: the point that is most expensive when it is wrong. Write the question in one sentence at the top of the prototype. Then select the type:

- **Logic**: "Does this behaviour, state, or data model work?" Go to "Logic prototype".
- **UI**: "What must this look like?" Go to "UI prototype".

Some work has no behaviour and no screen, for example a pure refactor. Then show the new interface or the new data shape in a small script that runs. Ask the same gate question.

## Rules for each prototype

- **Mark it.** Put it in `<work folder>/prototype/`. Put "PROTOTYPE" in the title.
- **Easy to start.** One command, or one HTML file that opens with a double-click. Give the command or the file to the human.
- **No storage.** Keep the state in memory, unless the question is about storage.
- **No polish.** No tests, no error handling that the question does not need, no abstractions.
- **Show the state.** After each action, show the full state that the question is about.
- **Domain words.** Write labels and buttons in the words of the user, not the words of the code.

## Logic prototype

One HTML file with inline CSS and JavaScript, and no framework or server.

1. Put the logic in one pure module in a `<script>` block. Use a reducer, a state machine, or a set of pure functions. The module does not touch the page. Later, you can move it into the real code.
2. Show these parts from the top down: the question, the state, one button for each action, and the scenario tabs. Show the state as labelled fields. Each scenario starts from a known state and has its steps as buttons. Include the normal case, a difficult edge case, and an action that must fail.

## UI prototype

Make 3 variants that are different in structure: layout, order of information, main action. Not only colour or text. Max 5 variants.

- In a repository with an interface: put the variants on the existing page when there is one, selected with `?variant=A`. Use the components and styles of the project. Add a small switch bar at the bottom to go to the next or the previous variant (also with the arrow keys). Hide the bar in production builds.
- Without a repository: one HTML file with the variants and the switch bar.
- Use stub data. Do not connect a variant to real changes in data.

## Gate G2

Give the human the file or the command, and the question. Ask: "Is this what you want?" Offer: yes, yes with changes, no. The best answer is often a mix: "the header of B with the list of C". Write the result and the reason in `decisions.md` and in the "Gates" section of `plan.md`.

Then update the specs and the tickets with what the prototype showed.
