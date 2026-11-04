import { HttpErrorResponse, httpResource } from '@angular/common/http';
import {
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChildren,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { Product } from '../product';
import { ProductsService } from '../products.service';

type FieldName = 'name' | 'code' | 'price' | 'availableFrom';
const FIELDS: FieldName[] = ['name', 'code', 'price', 'availableFrom'];

@Component({
  selector: 'app-product-edit',
  imports: [ReactiveFormsModule, RouterLink, TranslocoDirective],
  templateUrl: './product-edit.html',
})
export class ProductEdit {
  private readonly productsService = inject(ProductsService);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private readonly inputs = viewChildren<ElementRef<HTMLInputElement>>('field');

  readonly id = input.required<string>();

  protected readonly product = httpResource<Product>(() => `/api/products/${this.id()}`);
  protected readonly notFound = computed(() => (this.product.error() as HttpErrorResponse | undefined)?.status === 404);
  protected readonly saving = signal(false);
  protected readonly conflict = signal(false);
  protected readonly serverErrors = signal<Partial<Record<FieldName, string>>>({});
  private readonly submitted = signal(false);

  protected readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    code: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Z0-9-]{3,20}$/)],
    }),
    price: new FormControl<number | null>(null, [Validators.required, Validators.min(0)]),
    availableFrom: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    effect(() => {
      if (this.product.hasValue()) {
        const product = this.product.value();
        this.form.reset({ name: product.name, code: product.code, price: product.price, availableFrom: product.availableFrom });
      }
    });
  }

  protected hasError(field: FieldName): boolean {
    const control = this.form.controls[field];
    return !!this.serverErrors()[field] || (control.invalid && (control.touched || this.submitted()));
  }

  protected save(): void {
    this.submitted.set(true);
    this.form.markAllAsTouched();
    if (this.saving()) {
      return;
    }
    if (this.form.invalid) {
      this.focusFirstError();
      return;
    }
    const etag = this.product.headers()?.get('ETag');
    const value = this.form.getRawValue();
    this.saving.set(true);
    this.conflict.set(false);
    this.serverErrors.set({});
    this.productsService
      .update(Number(this.id()), { ...value, price: value.price ?? 0 }, etag ?? '*')
      .subscribe({
        next: () => this.router.navigate(['/products']),
        error: (error: HttpErrorResponse) => {
          this.saving.set(false);
          if (error.status === 412) {
            this.conflict.set(true);
          } else if (error.status === 400 && error.error?.errors) {
            this.serverErrors.set(toFieldErrors(error.error.errors));
            this.focusFirstError();
          }
        },
      });
  }

  protected clearServerError(field: FieldName): void {
    if (this.serverErrors()[field]) {
      this.serverErrors.update((errors) => ({ ...errors, [field]: undefined }));
    }
  }

  private focusFirstError(): void {
    afterNextRender(
      () => {
        const first = FIELDS.find((field) => this.hasError(field));
        this.inputs()
          .find((ref) => ref.nativeElement.id === first)
          ?.nativeElement.focus();
      },
      { injector: this.injector },
    );
  }
}

/** The API sends PascalCase keys (Name, AvailableFrom); the form uses camelCase. */
export function toFieldErrors(errors: Record<string, string[]>): Partial<Record<FieldName, string>> {
  const result: Partial<Record<FieldName, string>> = {};
  for (const [key, messages] of Object.entries(errors)) {
    const field = (key.charAt(0).toLowerCase() + key.slice(1)) as FieldName;
    if (FIELDS.includes(field) && messages.length) {
      result[field] = messages.join(' ');
    }
  }
  return result;
}
