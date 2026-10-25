# UX design: FL-44 Show the status of a parts order on the work order

Mode: full
Surface: operate

The planner opens a work order that waits for parts. The side panel of the work order shows the parts order from PartsLink: each part with its supplier status and expected date, the time of the last update, and a clear alert when a date moves, a part is cancelled, or PartsLink is silent. When all parts are delivered, the planner plans the next visit in the same side panel.

## Sources

| Source | Found | Used for |
|--------|-------|----------|
| `docs/work/fl-44-parts-status/story.md` | yes | problem, AC-1 to AC-5 |
| `PRODUCT.md` | yes | users, terms, facts (PartsLink delay up to 4 hours, 2 visits per slot, slots, 24-inch screen, keyboard use), WCAG 2.2 AA, no new components without approval |
| `DESIGN.md` | yes | principles, components, status words, "show the time of data that can be old" |
| `design-system/tokens.css` | yes | color, type, space, density tokens |
| `design-system/components.css` | yes | all components of the mockups |
| `docs/ux/decisions.md` | yes | UXD-001 to UXD-004 |
| `screens/work-orders.html` | yes | the existing screen `work-orders`, its list, its filter chip "Waiting for parts", sample data |
| `docs/ux/research-backlog.md` | no | created by this design |
| System audit | yes | `design-system-issues.md` (DS-1 to DS-7), `mockups/ds-fixes.css` |

## Frame

| Question | Answer | Evidence |
|----------|--------|----------|
| Problem | To know when a part arrives, the planner logs in to PartsLink and searches for the order. The planner often plans a visit too early, and the technician arrives without the part. | checked: story |
| Users | Planners who handle 60 to 120 work orders a day each, at a desk with a 24-inch screen, much with the keyboard. They move visits when a part is late and call customers to confirm. | checked: PRODUCT.md |
| Users (behavior) | The planner checks parts status only for work orders with the status "Waiting for parts", and wants to plan the next visit at the moment the parts are in. | judgment: the story names this moment ("plan the next visit when the part arrives"). How often a planner checks is [NEEDS EVIDENCE]. |
| Value | No switch to PartsLink. No visit planned before the part is delivered. The planner can tell the customer a date from the supplier. | needs-users |
| Success | AC-1 to AC-5 pass. Fewer visits where the technician arrives without the part. | checked: story (AC). The count of visits without the part today is [NEEDS EVIDENCE]. |
| Constraints | Data from PartsLink can be up to 4 hours old. Max 2 visits per technician per slot. No new design-system components without the design lead. WCAG 2.2 AA. English only. Detail of a work order opens in a side panel (UXD-004). | checked: PRODUCT.md, decisions.md |

The story names a place (the work order) and a moment (when the part arrives). The problem behind it, "the visit is planned too early", is confirmed by the story text only. The design blocks planning before delivery, so it acts on that problem directly (UXD-007).

## Opportunities

| ID | Opportunity | From | Decision |
|----|-------------|------|----------|
| OP-1 | Show a short parts status ("All delivered", "Expected Thu 29 Oct", "1 cancelled") in a "Parts" column of the work order list, and add a filter chip "Parts delivered". The planner then sees which waiting work orders are ready to plan, without opening each one. | product: planners handle 60 to 120 work orders a day and need "speed with many items" and "see what is still open" | UXD-010 (proposed) |
| OP-2 | In the "Plan next visit" form, preselect the technician of the first visit when that technician has a free place. That technician knows the fault and the address. | product: "A work order can need more than one visit"; story: the next visit installs the part | UXD-011 (proposed) |

## Flow

