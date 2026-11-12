import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PriceChangeService } from './price-change.service';

const lamp = { id: 2, name: 'Desk lamp', code: 'DL-210', price: 39.95 };
const chair = { id: 3, name: 'Office chair', code: 'OC-300', price: 189 };

describe('PriceChangeService', () => {
  let service: PriceChangeService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClientTesting(), provideHttpClient()],
    });
    service = TestBed.inject(PriceChangeService);
    http = TestBed.inject(HttpTestingController);
  });

  it('adds a product once', () => {
    service.add(lamp);
    service.add(lamp);

    expect(service.selection()).toEqual([lamp]);
  });

  it('removes the given products', () => {
    service.add(lamp);
    service.add(chair);
    service.remove([2]);

    expect(service.selection()).toEqual([chair]);
  });

  it('does not call the API when products are selected', () => {
    service.add(lamp);
    service.add(chair);

    http.expectNone('/api/price-changes');
    expect(service.selection().length).toBe(2);
  });
});
