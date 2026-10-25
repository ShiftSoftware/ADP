import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import * as revealBelowModule from '~lib/reveal-below';

import { VehicleAccessories } from './vehicle-accessories';
import { ShiftImage } from '../components/shift-image/shift-image';
import { ShiftImageLayer } from '../components/shift-image/shift-image-layer';
import { ShiftPortal } from '../components/shift-portal';
import { VehicleLookup } from './vehicle-lookup';
import {
  AccessoriesLocale,
  DEFAULT_HIDDEN_FIELDS,
  DEFAULT_PAGE_SIZE,
  FIELDS,
  PAGE_SIZE_OPTIONS,
  PAGE_SIZE_STORAGE_KEY,
  PanelState,
  accessoryColumns,
  clampPage,
  columnCount,
  countText,
  hasRecords,
  isPaginated,
  leadNotice,
  listed,
  panelBody,
  panelLead,
  pluralCategory,
  pageCount,
  pageForSize,
  pageRangeText,
  pageRows,
  pagingNeedsReveal,
  panelPlaceholder,
  parsePageSize,
  sameColumns,
  totalCount,
} from './components/VehicleAccessoriesPanel';
import { parseHiddenFields } from '~features/lookup-table';
import { AccessoryDTO } from '~types/generated/vehicle-lookup/accessory-dto';

import englishLocale from '../../locales/vehicleLookup/accessories/en.json';
import arabicLocale from '../../locales/vehicleLookup/accessories/ar.json';
import kurdishLocale from '../../locales/vehicleLookup/accessories/ku.json';
import russianLocale from '../../locales/vehicleLookup/accessories/ru.json';
import englishShared from '../../locales/en.json';

import brokerMarketMocks from '../../features/mocks/data/generated/broker-market/vehicle-lookup.json';
import edgeCaseMocks from '../../features/mocks/data/generated/edge-cases/vehicle-lookup.json';

/**
 * vehicle-accessories: the pure derivations, then the owner through the DOM. The stylesheet cannot
 * be evaluated here (mock-doc has no layout and no container queries), so layout is pinned by the
 * attributes the CSS keys on and the lookup table's own spec pins the cells. Fixtures are fictional
 * or generated; no client data.
 */

const locale = englishLocale as unknown as AccessoriesLocale;

// The generated fixtures (see the case map in the run status): a single accessory, nine, twenty-four, an
// authorized vehicle with an empty list, an unauthorized one, and the edge-case vehicle with job 0 / no job.
const SINGLE_VIN = 'ZS8AX5KBXT6710002';
const NINE_VIN = 'ZS8EU2ZF6ST814892';
const MANY_VIN = 'ZS8AX5KB4T6710013';
const EMPTY_VIN = 'ZU8ZL7VAXG2426255';
const UNAUTHORIZED_VIN = 'ZV8GHHHP37P214642';
const MISSING_VALUES_VIN = 'ZS8AX5KB8T6720009';

const ALL_FIELDS = 'hidden-fields=""';

const accessory = (n: number, over: object = {}) => ({
  partNumber: `TST-${String(n).padStart(4, '0')}`,
  description: `Sample accessory ${n}`,
  image: `https://example.invalid/accessory-${n}.png`,
  jobNumber: 1000 + n,
  invoiceNumber: 5000 + n,
  companyName: 'Sample Dealer',
  ...over,
});

const dto = (count: number, over: object = {}) => ({
  vin: `ZS8TESTVIN${String(count).padStart(7, '0')}`,
  isAuthorized: true,
  accessories: Array.from({ length: count }, (_, index) => accessory(index + 1)),
  ...over,
});

const state = (over: Partial<PanelState> = {}): PanelState => ({
  vehicleLoaded: true,
  authorized: true,
  accessories: [accessory(1), accessory(2)] as unknown as PanelState['accessories'],
  hidden: parseHiddenFields(DEFAULT_HIDDEN_FIELDS),
  ...over,
});