1. The planner selects the chip "Waiting for parts" in the work order list. Screen: `work-orders`. AC: none (existing).
2. The planner opens a work order. The side panel opens next to the list (UXD-004) and loads the parts order. Screen: `work-orders`. AC: AC-1.
3. The planner reads the summary line, each part with its supplier status and expected date, and "Updated" with the time of the last PartsLink update. Screen: `work-orders`. AC: AC-1, AC-2.
4. When a date moved, a part is late, or a part is cancelled, an alert at the top of the parts section tells what changed and what to do. Screen: `work-orders`. AC: AC-4.
5. When PartsLink sent no update for this order for more than 24 hours, a warning alert shows. Screen: `work-orders`. AC: AC-5.
6. When all parts are delivered, the planner selects "Plan next visit". The form opens in the side panel: date, slot, technician. Screen: `work-orders`. AC: AC-3.
7. The planner selects "Plan visit". The visit is saved, the work order changes to "Planned", and a toast shows with "Undo" for 8 seconds (UXD-003). Screen: `work-orders`. AC: AC-3.
8. The planner calls the customer with the phone number in the side panel. Screen: `work-orders`. AC: story ("inform the customer").

Decision points: at step 4, the parts status decides the branch: all delivered (plan, step 6), not all delivered (wait, "Plan next visit" disabled with the reason), a part cancelled (order a replacement in PartsLink, planning disabled). At step 5, the age of the last update decides the warning. At step 7, the free places of the technician decide save or conflict.

### Failure paths

| Step | Failure | State |
|------|---------|-------|
| 2 | The parts order does not load from the Fieldline server | error-load |
| 2 | Fieldline has no parts order for this work order | empty |
| 3 | PartsLink has data for some parts, not for all | partial |
| 4 | PartsLink cancels a part | cancelled |
| 4 | The expected date moves, or passes without delivery | date-moved |
| 5 | PartsLink sends no update for more than 24 hours | stale |
| 6 | The planner selects "Plan visit" with an empty field | error-validation |
| 7 | The save fails (server, network) | error-save |
| 7 | Another planner took the last place of the technician in that slot | conflict |
| 7 | The undo fails | error-undo |
| 2-7 | The connection is lost | offline |

## Patterns

Task facts of the side panel: items 1 to 6 parts per order (`unknown` maximum, assumed up to 10), job read and then act, frequency daily, expertise expert, context yes (the list stays visible), risk reversible (a planned visit can be undone or moved), wait: under 1 s to read Fieldline data, an external party (PartsLink) for the status, device desktop first.

| Need | Rule | Result |
|------|------|--------|
| Where the parts order shows | UXD-004, PAT-OVL-02, PAT-DETAIL-01 | In the side panel of the work order, list left, panel right |
| Parts list | PAT-LIST-04 | Simple list: part name first, then supplier status and expected date. Built with `.fl-table` (DS-7) |
| Status of an external party | PAT-FB-05 | Summary line (current status, what happens next), "Updated" time, status history timeline |
| One primary per view | PAT-ACT-01, UXD-009 | "Plan next visit" (or "Plan visit" in the form) is the one primary while the panel is open |
| Plan the visit | PAT-EDIT-02 | Short form in the side panel: date, slot, technician. Not a dialog: the planner keeps the parts list in view |
| Date | PAT-IN-06 | Date input that accepts a typed date, with the calendar picker of the browser |
| Slot | PAT-IN-01 | Radio buttons: Morning, Afternoon (DS-3) |
| Technician | PAT-IN-02 | Select with the technicians of the region who have the skill, each with free places |
| Labels and validation | PAT-IN-07, PAT-FB-07 | Labels above fields, required fields checked on submit, summary at the top for 2 or more errors |
| Summary before save | PAT-ACT-08 | One line next to "Plan visit": what changes and the undo |
| Save result | PAT-ACT-03, UXD-003, PAT-FB-06 | Save at once, toast with "Undo" for 8 seconds |
| Loading | PAT-FB-02 | Skeleton rows in the parts section, the rest of the panel stays usable |
| No parts order | no rule matches (not first use, not a filter, not done, not access) | UXD-013 (proposed) |

## Options

