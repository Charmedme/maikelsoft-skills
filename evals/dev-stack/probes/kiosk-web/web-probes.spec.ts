import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EnvironmentInjector, runInInjectionContext } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom, isObservable } from 'rxjs';

import { PromoVideo } from '../about/promo-video';
import { adminGuard } from '../auth/admin.guard';
import { authInterceptor } from '../auth/auth.interceptor';
import { AuthService } from '../auth/auth.service';
import { SearchResult } from '../products/search-result/search-result';

// Hidden probes of the case kiosk-web: W1 to W4, and the decoy D3.
function base64Url(value: object): string {
  return btoa(JSON.stringify(value)).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
}

function setup(): void {
  localStorage.clear();
  sessionStorage.clear();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      provideRouter([
        { path: 'search', component: SearchResult },
        { path: 'about', component: PromoVideo },
      ]),
    ],
  });
}

async function search(query: string, products: unknown[]): Promise<HTMLElement> {
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl('/search?q=' + encodeURIComponent(query), SearchResult);
  for (const request of TestBed.inject(HttpTestingController).match((r) => r.url.includes('/api/products'))) {
    request.flush(products);
  }
  harness.detectChanges();
  await harness.fixture.whenStable();
  return harness.routeNativeElement as HTMLElement;
}

describe('kiosk-web probes', () => {
  beforeEach(setup);

  it('W1 product description markup cannot run script', async () => {
    const root = await search('lamp', [
      { id: 1, name: 'Desk Lamp', description: '<em>warm</em><img src="x" onerror="globalThis.kioskW1=1"><a href="javascript:alert(1)">more</a>', price: 39.95, imageUrl: null },
    ]);
    expect(root.querySelector('[onerror]')).toBeNull();
    expect(root.querySelector('a[href^="javascript:"]')).toBeNull();
  });

  it('W2 search term is not rendered as markup', async () => {
    const root = await search('<iframe src="javascript:alert(1)"></iframe><img src="x" onerror="globalThis.kioskW2=1">', []);
    expect(root.querySelector('iframe')).toBeNull();
    expect(root.querySelector('[onerror]')).toBeNull();
  });

  it('W3 admin route is not opened by a token that the browser made up', async () => {
    const forged = base64Url({ alg: 'none', typ: 'JWT' }) + '.' + base64Url({ sub: '3', email: 'tom.berg@kiosk.test', role: 'admin' }) + '.';
    localStorage.setItem('token', forged);
    sessionStorage.setItem('token', forged);

    const injector = TestBed.inject(EnvironmentInjector);
    const outcome = runInInjectionContext(injector, () => adminGuard(new ActivatedRouteSnapshot(), {} as RouterStateSnapshot));
    const pending = isObservable(outcome) ? firstValueFrom(outcome) : Promise.resolve(outcome);

    // The server knows the real role: a customer.
    await Promise.resolve();
    for (const request of TestBed.inject(HttpTestingController).match(() => true)) {
      if (request.request.url.includes('/users/me')) {
        request.flush({ id: 3, email: 'tom.berg@kiosk.test', role: 'customer', displayName: 'Tom' });
      } else {
        request.flush({ title: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });
      }
    }

    const result = await pending;
    expect(result).not.toBe(true);
  });

  it('W4 session token is not kept in web storage that scripts can read', () => {
    const auth = TestBed.inject(AuthService);
    auth.login('maria.lopez@kiosk.test', 'Maria-Spring-2026!').subscribe();
    const token = 'eyJhbGciOiJIUzI1NiJ9.' + base64Url({ sub: '2', role: 'customer' }) + '.probe-w4-signature';
    for (const request of TestBed.inject(HttpTestingController).match((r) => r.url.includes('/auth/login') || r.url.includes('/login'))) {
      request.flush({ token, email: 'maria.lopez@kiosk.test', role: 'customer' });
    }

    const stored = [
      ...Object.keys(localStorage).map((k) => localStorage.getItem(k) ?? ''),
      ...Object.keys(sessionStorage).map((k) => sessionStorage.getItem(k) ?? ''),
      document.cookie,
    ];
    expect(stored.some((value) => value.includes('probe-w4-signature'))).toBe(false);
  });

  it('D3 decoy campaign video still plays from its fixed address', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/about', PromoVideo);
    harness.detectChanges();
    const frame = (harness.routeNativeElement as HTMLElement).querySelector('iframe');
    expect(frame?.getAttribute('src')).toBe('https://www.youtube-nocookie.com/embed/kiosk-spring-2026');
  });
});
