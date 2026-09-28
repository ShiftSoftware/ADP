import { Availability } from '~lib/calendar-availability';
import { addDays, toDayNumber } from '~lib/calendar-date';

export const MAX_DAYS = 366;
export const DEFAULT_DAYS = 30;

export interface StripRange {
  days: string[];
  capped: boolean;
}

export function stripRange(rules: Pick<Availability, 'min' | 'max' | 'enabledDates'>, today: string): StripRange {
  const { min, max } = rules;
  const enabled = [...(rules.enabledDates ?? [])].filter(date => (!min || date >= min) && (!max || date <= max)).sort();

  const first = min ?? enabled[0] ?? (max && max < today ? addDays(max, 1 - DEFAULT_DAYS) : today);
  const last = max ?? (enabled.length && enabled[enabled.length - 1] >= first ? enabled[enabled.length - 1] : addDays(first, DEFAULT_DAYS - 1));

  if (last < first) return { days: [], capped: false };

  const count = toDayNumber(last) - toDayNumber(first) + 1;
  const shown = Math.min(count, MAX_DAYS);

  return { days: Array.from({ length: shown }, (_, index) => addDays(first, index)), capped: count > MAX_DAYS };
}

export function moveInStrip(index: number, key: string, context: { rtl: boolean; count: number; page: number }): number | null {
  const forward = context.rtl ? 'ArrowLeft' : 'ArrowRight';
  const backward = context.rtl ? 'ArrowRight' : 'ArrowLeft';
  const last = context.count - 1;

  if (last < 0) return null;

  const clamp = (value: number) => Math.min(last, Math.max(0, value));

  switch (key) {
    case forward:
      return clamp(index + 1);
    case backward:
      return clamp(index - 1);
    case 'Home':
      return 0;
    case 'End':
      return last;
    case 'PageDown':
      return clamp(index + context.page);
    case 'PageUp':
      return clamp(index - context.page);
    default:
      return null;
  }
}
