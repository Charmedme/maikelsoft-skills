# Pattern decision tables

In each table, take the first rule whose condition matches. Each rule has an ID for the design spec. The rules follow the research of the Nielsen Norman Group, the GOV.UK Design System, and WCAG 2.2, and the common rules of mature design systems.

## Contents

- LIST: show a set of items
- PAGE: long lists
- FILTER: narrow a list
- DETAIL: show one item
- EDIT: change data
- OVL: page, side panel, or modal
- ACT: actions
- FB: feedback and waiting
- NAV: navigation
- IN: input controls
- ICON and TOGGLE: icons and toggles
- EMPTY: empty states
- DASH: overview screens

## LIST: show a set of items

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-LIST-01 | job is compare, or items more than 20 and job is act | Data table: one row per item, sortable columns, the key column first, numbers right-aligned | A table lets the eye move down one attribute. Cards break that line. |
| PAT-LIST-02 | items more than 100 and the user knows what to find | Search first, then results in a table | Scanning a long list is slower than typing a known name. |
| PAT-LIST-03 | job is scan, the item has an image or a visual identity, items up to 50 | Cards in a grid | The image is the identifier. |
| PAT-LIST-04 | job is scan or act, one main attribute and max 2 secondary values | Simple list: one row per item, the main attribute first | A table with 2 columns adds noise. |
| PAT-LIST-05 | device is mobile first and job is compare | List rows that show the 2 or 3 key attributes, detail on tap | A wide table does not fit a phone. |

## PAGE: long lists

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-PAGE-01 | items up to 50 | No pagination, one list | Pages hide items for no gain. |
| PAT-PAGE-02 | work list: the user returns to a position, or refers to a page | Numbered pages, a page-size control, the total count | The position stays stable and can be shared. |
| PAT-PAGE-03 | browse by time (a feed or a log), no footer actions | A "Show more" button that adds items | The user keeps the place. Infinite scroll hides the footer and breaks the back button. |

## FILTER: narrow a list

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-FILTER-01 | 1 to 3 filters, used often | Inline filters above the list (chips or a segmented control) | One click, always visible. |
| PAT-FILTER-02 | 4 or more filters | A filter panel (side panel on desktop, sheet on mobile) and chips for the active filters | Many filters need space. The chips show what is active. |
| PAT-FILTER-03 | always, with PAT-FILTER-01 or 02 | Show the result count, the active filters, and "Clear all" | The user must see why items are missing. |
| PAT-FILTER-04 | wait under 1 s | Apply each filter at once | Instant feedback. |
| PAT-FILTER-05 | wait 1 s or more | An "Apply" button | No reload after each click. |

## DETAIL: show one item

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-DETAIL-01 | the user works through a list item by item, desktop | Master-detail: list left, detail right | No navigation back and forth. |
| PAT-DETAIL-02 | the detail has its own tasks, tabs, or more than one screen of content | A page with its own URL | The item can be linked and has room. |
| PAT-DETAIL-03 | short extra information, no actions | Expand the row in place | No context switch. |

## EDIT: change data

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-EDIT-01 | one field, frequent, reversible | Inline edit with save and cancel, or autosave with undo | The fastest path for one value. |
| PAT-EDIT-02 | fields that belong together, or rules across fields | A form with sections | One place for one validation. |
| PAT-EDIT-03 | more than 7 fields where later questions depend on earlier answers, or a rare task for occasional users | A step-by-step flow with a summary before submit (one question or one topic per page) | The user sees only what applies. |
| PAT-EDIT-04 | the same change on many items | Bulk edit from the selection (PAT-ACT-02) | One change, not many. |
| PAT-EDIT-05 | 2 to 4 decisions in a fixed order on one screen (for example customer, then installation, then urgency) | Numbered sections in the order of the task. A later section shows what it needs from an earlier one. | The user sees the order and where they are. |

## OVL: page, side panel, or modal

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-OVL-01 | a short decision that blocks the flow (a confirm, max 3 fields), no need to see other content | Modal dialog | It holds the focus on one decision. |
| PAT-OVL-02 | context is yes: the user edits or reads an item while the list stays visible | Side panel | The list stays as a reference. |
| PAT-OVL-03 | long content, its own navigation, or it needs a URL | Page | A modal is not a page. |
| PAT-OVL-04 | a modal needs a second step | Put the step in the same modal, or move the task to a page | A modal on a modal loses the user. |

