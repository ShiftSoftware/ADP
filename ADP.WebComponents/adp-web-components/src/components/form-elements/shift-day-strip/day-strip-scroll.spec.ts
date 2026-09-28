import { pageDelta, revealDelta, Span, visibleCount } from './day-strip-scroll';

// Cards 58 wide, 8 apart, starting 2px in; the window is 300 wide with a 16px inset each side.
const strip = (offset = 0, count = 20): Span[] => Array.from({ length: count }, (_, i) => ({ start: 2 + i * 66 - offset, end: 2 + i * 66 + 58 - offset }));

describe('revealDelta', () => {
  it('leaves a card that is fully inside alone', () => {
    const cards = strip(50);
    expect(revealDelta(cards[2], cards, 300, 16)).toBe(0);
  });

  it('brings an earlier card to the start inset', () => {
    const cards = strip(50);
    expect(revealDelta(cards[0], cards, 300, 16)).toBe(cards[0].start - 16);
  });

  it('brings a later card in by landing on a card start', () => {
    const cards = strip(0);
    const delta = revealDelta(cards[5], cards, 300, 16);

    expect(cards.map(card => card.start - 16)).toContain(delta);
    expect(cards[5].end - delta).toBeLessThanOrEqual(300 - 16);
    expect(cards[5].end - (delta - 66)).toBeGreaterThan(300 - 16);
  });
});

describe('paging', () => {
  it('counts whole cards inside the insets', () => {
    expect(visibleCount(300, 16, strip())).toBe(4);
    expect(visibleCount(120, 16, strip())).toBe(1);
    expect(visibleCount(300, 16, strip(0, 1))).toBe(1);
  });

  it('moves by one visible page each way and stops at the ends', () => {
    const cards = strip(2 + 66 * 5 - 16);

    expect(pageDelta(cards, 300, 16, 1)).toBe(66 * 4);
    expect(pageDelta(cards, 300, 16, -1)).toBe(-66 * 4);
    expect(pageDelta(strip(0, 3), 300, 16, 1)).toBe(2 + 66 * 2 - 16);
    expect(pageDelta(strip(0), 300, 16, 1)).toBe(2 + 66 * 4 - 16);
    expect(pageDelta(strip(2 + 66 * 4 - 16), 300, 16, -1)).toBeLessThanOrEqual(-(2 + 66 * 4 - 16));
  });
});
