import { InferType } from 'yup';

import bookingSchema from '~locales/booking/type';
import ar from '../../../locales/booking/ar.json';
import en from '../../../locales/booking/en.json';
import ku from '../../../locales/booking/ku.json';
import ru from '../../../locales/booking/ru.json';

import { CalendarNumerals, formatDigits } from '../shift-calendar/calendar-locale';

export type HourCycle = 'h23' | 'h12';
export type BookingStrings = InferType<typeof bookingSchema>;

const STRINGS: Record<string, BookingStrings> = { en, ar, ku, ru };

export const bookingStrings = (language: string): BookingStrings => STRINGS[language] ?? STRINGS.en;

const TIME = /^([01]\d|2[0-3]):([0-5]\d)$/;

export const isTime = (value: unknown): value is string => typeof value === 'string' && TIME.test(value);

export function parseTimes(input: string | string[] | null | undefined): string[] {
  let list: unknown[] = [];

  if (Array.isArray(input)) list = input;
  else if (typeof input === 'string' && input.trim()) {
    const text = input.trim();

    if (text.startsWith('[')) {
      try {
        const parsed = JSON.parse(text);
        if (Array.isArray(parsed)) list = parsed;
      } catch {
        list = [];
      }
    } else list = text.split(/[\s,]+/);
  }

  return [...new Set(list.map(entry => String(entry).trim()).filter(isTime))].sort();
}

export function formatTime(time: string, hourCycle: HourCycle, numerals: CalendarNumerals, strings: Pick<BookingStrings, 'am' | 'pm'>): string {
  const [hours, minutes] = time.split(':').map(Number);

  if (hourCycle !== 'h12') return formatDigits(time, numerals);

  const hour = hours % 12 || 12;

  return `${formatDigits(`${hour}:${String(minutes).padStart(2, '0')}`, numerals)} ${hours < 12 ? strings.am : strings.pm}`;
}

export const fill = (template: string, values: Record<string, string | number>) => template.replace(/\{(\w+)\}/g, (_, key) => String(values[key] ?? ''));

// Rows follow the auto-fill columns, so Up and Down move by however many columns fit; disabled times are passed over.
export function moveInTimes(
  index: number,
  key: string,
  { count, columns, rtl, skip = () => false }: { count: number; columns: number; rtl: boolean; skip?: (index: number) => boolean },
): number | null {
  if (!count) return null;

  const step = Math.max(1, columns);
  const forward = rtl ? 'ArrowLeft' : 'ArrowRight';
  const back = rtl ? 'ArrowRight' : 'ArrowLeft';
  const open = (from: number, by: number) => {
    for (let next = from; next >= 0 && next < count; next += by) if (!skip(next)) return next;
    return null;
  };
  const from = Math.min(count - 1, Math.max(0, index));

  let next: number | null;

  if (key === forward) next = open(from + 1, 1);
  else if (key === back) next = open(from - 1, -1);
  else if (key === 'ArrowDown') next = open(from + step, step);
  else if (key === 'ArrowUp') next = open(from - step, -step);
  else if (key === 'Home') next = open(0, 1);
  else if (key === 'End') next = open(count - 1, -1);
  else return null;

  return next ?? from;
}
