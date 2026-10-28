# Web values for icons and toggles

The rules in `patterns.md` are universal. This file gives the values and the markup for web screens (HTML mockups and web apps). Read it when a web design has icons or toggles.

## Target size

| Case | Value | Source |
|------|-------|--------|
| Each interactive icon (WCAG AA) | At least 24 x 24 CSS px. A smaller target passes only when a circle of 24 CSS px on its center does not touch the circle of another target. | WCAG 2.2 SC 2.5.8 |
| Touch screens (mobile first, or a frame of 480 px or less) | Aim for 44 x 44 CSS px. | WCAG 2.2 SC 2.5.5 (AAA), Apple HIG (44 x 44 pt) |

The target can be larger than the glyph. A 20 px icon in a 44 px button is correct. Give the size of the smallest target in the density decision of the spec.

## Icon-only button

```html
<button type="button" aria-label="Share">
  <svg aria-hidden="true" focusable="false"><!-- icon --></svg>
</button>
```

- The name comes from `aria-label`, or from text that only a screen reader reads.
- An emoji, a Unicode symbol, or the ligature of an icon font (for example `<span class="material-icons">share</span>`) is not a name.
- An icon next to visible text gets `aria-hidden="true"` on the `svg`, or `alt=""` on an `img`.

## Icon toggle button (PAT-TOGGLE-02)

```html
<button type="button" aria-pressed="true" aria-label="Mute">
  <svg aria-hidden="true" focusable="false"><!-- slashed speaker --></svg>
</button>
```

- `aria-pressed` is `true` or `false`. The script changes `aria-pressed` and the icon. It does not change `aria-label`.
- The name says what is on when pressed: "Mute", "Microphone", "Music". A name such as "Unmute" or "Turn music off" is wrong.
- The two states have a different shape: a slashed and a plain icon, or a filled and an outlined icon. A different color alone is not enough.

## Switch (PAT-TOGGLE-01)

```html
<label>
  <input type="checkbox" role="switch" checked>
  Music
</label>
```

- Use `role="switch"` on a checkbox, or on a `button` with `aria-checked`. The visible label stays the same in both states.
- Use a switch only for on and off. For named values, use radio buttons or a segmented control.

## Tooltip

- The `title` attribute is not a tooltip that all users get. It does not show on touch or on keyboard focus, and some screen readers do not read it. Do not use `title` as the only name.
- Make a tooltip that shows on hover and on keyboard focus. It stays while the pointer is on it, and Esc closes it (WCAG 2.2 SC 1.4.13).
- The tooltip text is the same as the accessible name.

## Shared elements in mockups

Mark each element of the Element inventory with `data-element="<inventory name>"`. Put `data-region="<region name>"` on an ancestor, for example `top-bar`, `bottom-bar`, `panel`, or `main`. `uxcheck.py mockup` then finds an element that has a different region on 2 screens.
