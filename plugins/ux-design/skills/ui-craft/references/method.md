# Method: the order of visual decisions

Decide in this order. A later decision does not change an earlier one. When it must, go back one step and write the reason.

## Contents

- 1. Hierarchy
- 2. Grid and layout
- 3. Type roles
- 4. Spacing
- 5. Color roles
- 6. Density
- 7. Detail

## 1. Hierarchy

- Name 3 levels: what the user sees first, second, and third. The first is the main task or the main status of the screen.
- Make the first level dominant with one tool: position (top, left), size, weight, or color, in that order of preference. Use color last, because color also carries status.
- Use max 3 levels of emphasis on one screen. More levels make all levels weak.
- The main task is visible without scrolling at the main device width.
- **Rules at the point of decision.** Each business rule of `PRODUCT.md` or the story that limits a choice on the screen is visible where the user makes that choice. Examples: "1 of 2 places free" next to a technician. "First visit by Tue 27 Oct" next to the save button. "4 technicians are full and not shown" under a list. A rule that the user cannot see becomes an error after the click.
- **What happens next.** Before a commit action (save, assign, send), show a short summary of the result: what changes, for which items, and what the user can undo.

## 2. Grid and layout

- Use the grid and the page layouts of the design system. Name them in the spec.
- Align the edges. Each block starts on a column line. A screen with fewer distinct left edges reads calmer.
- Group by meaning: one region for one job. Put the regions in the order of the flow.
- Text blocks: max 70 to 80 characters per line. Tables and work areas can use the full width.
- At the narrow width, keep the order of the hierarchy. The first level stays first.

## 3. Type roles

- Map each text to a role of the system: page title, section title, body, label, caption, data. Use only sizes from the type scale.
- Max 2 font families (normally 1 for an app). Max 3 weights on one screen.
- Numbers in tables and status values use tabular figures and align right.
- One page title per screen.

## 4. Spacing

- Use only values from the spacing scale of the system.
- Proximity: items that belong together are closer than items that do not. The space between groups is at least 2 steps larger than the space inside a group.
- The same component has the same padding in each place.

## 5. Color roles

- Neutral colors carry most of the screen.
- The accent color marks the primary action, the selection, and the focus. Nothing else.
- Status colors (success, warning, error, info) mark status only, never decoration.
- Color is never the only signal. Add text or an icon with a label.
- Text contrast is 4.5:1 or more. Large text and UI parts are 3:1 or more (WCAG 2.2 AA).

## 6. Density

| Use | Density | Reason |
|-----|---------|--------|
| Daily work by expert users, many items | compact | More items visible, less scrolling. |
| Occasional use, forms, mixed users | comfortable | Easier to read and to click. |
| First use, onboarding, read surfaces | spacious | Focus on one thing. |

Use the density modes of the system when it has them. Click targets stay 24 by 24 CSS pixels or more in each density (WCAG 2.5.8).

## 7. Detail

- Each interactive element has the states hover, focus (visible), active, and disabled from the system.
- Motion: none by default on an operate screen. Use motion only to show a change of place or state, max 200 ms, and respect reduced motion.
- Icons come from the icon set of the system. An icon-only button has an accessible name and a tooltip.
- Long values: names, translations (up to 35% longer), and large numbers fit or wrap. They do not break the layout.
