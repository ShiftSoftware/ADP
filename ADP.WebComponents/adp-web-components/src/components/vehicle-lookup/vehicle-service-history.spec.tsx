import { readFileSync } from 'fs';
import { join } from 'path';
import { newSpecPage } from '@stencil/core/testing';

import { SharedLocales } from '~features/multi-lingual';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import { VehicleServiceHistory } from './vehicle-service-history';
import { PAGE_SIZE_STORAGE_KEY, PanelState, listed, panelBody, panelPlaceholder, panelVerdict, serviceHistoryColumns } from './components/VehicleServiceHistoryPanel';
import serviceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';

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
  it('lists the visits of an unauthorized vehicle like the live sites, with no pill and an amber bar', () => {
    const state: PanelState = { vehicleLoaded: true, authorized: false, visits: [visit, visit] };

    expect(listed(state)).toEqual([visit, visit]);
    expect(panelBody(state)).toBe('rows');
    expect(panelVerdict(state, shared)).toEqual({ state: 'idle', text: '', accent: 'neutral' });
  });

  it('shows an unauthorized vehicle with no visits as empty, never green', () => {
    const state: PanelState = { vehicleLoaded: true, authorized: false, visits: [] };

    expect(panelVerdict(state, shared)).toEqual({ state: 'idle', text: '', accent: 'neutral' });
    expect(panelPlaceholder(state)).toBe('empty');
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

describe('vehicle-service-history — behaves like the live sites', () => {
  beforeAll(() => {
    globalThis.fetch = ((url: string) => {
      const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
      return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
    }) as unknown as typeof fetch;
  });

  const visits = Array.from({ length: 7 }, (_, index) => ({ ...visit, invoiceNumber: `INV-${index}` }));
  const record = { vin: 'ZS8TESTVIN0000001', isAuthorized: true, serviceHistory: visits } as unknown as VehicleLookupDTO;

  const mount = async (attributes = '') => {
    const page = await newSpecPage({ components: [VehicleServiceHistory], html: `<vehicle-service-history is-dev="true" ${attributes}></vehicle-service-history>` });
    await (page.rootInstance as VehicleServiceHistory).fetchVin(record);
    await page.waitForChanges();
    return page;
  };

  const rowCount = (page: Awaited<ReturnType<typeof mount>>) => page.root.shadowRoot.querySelectorAll('.lt-slot').length;
  const pagerOpen = (page: Awaited<ReturnType<typeof mount>>) => page.root.shadowRoot.querySelector('.service-history-pager').getAttribute('data-open');

  it('keeps the columns in the order the live sites show them', () => {
    expect(serviceHistoryColumns(serviceHistorySchema.getDefault()).map(column => column.field)).toEqual([
      'branchName',
      'companyName',
      'invoiceNumber',
      'serviceDate',
      'serviceType',
      'mileage',
    ]);
  });

  it('lists every visit with no pager unless pagination is turned on', async () => {
    const page = await mount();

    expect(rowCount(page)).toBe(7);
    expect(pagerOpen(page)).toBe('false');
  });

  it('pages five visits at a time when pagination is on, ignoring a size saved under the old key', async () => {
    localStorage.setItem('shift-components:vehicle-service-history:page-size', '25');
    const page = await mount('pagination="true"');

    expect(PAGE_SIZE_STORAGE_KEY).toBe('shift-components:vehicle-service-history:page-size:v2');
    expect(rowCount(page)).toBe(5);
    expect(pagerOpen(page)).toBe('true');
  });
});
