# Icons and toggles: the research

This file gives the detail and the sources of the ICON and TOGGLE rules in `patterns.md`. Read it when a design has icons or toggles. The rule IDs are the IDs of `patterns.md`. The research of 2026-10-27 used the book library of the owner first, and the primary sources after that.

Evidence tags: `checked` means a source says the rule. `judgment` means the sources support the direction of the rule, but no source says it in these words.

## Contents

- Icon, label, or text
- Each icon
- Toggles
- What the checker finds
- Gaps in the research
- Sources

## Icon, label, or text

| Rule | Detail | Evidence | Source |
|------|--------|----------|--------|
| PAT-ICON-01 | The primary action of a view has a text label. An icon before the text is optional. | checked | Grant P38, Apple HIG Buttons ("Consider using text when a short label communicates more clearly than an icon") |
| PAT-ICON-01 | A destructive or irreversible action (delete, leave the game, end the call, reset) has a text label. | judgment | XAG 114 asks that the player knows the result of a button before the click. No source says "never icon only" for a destructive action. |
| PAT-ICON-02 | A status, an error, a warning, a success, or a required field has text. Icon and color only add to the text. | checked | WCAG 2.2 SC 1.4.1 (failure F81) and SC 1.1.1, XAG 103 ("at least one additional signifier"), Crofte ch. 3 |
| PAT-ICON-03 | Main navigation has visible labels, also on mobile. On desktop, show the top-level items. On mobile, show 4 or fewer. When there are more, put the rest behind a button with the visible label "Menu". | checked | NN/g, *Hamburger Menus and Hidden Navigation Hurt UX Metrics* (hidden navigation cuts discoverability almost in half), Grant P29 ("label it Menu") and P38 |
| PAT-ICON-04 | When the designer cannot find an icon in 5 seconds, use text. Do not give a known icon a new meaning. | checked | NN/g, *Icon Usability* (the 5-second rule), Grant P36 and P66 |
| PAT-ICON-05 | A universal icon can be icon only when the space is small. It needs an accessible name and a tooltip on hover and on keyboard focus. | checked | NN/g, *Icon Usability*, Crofte ch. 4 "Use alternative text next to icons", WCAG SC 1.1.1 |
| PAT-ICON-06 | Trained daily users can learn icon-only tools. Test the recognition first. The main navigation keeps its labels. | checked | Grant P38, GOV.UK Design System, Images, NN/g, *Usability Testing of Icons* |
| PAT-ICON-07 | In each other case: icon and a visible label, always visible. | checked | NN/g, *Icon Usability* (labels visible at all times), GOV.UK Images ("Avoid using icons in most cases") |
| PAT-ICON-08 | Label next to the icon in buttons and rows. Label below the icon in tiles and tab bars. | checked for the positions, not tested | Apple HIG Buttons, WCAG Understanding 2.5.3 ("immediately below icons serving as buttons"). No test compares below and beside. |

Universal icons in the sources: home, print, and the magnifying glass for search (NN/g), play (Crofte), and bold, italic, underline (Grant). Nothing else gets this status. Ambiguous icons in the sources: history (clocks, arrows, hourglasses), share, `@`, cloud, link, globe, and the Wi-Fi "fan" (Grant P36, P38, P66).

## Each icon

PAT-ICON-09 and PAT-ICON-10 combine these rules.

