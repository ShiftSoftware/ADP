import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleServiceHistory } from './vehicle-service-history';
import {
  DEFAULT_HIDDEN_FIELDS,
  FIELDS,
  PAGE_SIZE_STORAGE_KEY,
  PanelState,
  ServiceHistoryLocale,
  accountNumber,
  bodyOpen,
  columnCount,
  hasRecords,
  hideableOnly,
  historyKey,
  laborColumns,
  listed,
  metaItems,
  panelBody,
  panelLead,
  panelPlaceholder,
  partColumns,
  placeholderContent,
  realParentInvoice,
  sameColumns,
  serviceHistoryColumns,
} from './components/VehicleServiceHistoryPanel';
import * as lookupTableModule from '../../features/lookup-table/lookup-table';
import { drawerAfterLookup, drawerAfterPage, pageRows, parseHiddenFields, visibleColumns } from '~features/lookup-table';
import { recordVerdict } from '~features/vehicle-info-layout';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';
import { ErrorKeys } from '~features/multi-lingual';

import englishLocale from '../../locales/vehicleLookup/serviceHistory/en.json';
import englishShared from '../../locales/en.json';

import edgeCaseMocks from '../../features/mocks/data/generated/edge-cases/vehicle-lookup.json';

/**
 * The service history panel, in two halves, as accessories and the other record panels are tested:
 * the pure derivations the panel exports (state -> lead, placeholder, verdict; columns; meta line;
 * row key; the drawer through a lookup and a page), and the owner through the DOM with the edge-case
 * mocks (ZS8KD4RT*).
 *
 * Not tested, because it does not exist: a wrapper has no skip or clear API on this panel (only the
 * SSC has `skipLookup`), so the skipped and cleared states are not reachable here. The leave runs for
 * an error raised on a settled card, which is covered below.
 */

const LOCALE = { ...englishLocale, sharedLocales: englishShared } as unknown as ServiceHistoryLocale;

type Mocks = Record<string, { serviceHistory?: VehicleServiceHistoryDTO[]; isAuthorized?: boolean }>;
const mocks = edgeCaseMocks as unknown as Mocks;

// What the fixtures are, by VIN (edge-cases/vehicle-lookup.json).
const FIVE_RECORDS_VIN = 'ZS8KD4RTXT5100001'; // 5 records: a labor-only, a part-only, a parent invoice, "0" parent, a padded account number
const NULLS_VIN = 'ZS8KD4RT1T5100002'; // 5 records with null service type, mileage, branch, invoice, company
const TWELVE_VIN = 'ZS8KD4RT5T5100004'; // 12 records
const EXACT_FIVE_VIN = 'ZS8KD4RT7T5100005'; // exactly one page at the default size
const EXACT_TEN_VIN = 'ZS8KD4RT9T5100006'; // exactly two pages of 5
const LONG_LINES_VIN = 'ZS8KD4RT0T5100007'; // a record with 14 labor and 12 part lines
const UNAUTHORIZED_VIN = 'ZS8KD4RT2T5100008';
const EMPTY_VIN = 'ZW8UWF8J4TJ368365'; // authorized, an empty list

const history = (vin: string) => mocks[vin].serviceHistory as VehicleServiceHistoryDTO[];

const row = (over: Partial<VehicleServiceHistoryDTO> = {}): VehicleServiceHistoryDTO => ({ invoiceNumber: 'INV-1', ...over }) as VehicleServiceHistoryDTO;

