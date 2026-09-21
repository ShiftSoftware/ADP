import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleSpecification } from './vehicle-specification';

import vehicleLookupMocks from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';

/**
 * A record panel under the wrapper: the standard head is an anchor in every state, its verdict
 * follows the honesty rule, and every change of verdict is announced so a composite can colour its
 * one card from the active panel — including back to idle. Lookups run on real timers (the shared
 * helper waits a second before it answers).
 */

const AUTHORIZED_VIN = 'ZT8P9NAL1LG988010';
const UNKNOWN_VIN = 'UNKNOWN_VIN_12345';

beforeAll(() => {
  (global as any).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

const newPage = async () => {
  const page = await newSpecPage({
    components: [VehicleSpecification],
    html: '<vehicle-specification is-dev="true"></vehicle-specification>',
  });
  await (page.rootInstance as VehicleSpecification).setMockData(vehicleLookupMocks as any);
  return page;
};

const shadow = (page: Awaited<ReturnType<typeof newPage>>) => page.root.shadowRoot;

describe('vehicle-specification', () => {
  it('opens with the standard head, no pill and an idle accent, goes green on records, and announces each verdict it reaches', async () => {
    const page = await newPage();
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));

    expect(shadow(page).querySelector('.lookup-head .lookup-title')?.textContent).toBe('Vehicle Specifications');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('idle');

    // Records on file: the records are the statement — no pill — and the accent goes green: the
    // lookup succeeded and the panel holds what it asked for.
    await (page.rootInstance as VehicleSpecification).fetchVin(AUTHORIZED_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('positive');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();
    expect(shadow(page).querySelector('.lookup-head')).not.toBeNull();

    // Not in the distributor's records: amber, whatever the DTO carries.
    await (page.rootInstance as VehicleSpecification).fetchVin(UNKNOWN_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('neutral');

    // Cleared: back to idle, and said so.
    await (page.rootInstance as VehicleSpecification).fetchVin({ vin: '' } as any);
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('idle');

    // A failed lookup: no band — the accent goes negative and the pill carries the message.
    await (page.rootInstance as VehicleSpecification).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('negative');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-negative')).toBe(true);
    expect(shadow(page).querySelector('.lookup-summary .status-badge > span')?.textContent).toBe('Wrong response format');
    expect(shadow(page).querySelector('.lookup-error')).toBeNull();

    expect(heard).toEqual(['positive', 'neutral', 'idle', 'negative']);
  });
});
