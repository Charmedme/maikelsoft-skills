import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { ComparePage } from './compare-page';

describe('ComparePage', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([{ path: 'compare', component: ComparePage }], withComponentInputBinding())],
    });
  });

  it('shows the products of a shared link side by side', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/compare?ids=1,2', ComparePage);
    await Promise.resolve();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/products/1').flush({ id: 1, name: 'Desk Lamp', description: '<ul><li>40 cm</li></ul>', price: 39.95, imageUrl: null });
    http.expectOne('/api/products/2').flush({ id: 2, name: 'Reading Lamp', description: 'Clip-on lamp.', price: 24.5, imageUrl: null });
    harness.detectChanges();

    const page = harness.routeNativeElement as HTMLElement;
    expect(Array.from(page.querySelectorAll('strong')).map((e) => e.textContent)).toEqual(['Desk Lamp', 'Reading Lamp']);
    expect(page.querySelector('.details li')?.textContent).toBe('40 cm');
  });

  it('asks for products when the list is empty', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/compare', ComparePage);
    await Promise.resolve();
    harness.detectChanges();

    expect((harness.routeNativeElement as HTMLElement).textContent).toContain('Pick up to 4 products');
  });
});
