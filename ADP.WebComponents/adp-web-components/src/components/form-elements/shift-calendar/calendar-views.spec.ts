import { canPage, headerButtons, indexBounds, monthIndex, monthIsOutside, moveInGrid, pageContaining, yearIsOutside, yearsPageStart } from './calendar-views';

describe('calendar views', () => {
  it('years page around the year', () => {
    expect(yearsPageStart(2026)).toBe(2020);
    expect(pageContaining('years', 2019, 2020)).toBe(2008);
    expect(pageContaining('years', 2032, 2020)).toBe(2032);
    expect(pageContaining('months', monthIndex(2027, 1), 0)).toBe(2027);
  });

  it('months and years outside the range', () => {
    expect(monthIsOutside(2026, 10, '2025-03-15', '2026-10-05')).toBe(false);
    expect(monthIsOutside(2026, 11, '2025-03-15', '2026-10-05')).toBe(true);
    expect(monthIsOutside(2025, 2, '2025-03-15', '2026-10-05')).toBe(true);
    expect(monthIsOutside(2025, 3, '2025-03-15', null)).toBe(false);
    expect(yearIsOutside(2027, null, '2026-10-05')).toBe(true);
    expect(yearIsOutside(2024, '2025-03-15', null)).toBe(true);
    expect(yearIsOutside(2026, '2026-12-01', '2026-01-01')).toBe(true);
  });

  it('header buttons', () => {
    expect(headerButtons(null, null)).toEqual({ month: true, year: true });
    expect(headerButtons('2026-02-01', '2026-11-30')).toEqual({ month: true, year: false });
    expect(headerButtons('2026-09-02', '2026-09-20')).toEqual({ month: false, year: false });
    expect(headerButtons('2026-12-01', '2026-01-01')).toEqual({ month: false, year: false });
  });

  it('paging stops at the range', () => {
    expect(canPage('months', 2026, 1, null, '2026-10-05')).toBe(false);
    expect(canPage('months', 2026, -1, '2025-03-15', null)).toBe(true);
    expect(canPage('months', 2025, -1, '2025-03-15', null)).toBe(false);
    expect(canPage('years', 2020, 1, null, '2031-01-01')).toBe(false);
    expect(canPage('years', 2020, 1, null, '2032-01-01')).toBe(true);
    expect(canPage('years', 2020, -1, '2020-06-01', null)).toBe(false);
  });

  it('grid moves in LTR and RTL', () => {
    const context = { rtl: false, pageStart: 2020, low: -Infinity, high: Infinity };

    expect(moveInGrid(2026, 'ArrowRight', context)).toBe(2027);
    expect(moveInGrid(2026, 'ArrowRight', { ...context, rtl: true })).toBe(2025);
    expect(moveInGrid(2026, 'ArrowLeft', { ...context, rtl: true })).toBe(2027);
    expect(moveInGrid(2026, 'ArrowDown', context)).toBe(2029);
    expect(moveInGrid(2026, 'ArrowUp', context)).toBe(2023);
    expect(moveInGrid(2026, 'Home', context)).toBe(2026);
    expect(moveInGrid(2027, 'Home', context)).toBe(2026);
    expect(moveInGrid(2027, 'End', context)).toBe(2028);
    expect(moveInGrid(2026, 'PageUp', context)).toBe(2014);
    expect(moveInGrid(2026, 'PageDown', context)).toBe(2038);
    expect(moveInGrid(2026, 'Tab', context)).toBeNull();
  });

  it('grid clamps to the range', () => {
    const [low, high] = indexBounds('months', '2025-03-15', '2026-10-05');
    const context = { rtl: false, pageStart: monthIndex(2026, 1), low, high };

    expect(moveInGrid(monthIndex(2026, 10), 'ArrowRight', context)).toBe(monthIndex(2026, 10));
    expect(moveInGrid(monthIndex(2026, 1), 'PageUp', context)).toBe(monthIndex(2025, 3));
  });
});
