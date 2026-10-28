# UX design: NDK-14 Wachtkamer en stand

Mode: full
Surface: play (consumer game, phone first)
Status: approved by the owner on 2026-10-26. The next step is the mockups.

Scenario of the mockups: Friday 30 Oct 2026, 21:40. Anna hosts a game with 5 friends: Anna, Bram, Chantal, Dewi, Erik. Fleur joins late.

## Sources

| Source | Found | Used for |
|--------|-------|----------|
| docs/work/ndk-14-lobby-stand/story.md | yes | AC-1 to AC-5 |
| PRODUCT.md | yes | users, terms, phone widths, dark rooms, WCAG 2.2 AA |
| design-system/gameshow/tokens.css | yes | colors, type, space, radius |
| design-system/gameshow/components.css | yes | gs-showbalk, gs-tegel, gs-segment, gs-invoer, gs-codebord, gs-knop, gs-chip |
| src/app/lobby/lobby.component.html | yes | the current lobby |
| docs/ux/decisions.md | yes | UXD-001 mobile first, UXD-002 dark only |

## Frame

| Question | Answer | Evidence |
|----------|--------|----------|
| Problem | Players cannot set music, chat, and microphone in one place, and cannot see the stand without leaving the round. | checked: story |
| Users | Groups of friends, phone in one hand, in a dark room or on a video call. | checked: PRODUCT.md |
| Success | Each player finds the music, chat, and microphone setting in under 3 seconds on each screen. | needs-users |
| Constraints | Gameshow foundation only. Dark theme. Dutch. 390 px first, also 820 px. | checked: PRODUCT.md, UXD-001, UXD-002 |

## Flow

1. A player opens the lobby (`lobby`). The player sees the sessiecode, the players, and the own name in an input. AC-1.
2. The host sets the doelscore with the segmented control and can tap a player tile to remove that player. AC-2.
3. Each player sets music, chat, and microphone with the toggles in the lobby. AC-3.
4. During the game, a player opens the stand (`stand`) from the game screen. The stand is a full-height panel over the game. AC-4.
5. On the stand, a player switches music and the game sounds. AC-3, AC-4.
6. A player leaves the game from the lobby or the stand. A dialog asks "Weet je het zeker?". AC-5.

## Options

| Option | Structure | Task fit | Clarity | Consistency | Risk | Cost | Total |
|--------|-----------|----------|---------|-------------|------|------|-------|
| A | Settings in each screen, at the place where there is room. | 3 | 2 | 3 | 3 | 3 | 14 |
| B | One settings sheet behind a gear icon. | 2 | 2 | 2 | 3 | 2 | 11 |

Selected: A.

## Screen: lobby

| Decision | Value | Source |
|----------|-------|--------|
| purpose | Let players join and set up the game. | product: story AC-1, AC-2 |
| main-task | Wait for the group, set the own name, the host starts. | product: story |
| pattern | Header bar, code board, player tiles, settings rows. | judgment |
| layout | Top bar (`gs-showbalk`): title "WACHTKAMER" on the left, the music toggle on the right. Under it the code board (`gs-codebord`). Behind the code board, a decorative card stack (2 card backs). Then the input "Jouw naam". Then the player tiles in a 2-column grid. Then (host only) the doelscore segmented control. Then a settings row with the chat toggle and the microphone toggle. Then "Start het spel". Then at the bottom of the screen: "Spelregels" and "Spel verlaten". | system: components.css |
| hierarchy | 1: the code board. 2: the player tiles. 3: Start. 4: the settings. | judgment |
| components | gs-showbalk, gs-codebord, gs-invoer, gs-tegel, gs-segment, gs-knop, gs-knop--stil, gs-chip | system |
| density | comfortable, one hand | judgment |
| states | default (host), guest, waiting (2 players), name-error, offline | product: story |
| copy | see the copy table | product: PRODUCT.md Terms |
| motion | A new player tile slides in (200 ms). None with reduced motion. | judgment |

Visual details:

- Player tile: `gs-tegel`, fill `--color-surface` (#2e1a63) on `--color-bg` (#1a0b3b), no border. Avatar, name, and a "host" chip. For the host, each tile of another player is a button: a tap opens "Bram verwijderen?".
- Doelscore: `gs-segment`, track fill `--color-surface` (#2e1a63) on `--color-bg`, no border. The selected segment is violet (#8a6cff).
- Input "Jouw naam": `gs-invoer`, fill `--color-surface-sunken` (#0d0620), border 2 px `--color-surface-spot` (#42277f), on `--color-bg`.
- Decorative card stack: 2 card backs, fill `--color-surface-sunken` (#0d0620) on `--color-bg`, rotated 6 degrees, behind the code board. `aria-hidden="true"`. Not interactive. Decoration only.
- Music toggle: text button `gs-knop--stil` in the top bar, right: "Muziek: aan" or "Muziek: uit".
- Chat toggle: text button `gs-knop--stil`: "Chat: aan" or "Chat: uit".
- Microphone toggle: text button `gs-knop--stil`: "Microfoon: aan" or "Microfoon: uit".
- "Spelregels": text button `gs-knop--stil` without a border, text `--color-text-muted` (#c9b8ff). Opens the rules.
- "Spel verlaten": text button `gs-knop--stil` at the bottom of the screen, under "Spelregels".

### States: lobby

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
| default | Host view. Code K7PX2Q, 5 players, doelscore 7 selected, music on, chat on, microphone off. "Start het spel" enabled. | Set the doelscore, remove a player, set the toggles, start | n/a |
| guest | View of Bram: no doelscore control, no remove, the text "Anna start het spel". | Set the own name and the toggles | n/a |
| waiting | Only Anna and Bram. "Start het spel" disabled, hint "Nog 1 speler nodig". | Share the code | A third player joins |
| name-error | Input "Jouw naam" with "Bram" taken: error text "Deze naam is al gekozen. Kies een andere naam." | Change the name | A free name removes the error |
| offline | Banner "Geen verbinding. We proberen het opnieuw." Toggles stay usable. | Wait | The banner goes away when the connection is back |

### Copy: lobby

| Element | Text |
|---------|------|
| Title | WACHTKAMER |
| Code label | Sessiecode |
| Name input | Label "Jouw naam" |
| Doelscore | Label "Doelscore", segments 5, 7, 10 |
| Music toggle | Muziek: aan / Muziek: uit |
| Chat toggle | Chat: aan / Chat: uit |
| Microphone toggle | Microfoon: aan / Microfoon: uit |
| Start | Start het spel |
| Rules | Spelregels |
| Leave | Spel verlaten |
| Remove dialog | Bram verwijderen? · Verwijderen · Annuleren |

## Screen: stand

| Decision | Value | Source |
|----------|-------|--------|
| purpose | Show the points of each player during the game. | product: story AC-4 |
| main-task | See who leads and how far the own score is from the doelscore. | product: story |
| pattern | Full-height panel with a ranked list. | judgment |
| layout | Panel top bar: "Spel verlaten" on the left, title "STAND" in the middle, the game sounds toggle on the right. Under it: "Doelscore 7". Then the ranked player rows. At the bottom of the panel: the music toggle and "Terug naar het spel". | system: components.css |
| hierarchy | 1: the leader row. 2: the own row. 3: the other rows. | judgment |
| components | gs-showbalk, gs-tegel, gs-chip, gs-knop, gs-knop--stil | system |
| density | comfortable | judgment |
| states | default, sound-off, last-round | product: story |
| copy | see the copy table | product: PRODUCT.md Terms |
| motion | none | judgment |

Visual details:

- Player row: `gs-tegel`, fill `--color-surface` (#2e1a63) on `--color-bg`, no border. Rank, avatar, name, points. Tap a row to see the cards that the player won.
- Game sounds toggle: icon-only button with a speaker icon (24 px), in the panel top bar, right. When the sounds are on, the accessible name is "Dempen" and the icon is a speaker with waves. When the sounds are off, the accessible name is "Dempen opheffen" and the icon is a speaker with a cross.
- Music toggle: text button `gs-knop--stil` at the bottom of the panel: "Muziek: aan" or "Muziek: uit".
- "Spel verlaten": text button `gs-knop--stil` in the panel top bar, left.

### States: stand

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
| default | Round 6. Chantal 5, Anna 4, Bram 4, Dewi 2, Erik 1. Own row (Bram) marked "jij". Sounds on. Music on. | Switch the sounds and the music, go back, leave | n/a |
| sound-off | As default, the game sounds off. | Switch the sounds on | Tap the toggle again |
| last-round | Chantal has 6 of 7: chip "Nog 1 punt" on her row. | As default | n/a |

### Copy: stand

| Element | Text |
|---------|------|
| Title | STAND |
| Target | Doelscore 7 |
| Own row | jij |
| Game sounds | Dempen / Dempen opheffen (accessible name) |
| Music | Muziek: aan / Muziek: uit |
| Back | Terug naar het spel |
| Leave | Spel verlaten |

### Accessibility

- Each toggle is a button. The text says the state.
- Focus order follows the layout.
- The player tiles of the host are buttons with the name "Bram verwijderen".
- Contrast: all text pairs pass 4.5:1 (white and #c9b8ff on #1a0b3b and #2e1a63).

## Decisions

| ID | Decision | Status |
|----|----------|--------|
| UXD-004 | Settings in each screen, not in a settings sheet (option A). | accepted |
