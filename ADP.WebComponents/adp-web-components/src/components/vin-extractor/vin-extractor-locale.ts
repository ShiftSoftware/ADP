import { InferType } from 'yup';

import vinExtractorSchema from '~locales/vinExtractor/type';
import ar from '../../locales/vinExtractor/ar.json';
import en from '../../locales/vinExtractor/en.json';
import ku from '../../locales/vinExtractor/ku.json';
import ru from '../../locales/vinExtractor/ru.json';

import type { LanguageKeys } from '~features/multi-lingual';

export type VinExtractorStrings = InferType<typeof vinExtractorSchema>;

export interface VinExtractorLocale {
  language: LanguageKeys;
  direction: 'ltr' | 'rtl';
  strings: VinExtractorStrings;
}

// Bundled, not fetched: the labels are needed on the first render, before any lookup could answer.
const LOCALES: Record<LanguageKeys, VinExtractorLocale> = {
  en: { language: 'en', direction: 'ltr', strings: en },
  ar: { language: 'ar', direction: 'rtl', strings: ar },
  ku: { language: 'ku', direction: 'rtl', strings: ku },
  ru: { language: 'ru', direction: 'ltr', strings: ru },
};

export const vinExtractorLocale = (language: string): VinExtractorLocale => LOCALES[language as LanguageKeys] ?? LOCALES.en;
