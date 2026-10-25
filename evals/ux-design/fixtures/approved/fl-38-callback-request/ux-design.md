# UX design: FL-38 Register a repair request during a phone call

Mode: full
Surface: operate

## Sources

| Source | Found | Used for |
|--------|-------|----------|
| `docs/work/fl-38-callback-request/story.md` | yes | problem, acceptance criteria AC-1 to AC-6 |
| `PRODUCT.md` | yes | users (customer service), terms, contract rule of 2 work days, skills (heat pump, air conditioning, gas boiler), WCAG 2.2 AA, no new components without the design lead |
| `DESIGN.md` | yes | principles, components, status words, do and don't |
| `design-system/tokens.css` | yes | colors, type scale, space scale, control height |
| `design-system/components.css` | yes | shell, page header, panel, form, alert, table, badge, button |
| `docs/ux/decisions.md` | yes | UXD-001 (ID first), UXD-002 (compact rows), UXD-003 (undo toast), UXD-004 (detail in side panel) |
| `screens/work-orders.html` | yes | navigation items, real data (customers, addresses, WO-24806 for A. Yilmaz) |
| `design-system-issues.md` (this folder) | yes, made in this run | audit: DS-1 focus, DS-2 radio group, DS-3 link |
| `docs/ux/research-backlog.md` | no, made in this run | assumptions to test |
| Research with customer service | no | `[NEEDS EVIDENCE]`: no interviews, call data, or analytics exist |

## Frame

| Question | Answer | Evidence |
|----------|--------|----------|
| Problem | Customer service writes a repair request on paper or in an email. A planner types it into Fieldline later. Requests get lost, or have a wrong address, and the repair can miss the first visit within 2 work days. | checked: story, PRODUCT.md (contract rule) |
| Users | Customer service employees. They take calls all day and create repair work orders while the customer is on the line. They listen and type at the same time, so they enter data in the order that the caller gives it, and they must read data back (address, ID). They use this screen many times each day, so they learn it fast and use the keyboard. | checked: PRODUCT.md (users table), judgment: keyboard use and read-back follow from a phone task done all day |
| Users (device) | A desktop browser with a large screen, like the planners. | needs-users: PRODUCT.md gives the screen only for planners |
| Users (callers) | Most callers are existing customers with a registered installation, so the screen starts with the customer search. | needs-users: no call data |
| Value | The request is in Fieldline when the call ends. The planner sees it at once as an Unplanned repair. The caller hears the work order ID and the confirmed address. No retyping, no lost paper. | checked: story; needs-users: the size of the time gain |
| Success | Each repair call gives a work order with status Unplanned during the call (AC-4). The address is confirmed with the caller (story: wrong address). No second open repair for the same installation without a choice (AC-6). Team view: fewer repairs that miss the 2 work-day rule. | checked: AC-1 to AC-6; `[NEEDS EVIDENCE]` baseline of missed first visits |
| Constraints | English only. WCAG 2.2 AA. No new design-system component without the design lead. Plain HTML and CSS prototype. Work order IDs like WO-24817. Status words from DESIGN.md. A repair needs a technician with the skill of the installation. | checked: PRODUCT.md, DESIGN.md, CLAUDE.md |

Challenge: the story names a form, and the problem (lost and wrong requests) is a data-entry problem at the moment of the call. A form in Fieldline fits the problem. No problem mismatch. The story names a solution. The real cause of wrong addresses (a typo, or the wrong address of a known customer) is not confirmed: the design covers both with a read-back hint and a postcode check.

## Opportunities

| ID | Opportunity | From | Decision |
|----|-------------|------|----------|
| OP-1 | For a new customer, find the street and the town from the postcode and the house number. The employee types 2 short values and reads the found address back. | product: story ("requests have a wrong address") | UXD-009 (proposed) |
| OP-2 | When an open repair exists for the installation, "Add this call to WO-24806" adds the call as a note to the open work order, so the employee does not make a second work order or a paper note. | product: story AC-6 and "requests get lost" | UXD-010 (proposed) |

## Flow

