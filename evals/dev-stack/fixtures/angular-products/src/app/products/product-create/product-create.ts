import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { ProductsService } from '../products.service';

@Component({
  selector: 'app-product-create',
  imports: [ReactiveFormsModule, RouterLink, TranslocoDirective],
  templateUrl: './product-create.html',
})
export class ProductCreate {
  private readonly productsService = inject(ProductsService);
  private readonly router = inject(Router);

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

  protected save(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    this.saving.set(true);
    this.failed.set(false);
    this.productsService
      .create({ name: value.name, code: value.code, price: value.price ?? 0, availableFrom: value.availableFrom })
      .subscribe({
        next: () => this.router.navigate(['/products']),
        error: () => {
          this.saving.set(false);
          this.failed.set(true);
        },
      });
  }
}
