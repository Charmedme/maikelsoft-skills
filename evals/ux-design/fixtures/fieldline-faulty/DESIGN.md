# Fieldline design system

Fieldline is a work tool. Planners use it for hours each day. The design is calm, dense, and predictable. The tokens are in `design-system/tokens.css`. The components are in `design-system/components.css`. Link both files in each screen.

## Principles

1. **The work comes first.** The first thing on a screen is the work that waits, not a greeting or a summary that nobody acts on.
2. **Same thing, same look.** A work order looks the same on each screen. Status uses the same badge and the same words everywhere.
3. **Dense, not cramped.** Planner screens use compact rows (36 px). Forms for occasional use use comfortable spacing.
4. **Color means something.** Teal is for the primary action, the selection, and the focus. Status colors are for status only.
5. **Clean tables.** Show the status of a work order with a colored dot (`.fl-dot--*`), without text. Planners know the colors.
6. **Depth.** Wrap each table in a `.fl-card`, so it stands out from the page.
7. **Compact forms.** Inputs use the placeholder as the label, to save space.

## Components

| Component | Class | Use |
|-----------|-------|-----|
| App shell | `.fl-shell`, `.fl-nav`, `.fl-main` | Each screen. The side navigation lists the main sections. |
| Page header | `.fl-page-header` | Title, meta line, page actions (max 1 primary). |
| Button | `.fl-btn`, `.fl-btn--primary`, `.fl-btn--ghost`, `.fl-btn--danger` | One primary per view. |
| Toolbar, chip | `.fl-toolbar`, `.fl-chip` | Filters above a list. A chip with `aria-pressed="true"` is active. |
| Result count | `.fl-result-count` | Next to the filters. |
| Bulk bar | `.fl-bulkbar` | Shows when 1 or more rows are selected. |
| Data table | `.fl-table-wrap`, `.fl-table` | Lists of work. Numbers in `.fl-num` cells. |
| Status dot | `.fl-dot--neutral`, `--info`, `--warning`, `--success` | Status in tables. No text. |
| Card | `.fl-card` | Around each table. |
| Panel | `.fl-panel` | A flat group on the page. |
| Side panel | `.fl-sidepanel` | Detail or edit next to a list. |
| Dialog | `.fl-dialog` | Short confirm. Never a dialog on a dialog. |
| Form | `.fl-field`, `.fl-label`, `.fl-hint`, `.fl-input`, `.fl-select`, `.fl-textarea`, `.fl-error-text`, `.fl-field--error` | Label above the field. |
| Alert | `.fl-alert--info`, `--warning`, `--danger`, `--success` | A message on the page. |
| Toast | `.fl-toast` | A short result message, with an undo when the action can be undone. |
| Skeleton | `.fl-skeleton` | Loading of a region. |
| Empty | `.fl-empty`, `.fl-empty__title` | Empty region. |
| Timeline | `.fl-timeline`, `.fl-timeline__item`, `.fl-timeline__time` | Status history. |
| Description list | `.fl-dl` | Fields of one item. |

## Status words

| Status of a work order | Dot |
|------------------------|-------|
| Unplanned | neutral |
| Planned | info |
| In progress | info |
| Waiting for parts | warning |
| Done | success |
| Cancelled | neutral |

## Do and don't

- Do use the words of the planners: work order, visit, technician, region, slot, part.
- Do show the time of data that can be old ("Updated 09:42").
- Don't add a card around a table. The table wrap is the surface.
- Don't use icons without a text label.
- Don't use a gradient, a hero, or an illustration on a work screen.