1. The employee selects **Repair request** in the side navigation. The form opens empty, with the focus in **Find a customer**. Screen: `repair-request-form`. AC: AC-1.
2. The employee types a name, a postcode, or a phone number that the caller gives. Fieldline shows the matching customer addresses in a table. Screen: `repair-request-form`. AC: AC-1.
3. The employee selects the customer address. Or, when no customer matches, the employee selects **New customer** and enters the name, the address, and the phone number. Fieldline checks the postcode when the employee leaves the field. Screen: `repair-request-form`. AC: AC-1, AC-5.
4. The employee reads the address back to the caller. Screen: `repair-request-form`. AC: AC-1.
5. The employee selects the installation at the address. Fieldline checks for an open repair for that installation and shows it at once. Screen: `repair-request-form`. AC: AC-2, AC-6.
6. The employee describes the problem. Screen: `repair-request-form`. AC: AC-2.
7. The employee selects the urgency: not urgent, or one of the 3 urgent reasons. Screen: `repair-request-form`. AC: AC-3.
8. The employee reads the summary next to the button and selects **Create work order**. Fieldline checks the required fields and the postcode. Screen: `repair-request-form`. AC: AC-5, AC-6.
9. Fieldline creates the repair work order with status Unplanned and shows its ID in large text. The employee reads the ID to the caller and selects **New repair request** for the next call. Screen: `repair-request-form`. AC: AC-4.

Decision points:

- Step 3: the search finds the customer address (select it), or it finds nothing (New customer). A customer with 2 addresses shows 2 rows.
- Step 5: the address has registered installations (select one), or it has none, or they do not load (select the type: heat pump, air conditioning, gas boiler).
- Step 5: an open repair exists for this installation (state `duplicate`), or not ("No open repair for this installation").
- Step 8: all fields are valid (create), or not (`error-validation`).

Paths that are not designed: a caller who reports 2 installations in one call (the employee makes 2 requests, one after the other), and a caller who is not the customer (a tenant or a neighbour). Both are open questions.

### Failure paths

| Step | Failure | State |
|------|---------|-------|
| 2 | No customer matches the search | empty |
| 2 | The search does not answer | error-load |
| 2 | The search finds many customers | overflow |
| 3 | The postcode is not valid, or a required field is empty | error-validation |
| 5 | The installations of the address do not load, or the open-repair check does not answer | partial |
| 5 | An open repair exists for this installation | duplicate |
| 8 | A required field is empty or the postcode is not valid at the save | error-validation |
| 8 | The save fails | error-save |
| 8 | A colleague created a repair for the same installation during the call | conflict |
| 8 | The session ended during the call | session-expired |
| 1 to 8 | The connection is lost | offline |

## Options

| Option | Structure | Task fit | Clarity | Consistency | Risk | Cost | Total |
|--------|-----------|----------|---------|-------------|------|------|-------|
| A | One page with 3 numbered sections in the order of the call (1 Customer, 2 Installation and problem, 3 Urgency), and a summary column on the right with the deadline and the one primary button. | 3: all fields on one page, the employee jumps to any section when the caller changes the order | 3: the first section is the search, the button and the deadline are always in view | 2: one proposed part, the radio group (DS-2), and a new navigation item | 3: errors at the field and in a summary, the duplicate shows in section 2 and next to the button | 2: one small new part (radio group) | 13 |
| B | A step-by-step flow: one page for the customer, one for the installation and problem, one for the urgency, then a check page and Create. | 1: 4 page changes per call, and going back when the caller corrects the address costs a step | 3: one question per page | 2: the same radio group, and a step pattern that Fieldline does not have | 2: the duplicate shows on page 2, far from the save on page 4 | 2: radio group and step navigation | 10 |
| C | A side panel of 420 px next to the work order list (like UXD-004), with the same fields in one column and the button at the bottom. | 2: one place, but the employee scrolls the narrow panel to reach the button | 2: the list behind competes with the form, and customer service does not use the list during a call | 3: side panel of UXD-004 | 1: the search results do not fit 420 px (name, address, phone), and errors are out of view under the scroll | 3: existing side panel | 11 |

Selected: A. Reason: A has the highest score. The employee does not change page during the call, can correct any section in any order, and always sees the deadline, the duplicate warning, and the one button.

## Screen: repair-request-form