describe('vehicle-accessories — derivations', () => {
  it('lists only what the distributor may show: an unauthorized or unloaded vehicle has no rows, whatever the DTO holds', () => {
    expect(listed(state())).toHaveLength(2);
    expect(listed(state({ authorized: undefined }))).toHaveLength(2);
    expect(listed(state({ authorized: false }))).toEqual([]);
    expect(listed(state({ vehicleLoaded: false }))).toEqual([]);
    expect(listed(state({ accessories: null }))).toEqual([]);
    expect(listed(state({ accessories: undefined }))).toEqual([]);
    expect(hasRecords(state({ authorized: false }))).toBe(false);
  });

  it('opens the body on rows only when there is a list and a column to draw it in', () => {
    expect(panelBody(state())).toBe('rows');
    expect(panelBody(state({ accessories: [] }))).toBe('none');
    expect(panelBody(state({ authorized: false }))).toBe('none');
    expect(panelBody(state({ vehicleLoaded: false }))).toBe('none');
    expect(panelBody(state({ hidden: parseHiddenFields(FIELDS.join(',')) }))).toBe('none');
  });

  describe('panelLead: the headings depend on hiddenFields alone, so only an outcome without rows replaces them (owner, F1)', () => {
    const none = parseHiddenFields(FIELDS.join(','));
    const failed = { vehicleLoaded: false, accessories: undefined, error: 'Failed' };

    it('is the headings when idle: before any lookup', () => {
      expect(panelLead(state({ vehicleLoaded: false, accessories: undefined }), false)).toBe('columns');
    });

    it('is the headings while loading, whatever the outgoing state was', () => {
      expect(panelLead(state(), true)).toBe('columns');
      expect(panelLead(state({ accessories: [] }), true)).toBe('columns');
      expect(panelLead(state({ authorized: false }), true)).toBe('columns');
      expect(panelLead(state({ vehicleLoaded: false, accessories: undefined }), true)).toBe('columns');
      expect(panelLead(state(failed), true)).toBe('columns');
    });

    it('is the headings above rows', () => {
      expect(panelLead(state(), false)).toBe('columns');
    });

    it('stays on the headings for an outcome without rows: the pill says it, nothing covers or repeats it (owner, 2026-10-25)', () => {
      expect(panelLead(state({ accessories: [] }), false)).toBe('columns');
      expect(panelLead(state({ authorized: false }), false)).toBe('columns');
      expect(panelLead(state(failed), false)).toBe('columns');

      // No band: the pill says it (owner, 2026-10-25). The panel has no band at all.
    });

    it('is a notice when every column is hidden, in every state, because there are no headings to show', () => {
      expect(panelLead(state({ hidden: none }), false)).toBe('notice');
      expect(panelLead(state({ hidden: none }), true)).toBe('notice');
      expect(panelLead(state({ hidden: none, vehicleLoaded: false, accessories: undefined }), false)).toBe('notice');
      expect(panelLead(state({ hidden: none, vehicleLoaded: false, accessories: undefined }), true)).toBe('notice');
    });

    it('never answers skeleton', () => {
      const answers = new Set<string>();
      for (const busy of [false, true]) {
        for (const over of [{}, { accessories: [] }, { authorized: false }, failed, { vehicleLoaded: false, accessories: undefined }, { hidden: none }])
          answers.add(panelLead(state(over), busy));
      }
      expect([...answers].sort()).toEqual(['columns', 'notice']);
    });
  });

  it('says unauthorized in amber, an empty list in grey, never a verdict, and nothing above rows or before a lookup', () => {
    expect(leadNotice(state({ authorized: false }), locale)).toEqual({ tone: 'neutral', text: locale.unauthorizedNotice });
    // An unauthorized vehicle that carries a list in the DTO still says "not in the records": the list is a data fault.
    expect(leadNotice(state({ authorized: false, accessories: [accessory(1)] as unknown as PanelState['accessories'] }), locale)?.tone).toBe('neutral');
    expect(leadNotice(state({ accessories: [] }), locale)).toEqual({ tone: 'idle', text: locale.noAccessories });
    expect(leadNotice(state({ accessories: null }), locale)?.tone).toBe('idle');
    expect(leadNotice(state({ hidden: parseHiddenFields(FIELDS.join(',')) }), locale)).toEqual({ tone: 'idle', text: locale.noFieldsShown });
    // Also before a lookup: with no columns there are no headings to wait behind.
    expect(leadNotice(state({ vehicleLoaded: false, accessories: undefined, hidden: parseHiddenFields(FIELDS.join(',')) }), locale)).toEqual({
      tone: 'idle',
      text: locale.noFieldsShown,
    });
    expect(leadNotice(state(), locale)).toBeNull();
    expect(leadNotice(state({ vehicleLoaded: false }), locale)).toBeNull();
    expect(leadNotice(state({ vehicleLoaded: false, error: 'Failed' }), locale)).toEqual({ tone: 'negative', text: 'Failed' });

    for (const input of [state({ accessories: [] }), state({ authorized: false }), state({ accessories: null })]) {
      expect(['positive', 'negative']).not.toContain(leadNotice(input, locale)?.tone);
    }
  });

  it('counts only a loaded, authorized vehicle with records: never idle, unauthorized, empty or an error', () => {
    expect(totalCount(state())).toBe(2);
    expect(totalCount(state({ accessories: Array.from({ length: 24 }, (_, i) => accessory(i)) as unknown as PanelState['accessories'] }))).toBe(24);
    expect(totalCount(state({ vehicleLoaded: false }))).toBeNull();
    expect(totalCount(state({ authorized: false }))).toBeNull();
    expect(totalCount(state({ accessories: [] }))).toBeNull();
    expect(totalCount(state({ accessories: null }))).toBeNull();
    // The count is the vehicle's, not the screen's: hiding every column does not change it.
    expect(totalCount(state({ hidden: parseHiddenFields(FIELDS.join(',')) }))).toBe(2);
  });

  it('knows the columns hiddenFields leaves, and ignores names it does not know', () => {
    expect(columnCount(parseHiddenFields(DEFAULT_HIDDEN_FIELDS))).toBe(3);
    expect(columnCount(parseHiddenFields(''))).toBe(6);
    expect(columnCount(parseHiddenFields('image,foo'))).toBe(5);
    expect(sameColumns(parseHiddenFields('image'), parseHiddenFields(' image '))).toBe(true);
    expect(sameColumns(parseHiddenFields('image'), parseHiddenFields('image,foo'))).toBe(true);
    expect(sameColumns(parseHiddenFields('image'), parseHiddenFields('jobNumber'))).toBe(false);
  });

  it('defines the six columns in order, the part number as the row identity and the description as the one that grows', () => {
    const columns = accessoryColumns(locale);
    expect(columns.map(column => column.field)).toEqual([...FIELDS]);
    expect(columns.map(column => column.kind)).toEqual(['code', 'text', 'media', 'ref', 'ref', 'text']);
    expect(columns.filter(column => column.identity).map(column => column.field)).toEqual(['partNumber']);
    expect(columns.filter(column => column.grow).map(column => column.field)).toEqual(['description']);

    const row = { partNumber: 'P', description: 'D', image: 'I', jobNumber: undefined, invoiceNumber: null, companyName: undefined } as unknown as Parameters<
      ReturnType<typeof accessoryColumns>[number]['value']
    >[0];
    expect(columns.map(column => column.value(row))).toEqual(['P', 'D', 'I', undefined, null, undefined]);
  });

  it('pages: five by default, in steps of five to twenty-five, then all; anything else read back is the default', () => {
    expect(DEFAULT_PAGE_SIZE).toBe(5);
    expect(PAGE_SIZE_OPTIONS).toEqual([5, 10, 15, 20, 25, 'all']);
    expect(parsePageSize('10')).toBe(10);
    expect(parsePageSize(25)).toBe(25);
    expect(parsePageSize('all')).toBe('all');
    for (const junk of ['7', '30', '', null, undefined, 'ALL', '5.5']) expect(parsePageSize(junk)).toBe(5);
  });

  it('pages: cuts the list into pages and keeps a page index inside it', () => {
    const rows = (n: number) => Array.from({ length: n }, (_, index) => accessory(index + 1)) as unknown as AccessoryDTO[];
    const parts = (list: AccessoryDTO[]) => list.map(row => row.partNumber);

    expect(pageCount(23, 5)).toBe(5);
    expect(pageCount(25, 5)).toBe(5);
    expect(pageCount(0, 5)).toBe(1);
    expect(pageCount(23, 'all')).toBe(1);
    expect(clampPage(9, 23, 5)).toBe(4);
    expect(clampPage(-2, 23, 5)).toBe(0);
    expect(parts(pageRows(rows(23), 4, 5))).toEqual(parts(rows(23).slice(20)));
    expect(pageRows(rows(23), 9, 5)).toHaveLength(3);
    expect(pageRows(rows(23), 3, 'all')).toHaveLength(23);
    expect(isPaginated(5)).toBe(false);
    expect(isPaginated(6)).toBe(true);
  });

  it('pages: a new page size keeps the first row on screen in view', () => {
    expect(pageForSize(3, 5, 10, 23)).toBe(1); // row 16 is on page 2 of ten
    expect(pageForSize(1, 10, 5, 23)).toBe(2); // row 11 is on page 3 of five
    expect(pageForSize(4, 5, 25, 23)).toBe(0);
    expect(pageForSize(2, 5, 'all', 23)).toBe(0);
    expect(pageForSize(0, 'all', 5, 23)).toBe(0);
  });

  it('pages: the range says which rows are on screen', () => {
    expect(pageRangeText(0, 5, 23, locale)).toBe('1–5 of 23');
    expect(pageRangeText(4, 5, 23, locale)).toBe('21–23 of 23');
    expect(pageRangeText(0, 'all', 23, locale)).toBe('1–23 of 23');
  });

  it('placeholder: idle before a lookup, after an error and when not in records; empty for nothing on file; none above rows', () => {
    expect(panelPlaceholder(state({ vehicleLoaded: false, accessories: undefined }))).toBe('idle');
    expect(panelPlaceholder(state({ accessories: [] }))).toBe('empty');
    expect(panelPlaceholder(state())).toBeNull();
    // Not in the distributor's records: the pill says so and the table is in its idle look.
    expect(panelPlaceholder(state({ authorized: false }))).toBe('idle');
    // A failed lookup leaves the table idle.
    expect(panelPlaceholder(state({ vehicleLoaded: false, accessories: undefined, error: 'Failed' }))).toBe('idle');
    expect(panelPlaceholder(state({ vehicleLoaded: false, accessories: undefined, hidden: parseHiddenFields(FIELDS.join(',')) }))).toBeNull();
  });

  it('asks the page to follow a shorter page only when the rows that go would lift the controls off the top of the screen', () => {
    expect(pagingNeedsReveal(900, 400)).toBe(false);
    expect(pagingNeedsReveal(300, 400)).toBe(true);
    expect(pagingNeedsReveal(400, 400)).toBe(false);
    expect(pagingNeedsReveal(0, 0)).toBe(false);
  });
});

