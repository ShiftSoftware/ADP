/**
 * Eases a set of blocks between two content heights, for a fixed structure whose values change
 * under a cover (motion.md rule 11; the design language § 21, "the changeable content").
 *
 * The case: a panel whose body is the same titled slots for every vehicle does not shut to load —
 * each value block carries `shift-skeleton` and the wrapper's `.loading` covers it while the text
 * underneath is swapped. But a value that wraps to a second line on the next vehicle makes its
 * block taller, and without this that change lands in one frame under a cover that is about to
 * lift on it: the grey block jumps, its grid row jumps, and everything below jumps with it. A
 * height that changes without moving is a pop with a different name (motion.md rule 7).
 *
 * No primitive already covers it. `.collapsible` moves between zero and the content's height, and
 * the height announcer only tells *enclosing* containers to stop clipping; neither animates a
 * block between two non-zero content heights, and `height: auto` cannot be transitioned to on the
 * family's browser floor (`interpolate-size: allow-keywords` is not shippable).
 *
 * So: measure → patch → reflow → target → release, the three steps `createTabRegion` already uses
 * for a composite's tab region (lookup-tabs.tsx). The blocks carry `.resize-settle`
 * (lookup-motion.css), which reads `--resize-settle-height` and transitions `height` on `--settle`;
 * this writes the two figures into that property with one forced reflow between them, and clears
 * it once the change has settled. UNSET at rest is deliberate: the block is `auto` again, so a
 * host resize, a container breakpoint or a language change re-lays it out naturally instead of
 * animating from a figure that has gone stale.
 *
 * The owner calls `beforeSwap()` from `componentWillRender` — only when the render about to run
 * changes a value, which is the signature it already computes for the height announcer — and
 * `afterSwap()` from `componentDidRender`. Nothing here gates input; the only timer releases the
 * pinned heights.
 */

/** The `--settle` token (lookup-tokens.css) when the stylesheet cannot be read, as in tests. */
const DEFAULT_SETTLE_MS = 320;

const heightOf = (block: HTMLElement) => block.getBoundingClientRect().height;

const settleOf = (block: HTMLElement | undefined) => {
  if (!block || typeof getComputedStyle !== 'function') return DEFAULT_SETTLE_MS;
  const parsed = parseFloat(getComputedStyle(block).getPropertyValue('--settle'));
  return Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS;
};

export type ResizeSettle = {
  /** componentWillRender, when the render about to run changes a value: pin the live heights. */
  beforeSwap(): void;
  /** componentDidRender: one forced reflow, then the new measured heights on the stylesheet's transition. */
  afterSwap(): void;
  /** Clears --resize-settle-height, so the blocks are `auto` again. Call it when the owner disconnects. */
  dispose(): void;
};

export const createResizeSettle = (blocks: () => Iterable<HTMLElement>): ResizeSettle => {
  /** The heights the blocks were showing when the patch was announced, one entry per block. */
  const pinned = new Map<HTMLElement, number>();
  let releaseTimer: ReturnType<typeof setTimeout> | undefined;

  /** Back to `auto`: nothing left to go stale, so the next natural re-layout is a plain re-layout. */
  const release = () => {
    releaseTimer = undefined;
    for (const block of blocks()) {
      block.style.transition = '';
      block.style.removeProperty('--resize-settle-height');
    }
  };

  return {
    beforeSwap() {
      // A patch announced inside a settling one measures where the last one has got to, which is
      // what the block is actually showing — the same "read the box, never the last target" rule
      // createTabRegion follows, so a change that lands mid-transition retargets from the live value.
      pinned.clear();
      for (const block of blocks()) pinned.set(block, heightOf(block));
    },

    afterSwap() {
      if (!pinned.size) return;

      const items = [...blocks()].filter(block => pinned.has(block));
      pinned.clear();
      if (!items.length) return;

      // The new content is in the DOM but the blocks still carry the old pin. Lift every pin in one
      // style pass, then read: the first read lays the whole set out, so this costs one reflow, and
      // it all happens inside componentDidRender — before paint, so the natural height is never seen.
      const previous = items.map(block => heightOf(block));
      items.forEach(block => block.style.removeProperty('--resize-settle-height'));
      const next = items.map(block => heightOf(block));

      // Nothing moved: leave the blocks `auto` rather than pin them to a figure for a settle.
      if (next.every((height, index) => Math.abs(height - previous[index]) < 0.5)) return;

      // Put them back where they were, with the transition off so the pin lands instead of being
      // animated to, and force the one reflow that makes the pinned value the transition's start.
      items.forEach((block, index) => {
        block.style.transition = 'none';
        block.style.setProperty('--resize-settle-height', `${previous[index]}px`);
      });
      void items[0].offsetHeight;

      // Then the stylesheet's transition and the target, in the same task.
      items.forEach((block, index) => {
        block.style.transition = '';
        block.style.setProperty('--resize-settle-height', `${next[index]}px`);
      });

      if (releaseTimer) clearTimeout(releaseTimer);
      releaseTimer = setTimeout(release, settleOf(items[0]) + 40);
    },

    dispose() {
      if (releaseTimer) clearTimeout(releaseTimer);
      pinned.clear();
      release();
    },
  };
};
