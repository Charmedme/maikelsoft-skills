# Build the prototype

## Contents

- The file
- The page
- Scope
- Styles
- Data and states
- Text

## The file

- One HTML file with inline CSS and JavaScript. No server, no build step, no framework. It opens with a double-click.
- No network calls. No `fetch`, no external images, no external scripts. The prototype works offline.
- Name it `prototype-<slug>.html`. In a repository, put it in the work folder (for example `docs/work/<slug>/`), not in the source code.

## The page

From the top down:

1. **A banner:** "PROTOTYPE: not the final design" in a fixed bar. Give it `id="prototype-banner"`.
2. **The question:** one sentence in an element with `id="question"`.
3. **A switch:** buttons for the variants (A, B, C) or the steps of the flow. The arrow keys also switch.
4. **The screen:** the current variant or step.
5. **A state bar:** buttons for the states (see below).

## Scope

Show only what the question and the request need. Do not add a cart, an account menu, history, filters, detail pages, or numbers that the request does not name. A header or a menu of the existing product can stay, but without working parts. Put each idea for more functions in the reply as a question: "Do you also want a brand filter?" Extra functions make the human judge things that nobody asked for.

## Styles

- **With design sources (default when found):** put all tokens in CSS variables (`--color-primary`, `--font-family`, `--radius-md` and so on) and use them for each element. Copy the layout of the existing screens: the header, the navigation, the position of the main action. Name the components of the design system in comments. The prototype must look like a new page of the existing product.
- **Without design sources:** the plain style. Greys, one accent colour for the main action, the system font, and boxes for images. Say in the reply that the style is a placeholder.
- The layout works on a phone (360 px) and on a desktop.

## Data and states

- Use fake data that looks fake: "Customer A", "Product 001", "Status X". Never use real names or real numbers of customers.
- Show at least these states, each with a button in the state bar: `normal`, `empty` (no data), and `error` (the data does not load). Add `loading` when the question is about speed. Give each state a `data-state` attribute.
- Keep the state in memory. Do not save it.

## Text

- Labels and buttons use the words of the users, not the words of the code. Write them in the language of the users, and follow `language.md`.
- Use real text, not "Lorem ipsum". The words are part of what the human checks.
