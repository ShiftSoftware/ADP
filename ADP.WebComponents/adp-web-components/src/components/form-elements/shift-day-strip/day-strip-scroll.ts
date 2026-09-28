// Positions are measured along the reading direction from the viewport's start edge, so LTR and RTL share one set of rules.
export interface Span {
  start: number;
  end: number;
}

const EPSILON = 0.5;

// Lands on a card start, not the minimum distance, so mandatory snapping cannot pull the card back out of view.
export function revealDelta(card: Span, cards: Span[], width: number, inset: number): number {
  if (card.start >= inset - EPSILON && card.end <= width - inset + EPSILON) return 0;
  if (card.start < inset) return card.start - inset;

  const needed = card.end - (width - inset);
  const snap = cards.map(other => other.start - inset).find(delta => delta >= needed - EPSILON);

  return snap ?? needed;
}

export function visibleCount(width: number, inset: number, cards: Span[]): number {
  if (cards.length < 2) return 1;

  const pitch = cards[1].start - cards[0].start;
  const size = cards[0].end - cards[0].start;

  return pitch > 0 ? Math.max(1, Math.floor((width - inset * 2 - size + EPSILON) / pitch) + 1) : 1;
}

export function pageDelta(cards: Span[], width: number, inset: number, step: 1 | -1): number {
  if (!cards.length) return 0;

  // Scrolled to the very start there is no fade, so the first card counts as visible from the edge.
  const found = cards.findIndex((card, index) => card.start >= (index === 0 ? 0 : inset) - EPSILON);
  const firstVisible = found === -1 ? cards.length - 1 : found;
  const target = Math.min(cards.length - 1, Math.max(0, firstVisible + step * visibleCount(width, inset, cards)));

  return cards[target].start - inset;
}
