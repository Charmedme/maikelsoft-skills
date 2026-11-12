import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { translocoTesting } from '../../../testing/transloco-testing';
import { ProductEdit, toFieldErrors } from './product-edit';

const lamp = { id: 2, name: 'Desk lamp', code: 'DL-210', price: 39.95, availableFrom: '2026-02-01' };

describe('ProductEdit', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductEdit, translocoTesting()],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function load() {
    const fixture = TestBed.createComponent(ProductEdit);
    fixture.componentRef.setInput('id', '2');
    fixture.autoDetectChanges();
    TestBed.tick();
    http.expectOne('/api/products/2').flush(lamp, { headers: { ETag: '"4"' } });
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function input(element: HTMLElement, id: string) {
    return element.querySelector<HTMLInputElement>(`#${id}`)!;
  }

  it('shows the product in the form', async () => {
    const { element } = await load();

    expect(input(element, 'name').value).toBe('Desk lamp');
    expect(input(element, 'availableFrom').value).toBe('2026-02-01');
  });

  it('sends the ETag in If-Match and the date as it was typed', async () => {
    const { element, fixture } = await load();
    const date = input(element, 'availableFrom');
    date.value = '2026-03-01';
    date.dispatchEvent(new Event('input'));
    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    const request = http.expectOne({ method: 'PUT', url: '/api/products/2' });
    expect(request.request.headers.get('If-Match')).toBe('"4"');
    expect(request.request.body.availableFrom).toBe('2026-03-01');
    request.flush({ ...lamp, availableFrom: '2026-03-01' });
  });

  it('shows the server error next to the field', async () => {
    const { element, fixture } = await load();
    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    http
      .expectOne({ method: 'PUT', url: '/api/products/2' })
      .flush(
        { title: 'One or more validation errors occurred.', status: 400, errors: { Code: ['The code is already used.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await fixture.whenStable();

    const code = input(element, 'code');
    expect(code.getAttribute('aria-invalid')).toBe('true');
    expect(element.querySelector(`#${code.getAttribute('aria-describedby')}`)?.textContent).toContain(
      'The code is already used.',
    );
  });

  it('keeps the typed values on a conflict', async () => {
    const { element, fixture } = await load();
    const name = input(element, 'name');
    name.value = 'Desk lamp typed';
    name.dispatchEvent(new Event('input'));
    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    http.expectOne({ method: 'PUT', url: '/api/products/2' }).flush(null, { status: 412, statusText: 'Precondition Failed' });
    await fixture.whenStable();

    expect(name.value).toBe('Desk lamp typed');
    expect(element.textContent).toContain('Someone else changed this product');
  });

  it('maps the PascalCase keys of the API to the fields', () => {
    expect(toFieldErrors({ AvailableFrom: ['Wrong date.'], Other: ['x'] })).toEqual({ availableFrom: 'Wrong date.' });
  });
});
