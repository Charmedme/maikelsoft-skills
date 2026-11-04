import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { translocoTesting } from '../../../testing/transloco-testing';
import { ProductList } from './product-list';

describe('ProductList', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductList, translocoTesting()],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
  });

  it('shows one row for each product', async () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.autoDetectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/me').flush({ name: 'Ann', roles: ['catalog'] });
    http
      .expectOne('/api/products')
      .flush([
        { id: 1, name: 'Chair', code: 'CH-001', price: 49.5, availableFrom: '2026-01-15' },
        { id: 2, name: 'Desk', code: 'DS-002', price: 199, availableFrom: '2026-02-01' },
      ]);
    await fixture.whenStable();

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Chair');
    expect(rows[0].textContent).toContain('CH-001');
    expect(rows[1].textContent).toContain('€199.00');
  });

  it('says so when there are no products', async () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.autoDetectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/me').flush({ name: 'Ann', roles: ['catalog'] });
    http.expectOne('/api/products').flush([]);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('There are no products yet.');
  });
});
