# Sources: angular

The author checked all facts on 2026-11-02 and 2026-11-03, unless a line says otherwise. Versions from the npm registry on 2026-11-03: `@angular/core` 22.2.4 (next 22.3.0-next.3), `@jsverse/transloco` 8.4.0, `@ngrx/store` 22.0.1, `angular-eslint` 22.6.0, `axe-core` 4.14.0, `@axe-core/playwright` 4.14.0.

## Contents

- The official skills
- Versions and support
- Framework
- Forms and errors
- Accessibility
- State and translations
- Tests

## The official skills

- `angular/skills` at commit 39cea51 (2026-11-02): https://github.com/angular/skills. The skill `angular-developer` and 44 reference files. The gap analysis of 2026-11-02 searched all 46 files: no hits for NgRx, `takeUntilDestroyed`, `HttpTestingController`, ProblemDetails, `aria-live`, `aria-describedby`, or axe.

## Versions and support

- Release and support table: https://angular.dev/reference/releases
- What is new in 22.0 (OnPush default, `Eager`, Fetch default, `withFetch()` deprecated, `strictTemplates` default, `@Service()`, Signal Forms and resources stable, migrations): https://blog.ninja-squad.com/2026/06/03/what-is-new-angular-22.0
- The yearly release cycle (version 23 in June 2027): https://angular.love/angular-new-release-cycle-release-schedule-change-explained
- Release dates: the npm registry (`npm view @angular/core time`).

## Framework

- Zoneless: the default since 21, the steps to remove zone.js, and TestBed without zone.js by default. Existing suites can keep `detectChanges()`. Reactive forms do not notify. https://angular.dev/guide/zoneless
- `takeUntilDestroyed` in and outside an injection context: https://angular.dev/ecosystem/rxjs-interop/take-until-destroyed
- `@Service()` signatures (`autoProvided: false`): https://angular.dev/api/core/Service
- Route and component providers: https://angular.dev/guide/di/defining-dependency-providers
- `withAutoCleanupInjectors` (stable in 22.2): https://angular.dev/api/router/withAutoCleanupInjectors
- Control flow and `track`: https://angular.dev/guide/templates/control-flow
- HTTP requests (unsubscribe cancels the request): https://angular.dev/guide/http/making-requests
- CDK dialog `restoreFocus` (default `true`), `autoFocus` targets: `@angular/cdk` 22.2 package, `types/dialog.d.ts` and `fesm2022/dialog.mjs`.

## Forms and errors

- Signal Forms submission. The action returns errors with `kind`, `message`, and `fieldTree`. `submitting()` is true while the action runs. A second call during the action returns `false`. https://angular.dev/guide/forms/signals/form-submission
- Problem Details for HTTP APIs: https://www.rfc-editor.org/rfc/rfc9457.html
- Conditional requests, `If-Match` and 412: https://www.rfc-editor.org/rfc/rfc9110.html
- `DatePipe` uses the local time zone by default: https://angular.dev/api/common/DatePipe

## Accessibility

- WCAG 2.2 (1.3.1, 1.4.10 Reflow at 320 CSS px, 2.4.11 Focus Not Obscured, 2.5.8 Target Size 24 px, 3.3.1 Error Identification, 4.1.3 Status Messages): https://www.w3.org/TR/WCAG22/
- Live regions: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-live
- The dialog element: https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/dialog
- angular-eslint rules (`prefer-inject`, `prefer-on-push-component-change-detection`, `template/prefer-control-flow`, `template/label-has-associated-control` in the accessibility preset): https://github.com/angular-eslint/angular-eslint

## State and translations

- NgRx selectors and `selectSignal`, effects and error handling, ESLint rules: the NgRx docs in https://github.com/ngrx/platform (`projects/www/src/app/pages/guide/`), checked on 2026-11-03.
- Transloco installation and template use (the structural directive is the recommended form): https://jsverse.gitbook.io/transloco/getting-started/installation and https://jsverse.gitbook.io/transloco/core-concepts/translation-in-the-template

## Tests

- HTTP testing (`provideHttpClient()` before `provideHttpClientTesting()`, `verify()`): https://angular.dev/guide/http/testing
- Testing and the Vitest runner: https://angular.dev/guide/testing
- The move from Karma to Vitest (schematic `refactor-jasmine-vitest`): https://angular.dev/guide/testing/migrating-to-vitest
