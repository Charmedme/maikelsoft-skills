# Planted defects

The approved design is `ux-design.md` and the mockups of FL-31. The clean app is `fieldline-app`. This copy has 6 planted defects.

| ID | Defect | Where | What the user gets | Design rule that it breaks |
|----|--------|-------|--------------------|----------------------------|
| D1 | The bulk result shows only the counts, not which work orders failed and why. | Bulk result after a partial assignment | "3 of 5 assigned. 2 not assigned." The planner does not know which ones to plan again. | AC-5, state `bulk-result` and `partial` |
| D2 | The save error shows "Error 500." and has no "Try again" button. | Assign panel, state `error-save` | A technical code, no reason, and no retry. The planner must start again. | State `error-save`, copy table, H9 |
| D3 | The panel offers technicians without the needed skill. | Assign panel, technician list | The planner can assign a work order to a technician who cannot do it. The "Not shown" line does not name them. | AC-4, PAT rule of the panel |
| D4 | The row checkboxes have no accessible name. | List, each row | A screen reader says "checkbox, not checked" for each row, with no work order. | Accessibility notes, WCAG 4.1.2 |
| D5 | At 390 px (and 820 px) the page scrolls to the side. | Whole page, small widths | The list and the panel stay side by side with a minimum width. The planner must scroll sideways. | Layout decision, WCAG 1.4.10 reflow |
| D6 | The "Overdue" badge uses a hard-coded light red: low contrast (about 2.3:1) and colors outside the tokens. | List, due column | Overdue work orders are hard to read. | Design system tokens, WCAG 1.4.3 |
