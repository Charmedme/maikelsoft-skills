# UX design: FL-31 Assign unplanned work orders to a technician

Mode: full
Surface: operate

Scenario of the mockups: Monday 26 Oct 2026, 09:42, planner of region Central.

## Sources

| Source | Found | Used for |
|--------|-------|----------|
| docs/work/fl-31-assign-unplanned/story.md | yes | problem, AC-1 to AC-6, the Monday volume (40 to 70 unplanned work orders) |
| PRODUCT.md | yes | users, terms (work order, visit, slot, region, skill), max 2 visits per slot, repair first visit within 2 work days, 24-inch screen, keyboard use, WCAG 2.2 AA, no new components without the design lead |
| DESIGN.md | yes | principles, components, status words, do and don't ("Updated 09:42", no icon without text) |
| design-system/tokens.css | yes | color, type, space, density tokens |
| design-system/components.css | yes | shell, page header, toolbar, chip, bulk bar, table, badge, side panel, alert, toast, skeleton, empty, form |
| docs/ux/decisions.md | yes | UXD-001 (table, ID first), UXD-002 (compact), UXD-003 (undo toast 8 s, no confirm), UXD-004 (side panel) |
| screens/work-orders.html | yes | the existing screen `work-orders`: status chips, columns, header actions |
| design-system-issues.md | yes (made in this run) | audit of the system: DS-1 to DS-7, fixes in `mockups/ds-fixes.css` |
| docs/ux/research-backlog.md | no (made in this run) | needs-users assumptions |

## Frame

| Question | Answer | Evidence |
|----------|--------|----------|
| Problem | "On a Monday I have 40 to 70 unplanned work orders, and I open and assign each one alone." Each assignment also means a search for a technician who has the skill and a free place in the slot. | checked: story |
| Users | Planners who work all day on 60 to 120 work orders, at a desk with a 24-inch screen, and often with the keyboard. They plan one region. They care about speed with many items, no double booking, and what is still open. | checked: PRODUCT.md Users and Facts |
| Value | Fewer steps per work order, and no trial and error for a technician: Fieldline shows only the technicians that can take the work. The limit of 2 visits per slot means that one assign action plans at most 2 work orders. So most of the gain comes from the filtered technician choice, not from the size of the selection. | judgment: arithmetic from "max 2 visits per slot" (PRODUCT.md). The real time gain is needs-users. |
| Success | The planner plans the unplanned work orders of a region with fewer actions than one by one. No double booking. Each work order that is not assigned is visible with its reason. A wrong assignment can be undone. | checked: AC-3, AC-4, AC-5, AC-6 |
| Constraints | Existing screen `work-orders` with the "Unplanned" chip. Detail and edit in a side panel (UXD-004). Undo toast, no confirm (UXD-003). No new components without the design lead. WCAG 2.2 AA. English only. Other planners change the plan at the same time (6 planners, 3 regions). | checked: PRODUCT.md, DESIGN.md, docs/ux/decisions.md |
| Planner habits | Planners select work orders that are close together (same town) for one technician. | needs-users |

The story names a solution (a bulk assign). The problem behind it (slow planning on Monday) is confirmed by the story. The limit of 2 per slot is not in the story, so the story can promise more speed than one action gives. OP-2 is the answer that we propose. This is not a problem mismatch: the filtered technician choice still removes the search for each work order.

## Opportunities

| ID | Opportunity | From | Decision |
|----|-------------|------|----------|
| OP-1 | Warn before the contract rule is broken: a repair whose 2-work-day first-visit limit ends today or the next work day gets a "Contract limit" warning badge next to the type. "Overdue" shows only after the limit. | product: PRODUCT.md "Repairs must get a first visit within 2 work days" | UXD-012 (proposed) |
| OP-2 | Let the planner give a whole day of one technician (morning and afternoon, max 4 places) in one action. Fieldline fills the morning first, then the afternoon. | product: PRODUCT.md "max 2 visits per slot", story "40 to 70 unplanned work orders" | UXD-013 (proposed) |
| OP-3 | Keyboard shortcuts: "A" opens the assign panel for the selection, arrow keys move in the technician table, Enter assigns. | product: PRODUCT.md "Many planners use the keyboard a lot" | UXD-014 (proposed) |

## Flow

