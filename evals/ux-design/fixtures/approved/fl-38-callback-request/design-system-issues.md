# Design system issues: FL-38

Audit of `design-system/tokens.css`, `design-system/components.css`, and `DESIGN.md` for the screen `repair-request-form`. `uxcheck.py system` gives 0 errors and 1 warning (SYS-FOCUS). The project files stay as they are. The mockups load `mockups/ds-fixes.css` after the system CSS. The design lead decides on each fix.

| ID | Severity | Where | Rule that it breaks | Fix |
|----|----------|-------|---------------------|-----|
| DS-1 | P2 | `components.css`: no `:focus-visible` rule | WCAG 2.4.7 and 2.4.11. The browser default ring has low contrast on the teal primary button and on the dark side navigation. DESIGN.md says that teal is for the focus, but no rule sets it. | `ds-fixes.css`: 2 px outline in `--fl-color-accent` with a 2 px offset on each interactive element. On the dark navigation, a white outline. Decision UXD-007 (proposed). |
| DS-2 | Gap | Form components | The story needs a choice of 1 of 2 to 5 options (PAT-IN-01): installation, type of installation, urgency. The system has no radio group and no fieldset style. | `ds-fixes.css`: proposed `.fl-fieldset`, `.fl-legend`, `.fl-choice`. The native radio with `accent-color`, a click target of 24 px or more. Decision UXD-006 (proposed). PRODUCT.md: a new component needs the approval of the design lead. |
| DS-3 | Gap | Links | The system has no link style. The browser default blue (#0000ee) is not a token. The error summary and the "Open WO-24806" links need one. | `ds-fixes.css`: `.fl-link` in `--fl-color-accent`, underlined. Part of UXD-006. |
| DS-4 | P3 | `.fl-btn:disabled` | `opacity: 0.5` makes the text of a disabled button about 2.3:1. WCAG does not require contrast for a disabled control, but the label "Creating work order..." must be readable. | Listed. The mockup adds the reason as text next to the button, so the user does not depend on the faded label. |
| DS-5 | P3 | `.fl-chip` | `height: 28px` and `border-radius: 14px` are not tokens. | Listed. The screen does not use chips. |
| DS-7 | P2 | `.fl-field--error` | The error style sets the red border on `.fl-input` and `.fl-select`, not on `.fl-textarea`. The Problem field has no error border. | `ds-fixes.css`: the same red border on `.fl-textarea`. |
| DS-6 | P3 | Forms in DESIGN.md | No rule for required and optional fields. | Listed. The design marks the optional fields with "(optional)" and makes all other fields required (GOV.UK rule). |
