import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage, SpecPage } from '@stencil/core/testing';

import { VehicleSsc } from './vehicle-ssc';

import * as revealBelowModule from '~lib/reveal-below';

import sscLocale from '../../locales/vehicleLookup/ssc/en.json';
import vehicleLookupMocks from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';

/**
 * The whole panel driven the way a host drives it — `setMockData` then `fetchVin`, `skipLookup` —
 * against the generated fixtures. `SscCampaigns.spec.tsx` pins what the panel may assert; these
 * pin the flows around it: the manufacturer check for an unauthorized vehicle uses the stand-in
 * widget in development (never Google's), its answer replaces the prompt, the next lookup resets
 * it, and a skipped lookup can be run from the panel itself.
 *
 * Lookups run on real timers (the shared lookup helper waits a second before it answers, the
 * development stand-in for the manufacturer takes three), so the tests that ask the manufacturer
 * carry their own timeout.
 */

/** A vehicle the distributor has no records for: `isAuthorized: false`, no campaign list. */
const UNKNOWN_VIN = 'UNKNOWN_VIN_12345';
/** Authorized, in the records, and clear: no campaign affects it. */
const CLEAR_VIN = 'JTMW43FV10D123456';
/** Authorized with one open and one repaired campaign. */
const CAMPAIGN_VIN = 'JTMHX01J8L4198293';

const MANUFACTURER_ANSWERS = [sscLocale.recallExists, sscLocale.noRecall, sscLocale.noApplicableVehicleFound];

