import { TranslocoTestingModule, TranslocoTestingOptions } from '@jsverse/transloco';

import en from '../../public/i18n/en.json';
import nl from '../../public/i18n/nl.json';

export function translocoTesting(options: TranslocoTestingOptions = {}) {
  return TranslocoTestingModule.forRoot({
    langs: { en, nl },
    translocoConfig: { availableLangs: ['en', 'nl'], defaultLang: 'en', reRenderOnLangChange: true },
    preloadLangs: true,
    ...options,
  });
}