| Topic | Detail | Evidence | Source |
|-------|--------|----------|--------|
| One set | One icon set and one style for each product: the same size, level of detail, stroke weight, and perspective. Match the weight of the icon to the weight of the text next to it. | checked | Apple HIG Icons. Grant P34. *Mastering UI/UX Design* ch. 7 |
| One meaning | One icon has one meaning in the whole product, and one action has one icon. Use the platform icon for a standard action (magnifying glass for search, gear for settings). | checked | GOV.UK Images ("Do not use a single icon to represent more than one thing"), XAG 114, Grant P66 |
| Name | An icon-only control has a name that tells the purpose: "Share", not "Arrow out of box". Do not add the role: "Submit", not "Submit button". | checked | WCAG SC 1.1.1 and 4.1.2, Android Developers, *Make apps more accessible* |
| Decorative | Hide an icon next to a visible label from assistive technology. Else the screen reader reads the label two times, or reads a glyph name. | checked | WCAG SC 1.1.1, GOV.UK Images, XAG 103 |
| Contrast | An icon that the user needs, with no text next to it, has 3:1 against the color next to it. An icon with a label that says the same is exempt. | checked | WCAG SC 1.4.11 |
| Target | An interactive icon has a target that is larger than the glyph. Do not make small decorative icons interactive. The size values are platform values. | checked | WCAG SC 2.5.8 and 2.5.5, Apple HIG Buttons (44 x 44 pt), Android (48 x 48 dp), Fluent 2 Iconography |
| No text | No text inside an icon. The exception is a character for text formatting, which you translate. | checked | Grant P37, Apple HIG Icons |
| Old objects | A floppy disk, a handset phone, or a gesture that differs by culture: keep it only when users still recognize it in a test. Else use a label. Use figures that do not show a gender. | checked | Grant P35, Apple HIG Icons ("Use inclusive images"), Fluent 2 Iconography |
| Test | Test a new icon: recognition and meaning out of context, then findability in context. | checked | NN/g, *Usability Testing of Icons*, Grant P35 |
| No emoji | Use a vector icon with a text alternative, not an emoji or a Unicode symbol. A screen reader reads the Unicode name ("Bust in silhouette"). | checked | XAG 103, TELL-06 of `ui-craft` |

## Toggles

| Rule | Detail | Evidence | Source |
|------|--------|----------|--------|
| PAT-TOGGLE-01 | An on/off setting in a list: a switch with a visible label next to it. The label does not change with the state. | checked | WAI-ARIA APG Switch pattern, Apple HIG Toggles ("Use the switch toggle style only in a list row"), XAG 114 |
| PAT-TOGGLE-01 | A switch is for binary input only. For 2 named values or more than 2 values, use a segmented control or radio buttons (PAT-IN-01). | checked | WAI-ARIA APG Switch (binary input only), Apple HIG Toggles |
| PAT-TOGGLE-02 | An on/off mode during the task, outside a list: an icon toggle button with a fixed name and an exposed pressed state. The name says what is on when pressed. | checked | WAI-ARIA APG Button pattern (the label of a toggle does not change with its state). Apple HIG Toggles ("Outside of a list, use a button that behaves like a toggle") |
| PAT-TOGGLE-02 | The state shows by shape: a slashed icon, a filled or outlined icon, or a background shape that shows and hides. Not by color alone. | checked | Apple HIG Toggles ("Avoid relying solely on different colors to communicate state"), Fluent 2 Iconography (filled icons for selected states), WCAG SC 1.4.1 |
| PAT-TOGGLE-02 | The state indicator (the fill, the slash, the knob) has 3:1 against the colors next to it. | checked | WCAG SC 1.4.11 ("visual information necessary to indicate state") |
| PAT-TOGGLE-02 | Put the quick toggle in the bar, and the same setting with a text label in the settings screen. | judgment | XAG 114 (labels next to toggles) and Apple HIG Toggles support each part. No source names the combination. |

A conflict: Material Web lets a toggle icon button have a second label when selected (`aria-label-selected`). The APG says that the label must not change. Follow the APG.

A game uses icons in the same way as other apps. The game sources give rules for audio channels and voice chat. These are product decisions, not icon rules, so this file does not include them.

## What the checker finds

`uxcheck.py mockup` finds only the rules with one clear cause. The IDs are in `tells.md` of `ui-craft`.