describe('vehicle-accessories — plural forms', () => {
  // One distinct string per category, so a test sees which key was picked.
  const forms: AccessoriesLocale = {
    ...locale,
    accessoryCountZero: 'zero:{count}',
    accessoryCountOne: 'one:{count}',
    accessoryCountTwo: 'two:{count}',
    accessoryCountFew: 'few:{count}',
    accessoryCountMany: 'many:{count}',
    accessoryCountOther: 'other:{count}',
  };

  it('English has two forms', () => {
    expect(countText(1, locale, 'en')).toBe('1 accessory');
    expect(countText(2, locale, 'en')).toBe('2 accessories');
    expect(countText(24, locale, 'en')).toBe('24 accessories');
    expect(countText(9, locale, 'en')).toBe('9 accessories');
  });

  it('Arabic has six: zero, one, two, few, many, other', () => {
    expect(pluralCategory(0, 'ar')).toBe('zero');
    expect(pluralCategory(1, 'ar')).toBe('one');
    expect(pluralCategory(2, 'ar')).toBe('two');
    expect(pluralCategory(3, 'ar')).toBe('few');
    expect(pluralCategory(10, 'ar')).toBe('few');
    expect(pluralCategory(11, 'ar')).toBe('many');
    expect(pluralCategory(99, 'ar')).toBe('many');
    expect(pluralCategory(100, 'ar')).toBe('other');

    expect([0, 1, 2, 3, 11, 100].map(n => countText(n, forms, 'ar'))).toEqual(['zero:0', 'one:1', 'two:2', 'few:3', 'many:11', 'other:100']);
  });

  it('Russian picks one, few, many by the last digits', () => {
    expect([1, 2, 5, 11, 21, 22, 25].map(n => countText(n, forms, 'ru'))).toEqual(['one:1', 'few:2', 'many:5', 'many:11', 'one:21', 'few:22', 'many:25']);
  });

  it('falls back to "other" when a locale left a category blank, and to one/other for a language the runtime does not know', () => {
    expect(countText(3, { ...forms, accessoryCountFew: '' }, 'ar')).toBe('other:3');
    expect(pluralCategory(1, 'not a language!')).toBe('one');
    expect(pluralCategory(7, 'not a language!')).toBe('other');
  });

  it('every language carries every key of English, and every count form keeps its {count}', () => {
    const keys = Object.keys(englishLocale).sort();
    for (const other of [arabicLocale, kurdishLocale, russianLocale]) expect(Object.keys(other).sort()).toEqual(keys);

    for (const file of [englishLocale, arabicLocale, kurdishLocale, russianLocale] as Record<string, string>[]) {
      for (const key of keys.filter(k => k.startsWith('accessoryCount'))) expect([key, file[key].includes('{count}')]).toEqual([key, true]);
      for (const placeholder of ['{from}', '{to}', '{total}']) expect(file.pageRange.includes(placeholder)).toBe(true);
      expect(Object.values(file).every(value => typeof value === 'string' && value.trim() !== '')).toBe(true);
    }
  });
});

beforeAll(() => {
  (global as unknown as { fetch: unknown }).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

const newPage = async (mocks: unknown = brokerMarketMocks, attributes = '') => {
  const page = await newSpecPage({
    components: [VehicleAccessories, ShiftImage, ShiftImageLayer, ShiftPortal],
    html: `<vehicle-accessories is-dev="true" ${attributes}></vehicle-accessories>`,
  });
  await (page.rootInstance as VehicleAccessories).setMockData(mocks as never);
  return page;
};

type Page = Awaited<ReturnType<typeof newPage>>;

const owner = (page: Page) => page.rootInstance as VehicleAccessories;
const internals = (page: Page) => page.rootInstance as unknown as { leaving: boolean; reconfiguring: boolean; page: number; pageSize: number | 'all'; paging: boolean };
const shadow = (page: Page) => page.root.shadowRoot;
const squash = (value?: string | null) => value?.replace(/\s+/g, ' ').trim();
const text = (page: Page, selector: string) => squash(shadow(page).querySelector(selector)?.textContent);
const all = (page: Page, selector: string) => Array.from(shadow(page).querySelectorAll<HTMLElement>(selector));
const attr = (page: Page, selector: string, name: string) => shadow(page).querySelector(selector)?.getAttribute(name);

const load = async (page: Page, vin: string | object) => {
  await owner(page).fetchVin(vin as never);
  await page.waitForChanges();
};

const settle = async (page: Page) => {
  await new Promise(resolve => setTimeout(resolve, 700));
  await page.waitForChanges();
};

const verdict = (page: Page) => attr(page, '.lookup-card', 'data-verdict');
const pill = (page: Page) => shadow(page).querySelector('.lookup-summary .status-badge');
const count = (page: Page) => shadow(page).querySelector('.accessories-count');
const lead = (page: Page) => attr(page, '.lt-lead', 'data-lead');
const rows = (page: Page) => all(page, '.lt-row');
const headings = (page: Page) => all(page, '.lt-columns > .lt-cell').map(cell => squash(cell.textContent));
const bodyOpen = (page: Page) => attr(page, '.lt-body', 'data-open');
/** There is no notice band repeating the pill (owner, 2026-10-25). */
const noBand = (page: Page) => expect(shadow(page).querySelector('.accessories-notice')).toBeNull();
const pagerOpen = (page: Page) => attr(page, '.accessories-pager', 'data-open');
const nextButton = (page: Page) => shadow(page).querySelector<HTMLButtonElement>('.accessories-page-button.is-next');
const previousButton = (page: Page) => shadow(page).querySelector<HTMLButtonElement>('.accessories-page-button.is-previous');
const sizeSelect = (page: Page) => shadow(page).querySelector<HTMLSelectElement>('.accessories-page-size-select');
/** The option the render marked selected (mock-doc does not derive a select's value from it). */
const sizeShown = (page: Page) =>
  all(page, '.accessories-page-size-select option')
    .find(option => option.hasAttribute('selected'))
    ?.getAttribute('value');
const chooseSize = async (page: Page, size: string) => {
  const select = sizeSelect(page);
  select.value = size;
  select.dispatchEvent(new Event('change'));
  await page.waitForChanges();
};
/** The rows in open slots: what the reader sees (leaving and arriving slots are shut). */
const openRows = (page: Page) => all(page, '.lt-slot[data-open="true"] .lt-row');
/** mock-doc has no layout: a test gives an element the box it wants the owner to measure. */
const withRect = (element: Element, rect: { top: number; height: number }) => {
  (element as unknown as { getBoundingClientRect: () => unknown }).getBoundingClientRect = () => rect;
};
type PanelHost = { hiddenFields: string; coreOnly: boolean; shadowRoot: ShadowRoot };
const cellsOf = (row: HTMLElement) => Array.from(row.querySelectorAll<HTMLElement>('.lt-cell'));

describe('vehicle-accessories — the states', () => {
  it('opens idle: the column headings in the strip, no rows, no pill, no count, a grey accent and shut disclosure', async () => {
    const page = await newPage();

    expect(verdict(page)).toBe('idle');
    // The columns depend on hiddenFields alone, so the strip heads them before any answer: not a skeleton.
    expect(lead(page)).toBe('columns');
    expect(headings(page)).toEqual(['Part Number', 'Description', 'Image']);
    expect(attr(page, '.lt-columns', 'data-active')).toBe('true');
    expect(shadow(page).querySelector('.lt-skeleton')).toBeNull();
    // Visible, but not a table: no roles, and hidden from assistive tech until rows open.
    expect(attr(page, '.lt-columns', 'role')).toBeNull();
    expect(attr(page, '.lt-columns', 'aria-hidden')).toBe('true');
    expect(attr(page, '.accessories-card', 'data-body')).toBe('none');
    expect(bodyOpen(page)).toBe('false');
    expect(rows(page)).toHaveLength(0);
    expect(pill(page)).toBeNull();
    expect(count(page)).toBeNull();
    expect(pagerOpen(page)).toBe('false');
    expect(text(page, '.lookup-title')).toBe('Vehicle Accessories');
    // Not a table that is not there.
    expect(attr(page, '.lt-grid', 'role')).toBeNull();
  });

  it('is a record panel: with records it shows the list, no pill, a green accent, headings and the count', async () => {
    const page = await newPage();
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));

    await load(page, NINE_VIN);

    expect(verdict(page)).toBe('positive');
    expect(pill(page)).toBeNull();
    expect(lead(page)).toBe('columns');
    expect(bodyOpen(page)).toBe('true');
    expect(rows(page)).toHaveLength(5);
    expect(attr(page, '.lt-grid', 'role')).toBe('table');
    expect(heard).toEqual(['positive']);
    expect(text(page, '.accessories-count')).toBe('9 accessories');
    expect(attr(page, '.accessories-card', 'data-phase')).toBe('settled');
  });

  it('says "1 accessory" for one', async () => {
    const page = await newPage();
    await load(page, SINGLE_VIN);
    expect(rows(page)).toHaveLength(1);
    expect(text(page, '.accessories-count')).toBe('1 accessory');
  });

  it('shows the default columns only: part number, description, image; the other three leave no heading, cell or caption', async () => {
    const page = await newPage();
    await load(page, SINGLE_VIN);

    expect(headings(page)).toEqual(['Part Number', 'Description', 'Image']);
    expect(cellsOf(rows(page)[0]).map(cell => cell.getAttribute('data-label'))).toEqual(['Part Number', 'Description', 'Image']);
    expect(all(page, '.lt-ref')).toHaveLength(0);
    expect(owner(page).hiddenFields).toBe(DEFAULT_HIDDEN_FIELDS);
  });

  it('shows all six columns with hidden-fields="", and a shown column with no value reads as a dash', async () => {
    const page = await newPage(edgeCaseMocks, ALL_FIELDS);
    await load(page, MISSING_VALUES_VIN);

    expect(headings(page)).toEqual(['Part Number', 'Description', 'Image', 'Job Number', 'Invoice Number', 'Company']);
    const [first, second] = rows(page).map(cellsOf);
    // Row 1 carries neither a job nor an invoice number; row 2 an invoice number only.
    expect(first.map(cell => squash(cell.textContent)).filter((_, index) => index === 3 || index === 4)).toEqual(['—', '—']);
    expect(squash(second[3].textContent)).toBe('—');
    expect(squash(second[4].textContent)).toBe('71020002');
    expect(squash(first[5].textContent)).toBe('Sample Dealer');
  });

  it('treats a missing value and a null the same: a dash, never "null" or "undefined"', async () => {
    const page = await newPage(brokerMarketMocks, ALL_FIELDS);
    const odd = dto(2);
    const list = odd.accessories as Record<string, unknown>[];
    delete list[0].jobNumber;
    list[0].invoiceNumber = null;
    list[1].companyName = null;
    await load(page, odd);

    const [first, second] = rows(page).map(cellsOf);
    expect([first[3], first[4], second[5]].map(cell => squash(cell.textContent))).toEqual(['—', '—', '—']);
    expect(shadow(page).textContent).not.toMatch(/null|undefined/);
  });

  it('duplicate part numbers are distinct rows', async () => {
    const page = await newPage();
    await load(page, dto(3, { accessories: [accessory(1, { partNumber: 'DUP-1' }), accessory(2, { partNumber: 'DUP-1' }), accessory(3, { partNumber: 'DUP-1' })] }));
    expect(rows(page)).toHaveLength(3);
    expect(all(page, '.lt-code').map(cell => squash(cell.textContent))).toEqual(['DUP-1', 'DUP-1', 'DUP-1']);
  });

  it('an authorized vehicle with an empty list: a grey statement, the grey pill, an idle accent, never "0", never a verdict', async () => {
    const page = await newPage();
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));

    await load(page, EMPTY_VIN);

    // The headings stay; the sentence is in its own band above them.
    expect(lead(page)).toBe('columns');
    noBand(page);
    expect(bodyOpen(page)).toBe('false');
    expect(rows(page)).toHaveLength(0);
    expect(verdict(page)).toBe('idle');
    expect(pill(page)?.classList.contains('is-idle')).toBe(true);
    expect(squash(pill(page)?.textContent)).toBe(englishShared.noRecords);
    expect(count(page)).toBeNull();
    expect(pagerOpen(page)).toBe('false');
    expect(heard).toEqual([]);
  });

  it('an unauthorized vehicle: the amber notice and pill, never the list, never a count', async () => {
    const page = await newPage();
    await load(page, UNAUTHORIZED_VIN);

    expect(lead(page)).toBe('columns');
    noBand(page);
    expect(verdict(page)).toBe('neutral');
    expect(pill(page)?.classList.contains('is-neutral')).toBe(true);
    expect(rows(page)).toHaveLength(0);
    expect(count(page)).toBeNull();
    expect(bodyOpen(page)).toBe('false');

    // The same, when the DTO carries a list it should not: the panel follows isAuthorized.
    await load(page, dto(5, { isAuthorized: false }));
    expect(rows(page)).toHaveLength(0);
    expect(count(page)).toBeNull();
    noBand(page);
    expect(verdict(page)).toBe('neutral');
    expect(pagerOpen(page)).toBe('false');
  });

  it('an error: the negative pill carries it alone, the table goes back to idle, and there is no count', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);
    expect(count(page)).not.toBeNull();

    await owner(page).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();

    expect(verdict(page)).toBe('negative');
    expect(pill(page)?.classList.contains('is-negative')).toBe(true);
    expect(squash(pill(page)?.textContent)).toBe('Wrong response format');
    expect(lead(page)).toBe('columns');
    noBand(page);
    expect(attr(page, '.accessories-placeholder', 'data-open')).toBe('true');
    expect(squash(shadow(page).querySelector('.accessories-placeholder-layer[data-active="true"] .accessories-placeholder-text')?.textContent)).toBe(englishLocale.idleHint);
    expect(count(page)).toBeNull();
    expect(bodyOpen(page)).toBe('false');
  });

  it('the count and the pill never appear together, and the count is not in the head before the data has landed', async () => {
    const page = await newPage();
    const pending = owner(page).fetchVin(NINE_VIN);
    await page.waitForChanges();

    expect(owner(page).isLoading).toBe(true);
    expect(count(page)).toBeNull();
    expect(lead(page)).toBe('columns');
    expect(attr(page, '.accessories-card', 'data-phase')).toBe('busy');

    await pending;
    await page.waitForChanges();
    expect(count(page)).not.toBeNull();
    expect(pill(page)).toBeNull();
  });

  it('has no notice band in any state: the pill speaks for the vehicle', async () => {
    const page = await newPage();
    noBand(page);
    for (const vin of [NINE_VIN, UNAUTHORIZED_VIN, EMPTY_VIN]) {
      await load(page, vin);
      noBand(page);
    }
  });
});

