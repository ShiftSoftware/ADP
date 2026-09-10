/**
 * Brings `anchor` to the top of its scroll container with the browser's own kind of movement —
 * eased in and out, its length set by the distance — and keeps it there while the layout beneath
 * is still growing, so a drawer sliding open under a row carries the page with it instead of
 * opening out of sight below the fold.
 *
 * Why not `scrollIntoView({ behavior: 'smooth' })`: it fixes its destination when it is called,
 * and a page that is still growing cannot yet scroll that far, so the row stops short and the
 * drawer opens where the reader cannot see it. It also scrolls `overflow: hidden` ancestors
 * internally, which shifts the content of a container that is animating its height. This eases
 * the scroll position itself, re-reads the target and the scroll range on every frame, and keeps
 * following the range for `followMs` — how long the layout beneath takes to settle — so the page
 * and the drawer land together.
 *
 * Honours the anchor's `scroll-margin-top` and each container's `scroll-padding-top` (a host whose
 * fixed header covers the top of the page sets the latter on its scroller, as it would for
 * `scrollIntoView`), lands at once under reduced motion, and yields to the reader: a wheel, touch
 * or key cancels it. Returns a function that stops it early.
 */
export const revealBelow = (anchor: Element, followMs: number): (() => void) => {
  const scrollers = anchor.isConnected ? scrollContainersOf(anchor) : [];
  if (!scrollers.length) return () => {};

  const reduced = prefersReducedMotion();
  let frame: number | undefined;
  let startedAt: number | undefined;
  let starts: number[] = [];
  let travel = 0;

  const stop = () => {
    if (frame !== undefined) window.cancelAnimationFrame(frame);
    frame = undefined;
    CANCEL_EVENTS.forEach(type => window.removeEventListener(type, stop));
  };

  /**
   * Where the container would have to be scrolled to for the anchor to sit at its top — read afresh
   * each frame, because what is above the anchor may be moving too.
   */
  const targetOf = (scroller: Element) => {
    const isDocument = scroller === document.scrollingElement;
    const viewportTop = isDocument ? 0 : scroller.getBoundingClientRect().top + scroller.clientTop;
    const padding = pixels(window.getComputedStyle(isDocument ? document.documentElement : scroller).scrollPaddingTop);
    const margin = pixels(window.getComputedStyle(anchor).scrollMarginTop);

    return scroller.scrollTop + anchor.getBoundingClientRect().top - viewportTop - margin - padding;
  };

  const step = (timestamp?: number) => {
    frame = undefined;
    const now = typeof timestamp === 'number' ? timestamp : Date.now();

    if (startedAt === undefined) {
      // The journey is measured on the first frame, when layout has settled, and its length set
      // from the distance the way the browser's own smooth scroll does.
      startedAt = now;
      starts = scrollers.map(scroller => scroller.scrollTop);
      const distance = Math.max(...scrollers.map((scroller, index) => Math.abs(targetOf(scroller) - starts[index])));
      travel = reduced ? 0 : scrollDuration(distance);
    }

    const elapsed = now - startedAt;
    const eased = travel > 0 ? easeInOut(Math.min(1, elapsed / travel)) : 1;
    let heldBack = false;

    scrollers.forEach((scroller, index) => {
      const target = targetOf(scroller);
      const position = starts[index] + (target - starts[index]) * eased;
      const range = scroller.scrollHeight - scroller.clientHeight;
      const reach = Number.isFinite(range) ? Math.max(0, range) : position;

      if (target > reach) heldBack = true;
      scroller.scrollTop = Math.max(0, Math.min(position, reach));
    });

    // Keep going while the movement is under way, and after it only while something beneath is
    // still growing the page towards the target.
    if (elapsed < travel || (heldBack && elapsed < followMs)) frame = window.requestAnimationFrame(step);
    else stop();
  };

  CANCEL_EVENTS.forEach(type => window.addEventListener(type, stop, { passive: true }));
  frame = window.requestAnimationFrame(step);

  return stop;
};

/**
 * How long a scroll of `distance` pixels takes: the square-root law the browser uses for its own
 * smooth scrolls, so a short hop is brief and a long journey is long without being slow, held
 * between a floor that keeps a small nudge gentle and a ceiling that keeps a page-length one brisk.
 */
export const scrollDuration = (distance: number) => Math.min(MAX_DURATION_MS, Math.max(MIN_DURATION_MS, Math.sqrt(Math.max(0, distance)) * DURATION_PER_ROOT_PX));

const MIN_DURATION_MS = 260;
const MAX_DURATION_MS = 720;
const DURATION_PER_ROOT_PX = 16.7;

const CANCEL_EVENTS = ['wheel', 'touchmove', 'keydown'] as const;

/** Slow to start, slow to arrive: the movement of something with mass. */
const easeInOut = (t: number) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);

/** A computed length in pixels; anything else (`auto`, a percentage, nothing) counts as zero. */
const pixels = (value?: string | null) => (value && value.endsWith('px') ? parseFloat(value) || 0 : 0);

const prefersReducedMotion = () => typeof window.matchMedia === 'function' && !!window.matchMedia('(prefers-reduced-motion: reduce)')?.matches;

/** The parent to walk to next, stepping out of a shadow root onto its host. */
const parentOf = (node: Node): Element | null => node.parentElement ?? ((node.getRootNode() as ShadowRoot).host || null);

/**
 * Every scroll container above the anchor, innermost first, ending with the document's. A
 * container counts when it is set to scroll and can: an `overflow: auto` wrapper whose content
 * fits is not a scroller, and an `overflow: hidden` box is never one — scrolling that would shift
 * its content, not the page.
 */
const scrollContainersOf = (anchor: Element): Element[] => {
  const found: Element[] = [];

  for (let node = parentOf(anchor); node; node = parentOf(node)) {
    const overflow = window.getComputedStyle(node).overflowY;
    if (/^(auto|scroll|overlay)$/.test(overflow || '') && node.scrollHeight > node.clientHeight) found.push(node);
  }

  const root = document.scrollingElement;
  if (root && !found.includes(root)) found.push(root);

  return found.filter(scroller => typeof scroller.scrollTop === 'number');
};