| Option | Structure | Task fit | Clarity | Consistency | Risk | Cost | Total |
|--------|-----------|----------|---------|-------------|------|------|-------|
| A | Parts section in the work order side panel (UXD-004): summary, parts list, "Plan next visit", history. The plan form opens in the same panel. | 3 | 3 | 3 | 3 | 3 | 15 |
| B | New screen "Parts" (the item exists in the navigation): a table of all open parts orders over all work orders, detail in a side panel, planning on the planning board. | 2 | 3 | 2 | 2 | 1 | 10 |
| C | Expand the work order row in the list to show the parts (PAT-DETAIL-03). Plan from the side panel. | 2 | 2 | 2 | 2 | 2 | 10 |

Reasons for the scores:

- A. Task fit 3: status and planning in one place, 1 click from the list (AC-3 "from the same place"). Clarity 3: the summary line is first. Consistency 3: existing side panel, existing components. Risk 3: alerts, empty, and error have one region (the parts section). Cost 3: only the proposed radio group style.
- B. Task fit 2: good to scan all orders, but planning is on another screen, which breaks AC-3. Consistency 2: a new screen and a new list. Risk 2: two places show the same status. Cost 1: a new screen.
- C. Task fit 2: the planner must expand, then open the panel to plan. Clarity 2: expanded rows compete with the list. Consistency 2: PAT-DETAIL-03 is for "no actions". Risk 2: alerts inside a row are easy to miss. Cost 2: a new row layout.

Selected: A. Reason: it is the only option where the planner reads the status and plans the visit in one place, with existing components. The scan benefit of B is offered as OP-1 (a "Parts" column in the existing list).

## Screen: work-orders

| Decision | Value | Source |
|----------|-------|--------|
| purpose | Show the parts order of a work order that waits for parts, and let the planner plan the next visit when all parts are delivered | product: story, AC-1, AC-3 |
| main-task | Find out if the parts are in, and when they are, plan the next visit | product: story |
| pattern | Master-detail: work order list left, side panel right. In the panel: parts summary, parts list (simple list), status history, plan form | decision: UXD-004 |
| layout | Page header, filter toolbar, then 2 columns: list (flexible) and side panel (420 px). Panel order: work order header and customer line, parts section (title with "Updated" time, alert, summary line, parts list, plan action), plan form (when open), status history | pattern: PAT-DETAIL-01 |
| hierarchy | 1: the summary line of the parts section ("All 3 parts delivered" or "Last part expected Thu 29 Oct") with an alert above it when something changed. 2: the parts list and the "Plan next visit" action. 3: the status history and the customer data | pattern: PAT-FB-05 |
| components | App shell, page header, toolbar and chips, data table (list and parts list), side panel, badge, alert, button (primary, ghost), form fields, radio group (proposed, DS-3), timeline, toast, skeleton, empty | system: DESIGN.md components |
| density | compact: planners use it all day with many items. The plan form in the panel uses the same compact controls (32 px) | decision: UXD-002 |
| states | default (all delivered), waiting, disabled, date-moved, cancelled, stale, partial, in-progress, error-validation, success, error-save, conflict, error-undo, error-load, empty, loading, overflow, offline | product: AC-1 to AC-5 |
| copy | Domain words: work order, part, parts order, visit, slot, technician, PartsLink. Supplier status words: Ordered, Confirmed, Shipped, Delivered, Cancelled. Plain, short, says what to do next | product: PRODUCT.md terms, AC-1 |
| motion | none. The side panel and the form appear at once | system: DESIGN.md "calm, dense, and predictable" |

### Supplier status of a part

| Supplier status | Badge | Meaning for the planner |
|-----------------|-------|-------------------------|
| Ordered | neutral | Sent to PartsLink. No date from the supplier yet: the expected date shows "No date yet". |
| Confirmed | info | The supplier confirmed the part and gave an expected date. |
| Shipped | info | The part is on the way. |
| Delivered | success | The part is in. |
| Cancelled | danger | The supplier will not deliver. The planner must act. |

