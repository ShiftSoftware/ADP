import {
  addDays,
  addMonths,
  addMonthsToMonth,
  addYears,
  clampDate,
  clampMonth,
  daysInMonth,
  endOfWeek,
  fromDayNumber,
  isLeapYear,
  lastOfMonth,
  localDate,
  monthGrid,
  normaliseWeekday,
  parseDate,
  parseMonth,
  startOfWeek,
  toDayNumber,
  Weekday,
  weekdayOf,
} from './calendar-date';

const MONTH_STARTING_ON: Record<Weekday, string> = {
  0: '2026-02',
  1: '2026-06',
  2: '2026-09',
  3: '2026-04',
  4: '2026-01',
  5: '2026-05',
  6: '2026-08',
};

describe('calendar-date parsing', () => {
  it('real dates only', () => {
    expect(parseDate('2026-09-27')).toBe('2026-09-27');
    expect(parseDate(' 2024-02-29 ')).toBe('2024-02-29');
    expect(parseDate('2026-02-29')).toBeNull();
    expect(parseDate('2026-13-01')).toBeNull();
    expect(parseDate('2026-9-1')).toBeNull();
    expect(parseDate('2026-09-27T10:00')).toBeNull();
    expect(parseDate(undefined)).toBeNull();
    expect(parseMonth('2026-09')).toBe('2026-09');
    expect(parseMonth('2026-00')).toBeNull();
    expect(parseMonth('2026-09-01')).toBeNull();
  });

  it('leap years', () => {
    expect([2024, 2000, 2400].map(isLeapYear)).toEqual([true, true, true]);
    expect([2026, 1900, 2100].map(isLeapYear)).toEqual([false, false, false]);
    expect(daysInMonth(2024, 2)).toBe(29);
    expect(daysInMonth(2026, 2)).toBe(28);
    expect(daysInMonth(1900, 2)).toBe(28);
    expect(daysInMonth(2000, 2)).toBe(29);
    expect(lastOfMonth('2024-02')).toBe('2024-02-29');
  });

  it('day numbers round-trip', () => {
    expect(toDayNumber('1970-01-01')).toBe(0);
    for (const date of ['0001-01-01', '1899-12-31', '1900-03-01', '2000-02-29', '2024-02-29', '2026-09-27', '9999-12-31']) {
      expect(fromDayNumber(toDayNumber(date))).toBe(date);
    }
  });

  it('weekdays', () => {
    expect(weekdayOf('2026-09-27')).toBe(0);
    expect(weekdayOf('2026-10-02')).toBe(5);
    expect(weekdayOf('2000-01-01')).toBe(6);
    expect(weekdayOf('1970-01-01')).toBe(4);
  });
});

describe('calendar-date arithmetic', () => {
  it('addDays', () => {
    expect(addDays('2026-12-31', 1)).toBe('2027-01-01');
    expect(addDays('2024-02-28', 1)).toBe('2024-02-29');
    expect(addDays('2024-03-01', -1)).toBe('2024-02-29');
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28');
  });

  it('addMonths and addYears clamp', () => {
    expect(addMonths('2026-01-31', 1)).toBe('2026-02-28');
    expect(addMonths('2024-01-31', 1)).toBe('2024-02-29');
    expect(addMonths('2026-03-31', -1)).toBe('2026-02-28');
    expect(addMonths('2026-12-15', 1)).toBe('2027-01-15');
    expect(addYears('2024-02-29', 1)).toBe('2025-02-28');
    expect(addYears('2024-02-29', 4)).toBe('2028-02-29');
    expect(addMonthsToMonth('2026-01', -1)).toBe('2025-12');
  });

  it('week start and end', () => {
    expect(startOfWeek('2026-09-30', 1)).toBe('2026-09-28');
    expect(startOfWeek('2026-09-30', 6)).toBe('2026-09-26');
    expect(startOfWeek('2026-09-30', 0)).toBe('2026-09-27');
    expect(endOfWeek('2026-09-30', 6)).toBe('2026-10-02');
    expect(endOfWeek('2026-09-30', 1)).toBe('2026-10-04');
  });

  it('clamp', () => {
    expect(clampDate('2026-09-01', '2026-09-10', '2026-09-20')).toBe('2026-09-10');
    expect(clampDate('2026-09-30', '2026-09-10', '2026-09-20')).toBe('2026-09-20');
    expect(clampDate('2026-09-15', null, null)).toBe('2026-09-15');
    expect(clampMonth('2026-07', '2026-09-10', '2026-11-02')).toBe('2026-09');
    expect(clampMonth('2027-01', '2026-09-10', '2026-11-02')).toBe('2026-11');
  });

  it('normaliseWeekday', () => {
    expect(normaliseWeekday(7, 1)).toBe(0);
    expect(normaliseWeekday(-1, 1)).toBe(6);
    expect(normaliseWeekday('6', 1)).toBe(6);
    expect(normaliseWeekday(undefined, 1)).toBe(1);
    expect(normaliseWeekday('x', 6)).toBe(6);
    expect(normaliseWeekday(1.5, 0)).toBe(0);
  });
});

