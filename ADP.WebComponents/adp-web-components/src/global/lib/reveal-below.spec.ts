import { revealBelow, scrollDuration } from './reveal-below';

/**
 * The scroll that follows an opening drawer. Frames are driven by hand: the helper asks the window
 * for animation frames, and each `tick` runs whatever it asked for at the given time.
 */

const ANCHOR_TOP = 800;
const VIEWPORT = 500;
const FOLLOW = 600;

type Fixture = ReturnType<typeof fixture>;

/** A scroll container holding the anchor, with geometry the mock DOM does not have. */
const fixture = ({ contentHeight = 2000, margin = 0, padding = 0 } = {}) => {
  const scroller = document.createElement('div');
  const anchor = document.createElement('div');
  scroller.appendChild(anchor);
  document.body.appendChild(scroller);

  let scrollTop = 0;
  let scrollHeight = contentHeight;
  const positions: number[] = [];

  Object.defineProperties(scroller, {
    scrollTop: {
      configurable: true,
      get: () => scrollTop,
      set: (value: number) => {
        scrollTop = value;
        positions.push(value);
      },
    },
    scrollHeight: { configurable: true, get: () => scrollHeight },
    clientHeight: { configurable: true, get: () => VIEWPORT },
    clientTop: { configurable: true, get: () => 0 },
  });
  scroller.getBoundingClientRect = () => ({ top: 0, left: 0, right: 0, bottom: VIEWPORT, width: 0, height: VIEWPORT, x: 0, y: 0 }) as DOMRect;
  anchor.getBoundingClientRect = () => ({ top: ANCHOR_TOP - scrollTop, left: 0, right: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0 }) as DOMRect;

  const styles = jest.spyOn(window, 'getComputedStyle').mockImplementation(
    el =>
      ({
        overflowY: el === scroller ? 'auto' : 'visible',
        scrollMarginTop: el === anchor ? `${margin}px` : '0px',
        scrollPaddingTop: el === scroller ? `${padding}px` : 'auto',
      }) as CSSStyleDeclaration,
  );

  return {
    anchor,
    scroller,
    /** Every position written, in order. */
    positions,
    get scrollTop() {
      return scrollTop;
    },
    grow: (by: number) => (scrollHeight += by),
    dispose: () => {
      styles.mockRestore();
      scroller.remove();
    },
  };
};

let frames: Array<(timestamp: number) => void> = [];
let reduced = false;

const tick = (timestamp: number) => {
  const due = frames;
  frames = [];
  due.forEach(frame => frame(timestamp));
};

/** Runs frames every 16ms from `from` until `until` inclusive. */
const run = (from: number, until: number) => {
  for (let at = from; at <= until; at += 16) tick(at);
};

beforeEach(() => {
  frames = [];
  reduced = false;
  jest.spyOn(window, 'requestAnimationFrame').mockImplementation(frame => frames.push(frame));
  jest.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => (frames = []));
  jest.spyOn(window, 'matchMedia').mockImplementation(() => ({ matches: reduced }) as MediaQueryList);
});

afterEach(() => jest.restoreAllMocks());

describe('scrollDuration', () => {
  it('lengthens with the distance, as the browser’s own smooth scroll does, between a gentle floor and a brisk ceiling', () => {
    expect(scrollDuration(0)).toBe(260);
    expect(scrollDuration(100)).toBe(260);
    expect(scrollDuration(400)).toBeGreaterThan(scrollDuration(100));
    expect(scrollDuration(1600)).toBeGreaterThan(scrollDuration(400));
    expect(scrollDuration(1600)).toBeCloseTo(668, 0);
    expect(scrollDuration(10000)).toBe(720);
  });
});

