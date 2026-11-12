import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';

import { AuthService } from './auth/auth.service';
import { AVAILABLE_LANGUAGES, Language, storeLanguage } from './i18n/language';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoDirective],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly transloco = inject(TranslocoService);
  protected readonly auth = inject(AuthService);

  protected readonly languages = AVAILABLE_LANGUAGES;
  protected readonly activeLanguage = this.transloco.activeLang;

  constructor() {
    document.documentElement.lang = this.transloco.getActiveLang();
  }

  protected selectLanguage(lang: Language): void {
    this.transloco.setActiveLang(lang);
    storeLanguage(lang);
    document.documentElement.lang = lang;
  }
}
