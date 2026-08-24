import { readFileSync } from 'fs';
import { join } from 'path';

import { MockElement } from '@stencil/core/mock-doc';
import { newSpecPage, SpecPage } from '@stencil/core/testing';

import { VehicleClaimableItems } from './vehicle-claimable-items';

import vehicleLookupMocks from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';

/**
 * The lane behind the cards is drawn by measuring where the next claimable card sits, which makes
 * it the one piece of this component that cannot be produced by rendering alone: it has to read
 * the DOM back. These tests are about *when* that read happens, because reading it at the wrong
 * moment is invisible in every ordinary case and leaves the lane empty in the awkward one — a
 * lookup that lands while the browser tab is in the background, where the browser goes on running
 * timers but stops running requestAnimationFrame, which is what Stencil renders on.
 */

/** The mock the generator produces: two claimed items, then the next one waiting to be claimed. */
const lookup = (vehicleLookupMocks as any)['JTMHX01J8L4198293'];

/** SI-003 — the first item that is pending and not locked, so the lane should stop at it. */
const FIRST_AWAITING_INDEX = 2;

const LANE_LEFT = 40;
const LANE_WIDTH = 1000;

/** How far apart the cards sit. Mutable so a test can change the geometry under a resize. */
let cardPitch = 250;

/** Set while a test wants the DOM to measure like a browser rather than like an empty layout. */
let laidOut = false;

const ZERO_RECT = { bottom: 0, height: 0, left: 0, right: 0, top: 0, width: 0, x: 0, y: 0 };

const rectAt = (left: number, width: number) => ({ ...ZERO_RECT, left, width, right: left + width, x: left });

/**
 * mock-doc has no layout engine, so everything measures as zero — which is also what a real
 * browser reports for an element it has not laid out. Both are worth exercising, so geometry is
 * handed out here and `laidOut` decides which of the two the test gets.
 */
beforeAll(() => {
  (MockElement.prototype as any).getBoundingClientRect = function () {
    if (!laidOut) return ZERO_RECT;

    if (this.classList?.contains('progress-lane')) return rectAt(LANE_LEFT, LANE_WIDTH);

    if (this.classList?.contains('claimable-item')) {
      const cards = Array.from(this.parentElement?.children ?? []).filter((child: any) => child.classList?.contains('claimable-item'));
      return rectAt(LANE_LEFT + cards.indexOf(this) * cardPitch, 120);
    }

    return ZERO_RECT;
  };

  // The component reads its locale files over the network; serve them from disk instead.
  (global as any).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

beforeEach(() => {
  laidOut = true;
  cardPitch = 250;
});

const newPage = () =>
  newSpecPage({
    components: [VehicleClaimableItems],
    html: '<vehicle-claimable-items></vehicle-claimable-items>',
  });

const component = (page: SpecPage) => page.rootInstance as VehicleClaimableItems;

const laneWidth = (page: SpecPage) => (page.root.shadowRoot.querySelector('.progress-bar') as HTMLElement).style.width;

const cardCount = (page: SpecPage) => page.root.shadowRoot.querySelectorAll('.claimable-item').length;

/**
 * A fresh list resets the bar and yields once, so the browser paints a zero-width lane before the
 * lane animates out to its measurement. Give that yield room to happen.
 */
const settle = async (page: SpecPage) => {
  await new Promise(resolve => setTimeout(resolve, 30));
  await page.waitForChanges();
};

/** Cards sit one pitch apart, so the lane should stop on the first awaiting card. */
const expectedWidth = () => `${(((FIRST_AWAITING_INDEX * cardPitch) / LANE_WIDTH) * 100).toFixed(2)}%`;

describe('progress lane measurement', () => {
  it('measures the lane on the render that draws the cards', async () => {
    const page = await newPage();

    component(page).vehicleLookup = lookup;
    await page.waitForChanges();
    await settle(page);

    expect(cardCount(page)).toBe(lookup.serviceItems.length);
    expect(laneWidth(page)).toBe(expectedWidth());
  });

  it('survives an answer that arrives before the cards do, and measures once they are drawn', async () => {
    const page = await newPage();
    const claimableItems = component(page);

    // A background tab keeps running timers but stops running requestAnimationFrame, so the
    // answer lands and anything on a timer fires while the render that draws the cards is still
    // queued. Taking the measurement at that moment is what used to leave the lane empty.
    claimableItems.vehicleLookup = lookup;
    expect(cardCount(page)).toBe(0);
    await (claimableItems as any).updateProgressBar();

    // ... and then the tab comes back and Stencil finally renders.
    await page.waitForChanges();
    await settle(page);

    expect(cardCount(page)).toBe(lookup.serviceItems.length);
    expect(laneWidth(page)).toBe(expectedWidth());
  });

  it('leaves the request standing when nothing has been laid out, rather than writing an unusable width', async () => {
    laidOut = false;

    const page = await newPage();

    component(page).vehicleLookup = lookup;
    await page.waitForChanges();
    await settle(page);

    // The cards are on the page, so the only thing missing is the layout. There is no ratio to
    // compute against a zero-width lane, and the bar keeps the zero the reset gave it instead of
    // the NaN the arithmetic produces, which a browser discards silently.
    expect(cardCount(page)).toBe(lookup.serviceItems.length);
    expect(laneWidth(page)).toBe('0%');
  });

  it('takes the standing measurement when the page becomes visible again', async () => {
    laidOut = false;

    const page = await newPage();

    component(page).vehicleLookup = lookup;
    await page.waitForChanges();
    await settle(page);
    expect(laneWidth(page)).toBe('0%');

    laidOut = true;
    page.doc.dispatchEvent(new (page.win as any).Event('visibilitychange'));
    await settle(page);

    expect(laneWidth(page)).toBe(expectedWidth());
  });

  it('re-measures on a resize', async () => {
    const page = await newPage();

    component(page).vehicleLookup = lookup;
    await page.waitForChanges();
    await settle(page);
    expect(laneWidth(page)).toBe('50.00%');

    cardPitch = 100;
    page.win.dispatchEvent(new (page.win as any).Event('resize'));
    await settle(page);

    expect(laneWidth(page)).toBe('20.00%');
  });
});
