import { fill, formatTime, isTime, moveInTimes, parseTimes } from './time-slots';

const AMPM = { am: 'AM', pm: 'PM' };

describe('time-slots helpers', () => {
  it('parseTimes: arrays, lists and JSON, sorted and unique, HH:mm only', () => {
    expect(parseTimes(['10:00', '09:00', '10:00'])).toEqual(['09:00', '10:00']);
    expect(parseTimes('13:00, 08:30 9:00')).toEqual(['08:30', '13:00']);
    expect(parseTimes('["23:59","00:00"]')).toEqual(['00:00', '23:59']);
    expect(parseTimes('[not json')).toEqual([]);
    expect(parseTimes('24:00 12:60 noon')).toEqual([]);
    expect(parseTimes(undefined)).toEqual([]);
    expect(parseTimes('')).toEqual([]);
  });

  it('isTime', () => {
    expect([isTime('00:00'), isTime('23:59'), isTime('7:00'), isTime('24:00'), isTime(900)]).toEqual([true, true, false, false, false]);
  });

  it('formatTime: h23 as is, h12 with both twelves', () => {
    expect(formatTime('09:00', 'h23', 'latn', AMPM)).toBe('09:00');
    expect(formatTime('00:00', 'h12', 'latn', AMPM)).toBe('12:00 AM');
    expect(formatTime('00:30', 'h12', 'latn', AMPM)).toBe('12:30 AM');
    expect(formatTime('11:59', 'h12', 'latn', AMPM)).toBe('11:59 AM');
    expect(formatTime('12:00', 'h12', 'latn', AMPM)).toBe('12:00 PM');
    expect(formatTime('13:05', 'h12', 'latn', AMPM)).toBe('1:05 PM');
    expect(formatTime('13:05', 'h23', 'arab', AMPM)).toBe('١٣:٠٥');
  });

  it('fill', () => {
    expect(fill('{count} times', { count: 3 })).toBe('3 times');
    expect(fill('{date} at {time}', { date: 'Mon' })).toBe('Mon at ');
  });

  it('moveInTimes: rows follow the columns, edges hold', () => {
    const grid = { count: 10, columns: 4, rtl: false };

    expect(moveInTimes(0, 'ArrowRight', grid)).toBe(1);
    expect(moveInTimes(0, 'ArrowLeft', grid)).toBe(0);
    expect(moveInTimes(9, 'ArrowRight', grid)).toBe(9);
    expect(moveInTimes(1, 'ArrowDown', grid)).toBe(5);
    expect(moveInTimes(7, 'ArrowDown', grid)).toBe(7);
    expect(moveInTimes(5, 'ArrowUp', grid)).toBe(1);
    expect(moveInTimes(2, 'ArrowUp', grid)).toBe(2);
    expect(moveInTimes(6, 'Home', grid)).toBe(0);
    expect(moveInTimes(2, 'End', grid)).toBe(9);
    expect(moveInTimes(2, 'Tab', grid)).toBeNull();
    expect(moveInTimes(0, 'ArrowRight', { count: 0, columns: 4, rtl: false })).toBeNull();
  });

  it('moveInTimes passes over disabled times and holds when none is left', () => {
    const off = new Set([1, 2, 5, 9]);
    const grid = { count: 10, columns: 4, rtl: false, skip: (index: number) => off.has(index) };

    expect(moveInTimes(0, 'ArrowRight', grid)).toBe(3);
    expect(moveInTimes(3, 'ArrowLeft', grid)).toBe(0);
    expect(moveInTimes(1, 'ArrowDown', grid)).toBe(1);
    expect(moveInTimes(0, 'ArrowDown', grid)).toBe(4);
    expect(moveInTimes(8, 'ArrowRight', grid)).toBe(8);
    expect(moveInTimes(4, 'End', grid)).toBe(8);
    expect(moveInTimes(4, 'Home', { ...grid, skip: (index: number) => index < 2 })).toBe(2);
  });

  it('moveInTimes mirrors in RTL', () => {
    const grid = { count: 4, columns: 2, rtl: true };

    expect(moveInTimes(1, 'ArrowLeft', grid)).toBe(2);
    expect(moveInTimes(1, 'ArrowRight', grid)).toBe(0);
    expect(moveInTimes(0, 'ArrowDown', grid)).toBe(2);
  });
});
