import { buildAvailability } from '~lib/calendar-availability';

import { DEFAULT_DAYS, MAX_DAYS, moveInStrip, stripRange } from './day-strip-range';

const range = (input: Parameters<typeof buildAvailability>[0], today = '2026-09-27') => stripRange(buildAvailability(input), today);
const ends = (input: Parameters<typeof buildAvailability>[0], today?: string) => {
  const { days } = range(input, today);
  return [days[0], days[days.length - 1], days.length];
};

describe('stripRange', () => {
  it('min to max, inclusive', () => {
    expect(ends({ min: '2026-09-28', max: '2026-10-02' })).toEqual(['2026-09-28', '2026-10-02', 5]);
  });

  it('allow-list without a range: first to last enabled date, in any order', () => {
    expect(ends({ enabledDates: ['2026-10-15', '2026-09-29', '2026-10-01'] })).toEqual(['2026-09-29', '2026-10-15', 17]);
  });

  it('allow-list fills only the missing end', () => {
    expect(ends({ min: '2026-09-20', enabledDates: '2026-09-29, 2026-10-03' })).toEqual(['2026-09-20', '2026-10-03', 14]);
    expect(ends({ max: '2026-10-10', enabledDates: '2026-09-29, 2026-10-03' })).toEqual(['2026-09-29', '2026-10-10', 12]);
  });

  it(`nothing set: ${DEFAULT_DAYS} days from today`, () => {
    expect(ends({})).toEqual(['2026-09-27', '2026-10-26', DEFAULT_DAYS]);
    expect(ends({ min: '2026-12-30' })).toEqual(['2026-12-30', '2027-01-28', DEFAULT_DAYS]);
    expect(ends({ max: '2026-09-01' })).toEqual(['2026-08-03', '2026-09-01', DEFAULT_DAYS]);
    expect(ends({ enabledDates: [] })).toEqual(['2026-09-27', '2026-10-26', DEFAULT_DAYS]);
  });

  it('min after max is empty', () => {
    expect(range({ min: '2026-10-02', max: '2026-09-28' }).days).toEqual([]);
  });

  it(`caps at ${MAX_DAYS} days from the start`, () => {
    const { days, capped } = range({ min: '2026-01-01', max: '2030-12-31' });

    expect(days).toHaveLength(MAX_DAYS);
    expect(days[0]).toBe('2026-01-01');
    expect(days[MAX_DAYS - 1]).toBe('2027-01-01');
    expect(capped).toBe(true);
    expect(range({ min: '2026-01-01', max: '2027-01-01' }).capped).toBe(false);
  });
});

describe('moveInStrip', () => {
  const ltr = { rtl: false, count: 10, page: 4 };
  const rtl = { ...ltr, rtl: true };

  it('arrows follow the reading direction and stop at the ends', () => {
    expect(moveInStrip(3, 'ArrowRight', ltr)).toBe(4);
    expect(moveInStrip(3, 'ArrowLeft', ltr)).toBe(2);
    expect(moveInStrip(3, 'ArrowLeft', rtl)).toBe(4);
    expect(moveInStrip(3, 'ArrowRight', rtl)).toBe(2);
    expect(moveInStrip(9, 'ArrowRight', ltr)).toBe(9);
    expect(moveInStrip(0, 'ArrowLeft', ltr)).toBe(0);
  });

  it('Home, End and a page at a time', () => {
    expect(moveInStrip(5, 'Home', ltr)).toBe(0);
    expect(moveInStrip(5, 'End', ltr)).toBe(9);
    expect(moveInStrip(5, 'PageDown', ltr)).toBe(9);
    expect(moveInStrip(2, 'PageDown', ltr)).toBe(6);
    expect(moveInStrip(2, 'PageUp', ltr)).toBe(0);
    expect(moveInStrip(2, 'ArrowUp', ltr)).toBeNull();
    expect(moveInStrip(0, 'ArrowRight', { ...ltr, count: 0 })).toBeNull();
  });
});
