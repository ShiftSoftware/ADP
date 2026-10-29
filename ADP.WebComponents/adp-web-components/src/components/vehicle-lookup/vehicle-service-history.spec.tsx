import { readFileSync } from 'fs';
import { join } from 'path';
import { newSpecPage } from '@stencil/core/testing';

import { SharedLocales } from '~features/multi-lingual';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import { VehicleServiceHistory } from './vehicle-service-history';
import { PanelState, listed, panelBody, panelPlaceholder, panelVerdict } from './components/VehicleServiceHistoryPanel';

import englishShared from '../../locales/en.json';

const shared = englishShared as unknown as SharedLocales;

const visit = {
  serviceType: 'Periodic maintenance',
  serviceDate: '2025-03-14',
  mileage: 71828,
  companyName: 'Dealer',
  branchName: 'Branch',
  invoiceNumber: 'INV-1',
} as VehicleServiceHistoryDTO;

describe('vehicle-service-history — the honesty rule', () => {
  it('lists nothing for an unauthorized vehicle, even when the data carries visits, and is amber', () => {
    const state: PanelState = { vehicleLoaded: true, authorized: false, visits: [visit, visit] };
    const verdict = panelVerdict(state, shared);

    expect(listed(state)).toEqual([]);
    expect(panelBody(state)).toBe('none');
    expect(verdict.state).toBe('neutral');
    expect(verdict.text).toBe(shared.notInRecords);
    expect(verdict.accent).toBe('neutral');
  });

  it('greys an authorized vehicle with no visits, never green, and shows the empty placeholder', () => {
    const state: PanelState = { vehicleLoaded: true, authorized: true, visits: [] };
    const verdict = panelVerdict(state, shared);

    expect(verdict).toEqual({ state: 'idle', text: shared.noRecords, accent: 'idle' });
    expect(panelPlaceholder(state)).toBe('empty');
  });

  it('has no pill and no row before a lookup', () => {
    const state: PanelState = { vehicleLoaded: false };

    expect(panelVerdict(state, shared).text).toBe('');
    expect(listed(state)).toEqual([]);
    expect(panelPlaceholder(state)).toBe('idle');
  });

  it('is negative on a failed lookup and shows the idle placeholder', () => {
    const state: PanelState = { vehicleLoaded: false, error: 'Something went wrong' };
    const verdict = panelVerdict(state, shared);

    expect(verdict.state).toBe('negative');
    expect(verdict.accent).toBe('negative');
    expect(panelPlaceholder(state)).toBe('idle');
  });
});

describe('vehicle-service-history — the drawer', () => {
  beforeAll(() => {
    globalThis.fetch = ((url: string) => {
      const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
      return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
    }) as unknown as typeof fetch;
  });

  it('shuts an open drawer when a new lookup starts, so it cannot reopen on another visit', async () => {
    const page = await newSpecPage({ components: [VehicleServiceHistory], html: '<vehicle-service-history is-dev="true"></vehicle-service-history>' });
    const owner = page.rootInstance as VehicleServiceHistory;
    const record = (vin: string) => ({ vin, isAuthorized: true, serviceHistory: [visit, visit] }) as unknown as VehicleLookupDTO;
    const drawer = () => page.root.shadowRoot.querySelector('.lt-slot .lt-drawer');

    await owner.fetchVin(record('ZS8TESTVIN0000001'));
    await page.waitForChanges();
    page.root.shadowRoot.querySelector<HTMLButtonElement>('.lt-toggle-button').click();
    await page.waitForChanges();
    expect(drawer().getAttribute('data-open')).toBe('true');

    const next = owner.fetchVin(record('ZS8TESTVIN0000002'));
    await page.waitForChanges();
    expect(drawer().getAttribute('data-open')).toBe('false');

    await next;
    await page.waitForChanges();
    expect(drawer().getAttribute('data-open')).toBe('false');
  });
});
