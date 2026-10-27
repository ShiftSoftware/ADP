import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleServiceHistory } from './vehicle-service-history';
import * as revealBelowModule from '~lib/reveal-below';
import { ErrorKeys } from '~features/multi-lingual';

import englishLocale from '../../locales/vehicleLookup/serviceHistory/en.json';
import englishShared from '../../locales/en.json';

const labor = (count: number) =>
  Array.from({ length: count }, (_, at) => ({ laborCode: `LB-${at}`, packageCode: '', serviceCode: 'BRK', serviceDescription: `Operation ${at + 1}` }));
const parts = (count: number) => Array.from({ length: count }, (_, at) => ({ partNumber: `P-${at}`, qty: at + 1, packageCode: '', partDescription: `Part ${at + 1}` }));
const visit = (at: number, over: Record<string, unknown> = {}) => ({
  serviceType: `Service ${at}`,
  serviceDate: `2026-0${(at % 9) + 1}-15`,
  mileage: 40000 + at * 1000,
  companyName: 'Sample Dealer',
  branchName: 'Main Showroom',
  accountNumber: '',
  invoiceNumber: `INV-${100 + at}`,
  parentInvoiceNumber: '',
  jobNumber: `${7000 + at}`,
  laborLines: labor(1),
  partLines: [],
  ...over,
});

const RECORDS_VIN = 'VIN-RECORDS';
const TWELVE_VIN = 'VIN-TWELVE';
const UNAUTHORIZED_VIN = 'VIN-UNAUTHORIZED';
const EMPTY_VIN = 'VIN-EMPTY';

const fixtures: Record<string, { serviceHistory: unknown[]; isAuthorized: boolean }> = {
  [RECORDS_VIN]: {
    isAuthorized: true,
    serviceHistory: [
      visit(1, { serviceType: 'Brake service', mileage: 60412, invoiceNumber: '60110245', accountNumber: 'UD50011 ', jobNumber: '71002', laborLines: labor(2) }),
      visit(2, { invoiceNumber: '60109871', laborLines: [], partLines: parts(3) }),
      visit(3, { invoiceNumber: '60104420', laborLines: labor(2), partLines: parts(3) }),
    ],
  },
  [TWELVE_VIN]: { isAuthorized: true, serviceHistory: Array.from({ length: 12 }, (_, at) => visit(at)) },
  [UNAUTHORIZED_VIN]: { isAuthorized: false, serviceHistory: [] },
  [EMPTY_VIN]: { isAuthorized: true, serviceHistory: [] },
};
const mockData = Object.fromEntries(Object.entries(fixtures).map(([vin, vehicle]) => [vin, { vin, ...vehicle }]));

