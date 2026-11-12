import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { translocoTesting } from '../testing/transloco-testing';
import { App } from './app';
import { LANGUAGE_STORAGE_KEY } from './i18n/language';

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App, translocoTesting()],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('shows the title and the navigation in English', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.autoDetectChanges();
    TestBed.inject(HttpTestingController).expectOne('/api/me').flush({ name: 'Ann', roles: ['catalog'] });
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.app-title')?.textContent).toContain('Products admin');
    expect(element.querySelector('nav a')?.textContent).toContain('Products');
  });

  it('switches to Dutch and remembers the choice', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.autoDetectChanges();
    TestBed.inject(HttpTestingController).expectOne('/api/me').flush({ name: 'Ann', roles: ['catalog'] });
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    const dutch = Array.from(element.querySelectorAll<HTMLButtonElement>('.language-switch button')).find(
      (button) => button.textContent?.trim() === 'Nederlands',
    );
    dutch?.click();
    await fixture.whenStable();

    expect(element.querySelector('.app-title')?.textContent).toContain('Productbeheer');
    expect(dutch?.getAttribute('aria-pressed')).toBe('true');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('nl');
  });
});
