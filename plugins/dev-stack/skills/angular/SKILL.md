---
name: angular
description: Rules for Angular 22 and 21 code in an existing app with an ASP.NET Core backend, as a companion to the official angular-developer skill. Covers the version gate, zone.js and NgModule apps, outdated APIs, service scope, httpResource reads and HttpClient writes, subscription cleanup and double submits, ProblemDetails errors on form fields, ETag and 412 conflicts, stale lists after a save, loading and error states, focus after a dialog or delete, live regions, small screens, Transloco texts, NgRx, dates, tests with Vitest or Karma, and the browser check. Use when the user writes, tests, or reviews Angular components, templates, services, forms, routes, or specs (`@angular/*`, `*.component.ts`, `*.spec.ts`), or translates UI text with Transloco. Not for AngularJS 1.x, security rules such as token storage, XSS, or CSRF (dev-stack:secure-coding), the design of a screen, its layout, or its states before the build (ux-design), or the API endpoints (dev-stack:aspnetcore).
metadata:
  verified: "2026-11-03"
  versions: "Angular 22.2.4 (21.x in references/versions.md), @jsverse/transloco 8.4.0, @ngrx/store 22.0.1, angular-eslint 22.6.0, axe-core 4.14.0, official skills angular/skills 39cea51"
---

# Angular in an existing app

The official skill `angular-developer` (from `angular/skills`) teaches the Angular APIs. It assumes a new Angular 22 app. This skill adds what an agent needs in an existing app. It gives the version gate, the rules for old code, and the behavior that users and tests see. It also gives the checks and the review. It does not repeat the official text.

## First

1. Read `references/stack-rules.md`.
2. Load the official skill `angular-developer`. When it is not installed, tell the human the command `npx skills add https://github.com/angular/skills` and continue with this skill. Where the official text and this skill disagree, this skill wins (section "Corrections to the official skill").
3. Read the versions in `package.json` and `angular.json`:
   - `@angular/core` 22: use this file.
   - 21: also read `references/versions.md`. Write `@Injectable`, not `@Service()`.
   - 20 or older: tell the human. Version 20 ends LTS on 2026-11-28, older versions have no support. The rules still apply, except the 21 and 22 APIs.
4. Read the app facts, and name each one in the report:
   - Change detection: zone.js in `polyfills` or `provideZoneChangeDetection`, or zoneless.
   - Components: standalone, or declared in an NgModule.
   - Forms: Signal Forms, reactive forms, or template-driven forms.
   - State: signals in services, `BehaviorSubject` services, NgRx Store, or NgRx SignalStore.
   - Texts: Transloco, `@angular/localize`, or plain text.
   - Tests: Vitest (`@angular/build:unit-test`) or Karma with Jasmine.
5. Read one existing feature of the same kind (a list, a form, a dialog). The project style wins in existing code (stack rule 3).

Done when you can name the six app facts and the feature that you used as the example.

## Defaults for new code