describe('monthGrid', () => {
  const weekStarts: Weekday[] = [0, 1, 2, 3, 4, 5, 6];

  for (const firstWeekday of weekStarts) {
    for (const weekStartsOn of weekStarts) {
      it(`first weekday ${firstWeekday}, week start ${weekStartsOn}`, () => {
        const month = MONTH_STARTING_ON[firstWeekday];
        const cells = monthGrid(month, weekStartsOn);
        const lead = (firstWeekday - weekStartsOn + 7) % 7;
        const inMonth = cells.filter(cell => cell.inMonth);

        expect(weekdayOf(`${month}-01`)).toBe(firstWeekday);
        expect(cells).toHaveLength(42);
        expect(weekdayOf(cells[0].date)).toBe(weekStartsOn);
        expect(cells.findIndex(cell => cell.inMonth)).toBe(lead);
        expect(inMonth[0].date).toBe(`${month}-01`);
        expect(inMonth.at(-1)?.date).toBe(lastOfMonth(month));
        cells.slice(1).forEach((cell, index) => expect(cell.date).toBe(addDays(cells[index].date, 1)));
      });
    }
  }

  it('six-week and four-week months', () => {
    const august = monthGrid('2026-08', 0);
    expect(august.filter(cell => cell.inMonth)).toHaveLength(31);
    expect(august.map(cell => cell.inMonth).lastIndexOf(true)).toBe(36);

    const february = monthGrid('2026-02', 0);
    expect(february.filter(cell => cell.inMonth)).toHaveLength(28);
    expect(february[0].date).toBe('2026-02-01');
    expect(february.slice(28).every(cell => !cell.inMonth)).toBe(true);
  });

  it('29 February', () => {
    expect(monthGrid('2024-02', 1).some(cell => cell.date === '2024-02-29' && cell.inMonth)).toBe(true);
    expect(monthGrid('2100-02', 1).some(cell => cell.date === '2100-02-29')).toBe(false);
  });
});

describe('time zones', () => {
  const RealDate = Date;

  afterEach(() => {
    global.Date = RealDate;
  });

  it('no Date', () => {
    global.Date = function () {
      throw new Error('Date must not be used');
    } as unknown as DateConstructor;

    expect(monthGrid('2026-03', 6)[0].date).toBe('2026-02-28');
    expect(addMonths('2026-03-31', -1)).toBe('2026-02-28');
    expect(weekdayOf('2026-03-29')).toBe(0);
    expect(clampDate('2026-03-01', '2026-03-05', null)).toBe('2026-03-05');
  });

  it('local today', () => {
    const fake = (year: number, monthIndex: number, day: number) => ({ getFullYear: () => year, getMonth: () => monthIndex, getDate: () => day }) as Date;

    // 22:30 UTC on 27 September is still the 27th in New York and already the 28th in Baghdad.
    expect(localDate(fake(2026, 8, 27))).toBe('2026-09-27');
    expect(localDate(fake(2026, 8, 28))).toBe('2026-09-28');
  });
});
