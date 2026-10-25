# Fieldline fixture app (FL-31 assign unplanned)

A small working web app that implements the approved UX design of FL-31 (`ux-design.md` and mockups of iter5-opus). It is a test fixture for the UX review tool.

## Run

```
python3 server.py [port]    # default 8123, Python 3 standard library only
```

Open http://localhost:8123/. All data is in memory and resets when the server starts. The scenario clock is Mon 26 Oct 2026, 09:42 (it runs with real time from the start).

Query option: `/?poll=500` sets the interval (ms) for the connection check, the "new work orders" check and the free-places refresh. Default 30000.

## API

| Method and path | Result |
|-----------------|--------|
| GET /api/health | `ok` |
| GET /api/work-orders | Unplanned work orders of region Central: `{region, today, nextWorkDay, updatedAt, seq, workOrders[]}`. `?status=open\|waiting\|repairs` gives the other status views. |
| GET /api/work-orders/changes?since=SEQ | `{newCount}`: new unplanned work orders since the list was loaded |
| GET /api/technicians?skills=A,B&date=YYYY-MM-DD[&slot=morning] | All technicians of the region with `skills`, `hasSkills` and `free: {morning, afternoon}` (max 2 visits per slot; weekends 0). `skill=` also works. |
| POST /api/assignments | Body `{workOrderIds, technicianId, slot, date?, expectedFree?}`. Returns `{assignmentId, assigned[], failed[{id, reason}]}`. Assigns in the given order until the slot is full (partial success). Returns 409 `{error:"conflict", free, changedAt}` when `expectedFree` is not the current free count. |
| POST /api/assignments/undo | Body `{assignmentId}`. Returns 200. |
| GET or POST /api/_debug/reset | Test helper: reset the data to the seed. |
| POST /api/_debug/book | Test helper: another planner takes a place `{technicianId, date, slot}`. |
| POST /api/_debug/add-work-orders | Test helper: add `{n}` new unplanned work orders. |

## Test ids

| Element | Selector |
|---------|----------|
| Status chips | `status-unplanned`, `status-open`, `status-waiting`, `status-repairs` |
| Filter chips | `filter-type-repair`, `filter-skill-heat-pump`, `filter-due-overdue`, ... (`filter-<group>-<value>`) |
| Result count, clear filters | `result-count`, `clear-filters`, `empty-clear-filters` |
| Rows, row checkboxes | `wo-row`, `wo-check` (both with `data-wo="WO-24817"`), `select-all`, `wo-table` |
| Bulk bar | `bulkbar`, `bulk-count`, `assign-open`, `clear-selection` |
| Assign panel | `assign-panel`, `assign-selected`, `show-all-selected`, `assign-date`, `pick-date`, `tech-list`, `tech-row`, `slot-radio` (`data-tech`, `data-slot`), `slot-full`, `not-shown`, `no-technician`, `next-work-day`, `assign-summary`, `due-warning`, `assign-hint`, `assign-confirm`, `assign-cancel`, `assign-close`, `tech-loading`, `techs-error`, `retry-techs`, `conflict-alert` |
| Results | `result-alert`, `result-close`, `toast`, `toast-undo`, `error-undo`, `undo-again`, `undo-error-close` |
| Page states | `list-loading`, `error-load`, `retry-load`, `empty`, `empty-all-done`, `show-all-open`, `error-save`, `retry-save`, `offline-alert`, `stale-alert`, `refresh-list` |

## States and how to produce them

`walk.json` produces each state below (each state starts with `/api/_debug/reset`).

| Design state | How |
|--------------|-----|
| default | Open `/` |
| in-progress | Select 3 rows |
| disabled | Select WO-24817, WO-24822, WO-24840, then "Assign to technician" |
| assign-loading | GET `/api/technicians?**` is held (never answers) |
| assign-ready | As disabled, then K. Bos, morning |
| assign-no-technician | Select WO-24817 and WO-24828 (two skills), open the panel, type date `Sat 31 Oct 2026` |
| empty | Filters Repair and Gas boiler |
| empty-all-done | GET `/api/work-orders` returns an empty `workOrders` list |
| loading | GET `/api/work-orders` is held |
| partial | GET `/api/technicians?**` returns 500 |
| error-load | GET `/api/work-orders` returns 500 (or no answer after 10 s) |
| error-save | POST `/api/assignments` returns 500 |
| error-undo | POST `/api/assignments/undo` returns 500, after a successful assign |
| conflict | POST `/api/assignments` returns 409 `{"free":1,"changedAt":"09:51"}`; real case: `/api/_debug/book` while the panel is open |
| success | Assign WO-24817 and WO-24822 to K. Bos, morning |
| bulk-result | Assign WO-24817, WO-24822, WO-24840 to K. Bos, morning (real partial success) |
| stale | GET `/api/work-orders/changes?**` returns `{"newCount":4}`, open `/?poll=500` |
| overflow | "Select all" (14), open the panel: 3 listed and "Show all, 11 more"; long customer and technician names |
| offline | GET `/api/health` aborts, open `/?poll=500` (also `navigator.onLine` false) |

Not implemented: none of the design states. `session-expired`, `no-permission` and `locked` are n/a in the design.