| Decision | Value | Source |
|----------|-------|--------|
| purpose | Register a repair request while the customer is on the phone, so that a repair work order with status Unplanned exists when the call ends. | product: story, AC-4 |
| main-task | Find or enter the customer, select the installation, describe the problem, and create the work order in one pass. | product: story AC-1 to AC-4 |
| pattern | Numbered sections in the order of the call. Search first, then results in a table. Radio buttons for the installation and the urgency. Errors at the field, plus a summary after the save with 2 or more errors. A summary of the result next to the button. | pattern: PAT-EDIT-05, PAT-LIST-02, PAT-IN-01, PAT-IN-07, PAT-FB-07, PAT-ACT-08 |
| layout | App shell with side navigation. Page header (title and the time of the call). Then 2 columns: on the left, 3 numbered sections (1 Customer, 2 Installation and problem, 3 Urgency); on the right, a summary column of 380 px with the deadline, the result, and the button. The summary column is sticky, so the button stays in view when the page scrolls. At a narrow width, the summary goes under section 3. Page, not a side panel: the employee does not need the list behind. | decision: UXD-005 |
| hierarchy | 1: section 1, the search field with the focus (top left). 2: the open section that needs input next (installation, problem). 3: the summary column with "First visit by" and **Create work order**. | pattern: PAT-EDIT-05 |
| components | App shell, page header, panel (one per section and the summary), form field with label and hint, input, textarea, error text, data table (search results), alert (duplicate, errors, partial, offline), badge (status Unplanned, Urgent, Open repair status), description list (selected customer, summary, result), button (one primary), toast (Clear form with Undo), proposed radio group | system: DESIGN.md components; decision: UXD-006 |
| density | Comfortable for the form fields (system form spacing: 32 px controls, 16 px between fields), because DESIGN.md gives comfortable spacing to forms. Compact 36 px rows for the search results table, as for each table. | system: DESIGN.md principle 3, tokens `--fl-control-height`, `--fl-row-height-compact` |
| states | default, search-results, in-progress, new-customer, duplicate, empty, loading, partial, error-validation, error-load, error-save, conflict, session-expired, success, disabled, overflow, offline | product: AC-1 to AC-6 |
| copy | Words of the domain: customer, address, installation, heat pump, air conditioning, gas boiler, repair, work order, Unplanned, urgent, first visit. Main labels: "Find a customer", "New customer", "Installation", "Problem", "Urgency", "Create work order", "New repair request". Short, direct, no blame. | product: PRODUCT.md terms, story |
| motion | none. Results, alerts, and the success panel show at once. | system: DESIGN.md "calm, predictable" |

Business rules at the point of decision:

- "First visit by Tue 27 Oct (2 work days)" in the summary, next to the button (contract rule, PRODUCT.md). The date counts 2 work days after the day of the call, Monday to Friday.
- "Only these 3 reasons make a request urgent" at the urgency choice (AC-3).
- "Open repair WO-24806 for this heat pump" in section 2 and next to the button (AC-6).
- "Read the address to the caller" at the selected customer (story: wrong address).
- "Read this ID to the customer" at the new ID (AC-4).