beforeAll(() => {
  (globalThis as unknown as { fetch: unknown }).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

const newPage = async (attributes = '') => {
  const page = await newSpecPage({
    components: [VehicleServiceHistory],
    html: `<vehicle-service-history is-dev="true" ${attributes}></vehicle-service-history>`,
  });
  await (page.rootInstance as VehicleServiceHistory).setMockData(mockData as unknown as Parameters<VehicleServiceHistory['setMockData']>[0]);
  await page.waitForChanges();
  return page;
};

type Page = Awaited<ReturnType<typeof newPage>>;

const owner = (page: Page) => page.rootInstance as VehicleServiceHistory;
const q = (page: Page, selector: string) => page.root.shadowRoot.querySelector(selector) as HTMLElement | null;
const qa = (page: Page, selector: string) => Array.from(page.root.shadowRoot.querySelectorAll(selector)) as HTMLElement[];
const squash = (value?: string | null) => value?.replace(/\s+/g, ' ').trim();

const settle = async (page: Page, ms = 700) => {
  await new Promise(resolve => setTimeout(resolve, ms));
  await page.waitForChanges();
};

const load = async (page: Page, vin: string) => {
  await owner(page).fetchVin(vin);
  await page.waitForChanges();
  await settle(page);
};

const LABEL: Record<string, string> = {
  branchName: englishLocale.branch,
  companyName: englishLocale.dealer,
  invoiceNumber: englishLocale.invoiceNumber,
  jobNumber: englishLocale.jobNumber,
  serviceType: englishLocale.serviceType,
  mileage: englishLocale.odometer,
};
const rowsOf = (page: Page) => qa(page, '.lt-slot[data-open="true"] > .lt-slot-body > .lt-row');
const cellText = (row: HTMLElement, field: string) => squash(row.querySelector(`[data-label="${LABEL[field]}"] .lt-value`)?.textContent);
const invoicesOf = (page: Page) => qa(page, '.lt-slot > .lt-slot-body > .lt-row').map(row => cellText(row, 'invoiceNumber'));
const buttons = (page: Page) => qa(page, '.lt-slot > .lt-slot-body > .lt-row .lt-action-button');
const expanded = (page: Page) => buttons(page).map(button => button.getAttribute('aria-expanded'));
const drawers = (page: Page) => qa(page, '.lt-drawer[data-open="true"]');
const headings = (page: Page) => squash(q(page, '.lt-grid > .lt-lead .lt-columns')?.textContent);
const pageRange = (page: Page) => squash(q(page, '.lt-page-range')?.textContent);
const pagerOpen = (page: Page) => q(page, '.lt-pager').getAttribute('data-open');
const kind = (page: Page) =>
  qa(page, '.lt-placeholder-layer')
    .filter(layer => layer.getAttribute('data-active') === 'true')
    .map(layer => layer.getAttribute('data-kind'));
const words = (page: Page, which: string) => squash(q(page, `.lt-placeholder-layer.is-${which} .lt-placeholder-text`)?.textContent);
const verdict = (page: Page) => q(page, '.service-history-card').getAttribute('data-verdict');
const badge = (page: Page) => squash(q(page, '.status-badge')?.textContent);

const open = async (page: Page, index: number) => {
  buttons(page)[index].click();
  await page.waitForChanges();
};

describe('vehicle-service-history: states', () => {
  it('idle: the idle line under the headings, the body and the pager shut, no verdict', async () => {
    const page = await newPage();
    expect(kind(page)).toEqual(['idle']);
    expect(words(page, 'idle')).toBe(englishLocale.idleHint);
    expect(verdict(page)).toBe('idle');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('false');
    expect(pagerOpen(page)).toBe('false');
    expect(headings(page)).toContain(englishLocale.invoiceNumber);
    expect(headings(page)).not.toContain(englishLocale.jobNumber);
  });

  it('error: the error line and a negative pill, no rows', async () => {
    const page = await newPage();
    await owner(page).setErrorMessage('noBaseUrl' as ErrorKeys);
    await page.waitForChanges();
    await settle(page);
    expect(kind(page)).toEqual(['error']);
    expect(words(page, 'error')).toBe(englishLocale.errorHint);
    expect(verdict(page)).toBe('negative');
    expect(rowsOf(page)).toHaveLength(0);
  });

  it('empty is a grey statement and not in the records a neutral one, never a verdict and never rows', async () => {
    const page = await newPage();
    await load(page, EMPTY_VIN);
    expect(kind(page)).toEqual(['empty']);
    expect(verdict(page)).toBe('idle');
    expect(badge(page)).toBe(englishShared.noRecords);
    expect(rowsOf(page)).toHaveLength(0);

    await load(page, UNAUTHORIZED_VIN);
    expect(kind(page)).toEqual(['unauthorized']);
    expect(words(page, 'unauthorized')).toBe(englishLocale.unauthorizedHint);
    expect(verdict(page)).toBe('neutral');
    expect(badge(page)).toBe(englishShared.notInRecords);
    expect(rowsOf(page)).toHaveLength(0);
  });
});

describe('vehicle-service-history: rows and paging', () => {
  it('draws a row from its record, the job number hidden by default', async () => {
    const page = await newPage();
    await load(page, RECORDS_VIN);
    const [first] = rowsOf(page);
    expect(cellText(first, 'branchName')).toBe('Main Showroom');
    expect(cellText(first, 'companyName')).toBe('Sample Dealer');
    expect(cellText(first, 'invoiceNumber')).toBe('60110245');
    expect(cellText(first, 'serviceType')).toBe('Brake service');
    expect(cellText(first, 'mileage')).toBe('60,412');
    expect(first.querySelector(`[data-label="${englishLocale.jobNumber}"]`)).toBeNull();
  });

  it('pages 12 records as 5, 5, 2 with the range, and back; paging shuts an open drawer', async () => {
    const page = await newPage();
    await load(page, TWELVE_VIN);
    const invoices = (count: number) => invoicesOf(page).slice(0, count);
    const all = fixtures[TWELVE_VIN].serviceHistory.map(record => (record as { invoiceNumber: string }).invoiceNumber);
    const step = async (which: 'next' | 'previous') => {
      q(page, `.lt-page-button.is-${which}`).click();
      await settle(page);
    };

    expect(pagerOpen(page)).toBe('true');
    expect(pageRange(page)).toBe('1–5 of 12');
    expect(invoices(5)).toEqual(all.slice(0, 5));
    await open(page, 0);
    expect(drawers(page)).toHaveLength(1);
    await step('next');
    expect(drawers(page)).toHaveLength(0);
    expect(invoices(5)).toEqual(all.slice(5, 10));
    await step('next');
    expect(invoices(2)).toEqual(all.slice(10, 12));
    expect(pageRange(page)).toBe('11–12 of 12');
    expect(q(page, '.lt-page-button.is-next').hasAttribute('disabled')).toBe(true);
    await step('previous');
    expect(invoices(5)).toEqual(all.slice(5, 10));
  });

  it('hiddenFields: shows the job number when emptied, and drops a named column from the headings and the rows', async () => {
    const page = await newPage('hidden-fields=""');
    await load(page, RECORDS_VIN);
    expect(headings(page)).toContain(englishLocale.jobNumber);
    expect(cellText(rowsOf(page)[0], 'jobNumber')).toBe('71002');

    page.root.setAttribute('hidden-fields', 'serviceType,mileage,jobNumber');
    await page.waitForChanges();
    await settle(page, 1000);
    expect(headings(page)).not.toContain(englishLocale.serviceType);
    expect(headings(page)).not.toContain(englishLocale.jobNumber);
    expect(headings(page)).toContain(englishLocale.branch);
    expect(rowsOf(page)[0].querySelector(`[data-label="${englishLocale.serviceType}"]`)).toBeNull();
  });
});

describe('vehicle-service-history: the drawer', () => {
  const loaded = async () => {
    const page = await newPage();
    await load(page, RECORDS_VIN);
    return page;
  };

  it('opens from the row button, one at a time, and closes from the same button', async () => {
    const page = await loaded();
    const controls = buttons(page)[0].getAttribute('aria-controls');
    expect(buttons(page)[0].getAttribute('aria-label')).toBe(`${englishLocale.expandRow} 60110245`);

    await open(page, 0);
    expect(expanded(page)).toEqual(['true', 'false', 'false']);
    expect(drawers(page).map(drawer => drawer.id)).toEqual([controls]);
    expect(buttons(page)[0].getAttribute('aria-label')).toBe(`${englishLocale.collapseRow} 60110245`);

    await open(page, 2);
    expect(expanded(page)).toEqual(['false', 'false', 'true']);
    expect(drawers(page)).toHaveLength(1);

    await open(page, 2);
    expect(expanded(page)).toEqual(['false', 'false', 'false']);
    expect(drawers(page)).toHaveLength(0);
  });

  it('does not move the page when a drawer opens or shuts', async () => {
    const reveal = jest.spyOn(revealBelowModule, 'revealBelow');
    const page = await loaded();
    reveal.mockClear();
    await open(page, 0);
    await open(page, 1);
    await open(page, 1);
    expect(reveal).not.toHaveBeenCalled();
    reveal.mockRestore();
  });

  it('shows the meta line and the labor and part lines, and says when an invoice has none of a kind', async () => {
    const page = await loaded();
    await open(page, 0);
    expect(qa(page, '.service-history-meta-item').map(item => [item.getAttribute('data-field'), squash(item.textContent)])).toEqual([
      ['jobNumber', `${englishLocale.jobNumber} 71002`],
      ['accountNumber', `${englishLocale.accountNumber} UD50011`],
    ]);
    expect(squash(q(page, '.service-history-lines[data-kind="part"] .service-history-none').textContent)).toBe(englishLocale.noPartLines);

    await open(page, 2);
    await settle(page);
    const section = (which: string) => q(page, `.service-history-lines[data-kind="${which}"]`);
    expect(squash(section('labor').querySelector('.service-history-lines-count').textContent)).toBe('2');
    expect(section('labor').querySelectorAll('.lt-row')).toHaveLength(2);
    expect(section('part').querySelectorAll('.lt-row')).toHaveLength(3);

    await open(page, 1);
    await settle(page);
    expect(squash(q(page, '.service-history-lines[data-kind="labor"] .service-history-none').textContent)).toBe(englishLocale.noLaborLines);
  });

  it('stays open through a lookup that brings the same record back', async () => {
    const page = await loaded();
    await open(page, 0);

    const pending = owner(page).fetchVin(RECORDS_VIN);
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(drawers(page)).toHaveLength(1);

    await pending;
    await settle(page);
    expect(expanded(page)).toEqual(['true', 'false', 'false']);
    expect(drawers(page)).toHaveLength(1);
  });
});
