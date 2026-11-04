import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { EMPTY, Subject, catchError, exhaustMap } from 'rxjs';

import { NewProduct } from '../product';
import { ProductsService } from '../products.service';

@Component({
  selector: 'app-product-create',
  imports: [ReactiveFormsModule, RouterLink, TranslocoDirective],
  templateUrl: './product-create.html',
})
export class ProductCreate {
  private readonly productsService = inject(ProductsService);
  private readonly router = inject(Router);
  private readonly submits = new Subject<NewProduct>();

  protected readonly saving = signal(false);
  protected readonly failed = signal(false);

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
    this.submits
      .pipe(
        exhaustMap((product) =>
          this.productsService.create(product).pipe(
            catchError(() => {
              this.saving.set(false);
              this.failed.set(true);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.router.navigate(['/products']));
  }

  protected save(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    this.saving.set(true);
    this.failed.set(false);
    this.submits.next({ name: value.name, code: value.code, price: value.price ?? 0, availableFrom: value.availableFrom });
  }
}