## ACT: actions

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-ACT-01 | always | One primary action per view. Other actions are secondary or tertiary. | Two primary buttons compete for the first look. |
| PAT-ACT-02 | the same action on more than one item | Selection with a visible count. The bulk actions show when 1 or more items are selected. "Select all" says whether it means this page or all results. | The user must know what the action changes. |
| PAT-ACT-03 | risk is reversible | Do it at once, then an undo message for 5 to 10 seconds | A confirm for a reversible action trains the user to click it away. |
| PAT-ACT-04 | risk is irreversible | A confirm dialog that names the object and the result. The button repeats the action ("Delete 3 orders"). | The user decides on facts, not on "Are you sure?". |
| PAT-ACT-05 | risk is high impact | PAT-ACT-04, plus the user types the name or the count | It stops the automatic click. |
| PAT-ACT-06 | row actions | 1 or 2 frequent actions visible, the rest in a menu | Visible actions are found faster. |
| PAT-ACT-07 | bulk action where some items can fail | Report the result per item: "47 of 50 sent. 3 failed:" with the items and a retry | A total hides the failures. |
| PAT-ACT-08 | a commit action that changes data for the user or for others | A summary of the result next to the primary button: what changes, for which items, and the undo | The user commits to a known result. |

## FB: feedback and waiting

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-FB-01 | wait under 1 s | No indicator | An indicator that flashes is noise. |
| PAT-FB-02 | wait 1 to 10 s | A skeleton or a spinner in the place of the content. Keep the rest usable. | The user knows that the system works. |
| PAT-FB-03 | wait 10 to 30 s | A progress bar with steps or a percentage, and a cancel | Above 10 s, attention goes. |
| PAT-FB-04 | wait longer than 30 s | A background job: the user can leave, and gets a message when it is done | Nobody waits that long on a screen. |
| PAT-FB-05 | an external party decides (another system, an authority, a carrier) | A status timeline: current status, time of the last update, what happens next, what the user can do now | The user cannot see the other system. The screen must. |
| PAT-FB-06 | success of an action in place | A short message near the place of the action, or a toast | Confirm without moving the user. |
| PAT-FB-07 | validation errors | Error at the field, in text, with the fix. With 2 or more errors after submit, a summary at the top with links to the fields. | The user finds and fixes each error. |

## NAV: navigation

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-NAV-01 | an app with 5 or more main sections | A side navigation, the current section marked | Room for labels that grow. |
| PAT-NAV-02 | 2 to 5 views of one object | Tabs | Tabs are views of the same thing, not steps. |
| PAT-NAV-03 | the hierarchy is 3 levels or deeper | Breadcrumbs | The user sees where they are and can go up. |

## IN: input controls

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-IN-01 | select 1 of 2 to 5 options | Radio buttons or a segmented control | All options visible. |
| PAT-IN-02 | select 1 of 6 to 15 options | A select | Saves space, the list is short. |
| PAT-IN-03 | select 1 of more than 15 options | A combobox with search | Nobody scrolls 40 options. |
| PAT-IN-04 | select more than 1 of up to 7 options | Checkboxes | All options visible. |
| PAT-IN-05 | select more than 1 of more than 7 options | A multi-select combobox with chips | Room and search. |
| PAT-IN-06 | a date | A text input that accepts typed dates, plus a calendar picker | Typing is faster for known dates. |
| PAT-IN-07 | always | A visible label above the field. A placeholder is never the only label. Validate a format on blur, a required field on submit. | The label stays when the user types. Early errors irritate. |

## ICON and TOGGLE: icons and toggles

These rules are universal: they apply on each platform. For PAT-ICON-01 to PAT-ICON-07, take the first rule whose condition matches. PAT-ICON-08 to PAT-ICON-10 apply to each icon in addition. The size values and the markup for one platform are not in this table.