The word is always in the badge (DESIGN.md: "Always with a word"). A cancelled part uses `danger`, not the `neutral` of a cancelled work order, because it blocks the work order (UXD-006).

### Business rules on the screen

- "Plan next visit" is enabled only when all parts are delivered. Else it is disabled, with the reason and the expected date under it (UXD-007).
- The expected date of a part is the date that PartsLink sends. The summary line shows the latest expected date of the parts that are not delivered.
- A moved date shows the old date in the row ("was Mon 26 Oct") and a warning alert, until the part is delivered or cancelled (UXD-012). A date that moves earlier shows an info alert.
- A part that is not delivered after its expected date shows "Late since <date>" in the row and a warning alert (UXD-012).
- More than 24 hours since the last PartsLink update of this order: warning alert (UXD-008). "Updated <time>" shows on the parts section at all times, with a hint that a change can take up to 4 hours (PRODUCT.md).
- The technician select shows only technicians of the region of the work order with the skill of the work order, each with the free places in the selected slot ("1 of 2 places free"). Full technicians show as "full" and cannot be selected. Under the select: the count of technicians not shown.

### States: work-orders

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
| default | Side panel of WO-24806. Parts section: "All 3 parts delivered. Last delivery Thu 22 Oct." Each part with the badge "Delivered" and its date. "Updated 09:42". "Plan next visit" is the primary button. History below. | Plan the next visit, read the history, call the customer, close the panel | n/a |
| waiting | Parts on the way: "Shipped" (expected Mon 26 Oct), "Confirmed" (Tue 27 Oct), "Ordered" ("No date yet"). Summary: "0 of 3 parts delivered. Last part expected Tue 27 Oct. 1 part has no date yet." Next step line: "PartsLink sends the date when the supplier confirms." "Plan next visit" disabled with the reason (PAT-FB-05). | Read the status, tell the customer the expected date | The status updates when PartsLink sends a change. No action needed. |
| disabled | 2 of 3 parts delivered. "Plan next visit" is disabled. Under it: "You can plan when all parts are delivered. Not delivered: Fan motor 230 V, expected Mon 26 Oct." | Read the status, call the customer | The button becomes enabled when PartsLink reports the last part as delivered. |
| date-moved | Warning alert: "Expected date moved. Fan motor 230 V: now Thu 29 Oct, was Mon 26 Oct. PartsLink, Fri 23 Oct 08:05. Tell the customer the new date." The row shows "Thu 29 Oct" and "was Mon 26 Oct". | Call the customer with the phone number in the panel | The alert stays until the part is delivered or cancelled. |
| cancelled | Danger alert: "PartsLink cancelled 1 part. Expansion valve EEV-24, Thu 22 Oct 16:40. Reason from the supplier: Out of stock. Order a replacement in PartsLink. The new part shows here when PartsLink sends it." Row badge "Cancelled". "Plan next visit" disabled: "1 part is cancelled. Order a replacement first." | Open the order in PartsLink (link), call the customer | When the replacement is ordered, its row shows with "Ordered". The cancelled row stays in the list as history. |
| stale | Warning alert: "No update from PartsLink for 27 hours. Last update: Thu 22 Oct 07:12. The status can be out of date. Check the order in PartsLink before you tell the customer a date." "Updated Thu 22 Oct 07:12" | Open the order in PartsLink, still read the last known status | The alert goes away when PartsLink sends an update. "Plan next visit" follows the last known status. |
| partial | Info alert: "PartsLink has no data for 1 part. Check the order in PartsLink." The row "Gasket set" shows the badge "No data" and "No date yet". "Plan next visit" disabled: "Not all parts have a status from PartsLink." | Open the order in PartsLink | The row updates when PartsLink sends data. |
| in-progress | Plan form in the panel, under the parts list: Date (Mon 26 Oct), Slot (Morning), Technician (S. Visser, 1 of 2 places free). Hint under the date: "All parts delivered on Thu 22 Oct." Hint under the select: "3 technicians without the heat pump skill are not shown." Summary: "WO-24806 changes to Planned. You can undo for 8 seconds." | Change a field, plan the visit, cancel the form | "Cancel" closes the form. Nothing is saved. |
| error-validation | After "Plan visit" with 2 empty fields: danger alert at the top of the form "2 fields need a value: Slot, Technician", each name is a link to its field. Error text at each field: "Select a slot." "Select a technician." (PAT-FB-07) | Fill the fields, plan again | Focus moves to the alert. Each link moves the focus to the field. |
| success | Toast: "Visit planned: Mon 26 Oct, morning, S. Visser. Undo". The panel shows the badge "Planned" and "Next visit: Mon 26 Oct, morning, S. Visser". The list row shows "Planned" and stays in place until the filter changes. | Undo for 8 seconds, close the panel | Undo removes the visit and sets "Waiting for parts" again. After 8 seconds: move the visit on the planning board. |
| error-save | The form stays with the values. Danger alert above the buttons: "The visit was not planned. The server did not answer. Your choices are kept. Try again." | Plan again, cancel | "Plan visit" tries again. After 3 failures the alert adds: "If this continues, contact your team lead." |
| conflict | The technician field shows an error: "S. Visser has no free place on Mon 26 Oct, morning, anymore. Another planner planned a visit there at 10:14. Select another technician or slot." The select shows S. Visser as full. | Select another technician or slot, plan again | The other values stay. |
| error-undo | Danger alert at the top of the panel: "The undo did not work. The visit on Mon 26 Oct, morning, with S. Visser is still planned. To remove it, open the planning board." | Open the planning board | The work order stays "Planned". The planner removes the visit on the planning board. |
| error-load | Danger alert in the parts section: "The parts order did not load. The server did not answer." "Try again" button. The work order header and the customer line still show. | Try again, close the panel | "Try again" loads the section again. After 3 failures: "If this continues, contact your team lead." |
| empty | Empty region in the parts section: "No parts order found". "PartsLink has no order for WO-24806. Check in PartsLink that the order has the work order number." | Open PartsLink | When PartsLink sends an order with this work order number, the section shows it (UXD-013). |
| loading | Skeleton rows in the parts section. The header, the customer line, and the list stay usable (PAT-FB-02). | Read the work order, close the panel, select another row | Not needed |
| overflow | 6 parts with long names that wrap in the row. A long customer name wraps in the header. One part "Late since Wed 21 Oct". The status words do not wrap. | Same as default | n/a |
| offline | Warning alert above the list: "No connection. You see the data of 10:02. You cannot plan until the connection is back." "Plan next visit" disabled with the same reason. | Read the data that is shown | The alert goes away and the data refreshes when the connection is back. |
| error | n/a: each error kind has its own row (error-load, error-validation, error-save, conflict, error-undo) | | |

