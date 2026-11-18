# Brief: angular

Status: approved (G1, 2026-11-03)

## Need
The dev-team and dev-flow agents write and review Angular code in the frontend of a .NET 10 product. The stack index lists `angular` only as planned, so an agent loads no frontend rules today. Without rules, an agent writes the Angular of its training data (NgModules, `*ngIf`, constructor injection, `BehaviorSubject` state, Karma tests). It also skips the checks that only a browser shows. In the calibration of the delivery suite, a plain session built a delete dialog that passed its own tests. But the page scrolled sideways at 390 px.

Gap analysis of the official skills (commit 39cea51, 2026-11-02): they assume a new Angular 22 app. They have no rules per version, no rules for existing code, no "never write" list, and no check other than `ng build`. They also have no review checklist and no NgRx. They do not cover RxJS flattening or subscription cleanup. These topics have 0 hits in all 46 files: server errors on form fields, 409 and ETag, focus after an action, and live regions. Also 390 px, lint, axe, and `HttpTestingController`.

Three official samples disable the submit button. The testing guide forbids `detectChanges()`, which is wrong for a Karma or zone app.

## Workflow
Open: Q2. Proposed:
1. Read the versions in `package.json` and `angular.json`. Read one existing feature. Goes wrong when: the agent uses the defaults of a new app in an old app.
2. Write the component, the template, the service, the route, and the form. Goes wrong when: the code uses an old API. Or the template misses a label, a focus move, or a small screen.
3. Write the tests. Goes wrong when: the tests check the code, not what the user sees.
4. Run the checks (build, lint, unit tests, the rule check, a browser check). Goes wrong when: no step runs the page in a browser.
5. Review a change with the checklist.

## Output
Code changes in the Angular project, and the report of stack rule 8. Reader: the agent and the reviewer.

## Triggers
- Must start: "add an edit dialog to the product page", "review this Angular component", "move this form to signal forms".
- Must not start: "add an endpoint for products" (`dev-stack:aspnetcore`), "is this token storage safe" (`dev-stack:secure-coding`), "design the product page" (`ux-design`).

## Success criteria
Open: Q8.

## Limits
- No security rules. They stay in `dev-stack:secure-coding` (`references/frontend.md`), because one source per rule.
- No visual design. That is `ux-design`.
- No AngularJS 1.x. That is a separate planned skill.

## Language and runtime
English, ASD-STE100, as each dev-stack skill. Claude Code. Facts verified against Angular 22.

## Decisions
| No | Decision | Who |
|----|----------|-----|
| Q1 | The official Angular skills (`angular/skills`: `angular-developer`, `angular-new-app`) are primary. This skill fills only their gaps and does not repeat them. | human |
| Q2 | The 5 workflow steps above. | recommended, accepted |
| Q3 | Bad cases: old APIs, tests that check the code and not what the user sees, no browser run. | recommended, accepted |
| Q4 | Scope = the gaps of the official skills (gap analysis in progress). Security, visual design, and AngularJS stay out. | human |
| Q5 | Rules for Angular 22, a short note for 21. Version 20 and older: tell the human (Q9 drops 20). | human |
| Q6 | Check: `ng build`, `ng lint`, `ng test` (Vitest), `stackcheck.py` with `ANG-*` rules, Playwright at 390 and 1440 px with axe and a Tab walk for each changed screen. | recommended, accepted |
| Q7 | Test: the dev-stack eval method (3 prompts x 3 runs with and without, 10 trigger queries, planted review cases), field test `dash-delete-a11y`. | recommended, accepted |
| Q8 | The project installs the official skills (`npx skills add https://github.com/angular/skills`, later by `dev-setup`). When they are missing, this skill gives the command and works on its own rules. No copy in this plugin. | recommended, accepted |
| Q9 | Scope: the 10 gaps of the analysis, without version 20. The owner prefers zoneless. Transloco (`@jsverse/transloco` 8.4.0) is the default for translations. `httpResource` for reads, `HttpClient` for writes. The route or the component that uses a service provides it. Root only when the whole app needs one instance. | human |
| Q10 | When an official rule is wrong for the app, the rule of this skill wins. The skill lists each correction with its source. No issues filed upstream. | recommended, accepted |
| Q11 | New code follows the forms and state of the app. A new app: Signal Forms and signals. No NgRx in an app without it. Submit stays enabled, errors show on submit, focus goes to the first error. Server field errors come from ProblemDetails `errors`. Each default has an `ANG-xx` ID in `references/decisions.md`. | recommended, accepted |
| Q12 | About 190 lines in `SKILL.md`, 4 references (versions, errors and forms, accessibility and tests, NgRx), `rules.json` for the "never write" table. No text copied from the official skills. | recommended, accepted |
| Q13 | The stack index loads `angular-developer` first, then `dev-stack:angular`, for `@angular/*`. | recommended, accepted |
| Q14 | The owner removes the 3 community Angular skills from `~/.claude/skills`. | recommended, accepted |
