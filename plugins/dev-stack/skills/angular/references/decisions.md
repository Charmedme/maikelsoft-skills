# Team decisions: angular

The decisions that a team makes one time for new Angular code. Ask each decision when the work first needs it (stack rule 4). Existing code keeps its own style. The owner of this repository accepted the recommendations on 2026-11-03.

Batch the questions: ask ANG-01, ANG-03, ANG-04, ANG-05, and ANG-06 together as one "first feature" question. Ask the others when the work needs them.

## Contents

- ANG-01. Change detection
- ANG-02. Reads and writes
- ANG-03. Service scope
- ANG-04. Forms library
- ANG-05. State
- ANG-06. Translations
- ANG-07. Server errors
- ANG-08. Concurrency
- ANG-09. Submit button
- ANG-10. Dialogs
- ANG-11. Dates
- ANG-12. Test runner

## ANG-01. Change detection

- Question: Does new code depend on zone.js?
- Options: Zoneless with OnPush. Zone.js with the eager strategy.
- Recommended: Zoneless and OnPush in a new app. A zone.js app keeps zone.js, and new code works without it. Angular made zoneless the default in version 21 and OnPush the default in 22.

## ANG-02. Reads and writes

- Question: How does a component load and save data?
- Options: `httpResource` for reads and `HttpClient` for writes. `HttpClient` with RxJS for both. NgRx effects.
- Recommended: `httpResource` (stable in 22) for reads: it cancels an old request, and it has the loading and error states. `HttpClient` for writes, one request for each user action.
- Choose another option when: the app uses NgRx for this data (ANG-05), or the read needs RxJS operators (`rxResource`).

## ANG-03. Service scope

- Question: Where is a new service provided?
- Options: Root. The route (`Route.providers`). The component (`providers`).
- Recommended: The shortest life that works. Page state in the component. Services that the pages of one route share in the route. Root only for one instance in the whole app (auth, configuration, translations). A root service keeps its state for the whole session, so a selection or a draft leaks into the next visit of the page.
- Note: route providers live until the app ends, unless the app calls `withAutoCleanupInjectors()` (22.2).

## ANG-04. Forms library

- Question: Which forms library does a new form use?
- Options: Signal Forms. Reactive forms. Template-driven forms.
- Recommended: The library of the app. A new app on 22: Signal Forms.

## ANG-05. State

- Question: Where does shared state live?
- Options: Signals in a service. NgRx Store. NgRx SignalStore.
- Recommended: Signals in a service. NgRx only in an app that has it. Do not add a second state library.

## ANG-06. Translations

- Question: How does the app translate UI text?
- Options: Transloco. `@angular/localize` (one build for each language). Plain text.
- Recommended: Transloco (`@jsverse/transloco`): one build, a language switch at run time, and lazy files for each feature. Each UI text is a key in each language file.

## ANG-07. Server errors

- Question: How does a form show the errors of the API?
- Recommended: The `errors` of a ValidationProblemDetails go to the fields (keys matched without case). Other messages go in a summary above the form. Other status codes as in rule 16 of `SKILL.md`.

## ANG-08. Concurrency

- Question: What happens when two users change the same item?
- Options: `If-Match` with an ETag. A version field in the body. Last write wins.
- Recommended: `If-Match` with the ETag when the API gives one (`dev-stack:aspnetcore`). On 412 or 409, keep the typed values and offer a reload.

## ANG-09. Submit button

- Question: Is the submit button disabled while the form is invalid?
- Recommended: No. The button stays enabled. Submit shows all errors and moves the focus to the first one. Only a running request disables the button.

## ANG-10. Dialogs

- Question: Which dialog does a confirmation use?
- Options: The CDK or Material dialog. The native `<dialog>`. `window.confirm()`.
- Recommended: The dialog that the app already uses. Else `<dialog>` with `showModal()`. `window.confirm()` cannot show translated button texts or keep the style of the app.

## ANG-11. Dates

- Question: How are dates sent to the API?
- Recommended: A date without a time as `yyyy-MM-dd` (C# `DateOnly`). An instant as ISO 8601 with an offset (C# `DateTimeOffset`).

## ANG-12. Test runner

- Question: Which unit test runner?
- Options: Vitest. Karma with Jasmine.
- Recommended: The runner of the app. A new app: Vitest, the default of the CLI since version 21. Browser checks: Playwright.
