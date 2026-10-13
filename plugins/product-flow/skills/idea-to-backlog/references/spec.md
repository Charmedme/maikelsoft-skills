# Phase 4: Spec and story map

## Contents

- The spec
- The story map
- From the map to the backlog

## The spec

Write these parts in the "Spec" section. Take each part from the decisions and the prototype verdict. Do not add new decisions here: when one is missing, go back to phase 2 for that question only.

- **Goal:** the outcome for the users and the business, and the measure.
- **Users:** each type of user, in one line each.
- **Scope:** "In" and "Out".
- **Constraints:** the fixed systems, dates, and technologies.

## The story map

A story map shows the work in the order of the user, so that gaps and layer stories show before you write items.

1. **Activities.** The big things that the user does, from left to right, in the order of use. Example: "Find a product", "Order", "Follow the delivery".
2. **Steps.** Under each activity, the steps of the user. Each step is a future story.
3. **Release slices.** Draw a line under the steps of the first release. The first release is the smallest set of steps that lets a user reach the goal from start to end. Put the other steps under the line.

Write it as a table:

```markdown
| Activity | Release 1 | Later |
|----------|-----------|-------|
| Find a product | Search by name; search by article number | Search by brand |
| See the price | See my contract price | Compare with the list price |
```

## From the map to the backlog

- Each activity becomes a feature, or a group of activities becomes one feature when they are small. With 3 levels, the full map is the epic.
- Each step becomes a story under its feature. Steps under the line become stories too, with the note "Later".
- A step without user value is a task of the team. Leave it out.
