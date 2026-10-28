# AI tells for app screens

Version: 2026-10. Review this list every 6 months: the tells of generated design change.

A tell is a sign that a default filled a decision, not the product or the system. `uxcheck.py mockup` finds the tells with an ID. The others need your judgment.

| ID | Tell | Checked by |
|----|------|-----------|
| TELL-01 | More than one primary action in one view | uxcheck |
| TELL-02 | A font size outside the type scale | uxcheck |
| TELL-03 | A color outside the tokens | uxcheck |
| TELL-04 | A card inside a card (nested surfaces with shadow and radius) | uxcheck |
| TELL-05 | A gradient as a surface or a button | uxcheck |
| TELL-06 | An emoji used as an icon | uxcheck |
| TELL-07 | Centered body text over a wide line (more than 480 px) | uxcheck |
| TELL-08 | An icon-only button without an accessible name | uxcheck |
| TELL-09 | An input without a visible label (a placeholder as the only label) | uxcheck |
| TELL-10 | A font weight outside the weight tokens (or more than 3 weights when the system has no weight tokens) | uxcheck |
| TELL-11 | Text contrast under 4.5:1 (3:1 for large text) | uxcheck |
| TELL-12 | The same radius and shadow on each block, so nothing stands out | judgment |
| TELL-13 | A hero or a welcome banner on a work screen | judgment |
| TELL-14 | Metric tiles without a target or a comparison | judgment |
| TELL-15 | Decorative icons next to each heading | judgment |
| TELL-16 | Generic labels ("Manage items", "Overview", "Submit") instead of the words of the domain | judgment |
| TELL-17 | Everything centered in narrow columns on a wide screen | judgment |
| TELL-18 | A status shown by color only | judgment |
| TELL-19 | Lorem ipsum, "John Doe", or round fake numbers (100, 1,000) | judgment |
| TELL-20 | A modal on top of a modal | judgment |
| TELL-21 | A toggle name that changes with the state, or a name that tells the next action ("Unmute", "Turn on", "Enable") | uxcheck |
| TELL-22 | An icon that changes between 2 states, with no `aria-pressed`, `aria-checked`, or `role=switch` | uxcheck |
| TELL-23 | A toggle that shows its state by color only (the same icon and shape in the 2 states) | uxcheck |
| TELL-24 | An icon-only control with icon contrast under 3:1 | uxcheck |
| TELL-25 | An icon-only primary action | uxcheck |
| TELL-26 | A status shown by an icon without visible text | uxcheck |
| TELL-27 | An icon-only control that gets its name from `title` only | uxcheck |
| TELL-28 | A target under 24 x 24 px that is too near to another target | uxcheck |
| TELL-29 | Icon-only items in navigation, or an icon-only menu button | uxcheck |
| TELL-30 | One icon with 2 or more names | uxcheck |
