import { formatDate, lastOfMonth, monthOf } from '~lib/calendar-date';

export type CalendarView = 'days' | 'months' | 'years';

export const PAGE_SIZE = 12;
export const COLUMNS = 3;

export const yearsPageStart = (year: number): number => year - 6;

export const monthIndex = (year: number, month: number): number => year * 12 + month - 1;

export const monthFromIndex = (index: number): [number, number] => [Math.floor(index / 12), (index % 12) + 1];

const yearOf = (date: string) => Number(date.slice(0, 4));

const isEmpty = (min: string | null, max: string | null) => !!min && !!max && min > max;

export function monthIsOutside(year: number, month: number, min: string | null, max: string | null): boolean {
  const key = `${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}`;

  if (isEmpty(min, max)) return true;
  return (!!max && formatDate(year, month, 1) > max) || (!!min && lastOfMonth(key) < min);
}

export function yearIsOutside(year: number, min: string | null, max: string | null): boolean {
  if (isEmpty(min, max)) return true;
  return (!!max && year > yearOf(max)) || (!!min && year < yearOf(min));
}

export function headerButtons(min: string | null, max: string | null): { month: boolean; year: boolean } {
  if (isEmpty(min, max)) return { month: false, year: false };

  const sameYear = !!min && !!max && yearOf(min) === yearOf(max);
  const sameMonth = !!min && !!max && monthOf(min) === monthOf(max);

  return { month: !sameMonth, year: !sameYear };
}

export function indexBounds(view: 'months' | 'years', min: string | null, max: string | null): [number, number] {
  const low = min ? (view === 'months' ? monthIndex(yearOf(min), Number(min.slice(5, 7))) : yearOf(min)) : -Infinity;
  const high = max ? (view === 'months' ? monthIndex(yearOf(max), Number(max.slice(5, 7))) : yearOf(max)) : Infinity;

  return [low, high];
}

export function canPage(view: 'months' | 'years', pageStart: number, step: 1 | -1, min: string | null, max: string | null): boolean {
  if (isEmpty(min, max)) return false;

  const [low, high] = indexBounds(view, min, max);
  const first = view === 'months' ? monthIndex(pageStart, 1) : pageStart;
  const last = first + PAGE_SIZE - 1;

  return step < 0 ? first > low : last < high;
}

export function pageContaining(view: 'months' | 'years', index: number, pageStart: number): number {
  if (view === 'months') return Math.floor(index / 12);

  let start = pageStart;
  while (index < start) start -= PAGE_SIZE;
  while (index > start + PAGE_SIZE - 1) start += PAGE_SIZE;

  return start;
}

export interface GridMoveContext {
  rtl: boolean;
  pageStart: number;
  low: number;
  high: number;
}

// Months and years share one grid of three columns; index is absolute (a month index or a year).
export function moveInGrid(index: number, key: string, context: GridMoveContext): number | null {
  const forward = context.rtl ? 'ArrowLeft' : 'ArrowRight';
  const backward = context.rtl ? 'ArrowRight' : 'ArrowLeft';
  const column = (((index - context.pageStart) % COLUMNS) + COLUMNS) % COLUMNS;

  let target: number;

  switch (key) {
    case forward:
      target = index + 1;
      break;
    case backward:
      target = index - 1;
      break;
    case 'ArrowDown':
      target = index + COLUMNS;
      break;
    case 'ArrowUp':
      target = index - COLUMNS;
      break;
    case 'Home':
      target = index - column;
      break;
    case 'End':
      target = index + (COLUMNS - 1 - column);
      break;
    case 'PageUp':
      target = index - PAGE_SIZE;
      break;
    case 'PageDown':
      target = index + PAGE_SIZE;
      break;
    default:
      return null;
  }

  return Math.min(context.high, Math.max(context.low, target));
}
