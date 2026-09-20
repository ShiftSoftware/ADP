import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleLookup } from './vehicle-lookup';

import vehicleLookupMocks from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';

/**
 * How the wrapper hands one search's response to the other panels. With `sscQueryString` the
 * SSC panel's own request is the only one that counts as a campaign check, so a search from any
 * other panel must not hydrate it — and must not leave it blank either: it is told the lookup was
 * skipped, with the VIN, so it can say so and offer to run the check.
 */

const CAMPAIGN_VIN = 'ZT8P9NAL1LG988010';

const response = (vehicleLookupMocks as Record<string, any>)[CAMPAIGN_VIN];

beforeAll(() => {
  (global as any).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

const stubPanel = () => ({
  fetchVin: jest.fn().mockResolvedValue(undefined),
  skipLookup: jest.fn().mockResolvedValue(undefined),
  clearData: jest.fn().mockResolvedValue(undefined),
  setErrorMessage: jest.fn(),
  setMockData: jest.fn(),
});

const newWrapper = async (attributes = '') => {
  const page = await newSpecPage({
    components: [VehicleLookup],
    html: `<vehicle-lookup active-element="vehicle-specification" ${attributes}></vehicle-lookup>`,
  });

  const wrapper = page.rootInstance as VehicleLookup;
  // The children are unregistered here; stand-ins with the methods the wrapper calls take their place.
  const panels = Object.fromEntries(Object.keys((wrapper as any).componentsList).map(tag => [tag, stubPanel()]));
  (wrapper as any).componentsList = panels;

  return { wrapper, panels };
};

describe('vehicle-lookup', () => {
  it('replaces the mock map on every panel when the environment changes', async () => {
    const { wrapper, panels } = await newWrapper();
    const nextEnvironment = { SYNTHETIC: response };

    await wrapper.setMockData(nextEnvironment);

    for (const panel of Object.values(panels)) expect(panel.setMockData).toHaveBeenCalledWith(nextEnvironment);
  });

  it('hydrates every other panel from a search, the SSC panel included when nothing sets it apart', async () => {
    const { wrapper, panels } = await newWrapper();

    await wrapper.handleLoadData(response, panels['vehicle-specification']);

    expect(panels['vehicle-specification'].fetchVin).not.toHaveBeenCalled();
    expect(panels['vehicle-ssc'].fetchVin).toHaveBeenCalledWith(response);
    expect(panels['vehicle-ssc'].skipLookup).not.toHaveBeenCalled();
    expect(panels['vehicle-warranty-timeline'].fetchVin).toHaveBeenCalledWith(response);
  });

  it('tells the SSC panel a search from another tab skipped it, rather than clearing or hydrating it', async () => {
    const { wrapper, panels } = await newWrapper('ssc-query-string="logCampaignCheck=true"');

    await wrapper.handleLoadData(response, panels['vehicle-specification']);

    expect(panels['vehicle-ssc'].fetchVin).not.toHaveBeenCalled();
    expect(panels['vehicle-ssc'].clearData).not.toHaveBeenCalled();
    expect(panels['vehicle-ssc'].skipLookup).toHaveBeenCalledWith(CAMPAIGN_VIN);
    expect(panels['vehicle-warranty-timeline'].fetchVin).toHaveBeenCalledWith(response);
  });

  it('hands the SSC query string to the SSC panel as its lookup-only query string, apart from the shared one', async () => {
    const page = await newSpecPage({
      components: [VehicleLookup],
      html: '<vehicle-lookup active-element="vehicle-ssc" query-string="lang=en" ssc-query-string="logCampaignCheck=true"></vehicle-lookup>',
    });

    const ssc = page.root.shadowRoot.getElementById('vehicle-ssc');
    // The flag reaches the panel through the prop that joins lookups only — never the trace request.
    expect(ssc?.getAttribute('query-string')).toBe('lang=en');
    expect(ssc?.getAttribute('lookup-query-string')).toBe('logCampaignCheck=true');
    expect(page.root.shadowRoot.getElementById('vehicle-specification')?.getAttribute('query-string')).toBe('lang=en');
  });

  it('forwards today to every panel', async () => {
    const page = await newSpecPage({
      components: [VehicleLookup],
      html: '<vehicle-lookup active-element="vehicle-warranty-timeline" today="2026-09-01"></vehicle-lookup>',
    });

    for (const tag of [
      'vehicle-accessories',
      'vehicle-specification',
      'vehicle-paint-thickness',
      'vehicle-service-history',
      'vehicle-claimable-items',
      'vehicle-sale-information',
      'vehicle-warranty-timeline',
      'vehicle-ssc',
    ]) {
      expect(page.root.shadowRoot.getElementById(tag)?.getAttribute('today')).toBe('2026-09-01');
    }
  });

  /**
   * One card. Every panel is mounted in the tab region always; the active one is in flow and the
   * rest are hidden, inert and parked to the side they will return from, so a switch is a change
   * of attributes that the stylesheet turns into a movement. The accent reads the active panel's
   * announced verdict.
   */
  it('draws one card, keeps every panel mounted, and parks the inactive ones by strip order', async () => {
    const page = await newSpecPage({
      components: [VehicleLookup],
      html: '<vehicle-lookup active-element="vehicle-ssc" tab-order="vehicle-specification,vehicle-warranty-timeline,vehicle-ssc,vehicle-service-history"></vehicle-lookup>',
    });
    const shadow = page.root.shadowRoot;

    expect(shadow.querySelectorAll('.lookup-card')).toHaveLength(1);
    expect(shadow.querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('idle');
    expect(shadow.querySelectorAll('.lookup-tabs > .lookup-tab')).toHaveLength(8);

    const tab = (tag: string) => shadow.querySelector(`.lookup-tab[data-tab="${tag}"]`);
    expect(tab('vehicle-ssc')?.getAttribute('data-state')).toBe('active');
    expect(tab('vehicle-ssc')?.getAttribute('aria-hidden')).toBeNull();
    expect(tab('vehicle-specification')?.getAttribute('data-state')).toBe('inactive');
    expect(tab('vehicle-specification')?.getAttribute('aria-hidden')).toBe('true');
    expect(tab('vehicle-specification')?.hasAttribute('inert')).toBe(true);

    // Earlier in the strip parks to the left, later to the right; the active one is not parked.
    expect(shadow.getElementById('vehicle-specification')?.getAttribute('data-tab-park')).toBe('left');
    expect(shadow.getElementById('vehicle-warranty-timeline')?.getAttribute('data-tab-park')).toBe('left');
    expect(shadow.getElementById('vehicle-ssc')?.hasAttribute('data-tab-park')).toBe(false);
    expect(shadow.getElementById('vehicle-service-history')?.getAttribute('data-tab-park')).toBe('right');
    // A tag the host's order does not name keeps this component's own place, after the named ones.
    expect(shadow.getElementById('vehicle-claimable-items')?.getAttribute('data-tab-park')).toBe('right');

    // Switching re-parks from the new active tab, and the old active tab goes inactive.
    page.root.setAttribute('active-element', 'vehicle-specification');
    await page.waitForChanges();
    expect(tab('vehicle-specification')?.getAttribute('data-state')).toBe('active');
    expect(tab('vehicle-ssc')?.getAttribute('data-state')).toBe('inactive');
    expect(shadow.getElementById('vehicle-ssc')?.getAttribute('data-tab-park')).toBe('right');
  });

  it('colours its accent from the active panel’s announced verdict, and follows a switch', async () => {
    const page = await newSpecPage({
      components: [VehicleLookup],
      html: '<vehicle-lookup active-element="vehicle-ssc"></vehicle-lookup>',
    });
    const shadow = page.root.shadowRoot;
    const announce = (tag: string, verdict: string) =>
      shadow.getElementById(tag)?.dispatchEvent(new CustomEvent('verdictChange', { detail: verdict, bubbles: true, composed: true }));

    announce('vehicle-ssc', 'negative');
    announce('vehicle-specification', 'positive');
    await page.waitForChanges();
    expect(shadow.querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('negative');

    page.root.setAttribute('active-element', 'vehicle-specification');
    await page.waitForChanges();
    expect(shadow.querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('positive');

    announce('vehicle-specification', 'idle');
    await page.waitForChanges();
    expect(shadow.querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('idle');
  });

  it('hydrates the other panels from the SSC panel’s own search without skipping anything', async () => {
    const { wrapper, panels } = await newWrapper('ssc-query-string="logCampaignCheck=true"');

    await wrapper.handleLoadData(response, panels['vehicle-ssc']);

    expect(panels['vehicle-ssc'].fetchVin).not.toHaveBeenCalled();
    expect(panels['vehicle-ssc'].skipLookup).not.toHaveBeenCalled();
    expect(panels['vehicle-specification'].fetchVin).toHaveBeenCalledWith(response);
  });

  it('hydrates every panel from an injected response, which no tab made', async () => {
    const { wrapper, panels } = await newWrapper('ssc-query-string="logCampaignCheck=true"');

    await wrapper.handleLoadData(response, null);

    expect(panels['vehicle-ssc'].fetchVin).toHaveBeenCalledWith(response);
    expect(panels['vehicle-ssc'].skipLookup).not.toHaveBeenCalled();
  });
});