Stress cases:

- Longest value: a part name of 60 characters and a customer name of 45 characters wrap to 2 lines (overflow). A text 35% longer still wraps and does not push the badge out of the row.
- 0 parts: empty. 1 part: the summary says "1 part". Max parts: [NEEDS EVIDENCE], the design holds 10 rows in the panel and the panel scrolls.
- Two planners plan the same technician and slot: conflict.
- Refresh or second tab: the panel opens again on the same work order from the URL (UXD-004 behavior, not changed).
- Undo fails: error-undo.
- Keyboard only and 200% zoom: see the accessibility notes. At 200% zoom, the side panel goes under the list (the order stays: list, then panel).

### Copy: work-orders

| Element | Text |
|---------|------|
| Parts section title | Parts order PL-558213 |
| Updated time | Updated 09:42 |
| Updated hint | Changes in PartsLink can take up to 4 hours to show here. |
| Parts list columns | Part, Supplier status and date (the badge, with the date on the line under it, so the part name keeps the width of the 420 px panel) |
| Date, no date | No date yet |
| Date, delivered | Thu 22 Oct (under the badge "Delivered") |
| Date, not delivered | Expected Mon 26 Oct |
| Date, moved | Expected Thu 29 Oct, then "was Mon 26 Oct" on the next line |
| Expected, late | Late since Wed 21 Oct |
| Summary, all delivered | All 3 parts delivered. Last delivery Thu 22 Oct. |
| Summary, waiting | 0 of 3 parts delivered. Last part expected Tue 27 Oct. |
| Next step line | Next: PartsLink sends the date when the supplier confirms. |
| Primary action | Plan next visit |
| Disabled reason | You can plan when all parts are delivered. Not delivered: Fan motor 230 V, expected Mon 26 Oct. |
| Date moved alert | Expected date moved. Fan motor 230 V: now Thu 29 Oct, was Mon 26 Oct. PartsLink, Fri 23 Oct 08:05. Tell the customer the new date. |
| Cancelled alert | PartsLink cancelled 1 part. Expansion valve EEV-24, Thu 22 Oct 16:40. Reason from the supplier: Out of stock. Order a replacement in PartsLink. |
| Stale alert | No update from PartsLink for 27 hours. Last update: Thu 22 Oct 07:12. The status can be out of date. Check the order in PartsLink before you tell the customer a date. |
| PartsLink link | Open order PL-558213 in PartsLink |
| Form title | Plan next visit |
| Form labels | Date, Slot, Technician |
| Slot options | Morning 08:00 to 12:00, Afternoon 12:30 to 17:00 |
| Technician option | S. Visser, 1 of 2 places free |
| Form summary | WO-24806 changes to Planned. You can undo for 8 seconds. |
| Form buttons | Plan visit, Cancel |
| Toast | Visit planned: Mon 26 Oct, morning, S. Visser. Undo |
| Error save | The visit was not planned. The server did not answer. Your choices are kept. Try again. |
| Error load | The parts order did not load. The server did not answer. |
| Empty title | No parts order found |
| Offline | No connection. You see the data of 10:02. You cannot plan until the connection is back. |
| Panel close | Close |