### States: repair-request-form

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
| default | Empty form. The focus is in **Find a customer** (hint: name, postcode, or phone number). Section 2 says "Select a customer first". The summary shows "First visit by Tue 27 Oct" and **Create work order**. | Type in the search, or select **New customer**. | n/a |
| search-results | Under the search field: "3 customer addresses match "smit"" and a compact table with customer, address, postcode, phone, and installations. One row per customer address. | Select a row with the mouse or with Tab and Enter. Change the search. Select **New customer**. | The search updates when the employee types. |
| in-progress | Section 1 shows the selected customer as a description list, with "Read the address to the caller" and **Change**. Section 2 lists the installations at the address as radio buttons, and "No open repair for this installation". The problem text. Section 3 with "Not urgent". The summary lists customer, installation, urgency, and the result. | Change any section in any order. Create the work order. **Clear form** (toast with **Undo**, UXD-003). | Each section can change until the save. |
| new-customer | Section 1 shows the fields Name, Postcode, House number, Street, Town, and Phone number. Section 2 shows the 3 installation types as radio buttons, because a new address has no registered installation. | Enter the fields. Select **Find a customer** to go back to the search with the entries kept. | The postcode check runs when the employee leaves the field. |
| duplicate | A warning alert in section 2: "Open repair for this heat pump: WO-24806, Waiting for parts, technician S. Visser, created 19 Oct." with the link **Open WO-24806**. The summary repeats "Open repair WO-24806" and the button says **Create second work order**. | Open WO-24806 in a new tab to read it. Select another installation. Create a second work order. | The warning goes away when the employee selects another installation. |
| empty | Under the search: "No customer matches "Wetering 12"." and "Check the spelling, or search by postcode or phone number." with the button **New customer**. | Change the search. Select **New customer**. The search text goes into the Name field when it is not a postcode or a number. | Pattern PAT-EMPTY-02. |
| loading | 3 skeleton rows in the place of the results. The search field and the other sections stay usable. | Keep typing. Fill the other sections. | When the search takes more than 10 seconds, the state changes to `error-load`. |
| partial | The customer is selected, but the installations did not load: "The installations at this address did not load. Select the type of installation." The 3 types show as radio buttons, and **Try again**. The open-repair check runs at the save when it did not run before. | Select the type and continue. Select **Try again**. | **Try again** loads the installations. The duplicate check at the save gives the `duplicate` or `conflict` state when it finds an open repair. |
| error | n/a: each error kind has its own row (error-validation, error-load, error-save, conflict, session-expired) | | |
| error-validation | After **Create work order**: an error summary at the top of the form, "Fix 3 fields before you create the work order", with a link to each field. Each field has a red border and the error text under the label. A postcode that is not valid shows its error when the employee leaves the field. | Select a link to go to the field. Fix the fields. Create again. | The summary and the field errors go away when the employee fixes them and creates again. |
| error-load | In section 1, a danger alert: "The customer search did not work. Fieldline did not answer." with **Try again**. **New customer** stays available. | Try again. Enter the customer as new when the caller cannot wait. | **Try again** runs the same search. |
| error-save | A danger alert in the summary: "The work order is not created. Fieldline did not answer. Your entries are kept. Select Create work order to try again." | Create again. Keep talking to the caller. | When it fails again, the alert adds: "Write down the request and give it to a planner." |
| conflict | At the save, Fieldline finds a new open repair: "A colleague created WO-24834 for this heat pump at 14:12, during this call." The work order is not created. The button says **Create second work order**. | Open WO-24834. Create a second work order. Clear the form. | The employee decides with the facts. Nothing is created without a second click. |
| session-expired | A warning alert at the top: "Your session ended. Sign in again to create the work order. Your entries are kept on this screen." with **Sign in again** (opens in a new window). **Create work order** is not available, with the reason next to it. | Sign in again. Keep reading the entries to the caller. | After the sign-in, the button is available again and the entries are the same. |
| success | The form is replaced by a result panel: "Work order created", the ID **WO-24832** in large mono text, "Read this ID to the customer.", status badge Unplanned, the customer, the address, the installation, the urgency, and "First visit by Tue 27 Oct". The focus is on the heading. | Read the ID. Select **New repair request** (primary). Select **Open work order** (opens the side panel on Work orders, UXD-004). | n/a |
| disabled | While Fieldline creates the work order, the button says "Creating work order..." and is not available. The text next to it says "Do not close this screen". | Wait (less than 1 second expected). | When it takes more than 10 seconds, the state changes to `error-save`. |
| overflow | A search for "Jansen" shows "Showing 10 of 48 customer addresses. Add the postcode or the house number to see fewer.". A long name ("Vereniging van Eigenaren Appartementencomplex Jansenhof De Wetering-Noord") wraps in the table and in the summary. Postcodes and phone numbers do not wrap. A long problem text grows the textarea and shows "774 of 1000 characters" (the count shows from 700 characters). | Narrow the search. Read the long name. | The table wraps the text, it does not cut it. |
| offline | A warning alert at the top: "No connection. Your entries stay on this screen. You can create the work order when the connection is back." The search is not available. The button is not available, with the reason next to it. | Keep entering the problem and the urgency. Wait for the connection. | When the connection is back, the alert goes away and the button is available. |

Stress cases: the longest name and a 48-match search are in `overflow`. Two users on the same installation is `conflict`. The session ends during a long call: `session-expired`. Keyboard only and 200% zoom: see the accessibility notes. At 200% zoom, the summary goes under section 3 and keeps the button after the urgency.

### Copy: repair-request-form

