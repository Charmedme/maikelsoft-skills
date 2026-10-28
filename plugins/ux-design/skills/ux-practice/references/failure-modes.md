# Failure modes

Each artifact has a named failure. In the self-check, read the row of each artifact that you made. When the failure is present, fix it, or write the refusal sentence in the open questions.

| Artifact | Failure | How you see it | Refusal sentence |
|----------|---------|----------------|------------------|
| Frame | False orientation | The problem is sized to fit the solution that the story already names. | "The story names a solution. The problem behind it is not confirmed." |
| Frame | Invented user | A persona, a quote, or a number that no source gives. | "No source gives this user fact. It is an assumption." |
| Flow | False coherence | Several real user paths are merged into one happy path. | "This flow covers one path. Paths X and Y are not designed." |
| Flow | Orphan screen | A screen has no job in the flow. | "Screen X has no step. Remove it, or name its step." |
| Patterns | Pattern by taste | A pattern with no rule ID and no decision. | "This pattern has no rule. It is a proposed decision." |
| Options | Fake options | The options differ only in color, text, or spacing. | "The options must differ in layout, order, or main action." |
| Options | Old look again | In new-direction mode, all concepts keep the old colors, type, and card shapes. No option has Difference 3. | "The request asks for a new look. Concept X changes the color, the type, and the shape." |
| Spec | Empty decision | A decision without a source tag. | "This decision has no source. It is an open question." |
| Spec | Invisible controls | Control edges, toggle states, or icons below 3:1 against the color next to them. Raised tiles below 1.5:1 on the stage. The check covered only text pairs. | "Edge X has a ratio of 1.4:1. A control edge needs 3:1 (WCAG 1.4.11)." |
| States | Happy path only | Empty, error, or loading has no design. | "State X has no design. The build will improvise it." |
| Copy | System words | Labels use the words of the code, not of the user. | "Label X uses an internal term. The user term is Y." |
| Mockups | Polish trap | Styling that invites feedback on style, while the question is about structure. | "This mockup shows structure. Style comes from the design system." |
| Mockups | Drift | A component, color, or size that is not in the design system. | "X is not in the system. Use Y, or propose a new component." |
| Mockups | Moving element | The same element (a toggle, the leave action, the player list) is in a different region on each screen. Each frame scores well alone. | "Element X is in the top bar on screen A and in the bottom bar on screen B. Its home is the top bar." |
| Mockups | Text toggle | An on/off setting during the task (music, chat, microphone) is a text button, or its label changes with the state ("Music on", "Music off"). | "X is a toggle. Use an icon button with `aria-pressed` and a fixed name (PAT-TOGGLE-02)." |
| Mockups | States before direction | All states of all screens exist before the owner agreed on the look of the key screens. | "The owner did not approve the key screens. The states wait for that approval." |
