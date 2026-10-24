# States

## Contents

- Core states (each screen)
- Business states (when they can occur)
- Stress list

## Core states (each screen)

| State | When | Design question |
|-------|------|-----------------|
| default | The moment the user starts the main task, with real data around it. For a form: empty and ready for input, focus in the first field. For a list: the list as the user finds it. | Is the main task the first thing the user sees? |
| in-progress | Optional. The user is in the middle of the task (a form partly filled, items selected). | Does the user see where they are and what is left? |
| empty | No data. Use one of 4 kinds: first use, no results, all done, no access (`PAT-EMPTY-01` to `04`) | Does the empty screen tell the next step? |
| loading | The system works. Use the wait of the task (`PAT-FB-01` to `04`) | Can the user still do something? |
| partial | Some data loaded, some failed, or a list is still growing | Does the user see what is missing? |
| error | One row per kind that can occur: `error-validation`, `error-load`, `error-save`, `error-undo`, `error-external`, `conflict` (another user or process changed the data), `session-expired`. Use the plain `error` row only when one kind can occur. | Does the text say what happened and what to do? |
| success | The action worked | Does the user see the result where they look? |
| disabled | An action is not possible now | Does the user see why, and what makes it possible? |
| overflow | Long names, many items, large numbers, long translations | Does the layout hold? |
| offline | No connection | Is work kept? What can the user still do? |

An error text has 3 parts: what happened, the cause when known, and what the user can do. Do not blame the user.

## Business states (when they can occur)

| State | When | Design question |
|-------|------|-----------------|
| no-permission | The role of the user cannot do an action or see data | Is the action hidden or disabled with the reason? Who can give access? |
| stale | Data can change while the user looks at it | Does the user see the time of the data? How does the user refresh? |
| locked | Another user edits the same item | Who has the lock, until when, and what can this user do? |
| waiting | An external party decides (another system, an authority, a partner) | Status, time of the last update, the next step, what the user can do now (`PAT-FB-05`) |
| bulk-result | A bulk action where some items fail | The count of success and failure, the failed items, a retry (`PAT-ACT-07`) |
| read-only | The item is archived, closed, or signed | Does the user see why it cannot change? Is there a path to a new version? |

## Stress list

Check each case that the story makes possible:

- The longest real name or value in the data, and a translation that is 35% longer.
- 0, 1, and the maximum number of items.
- A number with 7 digits, a negative amount, and a currency with decimals.
- Two users act on the same item at the same time.
- The user goes back, refreshes, or opens the screen in a second tab.
- The session expires during a long form.
- The undo fails, or the save succeeds for some items and fails for others.
- The user works with the keyboard only, and with 200% zoom.
