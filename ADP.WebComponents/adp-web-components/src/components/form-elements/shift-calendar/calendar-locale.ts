import { InferType } from 'yup';

import calendarSchema from '~locales/calendar/type';
import ar from '../../../locales/calendar/ar.json';
import en from '../../../locales/calendar/en.json';
import ku from '../../../locales/calendar/ku.json';
import ru from '../../../locales/calendar/ru.json';

import { dateParts, monthParts, Weekday, weekdayOf } from '~lib/calendar-date';

export type CalendarLanguage = 'en' | 'ar' | 'ku' | 'ru';
export type CalendarNumerals = 'latn' | 'arab';
export type CalendarStrings = InferType<typeof calendarSchema>;

export interface CalendarLocale {
  language: CalendarLanguage;
  direction: 'ltr' | 'rtl';
  weekStartsOn: Weekday;
  numerals: CalendarNumerals;
  strings: CalendarStrings;
}

// Bundled, not fetched: the names are the grid, and a first paint without them is a broken calendar.
const LOCALES: Record<CalendarLanguage, CalendarLocale> = {
  en: { language: 'en', direction: 'ltr', weekStartsOn: 1, numerals: 'latn', strings: en },
  ar: { language: 'ar', direction: 'rtl', weekStartsOn: 6, numerals: 'arab', strings: ar },
  ku: { language: 'ku', direction: 'rtl', weekStartsOn: 6, numerals: 'arab', strings: ku },
  ru: { language: 'ru', direction: 'ltr', weekStartsOn: 1, numerals: 'latn', strings: ru },
};

export const calendarLocale = (language: string): CalendarLocale => LOCALES[language as CalendarLanguage] ?? LOCALES.en;

const ARABIC_INDIC_ZERO = 0x0660;

export function formatDigits(value: number | string, numerals: CalendarNumerals): string {
  const text = String(value);

  return numerals === 'arab' ? text.replace(/[0-9]/g, digit => String.fromCharCode(ARABIC_INDIC_ZERO + Number(digit))) : text;
}

export function monthTitle(month: string, locale: CalendarLocale, numerals: CalendarNumerals): string {
  const [year, m] = monthParts(month);

  return `${locale.strings.months[m - 1]} ${formatDigits(year, numerals)}`;
}

export function dateLabel(date: string, locale: CalendarLocale, numerals: CalendarNumerals): string {
  const [year, month, day] = dateParts(date);
  const parts: Record<string, string> = {
    weekday: locale.strings.weekdays[weekdayOf(date)],
    day: formatDigits(day, numerals),
    month: locale.strings.monthsInDate[month - 1],
    year: formatDigits(year, numerals),
  };

  return locale.strings.dateLabel.replace(/\{(\w+)\}/g, (_, key) => parts[key] ?? '');
}
