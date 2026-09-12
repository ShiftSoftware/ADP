import { decodeTimeOffset } from './decode-time-offset';

describe('decodeTimeOffset', () => {
  it('applies relative calendar limits from the supplied page clock', () => {
    expect(decodeTimeOffset({ offsets: [0, 0, 5], type: 'date', today: '2026-09-01' })).toBe('2026-09-06');
    expect(decodeTimeOffset({ offsets: [0, 0, 0, 9, 30], type: 'datetime-local', today: '2026-09-01' })).toBe('2026-09-01T09:30');
  });
});
