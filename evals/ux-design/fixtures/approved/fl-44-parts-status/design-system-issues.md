# Design system issues: FL-44

Audit of `design-system/tokens.css`, `design-system/components.css`, and `DESIGN.md` for the screens of FL-44. `uxcheck.py system` gave 0 errors and 1 warning (SYS-FOCUS). The design system of the project stays as it is. The design lead decides on each fix. The mockups load `mockups/ds-fixes.css` after the system CSS.

| ID | Severity | Where | Rule that it breaks | Fix |
|----|----------|-------|---------------------|-----|
| DS-1 | P2 | `components.css`: no `:focus-visible` rule | WCAG 2.4.7 and 2.4.11 depend on the browser default. The default ring is not the teal focus color that `DESIGN.md` names ("Teal is for ... the focus"). On the dark navigation, teal has less than 3:1. | `ds-fixes.css`: a 2 px teal outline with a 2 px offset on `:focus-visible`. In `.fl-nav`, a white outline. |
| DS-2 | P3 | `.fl-btn:disabled` | The style has no place for the reason. `ux-states` asks that the user sees why an action is disabled. | No CSS fix. The screens put a `.fl-hint` under each disabled button with the reason. Propose a pattern note in `DESIGN.md`. |
| DS-3 | Gap | No radio group style | `PAT-IN-01` needs radio buttons for 1 of 2 options (slot: morning, afternoon). | `ds-fixes.css`: proposed `.fl-radio-group` (fieldset reset, inline options, teal accent). Needs approval of the design lead (`PRODUCT.md` constraint). |
| DS-4 | Gap | `.fl-sidepanel` has no header, close button, or section structure | Each screen that uses the side panel must improvise its header. | Mockups use `.fl-section-title`, `.fl-page-header__meta`, and a ghost button "Close". Propose a `.fl-sidepanel__header` in a later story. |
| DS-5 | P3 | `.fl-timeline__item` has no marker and no "current" style | The current status of a timeline is not different from the history. | Mockups put the current status in the summary line above the timeline, not in the timeline. No CSS fix. |
| DS-6 | P3 | `DESIGN.md` says "Panel: the one surface with elevation", but `.fl-panel` has no shadow | Naming: the guidance does not match the CSS. | List only. |
| DS-8 | P2 | `.fl-badge`, `.fl-table td.fl-num` | A status word or a date breaks over 2 lines in a narrow table ("Waiting for / parts"). The status is harder to scan. | `ds-fixes.css`: `white-space: nowrap` on the badge and on number cells. |
| DS-7 | P3 | No simple list component | `PAT-LIST-04` (simple list) has no system component. | Mockups use `.fl-table` with 3 columns for the parts list. No new component. |