1. The planner opens Work orders and selects the "Unplanned" chip. The list shows the unplanned work orders of the region with type, customer, address, skill needed, and due date, earliest due date first. Screen: `work-orders`. AC: AC-1.
2. The planner narrows the list with the chips Type, Skill needed, and Due. Each chip applies at once. The result count and "Clear filters" show. Screen: `work-orders`. AC: AC-2.
3. The planner selects work orders with the row checkboxes (Space, Shift+click for a range). The bulk bar shows the count and "Assign to technician". Screen: `work-orders`. AC: AC-3.
4. The planner selects "Assign to technician". The side panel opens next to the list. Fieldline loads the free places of the technicians. Screen: `work-orders` (state `disabled` until a choice). AC: AC-3, AC-4.
5. The planner sets the date (default: next work day) and selects one technician and one slot in the technician table. The table shows only technicians of the region with each skill that the selection needs, and with a free place on that date. A line under the table says how many technicians are not shown and why. Screen: `work-orders` (state `assign-ready`). AC: AC-4.
6. The planner reads the summary next to the button: which work orders get the visit, which stay unplanned and why, and a warning when the date is after a due date. The planner selects "Assign 2 work orders". Screen: `work-orders`. AC: AC-3, AC-5.
7. Fieldline assigns. When all are assigned, the panel closes, the rows leave the Unplanned list, and a toast says the result with "Undo" (state `success`). When some are not assigned, an alert above the list names each work order and the reason. These work orders stay selected and the panel stays open, so the planner can select another technician at once (state `bulk-result`). Screen: `work-orders`. AC: AC-5.
8. Within 8 seconds the planner can select "Undo" in the toast. The visits are removed and the work orders come back in the list, selected. Screen: `work-orders`. AC: AC-6.

Decision points: at step 5, when no technician has the skills and a free place, the planner changes the date or the selection (`assign-no-technician`). At step 6, when the selection is larger than the free places of the slot, Fieldline assigns in the order of the due date (UXD-006) and the summary names the rest. At step 7, the flow branches on "all assigned" or "some not assigned".

### Failure paths

| Step | Failure | State |
|------|---------|-------|
| 1 | The list does not load | error-load |
| 4 | The free places of the technicians do not load | partial |
| 5 | No technician has the skills and a free place on the date | assign-no-technician |
| 5 | Another planner fills a place while the panel is open | conflict |
| 7 | The assignment fails for all work orders (no answer from the server) | error-save |
| 7 | The assignment fails for some work orders | bulk-result |
| 8 | The undo fails | error-undo |
| 1 to 8 | The connection is lost | offline |

## Patterns

Task facts of `work-orders` in the Unplanned view: items 40 to 70 (story); job act and compare (story AC-3, AC-4); frequency daily (PRODUCT.md); expertise expert (PRODUCT.md); context yes, the planner checks address and due date of the selection while choosing (judgment); risk reversible (UXD-003, AC-6), affects a technician's day; wait under 1 s for filters, 1 to 10 s for availability and assign (judgment, [NEEDS EVIDENCE] from the developers); device desktop first (PRODUCT.md).

| Need | Pattern | Source |
|------|---------|--------|
| Show the work orders | Data table, ID first, sortable | pattern: PAT-LIST-01, decision: UXD-001 |
| Long list | Numbered pages, page size, total; 100 per page, so a Monday fits on one page | pattern: PAT-PAGE-02, decision: UXD-008 |
| Narrow the list | 3 inline chip groups, result count, "Clear filters", apply at once | pattern: PAT-FILTER-01, PAT-FILTER-03, PAT-FILTER-04 |
| Select several | Checkbox selection, count in the bulk bar | pattern: PAT-ACT-02 |
| Choose technician and slot | Side panel next to the list | pattern: PAT-OVL-02, decision: UXD-004 |
| Date | Text input with typed date and calendar picker | pattern: PAT-IN-06 |
| Technician and slot | One radio per free technician and slot in a table (1 choice, all options visible) | pattern: PAT-IN-01, PAT-LIST-01 |
| Before commit | Summary of the result next to the primary button | pattern: PAT-ACT-08 |
| Commit | At once, toast with Undo 8 s, no confirm | pattern: PAT-ACT-03, decision: UXD-003 |
| Some fail | Per-item result with reason, and a direct retry | pattern: PAT-ACT-07 |
| Loading | Skeleton rows, filters stay usable | pattern: PAT-FB-02 |
| Empty | No results with filters; all done | pattern: PAT-EMPTY-02, PAT-EMPTY-03 |

