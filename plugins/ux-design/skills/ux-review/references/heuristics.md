# Heuristics and walkthrough

## Heuristics

Give each heuristic one verdict for the feature: **holds**, **strained** (the user can continue, with effort), or **broken** (the user fails or makes an error). Do not add up scores.

| ID | Heuristic | Look for |
|----|-----------|----------|
| H1 | Visibility of system status | A loading state for each wait over 1 second. A result after each action (how many saved, which ones failed). The selection count. |
| H2 | Match with the real world | The words of the domain from the copy table. Dates, numbers, and money in the format of the locale. The order of the real task. |
| H3 | User control and freedom | Undo for a bulk action. Cancel in each panel and dialog. The selection stays after an error. |
| H4 | Consistency and standards | The components and tokens of the design system. The same action has the same label and place on each screen. |
| H5 | Error prevention | Only valid choices are offered (for example only technicians with a free place). The rule shows at the point of decision. A confirm step before a large change. |
| H6 | Recognition over recall | The selected items stay visible in the panel. The filters show their values. The user does not need to remember an ID. |
| H7 | Flexibility and efficiency | Keyboard use for the main task. Bulk actions for repeated work. Filters that stay. |
| H8 | Focused design | One primary action for each view. No text or decoration that does not help the task. |
| H9 | Recovery from errors | Each error says what happened, why it matters, and what to do. A retry where a retry can work. No technical codes alone. |
| H10 | Help | Help at the point of need, for example the reason why an item is not available. |

Also check the accessibility facts that the tools do not see:

- The focus moves to the result after an action, and the focus is visible.
- A live region announces a bulk result.
- The order of the keyboard follows the order of the task.

## Walkthrough of the main task

Walk the main task step by step, as the user of the frame. For each step, answer:

1. Does the user try to do the right thing here?
2. Does the user see the control for it?
3. Does the user connect the control with the goal (the label, the place)?
4. After the action, does the user see that it worked?

Give each step **pass**, **hesitation**, or **failure**. A failure is a finding. A hesitation is a finding when it occurs on the main task.

## Dark patterns

Check for these patterns, first in the forms of a business app:

- A default that the user would not select (a checked box for a cost or a mailing).
- A hidden cost, a hidden consequence, or a confirm button that does not say the action.
- A cancel that is harder than the start.
- Shame or pressure in the text ("No, I do not want to save time").
- A fake deadline or a fake count.
