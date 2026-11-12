import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ProductEditService } from './product-edit.service';

describe('ProductEditService', () => {
  let service: ProductEditService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ProductEditService);
    http = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('loads a product', () => {
    service.load('2').subscribe((response) => expect(response.body?.name).toBe('Desk lamp'));
    http.expectOne('/api/products/2').flush({ id: 2, name: 'Desk lamp' });
  });

  it('sends If-Match', () => {
    service.save('2', { name: 'a', code: 'AB-123', price: 1, availableFrom: '2026-01-01' }, '"1"').subscribe();
    expect(http.expectOne('/api/products/2').request.headers.get('If-Match')).toBe('"1"');
  });
});