describe('vehicle-accessories — pictures', () => {
  it('a picture that does not load is a failed state with a stand-in, not a blank or a broken icon (case 7)', async () => {
    const page = await newPage();
    await load(page, dto(2, { accessories: [accessory(1, { image: 'https://example.invalid/dead-link.png' }), accessory(2)] }));

    const cells = all(page, '.lt-media');
    expect(cells).toHaveLength(2);
    expect(cells.map(cell => cell.getAttribute('data-media'))).toEqual(['loading', 'loading']);

    // The browser reports the URL as dead: the same error event the image would raise.
    cells[0].querySelector('img').dispatchEvent(new Event('error'));
    await page.waitForChanges();

    expect(cells[0].getAttribute('data-media')).toBe('failed');
    const stand = cells[0].querySelector('.lt-media-stand');
    expect(stand?.getAttribute('role')).toBe('img');
    expect(stand?.getAttribute('aria-label')).toBe(englishLocale.imageFailed);
    expect(stand?.getAttribute('aria-hidden')).toBeNull();
    expect(cells[0].querySelector('.lt-media-open')?.hasAttribute('disabled')).toBe(true);
    // The other picture is untouched.
    expect(cells[1].getAttribute('data-media')).toBe('loading');
  });

  it('survives the panel re-rendering: a failed picture stays failed when the body is shut and reopened by a hiddenFields change', async () => {
    const page = await newPage();
    await load(page, dto(1, { accessories: [accessory(1, { image: 'https://example.invalid/dead-link.png' })] }));

    all(page, '.lt-media')[0].querySelector('img').dispatchEvent(new Event('error'));
    await page.waitForChanges();
    expect(attr(page, '.lt-media', 'data-media')).toBe('failed');

    page.root.setAttribute('hidden-fields', 'companyName');
    await page.waitForChanges();
    await settle(page);

    expect(headings(page)).toContain('Job Number');
    expect(attr(page, '.lt-media', 'data-media')).toBe('failed');
  });

  it('no picture on record is its own state, with its own words', async () => {
    const page = await newPage();
    await load(page, dto(1, { accessories: [accessory(1, { image: null })] }));

    expect(attr(page, '.lt-media', 'data-media')).toBe('none');
    expect(attr(page, '.lt-media-stand', 'aria-label')).toBe(englishLocale.noImage);
    expect(shadow(page).querySelector('.lt-media-open')).toBeNull();
  });

  it('opens the image viewer on the document body, outside the panel (a fixed overlay inside the shadow root is trapped by the panel container)', async () => {
    const page = await newPage();
    await load(page, dto(1));

    const image = shadow(page).querySelector<HTMLImageElement>('.lt-media-image');
    image.dispatchEvent(new Event('load'));
    await page.waitForChanges();

    const open = shadow(page).querySelector<HTMLButtonElement>('.lt-media-open');
    expect(open.getAttribute('aria-label')).toBe(`${englishLocale.openImage} TST-0001`);
    expect(shadow(page).querySelector('.image-viewer-expanded-image')).toBeNull();

    open.click();
    await page.waitForChanges();

    const layer = page.doc.body.querySelector('shift-image-layer') as HTMLElement & { open: boolean };
    expect(layer).not.toBeNull();
    expect(layer.open).toBe(true);
    expect(layer.getAttribute('role')).toBe('dialog');
    expect(layer.getAttribute('aria-label')).toBe(`${englishLocale.openImage} TST-0001`);
  });
});

