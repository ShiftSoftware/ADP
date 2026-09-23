/**
 * Eases blocks between two content heights, for a fixed structure whose values change under a
 * cover (motion.md rule 11; design language § 21).
 *
 * A value that wraps to a second line on the next vehicle makes its block taller, and that change
 * would otherwise land in one frame under a cover about to lift on it. No primitive covers it:
 * `.collapsible` moves between zero and content height, and `height: auto` cannot be transitioned
 * to on the family's browser floor.
 *
 * So: measure → patch → reflow → target → release, the steps `createTabRegion` already uses. The
 * blocks carry `.resize-settle`, which reads `--resize-settle-height`; this writes the two figures
 * with one forced reflow between them and clears it once settled. Unset at rest is deliberate — the
 * block is `auto` again, so a resize or a language change re-lays it out rather than animating from
 * a stale figure.
 *
 * Called from `componentWillRender` / `componentDidRender`. Nothing here gates input.
 */

/** The `--settle` token (lookup-tokens.css) when the stylesheet cannot be read, as in tests. */
const DEFAULT_SETTLE_MS = 480;

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
      // `previous` is what `beforeSwap` measured, BEFORE the patch. Re-measuring it here would read
      // the element that has already been patched, which makes it equal to `next` and sends the
      // "nothing moved" guard below off every time — the defect that left the primitive inert from
      // the day it landed until 2026-09-22. The recorded heights are the whole point of the map.
      const previous = items.map(block => pinned.get(block) as number);
      pinned.clear();
      if (!items.length) return;

      // A pin left over from a settle that is still running would make `next` the old target rather
      // than the natural height of the new content, so it is lifted first — in one style pass, so
      // the read below costs one reflow. All of this happens inside componentDidRender, before
      // paint, so the natural height is never on screen.
      const settling = items.some(block => !!block.style.getPropertyValue('--resize-settle-height'));
      if (settling) items.forEach(block => block.style.removeProperty('--resize-settle-height'));
      const next = items.map(block => heightOf(block));

      // Nothing moved: leave the blocks `auto` rather than pin them to a figure for a settle. The
      // lift above already released anything that was still running, so this path ends at `auto`.
      if (next.every((height, index) => Math.abs(height - previous[index]) < 0.5)) {
        if (settling) {
          if (releaseTimer) clearTimeout(releaseTimer);
          release();
        }
        return;
      }

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
