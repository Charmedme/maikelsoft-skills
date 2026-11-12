import { HttpClient } from '@angular/common/http';
import { Service, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { toLocalDate } from '../shared/dates';
import { PriceChange, SelectedProduct } from './price-change';

@Service()
export class PriceChangeService {
  private readonly http = inject(HttpClient);

  readonly selection = signal<SelectedProduct[]>([]);

  add(product: SelectedProduct): void {
    if (!this.selection().some((p) => p.id === product.id)) {
      this.selection.update((products) => [...products, product]);
    }
  }

  remove(ids: number[]): void {
    this.selection.update((products) => products.filter((p) => !ids.includes(p.id)));
  }

  apply(percentage: number, effectiveDate: string): Observable<PriceChange> {
    return this.http
      .post<PriceChange>('/api/price-changes', {
        productIds: this.selection().map((p) => p.id),
        percentage,
        effectiveDate: toLocalDate(effectiveDate).toISOString().slice(0, 10),
      })
      .pipe(tap(() => this.selection.set([])));
  }
}
