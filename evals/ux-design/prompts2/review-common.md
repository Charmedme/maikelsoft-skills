You work in the repository <repo>. The story is in `<work>/story.md`. The approved UX design is in `<work>/ux-design.md`, and the approved mockups are in `<work>/mockups/`. The project facts are in `CLAUDE.md`, `PRODUCT.md`, and `DESIGN.md`.

The developers built the feature of the story. The code is in `app/`, and the app runs at http://localhost:<port> (it is already started, do not start or stop it). The app has a JSON API under `/api/`.

Task: review the built feature against the approved design, before it goes to the users. Check each state of the design that the built app can show, at the widths 390, 820, and 1440 px. Python Playwright is installed (`from playwright.sync_api import sync_playwright`, `chromium.launch()` needs no options). Playwright `page.route` can fake an answer of the API, to make a state such as an error. axe-core is in `node_modules/axe-core/axe.min.js`.

Write `<work>/ux-review.md`. For each finding give: what is wrong, where (state, width, element), the evidence, the severity (P0 to P3), and who must fix it. End with one verdict: `CHANGES_REQUIRED` (a P0 or P1 finding) or `DONE`.

You cannot ask questions. Do not change the code in `app/`. Write your test scripts and screenshots in `<work>/review/` or `tests/ux/`.
