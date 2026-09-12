import { now, today } from './clock';

describe('clock', () => {
  beforeEach(() => {
    jest.useFakeTimers().setSystemTime(new Date('2031-04-05T21:30:00.000Z'));
  });

  afterEach(() => jest.useRealTimers());

  it('uses the wall clock when no date is supplied', () => {
    expect(now().toISOString()).toBe('2031-04-05T21:30:00.000Z');
    expect(today()).toBe('2031-04-05');
  });

  it('reads a supplied calendar date as midnight UTC', () => {
    expect(now('2026-09-01').toISOString()).toBe('2026-09-01T00:00:00.000Z');
    expect(today('2026-09-01')).toBe('2026-09-01');
  });

  it('falls back to the wall clock for malformed and impossible dates', () => {
    expect(today('not-a-date')).toBe('2031-04-05');
    expect(today('2026-02-30')).toBe('2031-04-05');
  });
});