describe('revealBelow', () => {
  let page: Fixture;
  afterEach(() => page?.dispose());

  it('moves the anchor to the top of its scroll container, starting and arriving gently, keeping its scroll margin', () => {
    page = fixture({ margin: 12 });
    const distance = ANCHOR_TOP - 12;
    const travel = scrollDuration(distance);
    revealBelow(page.anchor, FOLLOW);

    tick(1000);
    expect(page.scrollTop).toBe(0);

    // A tenth of the way in, it has barely left; a tenth from the end, it has all but arrived.
    tick(1000 + travel / 10);
    expect(page.scrollTop).toBeLessThan(distance * 0.05);
    tick(1000 + travel / 2);
    expect(page.scrollTop).toBeCloseTo(distance / 2, 0);
    tick(1000 + (travel * 9) / 10);
    expect(page.scrollTop).toBeGreaterThan(distance * 0.95);

    tick(1000 + travel);
    expect(page.scrollTop).toBe(distance);
    // Arrived on a page that is not growing: nothing left to follow.
    expect(frames).toHaveLength(0);
  });

  it('never jumps: no frame moves more than a small share of the journey', () => {
    page = fixture();
    const travel = scrollDuration(ANCHOR_TOP);
    revealBelow(page.anchor, FOLLOW);

    run(1000, 1000 + travel + 16);

    const steps = page.positions.map((position, index) => position - (page.positions[index - 1] ?? 0));
    expect(Math.max(...steps)).toBeLessThan(ANCHOR_TOP * 0.12);
    expect(steps.every(step => step >= 0)).toBe(true);
    expect(page.scrollTop).toBe(ANCHOR_TOP);
  });

  it('follows a container that is still growing, landing where it could not reach when it started', () => {
    // The anchor sits 800px down; the page can scroll only 400px until the drawer beneath opens.
    page = fixture({ contentHeight: VIEWPORT + 400 });
    const travel = scrollDuration(ANCHOR_TOP);
    revealBelow(page.anchor, FOLLOW);

    run(1000, 1000 + travel + 16);
    expect(page.scrollTop).toBe(400);
    expect(frames).toHaveLength(1);

    page.grow(400);
    tick(1000 + travel + 32);
    expect(page.scrollTop).toBe(ANCHOR_TOP);
    // Arrived: the follow ends before its window does.
    expect(frames).toHaveLength(0);
  });

  it('gives up following once the settle window has passed', () => {
    page = fixture({ contentHeight: VIEWPORT + 400 });
    revealBelow(page.anchor, FOLLOW);

    run(1000, 1000 + FOLLOW + 16);
    expect(page.scrollTop).toBe(400);
    expect(frames).toHaveLength(0);
  });

  it('leaves room for a fixed header declared as scroll-padding on the container', () => {
    page = fixture({ padding: 64 });
    revealBelow(page.anchor, FOLLOW);

    run(1000, 1000 + scrollDuration(ANCHOR_TOP - 64) + 16);
    expect(page.scrollTop).toBe(ANCHOR_TOP - 64);
  });

  it('lands at once under reduced motion, and still follows a growing page', () => {
    reduced = true;
    page = fixture({ contentHeight: VIEWPORT + 400 });
    revealBelow(page.anchor, FOLLOW);

    tick(1000);
    expect(page.scrollTop).toBe(400);

    page.grow(400);
    tick(1016);
    expect(page.scrollTop).toBe(ANCHOR_TOP);
    expect(frames).toHaveLength(0);
  });

  it('yields to the reader and to its caller', () => {
    page = fixture();
    revealBelow(page.anchor, FOLLOW);

    tick(1000);
    window.dispatchEvent(new Event('wheel'));
    tick(1000 + 200);
    expect(page.scrollTop).toBe(0);
    expect(frames).toHaveLength(0);

    const stop = revealBelow(page.anchor, FOLLOW);
    tick(2000);
    stop();
    tick(2000 + 200);
    expect(page.scrollTop).toBe(0);
    expect(frames).toHaveLength(0);
  });

  it('does nothing for an anchor with no scroll container', () => {
    const loose = document.createElement('div');
    expect(revealBelow(loose, FOLLOW)).toBeInstanceOf(Function);
    expect(frames).toHaveLength(0);
  });
});