Language readiness: the app is English only (PRODUCT.md). Dates use the format of the existing screen ("24 Oct") with the weekday ("Mon 26 Oct"), because planners plan by work day. The code fills values into whole sentences per state; it does not join parts of sentences.

### Accessibility: work-orders

- Keyboard order: list row, then the side panel: "Close", the alert and its link, the parts list, "Plan next visit", the history. In the form: Date, Slot (arrow keys move between Morning and Afternoon), Technician, "Plan visit", "Cancel". Escape closes the form, a second Escape closes the panel.
- Focus after an action: opening the panel moves the focus to the panel heading (WO number). "Plan next visit" moves the focus to the Date field. After save, the focus moves to "Next visit" in the panel, and the toast stays while it has focus or the pointer. After a validation error, the focus moves to the error summary. Closing the panel returns the focus to the row.
- Labels and announcements: the side panel is a `region` with the name "Work order WO-24806". The parts list is a table with column headers. The badge has the status word, never color only. Alerts of the parts section use `role="status"` (not `alert`), because they show on open, not as an interruption. The error alerts after save use `role="alert"`. The toast uses `role="status"`. A disabled button has `aria-describedby` that points to its reason.
- Contrast: all text pairs come from the tokens and pass 4.5:1 (uxcheck system). Focus is visible with the teal outline (DS-1).
- Target size: buttons 32 px high, radio labels 32 px high, more than 24 by 24 px (WCAG 2.5.8).
- The 8-second undo is not the only path: the planner can move or remove the visit on the planning board (WCAG 2.2.1).

## Decisions

