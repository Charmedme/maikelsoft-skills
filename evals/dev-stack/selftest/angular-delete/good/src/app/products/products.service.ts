import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';

import { NewProduct, Product } from './product';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly products$ = new BehaviorSubject<Product[]>([]);
  private loaded = false;

  list(): Observable<Product[]> {
    if (!this.loaded) {
      this.loaded = true;
      this.http.get<Product[]>('/api/products').subscribe((products) => this.products$.next(products));
    }
    return this.products$.asObservable();
  }

  create(product: NewProduct): Observable<Product> {
    return this.http
      .post<Product>('/api/products', product)
      .pipe(tap((created) => this.products$.next([...this.products$.value, created])));
  }

  delete(id: number): Observable<void> {
    return this.http
      .delete<void>(`/api/products/${id}`)
      .pipe(tap(() => this.products$.next(this.products$.value.filter((product) => product.id !== id))));
  }
}