| Element | Text |
|---------|------|
| Navigation item | Repair request |
| Page title | New repair request |
| Page meta | Customer service · Fri 23 Oct 2026, 14:06 |
| Section 1 title | 1. Customer |
| Search label | Find a customer |
| Search hint | Name, postcode, or phone number |
| Search result count | 3 customer addresses match "smit" |
| Results columns | Customer · Address · Postcode · Phone · Installations |
| New customer button | New customer |
| Back to search | Find a customer |
| Selected customer hint | Read the address to the caller. |
| Change customer button | Change |
| New customer fields | Name · Postcode · House number · Street · Town · Phone number |
| Postcode hint | For example 3861 AJ |
| House number hint | With the addition, for example 12A |
| Section 2 title | 2. Installation and problem |
| Section 2 before customer | Select a customer first. Then you see the installations at the address. |
| Installation legend | Installation |
| Installation type legend | Type of installation |
| Installation options | Heat pump · Air conditioning · Gas boiler |
| No open repair | No open repair for this installation. |
| Duplicate alert | Open repair for this heat pump: WO-24806, Waiting for parts, technician S. Visser, created 19 Oct. Read it before you create a second work order. |
| Duplicate link | Open WO-24806 |
| Problem label | Problem |
| Problem hint | What the customer sees or hears, an error code on the display, and since when. |
| Character count | 774 of 1000 characters |
| Section 3 title | 3. Urgency |
| Urgency legend | Urgency |
| Urgency hint | Only these 3 reasons make a request urgent. |
| Urgency options | Not urgent · Urgent: no heating · Urgent: no cooling in a heat wave · Urgent: leak |
| Summary title | Summary |
| Summary rows | Customer · Address · Installation · Urgency · Result |
| Result row | New repair work order, status Unplanned |
| Deadline | First visit by Tue 27 Oct (2 work days) |
| Primary button | Create work order |
| Primary button, duplicate or conflict | Create second work order |
| Secondary button | Clear form |
| Toast after Clear form | Form cleared. · Undo |
| Saving | Creating work order... · Do not close this screen. |
| Success heading | Work order created |
| Success hint | Read this ID to the customer. |
| Success actions | New repair request · Open work order |
| Empty search | No customer matches "Wetering 12". · Check the spelling, or search by postcode or phone number. |
| Overflow count | Showing 10 of 48 customer addresses. Add the postcode or the house number to see fewer. |
| Error summary | Fix 3 fields before you create the work order |
| Error: no customer | Select a customer, or enter a new customer. |
| Error: name | Enter the name of the customer. |
| Error: postcode empty | Enter the postcode. |
| Error: postcode not valid | Enter a postcode with 4 numbers and 2 letters, for example 3861 AJ. |
| Error: house number | Enter the house number. |
| Error: street | Enter the street. |
| Error: town | Enter the town. |
| Error: phone | Enter a phone number for the visit. |
| Error: installation | Select the installation. |
| Error: problem | Describe the problem. |
| Error: search | The customer search did not work. Fieldline did not answer. · Try again |
| Partial | The installations at this address did not load. Select the type of installation. · Try again |
| Error: save | The work order is not created. Fieldline did not answer. Your entries are kept. Select Create work order to try again. |
| Error: save again | Write down the request and give it to a planner. |
| Conflict | A colleague created WO-24834 for this heat pump at 14:12, during this call. The work order is not created. |
| Session expired | Your session ended. Sign in again to create the work order. Your entries are kept on this screen. · Sign in again |
| Button reason, session | Sign in again to create the work order. |
| Offline | No connection. Your entries stay on this screen. You can create the work order when the connection is back. |
| Button reason, offline | Not possible without a connection. |
| Urgent badge | Urgent |

### Accessibility: repair-request-form

- Keyboard order: side navigation, then **Find a customer**, the result rows (each customer name is a button), **New customer**, section 2 (installation radio group: one Tab stop, arrow keys inside), Problem, section 3 (urgency radio group), then **Create work order** and **Clear form** in the summary. The visual order and the Tab order are the same.
- Focus on load: in **Find a customer**. After a customer is selected: on the first installation radio button. After **New customer**: in Name. After **Create work order** with errors: on the error summary (`role="alert"`, `tabindex="-1"`). After success: on the heading "Work order created" (`tabindex="-1"`), so a screen reader reads the ID. After **Clear form**: in **Find a customer**.
- Labels: each field has a visible label above it (PAT-IN-07). Each radio group is a `fieldset` with a `legend`. Hints and errors connect with `aria-describedby`. Fields with an error get `aria-invalid="true"`. The section titles are `h2` headings.
- Announcements: the result count is in an `aria-live="polite"` region. The duplicate alert and the partial alert use `role="status"`. The save error, the conflict, and the session alert use `role="alert"`.
- Color is never the only signal: errors have text, the badges have a word, the duplicate alert names the work order.
- Targets: the radio choices have a click target of 24 px or more (WCAG 2.5.8). The focus ring is 2 px teal (DS-1).
- Zoom: at 200% the 2 columns become 1 column. The summary goes after section 3. No horizontal scroll, except inside the results table.
- The ID uses the mono font with a space-free format (WO-24832), so a screen reader and the employee read it the same way.