| ID | Decision | Status | Reason |
|----|----------|--------|--------|
| UXD-005 | The parts order of a work order shows in a parts section of the work order side panel. The planner plans the next visit in the same side panel, with a short form (date, slot, technician). | proposed | AC-3 "from the same place". Option A of the Options table. |
| UXD-006 | Badges for the supplier status of a part: Ordered neutral, Confirmed info, Shipped info, Delivered success, Cancelled danger. | proposed | A cancelled part blocks the work order and needs action, so it uses danger, not the neutral of a cancelled work order. Recommendation: add the table to the status words of DESIGN.md. |
| UXD-007 | "Plan next visit" is enabled only when all parts of the order are delivered. Else it is disabled with the reason and the expected date. | proposed | The problem of the story is that visits are planned too early. Risk: planners who want to plan ahead on a firm date cannot do it here. This is a needs-users assumption. |
| UXD-008 | "Last update" is the time of the last message from PartsLink about this order. The warning shows when it is more than 24 hours ago. | proposed | AC-5. Assumption: PartsLink sends an update for each open order a few times a day, also without a change (PRODUCT.md). If PartsLink sends only changes, the warning must use the time of the last message from PartsLink for any order. |
| UXD-009 | While the side panel is open, the panel holds the one primary action. "New work order" in the page header shows as a secondary button. With the panel open, the list hides the column "Address" (it is in the panel). | proposed | PAT-ACT-01 one primary per view. The panel is where the planner works. The list keeps the key columns at 1440 px. |
| UXD-010 | OP-1: a "Parts" column in the work order list for work orders that wait for parts, and a filter chip "Parts delivered". | proposed | Planners handle 60 to 120 work orders a day. They see which work orders are ready to plan without opening each one. |
| UXD-011 | OP-2: the plan form preselects the technician of the first visit when that technician has a free place in the selected slot. | proposed | The technician knows the fault and the address. The planner can change it. |
| UXD-012 | A moved expected date shows the old date in the row and an alert until the part is delivered or cancelled. A past expected date without delivery shows "Late since <date>". | proposed | AC-4 "sees this clearly". Text, not a strikethrough, so the old date is readable and not shown by style only. |
| UXD-013 | When Fieldline has no parts order for a work order that waits for parts, the parts section shows an empty region that says so and links to PartsLink. | proposed | No empty pattern matches this case. The planner needs the next step. |

## Assumptions

- A1: Each work order has max 1 parts order in PartsLink, with the work order number on it. A second order (for a replacement) shows as extra rows in the same section. [NEEDS EVIDENCE]
- A2: PartsLink gives an expected date per part, not only per order. If it gives one date per order, the column "Expected" moves to the summary line.
- A3: PartsLink gives a reason for a cancellation. If not, the alert says "No reason given".
- A4: PartsLink has a URL to open one order, so "Open order in PartsLink" can go to the order. If not, the link opens PartsLink and the order number shows next to it to copy.
- A5: The work order status does not change when the parts are delivered. It changes to "Planned" when the planner plans the visit.
- A6: The mockups use Fri 23 Oct 2026, 10:02, as "now", so that the data is on work days.
- A7: All planners can plan visits from the side panel. Team leads read the same panel. [NEEDS EVIDENCE]

## Open questions

- My judgment as the planner: the screen answers "can I plan now?" in one line, and the disabled button says when. The weak point is the stale warning: if PartsLink often goes quiet at weekends, the warning shows each Monday and planners learn to ignore it.
- UXD-007 blocks planning before delivery. Some planners may want to plan on a confirmed ship date to save a day. Test this with planners before the build.
- [NEEDS EVIDENCE] Does PartsLink send an update per open order without a change, and at weekends? Method: ask the PartsLink integration owner, look at 2 weeks of messages.
- [NEEDS EVIDENCE] Where is a delivered part: at the depot of the region or at the customer? This decides if the next visit can be the next work day. Method: interview 2 planners and 1 team lead.
- [NEEDS EVIDENCE] How many visits today are planned before the part is delivered? Method: data from planned visits with the result "part missing".
