# Foundation: a minimal design system

Use this recipe when the project has no design system. It gives the same foundation each time for the same input. Each value is a default with a reason. A brand fact in `PRODUCT.md`, a brand file, or the human wins over a default.

## Contents

- Inputs
- Tokens
- Components
- Files
- Check

## Inputs

Collect these facts first. Write `unknown` when no source gives one, and use the default.

| Input | Source | Default |
|-------|--------|---------|
| Brand color | `PRODUCT.md`, brand file, logo | Teal `#0b6e66` |
| Brand font | brand file | The system font stack |
| Surface | the frame | operate |
| Density | the main user (see `method.md`) | compact for daily expert use, else comfortable |

## Tokens

Write the tokens as CSS custom properties on `:root`. Use these names, so each screen and each run uses the same tokens.

| Group | Tokens | Rule |
|-------|--------|------|
| Neutral | `--color-bg`, `--color-surface`, `--color-surface-sunken`, `--color-border`, `--color-border-strong`, `--color-text`, `--color-text-muted`, `--color-text-inverse` | A slate scale with a slight bias to the brand hue. `--color-text-muted` has 4.5:1 or more on `--color-surface` and `--color-bg`. |
| Accent | `--color-accent`, `--color-accent-hover`, `--color-accent-soft` | From the brand color. Darken it until white text on it has 4.5:1 or more. `--color-accent-soft` is a light tint for selection. |
| Status | `--color-success`, `--color-warning`, `--color-danger`, `--color-info`, each with a `-soft` background | Success `#1d7a3a`, warning `#8a5a00`, danger `#b42318`, info `#1f5fa8`. Each has 4.5:1 or more on its soft background. |
| Type | `--font`, `--size-xs` 12px, `--size-sm` 13px, `--size-md` 14px, `--size-lg` 16px, `--size-xl` 20px, `--size-2xl` 24px, `--weight-regular` 400, `--weight-medium` 500, `--weight-bold` 600 | Base 14px for operate screens, 16px for read screens. Max 3 weights. |
| Space | `--space-1` 4px to `--space-7` 48px (4, 8, 12, 16, 24, 32, 48) | A 4px base. |
| Shape | `--radius-sm` 4px, `--radius-md` 6px, `--shadow-overlay` | Shadow only for overlays (side panel, dialog, toast). |
| Size | `--control-height` 32px (compact) or 40px (comfortable), `--row-height` 36px or 44px | Click targets stay 24 by 24 px or more. |

## Components

Make only the components that the design spec needs. Each component follows these rules:

| Component | Rule |
|-----------|------|
| Button | Primary, secondary, ghost, danger. A visible `:focus-visible` outline. Disabled has a reason in the UI. |
| Input, select, textarea | A visible label above the field, a hint, an error text. The placeholder is an example, never the label. |
| Table | A header row, the key column first, numbers right-aligned with tabular figures. No card around the table. |
| Badge | A word and a color. Never a color only. |
| Alert, toast | An icon is optional, the text is required. A toast with an undo has a visible button. |
| Empty, skeleton | Text that says what will be here and the next step. A skeleton in the shape of the content. |
| Side panel, dialog | The only parts with a shadow. Never a dialog on a dialog. |

## Files

The foundation is a proposal until the design lead accepts it. Write it in the work folder, not in the project:

- `<work>/design-system/tokens.css`
- `<work>/design-system/components.css`
- `<work>/design-system/DESIGN.md`: the principles (3 or 4), the components table, the status words, and the rules to do and to avoid.

Add one `proposed` decision: "Use the foundation in `<work>/design-system/` as the first design system." Link these files in the mockups.

## Check

Run `uxcheck.py system <work>/design-system/tokens.css <work>/design-system/components.css`. Done when it shows 0 errors.
