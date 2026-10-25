# Accessibility checklist (WCAG 2.2 AA)

Each item has the WCAG number, the level, and how to check it. "Tool" means a rule of `uxcheck.py review` or axe-core finds it. "Manual" means you must look.

## Design

Check these items on `ux-design.md` and the mockups.

| Item | WCAG | Check |
|------|------|-------|
| Each screen has one `h1` and a heading order without gaps. | 1.3.1 A, 2.4.6 AA | The spec names the heading of each region. |
| The focus order follows the reading order. The design says where the focus goes after each action: open, close, save, delete, an error. | 2.4.3 A | The accessibility notes name the target of the focus for each action. |
| A side panel is not a modal: Tab can leave it. A dialog is modal: it keeps the focus, Esc closes it, and the focus goes back to the control that opened it. | 2.1.2 A, 2.4.3 A | The pattern of the panel or dialog says which one it is. |
| Each status message is in a live region: `role="status"`, or `role="alert"` only for an error that blocks. Examples: saved, an error after a save, a result count, an undo toast. | 4.1.3 AA | The states table names the live region of each message. |
| A sticky header, a sticky footer, or a toast does not cover the focused element. | 2.4.11 AA | The layout says how the page keeps the focus visible under sticky parts. |
| Each control has a visible label. The accessible name starts with the visible label. | 2.5.3 A, 3.3.2 A | The copy table has a label for each input and each icon button. |
| A drag action has a single-pointer alternative, for example a menu or buttons. | 2.5.7 AA | The flow names the alternative. |
| Targets are at least 24 by 24 CSS px, or have the space around them. | 2.5.8 AA | The density decision gives the size of the smallest target. |
| An error says what is wrong and how to fix it, next to the field and in a summary for more than one error. | 3.3.1 A, 3.3.3 AA | The copy table has the error text for each error state. |
| The user does not type the same data two times in one flow. | 3.3.7 A | The flow fills known data or offers it. |
| A sign-in does not need a memory test or a puzzle. Paste and password managers work. | 3.3.8 AA | The flow of the sign-in. |
| Help (contact, chat, help page) is in the same place on each page. | 3.2.6 A | The layout of the shell. |
| Text contrast 4.5:1, large text 3:1. Borders of controls, focus rings, and icons 3:1. | 1.4.3 AA, 1.4.11 AA | `uxcheck.py system`, `brandcheck.py check` with contrast pairs. |
| Color is not the only signal: a status has a word or an icon too. | 1.4.1 A | The badges and the errors in the mockups. |
| Motion has a reason. With `prefers-reduced-motion`, motion stops or becomes a fade. Nothing flashes more than 3 times a second. | 2.3.1 A, 2.3.3 AAA | The motion decision of each screen. |
| A time limit can be turned off, changed, or extended. An undo toast stays while it has the focus or the pointer. | 2.2.1 A | The states with a timer. |
| At 320 CSS px (400% zoom) the page does not scroll to the side, except data tables and maps. | 1.4.10 AA | The layout at 390 px, and the rule for wide tables. |

## Build

Check these items on the built feature, for each state in scope.

| Item | WCAG | How | Tool rule |
|------|------|-----|-----------|
| Names, roles, labels, contrast, target size, landmarks. | 1.1.1, 1.3.1, 1.4.3, 2.5.8, 4.1.2 | Tool | `RV-AXE-*` |
| The focus shows on each control. | 2.4.7 AA | Tool, then look at the screenshot of one focused control. | `RV-FOCUS-VISIBLE` |
| No focus trap outside a modal dialog. | 2.1.2 A | Tool. In a modal dialog: Tab stays in, Esc closes, the focus goes back. | `RV-FOCUS-TRAP` |
| The focus is not under a sticky or fixed part. | 2.4.11 AA | Tool. Also scroll to the end of a long list and Tab. | `RV-FOCUS-OBSCURED` |
| The focus order follows the reading order. No `tabindex` above 0. | 2.4.3 A | Tool: a tabindex above 0 is an error, a jump back is a warning. Then read the order. | `RV-TABINDEX`, `RV-FOCUS-ORDER` |
| Animations stop with reduced motion. | 2.3.3 AAA, 2.2.2 A | Tool. | `RV-MOTION` |
| The focus moves as the design says after open, close, save, delete, and an error. | 2.4.3 A | Manual: run the action, then read `document.activeElement`. | |
| Each status message is read. The element or a parent has `role="status"`, `role="alert"`, or `aria-live`. The element was in the page before the text came. | 4.1.3 AA | Manual: read the markup of the message and its parents. | |
| The page works at 200% text size and at 320 px width. | 1.4.4 AA, 1.4.10 AA | Tool for 390 px (`RV-OVERFLOW`). Manual: set `font-size: 200%` on `html` and look for cut text. | `RV-OVERFLOW` |
| Each control works with the keyboard. Enter and Space work on buttons. Arrows work in a radio group, a list box, or tabs. Esc closes a menu or a dialog. | 2.1.1 A | Manual: run the main task with the keyboard only. | |
| A screen reader says the name, the role, and the state of each control in the main task. | 4.1.2 A | Manual: read the accessibility tree in Playwright (`page.accessibility.snapshot()`), or use NVDA or VoiceOver. Say which one you used. | |
| The language of the page is set (`lang`). | 3.1.1 A | Tool. | `RV-AXE-html-has-lang` |