| Rule | Check | Level |
|------|-------|-------|
| PAT-ICON-10 name | TELL-08: an icon-only control without a name. An emoji or an icon-font ligature is not a name. | error |
| PAT-TOGGLE-02 fixed name | TELL-21: the name changes with the pressed state, or starts with "un", "turn on", "turn off", "enable", or "disable". | error |
| PAT-TOGGLE-02 exposed state | TELL-22: the icon changes between frames, but the control has no pressed or checked state. | error |
| PAT-TOGGLE-02 shape | TELL-23: the two states have the same icon and differ only in color. | error |
| PAT-ICON-10 contrast | TELL-24: the icon of an icon-only control is below 3:1. | error |
| PAT-ICON-01 | TELL-25: an icon-only primary action. | error |
| PAT-ICON-02 | TELL-26: a status or an alert with an icon and no visible text. | error |
| PAT-ICON-05 | TELL-27: the only name is `title`. | warning |
| PAT-ICON-10 target | TELL-28: a target below 24 x 24 CSS px without the spacing exception. | warning |
| PAT-ICON-03 | TELL-29: icon-only items in navigation, or an icon-only menu button. | warning |
| PAT-ICON-09 one meaning | TELL-30: one icon with 2 or more names. | warning |

The checker does not find these faults. Check them in the critique: a destructive action as icon only, mixed icon sets, and a glyph that is too small. A word list for destructive actions guesses too much.

## Gaps in the research

- No source gives a minimum glyph size in px for an interface icon, or the gap in px between an icon and its label. A number for these is an owner decision.
- No test compares a label below an icon with a label beside it.
- The research could not read the Material Design 3 pages. The 48 dp target is from the Android page.
- WCAG SC 1.4.13 (content on hover or focus, for tooltips) was not read in this research.

## Sources

Book library of the owner:

- Will Grant, *101 UX Principles*, 2nd ed. (2022). Principles 29, 34, 35, 36, 37, 38, and 66.
- Geoffrey Crofte, *What Designers Need To Know About Accessibility* (2024). Chapter 3 (color alone) and chapter 4, "Use alternative text next to icons".
- *Mastering UI/UX Design* (Sonar Publishing, 2024). Chapter 7, "Iconography and Imagery". Low weight: general text.

Web, read on 2026-10-27:

- [NN/g: Icon Usability](https://www.nngroup.com/articles/icon-usability/), Aurora Harley, 2014.
- [NN/g: Usability Testing of Icons](https://www.nngroup.com/articles/icon-testing/), Aurora Harley, 2016.
- [NN/g: Hamburger Menus and Hidden Navigation Hurt UX Metrics](https://www.nngroup.com/articles/hamburger-menus/), Kara Pernice and Raluca Budiu, 2016.
- [WAI-ARIA APG: Button pattern](https://www.w3.org/WAI/ARIA/apg/patterns/button/)
- [WAI-ARIA APG: Switch pattern](https://www.w3.org/WAI/ARIA/apg/patterns/switch/)
- [Understanding WCAG 2.2: Non-text Content](https://www.w3.org/WAI/WCAG22/Understanding/non-text-content.html)
- [Understanding WCAG 2.2: Use of Color](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html)
- [Understanding WCAG 2.2: Non-text Contrast](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html)
- [Understanding WCAG 2.2: Label in Name](https://www.w3.org/WAI/WCAG22/Understanding/label-in-name.html)
- [Understanding WCAG 2.2: Target Size (Minimum)](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html)
- [Understanding WCAG 2.2: Target Size (Enhanced)](https://www.w3.org/WAI/WCAG22/Understanding/target-size-enhanced.html)
- [Apple Human Interface Guidelines: Buttons](https://developer.apple.com/design/human-interface-guidelines/buttons), and the pages Icons, Toggles, and Accessibility.
- [GOV.UK Design System: Images](https://design-system.service.gov.uk/styles/images/)
- [Microsoft Fluent 2: Iconography](https://fluent2.microsoft.design/iconography)
- [Xbox Accessibility Guidelines 103](https://learn.microsoft.com/gaming/accessibility/xbox-accessibility-guidelines/103), and the guidelines 112 and 114.
- [Material Web: Icon button](https://github.com/material-components/material-web/blob/main/docs/components/icon-button.md)
- [Android Developers: Make apps more accessible](https://developer.android.com/guide/topics/ui/accessibility/apps)