Existing code keeps its style. For a new decision, use the accepted answer in `docs/dev-team/decisions.md`, else ask the human (stack rule 4). Detail: `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Change detection | Zoneless and OnPush. A zone.js app keeps zone.js, and new code works without it | ANG-01 |
| Reads | `httpResource` (or `resource`, `rxResource`) | ANG-02 |
| Writes | `HttpClient` in a service, one call for each user action | ANG-02 |
| Service scope | Page state: the providers of the page component. Root only for one instance in the whole app | ANG-03 |
| Forms | The forms library of the app. A new app: Signal Forms | ANG-04 |
| State | Signals in a service. NgRx only in an app that has it | ANG-05 |
| Texts | Transloco keys for each UI text, in each language file of the app | ANG-06 |
| Server errors | ProblemDetails `errors` on the fields, the rest in a summary | ANG-07 |
| Conflicts | `If-Match` with the ETag. On 412 or 409, keep the typed values | ANG-08 |
| Submit | Always enabled, validate on submit, focus to the first error | ANG-09 |
| Dialogs | The dialog of the app (CDK, Material). Else `<dialog>` with `showModal()` | ANG-10 |
| Dates | A date without a time is a `yyyy-MM-dd` string. An instant is ISO 8601 with an offset | ANG-11 |
| Tests | The runner of the app. A new app: Vitest. The browser check: Playwright | ANG-12 |

## Rules

### Existing code

1. Change only the files that the task needs. Do not move a file to new APIs when you only touch it. A new file uses the new APIs, except where it plugs into old code. Declare a new component of an NgModule feature in that module. A new part of an old reactive form stays reactive.
2. Bridge old and new at the edge. In a signal component, read a `BehaviorSubject` or `store.select` with `toSignal(observable)`. Use `toObservable(signal)` the other way. Do not mix both styles inside one class.
3. Run a migration schematic (`ng generate @angular/core:...`) only when the task asks for it. Limit it with `--path` and commit it alone, so a review can tell the migration from the feature.

### Change detection

4. New code works without zone.js. Use signals or `markForCheck()` after each async change. Do not use `NgZone.onStable`, `onMicrotaskEmpty`, or a `setTimeout` to trigger a render. Use `afterNextRender` for DOM work. Such code also runs in a zone.js app, so the app can drop zone.js later.
5. Reactive forms do not notify a zoneless app. Read their values through `toSignal(control.valueChanges)` or call `markForCheck()`.

### Services and data

6. Give a service the shortest life that works. Put page state (a selection, a draft, filters) in the `providers` of the page component. Then it ends with the page. `Route.providers` lives until the app ends, unless the app uses `withAutoCleanupInjectors()` (22.2 and later). Root (`@Service()`, `providedIn: 'root'`) is for one instance in the whole app: auth, configuration, translations.
7. Read with `httpResource` and render all four states: loading, error with a retry, empty, and data. Guard `value()` with `hasValue()`, because `value()` throws in the error state. Keep the old data on the page while `isLoading()` is true during a reload.
8. Write with `HttpClient`. One user action sends one request. Block a second submit while the first runs: use `exhaustMap`, or a `submitting` signal that the handler checks. A disabled button alone does not stop a fast double click.
9. In RxJS code, pick the flattening operator by intent:
   - `switchMap` for a search or a load that a newer input replaces.
   - `exhaustMap` for a save.
   - `concatMap` for an ordered queue.
   - `mergeMap` only for independent items.
10. End each `subscribe()` with the component: `takeUntilDestroyed()` in the constructor or a field, or `takeUntilDestroyed(this.destroyRef)` in a method. `toSignal` and the `async` pipe end by themselves. A one-shot HTTP call completes, so it needs no cleanup.
11. Do not call HTTP in an `effect()`. Give the signal to `httpResource` or `resource` as a parameter. An effect that loads data has no cancel, no error state, and no loading state.
12. After a successful write, update the state that shows the data. Set it from the response, call `reload()` on the resource, or reload the list in the service. Look in the existing services for a list that loads one time and then stays (`BehaviorSubject`, `shareReplay`, a store). Else the list shows the old values.
13. An optimistic update keeps a copy of the old value. When the request fails, it restores the copy and shows the error.

### Server errors and conflicts

14. Map a 400 ValidationProblemDetails to the fields. ASP.NET Core sends the keys of `errors` in PascalCase (`Name`, `AvailableFrom`), and the controls are camelCase. Match the keys without case. Show each message at its field. Show a key without a field in a summary above the form. Code for reactive forms and Signal Forms: `references/errors-forms.md`.
15. Send `If-Match` with the ETag of the loaded item on PUT, PATCH, and DELETE when the API gives an ETag. On 412 or 409, keep the typed values. Tell the user that someone else changed the item, and offer a reload. Do not retry.
16. Map the other status codes:
    - 0: a network failure. Show an offline message and a retry.
    - 401: go to the sign-in flow of the app.
    - 403: say that the user has no permission.
    - 404: show "not found", not an empty form.
    - 5xx: show a retry and the trace id from the ProblemDetails.

### Forms and accessibility

17. Each field has a visible `<label for>`. Each error text has an `id`. The field gets `aria-describedby` with that id and `aria-invalid="true"` while the error shows. Then a screen reader reads the error with the field.
18. Keep the submit button enabled. On submit, show all errors. Move the focus to the first field with an error, or to the error summary. Disable the button only while the request runs. A disabled button gives no reason, and a keyboard user cannot reach it.
19. A dialog has a name (`aria-labelledby` on its title), takes the focus when it opens, and closes on Escape. On cancel, the focus goes back to the button that opened it. After a delete, that button is gone. Move the focus to the next row, or to the list heading when the list is empty. CDK and Material dialogs restore the focus by default (`restoreFocus`). Give a selector or an element when the opener is gone.
20. Put the result of an action (saved, deleted, failed) in a live region. The region is in the DOM before the text changes. Use one `role="status"` element for each page, always there, with a text signal. A region that an `@if` creates together with its text is not read. Use `role="alert"` only for an error that stops the user.
21. At 390 px and at 320 px the page does not scroll sideways (WCAG 1.4.10). A wide table scrolls in its own container. A target for a pointer is at least 24 by 24 px (WCAG 2.5.8).
22. Use `<button type="button">` for each click action that is not a link. A click handler on a `div` or `span` has no keyboard support.

### Texts, dates, and state libraries

23. When the app uses Transloco, each new UI text is a key in each language file of the app. This includes labels, buttons, dialog texts, error and status messages, and `aria-label` values. Use the structural directive `*transloco="let t"` in the template. In code, use `selectTranslate` (an observable) or translate after the files loaded. `translate()` before the load returns the key.
24. Send a date without a time as `yyyy-MM-dd`. Make it from the local parts of the date, or use the value of `<input type="date">`. `toISOString()` of a local midnight gives the day before in each time zone east of UTC. Show dates with the `DatePipe` and the locale of the app.
25. In an app with NgRx Store, shared server state stays in the Store. Read it with `store.selectSignal(selector)` in new code. Local UI state is a `signal`. An effect uses `exhaustMap` for a save and `switchMap` for a load. Put `catchError` inside the inner pipe, so the effect stays alive after an error. Detail: `references/state-and-i18n.md`.

### Tests

26. Test what the user sees and does: find elements by role, label, and text (a harness for Material). Test one failure and one empty state for each new list or form.
27. Use the runner of the app. In a Karma app, write Jasmine (`jasmine.createSpy`, `spyOn`). Keep `fixture.detectChanges()` there. TestBed is zoneless by default, and angular.dev lets existing suites keep the call. Move to Vitest only when the task asks for it.
28. HTTP tests: `provideHttpClient()` first, then `provideHttpClientTesting()`. Use `HttpTestingController.expectOne`, `flush` with the status and the ProblemDetails body, and `verify()` after each test.
29. Use fake timers for a debounce or a delay. A real wait makes the test slow and flaky.

## Corrections to the official skill

| Official text | Use instead | Reason |
|---------------|-------------|--------|
| `testing-fundamentals.md`: "Do NOT use `fixture.detectChanges()`" | Rule 27 in a Karma or zone.js suite | angular.dev, zoneless guide: existing suites can keep it |
| `testing-fundamentals.md`: "declare components" in `configureTestingModule` | `imports: [TheComponent]` | Components are standalone by default |
| `creating-services.md`, `di-fundamentals.md`: only `@Service()` | `@Injectable` in Angular 21, and rule 6 for the scope | `@Service()` is new in 22 and provides in root |
| `signal-forms.md`, `reactive-forms.md`, `template-driven-forms.md`: `[disabled]` on submit while the form is invalid | Rule 18 | WCAG 3.3.1, and the user gets no reason |
| `signal-forms.md`: error `<span>` without a link to the field | Rule 17 | WCAG 1.3.1 and 3.3.1 |
| `SKILL.md`: `ng build` is the only check | Section "Check" | A build does not find a lint, test, or browser defect |

## Never write

These rows are in `rules.json` with the ID in brackets, except the rows marked "judge".

| Wrong | Correct |
|-------|---------|
| `*ngIf`, `*ngFor`, `[ngSwitch]` in a new template (ANG-OLD-CONTROL-FLOW) | `@if`, `@for (x of xs; track x.id)`, `@switch` |
| `track $index` on a list that changes (ANG-TRACK-INDEX) | `track item.id` |
| `constructor(private http: HttpClient)` (ANG-CONSTRUCTOR-DI) | `private readonly http = inject(HttpClient);` |
| `ChangeDetectionStrategy.Default` or `Eager` in a new component (ANG-EAGER-CD) | OnPush (the default in 22) |
| `provideHttpClient(withFetch())` in 22 (ANG-WITH-FETCH) | `provideHttpClient()`: Fetch is the default |
| `HttpClientModule`, `HttpClientTestingModule` (ANG-HTTP-MODULE) | `provideHttpClient()`, `provideHttpClientTesting()` |
| `[disabled]="form.invalid"` on submit (ANG-DISABLED-SUBMIT) | Rule 18 |
| `date.toISOString()` for a date field (ANG-ISO-DATE) | Rule 24 |
| `@if (msg()) { <div role="status">` (ANG-LIVE-REGION-IF) | A status element that is always there (rule 20) |
| `effect(() => this.http.get(...))` (judge) | `httpResource(() => ...)` |
| `this.route.paramMap.subscribe(...)` without cleanup (judge) | `toSignal(this.route.paramMap)` or input binding |
| A `BehaviorSubject` for new state in a service (judge) | A private `signal` and `asReadonly()` |

## Check

Run the checks in this order. Fix each error. For each warning, fix it or give the reason.

1. Build. Pass: exit code 0, and no warning in the changed files. `strictTemplates` is on by default in 22.

   ```
   npx ng build
   ```

2. Lint. Pass: 0 errors. When the app has no lint setup, propose `ng add angular-eslint` (a new package, stack rule 5) and continue.

   ```
   npx ng lint
   ```

3. Unit tests. Pass: exit code 0.

   ```
   npx ng test --no-watch
   ```

4. The rule check of this skill. Pass: `stackcheck: 0 errors`. In an existing app, check only the changed files, because old code has many old patterns.

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo> --changed <base branch>
   ```

5. The browser check, for each changed screen. Script and steps: `references/a11y-testing.md`. Pass:
   - Axe shows no serious or critical violation.
   - The page does not scroll sideways at 390 px.
   - The Tab key reaches each control in a logical order.
   - After each action of the change, the focus is on a visible element.

## Review checklist

1. Old APIs in new code: control flow, constructor injection, `withFetch()`, the HTTP modules. tool (ANG-OLD-CONTROL-FLOW, ANG-CONSTRUCTOR-DI, ANG-WITH-FETCH, ANG-HTTP-MODULE)
2. Each `subscribe()` ends with the component, or is a one-shot call. judge
3. A search uses `switchMap` or a resource. A save sends one request for each action. judge
4. No HTTP in an `effect()`. judge
5. Each new service has the shortest scope that works (rule 6). judge
6. Each async view renders loading, error, empty, and data. judge
7. After a write, each view of that data shows the new values (rule 12). judge
8. 400 errors show at the fields, with PascalCase keys mapped. judge
9. Writes send `If-Match` when the API has ETags, and a 412 keeps the typed values. judge
10. Labels, `aria-describedby`, `aria-invalid`, and an enabled submit. tool (ANG-DISABLED-SUBMIT) and judge
11. Focus after open, cancel, and delete. A live region that exists before its text. tool (ANG-LIVE-REGION-IF) and judge
12. No sideways scroll at 390 px. Axe without serious violations. browser check
13. Each new UI text is a translation key in each language file. judge
14. The code sends each date without a time as `yyyy-MM-dd`. tool (ANG-ISO-DATE) and judge
15. `@for` tracks a stable id. tool (ANG-TRACK-INDEX)
16. Tests find elements by role and text, cover one failure, and HTTP tests call `verify()`. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Under "Assumptions", write:

- The Angular version and the six app facts of step 4.
- The text of each new message, dialog, and button, in the default language.
- The element that gets the focus after each new action: open, cancel, save, delete, and error.

## References

- `references/versions.md`: read when the app is on Angular 21, uses zone.js or NgModules, or the task is an upgrade.
- `references/decisions.md`: read when new code needs a decision of the defaults table.
- `references/errors-forms.md`: read when you write a form, a save, server error handling, an ETag flow, or a date field.
- `references/a11y-testing.md`: read when you write a dialog, a delete, a status message, or tests, and before the browser check.
- `references/state-and-i18n.md`: read when the app uses NgRx or Transloco.
- `references/sources.md`: the primary sources and the date of each check.
- `references/stack-rules.md`: the common rules of each dev-stack skill.
- `references/language.md`: the rules for text output.
