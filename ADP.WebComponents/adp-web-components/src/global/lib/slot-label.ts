import { format } from 'date-fns';
import type { Locale } from 'date-fns';
import { enUS } from 'date-fns/locale';

export interface SlotLabelNames {
  months?: string[];
  monthsShort?: string[];
  weekdays?: string[];
  weekdaysShort?: string[];
  weekdaysNarrow?: string[];
  am?: string;
  pm?: string;
}

// date-fns does the pattern; the names are the component's own, so the label reads like the calendar beside it.
function namedLocale(names: SlotLabelNames): Locale {
  return {
    ...enUS,
    localize: {
      ...enUS.localize,
      month: (month: number, options?: { width?: string }) => (options?.width === 'abbreviated' ? names.monthsShort : names.months)?.[month] ?? '',
      day: (day: number, options?: { width?: string }) =>
        (options?.width === 'narrow' ? names.weekdaysNarrow : options?.width === 'abbreviated' || options?.width === 'short' ? names.weekdaysShort : names.weekdays)?.[day] ?? '',
      dayPeriod: (period: string, options?: object) => (period === 'am' ? (names.am ?? '') : period === 'pm' ? (names.pm ?? '') : enUS.localize.dayPeriod(period as 'am', options)),
    },
  };
}

export function formatSlotLabel(local: string, pattern: string, fallback: string, names: SlotLabelNames, toDigits: (text: string) => string): string {
  const [date, time = '00:00'] = local.split('T');
  const [year, month, day] = date.split('-').map(Number);
  const [hours, minutes] = time.split(':').map(Number);
  const when = new Date(year, month - 1, day, hours, minutes);
  const locale = namedLocale(names);

  for (const candidate of [pattern, fallback]) {
    try {
      return toDigits(format(when, candidate, { locale }));
    } catch {
      continue;
    }
  }

  return local;
}
