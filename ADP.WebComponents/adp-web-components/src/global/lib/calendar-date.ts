// Integer day numbers only, never a Date, so no time zone can move a day.

const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})$/;
const ISO_MONTH = /^(\d{4})-(\d{2})$/;

export type Weekday = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export interface GridCell {
  date: string;
  inMonth: boolean;
}

const pad = (value: number, width = 2) => String(value).padStart(width, '0');

export const isLeapYear = (year: number): boolean => (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;

export function daysInMonth(year: number, month: number): number {
  if (month === 2) return isLeapYear(year) ? 29 : 28;
  return [4, 6, 9, 11].includes(month) ? 30 : 31;
}

export function formatDate(year: number, month: number, day: number): string {
  return `${pad(year, 4)}-${pad(month)}-${pad(day)}`;
}

export function parseDate(value: unknown): string | null {
  if (typeof value !== 'string') return null;

  const match = ISO_DATE.exec(value.trim());
  if (!match) return null;

  const [year, month, day] = match.slice(1).map(Number);
  if (year < 1 || month < 1 || month > 12 || day < 1 || day > daysInMonth(year, month)) return null;

  return formatDate(year, month, day);
}

export function parseMonth(value: unknown): string | null {
  if (typeof value !== 'string') return null;

  const match = ISO_MONTH.exec(value.trim());
  if (!match) return null;

  const [year, month] = match.slice(1).map(Number);
  if (year < 1 || month < 1 || month > 12) return null;

  return `${pad(year, 4)}-${pad(month)}`;
}

export function dateParts(date: string): [number, number, number] {
  return date.split('-').map(Number) as [number, number, number];
}

export function monthParts(month: string): [number, number] {
  return month.split('-').map(Number) as [number, number];
}

// Howard Hinnant's days_from_civil.
export function toDayNumber(date: string): number {
  const [y, m, d] = dateParts(date);
  const year = m <= 2 ? y - 1 : y;
  const era = Math.floor(year / 400);
  const yearOfEra = year - era * 400;
  const dayOfYear = Math.floor((153 * (m + (m > 2 ? -3 : 9)) + 2) / 5) + d - 1;
  const dayOfEra = yearOfEra * 365 + Math.floor(yearOfEra / 4) - Math.floor(yearOfEra / 100) + dayOfYear;

  return era * 146097 + dayOfEra - 719468;
}

export function fromDayNumber(dayNumber: number): string {
  const z = dayNumber + 719468;
  const era = Math.floor(z / 146097);
  const dayOfEra = z - era * 146097;
  const yearOfEra = Math.floor((dayOfEra - Math.floor(dayOfEra / 1460) + Math.floor(dayOfEra / 36524) - Math.floor(dayOfEra / 146096)) / 365);
  const dayOfYear = dayOfEra - (365 * yearOfEra + Math.floor(yearOfEra / 4) - Math.floor(yearOfEra / 100));
  const mp = Math.floor((5 * dayOfYear + 2) / 153);
  const day = dayOfYear - Math.floor((153 * mp + 2) / 5) + 1;
  const month = mp < 10 ? mp + 3 : mp - 9;

  return formatDate(yearOfEra + era * 400 + (month <= 2 ? 1 : 0), month, day);
}

// 1970-01-01 was a Thursday.
export const weekdayOf = (date: string): Weekday => ((((toDayNumber(date) + 4) % 7) + 7) % 7) as Weekday;

export const addDays = (date: string, days: number): string => fromDayNumber(toDayNumber(date) + days);

export const compareDates = (a: string, b: string): number => (a < b ? -1 : a > b ? 1 : 0);

export const monthOf = (date: string): string => date.slice(0, 7);

export function addMonthsToMonth(month: string, count: number): string {
  const [year, m] = monthParts(month);
  const index = year * 12 + (m - 1) + count;

  return `${pad(Math.floor(index / 12), 4)}-${pad((index % 12) + 1)}`;
}

export function addMonths(date: string, count: number): string {
  const [, , day] = dateParts(date);
  const [year, month] = monthParts(addMonthsToMonth(monthOf(date), count));

  return formatDate(year, month, Math.min(day, daysInMonth(year, month)));
}

export const addYears = (date: string, count: number): string => addMonths(date, count * 12);

export const firstOfMonth = (month: string): string => `${month}-01`;

export function lastOfMonth(month: string): string {
  const [year, m] = monthParts(month);

  return formatDate(year, m, daysInMonth(year, m));
}

export const normaliseWeekday = (value: unknown, fallback: Weekday): Weekday => {
  const number = typeof value === 'string' && value.trim() !== '' ? Number(value) : value;

  return typeof number === 'number' && Number.isInteger(number) ? ((((number % 7) + 7) % 7) as Weekday) : fallback;
};

export function startOfWeek(date: string, weekStartsOn: Weekday): string {
  return addDays(date, -((weekdayOf(date) - weekStartsOn + 7) % 7));
}

export const endOfWeek = (date: string, weekStartsOn: Weekday): string => addDays(startOfWeek(date, weekStartsOn), 6);

// Always six rows, so the grid keeps its height from month to month.
export function monthGrid(month: string, weekStartsOn: Weekday): GridCell[] {
  const first = startOfWeek(firstOfMonth(month), weekStartsOn);
  const start = toDayNumber(first);

  return Array.from({ length: 42 }, (_, index) => {
    const date = fromDayNumber(start + index);

    return { date, inMonth: monthOf(date) === month };
  });
}

export function clampDate(date: string, min?: string | null, max?: string | null): string {
  if (min && date < min) return min;
  if (max && date > max) return max;
  return date;
}

export function clampMonth(month: string, min?: string | null, max?: string | null): string {
  if (min && month < monthOf(min)) return monthOf(min);
  if (max && month > monthOf(max)) return monthOf(max);
  return month;
}

// The reader's own calendar date: the local fields, never the UTC ones.
export const localDate = (now: Date = new Date()): string => formatDate(now.getFullYear(), now.getMonth() + 1, now.getDate());
