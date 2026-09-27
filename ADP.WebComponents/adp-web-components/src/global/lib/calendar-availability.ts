import { parseDate, weekdayOf } from './calendar-date';
import { parseDateList, parseWeekdayList } from './slot-day-rules';

// Precedence order; rules only block, so nothing re-enables a day another rule blocked.
export type BlockReason = 'range' | 'disabled-date' | 'disabled-weekday' | 'not-enabled' | 'rule';

export const BLOCK_ORDER: readonly BlockReason[] = ['range', 'disabled-date', 'disabled-weekday', 'not-enabled', 'rule'];

export interface AvailabilityInput {
  min?: string | null;
  max?: string | null;
  disabledDates?: string | string[] | null;
  disabledWeekdays?: string | number[] | null;
  enabledDates?: string | string[] | null;
  isDateDisabled?: ((date: string) => boolean) | null;
}

export interface Availability {
  min: string | null;
  max: string | null;
  disabledDates: Set<string>;
  disabledWeekdays: Set<number>;
  enabledDates: Set<string> | null;
  isDateDisabled: ((date: string) => boolean) | null;
}

export function buildAvailability(input: AvailabilityInput): Availability {
  const enabled = input.enabledDates;
  const hasAllowList = enabled !== undefined && enabled !== null && !(typeof enabled === 'string' && enabled.trim() === '');

  return {
    min: parseDate(input.min),
    max: parseDate(input.max),
    disabledDates: new Set(parseDateList(input.disabledDates).map(parseDate).filter(Boolean)),
    disabledWeekdays: new Set(parseWeekdayList(input.disabledWeekdays)),
    enabledDates: hasAllowList ? new Set(parseDateList(enabled).map(parseDate).filter(Boolean)) : null,
    isDateDisabled: typeof input.isDateDisabled === 'function' ? input.isDateDisabled : null,
  };
}

export const isInRange = (date: string, rules: Pick<Availability, 'min' | 'max'>): boolean => (!rules.min || date >= rules.min) && (!rules.max || date <= rules.max);

export function blockReason(date: string, rules: Availability): BlockReason | null {
  if (!isInRange(date, rules)) return 'range';
  if (rules.disabledDates.has(date)) return 'disabled-date';
  if (rules.disabledWeekdays.has(weekdayOf(date))) return 'disabled-weekday';
  if (rules.enabledDates && !rules.enabledDates.has(date)) return 'not-enabled';

  if (rules.isDateDisabled) {
    try {
      if (rules.isDateDisabled(date)) return 'rule';
    } catch {
      // A rule that cannot answer cannot vouch for the day either.
      return 'rule';
    }
  }

  return null;
}

export const blockKind = (reason: BlockReason | null): 'closed' | 'unavailable' | null =>
  reason === null ? null : reason === 'range' || reason === 'not-enabled' ? 'unavailable' : 'closed';
