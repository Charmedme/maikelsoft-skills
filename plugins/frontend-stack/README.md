# frontend-stack

frontend-stack is a Claude Code plugin with stack skills for the front end of a web app. It has 3 skills. Each skill tells the agent what is true for this version of the stack and what the agent usually gets wrong. Each skill also has a check step with a tool and one report format.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code, Node.js, and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install frontend-stack@maikelsoft-skills
```

Also install the two official skills that frontend-stack works with:

```sh
# The official Angular skill, in an Angular project
npx skills add https://github.com/angular/skills

# Modern Web Guidance of Google Chrome
claude plugin marketplace add GoogleChrome/modern-web-guidance
claude plugin install modern-web-guidance@googlechrome
```

The back-end rules are in dev-stack, and the test rules in test-stack.

## Quick start

1. Open Claude Code in a repository with a web front end.
2. Ask for a change or a review in your own words:

   ```
   Add a confirm dialog before the user deletes an order.
   ```

   ```
   Review this component for XSS before we merge.
   ```

3. Claude Code loads the skills that match the work. One request can load more than one skill, for example `angular` and `modern-web`.

## Commands

| Command | What it does | Starts by itself when you ask for |
|-------|------------|-----------------------------------|
| `/frontend-stack:angular` | Angular 22 and 21 in an existing app, next to the official `angular-developer` skill. Server errors on form fields, ETag and 412 conflicts, stale lists, focus, live regions, Transloco, dates, tests, and the browser check. | Angular components, templates, services, forms, routes, specs, or Transloco texts |
| `/frontend-stack:web-security` | Security rules for browser code: the Angular sanitizer, `bypassSecurityTrust`, DOM writes, CSP and Trusted Types, XSRF, token storage, and AngularJS. | A token in the browser, `innerHTML`, a CSP, or a security review of front-end code |
| `/frontend-stack:modern-web` | Sends HTML, CSS, and client-side JavaScript work to Modern Web Guidance of Google Chrome. It holds no guidance of its own. | Browser UI, CSS layout or animation, or client-side JavaScript |

## Why modern-web is a pointer

Modern Web Guidance (https://github.com/GoogleChrome/modern-web-guidance) gives the current pattern for each web platform task. The Chrome, Edge, Firefox, and MDN teams keep it up to date, with evals for each guide. A copy here would get old. So `modern-web` only starts that skill. When the plugin is not installed, `modern-web` gives the install command, and uses the `npx` search of the package after your approval.

## Moved from dev-stack

`angular` and `web-security` come from dev-stack 1.3.0. `web-security` is the browser part of `dev-stack:secure-coding`. The old name `dev-stack:angular` still works in dev-stack 2.x: it sends the request here.

## Test results

The test results of `angular` (dev-stack 1.3.0) are in the README of dev-stack and in `docs/work/dev-stack/angular/results.md`. The trigger check of version 1.0.0 is in `docs/work/stack-split/results.md`.

## Limits

- `modern-web` needs Modern Web Guidance or `npx` with network access. Without both, the agent works without the guidance and says so.
- `web-security` covers Angular and AngularJS. Other frameworks get only the general rules.
