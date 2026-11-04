import { CurrencyPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, OnInit, effect, inject, signal, viewChildren } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY, Subject, catchError, mergeMap } from 'rxjs';

import { Product } from '../products/product';
import { ProductsService } from '../products/products.service';
import { toLocalDate, today } from '../shared/dates';
import { PriceLine } from './price-change';
import { PriceChangeService } from './price-change.service';

function notInPast(control: AbstractControl<string>): ValidationErrors | null {
  return control.value && toLocalDate(control.value) < today() ? { past: true } : null;
}

@Component({
  selector: 'app-price-change-page',
  imports: [CurrencyPipe, ReactiveFormsModule],
  templateUrl: './price-change-page.html',
  styleUrl: './price-change-page.css',
})
export class PriceChangePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly priceChanges = inject(PriceChangeService);
  private readonly checkboxes = viewChildren<ElementRef<HTMLInputElement>>('rowCheckbox');

  protected readonly products = toSignal(inject(ProductsService).list(), { initialValue: [] });
  protected readonly rows = this.priceChanges.selection;
  protected readonly preview = signal<PriceLine[]>([]);
  protected readonly message = signal('');
  protected readonly applyClicks = new Subject<void>();

  protected readonly form = new FormGroup({
    percentage: new FormControl<number | null>(null, [Validators.required, Validators.min(-90), Validators.max(100)]),
    effectiveDate: new FormControl('', { nonNullable: true, validators: [Validators.required, notInPast] }),
  });
  private readonly percentage = toSignal(this.form.controls.percentage.valueChanges, { initialValue: null });

  constructor() {
    effect(() => {
      const ids = this.rows().map((row) => row.id);
      const percentage = this.percentage();
      if (!ids.length || percentage === null) {
        this.preview.set([]);
        return;
      }
      this.http
        .get<PriceLine[]>('/api/price-changes/preview', { params: { ids: ids.join(','), percentage } })
        .subscribe((lines) => this.preview.set(lines));
    });

    this.applyClicks
      .pipe(
        mergeMap(() => {
          const { percentage, effectiveDate } = this.form.getRawValue();
          return this.priceChanges.apply(percentage ?? 0, effectiveDate).pipe(
            catchError(() => {
              this.message.set('The price change could not be applied. Try again.');
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((change) => this.router.navigate(['/price-changes', change.id]));
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('productId'));
      if (id) {
        this.addProduct(id);
      }
    });
  }

  protected addProduct(id: number | string): void {
    this.http.get<Product>(`/api/products/${id}`).subscribe((product) => {
      this.priceChanges.add(product);
      this.message.set(`${product.name} was added.`);
    });
  }

  protected removeRow(id: number): void {
    this.priceChanges.remove([id]);
  }

  protected removeSelected(): void {
    const ids = this.checkboxes()
      .map((box, index) => (box.nativeElement.checked ? this.rows()[index]?.id : undefined))
      .filter((id): id is number => id !== undefined);
    this.priceChanges.remove(ids);
  }

  protected newPrice(id: number): number | undefined {
    return this.preview().find((line) => line.productId === id)?.newPrice;
  }
}
