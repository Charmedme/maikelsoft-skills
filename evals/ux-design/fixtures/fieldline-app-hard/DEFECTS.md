# Planted defects (hard set)

The approved design is `ux-design.md` and the mockups of FL-31. The clean app is `fieldline-app`. This copy has 6 subtle defects. Each one is visible only when you compare the build with the design state by state, or read the behavior.

| ID | Defect | Where | What the user gets | Design rule that it breaks |
|----|--------|-------|--------------------|----------------------------|
| D1 | The summary before the commit does not say which work orders stay unplanned. | Assign panel, state `assign-ready` with more work orders than free places | The planner learns only after the click that some work orders were not planned. | AC-5, the "stays unplanned" line (the design calls it its most important text) |
| D2 | The undo toast closes after 3 seconds, but the text says "You can undo for 8 seconds". | Toast after the assignment, state `success` | The planner misses the undo. The text is wrong. | AC-6, the undo time of the design (8 s), WCAG 2.2.1 |
| D3 | The loading state of the list is blank: no skeleton rows and no header. | List, state `loading` | An empty white area. The planner does not know if the list loads. | State `loading` (skeleton rows, PAT-FB-02) |
| D4 | After an assignment, the focus does not move to the result message. | List, states `success` and `bulk-result` | A keyboard or screen reader user does not hear the result, and the focus stays on a button that is gone. | Accessibility notes (focus to the result), H1 |
| D5 | The empty state for filters has no "Clear filters" button and does not show the active filters. | List, state `empty` (no filter results) | The planner sees "No unplanned work orders match these filters." and has no way back in that place. | State `empty` (recovery), copy table |
| D6 | The Cancel button of the assign panel is a primary button. | Assign panel, footer | Two primary actions next to each other. The planner can click Cancel by mistake. | One primary action per view (design system), H8 |
