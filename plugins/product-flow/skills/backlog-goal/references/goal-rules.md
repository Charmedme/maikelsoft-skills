# Goal rules

Each rule has an ID. The checker (`backlogcheck.py`) finds the rules marked "checker". You find the others. A review cites the ID.

## Contents

- Rules for each goal
- Product goal
- Sprint goal
- Where the goal goes
- Dutch

## Rules for each goal

- **GL1 One sentence.** The goal is one sentence of at most 25 words. A reader must remember it. (checker)
- **GL2 Outcome, not output.** The goal says what changes for the users or the business, not which items the team finishes. "More customers order without help" is an outcome. "Release the search feature" is output.
- **GL3 One goal.** A goal with "and" often holds two goals. Select the one that matters most. (checker: warning)
- **GL4 Supported.** The goal lists the items that serve it under "Supports". An item that serves no goal is a question for the product owner.

## Product goal

- **PG1 Measure.** "Measure:" gives a number and, when known, the start value: "Orders without a phone call: from 60% to 75%". When nobody can give the number, write "Measure: open question". Give your proposal in the question, not in the goal. (checker)
- **PG2 Time frame.** Give a time frame of some months. One product goal at a time is normal in Scrum. (checker)

## Sprint goal

- **SG1 One objective.** The goal gives the sprint one direction. The team can change the scope of the items and still meet the goal.
- **SG2 Check.** "Check:" says how the team sees at the sprint review that it met the goal. Example: "A customer can find a product by its name on the test site." (checker)
- **SG3 Proposal.** The team sets the sprint goal in sprint planning. Say that this goal is a proposal.

## Where the goal goes

The default backlog has 2 levels. The product goal then goes in the "Goal" section of the feature. With 3 levels, it goes in the feature, the lowest of the two parent levels. When the ticket system has its own place for goals, `backlog-push` also puts it there:

| System | Product goal | Sprint goal |
|--------|--------------|-------------|
| Jira | Atlassian Goal (Goals field), or the feature | `goal` field of the sprint |
| Azure DevOps | The feature (no goal type) | Team wiki (no goal field) |
| GitHub | The feature, or an issue type "Goal" when the organization has one | The feature (no field) |
| GitLab | Objective (Ultimate, when the feature flag is on), else the feature | Description of the iteration cadence |
| Linear | Initiative | Description of the cycle |

## Dutch

Use "Productdoel" and "Sprintdoel" as the type. Use "Termijn:", "Meten:", "Controle:", "Draagt bij", and "Notities". Follow the Dutch rules in `language.md`.
