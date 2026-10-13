# Mapping

## Contents

- Generic levels
- Default mapping
- A missing level
- Goals
- Fields
- Text format

## Generic levels

The draft uses these types, from the top down:

| Draft type | Level |
|------------|-------|
| Product Goal | Above everything |
| Epic | Only with 3 levels |
| Feature | The parent of the stories |
| User Story, Product Backlog Item, Story | The work item of the team |
| Spike, Enabler | The same level as a story, with a label |

The default backlog has 2 levels: Feature, then User Story. Sprint goals are not items: they go on the sprint (see the system reference).

## Default mapping

Map each draft type to the level of the system, not to the name. Read the real levels of the project first: teams rename types and add levels.

- With 2 levels, the Feature goes to the first level above the story level of the system. In Jira that is "Epic". In Azure DevOps (Agile, Scrum, CMMI) that is "Feature".
- With 3 levels, the Epic goes to the level above that one.
- When the project has a template for a type, use the fields and headings of the template, and put the draft content in them.

## A missing level

When the system has no level for a draft type, propose one option with a recommendation, and let the human decide:

1. **Label.** Put the item as a label or tag on its children. Put its goal and scope in the description of the first child. Recommended when the level only groups items.
2. **Next level up.** Use the next level that exists. Recommended when the next level is empty in this project.
3. **Leave it out.** Link the children to the level above. Recommended only when the human says that the level has no value.

## Goals

The product goal goes in the "Goal" section of the lowest parent level (the feature). When the system has its own goal object, also create the goal there and link it (see the system reference). When the goal has no place in the system, it stays in the feature only. This is normal.

## Fields

Fill only the fields that the draft has. Do not invent a value for a size, a priority, or a date. Leave them for the team.

| Draft part | Where it goes |
|------------|---------------|
| Title | Title or summary |
| Story sentence, goal, scope, notes | Description |
| Acceptance criteria | The acceptance criteria field when the system has one, else a section of the description |
| Spike, Enabler | A label or a tag, when the system has no type for it |

## Text format

Convert the Markdown of the draft to the format of the field. The system reference gives the format. Keep the checklist of the acceptance criteria as a list.