## Options

| Option | Structure | Task fit | Clarity | Consistency | Risk | Cost | Total |
|--------|-----------|----------|---------|-------------|------|------|-------|
| A | `work-orders` list with chips and checkboxes. The bulk bar opens a side panel: date, then a technician table with a radio per free slot, then the summary and "Assign". Results in an alert above the list and a toast. | 3: select, one click in the table, assign. The list stays visible. | 3: the panel has one job, the summary sits at the button. | 3: table, bulk bar, side panel (UXD-004), toast (UXD-003). | 3: each failure has a place: alert above the list, alert in the panel. | 2: one small new part (checkbox and radio style, DS-4). | 14 |
| B | A new `planning-board` screen: unplanned list on the left, a technician by slot grid for the week on the right. The planner selects rows and clicks a free cell. | 3: one click per cell, the week is visible. | 2: two large regions compete for the first look. | 1: a new grid component and a new page layout. | 2: a partial result has no obvious place in the grid. | 1: new components, needs the design lead. | 9 |
| C | `work-orders` list. "Assign" opens a modal dialog with a slot select and a technician select (only free technicians), then "Assign". | 2: the select hides the free places, the list is hidden behind the dialog. | 3: one decision in focus. | 2: a dialog for a task with a list of options, against UXD-004. | 2: the partial result shows after the dialog closes, far from the choice. | 3: existing components only. | 12 |

Selected: A. Reason: it has the highest total. It keeps the selection visible next to the choice, uses only logged patterns, and gives each failure an obvious place. B is a good later step for week planning, but it needs new components.

## Screen: work-orders

| Decision | Value | Source |
|----------|-------|--------|
| purpose | Show the open work orders of the region and let the planner assign unplanned ones to a technician and slot. | product: story AC-1, AC-3 |
| main-task | Select unplanned work orders and assign them to one technician and one slot with a free place. | product: story AC-3, AC-4 |
| pattern | Data table with checkbox selection and bulk bar; assign in a side panel; undo toast; per-item result. | pattern: PAT-LIST-01, PAT-ACT-02, PAT-OVL-02, PAT-ACT-03, PAT-ACT-07 |
| layout | Side nav; main: page header, status chips, filter chips with result count, alert region, bulk bar, table, pager. With a selection: side panel (420 px) to the right of the table. Panel: title, selected work orders, date, technician table, "not shown" line, summary and buttons at the bottom. | system: .fl-shell, .fl-page-header, .fl-toolbar, .fl-bulkbar, .fl-table-wrap, .fl-sidepanel |
| hierarchy | 1: the list of unplanned work orders (top left, full width); in the panel, the technician table. 2: the bulk bar with the count and "Assign to technician"; in the panel, the summary and "Assign". 3: filters, page header, "not shown" line. | product: DESIGN.md principle 1 "The work comes first" |
| components | App shell, page header, chip, result count, bulk bar, data table, badge, side panel, form field (date), alert, toast, skeleton, empty, button. Proposed: checkbox and radio style (DS-4), toast action (DS-3). | system: DESIGN.md components |
| density | compact: 36 px rows in the list and in the technician table, expert daily use | decision: UXD-002 |
| states | default, in-progress, disabled, assign-ready, assign-loading, assign-no-technician, empty, empty-all-done, loading, partial, error-load, error-save, error-undo, conflict, success, bulk-result, stale, overflow, offline | product: story AC-4, AC-5, AC-6 |
| copy | "Unplanned", "Assign to technician", "Assign 2 work orders", "Undo", "Skill needed", "Due", "free" and "Full" per slot; domain words only | product: PRODUCT.md Terms, DESIGN.md "Do use the words of the planners" |
| motion | none: the panel and the toast appear without animation | product: DESIGN.md "calm, dense, and predictable" |

Visual decisions in the order of the `ui-craft` method:

1. Hierarchy: the list is first by position. In the panel, the technician table is first by position under the short header. The rules show at the point of decision: "2 free" or "Full" per technician and slot (AC-4, max 2 visits), "Not shown: ..." under the table, the due warning and the "stays unplanned" line next to "Assign".
2. Grid: the existing page layout. The table and the panel share the top edge under the toolbar.
3. Type: page title 24 px, panel title 16 px bold (`.fl-section-title`), body and data 14 px, table headers and hints 12 px. Dates in the Due column right-aligned with tabular figures (`.fl-num`).
4. Spacing: 8 px inside groups, 16 to 24 px between groups (token scale).
5. Color: teal only for the primary button, the selected rows, the selected radio, and the focus. Status badges only for status and for "Overdue" (UXD-010). The alert colors for results: success, warning for "some not assigned", danger for errors.
6. Density: compact.
7. Detail: native checkbox and radio at 20 px (WCAG 2.5.8), visible focus (DS-1), no motion.

