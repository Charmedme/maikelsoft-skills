import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { NewProduct, Product } from '../product';

@Injectable({ providedIn: 'root' })
export class ProductEditService {
  constructor(private http: HttpClient) {}

  load(id: string): Observable<HttpResponse<Product>> {
    return this.http.get<Product>(`/api/products/${id}`, { observe: 'response' });
  }

  save(id: string, product: NewProduct, etag: string): Observable<Product> {
    return this.http.put<Product>(`/api/products/${id}`, product, { headers: { 'If-Match': etag } });
  }
}
