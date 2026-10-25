# Prototype shell standard (proto-shell/1)

Each prototype is one HTML file with two parts:

1. **The shell** (fixed). `protobuild.py` adds it from `references/proto-shell/` of the skill. Nobody changes it in a prototype. It gives the same overlay in each prototype: modes, toolbar, frame, test runner, and results.
2. **The content** (written per prototype). A manifest, the prototype styles, the screen templates, and an optional script. `protocheck.py` checks the content against this standard. `protobuild.py` wraps the content in the shell. It stores the prototype styles as inert text, so they style only the frame and never the shell.

The shell has its own neutral look. The prototype has the look of the product (tokens of the design system). The two do not mix: the prototype runs in an iframe, so its media queries see the real width of the selected size.

## Contents

- Header (required in each prototype)
- Modes
- Toolbar
- Content contract
- Manifest
- Screen templates
- Script API
- A/B test runner
- Results
- Link state and keys
- Rules

## Header (required in each prototype)

Each prototype of each plugin shows the same header at the top. It tells each viewer that this is not the product, what it is, and what it covers.

| Part | Source | Example |
|------|--------|---------|
| Badge | Fixed text in the language of the prototype | "Prototype, not the product" / "Prototype, niet het product" / "Prototyp, nicht das Produkt" |
| Title | Manifest `title`, max 40 characters | "Approval requests" |
| Kind and scope | Manifest `kind` (in the language of the prototype) and `scope` (one sentence, max 120 characters) | "throwaway · Request list, request detail, and the approved message." |

- **In the shell**, the header is automatic. In Test mode it shows the badge and the title only, so the participant still knows that it is a prototype.
- **Outside the shell**, for example a logic prototype or a variant bar on a page of the real product: run `protobuild.py --header <manifest>`. Put the output first in the body. The snippet uses a shadow root, so the page CSS cannot change it. In the real product, show it only in development builds.
- `protocheck.py` checks the title and the scope. `protocheck.py --header-only <file>` checks a page outside the shell for the header.
- The header text and the participant text of Test mode come from `strings.json` (en, nl, de). The toolbar, the setup, and the results stay in English, because the team uses them. A new language is a change to that file.

## Modes

| Mode | Job |
|------|-----|
| Explore | One frame. The reviewer clicks through the flow and uses the toolbar. |
| Overview | All screens and states of one variant as a grid, at the selected size. A click opens the frame in Explore. |
| Compare | Variant A and variant B side by side, same screen, state, theme, and size. Navigation can be linked. |
| Test | A moderated A/B test with participants: tasks, timer, success, SEQ, and preference. The variant names are hidden. |
| Results | The results of all test sessions per variant and task. Export to JSON and CSV, import from other devices. |

## Toolbar

| Control | Values | Effect in the frame |
|---------|--------|---------------------|
| Screen | From the manifest | Shows the screen template. |
| State | The states of the screen | Sets `data-state` on `<html>`. Elements with `data-show-state` show only in their states. |
| Variant | A, B (max 3) | Shows the template of that variant. A screen without a variant template uses the shared template. |
| Design | Styled, Wireframe | Sets `data-design`. Wireframe uses the wireframe tokens of the content, or grayscale when there are none. |
| Theme | From the manifest (for example light, dark) | Sets `data-theme`. |
| Size | Mobile 390 x 844, Tablet 820 x 1180, Desktop 1440 x 900, Fit | Sets the iframe size. The shell scales the frame to fit and shows the scale. |
| Notes | On, Off | Shows the numbered decision notes on the elements with `data-note`, and the note text in the side panel. |
| Back, Reset, Link | | History back, start screen, copy a link to the current view. |

## Content contract

```html
<!-- PROTO-CONTENT START -->
<script type="application/json" id="proto-manifest">{ ... }</script>
<style id="proto-css"> tokens and styles ... </style>
<template data-screen="list" data-variant="A"> ... </template>
<template data-screen="list" data-variant="B"> ... </template>
<template data-screen="detail" data-variant="*"> ... </template>
<script type="text/plain" id="proto-script"> optional JS ... </script>
<!-- PROTO-CONTENT END -->
```

## Manifest

