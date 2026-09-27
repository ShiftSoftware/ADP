import { blockKind, blockReason, buildAvailability, BLOCK_ORDER } from './calendar-availability';

const reasonOf = (date: string, input: Parameters<typeof buildAvailability>[0]) => blockReason(date, buildAvailability(input));

describe('calendar availability', () => {
  it('no rules', () => {
    expect(reasonOf('2026-09-27', {})).toBeNull();
  });

  it('slot-day-rules input shapes', () => {
    for (const disabledWeekdays of [[5], '5', '5,6', '5 6', '[5,6]']) {
      expect(reasonOf('2026-10-02', { disabledWeekdays })).toBe('disabled-weekday');
    }

    for (const disabledDates of [['2026-10-06'], '2026-10-06', '2026-10-05, 2026-10-06', '["2026-10-06"]']) {
      expect(reasonOf('2026-10-06', { disabledDates })).toBe('disabled-date');
    }
  });

  it('min and max inclusive', () => {
    const range = { min: '2026-09-29', max: '2026-10-13' };

    expect(reasonOf('2026-09-28', range)).toBe('range');
    expect(reasonOf('2026-09-29', range)).toBeNull();
    expect(reasonOf('2026-10-13', range)).toBeNull();
    expect(reasonOf('2026-10-14', range)).toBe('range');
  });

  it('allow-list on and off', () => {
    expect(reasonOf('2026-10-01', { enabledDates: ['2026-10-01'] })).toBeNull();
    expect(reasonOf('2026-10-02', { enabledDates: ['2026-10-01'] })).toBe('not-enabled');
    expect(reasonOf('2026-10-01', { enabledDates: [] })).toBe('not-enabled');
    expect(reasonOf('2026-10-01', { enabledDates: '[]' })).toBe('not-enabled');
    expect(reasonOf('2026-10-01', { enabledDates: null })).toBeNull();
    expect(reasonOf('2026-10-01', { enabledDates: undefined })).toBeNull();
    expect(reasonOf('2026-10-01', { enabledDates: '' })).toBeNull();
  });

  it('precedence', () => {
    const everything = {
      min: '2026-10-05',
      disabledDates: ['2026-10-02', '2026-10-09'],
      disabledWeekdays: [5],
      enabledDates: ['2026-10-08'],
      isDateDisabled: () => true,
    };

    expect(BLOCK_ORDER).toEqual(['range', 'disabled-date', 'disabled-weekday', 'not-enabled', 'rule']);
    expect(reasonOf('2026-10-02', everything)).toBe('range');
    expect(reasonOf('2026-10-09', everything)).toBe('disabled-date');
    expect(reasonOf('2026-10-16', everything)).toBe('disabled-weekday');
    expect(reasonOf('2026-10-07', everything)).toBe('not-enabled');
    expect(reasonOf('2026-10-08', everything)).toBe('rule');
  });

  it('allow-list never re-enables', () => {
    const rules = { enabledDates: ['2026-10-02', '2026-10-06'], disabledDates: ['2026-10-06'], disabledWeekdays: [5] };

    expect(reasonOf('2026-10-02', rules)).toBe('disabled-weekday');
    expect(reasonOf('2026-10-06', rules)).toBe('disabled-date');
  });

  it('isDateDisabled: asked last, fails closed', () => {
    const asked: string[] = [];
    const isDateDisabled = (date: string) => {
      asked.push(date);
      return date === '2026-10-07';
    };

    expect(reasonOf('2026-10-07', { isDateDisabled })).toBe('rule');
    expect(reasonOf('2026-10-08', { isDateDisabled })).toBeNull();
    expect(reasonOf('2026-10-10', { isDateDisabled, max: '2026-10-09' })).toBe('range');
    expect(asked).toEqual(['2026-10-07', '2026-10-08']);

    expect(
      reasonOf('2026-10-08', {
        isDateDisabled: () => {
          throw new Error('boom');
        },
      }),
    ).toBe('rule');
  });

  it('closed vs unavailable', () => {
    expect(blockKind('range')).toBe('unavailable');
    expect(blockKind('not-enabled')).toBe('unavailable');
    expect(blockKind('disabled-date')).toBe('closed');
    expect(blockKind('disabled-weekday')).toBe('closed');
    expect(blockKind('rule')).toBe('closed');
    expect(blockKind(null)).toBeNull();
  });
});