describe('vehicle-accessories — the placeholder', () => {
  const placeholder = (page: Page) => attr(page, '.accessories-placeholder', 'data-open');
  const activeHint = (page: Page) => squash(shadow(page).querySelector('.accessories-placeholder-layer[data-active="true"] .accessories-placeholder-text')?.textContent);

  it('says "look a vehicle up" before any lookup, and holds it through the lookup under the skeleton', async () => {
    const page = await newPage();
    expect(placeholder(page)).toBe('true');
    expect(activeHint(page)).toBe(englishLocale.idleHint);
    expect(attr(page, '.accessories-placeholder-text', 'class')).toContain('shift-skeleton');

    owner(page).isLoading = true;
    await page.waitForChanges();
    expect(placeholder(page)).toBe('true');
  });

  it('describes what would be listed for a vehicle with nothing on file, shuts for rows, and goes idle when not in records', async () => {
    const page = await newPage();
    await load(page, EMPTY_VIN);
    expect(placeholder(page)).toBe('true');
    expect(activeHint(page)).toBe(englishLocale.emptyHint);

    await load(page, NINE_VIN);
    expect(placeholder(page)).toBe('false');

    await load(page, UNAUTHORIZED_VIN);
    // Not in records: the pill says so, the table is in its idle look.
    expect(placeholder(page)).toBe('true');
    expect(activeHint(page)).toBe(englishLocale.idleHint);
  });
});

describe('vehicle-accessories — the placeholder never shuts to reopen', () => {
  const placeholder = (page: Page) => attr(page, '.accessories-placeholder', 'data-open');

  it('stays open from idle through an error raised on a settled card', async () => {
    const page = await newPage();
    expect(placeholder(page)).toBe('true');

    const erroring = owner(page).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();
    expect(placeholder(page)).toBe('true');
    await erroring;
    await page.waitForChanges();
    expect(placeholder(page)).toBe('true');
  });

  it('stays open from an empty list through a clear, and opens while rows shut on an error', async () => {
    const page = await newPage();
    await load(page, EMPTY_VIN);
    const clearing = owner(page).clearData();
    await page.waitForChanges();
    expect(placeholder(page)).toBe('true');
    await clearing;

    await load(page, NINE_VIN);
    expect(placeholder(page)).toBe('false');
    const erroring = owner(page).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();
    // The rows shut and the placeholder opens in the same movement.
    expect(bodyOpen(page)).toBe('false');
    expect(placeholder(page)).toBe('true');
    await erroring;
  });
});

describe('vehicle-accessories — pages', () => {
  it('five rows or fewer: no page controls', async () => {
    const page = await newPage();

    await load(page, dto(5));
    expect(openRows(page)).toHaveLength(5);
    expect(pagerOpen(page)).toBe('false');
    expect(attr(page, '.accessories-pager', 'aria-hidden')).toBe('true');

    await load(page, dto(6));
    await settle(page);
    expect(openRows(page)).toHaveLength(5);
    expect(pagerOpen(page)).toBe('true');
    expect(attr(page, '.accessories-pager', 'aria-hidden')).toBeNull();
  });

  it('opens on five rows, the range, and wired previous and next buttons', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);

    expect(openRows(page)).toHaveLength(5);
    expect(text(page, '.accessories-count')).toBe('24 accessories');
    expect(text(page, '.accessories-page-range')).toBe('1–5 of 24');
    expect(previousButton(page).hasAttribute('disabled')).toBe(true);
    expect(nextButton(page).hasAttribute('disabled')).toBe(false);
    expect(nextButton(page).getAttribute('aria-label')).toBe(englishLocale.nextPage);
    expect(nextButton(page).getAttribute('aria-controls')).toBe('accessories-list');
    expect(sizeShown(page)).toBe('5');
  });

  it('next fades the values out, swaps the page while they are out, and fades them back in', async () => {
    const page = await newPage();
    await load(page, dto(12));

    nextButton(page).click();
    await page.waitForChanges();
    expect(internals(page).paging).toBe(true);
    expect(all(page, '.lt-slot').map(slot => slot.getAttribute('data-veil'))).toEqual(Array(5).fill('fade'));
    expect(text(page, '.lt-row .lt-code')).toBe('TST-0001');

    await settle(page);
    expect(internals(page).paging).toBe(false);
    expect(text(page, '.lt-row .lt-code')).toBe('TST-0006');
    expect(all(page, '.lt-slot').map(slot => slot.getAttribute('data-veil'))).toEqual(Array(5).fill('none'));
    expect(text(page, '.accessories-page-range')).toBe('6–10 of 12');
  });

  it('a shorter last page: the rows it drops shut in their slots, faded, and are gone a settle later', async () => {
    const page = await newPage();
    await load(page, dto(12));

    nextButton(page).click();
    await settle(page);
    nextButton(page).click();
    await page.waitForChanges();
    await new Promise(resolve => setTimeout(resolve, 600));
    await page.waitForChanges();

    const slots = all(page, '.lt-slot');
    expect(slots.map(slot => slot.getAttribute('data-open'))).toEqual(['true', 'true', 'false', 'false', 'false']);
    expect(slots.slice(2).map(slot => slot.getAttribute('data-veil'))).toEqual(['fade', 'fade', 'fade']);
    expect(slots.slice(2).map(slot => slot.getAttribute('aria-hidden'))).toEqual(['true', 'true', 'true']);

    await settle(page);
    expect(all(page, '.lt-slot')).toHaveLength(2);
    expect(nextButton(page).hasAttribute('disabled')).toBe(true);
  });

  it('a bigger page from the first: nothing fades, the new rows arrive shut and then open', async () => {
    const page = await newPage();
    await load(page, dto(12));

    await chooseSize(page, '10');
    expect(internals(page).paging).toBe(false);
    const slots = all(page, '.lt-slot');
    expect(slots).toHaveLength(10);
    expect(slots.map(slot => slot.getAttribute('data-open'))).toEqual([...Array(5).fill('true'), ...Array(5).fill('false')]);

    await settle(page);
    expect(all(page, '.lt-slot').map(slot => slot.getAttribute('data-open'))).toEqual(Array(10).fill('true'));
  });

  it('keeps the chosen size in localStorage and opens on it next time', async () => {
    const store = new Map<string, string>();
    const storage = { getItem: (key: string) => store.get(key) ?? null, setItem: (key: string, value: string) => void store.set(key, value) };
    const previous = Object.getOwnPropertyDescriptor(globalThis, 'localStorage');
    Object.defineProperty(globalThis, 'localStorage', { value: storage, configurable: true });

    try {
      const page = await newPage();
      await load(page, MANY_VIN);
      await chooseSize(page, '15');
      expect(store.get(PAGE_SIZE_STORAGE_KEY)).toBe('15');

      const next = await newPage();
      await load(next, MANY_VIN);
      expect(internals(next).pageSize).toBe(15);
      expect(sizeShown(next)).toBe('15');
      expect(openRows(next)).toHaveLength(15);
    } finally {
      if (previous) Object.defineProperty(globalThis, 'localStorage', previous);
      else delete (globalThis as { localStorage?: unknown }).localStorage;
    }
  });

  it('a storage that throws leaves the default', async () => {
    const previous = Object.getOwnPropertyDescriptor(globalThis, 'localStorage');
    Object.defineProperty(globalThis, 'localStorage', {
      configurable: true,
      get: () => {
        throw new Error('blocked');
      },
    });

    try {
      const page = await newPage();
      await load(page, MANY_VIN);
      expect(internals(page).pageSize).toBe(5);
      await chooseSize(page, '10');
      expect(internals(page).pageSize).toBe(10);
    } finally {
      if (previous) Object.defineProperty(globalThis, 'localStorage', previous);
      else delete (globalThis as { localStorage?: unknown }).localStorage;
    }
  });

  it('All shows every row; a new vehicle starts again on its first page', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);
    nextButton(page).click();
    await settle(page);
    expect(internals(page).page).toBe(1);

    await load(page, dto(12));
    expect(internals(page).page).toBe(0);
    expect(text(page, '.lt-row .lt-code')).toBe('TST-0001');

    await chooseSize(page, 'all');
    await settle(page);
    expect(openRows(page)).toHaveLength(12);
    expect(text(page, '.accessories-page-range')).toBe('1–12 of 12');
  });

  it('the controls are disabled while a lookup is in flight: the rows on screen are the last vehicle', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);

    owner(page).isLoading = true;
    await page.waitForChanges();
    expect(nextButton(page).hasAttribute('disabled')).toBe(true);
    expect(sizeSelect(page).hasAttribute('disabled')).toBe(true);
    expect(pagerOpen(page)).toBe('true');
  });

  it('brings the controls back into view when a shorter page would lift them off the top of the screen, and follows the page', async () => {
    const spy = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => () => undefined);
    try {
      const page = await newPage();
      await load(page, dto(12));
      await chooseSize(page, '10');
      await settle(page);

      all(page, '.lt-slot').forEach(slot => withRect(slot, { top: 0, height: 80 }));
      withRect(shadow(page).querySelector('.accessories-pager'), { top: 300, height: 48 });

      nextButton(page).click();
      await settle(page);
      await page.waitForChanges();

      expect(spy).toHaveBeenCalledTimes(1);
      expect(spy.mock.calls[0][0]).toBe(shadow(page).querySelector('.accessories-pager'));
    } finally {
      spy.mockRestore();
    }
  });

  it('leaves the page alone when the controls stay on screen', async () => {
    const spy = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => () => undefined);
    try {
      const page = await newPage();
      await load(page, dto(12));
      await chooseSize(page, '10');
      await settle(page);

      all(page, '.lt-slot').forEach(slot => withRect(slot, { top: 0, height: 80 }));
      withRect(shadow(page).querySelector('.accessories-pager'), { top: 2000, height: 48 });

      nextButton(page).click();
      await settle(page);

      expect(spy).not.toHaveBeenCalled();
    } finally {
      spy.mockRestore();
    }
  });
});

