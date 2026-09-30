import { addDays, localDate, parseDate } from './calendar-date';

export interface BookingTarget {
  url: string;
  // The branch's hash ID.
  branchId: string;
  departmentId: string;
  brandId: string;
}

export interface BookingTime {
  time: string;
  raw: string;
}

export interface BookingDay {
  date: string;
  times: BookingTime[];
}

export interface BookingAvailability {
  days: BookingDay[];
  enabledDates: string[];
  slotCounts: Record<string, number>;
  first: string;
  last: string;
}

export type BookingError = { kind: 'network' } | { kind: 'http'; status: number } | { kind: 'payload' };

export type BookingOutcome = { status: 'ready'; availability: BookingAvailability } | { status: 'empty' } | { status: 'error'; error: BookingError } | { status: 'cancelled' };

export interface LoadOptions {
  today?: string;
  language?: string;
}

export type Fetcher = (url: string, init: RequestInit) => Promise<Response>;

// The endpoint counts its booking delay from `from`, so one window from today, never paged forward.
export const REQUEST_DAYS = 30;

const SLOT = /^(\d{4}-\d{2}-\d{2})\s+(\d{1,2}):(\d{2})\s*([AaPp][Mm])$/;

const pad = (value: number) => String(value).padStart(2, '0');

export function parseSlot(raw: unknown): { date: string; time: string } | null {
  if (typeof raw !== 'string') return null;

  const match = raw.trim().match(SLOT);
  if (!match || !parseDate(match[1])) return null;

  const hour = Number(match[2]);
  const minute = Number(match[3]);
  if (hour < 1 || hour > 12 || minute > 59) return null;

  const hours = (hour % 12) + (match[4].toUpperCase() === 'PM' ? 12 : 0);

  return { date: match[1], time: `${pad(hours)}:${pad(minute)}` };
}

export const slotValue = (raw: string): string => {
  const slot = parseSlot(raw);

  return slot ? `${slot.date}T${slot.time}` : '';
};

export function parseDays(payload: unknown): BookingDay[] | null {
  if (!Array.isArray(payload)) return null;

  const byDate = new Map<string, Map<string, string>>();

  for (const entry of payload) {
    const date = parseDate(entry?.Date?.slice?.(0, 10));
    if (!date || !Array.isArray(entry.Times)) continue;

    const times = byDate.get(date) ?? new Map<string, string>();

    for (const raw of entry.Times) {
      const slot = parseSlot(raw);
      if (slot && slot.date === date && !times.has(slot.time)) times.set(slot.time, String(raw).trim());
    }

    byDate.set(date, times);
  }

  return [...byDate]
    .filter(([, times]) => times.size)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([date, times]) => ({
      date,
      times: [...times].sort(([a], [b]) => a.localeCompare(b)).map(([time, raw]) => ({ time, raw })),
    }));
}

export function summarise(days: BookingDay[]): BookingAvailability | null {
  if (!days.length) return null;

  return {
    days,
    enabledDates: days.map(day => day.date),
    slotCounts: Object.fromEntries(days.map(day => [day.date, day.times.length])),
    first: days[0].date,
    last: days[days.length - 1].date,
  };
}

// Everything the endpoint needs is present.
export function isCompleteTarget(target: Partial<BookingTarget> | null | undefined): boolean {
  return !!(target?.url && target.branchId && target.departmentId && target.brandId);
}

export function queryUrl(target: Partial<BookingTarget> | null | undefined, today?: string): string | null {
  if (!isCompleteTarget(target)) return null;

  const from = parseDate(today) ?? localDate();
  const query = new URLSearchParams({
    from,
    to: addDays(from, REQUEST_DAYS),
    branchId: String(target.branchId),
    departmentId: String(target.departmentId),
    brandId: String(target.brandId),
  });

  return `${target.url}${target.url.includes('?') ? '&' : '?'}${query}`;
}

const cache = new Map<string, BookingOutcome>();

const cacheKey = (url: string, language: string) => `${language} ${url}`;

export function clearAvailabilityCache() {
  cache.clear();
}

export function createAvailabilityLoader(fetcher: Fetcher = (url, init) => fetch(url, init)) {
  let controller: AbortController | null = null;

  const cancel = () => {
    controller?.abort();
    controller = null;
  };

  async function load(target: Partial<BookingTarget> | null | undefined, options: LoadOptions = {}): Promise<BookingOutcome> {
    cancel();

    const url = queryUrl(target, options.today);
    if (!url) return { status: 'cancelled' };

    const language = options.language || 'en';
    const key = cacheKey(url, language);
    const cached = cache.get(key);
    if (cached) return cached;

    const own = new AbortController();
    controller = own;

    let outcome: BookingOutcome;

    try {
      const response = await fetcher(url, { signal: own.signal, headers: { 'Accept-Language': language } });

      if (!response.ok) outcome = { status: 'error', error: { kind: 'http', status: response.status } };
      else {
        const days = parseDays(await response.json().catch(() => undefined));
        const availability = days && summarise(days);

        outcome = !days ? { status: 'error', error: { kind: 'payload' } } : availability ? { status: 'ready', availability } : { status: 'empty' };
      }
    } catch {
      outcome = own.signal.aborted ? { status: 'cancelled' } : { status: 'error', error: { kind: 'network' } };
    }

    if (own.signal.aborted) return { status: 'cancelled' };
    if (controller === own) controller = null;
    if (outcome.status === 'ready' || outcome.status === 'empty') cache.set(key, outcome);

    return outcome;
  }

  return { load, cancel };
}

const OFFSET = /^[+-](0\d|1[0-4]):[0-5]\d$/;

export function localOffset(local: string): string {
  const [date, time] = local.split('T');
  const [year, month, day] = date.split('-').map(Number);
  const [hours, minutes] = time.split(':').map(Number);
  const east = -new Date(year, month - 1, day, hours, minutes).getTimezoneOffset();
  const size = Math.abs(east);

  return `${east < 0 ? '-' : '+'}${pad(Math.floor(size / 60))}:${pad(size % 60)}`;
}

// A slot is branch wall-clock time; receivers read a DateTimeOffset, so it leaves with an explicit offset.
export function slotIso(local: string, utcOffset?: string): string {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(local || '')) return '';

  return `${local}:00${utcOffset && OFFSET.test(utcOffset) ? utcOffset : localOffset(local)}`;
}
