# Rubrics

Score each criterion 1, 2, or 3. Write one short reason for each score. A score without a reason does not count.

## Option rubric (step 3)

Score each concept against the frame, not against your taste.

| Criterion | 3 | 2 | 1 |
|-----------|---|---|---|
| Task fit | The main task takes the fewest steps of all concepts. | One extra step or look. | The main task is hidden or slow. |
| Clarity | The first level of the hierarchy is the main task or status. | The main task competes with one other element. | No clear first element. |
| Consistency | Only existing patterns, components, and logged decisions. | One new pattern or component, with a reason. | Several new parts, or a conflict with a logged decision. |
| Risk | Errors, empty, and slow states have an obvious place. | One state has no obvious place. | The structure makes errors likely or hard to recover. |
| Cost | Existing components only. | One small new part. | New components or a new layout. |

Select the highest total. On a tie, select the lower cost.

### New-direction variant

Use this variant only when `ux-design.md` has `Mode: new-direction`. In this mode, the owner asked for something new, so a concept that keeps the old look fails the request. The standard rubric gives the highest score to existing parts, so it selects the old look again.

Add the criterion Difference. Count Consistency and Cost at half weight. Total = Task fit + Clarity + Risk + 2 x Difference + (Consistency + Cost) / 2.

| Criterion | 3 | 2 | 1 |
|-----------|---|---|---|
| Difference | The color, the type, and the shape of the main element (for example the card) are all new, from the chosen style direction. | One or two of the three are new. | The concept keeps the old colors, type, and shapes. Only the layout changed. |

At least one concept has Difference 3. When no concept has Difference 3, make a new concept from the chosen style direction before you score. Consistency in this mode is consistency with the chosen style direction, not with the old screens.

Example: concept A keeps the old purple cards (Difference 1). Concept B has the new colors and type, but the old card shape (Difference 2). Concept C changes all three (Difference 3). Concept C is the one that answers the request, unless its Task fit or Risk scores 1.

## Mockup rubric (step 5)

Score each rendered frame. The bar: no criterion scores 1.

| Criterion | 3 | 2 | 1 |
|-----------|---|---|---|
| Hierarchy | The spec's first level is the first thing the eye finds. | The first level competes with one element. | The eye has no clear start. |
| Alignment | All blocks share few left edges. Nothing is off the grid. | One block is off. | Several edges, a ragged layout. |
| Rhythm | Spacing is on the scale and groups are clear. | One group is unclear. | The spacing does not show the groups. |
| Density | Matches the spec. Nothing is cramped or empty. | One region too tight or too loose. | The wrong density for the use. |
| Consistency | Only system components, tokens, and sizes. | One small drift. | Several values or components outside the system. |
| Content fit | Real labels and data from the product facts. The business rules show where the user decides. Long values fit. | One rule hidden, one value cut off, or one placeholder text. | Lorem ipsum, generic data, or the layout breaks with real content. |

### Set-level pass

Score the frames one by one first. Then give one score for the whole set of screens. A frame-by-frame score does not see that the same element moves from screen to screen. Use the Element inventory of `ux-design.md` as the list of shared elements, and compare the screens at the same width.

| Criterion | 3 | 2 | 1 |
|-----------|---|---|---|
| Set consistency | Each element of the inventory has the same home (region), the same component, and the same icon and label on each screen. | One element has a different place, component, or label on one screen. | Two or more elements move or change between screens. |

Example: the music toggle is in the top bar on the lobby and in the bottom bar on the game screen. This set gets Set consistency 2 at best. Fix it in the mockup, or change the inventory with a reason. Write the set score on its own line in `critique.md`: `Set consistency: <score>. <reason>.`