describe('vehicle-accessories — rows stay through a lookup (owner, 2026-10-25)', () => {
  it('in flight the body stays open and every row goes under the skeleton where it stands', async () => {
    const page = await newPage();
    await load(page, dto(5));

    owner(page).isLoading = true;
    await page.waitForChanges();

    expect(bodyOpen(page)).toBe('true');
    expect(all(page, '.lt-slot').map(slot => slot.getAttribute('data-open'))).toEqual(Array(5).fill('true'));
    expect(all(page, '.lt-row').every(row => row.classList.contains('loading'))).toBe(true);
    expect(all(page, '.lt-value').every(value => value.classList.contains('shift-skeleton'))).toBe(true);
    expect(attr(page, '.lt-grid', 'aria-busy')).toBe('true');
  });

  it('a shorter answer: the rows that stay keep their slots, the rest leave under the cover as their slots shut', async () => {
    const page = await newPage();
    await load(page, dto(5));
    const firstSlot = all(page, '.lt-slot')[0];

    await load(page, dto(3, { vin: 'ZS8TESTVINSHORTER' }));

    const slots = all(page, '.lt-slot');
    expect(slots[0]).toBe(firstSlot);
    expect(slots.map(slot => slot.getAttribute('data-open'))).toEqual(['true', 'true', 'true', 'false', 'false']);
    expect(slots.map(slot => slot.getAttribute('data-veil'))).toEqual(['none', 'none', 'none', 'cover', 'cover']);
    expect(slots.slice(3).every(slot => slot.querySelector('.lt-row').classList.contains('loading'))).toBe(true);

    await settle(page);
    expect(all(page, '.lt-slot')).toHaveLength(3);
  });

  it('a longer answer: the new rows arrive shut and open on the next frame', async () => {
    const page = await newPage();
    await load(page, dto(2));

    await load(page, dto(4, { vin: 'ZS8TESTVINLONGER00' }));
    expect(all(page, '.lt-slot').map(slot => slot.getAttribute('data-open'))).toEqual(['true', 'true', 'false', 'false']);

    await settle(page);
    expect(all(page, '.lt-slot').map(slot => slot.getAttribute('data-open'))).toEqual(['true', 'true', 'true', 'true']);
  });

  it('an answer with no rows: the body shuts over the covered rows, which stay open inside it until it has shut', async () => {
    const page = await newPage();
    await load(page, dto(3));

    await load(page, EMPTY_VIN);

    expect(bodyOpen(page)).toBe('false');
    noBand(page);
    const slots = all(page, '.lt-slot');
    expect(slots).toHaveLength(3);
    expect(slots.map(slot => slot.getAttribute('data-open'))).toEqual(['true', 'true', 'true']);
    expect(slots.map(slot => slot.getAttribute('data-veil'))).toEqual(['cover', 'cover', 'cover']);
  });
});

