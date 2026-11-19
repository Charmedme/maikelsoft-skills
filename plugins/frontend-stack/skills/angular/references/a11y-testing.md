# Accessibility, tests, and the browser check

The code compiles with Angular 22.2, `@angular/cdk` 22.2, and TypeScript 6. It belongs to rules 17 to 22 and 26 to 29 of `SKILL.md`. It also belongs to step 5 of the check.

## Contents

- Delete with a confirm dialog
- One status region for each page
- Unit tests that look like the user
- HTTP tests
- Karma and Vitest
- The browser check

## Delete with a confirm dialog

The dialog has a name, takes the focus, and closes on Escape. Cancel gives the focus back to the Delete button. After a delete, the focus goes to the next row, or to the heading when the list is empty, because the opener is gone.

With the CDK dialog (`@angular/cdk/dialog`): `restoreFocus` is `true` by default. It gives the focus back to the opener on cancel. After a delete, move the focus yourself when the dialog closed.

```ts
const ref = this.dialog.open<boolean>(ConfirmDelete, {
  data: { name: product.name },
  ariaLabelledBy: 'confirm-title',
  autoFocus: 'first-tabbable',
});
ref.closed.subscribe((confirmed) => {
  if (confirmed) {
    this.remove(product, index); // after the delete: focus to the next row, see below
  }
});
```

With a native dialog, `showModal()` makes the page behind it inert, and Escape closes it:

```html
<dialog #confirm aria-labelledby="confirm-title" (close)="onClose(confirm.returnValue)">
  <h2 id="confirm-title">{{ t('products.delete.title', { name: pending()?.name }) }}</h2>
  <form method="dialog">
    <button value="cancel" autofocus>{{ t('common.cancel') }}</button>
    <button value="delete">{{ t('products.delete.confirm') }}</button>
  </form>
</dialog>
```

Move the focus after the list rendered again:

```ts
private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
private readonly injector = inject(Injector);
private readonly heading = viewChild.required<ElementRef<HTMLElement>>('heading');

private focusAfterDelete(index: number): void {
  afterNextRender(() => {
    const buttons = this.host.nativeElement.querySelectorAll<HTMLElement>('[data-row-action="delete"]');
    (buttons[Math.min(index, buttons.length - 1)] ?? this.heading().nativeElement).focus();
  }, { injector: this.injector });
}
```

Give the heading `tabindex="-1"` so that it can take the focus.

## One status region for each page

The region is in the DOM from the first render. Only its text changes, so a screen reader reads each new text.

```html
<p role="status" class="visually-hidden-until-text">{{ status() }}</p>
```

```ts
protected readonly status = signal('');
// after the delete:
this.status.set(this.transloco.translate('products.deleted', { name: product.name }));
```

An error that stops the user goes in `role="alert"`. A failed delete keeps the row and shows the error near the list.

## Unit tests that look like the user

Find elements by role and text. Do not read private fields of the component.

```ts
it('removes the product after the user confirms', async () => {
  const fixture = TestBed.createComponent(ProductList);
  await fixture.whenStable();
  const root: HTMLElement = fixture.nativeElement;

  root.querySelector<HTMLButtonElement>('button[aria-label="Delete Chair"]')!.click();
  await fixture.whenStable();
  const dialog = document.querySelector('[role="dialog"]')!;
  expect(dialog.textContent).toContain('Chair');
});
```

For Angular Material, use the harness of the component (`MatDialogHarness`, `MatTableHarness`). Test one failure (the API returns 500) and one empty list for each new list or form.

## HTTP tests

```ts
beforeEach(() => {
  TestBed.configureTestingModule({
    imports: [ProductEdit],
    providers: [provideHttpClient(), provideHttpClientTesting()], // this order
  });
  http = TestBed.inject(HttpTestingController);
});

afterEach(() => http.verify());

it('shows the server error at the code field', async () => {
  // ... fill the form and submit
  http.expectOne({ method: 'PUT', url: '/api/products/1' }).flush(
    { status: 400, errors: { Code: ['Code PD-002 is already in use.'] } },
    { status: 400, statusText: 'Bad Request' },
  );
  // expect the message next to the code field, and aria-invalid on the field
});
```

A network failure is `req.error(new ProgressEvent('error'))`, status 0.

## Karma and Vitest

| Item | Karma with Jasmine | Vitest |
|------|--------------------|--------|
| Spy | `jasmine.createSpy()`, `spyOn(obj, 'm')` | `vi.fn()`, `vi.spyOn(obj, 'm')` |
| Time | `fakeAsync`, `tick` | `vi.useFakeTimers()`, `vi.advanceTimersByTime` |
| Render | `fixture.detectChanges()` can stay | `await fixture.whenStable()` |

Do not mix the two APIs in one project. A move to Vitest is a task of its own: the builder `@angular/build:unit-test`, the schematic `ng g @schematics/angular:refactor-jasmine-vitest`, and the removal of `karma.conf.js`. The schematic leaves TODO comments to fix.

## The browser check

Run the app (`ng serve` with the API, or the dev setup of the project). Then check each changed screen at 1440 and 390 px, with the action of the change done (dialog open, error shown, item deleted).

1. Axe: no violation with the impact `serious` or `critical`.
2. Sideways scroll: `document.documentElement.scrollWidth <= document.documentElement.clientWidth` at 390 px.
3. Tab walk: press Tab from the top. Each control of the change gets the focus, in the reading order, with a visible focus ring.
4. Focus after each action: `document.activeElement` is not `body` after open, cancel, save, and delete.

Use the Playwright MCP server when the session has it. Else write a Playwright test. `@playwright/test` and `@axe-core/playwright` are new packages: ask the human first (stack rule 5).

```ts
import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

for (const width of [1440, 390]) {
  test(`product list at ${width} px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/products');
    await page.getByRole('button', { name: /Delete/ }).first().click();
    const results = await new AxeBuilder({ page }).analyze();
    expect(results.violations.filter((v) => v.impact === 'serious' || v.impact === 'critical')).toEqual([]);
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
    await page.keyboard.press('Escape');
    await expect(page.getByRole('button', { name: /Delete/ }).first()).toBeFocused();
  });
}
```

Write the result of each check in the Checks table of the report. A check that did not run is "not run", with the reason.