beforeAll(() => {
  (globalThis as unknown as { fetch: unknown }).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

// ---------------------------------------------------------------------------------------------
// The derivations
// ---------------------------------------------------------------------------------------------

describe('vehicle-service-history — state to lead, placeholder and verdict', () => {
  const state = (over: Partial<PanelState> = {}): PanelState => ({
    vehicleLoaded: true,
    authorized: true,
    history: [row()],
    hidden: parseHiddenFields(DEFAULT_HIDDEN_FIELDS),
    ...over,
  });

  const verdictOf = (s: PanelState) =>
    recordVerdict({ locale: englishShared as never, error: s.error, vehicleLoaded: s.vehicleLoaded, authorized: s.authorized, hasRecords: hasRecords(s) });

  it('idle: no vehicle, nothing asserted: the headings on the strip, the body shut, the idle placeholder, an idle pill without words', () => {
    const s = state({ vehicleLoaded: false, authorized: undefined, history: undefined });
    expect(panelLead(s)).toBe('columns');
    expect(panelBody(s)).toBe('none');
    expect(panelPlaceholder(s)).toBe('idle');
    expect(verdictOf(s)).toMatchObject({ state: 'idle', text: '', accent: 'idle' });
    expect(bodyOpen(s, false, false)).toBe(false);
  });

  it('loading from idle: the state is still the idle one (the lookup has not answered), so the idle words stay in place under the skeleton', () => {
    // The owner's `busy` is not in PanelState: a lookup in flight does not change what the panel asserts.
    const s = state({ vehicleLoaded: false, authorized: undefined, history: undefined });
    expect(panelLead(s)).toBe('columns');
    expect(panelPlaceholder(s, false)).toBe('idle');
  });

  it('loading from a prior outcome: the prior answer holds (rows stay open and the headings stay; empty and not-in-records keep their words)', () => {
    expect(panelBody(state())).toBe('rows');
    expect(bodyOpen(state(), false, false)).toBe(true);
    expect(panelPlaceholder(state())).toBeNull();
    expect(panelLead(state())).toBe('columns');

    expect(panelPlaceholder(state({ history: [] }))).toBe('empty');
    expect(panelPlaceholder(state({ authorized: false }))).toBe('unauthorized');
    expect(panelPlaceholder(state({ vehicleLoaded: false, history: undefined, error: 'x' }))).toBe('error');
  });

  it('loaded: rows, no placeholder, an idle pill with no words and a positive accent', () => {
    const s = state();
    expect(listed(s)).toHaveLength(1);
    expect(hasRecords(s)).toBe(true);
    expect(panelBody(s)).toBe('rows');
    expect(panelPlaceholder(s)).toBeNull();
    expect(verdictOf(s)).toMatchObject({ state: 'idle', text: '', accent: 'positive' });
  });

  it('empty (an empty or a null list): the empty placeholder, the body shut, a grey statement and never a verdict', () => {
    for (const history of [[], null, undefined]) {
      const s = state({ history });
      expect(hasRecords(s)).toBe(false);
      expect(panelBody(s)).toBe('none');
      expect(panelPlaceholder(s)).toBe('empty');
      expect(panelLead(s)).toBe('columns');
      expect(verdictOf(s)).toMatchObject({ state: 'idle', accent: 'idle', text: englishShared.noRecords });
    }
  });

  it('error: the error placeholder and a negative pill carrying the sentence, whatever else the state holds', () => {
    const s = state({ vehicleLoaded: false, history: undefined, error: 'The lookup failed' });
    expect(panelPlaceholder(s)).toBe('error');
    expect(panelBody(s)).toBe('none');
    expect(verdictOf(s)).toMatchObject({ state: 'negative', text: 'The lookup failed', accent: 'negative' });
  });

  it('not in the records: the unauthorized placeholder and a neutral pill, never the list even if the DTO carries one (a data fault)', () => {
    const s = state({ authorized: false });
    expect(listed(s)).toEqual([]);
    expect(hasRecords(s)).toBe(false);
    expect(panelBody(s)).toBe('none');
    expect(panelPlaceholder(s)).toBe('unauthorized');
    expect(verdictOf(s)).toMatchObject({ state: 'neutral', text: englishShared.notInRecords, accent: 'neutral' });
    expect(panelPlaceholder(state({ authorized: false, history: [] }))).toBe('unauthorized');
  });

  it('a vehicle that failed has no list even when a previous one is still held', () => {
    expect(listed(state({ vehicleLoaded: false }))).toEqual([]);
  });

  it('leaving opens the idle words over rows that are shutting, and keeps the kind of an outcome without rows', () => {
    expect(panelPlaceholder(state(), true)).toBe('idle');
    expect(panelPlaceholder(state({ history: [] }), true)).toBe('empty');
    expect(panelPlaceholder(state({ authorized: false }), true)).toBe('unauthorized');
    expect(bodyOpen(state(), true, false)).toBe(false);
  });

  it('the body is shut while the columns swap (reconfiguring), and open otherwise', () => {
    expect(bodyOpen(state(), false, true)).toBe(false);
    expect(bodyOpen(state(), false, false)).toBe(true);
  });

  it('the host hiding every optional column still leaves the invoice, the date and the button: never a notice, never a blank body', () => {
    const s = state({ hidden: new Set(FIELDS) });
    expect(columnCount(s.hidden)).toBe(3);
    expect(panelLead(s)).toBe('columns');
    expect(panelBody(s)).toBe('rows');
  });

  it('gives every outcome its own glyph and its own line from the locale', () => {
    const content = placeholderContent(LOCALE);
    expect(new Set(Object.values(content).map(item => item.glyph)).size).toBe(4);
    expect(content.idle.text).toBe(englishLocale.idleHint);
    expect(content.empty.text).toBe(englishLocale.emptyHint);
    expect(content.error.text).toBe(englishLocale.errorHint);
    expect(content.unauthorized.text).toBe(englishLocale.unauthorizedHint);
  });

  it('keeps the placeholder lines honest: idle asserts nothing, unauthorized says not-in-records and never "none", empty is about the records', () => {
    expect(englishLocale.idleHint).not.toMatch(/\bno\b|\bnone\b|not in/i);
    expect(englishLocale.unauthorizedHint).toMatch(/not in the distributor's records/);
    expect(englishLocale.unauthorizedHint).not.toMatch(/\bno service history\b|\bnone\b/i);
    expect(englishLocale.emptyHint).toMatch(/on record/);
  });
});

describe('vehicle-service-history — the columns and hiddenFields', () => {
  const fieldsFor = (hidden: string) => visibleColumns(serviceHistoryColumns(LOCALE), parseHiddenFields(hidden)).map(column => column.field);

  it('hides the job number by default and shows everything else, in column order, the expand button last', () => {
    expect(DEFAULT_HIDDEN_FIELDS).toBe('jobNumber');
    expect(fieldsFor(DEFAULT_HIDDEN_FIELDS)).toEqual(['branchName', 'companyName', 'invoiceNumber', 'serviceDate', 'serviceType', 'mileage', 'details']);
  });

  it('shows the job number, between the invoice and the date, when the host empties hiddenFields', () => {
    expect(fieldsFor('')).toEqual(['branchName', 'companyName', 'invoiceNumber', 'jobNumber', 'serviceDate', 'serviceType', 'mileage', 'details']);
  });

  it.each(FIELDS.filter(field => field !== 'jobNumber'))('hides %s and nothing else when it is named (the job number then stays hidden only if named)', field => {
    const all = fieldsFor('');
    expect(fieldsFor(field)).toEqual(all.filter(name => name !== field));
    expect(fieldsFor(`${field},jobNumber`)).toEqual(all.filter(name => name !== field && name !== 'jobNumber'));
  });

  it('hides jobNumber alone when named alone, and tolerates spaces and unknown names', () => {
    expect(fieldsFor(' serviceType , nonsense ')).toEqual(fieldsFor('serviceType'));
  });

  it('describes the columns: the invoice is the identity, the service type grows, the odometer is a number, the button is an action', () => {
    const columns = serviceHistoryColumns(LOCALE);
    const byField = (field: string) => columns.find(column => column.field === field);
    expect(byField('invoiceNumber')).toMatchObject({ kind: 'code', identity: true });
    expect(byField('serviceType')).toMatchObject({ kind: 'text', grow: true });
    expect(byField('mileage')).toMatchObject({ kind: 'number' });
    expect(byField('serviceDate')).toMatchObject({ kind: 'date' });
    expect(byField('details')).toMatchObject({ kind: 'action' });
  });

  it('labels the columns from the locale', () => {
    const labels = serviceHistoryColumns(LOCALE).map(column => column.label);
    expect(labels).toEqual([
      englishLocale.branch,
      englishLocale.dealer,
      englishLocale.invoiceNumber,
      englishLocale.jobNumber,
      englishLocale.date,
      englishLocale.serviceType,
      englishLocale.odometer,
      englishLocale.details,
    ]);
  });

  it('counts the columns a hidden set leaves, and ignores names it does not know', () => {
    expect(columnCount(new Set())).toBe(FIELDS.length + 3);
    expect(columnCount(parseHiddenFields(DEFAULT_HIDDEN_FIELDS))).toBe(FIELDS.length + 2);
    expect(columnCount(new Set(['nonsense', 'details']))).toBe(FIELDS.length + 3);
    expect(columnCount(new Set(FIELDS))).toBe(3);
  });

  it('knows a layout change from an unknown name changing', () => {
    expect(sameColumns(new Set(['jobNumber']), new Set(['jobNumber', 'nonsense']))).toBe(true);
    expect(sameColumns(new Set(['jobNumber']), new Set(['mileage']))).toBe(false);
    expect(sameColumns(new Set(), new Set(['serviceType']))).toBe(false);
  });

  it('describes the drawer tables: the line code names the row; package and service codes, and a part description, can be dropped by the table', () => {
    expect(laborColumns(LOCALE).map(column => column.field)).toEqual(['laborCode', 'serviceDescription', 'packageCode', 'serviceCode']);
    expect(partColumns(LOCALE).map(column => column.field)).toEqual(['partNumber', 'partDescription', 'packageCode', 'qty']);
    expect(laborColumns(LOCALE)[0]).toMatchObject({ identity: true });
    expect(partColumns(LOCALE)[0]).toMatchObject({ identity: true });
  });

  it('keeps its own page-size storage key', () => {
    expect(PAGE_SIZE_STORAGE_KEY).toBe('shift-components:vehicle-service-history:page-size');
  });
});

describe('vehicle-service-history — hideableOnly', () => {
  it('keeps only the optional field names and drops unknown and fixed ones', () => {
    expect([...hideableOnly(new Set(['invoiceNumber', 'serviceDate', 'details', 'nope', 'mileage', 'branchName']))].sort()).toEqual(['branchName', 'mileage']);
    expect(hideableOnly(new Set(['invoiceNumber', 'serviceDate', 'details'])).size).toBe(0);
  });
});

describe('vehicle-service-history — the drawer meta line', () => {
  const keys = (r: VehicleServiceHistoryDTO) => metaItems(r, LOCALE).map(item => item.key);

  it('lists only the items that have a value, in order: job number, original invoice, account number', () => {
    expect(metaItems(row({ jobNumber: '71002', parentInvoiceNumber: '60097732', accountNumber: 'UD50011' }), LOCALE)).toEqual([
      { key: 'jobNumber', label: englishLocale.jobNumber, value: '71002' },
      { key: 'parentInvoice', label: englishLocale.parentInvoice, value: '60097732' },
      { key: 'accountNumber', label: englishLocale.accountNumber, value: 'UD50011' },
    ]);
    expect(keys(row({ jobNumber: '71002' }))).toEqual(['jobNumber']);
    expect(keys(row({ accountNumber: 'UD50011' }))).toEqual(['accountNumber']);
    expect(keys(row({ jobNumber: '71002', accountNumber: 'UD50011' }))).toEqual(['jobNumber', 'accountNumber']);
  });

  it('has no items for a record with none of them (the drawer then has no meta line)', () => {
    expect(metaItems(row(), LOCALE)).toEqual([]);
    expect(metaItems(row({ jobNumber: '', parentInvoiceNumber: '', accountNumber: '' }), LOCALE)).toEqual([]);
  });

  it('treats a parent invoice of "0", empty, blank or null as none, and shows a real one', () => {
    for (const parentInvoiceNumber of ['0', '', '   ', null, undefined, ' 0 ']) {
      expect(realParentInvoice(row({ parentInvoiceNumber }))).toBeNull();
      expect(keys(row({ jobNumber: '1', parentInvoiceNumber }))).not.toContain('parentInvoice');
    }
    expect(realParentInvoice(row({ parentInvoiceNumber: '60097732' }))).toBe('60097732');
    expect(realParentInvoice(row({ parentInvoiceNumber: ' 60097732 ' }))).toBe('60097732');
    expect(realParentInvoice(row({ parentInvoiceNumber: '00' }))).toBe('00');
  });

  it('trims the account number, and a whitespace-only one is none', () => {
    expect(accountNumber(row({ accountNumber: 'UD50011 ' }))).toBe('UD50011');
    expect(accountNumber(row({ accountNumber: '   ' }))).toBeNull();
    expect(accountNumber(row({ accountNumber: null }))).toBeNull();
    expect(accountNumber(row())).toBeNull();
    expect(keys(row({ jobNumber: '1', accountNumber: '  ' }))).toEqual(['jobNumber']);
  });

  it('reads the fixtures: the credit note shows its real parent, the "0" one does not, the padded account is trimmed', () => {
    const [padded, , , credit, zero] = history(FIVE_RECORDS_VIN);
    expect(metaItems(padded, LOCALE).find(item => item.key === 'accountNumber')?.value).toBe('UD50011');
    expect(keys(credit)).toEqual(['jobNumber', 'parentInvoice']);
    expect(keys(zero)).toEqual(['jobNumber', 'accountNumber']);
  });
});

describe('vehicle-service-history — the row key', () => {
  it('is company + branch + invoice + job, so two branches or dealers with the same invoice number are two rows', () => {
    const a = row({ companyName: 'Sample Dealer', branchName: 'Main Showroom', invoiceNumber: '1', jobNumber: '9' });
    expect(historyKey(a)).toBe('Sample Dealer|Main Showroom|1|9');
    expect(historyKey({ ...a, branchName: 'Service Annex' })).not.toBe(historyKey(a));
    expect(historyKey({ ...a, companyName: 'Sample Distributor' })).not.toBe(historyKey(a));
    expect(historyKey({ ...a, jobNumber: '10' })).not.toBe(historyKey(a));
  });

  it('is stable and trims, so a padded copy of a value is the same record', () => {
    expect(historyKey(row({ companyName: ' A ', branchName: 'B ', invoiceNumber: ' 1', jobNumber: '2' }))).toBe(
      historyKey(row({ companyName: 'A', branchName: 'B', invoiceNumber: '1', jobNumber: '2' })),
    );
  });

  it('is unique for every record of every fixture vehicle, including those with a null branch, invoice or company', () => {
    for (const vin of [FIVE_RECORDS_VIN, NULLS_VIN, TWELVE_VIN, EXACT_FIVE_VIN, EXACT_TEN_VIN, LONG_LINES_VIN]) {
      const keys = history(vin).map(historyKey);
      expect([vin, new Set(keys).size]).toEqual([vin, keys.length]);
    }
  });

  it('gives a record made of nulls a key, and tells two that differ only in a null apart by what they do have', () => {
    expect(historyKey(row({ invoiceNumber: null, branchName: null, companyName: null, jobNumber: null }))).toBe('|||');
    const withBranch = row({ invoiceNumber: null, branchName: 'Service Annex', companyName: 'Sample Dealer', jobNumber: '72004' });
    const without = row({ invoiceNumber: null, branchName: null, companyName: 'Sample Dealer', jobNumber: '72004' });
    expect(historyKey(withBranch)).not.toBe(historyKey(without));
    expect(historyKey(row({ jobNumber: '1', invoiceNumber: null }))).not.toBe(historyKey(row({ jobNumber: '2', invoiceNumber: null })));
  });
});

describe('vehicle-service-history — the drawer through a lookup and a page, as the owner wires the table helpers', () => {
  const rows = history(TWELVE_VIN);
  const firstPage = pageRows(rows, 0, 5);

  it('stays open through a lookup when the same key comes back on the first page', () => {
    const open = historyKey(rows[1]);
    expect(drawerAfterLookup(open, firstPage, historyKey)).toBe(open);
    expect(drawerAfterLookup(open, [...firstPage].reverse(), historyKey)).toBe(open);
  });

  it('closes when the key is gone from the answer, or is in the answer but not on the first page that is shown', () => {
    expect(drawerAfterLookup('Other||9|1', firstPage, historyKey)).toBeNull();
    expect(drawerAfterLookup(historyKey(rows[9]), firstPage, historyKey)).toBeNull();
    expect(drawerAfterLookup(historyKey(rows[1]), [], historyKey)).toBeNull();
  });

  it('has nothing to keep when nothing was open', () => {
    expect(drawerAfterLookup(null, firstPage, historyKey)).toBeNull();
    expect(drawerAfterPage(null, firstPage.map(historyKey))).toBeNull();
  });

  it('paging away closes it; a page size that keeps its row on the page leaves it open', () => {
    const open = historyKey(rows[1]);
    expect(drawerAfterPage(open, pageRows(rows, 1, 5).map(historyKey))).toBeNull();
    expect(drawerAfterPage(open, pageRows(rows, 0, 10).map(historyKey))).toBe(open);
    expect(drawerAfterPage(open, pageRows(rows, 0, 'all').map(historyKey))).toBe(open);
  });

  it('pages the 12-record vehicle as 5, 5, 2, and the exact-page vehicles as 1 and 2 pages', () => {
    expect([0, 1, 2].map(page => pageRows(rows, page, 5).length)).toEqual([5, 5, 2]);
    expect(pageRows(rows, 0, 'all')).toHaveLength(12);
    expect(pageRows(history(EXACT_FIVE_VIN), 0, 5)).toHaveLength(5);
    expect(pageRows(history(EXACT_TEN_VIN), 1, 5)).toHaveLength(5);
  });
});

// ---------------------------------------------------------------------------------------------
// The owner through the DOM
// ---------------------------------------------------------------------------------------------

const newPage = async (attributes = '') => {
  const page = await newSpecPage({
    components: [VehicleServiceHistory],
    html: `<vehicle-service-history is-dev="true" ${attributes}></vehicle-service-history>`,
  });
  await (page.rootInstance as VehicleServiceHistory).setMockData(edgeCaseMocks as unknown as Parameters<VehicleServiceHistory['setMockData']>[0]);
  await page.waitForChanges();
  return page;
};

type Page = Awaited<ReturnType<typeof newPage>>;

const owner = (page: Page) => page.rootInstance as VehicleServiceHistory;
const shadow = (page: Page) => page.root.shadowRoot;
const q = (page: Page, selector: string) => shadow(page).querySelector(selector) as HTMLElement | null;
const qa = (page: Page, selector: string) => Array.from(shadow(page).querySelectorAll(selector)) as HTMLElement[];
const squash = (value?: string | null) => value?.replace(/\s+/g, ' ').trim();

const load = async (page: Page, vin: string) => {
  await owner(page).fetchVin(vin);
  await page.waitForChanges();
};

const settle = async (page: Page, ms = 700) => {
  await new Promise(resolve => setTimeout(resolve, ms));
  await page.waitForChanges();
};

/** The rows of the main table: one per open slot, never the embedded tables' rows. */
const rowsOf = (page: Page) => qa(page, '.lt-slot[data-open="true"] > .lt-slot-body > .lt-row');
const rowCount = (page: Page) => rowsOf(page).length;
/** Every slot in the document, open or arriving: rows a page adds mount shut and open a frame later (requestAnimationFrame, which the spec DOM does not run). */
const slotCount = (page: Page) => qa(page, '.lt-slot').length;
const invoicesOf = (page: Page) => qa(page, '.lt-slot > .lt-slot-body > .lt-row').map(rowEl => cellText(rowEl, 'invoiceNumber'));
const buttons = (page: Page) => qa(page, '.lt-slot > .lt-slot-body > .lt-row .lt-action-button');
const cellText = (rowEl: HTMLElement, field: string) => squash(rowEl.querySelector(`[data-label="${LABEL[field]}"] .lt-value`)?.textContent);
const LABEL: Record<string, string> = {
  branchName: englishLocale.branch,
  companyName: englishLocale.dealer,
  invoiceNumber: englishLocale.invoiceNumber,
  jobNumber: englishLocale.jobNumber,
  serviceDate: englishLocale.date,
  serviceType: englishLocale.serviceType,
  mileage: englishLocale.odometer,
};
const headings = (page: Page) => squash(q(page, '.lt-grid > .lt-lead .lt-columns')?.textContent);
const drawers = (page: Page) => qa(page, '.lt-drawer[data-open="true"]');
const pageRange = (page: Page) => squash(q(page, '.lt-page-range')?.textContent);
const pagerOpen = (page: Page) => q(page, '.lt-pager').getAttribute('data-open');
const next = async (page: Page) => {
  q(page, '.lt-page-button.is-next').click();
  await settle(page);
};
const previous = async (page: Page) => {
  q(page, '.lt-page-button.is-previous').click();
  await settle(page);
};
const chooseSize = async (page: Page, size: string) => {
  const select = q(page, '.lt-page-size-select') as HTMLSelectElement;
  // mock-doc has no `value` on a select: give the handler what a browser would.
  Object.defineProperty(select, 'value', { value: size, configurable: true });
  select.dispatchEvent(new (page.win as unknown as { Event: typeof Event }).Event('change'));
  await settle(page);
};

const active = (page: Page) => qa(page, '.lt-placeholder-layer').filter(layer => layer.getAttribute('data-active') === 'true');
const kind = (page: Page) => active(page).map(layer => layer.getAttribute('data-kind'));
const words = (page: Page, which: string) => squash(q(page, `.lt-placeholder-layer.is-${which} .lt-placeholder-text`)?.textContent);

describe('vehicle-service-history — the owner: states', () => {
  it('idle: the idle line, the headings, the body and the pager shut, no verdict words', async () => {
    const page = await newPage();
    expect(kind(page)).toEqual(['idle']);
    expect(words(page, 'idle')).toBe(englishLocale.idleHint);
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(false);
    expect(q(page, '.service-history-card').getAttribute('data-verdict')).toBe('idle');
    expect(q(page, '.service-history-card').getAttribute('data-lead')).toBe('columns');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('false');
    expect(pagerOpen(page)).toBe('false');
    expect(rowCount(page)).toBe(0);
    expect(headings(page)).toContain(englishLocale.invoiceNumber);
    expect(headings(page)).not.toContain(englishLocale.jobNumber);
  });

  it('shows all four placeholder lines in English, one per outcome, in the one region', async () => {
    const page = await newPage();
    expect(qa(page, '.lt-placeholder-layer').map(layer => squash(layer.querySelector('.lt-placeholder-text')?.textContent))).toEqual([
      englishLocale.idleHint,
      englishLocale.emptyHint,
      englishLocale.errorHint,
      englishLocale.unauthorizedHint,
    ]);
  });

  it('error: the error line, a negative pill with the shared sentence, nothing in the body; error from idle: there is nothing to leave, the card goes straight to the error', async () => {
    const page = await newPage();
    await owner(page).setErrorMessage('noBaseUrl' as ErrorKeys);
    await page.waitForChanges();
    await settle(page);
    expect(kind(page)).toEqual(['error']);
    expect(words(page, 'error')).toBe(englishLocale.errorHint);
    expect(q(page, '.service-history-card').getAttribute('data-verdict')).toBe('negative');
    expect(squash(q(page, '.status-badge')?.textContent)?.length).toBeGreaterThan(0);
    expect(rowCount(page)).toBe(0);
    expect(pagerOpen(page)).toBe('false');
  });

  it('rows -> error: the rows leave first (the panel is leaving, under the skeleton), then only the error line is exposed', async () => {
    const page = await newPage();
    await load(page, FIVE_RECORDS_VIN);
    await settle(page);

    const failing = owner(page).setErrorMessage('noBaseUrl' as ErrorKeys);
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('false');

    await failing;
    await settle(page);
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(false);
    expect(kind(page)).toEqual(['error']);
    expect(rowCount(page)).toBe(0);
  });

  it('empty: the empty line in grey, never as a verdict, no rows, no pager', async () => {
    const page = await newPage();
    await load(page, EMPTY_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['empty']);
    expect(words(page, 'empty')).toBe(englishLocale.emptyHint);
    expect(q(page, '.service-history-card').getAttribute('data-verdict')).toBe('idle');
    expect(squash(q(page, '.status-badge')?.textContent)).toBe(englishShared.noRecords);
    expect(rowCount(page)).toBe(0);
    expect(pagerOpen(page)).toBe('false');
  });

  it('not in the records: the not-in-records line and a neutral pill, never "none" and never rows', async () => {
    const page = await newPage();
    await load(page, UNAUTHORIZED_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['unauthorized']);
    expect(words(page, 'unauthorized')).toBe(englishLocale.unauthorizedHint);
    expect(q(page, '.service-history-card').getAttribute('data-verdict')).toBe('neutral');
    expect(squash(q(page, '.status-badge')?.textContent)).toBe(englishShared.notInRecords);
    expect(rowCount(page)).toBe(0);
  });

  it('loading from idle keeps the idle line under the skeleton, then rows replace it', async () => {
    const page = await newPage();
    const pending = owner(page).fetchVin(FIVE_RECORDS_VIN);
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(kind(page)).toEqual(['idle']);
    expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('true');

    await pending;
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(false);
    expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('false');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('true');
    expect(q(page, '.service-history-card').getAttribute('data-verdict')).toBe('idle');
    await settle(page);
    expect(kind(page)).toEqual([]);
  });

  it('lookup mid-leave: the previous rows stay in place under the skeleton, `.loading` on the panel wrapper, the pager busy', async () => {
    const page = await newPage();
    await load(page, FIVE_RECORDS_VIN);
    await settle(page);
    const before = rowsOf(page).map(rowEl => cellText(rowEl, 'invoiceNumber'));
    expect(before).toHaveLength(5);

    const pending = owner(page).fetchVin(NULLS_VIN);
    await page.waitForChanges();

    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(q(page, '.service-history-card').getAttribute('data-phase')).toBe('busy');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('true');
    expect(kind(page)).toEqual([]);
    expect(rowsOf(page).map(rowEl => cellText(rowEl, 'invoiceNumber'))).toEqual(before);
    expect(rowsOf(page).every(rowEl => rowEl.classList.contains('loading'))).toBe(true);
    expect(q(page, '.lt-grid').getAttribute('data-phase')).toBe('busy');

    await pending;
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(false);
    expect(rowsOf(page).some(rowEl => rowEl.classList.contains('loading'))).toBe(false);
    expect(rowsOf(page).map(rowEl => cellText(rowEl, 'invoiceNumber'))).not.toEqual(before);
  });

  it('rows -> empty and rows -> not in the records: the answer shuts the body and the placeholder takes its place', async () => {
    const page = await newPage();
    await load(page, FIVE_RECORDS_VIN);
    await settle(page);

    await load(page, EMPTY_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['empty']);
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('false');
    expect(rowCount(page)).toBe(0);

    await load(page, UNAUTHORIZED_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['unauthorized']);
  });

  it('keeps the strip on the headings for every outcome (the placeholder says the outcome, the strip does not say it again)', async () => {
    const page = await newPage();
    for (const vin of [EMPTY_VIN, UNAUTHORIZED_VIN, FIVE_RECORDS_VIN]) {
      await load(page, vin);
      await settle(page);
      expect(q(page, '.lt-lead').getAttribute('data-lead')).toBe('columns');
    }
  });

  it('announces its verdict accent', async () => {
    const page = await newPage();
    const seen: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent) => seen.push(event.detail));
    await load(page, FIVE_RECORDS_VIN);
    await load(page, UNAUTHORIZED_VIN);
    await settle(page);
    expect(seen).toEqual(['positive', 'neutral']);
  });
});

describe('vehicle-service-history — the owner: rows, columns and paging', () => {
  it('shows one page of 5 for a 12-record vehicle, with the pager open and the range "1–5 of 12"', async () => {
    const page = await newPage();
    await load(page, TWELVE_VIN);
    await settle(page);
    expect(rowCount(page)).toBe(5);
    expect(pagerOpen(page)).toBe('true');
    expect(pageRange(page)).toBe('1–5 of 12');
    expect(qa(page, '.lt-page-size-select option').map(option => squash(option.textContent))).toEqual(['5', '10', '15', '20', '25', englishLocale.pageAll]);
  });

  it('pages 12 records as 5, 5, 2 and back, each page with its own rows', async () => {
    const page = await newPage();
    await load(page, TWELVE_VIN);
    await settle(page);
    // The page's rows come first; rows a shorter page drops stay shut in their slots for a settle, and a longer one's arrive shut.
    const invoices = (count: number) => invoicesOf(page).slice(0, count);
    const all = history(TWELVE_VIN).map(record => record.invoiceNumber);

    expect(invoices(5)).toEqual(all.slice(0, 5));
    await next(page);
    expect(invoices(5)).toEqual(all.slice(5, 10));
    expect(pageRange(page)).toBe('6–10 of 12');
    await next(page);
    expect(invoices(2)).toEqual(all.slice(10, 12));
    expect(pageRange(page)).toBe('11–12 of 12');
    expect(q(page, '.lt-page-button.is-next').hasAttribute('disabled')).toBe(true);
    await previous(page);
    expect(invoices(5)).toEqual(all.slice(5, 10));
  });

  it('page size "All" lists all 12 records, and a lookup returns to page one', async () => {
    const page = await newPage();
    await load(page, TWELVE_VIN);
    await settle(page);
    await chooseSize(page, 'all');
    expect(slotCount(page)).toBe(12);
    expect(pageRange(page)).toContain('of 12');

    await chooseSize(page, '5');
    await next(page);
    expect(pageRange(page)).toBe('6–10 of 12');
    await load(page, TWELVE_VIN);
    await settle(page);
    expect(pageRange(page)).toBe('1–5 of 12');
  });

  it('keeps the pager shut for a vehicle that fits one page (exactly 5), and opens it for exactly 10 (two pages)', async () => {
    const page = await newPage();
    await load(page, EXACT_FIVE_VIN);
    await settle(page);
    expect(rowCount(page)).toBe(5);
    expect(pagerOpen(page)).toBe('false');

    await load(page, EXACT_TEN_VIN);
    await settle(page);
    expect(rowCount(page)).toBe(5);
    expect(pagerOpen(page)).toBe('true');
    expect(pageRange(page)).toBe('1–5 of 10');
    await next(page);
    expect(rowCount(page)).toBe(5);
    expect(pageRange(page)).toBe('6–10 of 10');
  });

  it('draws a row from its record: branch, dealer, invoice, date, service type and odometer, the job number hidden by default', async () => {
    const page = await newPage();
    await load(page, FIVE_RECORDS_VIN);
    await settle(page);
    const [first] = rowsOf(page);
    expect(cellText(first, 'branchName')).toBe('Main Showroom');
    expect(cellText(first, 'companyName')).toBe('Sample Dealer');
    expect(cellText(first, 'invoiceNumber')).toBe('60110245');
    expect(cellText(first, 'serviceType')).toBe('Brake service');
    expect(cellText(first, 'mileage')).toBe('60,412');
    expect(first.querySelector(`[data-label="${englishLocale.jobNumber}"]`)).toBeNull();
    expect(headings(page)).not.toContain(englishLocale.jobNumber);
  });

  it('reads a missing value as a dash, never as an invented one; the long service type carries its whole text as a title', async () => {
    const page = await newPage();
    await load(page, NULLS_VIN);
    await settle(page);
    const [noType, , , noInvoice] = rowsOf(page);
    expect(cellText(noType, 'serviceType')).toBe('—');
    expect(cellText(rowsOf(page)[1], 'mileage')).toBe('—');
    expect(cellText(noInvoice, 'invoiceNumber')).toBe('—');

    await load(page, LONG_LINES_VIN);
    await settle(page);
    const title = rowsOf(page)[0].querySelector(`[data-label="${englishLocale.serviceType}"] .lt-value`).getAttribute('title');
    expect(title).toBe(history(LONG_LINES_VIN)[0].serviceType);
  });

  it('does not warn about repeated keys for a vehicle whose records have nulls (the keys are unique)', async () => {
    const warn = jest.spyOn(console, 'warn').mockImplementation(() => {});
    try {
      const page = await newPage();
      await load(page, NULLS_VIN);
      await settle(page);
      await load(page, TWELVE_VIN);
      await settle(page);
      expect(warn.mock.calls.filter(call => String(call[0]).includes('actions.keyOf'))).toEqual([]);
    } finally {
      warn.mockRestore();
    }
  });

  it('hiddenFields: shows the job number when emptied, and drops a named column from the headings and the rows', async () => {
    const page = await newPage('hidden-fields=""');
    await load(page, FIVE_RECORDS_VIN);
    await settle(page);
    expect(headings(page)).toContain(englishLocale.jobNumber);
    expect(cellText(rowsOf(page)[0], 'jobNumber')).toBe('71002');

    page.root.setAttribute('hidden-fields', 'serviceType,mileage,jobNumber');
    await page.waitForChanges();
    await settle(page, 1000);
    expect(headings(page)).not.toContain(englishLocale.serviceType);
    expect(headings(page)).not.toContain(englishLocale.odometer);
    expect(headings(page)).not.toContain(englishLocale.jobNumber);
    expect(headings(page)).toContain(englishLocale.branch);
    expect(rowsOf(page)[0].querySelector(`[data-label="${englishLocale.serviceType}"]`)).toBeNull();
  });
});

describe('vehicle-service-history — the owner: the drawer', () => {
  const open = async (page: Page, index: number) => {
    buttons(page)[index].click();
    await page.waitForChanges();
  };
  const expanded = (page: Page) => buttons(page).map(button => button.getAttribute('aria-expanded'));

  const loaded = async (vin = FIVE_RECORDS_VIN) => {
    const page = await newPage();
    await load(page, vin);
    await settle(page);
    return page;
  };

  it('every action button says aria-expanded="false" and points at a drawer id, which exists once its row is open', async () => {
    const page = await loaded();
    expect(buttons(page)).toHaveLength(5);
    expect(expanded(page)).toEqual(['false', 'false', 'false', 'false', 'false']);
    for (const button of buttons(page)) expect(button.getAttribute('aria-controls')).toMatch(/^lt-drawer-/);
    expect(button0Name(page)).toBe(`${englishLocale.expandRow} 60110245`);
    expect(drawers(page)).toHaveLength(0);
  });

  const button0Name = (page: Page) => buttons(page)[0].getAttribute('aria-label');

  it('opens a drawer from the button: aria-expanded true, the drawer region found by aria-controls, the name turns to "Hide details"', async () => {
    const page = await loaded();
    const controls = buttons(page)[0].getAttribute('aria-controls');
    await open(page, 0);

    expect(expanded(page)).toEqual(['true', 'false', 'false', 'false', 'false']);
    expect(drawers(page)).toHaveLength(1);
    expect(drawers(page)[0].id).toBe(controls);
    expect(drawers(page)[0].getAttribute('role')).toBe('region');
    expect(button0Name(page)).toBe(`${englishLocale.collapseRow} 60110245`);
    expect(rowsOf(page)[0].getAttribute('data-expanded')).toBe('true');
  });

  it('closes it from the same button', async () => {
    const page = await loaded();
    await open(page, 0);
    await open(page, 0);
    expect(expanded(page)).toEqual(['false', 'false', 'false', 'false', 'false']);
    expect(drawers(page)).toHaveLength(0);
    await settle(page);
    expect(qa(page, '.service-history-drawer')).toHaveLength(0);
  });

  it('is single-open: opening another closes the first', async () => {
    const page = await loaded();
    await open(page, 0);
    await open(page, 2);
    expect(expanded(page)).toEqual(['false', 'false', 'true', 'false', 'false']);
    expect(drawers(page)).toHaveLength(1);
    await settle(page);
    expect(qa(page, '.service-history-drawer')).toHaveLength(1);
  });

  it('brings the row into view on open (revealDrawer is called with the shadow root and the key), and not on close', async () => {
    // Spied on the table's own module: the barrel the owner imports from reads it live.
    const revealDrawer = jest.spyOn(lookupTableModule, 'revealDrawer');
    const page = await loaded();
    revealDrawer.mockClear();

    await open(page, 0);
    expect(revealDrawer).toHaveBeenCalledTimes(1);
    const [root, key, followMs] = revealDrawer.mock.calls[0];
    expect(root).toBe(shadow(page));
    expect(key).toBe(historyKey(history(FIVE_RECORDS_VIN)[0]));
    expect(followMs).toBeGreaterThan(0);

    await open(page, 0);
    expect(revealDrawer).toHaveBeenCalledTimes(1);

    await open(page, 1);
    expect(revealDrawer).toHaveBeenCalledTimes(2);
    expect(revealDrawer.mock.calls[1][1]).toBe(historyKey(history(FIVE_RECORDS_VIN)[1]));
  });

  it('writes the meta line: job number, and the account number trimmed, in the first record; nothing empty', async () => {
    const page = await loaded();
    await open(page, 0);
    const items = qa(page, '.service-history-meta-item').map(item => [item.getAttribute('data-field'), squash(item.textContent)]);
    expect(items).toEqual([
      ['jobNumber', `${englishLocale.jobNumber} 71002`],
      ['accountNumber', `${englishLocale.accountNumber} UD50011`],
    ]);
  });

  it('shows a real parent invoice and hides a "0" one, and omits the meta line when nothing is there', async () => {
    const page = await loaded();
    await open(page, 3); // credit note with a real parent
    expect(qa(page, '.service-history-meta-item').map(item => item.getAttribute('data-field'))).toEqual(['jobNumber', 'parentInvoice']);
    expect(squash(q(page, '.service-history-meta-item[data-field="parentInvoice"]').textContent)).toBe(`${englishLocale.parentInvoice} 60097732`);

    await open(page, 4); // parent "0"
    await settle(page);
    expect(qa(page, '.service-history-meta-item').map(item => item.getAttribute('data-field'))).toEqual(['jobNumber', 'accountNumber']);
  });

  it('lists the labor lines and the part lines as embedded tables with their counts', async () => {
    const page = await loaded();
    await open(page, 2); // 2 labor, 3 parts
    const section = (which: string) => q(page, `.service-history-lines[data-kind="${which}"]`);
    expect(squash(section('labor').querySelector('.service-history-lines-title').textContent)).toBe(englishLocale.laborLines);
    expect(squash(section('labor').querySelector('.service-history-lines-count').textContent)).toBe('2');
    expect(squash(section('part').querySelector('.service-history-lines-count').textContent)).toBe('3');
    expect(section('labor').querySelectorAll('.lt-row')).toHaveLength(2);
    expect(section('part').querySelectorAll('.lt-row')).toHaveLength(3);
    expect(section('part').querySelector('.service-history-none')).toBeNull();
  });

  it('drops the embedded columns that are empty on every line: no part description, no package code, where there are none', async () => {
    const page = await loaded();
    const heads = (which: string) =>
      Array.from(q(page, `.service-history-lines[data-kind="${which}"] .lt-columns`).querySelectorAll('.lt-cell')).map(cell => squash(cell.textContent));

    await open(page, 0); // labor lines with a service code and no package code
    expect(heads('labor')).toEqual([englishLocale.laborCode, englishLocale.serviceDescription, englishLocale.serviceCode]);

    await open(page, 2); // package code present on the labor lines; part description present
    await settle(page);
    expect(heads('labor')).toContain(englishLocale.packageCode);
    expect(heads('part')).toContain(englishLocale.partDescription);

    await open(page, 3); // parts with no description at all
    await settle(page);
    expect(heads('part')).not.toContain(englishLocale.partDescription);
    expect(heads('part')).toEqual(expect.arrayContaining([englishLocale.partNumber, englishLocale.qty]));
  });

  it('states that there are no labor lines for a part-only invoice, and no part lines for a labor-only one: about the invoice, not the vehicle', async () => {
    const page = await loaded();
    await open(page, 1); // part-only
    expect(squash(q(page, '.service-history-lines[data-kind="labor"] .service-history-none').textContent)).toBe(englishLocale.noLaborLines);
    expect(q(page, '.service-history-lines[data-kind="part"] .service-history-none')).toBeNull();

    await open(page, 0); // labor-only
    await settle(page);
    expect(squash(q(page, '.service-history-lines[data-kind="part"] .service-history-none').textContent)).toBe(englishLocale.noPartLines);
    expect(q(page, '.service-history-lines[data-kind="labor"] .service-history-none')).toBeNull();
  });

  it('builds a drawer only for the open row: no drawer content in a shut one', async () => {
    const page = await loaded();
    expect(qa(page, '.service-history-drawer')).toHaveLength(0);
    await open(page, 1);
    expect(qa(page, '.service-history-drawer')).toHaveLength(1);
  });

  it('stays open through a lookup that brings the same record back, veiled under the skeleton meanwhile', async () => {
    const page = await loaded();
    await open(page, 0);

    const pending = owner(page).fetchVin(FIVE_RECORDS_VIN);
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(expanded(page)[0]).toBe('true');
    expect(drawers(page)).toHaveLength(1);

    await pending;
    await settle(page);
    expect(expanded(page)).toEqual(['true', 'false', 'false', 'false', 'false']);
    expect(drawers(page)).toHaveLength(1);
  });

  it('slides shut when the lookup brings a vehicle whose records no longer include that one', async () => {
    const page = await loaded();
    await open(page, 0);

    await load(page, NULLS_VIN);
    await settle(page);
    expect(expanded(page)).toEqual(['false', 'false', 'false', 'false', 'false']);
    expect(drawers(page)).toHaveLength(0);
    expect(qa(page, '.service-history-drawer')).toHaveLength(0);
  });

  it('is shut by paging away, and still shut when the page comes back', async () => {
    const page = await loaded(TWELVE_VIN);
    await open(page, 0);
    expect(drawers(page)).toHaveLength(1);

    await next(page);
    expect(drawers(page)).toHaveLength(0);
    expect(expanded(page).every(state => state === 'false')).toBe(true);

    await previous(page);
    expect(expanded(page).every(state => state === 'false')).toBe(true);
    expect(drawers(page)).toHaveLength(0);
  });

  it('stays open when a bigger page keeps its row on screen', async () => {
    const page = await loaded(TWELVE_VIN);
    await open(page, 1);
    await chooseSize(page, '10');
    expect(slotCount(page)).toBe(10);
    expect(expanded(page)[1]).toBe('true');
    expect(drawers(page)).toHaveLength(1);
  });

  it('ignores a toggle while a lookup is in flight', async () => {
    const page = await loaded();
    const pending = owner(page).fetchVin(NULLS_VIN);
    await page.waitForChanges();
    buttons(page)[0].click();
    await pending;
    await settle(page);
    expect(drawers(page)).toHaveLength(0);
  });

  it('handles an invoice with many lines (14 labor, 12 part) as plain tables in the drawer', async () => {
    const page = await loaded(LONG_LINES_VIN);
    await open(page, 0);
    expect(q(page, '.service-history-lines[data-kind="labor"]').querySelectorAll('.lt-row')).toHaveLength(14);
    expect(q(page, '.service-history-lines[data-kind="part"]').querySelectorAll('.lt-row')).toHaveLength(12);
  });

  it('hiddenFields naming every column still renders the invoice, date and expand button, and a drawer opens', async () => {
    const page = await newPage('hidden-fields="invoiceNumber,serviceDate,details,branchName,companyName,jobNumber,serviceType,mileage"');
    await load(page, FIVE_RECORDS_VIN);
    await settle(page);
    expect(headings(page)).toContain(englishLocale.invoiceNumber);
    expect(headings(page)).not.toContain(englishLocale.serviceType);
    expect(cellText(rowsOf(page)[0], 'invoiceNumber')).toBe('60110245');
    expect(buttons(page)).toHaveLength(5);
    buttons(page)[0].click();
    await page.waitForChanges();
    expect(drawers(page)).toHaveLength(1);
  });
});