| Key | Required | Rule |
|-----|----------|------|
| `standard` | yes | `"proto-shell/1"`. |
| `title` | yes | Short name. |
| `question` | yes | The one question that the prototype answers. |
| `kind` | yes | `"throwaway"` or `"evolutionary"`. |
| `scope` | yes | What the prototype covers, in one sentence. |
| `lang` | no | Language of the content, for example `"nl"`. |
| `start` | yes | `{ "screen": "...", "state": "..." }`. |
| `screens` | yes | `[{ "id", "name", "states": ["default", ...] }]`. Each state of the state inventory that is in scope. |
| `variants` | yes | `[{ "id": "A", "name", "hypothesis" }]`. One variant is allowed. Two or three for an A/B test. |
| `themes` | yes | For example `["light", "dark"]`. |
| `notes` | no | `[{ "n": 1, "screen": "list", "text": "Decision: ..." }]`. |
| `tasks` | for Test | `[{ "id", "text", "start": {screen, state}, "success": { "screen"?, "state"?, "element"? } }]`. |
| `test` | no | `{ "design": "within" or "between", "preference": "question text" }`. Default: within. |

## Screen templates

- One `<template>` per screen and variant. `data-variant="*"` is shared by all variants.
- Navigation: `data-goto="screen"` or `data-goto="screen:state"`. Back: `data-back`.
- State change on the same screen: `data-set-state="loading"`.
- State-only parts: `data-show-state="empty error"`, or `data-hide-state="loading"`.
- Task success: `data-task-success="t1"` on the element that completes task `t1`.
- Decision notes: `data-note="1"` on the element that note 1 is about.
- Styles use only tokens (`var(--...)`). Wireframe tokens go under `[data-design="wireframe"]`, theme tokens under `[data-theme="dark"]`.

## Script API

The optional script runs on each render. It gets `window.proto`:

| Member | Job |
|--------|-----|
| `proto.screen`, `proto.state`, `proto.variant`, `proto.theme`, `proto.design` | The current view. |
| `proto.goto(screen, state?, delayMs?)` | Go to a screen. The delay runs in the shell, so it survives the reload of the frame. |
| `proto.setState(state)` | Change the state of the current screen. |
| `proto.back()` | History back. |
| `proto.success(taskId)` | Mark a task as done (Test mode). |
| `proto.get(key)`, `proto.set(key, value)` | Small values that stay during the session (for example the selected row). |

## A/B test runner

1. **Setup.** The moderator sets the participant (P01, P02, ...) and the size of the device. The moderator also sets the design: within (each participant sees all variants) or between (one variant each).
2. **Order.** The shell counterbalances: within gives AB to odd and BA to even participants. Between gives A to odd and B to even participants.
3. **Blind.** The variant names, the toolbar, and the question are hidden. The participant sees "Version 1" and "Version 2".
4. **Per task.** Task card, then "Start". The timer runs. The shell records the path (screens), the clicks, and the misclicks (a click on an element that does nothing). The task ends on success (target screen, target state, or `data-task-success`), "I am done", or "Give up".
5. **SEQ.** After each task: "Overall, how difficult or easy was this task?" 1 (very difficult) to 7 (very easy).
6. **Preference.** After all tasks (within): which version was easier, and why.
7. **Save.** The session goes to the browser storage. Export it, because the browser can clear it.

Success without a success rule in the task is "self-reported". "I am done" before the success rule is met is a failure.

## Results

- Per variant and task: attempts, success rate, median time, mean SEQ, mean misclicks.
- Per variant: overall success rate and mean SEQ. Preference counts.
- Below 20 participants per variant, the table shows: "Indicative. Read the sessions for the reasons."
- Export: JSON (all sessions) and CSV (one row per task attempt), shown as text with a Copy button. Download works for a local file. Import: JSON from other devices.

## Link state and keys

The view is in the URL hash. Thus a review state is a link when the file opens locally or from a web server. A Claude artifact does not pass this hash, so there the view stays in the page. Example: `#mode=explore&screen=list&state=empty&v=B&theme=dark&design=styled&size=mobile&notes=1`.

Keys (Explore and Compare):

| Key | Job |
|-----|-----|
| `1`, `2`, `3`, `0` | Mobile, tablet, desktop, fit |
| `v`, `t`, `d`, `n` | Next variant, next theme, design, notes |
| `[`, `]` | Previous and next state |
| `Esc` | Back to Explore |

## Rules

- Do not change the shell. A shell bug is a change to the skill asset.
- The prototype shows real labels, never lorem ipsum. The skill that makes the prototype sets the rule for the data.
- The prototype works offline: no network calls, and no external scripts, styles, or images.
- Each state in scope has a frame. Each variant differs in structure, not only in color.
- The question is always visible, except in Test mode.
