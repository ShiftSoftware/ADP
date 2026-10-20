import type { LanguageCode, LanguageKeys } from '~features/multi-lingual/types';
import { packLanguageFromUrl } from '~features/multi-lingual/language-packs';

const LANGUAGE_MAP: Record<string, LanguageKeys> = {
  en: 'en',
  english: 'en',

  ar: 'ar',
  arabic: 'ar',

  ku: 'ku',
  ckb: 'ku',
  kurdish: 'ku',
  sorani: 'ku',

  ru: 'ru',
  russian: 'ru',
};

export default function getLanguageFromUrl(): LanguageCode {
  if (typeof window === 'undefined') return 'en';

  const { pathname, search } = window.location;

  const params = new URLSearchParams(search);
  const queryLang = params.get('lang') || params.get('lng') || params.get('language');

  if (queryLang) {
    const normalized = queryLang.toLowerCase();
    if (LANGUAGE_MAP[normalized]) {
      return LANGUAGE_MAP[normalized];
    }
  }

  const path = pathname.toLowerCase();

  for (const key in LANGUAGE_MAP) {
    if (path.includes(`/${key}`)) {
      return LANGUAGE_MAP[key];
    }
  }

  return packLanguageFromUrl(queryLang, path) ?? 'en';
}