## Decisions

| ID | Decision | Status | Reason |
|----|----------|--------|--------|
| UXD-005 | The repair request form is a page of its own, `repair-request-form`, with the item "Repair request" in the side navigation. It is not a side panel on Work orders. | proposed | Customer service does not need the list during a call (option C scored 11). One click starts each call. Replaces nothing. |
| UXD-006 | Add a radio group (`.fl-fieldset`, `.fl-legend`, `.fl-choice`) and a link style (`.fl-link`) to the design system. Native inputs with `accent-color`, targets of 24 px or more. | proposed | PAT-IN-01 needs radio buttons, and the system has none (DS-2, DS-3). PRODUCT.md: the design lead must approve a new component. |
| UXD-007 | Each interactive element shows a 2 px teal focus outline with a 2 px offset (white on the dark navigation). | proposed | DS-1. DESIGN.md says that teal is for the focus. WCAG 2.4.7. |
| UXD-008 | An urgent repair shows the badge "Urgent" (danger) next to its status badge, in the summary, in the result, and in the work order lists. | proposed | AC-3 needs a mark that planners see. Urgency is not a status, so it is a second badge, and the status words of DESIGN.md stay the same. |
| UXD-009 | OP-1: for a new customer, Fieldline finds the street and the town from the postcode and the house number, and fills them. The employee can change them. | proposed | Story: wrong addresses. Needs an address service that PRODUCT.md does not name. Recommendation: accept, when an address service is available. |
| UXD-010 | OP-2: in the duplicate state, **Add this call to WO-24806** adds the problem text and the time of the call as a note to the open work order. No new work order. | proposed | Story: requests get lost, AC-6. Recommendation: accept, because a customer who calls about an open repair is a likely reason for a second call. |

Assumptions (taken because nobody can answer in this run):

- A-1: 2 work days count Monday to Friday after the day of the call. Public holidays are not known. A call on Fri 23 Oct gives "First visit by Tue 27 Oct".
- A-2: Urgent does not change the deadline of 2 work days. It marks the work order for the planners (UXD-008). `[NEEDS EVIDENCE]`
- A-3: The postcode is Dutch: 4 numbers and 2 letters (the addresses in Fieldline are Dutch towns). A space is optional, Fieldline stores "3861 AJ".
- A-4: One search result is one customer address. A customer with 2 addresses (for example a company with 2 offices) shows 2 rows.
- A-5: The installations of an address are registered in Fieldline when Fieldline installed or maintains them. A new address has none, so the employee selects the type.
- A-6: An open repair is a repair work order with status Unplanned, Planned, In progress, or Waiting for parts.
- A-7: The form keeps the entries in the browser when the session ends or the connection is lost.
- A-8: All fields are required. Urgency has the default "Not urgent", because most repairs are not urgent (judgment).
- A-9: The problem text has a maximum of 1000 characters.
- A-10: The mockup data (postcodes, phone numbers, WO-24832, WO-24834) is sample data in the format of PRODUCT.md, not real data.

## Open questions

- Judgment: as the employee with a caller on the line, the order of section 1 to 3 follows the call, and nothing forces a page change. The weakest moment is the new customer: 6 fields to type while the caller spells the street. OP-1 removes most of that risk.
- Judgment: the duplicate warning shows only after the installation is selected. A caller who says "I called last week" is better served when the open repair shows already at the customer. I recommend showing open repairs in the result rows too, when the product owner accepts OP-2.
- Judgment: "Create second work order" makes the second click a real choice, but it does not stop a fast employee. The team must see in the data how often second work orders happen.
- [NEEDS EVIDENCE] Which data does the caller give first: name, postcode, or phone number? Method: listen to 20 calls with customer service.
- [NEEDS EVIDENCE] Does urgent shorten the deadline (A-2)? Method: ask the product owner and read the contract.
- [NEEDS EVIDENCE] Which device and screen does customer service use? Method: ask the team lead of customer service.
- [NEEDS EVIDENCE] How does customer service handle a caller who is not the customer (a tenant), and a call about 2 installations? Method: 5 interviews with customer service.
- [NEEDS EVIDENCE] What is the fallback when Fieldline is down during a call? Method: ask the product owner.
