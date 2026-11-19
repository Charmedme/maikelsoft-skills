# NgRx and Transloco

Read this file when the app uses NgRx Store, NgRx SignalStore, or Transloco. It belongs to rules 23 and 25 of `SKILL.md`.

## Contents

- NgRx Store in an existing app
- NgRx SignalStore
- Transloco

## NgRx Store in an existing app

The app has `@ngrx/store` (22.0.1 supports Angular 22).

1. Shared server state stays in the Store. Do not add a signal service for the same data next to it: two sources give two truths.
2. Read state in new code with `store.selectSignal(selector)`. Keep `store.select` in old files.
3. Local UI state (an open panel, a draft) is a `signal` in the component.
4. An effect puts the HTTP call inside a flattening operator, and `catchError` inside the inner pipe. A `catchError` on the outer stream ends the effect after the first error.

```ts
saveProduct$ = createEffect(() =>
  this.actions$.pipe(
    ofType(ProductActions.save),
    exhaustMap(({ product }) =>
      this.api.save(product).pipe(
        map((saved) => ProductActions.saveSuccess({ product: saved })),
        catchError((error: HttpErrorResponse) => of(ProductActions.saveFailure({ error }))),
      ),
    ),
  ),
);
```

5. A save uses `exhaustMap`: it ignores a second click while the first save runs. A load that a newer load replaces uses `switchMap`.
6. A component does not call `store.subscribe()` (rule `@ngrx/no-store-subscription`). Use `selectSignal` or the `async` pipe.
7. After `saveSuccess`, the reducer updates the item in the list, or an effect loads the list again. Else the list shows the old values.

## NgRx SignalStore

The app has `@ngrx/signals`. A new feature store uses `signalStore` with `withState`, `withComputed`, and `withMethods`. Provide a feature store in the page component (ANG-03), not in root. For async work with RxJS operators, use `rxMethod`. The NgRx docs advise RxJS for complex async side effects.

## Transloco

The app has `@jsverse/transloco` (8.4.0 supports Angular 16 and later).

1. Each UI text is a key: labels, buttons, headings, dialog texts, status and error messages, `aria-label`, `title`, and `placeholder`. Add the key to each language file of the app, for example `en.json` and `nl.json`. Put it next to the other keys of the feature.
2. In a template, use the structural directive. It makes one subscription for the template.

   ```html
   <ng-container *transloco="let t">
     <h2 id="confirm-title">{{ t('products.delete.title', { name: product.name }) }}</h2>
     <button type="button" [attr.aria-label]="t('products.delete.label', { name: product.name })">
       {{ t('common.delete') }}
     </button>
   </ng-container>
   ```

   Import `TranslocoDirective` in the standalone component.
3. In code, use `selectTranslate(key, params)` (an observable), or `translate(key, params)` after the files loaded (for example in an event handler). `translate()` before the load returns the key.
4. A feature with many texts uses a scope (`provideTranslocoScope('products')`) so that its file loads with the route.
5. A test provides Transloco with the real language file or with `TranslocoTestingModule.forRoot({ langs: { en } })`. A test that checks a text then checks the real text.