Terms. A universal icon is an icon that users know without a label. Examples: home, print, the magnifying glass for search, play, and bold, italic, underline in a text editor. An ambiguous icon has many meanings in common use: history (clocks, arrows), share, `@`, cloud, link, globe.

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-ICON-01 | the control is the primary action of the view (PAT-ACT-01), or risk is irreversible or high impact | A text label. An icon before the text is optional. Not icon only. | The user must know what the main action or the destructive action does before the click. An icon does not tell that the first time. For irreversible actions this rule is judgment: no source says it directly. |
| PAT-ICON-02 | a status, an error, a warning, a success, or a required field | Text that says the state. The icon and the color only add to the text. | Color alone fails WCAG 1.4.1. An icon alone is often not understood. |
| PAT-ICON-03 | main navigation (tab bar, side navigation, top menu) | Each item has an icon and a visible text label, also on mobile. On mobile, show 4 or fewer items. When there are more, put the rest behind a button with the visible label "Menu". | Hidden navigation cuts discoverability almost in half. Mobile users need the labels too. |
| PAT-ICON-04 | an ambiguous icon, or no icon comes to mind in 5 seconds, or the meaning of the action changes with the context | Text only, or icon and text. Do not give a known icon a new meaning. | An icon that the designer cannot find in 5 seconds does not communicate. A known icon with a new meaning confuses the users who know the old meaning. |
| PAT-ICON-05 | a universal icon, and the space is small (a toolbar, player controls, a compact row) | You can use the icon only. The icon has an accessible name, a tooltip that shows on hover and on keyboard focus, and the same meaning everywhere. | Users recognize a universal icon. A label that shows only on hover is not enough on touch. |
| PAT-ICON-06 | expertise is expert, frequency is daily, and the icon passed a recognition test | You can use icon only for the frequent tools in a toolbar. The main navigation keeps its labels. | Trained daily users learn the icons. Most other users do not use the app enough to learn them. |
| PAT-ICON-07 | always, when no rule above matches | An icon and a visible text label. The label is always visible, not only on hover. | Icons are ambiguous. The label gives the meaning, and the icon helps the user find the control again. |
| PAT-ICON-08 | where to put the label (PAT-ICON-03, PAT-ICON-05 to PAT-ICON-07) | Buttons and list rows: the label next to the icon, the icon first. Icon tiles and tab bar items: the label directly below the icon. | The sources name both positions. No test compares them. |
| PAT-ICON-09 | each icon in the product | One icon set and one style: the same size, detail, and stroke weight. One icon has one meaning, and one action has one icon. Use the standard icon for a standard action. No text inside an icon. A vector icon, not an emoji or a Unicode symbol. | Users cannot rely on an icon that changes its meaning. A screen reader reads the Unicode name of an emoji ("Bust in silhouette"), not the purpose. |
| PAT-ICON-10 | each icon-only control, and each icon next to a text label | An icon-only control has an accessible name that tells the purpose ("Share", not "Arrow out of box"). Its icon has a contrast of 3:1 or more. Its target has the minimum size of the platform. An icon next to a text label is decorative and hidden from assistive technology. | WCAG 1.1.1, 1.4.11, and 4.1.2. A decorative icon that is not hidden makes the screen reader read the name two times. |
| PAT-TOGGLE-01 | a binary on/off setting in a list or a settings screen | A switch with a visible text label next to it. The label does not change with the state. For 2 named values or more than 2 values, use PAT-IN-01. | The label in the row gives the context. A switch is for binary input only. |
| PAT-TOGGLE-02 | a binary on/off mode that the user changes during the task, outside a list (mute, microphone, music, chat, favorite) | An icon toggle button with a fixed accessible name and an exposed pressed state. The name says what is on when pressed ("Mute"), not the next action ("Unmute"). The state shows by shape, not by color alone: a slashed icon, a filled or outlined icon, or a background shape. The state indicator has 3:1 against the colors next to it. A tooltip shows the name. Put the same setting with a text label in the settings screen too (PAT-TOGGLE-01). | A screen reader says "Mute, toggle button, pressed". A name that changes gives contradictory output. A state in color only fails users who cannot see the difference (WCAG 1.4.1, 1.4.11). |

## EMPTY: empty states

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-EMPTY-01 | first use, nothing exists yet | Say what will be here and why it is useful, with the primary action to create the first item | The empty screen teaches the next step. |
| PAT-EMPTY-02 | a filter or a search gives no results | Say that no item matches, show the active filters, and give "Clear filters" | The user sees the cause. |
| PAT-EMPTY-03 | all work is done | Confirm that nothing waits | Empty is good news here. |
| PAT-EMPTY-04 | no permission to see items | Say who can give access | The user can act. |

## DASH: overview screens

| ID | Condition | Pattern | Reason |
|----|-----------|---------|--------|
| PAT-DASH-01 | the user starts the day or a shift on this screen | The items that need action first, as counts that open the filtered list. Detail after that. | Summary before detail. |
| PAT-DASH-02 | a number without a target or a comparison | Add the comparison (target, last period) or remove the number | A number alone does not tell the user what to do. |