### States: work-orders

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
| default | Unplanned chip active, 63 unplanned work orders of region Central, earliest due first, overdue rows with an "Overdue" badge. No selection, no bulk bar. "Updated 09:42" in the header. | Filter, sort, select rows, open a work order | n/a |
| in-progress | 3 rows selected (teal rows), bulk bar "3 work orders selected", "Assign to technician" (primary), "Clear selection". | Select more, clear, open the assign panel | "Clear selection" |
| disabled | Panel open for 3 work orders. Date Tue 27 Oct. Technician table with free places. No choice yet: "Assign" is disabled with the hint "Select a technician and a slot." | Change the date, select a radio, cancel | Selecting a radio enables "Assign" |
| assign-ready | K. Bos, morning selected (2 free). Summary: "WO-24817 and WO-24822 get a visit with K. Bos, Tue 27 Oct, morning. WO-24840 stays unplanned: no free place left." Due warning for WO-24817 (due Mon 26 Oct). "Assign 2 work orders". | Assign, select another slot, change the date, cancel | Cancel keeps the selection |
| assign-loading | Panel with skeleton rows in the technician table, the date field usable (PAT-FB-02). | Change the date, cancel | The table fills when the free places load |
| assign-no-technician | Panel: "No technician with the skills Heat pump and Air conditioning has a free place on Tue 27 Oct." "Not shown: 6 technicians without both skills, 4 technicians with no free place." | Change the date, go to the next day, cancel and change the selection | "Next work day" sets Wed 28 Oct |
| empty | Filters Repair and Gas boiler give no result: "No unplanned work orders match these filters." Active chips stay visible (PAT-EMPTY-02). | Clear filters, change a chip | "Clear filters" |
| empty-all-done | "All work orders in region Central are planned." (PAT-EMPTY-03) | Go to All open | n/a |
| loading | Filters and header visible, 8 skeleton rows in the table (PAT-FB-02) | Change a filter | Table fills; after 10 s the error-load state |
| partial | The list shows, the panel says "The free places of the technicians did not load." with "Try again". "Assign" is disabled. | Try again, cancel | "Try again" loads the free places again |
| error | n/a: each kind has its own row (error-load, error-save, error-undo, conflict) | | |
| error-load | Danger alert in place of the table: "The work orders did not load. The server did not answer." Filters stay. | Try again | "Try again" reloads. Nothing is lost. |
| error-save | Panel stays open with the choice. Danger alert: "The work orders were not assigned. The server did not answer. Nothing changed." | Try again, select another slot, cancel | "Try again" sends the same assignment again |
| error-undo | Danger alert above the list: "The assignment was not undone. The server did not answer. WO-24817 and WO-24822 are still planned with K. Bos, Tue 27 Oct, morning." | Undo again, open the work orders | "Undo again" retries. The alert stays until it works or the planner closes it. |
| conflict | In the panel, a warning alert: "The free places changed at 09:51. K. Bos, morning now has 1 free place." The summary updates: 1 work order is assigned, 2 stay unplanned. | Assign with the new result, select another slot | The planner sees the new result before the commit |
| success | Panel closed, the 2 rows gone, count "61 unplanned work orders". Toast: "2 work orders assigned to K. Bos, Tue 27 Oct, morning." with "Undo" for 8 s. | Undo, continue planning | "Undo" in the toast (AC-6) |
| bulk-result | Warning alert above the list: "2 of 3 work orders assigned to K. Bos, Tue 27 Oct, morning. 1 not assigned:" with WO-24840 and the reason "K. Bos has no free place left in this slot." WO-24840 stays selected and the panel stays open for it. Toast with "Undo" for the 2. | Select another technician or slot for WO-24840, undo, close the alert | The panel stays open with the failed work order |
| stale | Info alert: "4 new unplanned work orders since 09:42." with "Refresh list". The meta line says "Updated 09:42". | Refresh, continue | "Refresh list" keeps the selection |
| overflow | 12 selected work orders: the panel lists 3 and "and 9 more" with "Show all". A long customer name ("Vereniging van Eigenaren Residentie Hoogstraat") and a long address wrap in the row. A long technician name wraps. | All tasks | The layout holds; rows grow, no text is cut off |
| offline | Warning alert: "No connection. You can look at the list. You cannot assign until the connection is back." "Assign" is disabled. | Read, filter, select | Fieldline checks the connection and removes the alert when it is back |
| session-expired | n/a: assumption, the session lasts the working day; the general sign-in flow handles it | | |
| no-permission | n/a: assumption, each planner and team lead can assign in the own region | | |
| locked | n/a: assignment checks the free places at the commit (conflict, bulk-result); there is no edit lock | | |

