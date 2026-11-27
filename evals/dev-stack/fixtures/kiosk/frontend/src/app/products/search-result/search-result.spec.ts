import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { SearchResult } from './search-result';

describe('SearchResult', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([{ path: 'search', component: SearchResult }])],
    });
  });

  it('shows the products that match the search', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/search?q=lamp', SearchResult);
    TestBed.inject(HttpTestingController)
      .expectOne((r) => r.url === '/api/products/search' && r.params.get('q') === 'lamp')
      .flush([
        { id: 1, name: 'Desk Lamp', description: 'A steady <em>warm</em> light.', price: 39.95, imageUrl: null },
        { id: 2, name: 'Reading Lamp', description: 'Clip-on lamp.', price: 24.5, imageUrl: null },
      ]);
    harness.detectChanges();

    const names = Array.from((harness.routeNativeElement as HTMLElement).querySelectorAll('.product h2')).map((e) => e.textContent);
    expect(names).toEqual(['Desk Lamp', 'Reading Lamp']);
    expect((harness.routeNativeElement as HTMLElement).querySelector('.description em')?.textContent).toBe('warm');
  });

  it('says so when nothing matches', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/search?q=sofa', SearchResult);
    TestBed.inject(HttpTestingController).expectOne((r) => r.url === '/api/products/search').flush([]);
    harness.detectChanges();

    expect((harness.routeNativeElement as HTMLElement).textContent).toContain('No products match your search.');
  });
});
