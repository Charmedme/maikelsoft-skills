import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Product } from './product';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  search(query: string): Observable<Product[]> {
    return this.http.get<Product[]>('/api/products/search', { params: new HttpParams().set('q', query) });
  }
}
