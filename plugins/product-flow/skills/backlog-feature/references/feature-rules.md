# Feature rules

Each rule has an ID. The checker (`backlogcheck.py`) finds the rules marked "checker". You find the others. A review cites the ID.

## Contents

- Title
- Goal
- Size
- Scope
- Acceptance criteria
- Stories
- Dutch
- Example

## Title

- **F1 Capability.** The title names what the users can do, not a part of the system: "Product search", "System status for customers". (checker)

## Goal

- **F2 Outcome, with a measure.** The goal says what changes for the users and for the business. "Measure:" gives the number that shows it. Example: "Customers find a product without help. Measure: 30% fewer calls to customer service about products." (checker)
- **F2a No invented target.** When nobody can give the number, write "Measure: open question" and put the question in the "Notes".

## Size

- **F3 A few sprints.** The team can deliver the feature in 1 to 3 sprints (SAFe: one program increment). A feature has 2 to 8 stories. More than 8 stories means 2 features. Fewer than 2 means that the feature is a story. (checker: warning for more than 8)

## Scope

- **F4 In and Out.** "In" lists the results for the users. "Out" lists what a reader can expect but does not get. Each "In" result has at least one story. (checker)
- **F4a From the request.** Each thing that the request names is "In", or "Out" with the reason. Example: "see the status of the system" needs one overall status, not only a status for each part.

## Acceptance criteria

- **F5 Feature level.** Write checks for the full feature, and quality requirements that apply to all its stories: "Security:", "Performance:", "Accessibility:". Do not copy the criteria of the stories. 2 to 6 criteria is normal. Use a checklist, not Given/When/Then. (checker)

## Stories

- **F6 Through backlog-story.** The stories follow the rules of the `backlog-story` skill. Each story has this feature as its parent.
- **F7 No bugs, no tasks.** A bug is wrong behaviour of an existing function. A task is work that the developers plan. Neither is part of a feature. Tell the human when the request is one of these.
- **F8 Need, not solution.** A technology in the request is a constraint. Put it in a "Security:" or other labelled criterion, or in the "Notes". Do not make a story for it.

## Dutch

Use the headings "Doel", "Scope", "Acceptatiecriteria", "Stories", and "Notities". Use "Meten:", "Binnen scope:", and "Buiten scope:". Follow the Dutch rules in `language.md`.

## Example

```markdown
## Product search

**Type:** Feature
**Parent:** None

### Goal

Customers find the products that they need without help, at their own price.
Measure: 30% fewer calls to customer service about products, 3 months after the release.

### Scope

- In: search by name, article number, and brand.
- In: contract prices in the results.
- Out: search by EAN code.

### Acceptance criteria

- [ ] A customer can find each of the 12,000 products by its name or article number.
- [ ] Performance: each search shows its results in less than 2 seconds.
- [ ] Security: a customer never sees the contract prices of another customer.

### Stories

- Find a product by its name
- Find a product by its article number
- Find the products of a brand
- See my contract price in the search results
```
