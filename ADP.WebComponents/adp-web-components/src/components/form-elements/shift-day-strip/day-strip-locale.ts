import { CalendarLanguage, CalendarNumerals, formatDigits } from '../shift-calendar/calendar-locale';

export interface DayStripStrings {
  days: string;
  earlierDays: string;
  laterDays: string;
  monthsShort: string[];
}

// Some Arabic locales abbreviate the two-word months to a letter and a number (ك٢ is كانون الثاني).
const STRINGS: Record<CalendarLanguage, DayStripStrings> = {
  en: {
    days: 'Choose a day',
    earlierDays: 'Earlier days',
    laterDays: 'Later days',
    monthsShort: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
  },
  ar: {
    days: 'اختر يوماً',
    earlierDays: 'الأيام السابقة',
    laterDays: 'الأيام التالية',
    monthsShort: ['ك2', 'شباط', 'آذار', 'نيسان', 'أيار', 'حزيران', 'تموز', 'آب', 'أيلول', 'ت1', 'ت2', 'ك1'],
  },
  ku: {
    days: 'ڕۆژێک هەڵبژێرە',
    earlierDays: 'ڕۆژانی پێشوو',
    laterDays: 'ڕۆژانی داهاتوو',
    monthsShort: ['ک2', 'شوبات', 'ئازار', 'نیسان', 'ئایار', 'حوزەیران', 'تەمووز', 'ئاب', 'ئەیلوول', 'ت1', 'ت2', 'ک1'],
  },
  ru: {
    days: 'Выберите день',
    earlierDays: 'Предыдущие дни',
    laterDays: 'Следующие дни',
    monthsShort: ['янв.', 'февр.', 'март', 'апр.', 'май', 'июнь', 'июль', 'авг.', 'сент.', 'окт.', 'нояб.', 'дек.'],
  },
};

export const dayStripStrings = (language: CalendarLanguage): DayStripStrings => STRINGS[language] ?? STRINGS.en;

export const shortMonth = (month: number, strings: DayStripStrings, numerals: CalendarNumerals): string => formatDigits(strings.monthsShort[month - 1], numerals);
