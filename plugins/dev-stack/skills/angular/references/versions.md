# Versions, zone.js, and NgModules

Read this file when the app is on Angular 21, when it uses zone.js or NgModules, or when the task is an upgrade.

## Contents

- Support dates
- Angular 21 compared with 22
- An app with zone.js
- An app with NgModules
- Upgrades

## Support dates

Angular releases one major version each year from version 22. The Angular team plans version 23 for June 2027.

| Version | Release | Active support to | LTS to |
|---------|---------|-------------------|--------|
| 22 | 2026-06-03 | 2027-06 | 2028-06 |
| 21 | 2025-11-19 | ended | 2027-06 |
| 20 | 2025-05-28 | ended | 2026-11-28 |
| 19 and older | | ended | ended |

Tell the human when the app is on version 20 or older. Give the date and the upgrade path.

## Angular 21 compared with 22

Write the 21 form in an app on 21. The rules of `SKILL.md` apply, with these changes:

| Item | Angular 21 | Angular 22 |
|------|------------|------------|
| Service | `@Injectable({ providedIn: 'root' })`, or `@Injectable()` and a provider | `@Service()` (root), or `@Service({ autoProvided: false })` and a provider |
| Change detection default of a new component | `Default` (eager): write `changeDetection: ChangeDetectionStrategy.OnPush` | OnPush. The old `Default` is now `Eager` |
| `HttpClient` backend | `provideHttpClient(withFetch())` for Fetch | Fetch is the default. Angular deprecated `withFetch()`. `withXhr()` for upload progress |
| `strictTemplates` | Set in `tsconfig.json` by the project | On by default |
| Signal Forms | Experimental: use only when the project already uses them | Stable |
| `resource`, `rxResource`, `httpResource` | Experimental: check the project decision | Stable |
| Zoneless | The default for new apps | The default for new apps |
| Unit test runner of a new app | Vitest | Vitest |
| Route providers cleanup | `withExperimentalAutoCleanupInjectors()` | `withAutoCleanupInjectors()` (22.2) |

## An app with zone.js

The app has `zone.js` in `polyfills` of `angular.json`, or calls `provideZoneChangeDetection()`.

1. Keep zone.js. Remove it only when the task asks for it.
2. Write new code that also works without zone.js (rule 4 of `SKILL.md`). Then each new component is ready for the move.
3. `NgZone.run` and `runOutsideAngular` can stay in old code. `NgZone.onStable`, `onMicrotaskEmpty`, and `isStable` do not work in a zoneless app: do not add new uses.
4. TestBed is zoneless by default, also when the app loads zone.js. A test that needs zone behavior adds `provideZoneChangeDetection()` to its providers.

The move to zoneless, when the task asks for it:

1. Make each component OnPush-safe: signals, or `markForCheck()` after each async change.
2. Replace `NgZone.onStable` and `onMicrotaskEmpty` with `afterNextRender` or `afterEveryRender`.
3. Remove `provideZoneChangeDetection()`. Remove `zone.js` and `zone.js/testing` from `polyfills` in the `build` and `test` targets, and from `polyfills.ts`.
4. Run `npm uninstall zone.js`. Run all tests and the browser check of each main screen.

## An app with NgModules

1. A new component in a feature that uses an NgModule: declare it in that module, with `standalone: false`. A new feature: standalone components and lazy routes.
2. Do not run the standalone migration as part of a feature. It is a task of its own: `ng g @angular/core:standalone`. It has three steps (convert, remove modules, bootstrap), each with a build and a commit.
3. Angular deprecated `HttpClientModule` and `HttpClientTestingModule`. Use `provideHttpClient()` and `provideHttpClientTesting()` also in a module (`providers`).

## Upgrades

Upgrade one major version at a time with `ng update @angular/core@<n> @angular/cli@<n>`. Read the migrations that it ran, and commit each version alone. Version 22 adds `changeDetection: ChangeDetectionStrategy.Eager` to each component without a strategy, sets `strictTemplates: false` where it was off, and adds `withXhr()` or removes `withFetch()`. These keep the old behavior. Move each component to OnPush later, one feature at a time, with its tests.
