# Brief: ux-design 0.4.0, fixes from a field report

Status: approved on 2026-10-27 (gate G1).
Mode: Improve. Date: 2026-10-27.

## Need

A field report of 2026-10-27 shows 6 failures of ux-design 0.3.1. The agent `ux-designer` made a new design for a Dutch party card game (Angular, mobile first). The owner rejected the result two times.

## The bad case

1. The owner asked for 3 concepts with a "new approach". All 3 concepts kept the old colors, card look, and fonts. Owner: "worse than what it was, nothing new or totally different". Cause in the skill: the option rubric of `ui-craft` gives the highest score to existing parts (Consistency, Cost).
2. Raised surfaces (#2e1a63) on the stage (#1a0b3b) have a contrast of about 1.4:1. Player tiles, chips, and the segmented control have no visible edge. `uxcheck.py` showed 0 errors on 52 frames. Cause: the checker tests text pairs only. The `web-accessibility` checklist says that `uxcheck.py system` covers 1.4.11. This is not true.
3. The same element was in a different place on each screen (music toggle, leave game, invite, players). Cause: the mockup rubric scores one frame at a time.
4. Music, chat, and microphone were text toggles. The owner wanted icon buttons with an on and off state. Cause: `ux-patterns` has no rule for icons or for toggles.
5. Two rounds made 121 frames with all states before the owner agreed on a direction. `uxcheck.py mockup` renders at 1440 px only, so the 390 and 820 px widths were not checked.
6. The agent type was not available until `/reload-plugins`.

Evidence: the field report on the mesh, and snapshots of the critique and the tokens (to become eval fixtures).

## Decisions

| No | Decision | Who |
|----|----------|-----|
| Q1 | One release, ux-design 0.4.0. Changes to `ui-craft`, `ux-practice` (and `uxcheck.py`), `ux-patterns`, `web-accessibility`, and the agent `ux-designer`. No new skill. | recommended, accepted |
| Q2 | A concept must break the visual identity only when the request asks for something new ("new approach", "totally different", "redesign"). This is the "new direction" mode. | recommended, accepted |
| Q3 | In the new direction mode, the option rubric has a Difference criterion, and Consistency and Cost weigh less. At least one concept has Difference 3. | recommended, accepted |
| Q4 | A style direction step before Options: 3 or 4 one-screen moodboards, then `NEEDS_HUMAN` for the owner choice. Only in the new direction mode, or when no design system exists. | recommended, accepted |
| Q5 | `uxcheck.py` checks non-text contrast of 3:1. System mode: borders, outlines, and focus rings on their background. Mockup mode: the rendered edge of each control. Error for a control edge below 3:1. Warning for a raised surface below 1.5:1 that holds an interactive tile. The design template gets a non-text contrast table, and the `web-accessibility` checklist gets the correct tool claim. | recommended, accepted |
| Q6 | `ux-design.md` gets an element inventory (element, one home, one component, screens). `uxcheck design` checks one home for each element. Mockups mark shared elements with `data-element`, and `uxcheck mockup` reports an element in another region. The critique gets one score for the set of screens. | recommended, accepted |
| Q7 | `ux-patterns` gets a rule for a persistent setting in an app bar or game bar. The rule: an icon button with `aria-pressed` and a fixed accessible name. The off state is not only a color. | recommended, accepted |
| Q7a | Before you write the rules of Q7, research how and when to use an icon. Also research when never to use an icon alone. Use the book library first, then the primary sources. The result becomes icon rules in `ux-patterns`, and checker rules where a test can find them. | human |
| Q8 | In full mode, the mockup mode has two passes. Pass 1: the key screens at 390 and 820 px, default state only, then `NEEDS_HUMAN` for the owner OK. Pass 2: all states. `uxcheck mockup` renders at 390, 820, and 1440 px. | recommended, accepted |
| Q9 | The README and `ux-setup` say to run `/reload-plugins` after the install. The agent file says how to run as a general-purpose agent. | recommended, accepted |
| Q10 | Tests: 2 new eval cases, 3 runs each, 0.3.1 against 0.4.0, on Sonnet. Case 1 is a new direction request for a party game. Case 2 is a review of a dark game UI with planted faults. Unit tests for each new checker rule. The field snapshots are the fixtures. | recommended, accepted |
| Q11 | `patterns.md` gets a core set of about 10 icon rows. They say when to use an icon only, an icon with a label, or text. They give the 4 cases with never an icon only, 2 toggle rules, one set and one meaning, and the target size. The full research goes to a reference that loads only when a design has icons. | recommended, accepted |
| Q12 | `uxcheck mockup` gets only the icon rules with one clear cause. Errors: an icon-only button without a name, a toggle name that changes, and a toggle without a state. Also a state shown by color only, an icon below 3:1, an icon-only primary action, and an icon-only status. Warnings: a name from `title` only, a target below 24 px, icon-only navigation, one icon with two meanings. Not included: a glyph below 16 px, a guess of a destructive action from words, mixed icon sets. | recommended, accepted |
| Q13 | A destructive or irreversible action gets a text label. The rule has the tag judgment, because no source says it directly. | recommended, accepted |
| Q14 | Only universal rules go into `patterns.md`. Rules for one platform go into a platform reference. The plugin makes web screens only. So the web values go into a web reference: the 24 CSS px target, the markup of `aria-pressed`, and the `title` tooltip. See Q15. | human |
| Q15 | A game does not use icons in a different way. The game rules from the research are about audio channels and voice chat, not about icons. So 0.4.0 has no game rules and no game flow. Color only and one place for each element are universal, and stay. | human (rule of Q14), recommended (the game finding) |

## Success criteria

- New direction request: at least one concept changes the color, the type, and the card look. The agent stops for a style choice before the full concepts.
- The before-fix tokens give an error for the control edges and a warning for the raised surfaces.
- A planted element in a different region on 2 screens gives a finding.
- A planted text toggle for music gives a finding with the icon toggle rule.
- In full mode, the agent stops after the key screens and before the states.
- No regression on the existing eval cases.

## Limits

- Light mode keeps one mockup pass.
- 1.4.11 asks for 3:1 only where the edge identifies a control or a state. A decorative card is not a failure.
