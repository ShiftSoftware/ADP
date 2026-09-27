import { KeyboardContext, moveFocus } from './calendar-keyboard';

const ltr: KeyboardContext = { rtl: false, weekStartsOn: 1 };
const rtl: KeyboardContext = { rtl: true, weekStartsOn: 6 };

describe('calendar keyboard', () => {
  it('day and week, LTR and RTL', () => {
    expect(moveFocus('2026-09-15', { key: 'ArrowRight' }, ltr)).toBe('2026-09-16');
    expect(moveFocus('2026-09-15', { key: 'ArrowLeft' }, ltr)).toBe('2026-09-14');
    expect(moveFocus('2026-09-15', { key: 'ArrowRight' }, rtl)).toBe('2026-09-14');
    expect(moveFocus('2026-09-15', { key: 'ArrowLeft' }, rtl)).toBe('2026-09-16');
    expect(moveFocus('2026-09-15', { key: 'ArrowDown' }, rtl)).toBe('2026-09-22');
    expect(moveFocus('2026-09-15', { key: 'ArrowUp' }, ltr)).toBe('2026-09-08');
  });

  it('month and year edges', () => {
    expect(moveFocus('2026-09-30', { key: 'ArrowRight' }, ltr)).toBe('2026-10-01');
    expect(moveFocus('2026-09-30', { key: 'ArrowLeft' }, rtl)).toBe('2026-10-01');
    expect(moveFocus('2026-01-01', { key: 'ArrowLeft' }, ltr)).toBe('2025-12-31');
    expect(moveFocus('2026-01-01', { key: 'ArrowRight' }, rtl)).toBe('2025-12-31');
    expect(moveFocus('2026-12-28', { key: 'ArrowDown' }, ltr)).toBe('2027-01-04');
  });

  it('Home and End', () => {
    expect(moveFocus('2026-10-01', { key: 'Home' }, ltr)).toBe('2026-09-28');
    expect(moveFocus('2026-10-01', { key: 'End' }, ltr)).toBe('2026-10-04');
    expect(moveFocus('2026-10-01', { key: 'Home' }, rtl)).toBe('2026-09-26');
    expect(moveFocus('2026-10-01', { key: 'End' }, rtl)).toBe('2026-10-02');
  });

  it('PageUp and PageDown', () => {
    expect(moveFocus('2026-01-31', { key: 'PageDown' }, ltr)).toBe('2026-02-28');
    expect(moveFocus('2024-01-31', { key: 'PageDown' }, ltr)).toBe('2024-02-29');
    expect(moveFocus('2026-03-31', { key: 'PageUp' }, rtl)).toBe('2026-02-28');
    expect(moveFocus('2026-12-15', { key: 'PageDown' }, ltr)).toBe('2027-01-15');
    expect(moveFocus('2024-02-29', { key: 'PageDown', shiftKey: true }, ltr)).toBe('2025-02-28');
    expect(moveFocus('2024-02-29', { key: 'PageUp', shiftKey: true }, rtl)).toBe('2023-02-28');
  });

  it('min and max', () => {
    const bounded = { ...ltr, min: '2026-09-29', max: '2026-10-13' };
    const boundedRtl = { ...rtl, min: '2026-09-29', max: '2026-10-13' };

    expect(moveFocus('2026-09-29', { key: 'ArrowLeft' }, bounded)).toBe('2026-09-29');
    expect(moveFocus('2026-09-29', { key: 'ArrowRight' }, boundedRtl)).toBe('2026-09-29');
    expect(moveFocus('2026-10-13', { key: 'ArrowLeft' }, boundedRtl)).toBe('2026-10-13');
    expect(moveFocus('2026-10-10', { key: 'ArrowDown' }, bounded)).toBe('2026-10-13');
    expect(moveFocus('2026-09-30', { key: 'PageUp' }, bounded)).toBe('2026-09-29');
    expect(moveFocus('2026-10-01', { key: 'PageDown', shiftKey: true }, boundedRtl)).toBe('2026-10-13');
    expect(moveFocus('2026-09-30', { key: 'Home' }, bounded)).toBe('2026-09-29');
  });

  it('other keys', () => {
    expect(moveFocus('2026-09-15', { key: 'Tab' }, ltr)).toBeNull();
    expect(moveFocus('2026-09-15', { key: 'a' }, ltr)).toBeNull();
  });
});