### Copy: work-orders

| Element | Text |
|---------|------|
| Page title | Work orders |
| Meta line | Region Central · Updated 09:42 |
| Status chip | Unplanned |
| Filter groups | Type: Installation, Maintenance, Repair · Skill needed: Heat pump, Air conditioning, Gas boiler · Due: Any, Overdue, By today, Within 2 work days |
| Clear filters | Clear filters |
| Result count | 63 unplanned work orders |
| Columns | Work order, Type, Customer, Address, Skill needed, Due |
| Overdue badge | Overdue |
| Bulk bar | 3 work orders selected · Assign to technician · Clear selection |
| Panel title | Assign 3 work orders |
| Panel selected list | Skills needed: Heat pump |
| Date field | Label "Date", hint "Morning 08:00 to 12:00, afternoon 12:30 to 17:00" |
| Technician table | Columns: Technician, Skills, Morning, Afternoon. Cells: "2 free", "1 free", "Full" |
| Not shown line | Not shown: 9 technicians without the skill Heat pump, 2 technicians with no free place on Tue 27 Oct. |
| Disabled hint | Select a technician and a slot. |
| Summary | WO-24817 and WO-24822 get a visit with K. Bos, Tue 27 Oct, morning. WO-24840 stays unplanned: no free place left. You can undo for 8 seconds. |
| Due warning | WO-24817 is a repair due Mon 26 Oct. This date is after the due date. |
| Primary | Assign 2 work orders |
| Secondary | Cancel |
| Toast | 2 work orders assigned to K. Bos, Tue 27 Oct, morning. · Undo |
| Bulk result | 2 of 3 work orders assigned to K. Bos, Tue 27 Oct, morning. 1 not assigned: WO-24840, K. Bos has no free place left in this slot. |
| No technician | No technician with the skills Heat pump and Air conditioning has a free place on Tue 27 Oct. · Next work day |
| Empty, filters | No unplanned work orders match these filters. · Clear filters |
| Empty, all done | All work orders in region Central are planned. · Show all open work orders |
| Error, load | The work orders did not load. The server did not answer. · Try again |
| Error, free places | The free places of the technicians did not load. · Try again |
| Error, save | The work orders were not assigned. The server did not answer. Nothing changed. · Try again |
| Error, undo | The assignment was not undone. The server did not answer. WO-24817 and WO-24822 are still planned with K. Bos, Tue 27 Oct, morning. · Undo again |
| Conflict | The free places changed at 09:51. K. Bos, morning now has 1 free place. |
| Stale | 4 new unplanned work orders since 09:42. · Refresh list |
| Offline | No connection. You can look at the list. You cannot assign until the connection is back. |

Language: dates use "Tue 27 Oct" as in the existing screen. The code builds no sentence from parts except the count and the names; each count has a singular form ("1 work order selected"). Texts fit when 35% longer: the panel texts wrap, the buttons have room.

### Accessibility: work-orders

- Keyboard order: nav, header actions, status chips, filter chips, "Clear filters", alert actions, bulk bar, table header checkbox, rows (checkbox, then the work order link), pager, then the side panel: close, date, technician table radios, "Assign", "Cancel".
- Selection: each row checkbox has the name "Select WO-24817". The header checkbox says "Select all 63 on this page". Shift+click and Shift+Space select a range. Selected rows have `aria-selected="true"`.
- Technician table: the radios of all cells form one group with the name "Technician and slot". Each radio has the name "K. Bos, morning, 2 free". A "Full" cell has no radio; its text says "Full".
- Focus after an action: "Assign to technician" moves the focus to the panel title; Escape and "Cancel" close the panel and return the focus to "Assign to technician". After a full success, the focus goes to the first row of the table, and the toast is announced. The focus does not jump to the toast. After a bulk-result, the focus goes to the alert heading.
- Announcements: the result count, the bulk bar count, the toast, and the alerts use a polite live region (`role="status"`); error alerts use `role="alert"`.
- The toast stays while it has the focus or the pointer (WCAG 2.2.1); the planner can also reach "Undo" with the keyboard.
- Status and "Full" use words, not only color. Contrast fixes DS-1 to DS-3 apply.
- Target size: checkboxes and radios 20 px with a 36 px row (WCAG 2.5.8).

