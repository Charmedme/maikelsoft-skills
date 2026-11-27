import { TestBed } from '@angular/core/testing';

import { CompareList } from './compare-list';

describe('CompareList', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('keeps at most 4 products', () => {
    const list = TestBed.inject(CompareList);
    [1, 2, 3, 4, 5].forEach((id) => list.toggle(id));

    expect(list.productIds()).toEqual([1, 2, 3, 4]);
    expect(list.full()).toBe(true);
  });

  it('removes a product on the second toggle', () => {
    const list = TestBed.inject(CompareList);
    list.toggle(7);
    list.toggle(7);

    expect(list.count()).toBe(0);
  });
});
