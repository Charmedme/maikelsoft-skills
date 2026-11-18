---
name: modern-web
description: Run first for each task that writes or changes HTML, CSS, or client-side JavaScript - dialogs and modals, popovers and tooltips, drawers and menus, forms and validation, layout and container queries, animations and view transitions, dark mode, accessibility, page speed - also inside Angular or another framework. Training data holds old web patterns. This skill gets the current pattern from Modern Web Guidance of Google Chrome - it starts that skill when it is installed, and else gives the install command and uses its npx search. Not for backend code, SQL, build tooling, CI, or tests.
---

# Modern web

Training data holds many old web patterns. Modern Web Guidance (https://github.com/GoogleChrome/modern-web-guidance) gives the current pattern for each web platform task. The Chrome, Edge, Firefox, and MDN teams keep it up to date. This skill only sends the work there.

## Steps

1. **Find the skill.** Look in the skill list of the session for `modern-web-guidance`. When it is there, start it with the task, follow it, and stop this skill. Done when you started it, or you know that it is not installed.
2. **Give the install command.** Tell the human once in the session:

   ```
   claude plugin marketplace add GoogleChrome/modern-web-guidance
   claude plugin install modern-web-guidance@googlechrome
   ```

   Done when the human has the commands.
3. **Search for this task.** Ask the human once before the first `npx` call, because it downloads the package `modern-web-guidance` from npm. After a yes, search with a query of 2 to 5 words for one platform feature or UI pattern. Leave out framework names and app names. Split a task with more features into more searches.

   ```
   npx -y modern-web-guidance@latest search "<query>"
   npx -y modern-web-guidance@latest retrieve "<id>[,<id>]"
   ```

   When the search gives no match, change the words one time, for example to the HTML element (`dialog`, `popover`, `table`, `form`). Done when you read each guide that matches the task, or you know that no guide matches.
4. **Write the code.** Use the pattern of the guide. Done when the code follows the guide, or the report says why not.

## Rules

- Existing code wins (stack rule 3 of the stack skills). A guide does not start a rewrite of code that the task does not touch.
- In an Angular app, `frontend-stack:angular` gives the Angular APIs, and the guide gives the HTML, CSS, and DOM. When the two disagree on an Angular API, `frontend-stack:angular` wins.
- Security rules for browser code are in `frontend-stack:web-security`.
- When the human says no to the install and to `npx`, go on without the guidance, and say so in the report.

## Report

Add one line to the report of the task: "Modern Web Guidance: <the guide IDs that you used>, or not used, with the reason."
