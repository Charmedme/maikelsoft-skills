import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Product } from './product';
import { ProductsService } from './products.service';

const chair: Product = { id: 1, name: 'Chair', code: 'CH-001', price: 49.5, availableFrom: '2026-01-15' };

describe('ProductsService', () => {
  let service: ProductsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the products from the API', () => {
    let received: Product[] = [];
    service.list().subscribe((products) => (received = products));

    http.expectOne('/api/products').flush([chair]);

    expect(received).toEqual([chair]);
  });

  it('adds a created product to the list', () => {
    let received: Product[] = [];
    service.list().subscribe((products) => (received = products));
    http.expectOne('/api/products').flush([chair]);

    service.create({ name: 'Desk', code: 'DS-002', price: 199, availableFrom: '2026-02-01' }).subscribe();
    const request = http.expectOne({ method: 'POST', url: '/api/products' });
    request.flush({ ...request.request.body, id: 2 });

    expect(received.map((product) => product.name)).toEqual(['Chair', 'Desk']);
  });

  it('removes a deleted product', () => {
    let received: Product[] = [];
    service.list().subscribe((products) => (received = products));
    http.expectOne('/api/products').flush([chair]);

    service.delete(1).subscribe();
    http.expectOne({ method: 'DELETE', url: '/api/products/1' }).flush(null);

    expect(received).toEqual([]);
  });

  it('calls the delete endpoint', () => {
    service.delete(5).subscribe();
    const request = http.expectOne('/api/products/5');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