beforeAll(() => {
  // The component reads its locale files over the network; serve them from disk instead.
  (global as any).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

const newPage = async () => {
  const page = await newSpecPage({
    components: [VehicleSsc],
    html: '<vehicle-ssc is-dev="true" recaptcha-key="site-key"></vehicle-ssc>',
  });

  await (page.rootInstance as VehicleSsc).setMockData(vehicleLookupMocks as any);

  return page;
};

const shadow = (page: SpecPage) => page.root.shadowRoot;

const lookup = async (page: SpecPage, vin: string) => {
  await (page.rootInstance as VehicleSsc).fetchVin(vin);
  await page.waitForChanges();
};

const wait = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

const verdictOf = (page: SpecPage) => shadow(page).querySelector('.ssc-card')?.getAttribute('data-verdict');
const phaseOf = (page: SpecPage) => shadow(page).querySelector('.ssc-card')?.getAttribute('data-phase');
const badgeText = (page: SpecPage) => shadow(page).querySelector('.ssc-summary .status-badge')?.textContent?.trim();
const leadOf = (page: SpecPage) => shadow(page).querySelector('.ssc-lead')?.getAttribute('data-lead');
const noticeText = (page: SpecPage) => shadow(page).querySelector('.ssc-lead-notice')?.textContent?.trim();
const bodyOpen = (page: SpecPage) => shadow(page).querySelector('.ssc-body')?.getAttribute('data-open') === 'true';
const rows = (page: SpecPage) => shadow(page).querySelectorAll('.ssc-row');

/** Google's widget would need its script in the head and its portal in the body; neither may exist in development. */
const realWidgetTouched = (page: SpecPage) =>
  !!page.doc.querySelector('script[src*="recaptcha"]') || [...page.doc.body.children].some(child => child !== page.root && child.tagName === 'DIV');

describe('vehicle-ssc', () => {
  it('offers the manufacturer check, with the stand-in widget, and no verdict for an unauthorized vehicle', async () => {
    const page = await newPage();

    await lookup(page, UNKNOWN_VIN);

    expect(shadow(page).querySelector('.vehicle-info-header-vin')?.textContent?.trim()).toBe(UNKNOWN_VIN);
    expect(verdictOf(page)).toBe('neutral');
    expect(badgeText(page)).toBe(sscLocale.notInRecords);
    expect(leadOf(page)).toBe('notice');
    expect(noticeText(page)).toBe(sscLocale.unauthorizedCheck);
    expect(rows(page)).toHaveLength(0);
    expect(shadow(page).textContent).not.toContain(sscLocale.noCampaigns);
    // Development never shows Google's widget: the stand-in is there, the real one was never created.
    expect(bodyOpen(page)).toBe(true);
    expect(shadow(page).querySelector('.ssc-check .dev-recaptcha')).not.toBeNull();
    expect(realWidgetTouched(page)).toBe(false);
  });

  it('asks the manufacturer when the stand-in is passed and shows only its answer', async () => {
    const page = await newPage();
    await lookup(page, UNKNOWN_VIN);

    (shadow(page).querySelector('.dev-recaptcha') as HTMLElement).click();
    await page.waitForChanges();

    // In flight: the pill and the strip go to the sheen, the widget stays with its spinner under it.
    expect(phaseOf(page)).toBe('busy');
    expect(leadOf(page)).toBe('skeleton');
    expect(bodyOpen(page)).toBe(true);
    expect(shadow(page).querySelector('.ssc-checking-slot')?.getAttribute('data-open')).toBe('true');
    expect(shadow(page).querySelector('.dev-recaptcha-checkbox.checked')).not.toBeNull();

    // The development stand-in answers after three seconds.
    await wait(3300);
    await page.waitForChanges();

    expect(phaseOf(page)).toBe('settled');
    expect(shadow(page).querySelector('.ssc-checking-slot')?.getAttribute('data-open')).toBe('false');
    expect(leadOf(page)).toBe('notice');
    expect(MANUFACTURER_ANSWERS).toContain(noticeText(page));
    // The body shuts over the widget — it leaves with the body rather than vanishing — and no list ever appears.
    expect(bodyOpen(page)).toBe(false);
    expect(shadow(page).querySelector('.ssc-check .dev-recaptcha')).not.toBeNull();
    expect(rows(page)).toHaveLength(0);
    expect(['positive', 'negative', 'neutral']).toContain(verdictOf(page));
    // The distributor's own wording stays reserved for its own records.
    expect(shadow(page).textContent).not.toContain(sscLocale.noCampaigns);
  }, 15000);

  it('resets the check on the next lookup', async () => {
    const page = await newPage();
    await lookup(page, UNKNOWN_VIN);
    (shadow(page).querySelector('.dev-recaptcha') as HTMLElement).click();
    await wait(3300);
    await page.waitForChanges();
    expect(MANUFACTURER_ANSWERS).toContain(noticeText(page));

    // Another vehicle, then back: the answer belongs to the vehicle it was given for.
    await lookup(page, CLEAR_VIN);
    expect(noticeText(page)).toBe(sscLocale.noCampaigns);
    expect(shadow(page).querySelector('.dev-recaptcha')).toBeNull();

    await lookup(page, UNKNOWN_VIN);
    expect(noticeText(page)).toBe(sscLocale.unauthorizedCheck);
    expect(bodyOpen(page)).toBe(true);
    expect(shadow(page).querySelector('.dev-recaptcha')).not.toBeNull();
    expect(shadow(page).querySelector('.dev-recaptcha-checkbox.checked')).toBeNull();
    expect(verdictOf(page)).toBe('neutral');
  }, 20000);

  it('says no campaign affects an authorized vehicle only on the distributor’s own records', async () => {
    const page = await newPage();

    await lookup(page, CLEAR_VIN);

    expect(verdictOf(page)).toBe('positive');
    expect(badgeText(page)).toBe(sscLocale.noPendingCampaign);
    expect(leadOf(page)).toBe('notice');
    expect(noticeText(page)).toBe(sscLocale.noCampaigns);
    expect(rows(page)).toHaveLength(0);
    expect(bodyOpen(page)).toBe(false);
    expect(shadow(page).querySelector('.ssc-check')).toBeNull();
  });

  it('lists an authorized vehicle’s campaigns and nothing about a manufacturer check', async () => {
    const page = await newPage();

    await lookup(page, CAMPAIGN_VIN);

    expect(rows(page)).toHaveLength(2);
    expect(leadOf(page)).toBe('columns');
    expect(bodyOpen(page)).toBe(true);
    expect(verdictOf(page)).toBe('negative');
    expect(badgeText(page)).toBe(`1 ${sscLocale.open} · 1 ${sscLocale.repaired}`);
    expect(shadow(page).querySelector('.ssc-check')).toBeNull();
    expect(shadow(page).querySelector('.dev-recaptcha')).toBeNull();
  });

  it('names a skipped check for the VIN and runs it on request', async () => {
    const page = await newPage();
    const loaded = jest.fn();
    (page.root as unknown as VehicleSsc).loadedResponse = loaded;

    await (page.rootInstance as VehicleSsc).skipLookup(CAMPAIGN_VIN);
    await page.waitForChanges();

    // Not blank, not a verdict: the VIN is named and the panel says the check was not run.
    expect(shadow(page).querySelector('.vehicle-info-header-vin')?.textContent?.trim()).toBe(CAMPAIGN_VIN);
    expect(verdictOf(page)).toBe('attention');
    expect(badgeText(page)).toBe(sscLocale.skipped);
    expect(noticeText(page)).toBe(sscLocale.skippedNotice);
    expect(rows(page)).toHaveLength(0);
    expect(bodyOpen(page)).toBe(true);
    expect(shadow(page).textContent).not.toContain(sscLocale.noCampaigns);

    (shadow(page).querySelector('.ssc-run-button') as HTMLElement).click();
    await page.waitForChanges();
    // The panel's own lookup, in flight: the sheen is on and the action slides away with the body.
    expect(phaseOf(page)).toBe('busy');
    expect(bodyOpen(page)).toBe(false);
    expect(shadow(page).querySelector('.ssc-run-button')).not.toBeNull();

    await wait(1300);
    await page.waitForChanges();

    expect(rows(page)).toHaveLength(2);
    expect(leadOf(page)).toBe('columns');
    expect(shadow(page).querySelector('.ssc-run-button')).toBeNull();
    // A real lookup, so the wrapper hears about it and can hydrate the other panels.
    expect(loaded).toHaveBeenCalledTimes(1);
    expect(loaded.mock.calls[0][0]?.vin).toBe(CAMPAIGN_VIN);
  });

  /**
   * KPI integrity. A logged lookup is a KPI entry, and a wrapper hands its logging flag to this panel
   * as `lookup-query-string`. The lookup request carries it; the trace request — a re-read of a lookup
   * that was already logged — must never carry it, or every opened evidence drawer counts as a lookup.
   */
  it('sends the lookup-only query string with the lookup and never with the trace', async () => {
    const requested: string[] = [];
    const localeFetch = (global as any).fetch;
    (global as any).fetch = (url: string, init?: RequestInit) => {
      if (url.includes('locales/')) return localeFetch(url, init);
      requested.push(url);
      const body = url.includes('trace=ssc')
        ? vehicleLookupMocks[CAMPAIGN_VIN]
        : { ...vehicleLookupMocks[CAMPAIGN_VIN], ssc: vehicleLookupMocks[CAMPAIGN_VIN].ssc.map(item => ({ ...item, trace: undefined })) };
      return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
    };

    try {
      const page = await newSpecPage({
        components: [VehicleSsc],
        html: '<vehicle-ssc base-url="https://lookup.example/api/" query-string="lang=en" lookup-query-string="logLookup=true" show-trace="true" disable-vin-validation="true"></vehicle-ssc>',
      });

      await (page.rootInstance as VehicleSsc).fetchVin(CAMPAIGN_VIN);
      await page.waitForChanges();
      expect(requested).toEqual([`https://lookup.example/api/${CAMPAIGN_VIN}?lang=en&logLookup=true`]);

      (shadow(page).querySelector('.ssc-trace-button') as HTMLElement).click();
      await page.waitForChanges();
      await wait(50);
      await page.waitForChanges();

      expect(requested).toHaveLength(2);
      expect(requested[1]).toBe(`https://lookup.example/api/${CAMPAIGN_VIN}?lang=en&trace=ssc`);
      expect(requested[1]).not.toContain('logLookup');
    } finally {
      (global as any).fetch = localeFetch;
    }
  });

  it('shuts the body over the campaigns it clears, then empties', async () => {
    const page = await newPage();
    await lookup(page, CAMPAIGN_VIN);

    const clearing = (page.rootInstance as VehicleSsc).clearData();
    await page.waitForChanges();
    expect(phaseOf(page)).toBe('busy');
    expect(bodyOpen(page)).toBe(false);
    expect(rows(page)).toHaveLength(2);

    await clearing;
    await page.waitForChanges();
    expect(verdictOf(page)).toBe('idle');
    expect(leadOf(page)).toBe('skeleton');
    expect(phaseOf(page)).toBe('settled');
  });

  /**
   * Opening a row's evidence brings the row to the top of the page and holds it there while the
   * drawer slides open beneath it, as an expanded service-history line does. The follow starts when
   * the drawer opens — for mock data, the moment the row is asked — and lets go when it shuts.
   */
  it('brings a row to the top of the page as its evidence opens, and lets go when it shuts', async () => {
    const letGo = jest.fn();
    const reveal = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => letGo);

    try {
      const page = await newSpecPage({
        components: [VehicleSsc],
        html: '<vehicle-ssc is-dev="true" show-trace="true"></vehicle-ssc>',
      });
      await (page.rootInstance as VehicleSsc).setMockData(vehicleLookupMocks as any);
      await lookup(page, CAMPAIGN_VIN);
      expect(reveal).not.toHaveBeenCalled();

      const buttons = shadow(page).querySelectorAll('.ssc-trace-button');
      (buttons[1] as HTMLElement).click();
      await page.waitForChanges();

      expect(reveal).toHaveBeenCalledTimes(1);
      const [row, duration] = reveal.mock.calls[0];
      expect(row).toBe(rows(page)[1]);
      expect(row.getAttribute('data-open')).toBe('true');
      expect(duration).toBeGreaterThan(0);
      expect(letGo).not.toHaveBeenCalled();

      (buttons[1] as HTMLElement).click();
      await page.waitForChanges();
      expect(letGo).toHaveBeenCalledTimes(1);
      expect(reveal).toHaveBeenCalledTimes(1);
    } finally {
      reveal.mockRestore();
    }
  });
});
