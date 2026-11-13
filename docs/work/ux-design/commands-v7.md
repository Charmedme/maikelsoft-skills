# ux-design 1.0.0: the commands of plan version 7

Step 8 of the delivery plan, version 7. The plan gives ux-design two commands. `/ux-design` makes the design, the mockups, the states, and the user test of an item with UI work. `/ux-review` checks the built screen against its design.

Before this version, a person asked for a mode of the `ux-designer` agent by name. Nobody could answer questions during the build of this step. So each open decision got the recommended answer. This file gives each decision and its reason.

## Decisions

| ID | Decision | Reason |
|----|----------|--------|
| D1 | `/ux-design` is a new, thin skill (`skills/ux-design/`). It gets the item, makes the work folder, starts the agent in design and mockup mode, and in prototype mode after a yes. | The agent cannot ask the human, and the modes have two hard stops. A skill in the main session can hold the stops. The method stays in the agent and in `ux-practice`, so there is one place for each rule. |
| D2 | The existing skill `ux-review` is the `/ux-review` command. A section "Start from a command" adds the context step, the work folder, the base of the diff, and the `Next:` line. The agent in review mode starts at step 1. | Design 7.2 names the existing skill as the entry. A second skill for the same job gives two entries. The main session can run the review itself and ask for the run command, so the command needs no extra agent. |
| D3 | `ux-practice` leaves the `/` menu (`user-invocable: false`). Its description names `ux-design:ux-design` for a person. | Its old trigger ("the UX design before developers build") is the job of `/ux-design`. Two entries for one job break the principle "one entry for each phase". The agent still preloads it. |
| D4 | `ux-setup` leaves the `/` menu. It stays the setup step: `/setup` of dev-setup and the two commands start it. It names `/setup` when the session has dev-setup. | The plan puts the setup step of each plugin out of the menu. The plugin must still work alone, so the step stays. |
| D5 | The `## Design` section moves to `.claude/rules/delivery.md`. The agent, the commands, the tool skills, and `setupscan.py` read it there first, then in `CLAUDE.md`. `ux-setup` copies an old section and removes it from `CLAUDE.md` only after the OK of the human. | The plan moves the facts of each plugin into one committed file that Claude Code loads at each session start. The fallback keeps each running project and each eval fixture working without a change. |
| D6 | The work folder is `docs/work/<item key>/`. `/ux-design` writes `story.md` there from the ticket, word for word, and continues at the first open step when the folder exists. | The plan says that `/ux-design` or `/build`, whichever comes first, makes the folder. The agent reads `story.md`. A resume avoids a second design after a stop. |
| D7 | The approval of the design is a line `Approved: <name> (<role>, <date>)` below the `Mode:` line of `ux-design.md`. The skill writes it, not the agent. | `/build` asks for an approved design. A line in the file is a fact that a later command can read. `uxcheck.py` ignores the line. |
| D8 | One message goes to the product owner with the steps of `po-questions.md`. It holds the state examples that the Behaviour spec does not cover, the proposed decisions, and the opportunities. The skill does not change the ticket. | The plan: "State examples go to the product owner as a spec change." The PO owns the spec until the build starts. |
| D9 | The prototype is optional. The skill recommends it only when an assumption of the main task is `needs-users`. | A prototype costs a full agent run. The test with users is the value, and only people run it. |
| D10 | `ux-prototype` stays in the menu. Its trigger is a prototype for a design that exists. | That is a later job, for example after a design from an earlier version. It does not compete with `/ux-design` for a new design. |
| D13 | The skills `ux-states`, `ux-patterns`, `ui-craft`, `brand-rules`, `web-accessibility`, `figma`, `claude-design`, and `openpencil` stay in the menu. | The plan takes the practice skills of the dev-team agents out of the menu. These skills are knowledge skills with their own trigger, like the dev-stack and doc- skills, which keep their names and start by themselves. |
| D11 | The `Next:` line of `/ux-design` depends on the role: a designer gives the item to the developer and comes back with `/ux-review`. Other roles get `/build <item>`. | The plan: each command ends with the next step of the route. |
| D12 | New major version 1.0.0. | The menu changes and the place of the Design section are breaking for a person who typed the old commands. The plan asks for a new major for each plugin of version 7. |

## Open

- dev-flow `build` (`references/ux-review.md`) still names the `## Design` section of `CLAUDE.md`. The fallback covers it. The text belongs to dev-flow, which another step of the plan owns.
- `/build` reads an approved design from the work folder only through its ticket step. A work folder that `/ux-design` made first needs a check in the comparison of plan step 9.
- No eval runs yet. Plan step 9 runs the comparison.