describe('vehicle-accessories — hiddenFields', () => {
  it('keeps the public surface: the default hides job, invoice and company', async () => {
    const page = await newSpecPage({ components: [VehicleAccessories], html: '<vehicle-accessories></vehicle-accessories>' });
    expect((page.root as unknown as { hiddenFields: string }).hiddenFields).toBe('jobNumber,invoiceNumber,companyName');
  });

  it('a hidden field removes its column: image hidden leaves no media cell, part number hidden leaves no code cell', async () => {
    const page = await newPage(brokerMarketMocks, 'hidden-fields="image,partNumber"');
    await load(page, SINGLE_VIN);

    expect(headings(page)).toEqual(['Description', 'Job Number', 'Invoice Number', 'Company']);
    expect(all(page, '.lt-media')).toHaveLength(0);
    expect(all(page, '.lt-code')).toHaveLength(0);
    expect(rows(page)).toHaveLength(1);
  });

  it('changes live: the body shuts over the old columns, the layout swaps while it is shut, and it reopens, with no loading state', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));
    const tracks = () => (attr(page, '.lt-grid', 'style') ?? '').replace(/\s+/g, ' ');
    const before = tracks();

    page.root.setAttribute('hidden-fields', '');
    await page.waitForChanges();

    expect(internals(page).reconfiguring).toBe(true);
    expect(bodyOpen(page)).toBe('false');
    expect(attr(page, '.lt-body', 'aria-hidden')).toBe('true');
    expect(pagerOpen(page)).toBe('false');
    // Still the old layout while it shuts.
    expect(headings(page)).toEqual(['Part Number', 'Description', 'Image']);
    expect(tracks()).toBe(before);
    expect(attr(page, '.lt-columns', 'data-reshaping')).toBe('true');
    // Not a lookup: nothing is loading, the sheen is off, the head is untouched.
    expect(owner(page).isLoading).toBe(false);
    expect(internals(page).leaving).toBe(false);
    expect(attr(page, '.accessories-card', 'data-phase')).toBe('settled');
    expect(attr(page, '.lookup-card', 'data-phase')).toBe('settled');
    expect(lead(page)).toBe('columns');
    expect(attr(page, '.lt-grid', 'role')).toBeNull();

    await settle(page);

    expect(internals(page).reconfiguring).toBe(false);
    expect(headings(page)).toEqual(['Part Number', 'Description', 'Image', 'Job Number', 'Invoice Number', 'Company']);
    expect(tracks()).not.toBe(before);
    expect(bodyOpen(page)).toBe('true');
    expect(attr(page, '.lt-columns', 'data-reshaping')).toBe('false');
    expect(attr(page, '.lt-grid', 'role')).toBe('table');
    expect(rows(page)).toHaveLength(5);
    expect(heard).toEqual([]);
    // The total is the vehicle's, not the layout's.
    expect(text(page, '.accessories-count')).toBe('9 accessories');
  });

  it('keeps the count, the page and the page size through a column change', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);
    await chooseSize(page, '10');
    nextButton(page).click();
    await settle(page);

    page.root.setAttribute('hidden-fields', 'image');
    await page.waitForChanges();
    expect(text(page, '.accessories-count')).toBe('24 accessories');
    await settle(page);

    expect(text(page, '.accessories-count')).toBe('24 accessories');
    expect(openRows(page)).toHaveLength(10);
    expect(text(page, '.accessories-page-range')).toBe('11–20 of 24');
  });

  it('does nothing for a name it does not know, or for the same columns written differently', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);

    page.root.setAttribute('hidden-fields', 'companyName, jobNumber,invoiceNumber, nonsense');
    await page.waitForChanges();
    expect(internals(page).reconfiguring).toBe(false);
    expect(bodyOpen(page)).toBe('true');
  });

  it('swaps at once when nothing is open: idle, or a notice', async () => {
    const page = await newPage();
    page.root.setAttribute('hidden-fields', '');
    await page.waitForChanges();
    expect(internals(page).reconfiguring).toBe(false);

    await load(page, EMPTY_VIN);
    page.root.setAttribute('hidden-fields', 'image');
    await page.waitForChanges();
    expect(internals(page).reconfiguring).toBe(false);

    await load(page, NINE_VIN);
    expect(headings(page)).toEqual(['Part Number', 'Description', 'Job Number', 'Invoice Number', 'Company']);
  });

  it('cancels a change that returns to the applied columns before it lands', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);

    page.root.setAttribute('hidden-fields', '');
    await page.waitForChanges();
    expect(internals(page).reconfiguring).toBe(true);

    page.root.setAttribute('hidden-fields', DEFAULT_HIDDEN_FIELDS);
    await page.waitForChanges();
    expect(internals(page).reconfiguring).toBe(false);
    await settle(page);

    expect(headings(page)).toEqual(['Part Number', 'Description', 'Image']);
    expect(bodyOpen(page)).toBe('true');
  });

  it('hiding every column is a statement in the strip, not an empty table; the count stays', async () => {
    const page = await newPage(brokerMarketMocks, 'hidden-fields="partNumber,description,image,jobNumber,invoiceNumber,companyName"');
    await load(page, NINE_VIN);

    expect(lead(page)).toBe('notice');
    expect(text(page, '.lt-notice')).toBe(englishLocale.noFieldsShown);
    expect(attr(page, '.lt-notice', 'class')).toContain('is-idle');
    expect(bodyOpen(page)).toBe('false');
    expect(verdict(page)).toBe('positive');
    expect(text(page, '.accessories-count')).toBe('9 accessories');
  });

  it('hiding every column says so before any lookup too: there are no headings to show while idle or loading', async () => {
    const page = await newPage(brokerMarketMocks, 'hidden-fields="partNumber,description,image,jobNumber,invoiceNumber,companyName"');
    expect(lead(page)).toBe('notice');
    expect(text(page, '.lt-notice')).toBe(englishLocale.noFieldsShown);

    const pending = owner(page).fetchVin(NINE_VIN);
    await page.waitForChanges();
    expect(lead(page)).toBe('notice');
    await pending;
  });

  it('keeps the same heading elements from idle through loading to rows: one structure, nothing re-animates', async () => {
    const page = await newPage();
    const strip = shadow(page).querySelector('.lt-columns');
    const first = shadow(page).querySelector('.lt-columns > .lt-cell');
    const seen: Array<string | null | undefined> = [attr(page, '.lt-lead', 'data-lead')];

    const pending = owner(page).fetchVin(NINE_VIN);
    await page.waitForChanges();
    seen.push(attr(page, '.lt-lead', 'data-lead'));
    await pending;
    await page.waitForChanges();
    seen.push(attr(page, '.lt-lead', 'data-lead'));

    expect(seen).toEqual(['columns', 'columns', 'columns']);
    expect(shadow(page).querySelector('.lt-columns')).toBe(strip);
    expect(shadow(page).querySelector('.lt-columns > .lt-cell')).toBe(first);
    // Roles arrive with the open rows.
    expect(attr(page, '.lt-columns', 'role')).toBe('row');
    expect(shadow(page).querySelector('.lt-columns')?.hasAttribute('aria-hidden')).toBe(false);
  });

  it('keeps the headings through a lookup from one notice to another; the band shuts in flight and reopens on the answer', async () => {
    const page = await newPage();
    await load(page, UNAUTHORIZED_VIN);
    expect(lead(page)).toBe('columns');
    noBand(page);

    const pending = owner(page).fetchVin(EMPTY_VIN);
    await page.waitForChanges();
    expect(lead(page)).toBe('columns');
    noBand(page);

    await pending;
    await page.waitForChanges();
    expect(lead(page)).toBe('columns');
    noBand(page);
  });
});

describe('vehicle-accessories — leaving and entering', () => {
  it('goes from loaded to loading to loaded: the rows stay on screen under the skeleton, the headings stay, the head content leaves', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);
    expect(rows(page)).toHaveLength(5);

    const pending = owner(page).fetchVin(SINGLE_VIN);
    await page.waitForChanges();

    expect(attr(page, '.accessories-card', 'data-phase')).toBe('busy');
    expect(attr(page, '.lt-grid', 'data-phase')).toBe('busy');
    expect(shadow(page).querySelector('.lookup-panel')?.classList.contains('loading')).toBe(true);
    expect(lead(page)).toBe('columns');
    // Owner, 2026-10-25: in flight the rows are not shut; each value is covered where it stands.
    expect(bodyOpen(page)).toBe('true');
    expect(rows(page)).toHaveLength(5);
    expect(rows(page).every(row => row.classList.contains('loading'))).toBe(true);
    expect(attr(page, '.lt-grid', 'aria-busy')).toBe('true');

    await pending;
    await page.waitForChanges();

    expect(attr(page, '.accessories-card', 'data-phase')).toBe('settled');
    expect(lead(page)).toBe('columns');
    expect(bodyOpen(page)).toBe('true');
    expect(openRows(page)).toHaveLength(1);
    expect(text(page, '.accessories-count')).toBe('1 accessory');

    await settle(page);
    expect(rows(page)).toHaveLength(1);
  });

  it('goes from rows to a statement and back: the rows are retained while the body shuts, the notice keeps its words as it fades', async () => {
    const page = await newPage();
    await load(page, UNAUTHORIZED_VIN);

    await load(page, NINE_VIN);
    expect(lead(page)).toBe('columns');
    noBand(page);

    await load(page, EMPTY_VIN);
    noBand(page);
    // The rows were retained as a kind only for a settle after the body shut.
    await settle(page);
    expect(rows(page)).toHaveLength(0);
    expect(attr(page, '.lt-body', 'data-body')).toBe('none');
  });

  it('clears through the leave and empties unseen', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);

    const pending = owner(page).clearData();
    await page.waitForChanges();
    expect(internals(page).leaving).toBe(true);
    expect(attr(page, '.accessories-card', 'data-phase')).toBe('busy');
    expect(bodyOpen(page)).toBe('false');
    expect(rows(page)).toHaveLength(5);
    expect(count(page)?.textContent).toContain('9');

    await pending;
    await page.waitForChanges();

    expect(internals(page).leaving).toBe(false);
    expect(verdict(page)).toBe('idle');
    expect(count(page)).toBeNull();
    expect(pill(page)).toBeNull();
    expect(lead(page)).toBe('columns');
    expect(attr(page, '.accessories-card', 'data-phase')).toBe('settled');
  });

  it('enters an error raised on a settled card through the leave', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);

    const seen: boolean[] = [];
    const pending = owner(page).setErrorMessage('wrongResponseFormat');
    seen.push(internals(page).leaving);
    seen.push(owner(page).isError);
    await pending;
    await page.waitForChanges();

    expect(seen).toEqual([true, false]);
    expect(internals(page).leaving).toBe(false);
    expect(pill(page)?.classList.contains('is-negative')).toBe(true);
  });

  it('lets a lookup started during a clear win, and settles on it (the leave race)', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);

    const clearing = owner(page).clearData();
    const fetching = owner(page).fetchVin(NINE_VIN);
    await Promise.all([clearing, fetching]);
    await settle(page);

    expect(owner(page).isLoading).toBe(false);
    expect(internals(page).leaving).toBe(false);
    expect(attr(page, '.accessories-card', 'data-phase')).toBe('settled');
    expect(verdict(page)).toBe('positive');
    expect(rows(page)).toHaveLength(5);
    expect(text(page, '.accessories-count')).toBe('9 accessories');
  });

  it('lets a lookup started during an error leave win', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);

    const erroring = owner(page).setErrorMessage('wrongResponseFormat');
    const fetching = owner(page).fetchVin(SINGLE_VIN);
    await Promise.all([erroring, fetching]);
    // The arrival renders here; the rows it drops leave over the settle after it.
    await page.waitForChanges();
    await settle(page);

    expect(owner(page).isError).toBe(false);
    expect(internals(page).leaving).toBe(false);
    expect(verdict(page)).toBe('positive');
    expect(rows(page)).toHaveLength(1);
  });

  it('announces each verdict once: positive, neutral, idle on clear, negative on error', async () => {
    const page = await newPage();
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));

    await load(page, NINE_VIN);
    await load(page, UNAUTHORIZED_VIN);
    await owner(page).clearData();
    await page.waitForChanges();
    await owner(page).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();

    expect(heard).toEqual(['positive', 'neutral', 'idle', 'negative']);
  });

  it('touches no loading flag when the language changes', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);

    await owner(page).changeLanguage('ar');
    await page.waitForChanges();

    expect(owner(page).isLoading).toBe(false);
    expect(internals(page).leaving).toBe(false);
    expect(bodyOpen(page)).toBe('true');
    expect(attr(page, '.lookup-panel', 'dir')).toBe('rtl');
    expect(text(page, '.lookup-title')).toBe(arabicLocale.vehicleAccessories);
    // Same keys, placeholder text for now: the count still carries its number.
    expect(text(page, '.accessories-count')).toContain('24');
  });
});

