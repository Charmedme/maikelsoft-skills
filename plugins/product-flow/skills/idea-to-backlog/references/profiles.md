# Profiles

A profile sets the names and the extra fields. The rules for each item type stay the same in each profile.

## Contents

- Levels
- Select the profile
- Agile
- Scrum
- SAFe

## Levels

The default is 2 levels: Feature, then User Story. Use 3 levels (Epic, Feature, User Story) only when the human asks for it. Bugs and tasks are not part of these levels.

Goals:

- Each Feature has its own goal and measure: the part of the result that this feature gives.
- Write the product goal one time, above the features. Put it in the context file. Make a goal item only when the team keeps goals as items.
- Each Feature refers to the product goal with a "Supports:" line in its "Goal" section. Do not copy the product goal into the features.

## Select the profile

Use the profile that the human states. Else, use the signals: the names in the templates, the ticket system, or the existing items. With no signal, use Agile.

## Agile

- **Type:** User Story. The parent is a Feature.

## Scrum

- **Type:** Product Backlog Item. The sentence and the criteria stay the same.
- Scrum has a product goal for the product and a sprint goal for each sprint. Refer to the product goal in "Notes" when one exists.

## SAFe

- **Type:** Story. Use "Enabler" for enabler stories.
- A story fits in one iteration.
- The parent is a Feature. In SAFe, the goal of a Feature is a benefit hypothesis: "We believe that <feature> gives <outcome> for <users>." The "Measure:" line stays.