## Decisions

| ID | Decision | Status | Reason |
|----|----------|--------|--------|
| UXD-005 | When the selection needs more than one skill, Fieldline offers only technicians with each needed skill. | proposed | AC-4 says "the needed skill". One visit per work order, but one technician for the whole selection. The panel shows the needed skills, so the planner can split the selection. |
| UXD-006 | When the selection is larger than the free places in the slot, Fieldline assigns in the order of the due date (earliest first, repairs first on the same date). The rest stays unplanned and selected. The summary names them before the commit. | proposed | AC-5 says the others are assigned, but not which ones. The due date and the repair rule (PRODUCT.md) make the earliest the most urgent. |
| UXD-007 | When 1 or more rows are selected, the primary button is "Assign to technician" in the bulk bar, and "New work order" becomes secondary. In the panel, "Assign" is the only primary. | proposed | PAT-ACT-01: one primary per view. The selection makes assign the main task. |
| UXD-008 | The Unplanned view hides the Status and Technician columns (each row has the same value), shows "Skill needed", sorts by due date (earliest first), and shows 100 work orders per page. While the assign panel is open, Customer and Address share one column ("Customer and address", address on the second line). | proposed | AC-1 names the columns. 100 per page fits a Monday (40 to 70) on one page, with PAT-PAGE-02. The shared column keeps the Due column visible next to the 420 px panel on narrower screens. |
| UXD-009 | The date in the assign panel starts on the next work day. | proposed | No source for a default. The morning of today is already running at 09:42. The planner can type another date. needs-users. |
| UXD-010 | A due date before today shows an "Overdue" danger badge next to the date. | proposed | DESIGN.md has no rule for a late due date (DS-7). Color with a word. |
| UXD-011 | After a partial result, the panel stays open with the work orders that were not assigned still selected. | proposed | AC-5: the planner must act on the rest. It saves 2 steps. |
| UXD-012 | OP-1: a "Contract limit" warning badge on a repair whose 2-work-day first-visit limit ends today or the next work day. | proposed | PRODUCT.md contract rule. The planner sees the risk before the work order is overdue. |
| UXD-013 | OP-2: an extra "Whole day" column in the technician table (max 4 places). | proposed | PRODUCT.md max 2 visits per slot limits one action to 2 work orders. |
| UXD-014 | OP-3: keyboard shortcuts "A" (assign), arrow keys in the technician table, Enter (assign). | proposed | PRODUCT.md keyboard use. |
| UXD-015 | Adopt the fixes DS-1 to DS-4 of `design-system-issues.md` in the design system (focus ring, input border, toast action, checkbox and radio). | proposed | WCAG 2.2 AA (PRODUCT.md). The design lead must approve new parts. |

## Open questions

- My judgment: a planner reading the panel sees at once who can take the work and how many places are left. The weak spot is the limit of 2 per slot: with a selection of 10, the planner repeats the panel 5 times. OP-2 halves that. Week planning (option B) is the real answer and is a separate story.
- My judgment: the "stays unplanned" line before the commit is the most important text of the design. Without it AC-5 becomes a surprise after the click.
- [NEEDS EVIDENCE] Do planners choose the slot first or the technician first? Method: observe 3 planners on a Monday.
- [NEEDS EVIDENCE] Do planners group work orders by town before they assign? If yes, a sort or filter by town is the next speed gain. Method: observe, and check the addresses of assignments in the data.
- [NEEDS EVIDENCE] How long do the free places and the assignment take to load (sets PAT-FB-02 or PAT-FB-03)? Method: ask the developers.
- [NEEDS EVIDENCE] Can a planner undo after the 8 seconds, from the work order? Assumption: yes, with the existing unassign in the work order detail. Method: ask the product owner.
- Assumption: the due date of an unplanned work order is the date of the first visit that the contract needs.
