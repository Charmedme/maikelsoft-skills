# Story rules

Each rule has an ID. The checker (`backlogcheck.py`) finds the rules marked "checker". You find the others. A review cites the ID.

## Contents

- The story sentence
- The title
- Acceptance criteria
- Size and scope
- Special stories
- Dutch
- Example

## The story sentence

- **S1 Specific role.** Name the type of user who gets the value: "warehouse planner", "new customer", "administrator". "User", "signed-in user", and "app user" are too wide: they hide who has the problem. When the request says only "user", find the real users in the context. If the context does not give them, ask, or write an assumption with a specific role. (checker)
- **S2 One goal.** One result for one role. A goal with "and" or "or" is often two stories. (checker: warning)
- **S3 Real value.** The "so that" part says why the role wants the goal. If the value only repeats the goal, ask the human or leave the part out. A story without value for a user is a task. Tasks belong to the developers, not to the backlog of the product owner.
- **S4 What, not how.** Write the need, not the solution. "I want to find an order by its number", not "I want a search field with autocomplete". The team selects the solution.

## The title

- **T1 Result for the user.** The title says what the user can do after the story: "Find an order by its number". (max 80 characters, checker)
- **T2 Not a layer.** A title with "API", "database", "backend", "frontend", "endpoint", or "table" describes a part of the system. Such a story gives no value alone. Make a vertical story: a narrow path through all layers that a user can see. (checker: warning)

## Acceptance criteria

The criteria tell when the product owner accepts the story. They come from the value: each criterion shows that the user gets the value, or protects it.

- **A1 Present.** Each story has acceptance criteria. (checker)
- **A2 One result, checkable.** Each criterion is one result that a person or a test can check with yes or no. (checker: warning for "and")
- **A3 Measurable words.** Replace "fast", "easy", "clear", "short", "main", "user-friendly", or "etc." with a number, a list, or an example. "The main parts" becomes the list of parts, or an open question that asks for the list. Example: "The list shows in less than 2 seconds for 10,000 orders". (checker: warning)
- **A4 Behaviour, not clicks.** Write what the user sees or gets, not the steps in the screen. "The user sees the orders of today", not "The user clicks the Orders button". (checker: warning)
- **A5 Rule list.** Write the criteria as a checklist (`- [ ]`). Do not use Given/When/Then. Put constraints and quality requirements in the same list, with a label: "Security:", "Performance:", "Accessibility:". (checker)
- **A6 Edge cases and errors.** Include the most important edge case and what the user sees when it fails. Example: "When no order matches, the user sees 'No orders found'."
- **A7 Enough, not more.** 3 to 7 criteria is normal. More than 8 means the story is too large: read `split.md`. (checker: warning)

## Size and scope

- **Z1 One sprint.** A story fits in one sprint. When it does not, split it.
- **Z2 Independent.** The team can build and show a story alone. When two stories depend on each other, say so in "Notes", or combine them.
- **Z3 Out of scope.** Name what the story does not do when a reader can expect it. Put it in "Notes".
- **Z4 No duplicates.** When an existing story covers the same result, refer to it. Do not write a second one.

## Special stories

- **Spike.** The team must learn something before it can estimate a story. Write the question and the time limit. The acceptance criterion is the answer: "The team has a decision on the payment provider, with the reason." Set **Type:** to "Spike".
- **Enabler.** Work that has no direct user value but that later stories need (architecture, compliance, infrastructure). Name the stories that need it. Set **Type:** to "Enabler". Use it only when a vertical story is not possible.
- **Defect.** When the request describes wrong behaviour of an existing function, it is a bug, not a story. Tell the human. Write it as a bug only when the human agrees.

## Dutch

Use this sentence: "Als <rol> wil ik <doel>, zodat <waarde>." Use the headings "Acceptatiecriteria" and "Notities". The labels are "Buiten scope:", "Open vraag:", and "Aanname:". Follow the Dutch rules in `language.md`.

## Example

```markdown
## Find an order by its number

**Type:** User Story
**Parent:** Order search

As a customer service agent, I want to find an order by its number, so that I can answer a customer on the phone at once.

### Acceptance criteria

- [ ] A full order number shows the order.
- [ ] A part of the number (minimum 4 digits) shows each order that contains it.
- [ ] When no order matches, the agent sees "No orders found".
- [ ] Performance: the result shows in less than 2 seconds.
- [ ] Security: the agent sees only the orders of their own company.

### Notes

- Out of scope: search by customer name.
```
