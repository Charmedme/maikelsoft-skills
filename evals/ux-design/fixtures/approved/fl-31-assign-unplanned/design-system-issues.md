# Design system issues: FL-31

Audit of `design-system/tokens.css`, `design-system/components.css`, and `DESIGN.md` for the screens of FL-31. `uxcheck.py system` gives 0 errors and 1 warning (SYS-FOCUS). The fixes are in `mockups/ds-fixes.css`. The design system stays as it is. The design lead decides on each fix (UXD-015, proposed).

| ID | Severity | Where | Rule that it breaks | Fix |
|----|----------|-------|---------------------|-----|
| DS-1 | P1 | `components.css`: no `:focus-visible` rule | WCAG 2.4.7 Focus visible, and `DESIGN.md` "Teal is for ... the focus". Planners use the keyboard a lot (`PRODUCT.md`). The browser default ring is not the same in each browser. | `:focus-visible` gets a 2 px outline in `--fl-color-accent` with a 2 px offset. |
| DS-2 | P1 | `.fl-input`, `.fl-select`: border `--fl-color-border-strong` (#b6c0cc) on white is 1.84:1 | WCAG 1.4.11 Non-text contrast (3:1 for the boundary of a control) | The border of inputs and selects uses `--fl-color-text-muted` (#556274, 6.2:1). Propose a darker `--fl-color-border-strong` to the design lead. |
| DS-3 | P1 | `.fl-toast` with a `.fl-btn--ghost` action: teal (#0b6e66) on the dark toast (#1b2430) is 2.56:1 | WCAG 1.4.3 Contrast, UXD-003 needs an "Undo" in the toast | New `.fl-toast__action`: inverse text, underlined, bold. Proposed component. |
| DS-4 | Gap | No checkbox or radio style | FL-31 needs row selection (PAT-ACT-02) and a choice of one technician and slot (PAT-IN-01) | Native checkbox and radio with `accent-color: var(--fl-color-accent)` and a 20 px size (WCAG 2.5.8 target size). |
| DS-5 | Gap | `.fl-sidepanel` has no header and footer regions | The assign panel needs a fixed footer with the summary next to the primary button (PAT-ACT-08) | Use `.fl-section-title` for the header and a plain block for the footer. No new class. Listed only. |
| DS-6 | P3 | `.fl-chip` border (#b6c0cc) on white is 1.84:1 | WCAG 1.4.11 (the chip text identifies the control, so the risk is low) | Listed only. The darker `--fl-color-border-strong` of DS-2 fixes it too. |
| DS-7 | P3 | `DESIGN.md` has no rule for a due date that is late | "Status colors are for status only" | FL-31 shows "Overdue" as a danger badge next to the date. Proposed decision UXD-010. |
