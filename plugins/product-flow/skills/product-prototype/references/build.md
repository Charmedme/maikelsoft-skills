# Build the prototype

## Contents

- The files
- The content
- Scope
- Styles
- Data and states
- Text

## The files

- You write one content file, `content-<slug>.html`, in the format of `proto-shell/STANDARD.md`. `protobuild.py` makes `prototype-<slug>.html` from it: one HTML file with the shell, that opens with a double-click.
- No server, no build step, no framework. No network calls: no `fetch`, no external images, fonts, or scripts. The prototype works offline.
- In a repository, put both files in the work folder (for example `docs/work/<slug>/`), not in the source code.

## The content

- **Manifest.** `title` (max 40 characters), `question` (the one question, ends with `?`), and `kind` `throwaway`. Also `scope` (one sentence), `lang` (the language of the users), `start`, `screens`, `variants`, and `themes`.
- **Screen type.** 2 or 3 variants, each with a `hypothesis`. Each variant has its own template for the screen that differs (`data-variant="A"`).
- **Flow type.** One variant. Each step of the flow is a screen. Use `data-goto` on the buttons that go to the next step.
- **Notes.** Put each decision of the context file that the prototype shows in a note (`data-note`). The product owner sees why the screen looks like this.
- Do not change the shell. It gives the header, the question, the variant switch, the state switch, and the sizes.

## Scope

Show only what the question and the request need. Do not add a cart, an account menu, history, filters, detail pages, or numbers that the request does not name. A header or a menu of the existing product can stay, but without working parts. Put each idea for more functions in the reply as a question: "Do you also want a brand filter?" Extra functions make the human judge things that nobody asked for.

## Styles

- **With design sources (default when found):** put all tokens in CSS variables in `proto-css`, for example `--color-primary`. Use them for each element. Copy the layout of the existing screens: the header, the navigation, the position of the main action. Name the components of the design system in comments. The prototype must look like a new page of the existing product.
- **Without design sources:** the plain style. Greys, one accent colour for the main action, the system font, and boxes for images. Say in the reply that the style is a placeholder.
- The layout works at the 3 sizes of the shell: 390, 820, and 1440 px.

## Data and states

- Use fake data that looks fake: "Customer A", "Product 001", "Status X". Never use real names or real numbers of customers.
- Each screen has at least the states `default`, `empty` (no data), and `error` (the data does not load). Add `loading` when the question is about speed. Put them in `states` of the screen in the manifest, and use `data-show-state` for the parts of each state.
- Keep the state in memory. Do not save it.

## Text

- Labels and buttons use the words of the users, not the words of the code. Write them in the language of the users, and follow `language.md`.
- Use real text, not "Lorem ipsum". The words are part of what the human checks.
