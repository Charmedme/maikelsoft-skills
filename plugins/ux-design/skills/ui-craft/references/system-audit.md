# System audit

Check the design system before you use it. A design that copies a fault of the system ships the fault to each screen. A design that ignores the system breaks consistency. So: find the faults, report them, and fix the serious ones in a visible way.

## Contents

- Checks
- What to do with a fault
- Files

## Checks

1. Run `uxcheck.py system <token files> <component files>`. It checks the contrast of the token pairs and of the component rules. It also checks gradients, the focus style, and the font sizes.
2. Read the guidance (`DESIGN.md` or the docs of the system). Compare each rule with WCAG 2.2 AA, the pattern rules of `ux-patterns`, and the method of this skill. Examples of faulty guidance: "use the placeholder as the label", "status as a colored dot", "a card around each table".
3. List the gaps: a component or a state that the design spec needs and the system does not have.

Done when each fault and gap has a row in the issues file.

## What to do with a fault

| Severity | Example | Action |
|----------|---------|--------|
| P0, P1 | Text contrast under 4.5:1, no focus style, status by color only, placeholder as the only label | Do not copy it. Fix it in `ds-fixes.css`. Write a `proposed` decision for the design lead. |
| P2 | A font size off the scale, a gradient on a button, a card around a table | Fix it in `ds-fixes.css` when the screens of the story use it. Else list it. |
| P3 | Naming, a missing token for a value that is used once | List it. |
| Gap | No error style for a radio group | Add it to `ds-fixes.css` as a proposed component. |

The design system of the project stays as it is. The design lead decides on the fixes. The mockups show the fixed version, so the review is about the right design.

## Files

- `<work>/design-system-issues.md`: a table with ID (`DS-1`), severity, where, the rule that it breaks, and the fix.
- `<work>/mockups/ds-fixes.css`: loaded after the system CSS in each mockup. Each rule has a comment with the issue ID: `/* DS-1: muted text had 2.6:1, now 4.6:1 */`.
