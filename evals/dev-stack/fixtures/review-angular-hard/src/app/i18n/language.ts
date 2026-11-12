export const AVAILABLE_LANGUAGES = ['en', 'nl'] as const;
export type Language = (typeof AVAILABLE_LANGUAGES)[number];

export const LANGUAGE_STORAGE_KEY = 'products-admin.lang';

export function isLanguage(value: string | null | undefined): value is Language {
  return !!value && (AVAILABLE_LANGUAGES as readonly string[]).includes(value);
}

/** The language from ?lang=, then the stored choice, then English. */
export function initialLanguage(): Language {
  const fromQuery = new URLSearchParams(globalThis.location?.search ?? '').get('lang');
  if (isLanguage(fromQuery)) {
    storeLanguage(fromQuery);
    return fromQuery;
  }
  const stored = readStoredLanguage();
  return isLanguage(stored) ? stored : 'en';
}

export function storeLanguage(lang: Language): void {
  try {
    globalThis.localStorage?.setItem(LANGUAGE_STORAGE_KEY, lang);
  } catch {
    // Storage can be blocked (private mode). The choice then lasts for this page only.
  }
}

function readStoredLanguage(): string | null {
  try {
    return globalThis.localStorage?.getItem(LANGUAGE_STORAGE_KEY) ?? null;
  } catch {
    return null;
  }
}
