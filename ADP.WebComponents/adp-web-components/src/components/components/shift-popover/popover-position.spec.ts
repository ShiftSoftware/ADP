import { placePanel, visiblePart } from './popover-position';

const viewport = { width: 1280, height: 800 };
const panel = { width: 380, height: 400 };
const options = { offset: 8, margin: 8, rtl: false };
const box = (top: number, left: number, width = 200, height = 40) => ({ top, left, bottom: top + height, right: left + width });

describe('placePanel', () => {
  it('below the anchor, start-aligned', () => {
    expect(placePanel(box(100, 300), panel, viewport, options)).toEqual({ top: 148, left: 300, side: 'below', maxHeight: null });
  });

  it('right-aligned in RTL', () => {
    expect(placePanel(box(100, 700), panel, viewport, { ...options, rtl: true })).toEqual({ top: 148, left: 520, side: 'below', maxHeight: null });
  });

  it('flips above when there is no room below', () => {
    expect(placePanel(box(700, 300), panel, viewport, options)).toEqual({ top: 292, left: 300, side: 'above', maxHeight: null });
  });

  it('never covers the anchor: without room on either side it takes the larger one and caps its height', () => {
    const short = { width: 1280, height: 720 };
    const middle = box(340, 300);

    expect(placePanel(middle, panel, short, options)).toEqual({ top: 388, left: 300, side: 'below', maxHeight: 720 - 8 - 388 });
    expect(placePanel(box(300, 300), panel, { width: 1280, height: 600 }, options)).toEqual({ top: 8, left: 300, side: 'above', maxHeight: 284 });

    for (const top of [0, 120, 260, 340, 420, 560, 680]) {
      const anchor = box(top, 300);
      const placed = placePanel(anchor, panel, short, options);
      const bottom = placed.top + (placed.maxHeight ?? panel.height);

      expect(bottom <= anchor.top || placed.top >= anchor.bottom).toBe(true);
    }
  });

  it('shifts to stay inside the viewport at either edge', () => {
    expect(placePanel(box(100, 1200, 60), panel, viewport, options).left).toBe(1280 - 8 - 380);
    expect(placePanel(box(100, -50), panel, viewport, options).left).toBe(8);
    expect(placePanel(box(100, 20, 60), panel, viewport, { ...options, rtl: true }).left).toBe(8);
  });

  it('a panel wider than the viewport starts at the margin', () => {
    expect(placePanel(box(100, 10), panel, { width: 320, height: 800 }, options).left).toBe(8);
  });
});

describe('visiblePart', () => {
  it('clips through every scrolling ancestor', () => {
    const viewportBox = { top: 0, left: 0, bottom: 800, right: 1280 };

    expect(visiblePart(box(100, 100), [viewportBox])).toEqual(box(100, 100));
    expect(visiblePart(box(100, 100), [viewportBox, { top: 200, left: 0, bottom: 400, right: 400 }])).toBeNull();
    expect(visiblePart(box(-60, 100), [viewportBox])).toBeNull();
    expect(visiblePart(box(190, 100), [viewportBox, { top: 200, left: 0, bottom: 400, right: 400 }])).toEqual({ top: 200, left: 100, bottom: 230, right: 300 });
  });
});