describe('vehicle-accessories — the owner', () => {
  it('keeps the public surface: every prop and its default', async () => {
    const page = await newSpecPage({ components: [VehicleAccessories], html: '<vehicle-accessories></vehicle-accessories>' });
    const element = page.root as unknown as Record<string, unknown>;

    expect(element.language).toBe('en');
    expect(element.coreOnly).toBe(false);
    expect(element.isDev).toBeUndefined();
    expect(element.today).toBeUndefined();
    expect(element.disableVinValidation).toBe(false);
    expect(element.baseUrl).toBeUndefined();
    expect(element.headers).toEqual({});
    expect(element.queryString).toBe('');
    expect(element.errorCallback).toBeUndefined();
    expect(element.loadingStateChange).toBeUndefined();
    expect(element.loadedResponse).toBeUndefined();
    expect(element.hiddenFields).toBe(DEFAULT_HIDDEN_FIELDS);
  });

  it('keeps the public surface: every method is there and async, and the callbacks fire', async () => {
    const page = await newPage();
    const element = page.root as unknown as Record<string, unknown>;

    for (const method of ['setBlazorRef', 'setMockData', 'fetchVin', 'setErrorMessage', 'clearData']) {
      expect([method, typeof element[method]]).toEqual([method, 'function']);
    }
    expect(owner(page).setBlazorRef(undefined as never)).toBeInstanceOf(Promise);
    expect(owner(page).clearData()).toBeInstanceOf(Promise);

    const loading: boolean[] = [];
    const loaded: string[] = [];
    element.loadingStateChange = (value: boolean) => loading.push(value);
    element.loadedResponse = (response: { vin: string }) => loaded.push(response.vin);
    await page.waitForChanges();

    await owner(page).fetchVin(NINE_VIN);
    await page.waitForChanges();
    expect(loading).toEqual([true, false]);
    expect(loaded).toEqual([NINE_VIN]);
  });

  it('marks itself untranslatable, carries the direction on the layout, and passes coreOnly through', async () => {
    const page = await newPage();
    expect(page.root.getAttribute('translate')).toBe('no');
    expect(attr(page, '.lookup-panel', 'dir')).toBe('ltr');
    expect(shadow(page).querySelector('.lookup-card')).not.toBeNull();
    expect(shadow(page).querySelector('.lookup-core')).toBeNull();

    const core = await newPage(brokerMarketMocks, 'core-only="true"');
    expect(shadow(core).querySelector('.lookup-core')).not.toBeNull();
    expect(shadow(core).querySelector('.lookup-card')).toBeNull();
    expect(attr(core, '.lookup-core', 'dir')).toBe('ltr');
    expect(shadow(core).querySelector('.accessories-card')).not.toBeNull();

    await load(core, NINE_VIN);
    expect(rows(core)).toHaveLength(5);
  });

  it('puts no heading element in the host page and no information-table in its tree', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);
    expect(all(page, 'h1, h2, h3, h4, h5, h6')).toHaveLength(0);
    expect(shadow(page).querySelector('information-table')).toBeNull();
  });
});

describe('vehicle-accessories — under vehicle-lookup', () => {
  it('receives hiddenFields through children-props, and keeps its default otherwise', async () => {
    const page = await newSpecPage({
      components: [VehicleLookup, VehicleAccessories],
      html: `<vehicle-lookup active-element="vehicle-accessories" children-props='{"vehicle-accessories":{"hiddenFields":"image"}}'></vehicle-lookup>`,
    });

    const panel = page.root.shadowRoot.getElementById('vehicle-accessories') as unknown as PanelHost;
    expect(panel.hiddenFields).toBe('image');
    expect(panel.coreOnly).toBe(true);
    expect(panel.shadowRoot.querySelector('.accessories-card')).not.toBeNull();

    const plain = await newSpecPage({
      components: [VehicleLookup, VehicleAccessories],
      html: '<vehicle-lookup active-element="vehicle-accessories"></vehicle-lookup>',
    });
    expect((plain.root.shadowRoot.getElementById('vehicle-accessories') as unknown as PanelHost).hiddenFields).toBe(DEFAULT_HIDDEN_FIELDS);
  });
});

describe('vehicle-accessories — the stylesheet contract', () => {
  const css = readFileSync(join(__dirname, 'vehicle-accessories.css'), 'utf8');
  const tableCss = readFileSync(join(__dirname, '../../features/lookup-table/lookup-table.css'), 'utf8');

  it('imports the table fragment after the tokens, the wrapper and the head, and never lookup-motion.css', () => {
    const order = ['base.css', 'lookup-tokens.css', 'shift-skeleton.css', 'vehicle-info-layout.css', 'lookup-head.css', 'lookup-table.css'].map(file => css.indexOf(`/${file}';`));
    expect(order.every(index => index >= 0)).toBe(true);
    expect([...order].sort((a, b) => a - b)).toEqual(order);
    expect(css).not.toMatch(/@import[^;]*lookup-motion\.css/);
  });

  it('is plain CSS: no Tailwind, no @apply, no reset of the host that would drop the container', () => {
    expect(css.startsWith('/* tailwind: off */')).toBe(true);
    expect(css).not.toContain('@apply');
    expect(css).not.toMatch(/all:\s*initial/);
  });

  it('compact rows: 65px, a 48px thumbnail and 8px padding; the table keeps its comfortable default; a cards layout at 720px and under', () => {
    expect(css).toMatch(/--lt-row-min:\s*65px/);
    expect(css).toMatch(/--lt-media-size:\s*48px/);
    expect(css).toMatch(/--lt-cell-pad-block:\s*8px/);
    expect(css).toMatch(/\.accessories-page-button\s*{[^}]*width:\s*40px/);
    expect(tableCss).toMatch(/--lt-media-size:\s*56px/);
    expect(tableCss).toContain('@container (max-width: 720px)');
  });
});
