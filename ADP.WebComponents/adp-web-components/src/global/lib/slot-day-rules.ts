/**
 * Day-level availability rules shared by the two booking pickers.
 *
 * The calendar endpoint already omits days a branch is closed, so a day it
 * never returns is simply absent. These rules cover what the endpoint cannot
 * express: a weekday a deployment never takes bookings on, and one-off blackout
 * dates — a holiday, a stock-take, a branch-wide event.
 *
 * A day matched here is rendered *disabled* rather than dropped, so a customer
 * can see the day exists and is merely not bookable, instead of wondering why
 * their calendar skips a date.
 */

/** Accepts `[5, 6]`, `"5,6"`, `"5 6"` or `"[5,6]"` — 0 is Sunday, 6 Saturday. */
export function parseWeekdayList(input?: string | number[] | null): number[] {
  return parseList(input)
    .map(entry => parseInt(entry, 10))
    .filter(day => Number.isInteger(day) && day >= 0 && day <= 6);
}

/** Accepts an array or a comma/space separated string of `YYYY-MM-DD` dates. */
export function parseDateList(input?: string | string[] | null): string[] {
  return parseList(input).filter(entry => /^\d{4}-\d{2}-\d{2}$/.test(entry));
}

/**
 * `2026-08-13` → 0 (Sunday) … 6 (Saturday).
 *
 * Built at midday on purpose: a midnight Date in a negative-offset zone rolls
 * back a day, which would shift every weekday by one.
 */
export function weekdayOf(date: string): number {
  const [year, month, day] = date.split('-').map(part => parseInt(part, 10));
  return new Date(year, month - 1, day, 12).getDay();
}

/** True when the day exists but must not be selectable. */
export function isDayBlocked(date: string, weekdays: number[], dates: string[]): boolean {
  if (dates.includes(date)) return true;
  return weekdays.length ? weekdays.includes(weekdayOf(date)) : false;
}

function parseList(input?: string | string[] | number[] | null): string[] {
  if (input === null || input === undefined || input === '') return [];
  if (Array.isArray(input)) return input.map(entry => `${entry}`.trim()).filter(Boolean);

  const text = `${input}`.trim();

  // Props arriving as HTML attributes lose their type, and a host copying a
  // value out of a config file will hand over the JSON form verbatim.
  if (text.startsWith('[')) {
    try {
      const parsed = JSON.parse(text);
      if (Array.isArray(parsed)) return parsed.map(entry => `${entry}`.trim()).filter(Boolean);
    } catch {
      /* fall through to the separator split */
    }
  }

  return text
    .split(/[\s,]+/)
    .map(entry => entry.trim())
    .filter(Boolean);
}
