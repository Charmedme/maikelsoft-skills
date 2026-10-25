# Planted accessibility defects (a11y set)

The approved design is `ux-design.md` and the mockups of FL-31. The clean app is `fieldline-app`. This copy has 6 accessibility defects. axe-core with the WCAG tags does not find them.

| ID | Defect | Where | What the user gets | Rule |
|----|--------|-------|--------------------|------|
| A1 | The filter chips have no visible focus: `outline: none` and no other focus style. | Toolbar, each state with the list | A keyboard user cannot see which chip has the focus. | WCAG 2.4.7 Focus Visible, the accessibility notes of the design |
| A2 | When the assign panel is open, Tab stays inside the panel. The panel is not a modal dialog. | Assign panel, states `assign-ready` and later | A keyboard user cannot go back to the list to change the selection while the panel is open. | WCAG 2.1.2 and 2.4.3, the design (side panel next to the list, not a modal) |
| A3 | The selection bar is fixed at the bottom of the window. It covers the rows that get the focus at the bottom. | List with a selection, states `in-progress`, `assign-ready` | The focused row is under the bar. | WCAG 2.4.11 Focus Not Obscured (Minimum) |
| A4 | The skeleton rows pulse without end, also with "reduce motion" on. | List, state `loading` | Motion that the user asked to stop. | WCAG 2.3.3 (AAA), 2.2.2 for motion that does not stop; the motion decision of the design |
| A5 | The links of overdue work orders have `tabindex="1"`. They get the focus before the navigation. | List | The focus order does not follow the reading order. | WCAG 2.4.3 Focus Order |
| A6 | The toast region has no `role="status"` or `aria-live`. | Toast after the assignment, states `success`, `bulk-result` | A screen reader does not read the result and the undo. | WCAG 4.1.3 Status Messages, the accessibility notes of the design |
