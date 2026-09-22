import { createResizeSettle } from './resize-settle';

/**
 * `.resize-settle` is the one new primitive the redesign adds, and the whole of M.8 rests on it: a
 * value block that goes from one line to two — the exterior colour cell, every time a catalogue
 * name resolves — must ease between the two heights under its cover instead of stepping.
 *
 * A spec page has no layout, so the heights are scripted on the blocks themselves: what is under
 * test is the measure → patch → reflow → target sequence, not the browser's line breaking.
 */

/** A real element whose height is read from a script, so the helper's three measurements are known. */
const block = (heights: number[]) => {
  const element = document.createElement('div');
  let read = 0;

  element.getBoundingClientRect = (() => ({ height: heights[Math.min(read++, heights.length - 1)] })) as never;
  return element;
};

const pin = (element: HTMLElement) => element.style.getPropertyValue('--resize-settle-height');

describe('createResizeSettle', () => {
  /**
   * The observed behaviour today, pinned so that the `it.failing` below cannot pass for the wrong
   * reason. A block whose content grew from one line to two is left with no pinned height at all,
   * so the stylesheet's `height: var(--resize-settle-height, auto)` stays `auto` and the change
   * lands in one frame.
   *
   * Confirmed in the browser as well as here: at 420px, 700px and the full column, the exterior
   * colour cell goes 16.9px → 34.4px across a lookup and neither a `--resize-settle-height` write
   * nor a `height` transition on any `.spec-value` is ever observed
   * (scratchpad/diag-resize.mjs).
   */
  it('DEFECT: writes no height at all when a value grows, so nothing eases', () => {
    // 20px before the patch; 40px once the new value is in the DOM.
    const grown = block([20, 40, 40]);
    const settle = createResizeSettle(() => [grown]);

    settle.beforeSwap();
    settle.afterSwap();

    expect(pin(grown)).toBe('');
  });

  /**
   * What M.8 specifies, and what the browser evidence says does not happen.
   *
   * The cause is in `afterSwap`: `beforeSwap` records the live heights into `pinned`, but it never
   * writes them to the DOM, and `afterSwap` then clears that map and re-measures `previous` from
   * the already-patched element. `previous` is therefore the *new* height, `next` is the same
   * figure, and the "nothing moved" guard returns before anything is pinned. The recorded heights
   * are never read. The fix is to take `previous` from `pinned` rather than from the DOM.
   *
   * `it.failing` so the suite stays honest in both directions: it passes while the defect is there
   * and fails the day somebody fixes it without coming back to this test.
   */
  it.failing('eases a grown block from its old height to its new one', () => {
    const grown = block([20, 40, 40]);
    const settle = createResizeSettle(() => [grown]);

    settle.beforeSwap();
    settle.afterSwap();

    // The last write is the target; the pin that preceded it was the measured old height.
    expect(pin(grown)).toBe('40px');
  });

  it('leaves a block that did not move alone', () => {
    // Nothing changed, so pinning a figure for a settle would be worse than doing nothing.
    const still = block([24, 24, 24]);
    const settle = createResizeSettle(() => [still]);

    settle.beforeSwap();
    settle.afterSwap();

    expect(pin(still)).toBe('');
  });

  it('does nothing when the render was not announced', () => {
    // `afterSwap` without a `beforeSwap` is a render that changed no value: no measurement, no pin.
    const orphan = block([20, 40, 40]);
    const settle = createResizeSettle(() => [orphan]);

    settle.afterSwap();

    expect(pin(orphan)).toBe('');
  });

  it('clears every pinned height on dispose, so nothing is left to go stale', () => {
    // The property is UNSET at rest by design: a host resize, a breakpoint or a language change
    // must re-lay the block out naturally rather than animate from a figure nobody refreshed.
    const element = block([20, 40, 40]);
    element.style.setProperty('--resize-settle-height', '40px');
    element.style.transition = 'none';

    const settle = createResizeSettle(() => [element]);
    settle.dispose();

    expect(pin(element)).toBe('');
    expect(element.style.transition).toBe('');
  });
});
