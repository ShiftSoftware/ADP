import { formatPickerValue, readPickerValue } from './picker';

describe('formatPickerValue', () => {
  it('iso by default, with an offset only when one is given', () => {
    expect(formatPickerValue('2026-10-08T15:00')).toBe('2026-10-08T15:00:00');
    expect(formatPickerValue('2026-10-08T15:00', 'iso', '+04:00')).toBe('2026-10-08T15:00:00+04:00');
    expect(formatPickerValue('2026-10-08T15:00', 'iso', 'nonsense')).toBe('2026-10-08T15:00:00');
  });

  it('a pattern of yyyy MM dd HH hh mm ss tt', () => {
    expect(formatPickerValue('2026-10-08T15:00', 'yyyy-MM-dd HH:mm')).toBe('2026-10-08 15:00');
    expect(formatPickerValue('2026-10-08T00:05', 'dd/MM/yyyy hh:mm tt')).toBe('08/10/2026 12:05 AM');
    expect(formatPickerValue('2026-10-08T15:00', 'yyyy-MM-ddTHH:mm:ss')).toBe('2026-10-08T15:00:00');
  });

  it('nothing for no slot', () => {
    expect(formatPickerValue('')).toBe('');
    expect(formatPickerValue('2026-10-08', 'yyyy-MM-dd HH:mm')).toBe('');
  });
});

describe('readPickerValue', () => {
  it('reads ISO in any offset as the branch time', () => {
    expect(readPickerValue('2026-10-08T15:00:00+03:00')).toBe('2026-10-08T15:00');
    expect(readPickerValue('2026-10-08T12:00:00Z')).toBe('2026-10-08T12:00');
    expect(readPickerValue('2026-10-08T12:00:00Z', 'iso', '+03:00')).toBe('2026-10-08T15:00');
    expect(readPickerValue('2026-10-08T12:00:00.0000000+00:00', 'iso', '+03:00')).toBe('2026-10-08T15:00');
    expect(readPickerValue('2026-10-08T23:30:00+03:00', 'iso', '+00:00')).toBe('2026-10-08T20:30');
    expect(readPickerValue('2026-10-08T15:00')).toBe('2026-10-08T15:00');
  });

  it('reads the value format too, and ISO whatever the format', () => {
    expect(readPickerValue('2026-10-08 15:00', 'yyyy-MM-dd HH:mm')).toBe('2026-10-08T15:00');
    expect(readPickerValue('08/10/2026 03:00 PM', 'dd/MM/yyyy hh:mm tt')).toBe('2026-10-08T15:00');
    expect(readPickerValue('2026-10-08T15:00:00+03:00', 'dd/MM/yyyy hh:mm tt')).toBe('2026-10-08T15:00');
  });

  it('nothing for empty or unreadable text', () => {
    expect(readPickerValue('')).toBe('');
    expect(readPickerValue('tomorrow')).toBe('');
    expect(readPickerValue('08/13/2026 15:00', 'dd/MM/yyyy HH:mm')).toBe('');
  });
});
