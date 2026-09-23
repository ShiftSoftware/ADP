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

/** Every `--resize-settle-height` written to a block, in order, so the sequence itself is testable. */
const recordWrites = (element: HTMLElement) => {
  const writes: string[] = [];
  const setProperty = element.style.setProperty.bind(element.style);

  element.style.setProperty = ((name: string, value: string) => {
    if (name === '--resize-settle-height') writes.push(value);
    setProperty(name, value);
  }) as never;

  return writes;
};

describe('createResizeSettle', () => {
  /**
   * What M.8 specifies: measure → patch → reflow → target.
   *
   * This failed from the day the primitive landed until 2026-09-22. `beforeSwap` recorded the live
   * heights into its map but never wrote them, and `afterSwap` cleared the map and re-measured
   * `previous` from the element that had already been patched — so `previous` was the *new* height,
   * `next` was the same figure, and the "nothing moved" guard returned before anything was pinned.
   * In the browser the exterior colour cell went 16.9px → 34.4px with zero `--resize-settle-height`
   * writes and zero `height` transitions (scratchpad/diag-resize.mjs). `previous` now comes from the
   * recorded map, which is what the map is for.
   */
  it('eases a grown block from its old height to its new one', () => {
    // 20px before the patch; 40px once the new value is in the DOM.
    const grown = block([20, 40, 40]);
    const writes = recordWrites(grown);
    const settle = createResizeSettle(() => [grown]);

    settle.beforeSwap();
    settle.afterSwap();

    // Pinned to the height it was showing, then sent to the new one: the pin is the transition's
    // start, and the reflow between them is what makes it land instead of being animated to.
    expect(writes).toEqual(['20px', '40px']);
    expect(pin(grown)).toBe('40px');
    // The transition is switched off for the pin and released before the target, so the figure the
    // stylesheet animates to is the one the browser eases on its own clock.
    expect(grown.style.transition).toBe('');
  });

  it('starts from where a settle still running has got to, not from its target', () => {
    // A lookup that lands while the last one is still easing: the block is pinned at 40px and is
    // showing 31px on its way there. The new value needs 24px, and the ease must start from 31.
    const interrupted = block([31, 24, 24]);
    interrupted.style.setProperty('--resize-settle-height', '40px');
    const writes = recordWrites(interrupted);
    const settle = createResizeSettle(() => [interrupted]);

    settle.beforeSwap();
    settle.afterSwap();

    expect(writes).toEqual(['31px', '24px']);
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
