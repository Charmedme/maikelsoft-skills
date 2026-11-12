# Errors, conflicts, and forms

The code below compiles with Angular 22.2 and TypeScript 6 (strict). Vitest tests checked the two error mappers and `toDateOnly` in the time zone Pacific/Auckland. It belongs to rules 8, 12, 14 to 18, and 24 of `SKILL.md`.

## Contents

- Server errors on reactive form fields
- Server errors in Signal Forms
- The error template
- Save with ETag, one request per action, and a fresh list
- Dates without a time

## Server errors on reactive form fields

ASP.NET Core sends a 400 ValidationProblemDetails. The keys of `errors` are the C# property paths: `Name`, `Lines[0].Price`. The controls are camelCase. This function puts each message on its control and returns the messages that have no control.

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormArray, FormGroup } from '@angular/forms';

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/** Puts the messages of a 400 ValidationProblemDetails on the matching controls. Returns the other messages. */
export function applyServerErrors(form: FormGroup, error: HttpErrorResponse): string[] {
  const problem = (error.error ?? {}) as ProblemDetails;
  const unmatched: string[] = [];
  for (const [key, messages] of Object.entries(problem.errors ?? {})) {
    const control = findControl(form, key);
    if (control) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }
  return unmatched;
}

/** Finds "Lines[0].Price" as lines.0.price, without case. */
function findControl(root: AbstractControl, key: string): AbstractControl | null {
  let current: AbstractControl | null = root;
  for (const part of key.replace(/\[(\d+)\]/g, '.$1').split('.').filter(Boolean)) {
    if (!(current instanceof FormGroup || current instanceof FormArray)) {
      return null;
    }
    const name: string | undefined = Object.keys(current.controls).find(
      (c) => c.toLowerCase() === part.toLowerCase(),
    );
    current = name === undefined ? null : current.get(name);
  }
  return current === root ? null : current;
}
```

The `server` error stays until the next submit. Clear it when the user edits the field: `control.valueChanges` with `takeUntilDestroyed()`, or set `updateOn` and validate again on submit.

## Server errors in Signal Forms

The action of `submit()` returns the errors. A returned error with `fieldTree` goes to that field. Signal Forms clear it when the user edits the field.

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { FieldTree, ValidationError } from '@angular/forms/signals';

/** Maps a 400 ValidationProblemDetails to submit errors of a Signal Form. */
export function toSubmitErrors<T extends object>(
  form: FieldTree<T>,
  error: HttpErrorResponse,
): ValidationError.WithOptionalFieldTree[] {
  const problem = (error.error ?? {}) as ProblemDetails;
  const fields = form as unknown as Record<string, FieldTree<unknown>>;
  return Object.entries(problem.errors ?? {}).flatMap(([key, messages]) => {
    const name = Object.keys(fields).find((f) => f.toLowerCase() === key.toLowerCase());
    return messages.map((message) =>
      name === undefined
        ? { kind: 'server', message }
        : { kind: 'server', message, fieldTree: fields[name] },
    );
  });
}
```

Use it in the action. `submit()` marks each field as touched, runs only when the form is valid, and sets `submitting()` while the action runs. A second call during the action returns `false`, so a double click sends one request.

```ts
protected async save(): Promise<void> {
  await submit(this.productForm, async () => {
    try {
      await firstValueFrom(this.http.put(`/api/products/${this.id()}`, this.model(), {
        headers: { 'If-Match': this.etag() },
      }));
      return undefined;
    } catch (e) {
      if (e instanceof HttpErrorResponse && e.status === 400) {
        return toSubmitErrors(this.productForm, e);
      }
      throw e;
    }
  });
}
```

## The error template

Each error has an `id`, and the field points to it. Only `submitting()` disables the submit button.

```html
<label for="code">{{ t('product.code') }}</label>
<input id="code" formControlName="code"
       [attr.aria-invalid]="codeError() ? 'true' : null"
       [attr.aria-describedby]="codeError() ? 'code-error' : null" />
@if (codeError(); as message) {
  <p id="code-error" class="field-error">{{ message }}</p>
}

<button type="submit" [disabled]="submitting()">{{ t('common.save') }}</button>
```

On submit with errors, move the focus to the first invalid field. Query the DOM after the render: `afterNextRender(() => host.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(), { injector })`.

## Save with ETag, one request per action, and a fresh list

The read keeps the ETag. The save sends it, blocks a second save, keeps the values on 412, and reloads the list.

```ts
import { HttpClient, HttpErrorResponse, httpResource } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';

interface Product { id: number; name: string; code: string; price: number; availableFrom: string }

@Component({ selector: 'app-product-edit', template: '' })
export class ProductEdit {
  private readonly http = inject(HttpClient);
  readonly id = input.required<string>();

  protected readonly product = httpResource<Product>(() => `/api/products/${this.id()}`);
  protected readonly submitting = signal(false);
  protected readonly conflict = signal(false);

  protected async save(value: Product): Promise<void> {
    if (this.submitting()) {
      return; // one request per action, also when the button is clicked twice
    }
    this.submitting.set(true);
    this.conflict.set(false);
    const etag = this.product.headers()?.get('ETag') ?? '';
    this.http.put<Product>(`/api/products/${this.id()}`, value, { headers: { 'If-Match': etag } })
      .subscribe({
        next: () => { this.submitting.set(false); /* reload the list state here, then navigate */ },
        error: (e: HttpErrorResponse) => {
          this.submitting.set(false);
          if (e.status === 412 || e.status === 409) {
            this.conflict.set(true); // keep the typed values; offer this.product.reload()
          }
        },
      });
  }
}
```

After the save, the list must show the new values. The list can live in a service with a `BehaviorSubject` or in a store that loaded one time. Then call its reload, or set the saved item from the response, before you navigate back.

## Dates without a time

```ts
/** "2026-12-01" from the local parts. toISOString() gives "2026-11-30T11:00:00.000Z" in Auckland. */
export function toDateOnly(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
```

The value of `<input type="date">` is already `yyyy-MM-dd`. Send it as it is. For an instant (a point in time), send `toISOString()` and let the API keep it as `DateTimeOffset`.
