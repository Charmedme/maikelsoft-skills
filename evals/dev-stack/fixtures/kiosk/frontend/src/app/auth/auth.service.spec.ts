import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthService } from './auth.service';

describe('AuthService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  });

  it('posts the credentials and reports the signed-in state', () => {
    const auth = TestBed.inject(AuthService);
    let role = '';
    auth.login('maria.lopez@kiosk.test', 'secret').subscribe((r) => (role = r.role));

    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/login');
    expect(request.request.body).toEqual({ email: 'maria.lopez@kiosk.test', password: 'secret' });
    request.flush({ token: 'a.eyJyb2xlIjoiY3VzdG9tZXIifQ.c', email: 'maria.lopez@kiosk.test', role: 'customer' });

    expect(role).toBe('customer');
    expect(auth.isLoggedIn()).toBe(true);
  });

  it('forgets the session on sign-out', () => {
    const auth = TestBed.inject(AuthService);
    auth.login('maria.lopez@kiosk.test', 'secret').subscribe();
    TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush({ token: 'a.b.c', email: 'x', role: 'customer' });

    auth.logout();

    expect(auth.isLoggedIn()).toBe(false);
  });
});
