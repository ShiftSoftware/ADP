import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import * as revealBelowModule from '~lib/reveal-below';

import { VehicleAccessories } from './vehicle-accessories';
import { VehicleLookup } from './vehicle-lookup';
import {
  AccessoriesLocale,
  COLLAPSED_ROWS,
  DEFAULT_HIDDEN_FIELDS,
  FIELDS,
  PanelState,
  accessoryColumns,
  collapseNeedsReveal,
  columnCount,
  countText,
  hasRecords,
  isDisclosable,
  leadNotice,
  listed,
  panelBody,
  panelLead,
  pluralCategory,
  sameColumns,
  showAllText,
  totalCount,
  visibleRows,
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

  it('puts the strip on the skeleton while busy or idle, on headings above rows, and on a notice otherwise', () => {
    expect(panelLead(state(), false)).toBe('columns');
    expect(panelLead(state(), true)).toBe('skeleton');
    expect(panelLead(state({ vehicleLoaded: false }), false)).toBe('skeleton');
    expect(panelLead(state({ accessories: [] }), false)).toBe('notice');
    expect(panelLead(state({ authorized: false }), false)).toBe('notice');
    expect(panelLead(state({ hidden: parseHiddenFields(FIELDS.join(',')) }), false)).toBe('notice');
    // The state an error leaves behind is not a vehicle: the strip stays on the skeleton, the pill speaks.
    expect(panelLead(state({ vehicleLoaded: false, accessories: undefined }), false)).toBe('skeleton');
  });

  it('says unauthorized in amber, an empty list in grey, never a verdict, and nothing above rows or before a lookup', () => {
    expect(leadNotice(state({ authorized: false }), locale)).toEqual({ tone: 'neutral', text: locale.unauthorizedNotice });
    // An unauthorized vehicle that carries a list in the DTO still says "not in the records": the list is a data fault.
    expect(leadNotice(state({ authorized: false, accessories: [accessory(1)] as unknown as PanelState['accessories'] }), locale)?.tone).toBe('neutral');
    expect(leadNotice(state({ accessories: [] }), locale)).toEqual({ tone: 'idle', text: locale.noAccessories });
    expect(leadNotice(state({ accessories: null }), locale)?.tone).toBe('idle');
    expect(leadNotice(state({ hidden: parseHiddenFields(FIELDS.join(',')) }), locale)).toEqual({ tone: 'idle', text: locale.noFieldsShown });
    expect(leadNotice(state(), locale)).toBeNull();
    expect(leadNotice(state({ vehicleLoaded: false }), locale)).toBeNull();

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

  it('disclosure: collapses a list longer than ten to its first ten, and leaves ten or fewer alone', () => {
    const rows = (n: number) => Array.from({ length: n }, (_, index) => accessory(index + 1)) as unknown as AccessoryDTO[];

    expect(COLLAPSED_ROWS).toBe(10);
    expect(isDisclosable(10)).toBe(false);
    expect(isDisclosable(11)).toBe(true);
    expect(visibleRows(rows(10), false)).toHaveLength(10);
    expect(visibleRows(rows(11), false)).toHaveLength(10);
    expect(visibleRows(rows(11), true)).toHaveLength(11);
    expect(visibleRows(rows(24), false).map(row => row.partNumber)).toEqual(rows(10).map(row => row.partNumber));
    expect(visibleRows([], false)).toEqual([]);
  });

  it('asks the page to follow a collapse only when the rows that go would lift the button off the top of the screen', () => {
    expect(collapseNeedsReveal(900, 400)).toBe(false);
    expect(collapseNeedsReveal(300, 400)).toBe(true);
    expect(collapseNeedsReveal(400, 400)).toBe(false);
    expect(collapseNeedsReveal(0, 0)).toBe(false);
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
    showAll: 'all[{count}]',
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

  it('puts the count in the disclosure button through the same strings', () => {
    expect(showAllText(24, locale, 'en')).toBe('Show all 24 accessories');
    expect(showAllText(11, forms, 'ar')).toBe('all[many:11]');
    expect(showAllText(23, forms, 'ru')).toBe('all[few:23]');
  });

  it('every language carries every key of English, and every count form keeps its {count}', () => {
    const keys = Object.keys(englishLocale).sort();
    for (const other of [arabicLocale, kurdishLocale, russianLocale]) expect(Object.keys(other).sort()).toEqual(keys);

    for (const file of [englishLocale, arabicLocale, kurdishLocale, russianLocale] as Record<string, string>[]) {
      for (const key of keys.filter(k => k.startsWith('accessoryCount'))) expect([key, file[key].includes('{count}')]).toEqual([key, true]);
      expect(file.showAll.includes('{count}')).toBe(true);
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
    components: [VehicleAccessories],
    html: `<vehicle-accessories is-dev="true" ${attributes}></vehicle-accessories>`,
  });
  await (page.rootInstance as VehicleAccessories).setMockData(mocks as never);
  return page;
};

type Page = Awaited<ReturnType<typeof newPage>>;

const owner = (page: Page) => page.rootInstance as VehicleAccessories;
const internals = (page: Page) => page.rootInstance as unknown as { leaving: boolean; reconfiguring: boolean; expanded: boolean; collapsing: boolean };
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
const moreOpen = (page: Page) => attr(page, '.accessories-more', 'data-open');
const button = (page: Page) => shadow(page).querySelector<HTMLButtonElement>('.accessories-more-button');
/** mock-doc has no layout: a test gives an element the box it wants the owner to measure. */
const withRect = (element: Element, rect: { top: number; height: number }) => {
  (element as unknown as { getBoundingClientRect: () => unknown }).getBoundingClientRect = () => rect;
};
type PanelHost = { hiddenFields: string; coreOnly: boolean; shadowRoot: ShadowRoot };
const cellsOf = (row: HTMLElement) => Array.from(row.querySelectorAll<HTMLElement>('.lt-cell'));

describe('vehicle-accessories — the states', () => {
  it('opens idle: a skeleton strip, no rows, no pill, no count, a grey accent and shut disclosure', async () => {
    const page = await newPage();

    expect(verdict(page)).toBe('idle');
    expect(lead(page)).toBe('skeleton');
    expect(attr(page, '.accessories-card', 'data-body')).toBe('none');
    expect(bodyOpen(page)).toBe('false');
    expect(rows(page)).toHaveLength(0);
    expect(pill(page)).toBeNull();
    expect(count(page)).toBeNull();
    expect(moreOpen(page)).toBe('false');
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
    expect(rows(page)).toHaveLength(9);
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

    expect(lead(page)).toBe('notice');
    expect(text(page, '.lt-notice')).toBe(englishLocale.noAccessories);
    expect(attr(page, '.lt-notice', 'class')).toContain('is-idle');
    expect(attr(page, '.lt-notice', 'data-active')).toBe('true');
    expect(bodyOpen(page)).toBe('false');
    expect(rows(page)).toHaveLength(0);
    expect(verdict(page)).toBe('idle');
    expect(pill(page)?.classList.contains('is-idle')).toBe(true);
    expect(squash(pill(page)?.textContent)).toBe(englishShared.noRecords);
    expect(count(page)).toBeNull();
    expect(moreOpen(page)).toBe('false');
    expect(heard).toEqual([]);
  });

  it('an unauthorized vehicle: the amber notice and pill, never the list, never a count', async () => {
    const page = await newPage();
    await load(page, UNAUTHORIZED_VIN);

    expect(lead(page)).toBe('notice');
    expect(text(page, '.lt-notice')).toBe(englishLocale.unauthorizedNotice);
    expect(attr(page, '.lt-notice', 'class')).toContain('is-neutral');
    expect(verdict(page)).toBe('neutral');
    expect(pill(page)?.classList.contains('is-neutral')).toBe(true);
    expect(rows(page)).toHaveLength(0);
    expect(count(page)).toBeNull();
    expect(bodyOpen(page)).toBe('false');

    // The same, when the DTO carries a list it should not: the panel follows isAuthorized.
    await load(page, dto(5, { isAuthorized: false }));
    expect(rows(page)).toHaveLength(0);
    expect(count(page)).toBeNull();
    expect(lead(page)).toBe('notice');
    expect(verdict(page)).toBe('neutral');
    expect(moreOpen(page)).toBe('false');
  });

  it('an error: the negative pill carries it, the strip stays on the skeleton, and there is no count', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);
    expect(count(page)).not.toBeNull();

    await owner(page).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();

    expect(verdict(page)).toBe('negative');
    expect(pill(page)?.classList.contains('is-negative')).toBe(true);
    expect(squash(pill(page)?.textContent)).toBe('Wrong response format');
    expect(lead(page)).toBe('skeleton');
    expect(count(page)).toBeNull();
    expect(bodyOpen(page)).toBe('false');
  });

  it('the count and the pill never appear together, and the count is not in the head before the data has landed', async () => {
    const page = await newPage();
    const pending = owner(page).fetchVin(NINE_VIN);
    await page.waitForChanges();

    expect(owner(page).isLoading).toBe(true);
    expect(count(page)).toBeNull();
    expect(lead(page)).toBe('skeleton');
    expect(attr(page, '.accessories-card', 'data-phase')).toBe('busy');

    await pending;
    await page.waitForChanges();
    expect(count(page)).not.toBeNull();
    expect(pill(page)).toBeNull();
  });

  it('marks the notice layer role=status, aria-hidden when it is not the active layer', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);
    expect(attr(page, '.lt-notice', 'aria-hidden')).toBe('true');
    expect(attr(page, '.lt-notice', 'data-active')).toBe('false');

    await load(page, UNAUTHORIZED_VIN);
    expect(attr(page, '.lt-notice', 'role')).toBe('status');
    expect(attr(page, '.lt-notice', 'aria-hidden')).toBeNull();
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

  it('opens the existing image viewer from a loaded picture', async () => {
    const page = await newPage();
    await load(page, dto(1));

    const image = shadow(page).querySelector<HTMLImageElement>('.lt-media-image');
    image.dispatchEvent(new Event('load'));
    await page.waitForChanges();

    const open = shadow(page).querySelector<HTMLButtonElement>('.lt-media-open');
    expect(open.getAttribute('aria-label')).toBe(`${englishLocale.openImage} TST-0001`);
    expect(shadow(page).querySelector('.image-viewer-expanded-image')).not.toBeNull();
  });
});

describe('vehicle-accessories — the disclosure', () => {
  it('ten rows or fewer: no button to press', async () => {
    const page = await newPage();

    await load(page, dto(10));
    expect(rows(page)).toHaveLength(10);
    expect(moreOpen(page)).toBe('false');
    expect(attr(page, '.accessories-more', 'aria-hidden')).toBe('true');

    await load(page, dto(11));
    expect(rows(page)).toHaveLength(10);
    expect(moreOpen(page)).toBe('true');
    expect(attr(page, '.accessories-more', 'aria-hidden')).toBeNull();
  });

  it('more than ten: the table gets the first ten, the button reads "Show all N accessories" and is a real, wired button', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);

    expect(rows(page)).toHaveLength(COLLAPSED_ROWS);
    expect(text(page, '.accessories-count')).toBe('24 accessories');

    const toggle = button(page);
    expect(toggle.tagName).toBe('BUTTON');
    expect(toggle.getAttribute('type')).toBe('button');
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(toggle.getAttribute('aria-controls')).toBe('accessories-list');
    expect(attr(page, '#accessories-list', 'class')).toContain('accessories-fold');
    expect(shadow(page).querySelector('#accessories-list .lt-grid')).not.toBeNull();

    const labels = all(page, '.accessories-more-label');
    expect(labels.map(label => label.getAttribute('data-active'))).toEqual(['true', 'false']);
    expect(squash(labels[0].textContent)).toBe('Show all 24 accessories');
    expect(squash(labels[1].textContent)).toBe('Show fewer');
    // Only the label that shows is exposed, so the button's name is one phrase.
    expect(labels.map(label => label.getAttribute('aria-hidden'))).toEqual([null, 'true']);
  });

  it('pressing it shows the rest in place and reads "Show fewer"; pressing again puts them away after the fold has shut', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);

    button(page).click();
    await page.waitForChanges();

    expect(rows(page)).toHaveLength(24);
    expect(button(page).getAttribute('aria-expanded')).toBe('true');
    expect(all(page, '.accessories-more-label').map(label => label.getAttribute('data-active'))).toEqual(['false', 'true']);
    expect(all(page, '.accessories-more-label').map(label => label.getAttribute('aria-hidden'))).toEqual(['true', null]);
    // The count is the vehicle's total and does not move with the rows on screen.
    expect(text(page, '.accessories-count')).toBe('24 accessories');
    // The body stays open: the rest arrived inside it.
    expect(bodyOpen(page)).toBe('true');

    button(page).click();
    await page.waitForChanges();

    // The extra rows stay until the fold has shut, so they leave by the fold closing, not by vanishing.
    expect(internals(page).collapsing).toBe(true);
    expect(button(page).getAttribute('aria-expanded')).toBe('false');
    expect(rows(page)).toHaveLength(24);

    await settle(page);

    expect(internals(page).collapsing).toBe(false);
    expect(rows(page)).toHaveLength(COLLAPSED_ROWS);
    expect(text(page, '.accessories-count')).toBe('24 accessories');
  });

  it('pressing again while it is still collapsing opens it again, with every row still there', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);

    button(page).click();
    await page.waitForChanges();
    button(page).click();
    await page.waitForChanges();
    button(page).click();
    await page.waitForChanges();
    await settle(page);

    expect(internals(page).collapsing).toBe(false);
    expect(internals(page).expanded).toBe(true);
    expect(rows(page)).toHaveLength(24);
  });

  it('resets to collapsed on a new vehicle, and is not carried over to a short list', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);
    button(page).click();
    await page.waitForChanges();
    expect(rows(page)).toHaveLength(24);

    await load(page, dto(15));
    expect(internals(page).expanded).toBe(false);
    expect(rows(page)).toHaveLength(10);
    expect(button(page).getAttribute('aria-expanded')).toBe('false');
    expect(text(page, '.accessories-more-label')).toBe('Show all 15 accessories');
  });

  it('shuts the disclosure with the body in flight, and keeps the rows it holds until the body has shut', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);
    expect(moreOpen(page)).toBe('true');

    const pending = owner(page).fetchVin(NINE_VIN);
    await page.waitForChanges();

    expect(bodyOpen(page)).toBe('false');
    expect(moreOpen(page)).toBe('false');
    expect(rows(page)).toHaveLength(COLLAPSED_ROWS);
    await pending;
    await page.waitForChanges();
    expect(moreOpen(page)).toBe('false');
    expect(rows(page)).toHaveLength(9);
  });

  it('brings the button back into view when collapsing would lift it off the top of the screen, and follows the page', async () => {
    const letGo = jest.fn();
    const reveal = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => letGo);

    try {
      const page = await newPage();
      await load(page, MANY_VIN);
      button(page).click();
      await page.waitForChanges();
      expect(reveal).not.toHaveBeenCalled();

      // 14 rows go, 80px each: the button, 600px down, would rise to -520.
      rows(page).forEach(row => withRect(row, { top: 0, height: 80 }));
      withRect(button(page), { top: 600, height: 44 });

      button(page).click();
      await page.waitForChanges();

      expect(reveal).toHaveBeenCalledTimes(1);
      const [anchor, followMs] = reveal.mock.calls[0];
      expect(anchor).toBe(shadow(page).querySelector('.accessories-more'));
      expect(followMs).toBeGreaterThan(0);

      await settle(page);
    } finally {
      reveal.mockRestore();
    }
  });

  it('leaves the page alone when the button stays on screen after the collapse', async () => {
    const reveal = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => () => {});

    try {
      const page = await newPage();
      await load(page, MANY_VIN);
      button(page).click();
      await page.waitForChanges();

      rows(page).forEach(row => withRect(row, { top: 0, height: 80 }));
      // 14 rows x 80px = 1120px go; a button 2000px down is still at 880 afterwards.
      withRect(button(page), { top: 2000, height: 44 });

      button(page).click();
      await page.waitForChanges();

      expect(reveal).not.toHaveBeenCalled();
      await settle(page);
    } finally {
      reveal.mockRestore();
    }
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
    expect(moreOpen(page)).toBe('false');
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
    expect(rows(page)).toHaveLength(9);
    expect(heard).toEqual([]);
    // The total is the vehicle's, not the layout's.
    expect(text(page, '.accessories-count')).toBe('9 accessories');
  });

  it('keeps the count and the expanded rows through a column change', async () => {
    const page = await newPage();
    await load(page, MANY_VIN);
    button(page).click();
    await page.waitForChanges();

    page.root.setAttribute('hidden-fields', 'image');
    await page.waitForChanges();
    expect(text(page, '.accessories-count')).toBe('24 accessories');
    await settle(page);

    expect(text(page, '.accessories-count')).toBe('24 accessories');
    expect(rows(page)).toHaveLength(24);
    expect(button(page).getAttribute('aria-expanded')).toBe('true');
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
});

describe('vehicle-accessories — leaving and entering', () => {
  it('goes from loaded to loading to loaded: the body shuts over the outgoing rows, the strip sweeps, the head content leaves', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);
    expect(rows(page)).toHaveLength(9);

    const pending = owner(page).fetchVin(SINGLE_VIN);
    await page.waitForChanges();

    expect(attr(page, '.accessories-card', 'data-phase')).toBe('busy');
    expect(attr(page, '.lt-grid', 'data-phase')).toBe('busy');
    expect(shadow(page).querySelector('.lookup-panel')?.classList.contains('loading')).toBe(true);
    expect(lead(page)).toBe('skeleton');
    expect(bodyOpen(page)).toBe('false');
    expect(attr(page, '.lt-body', 'aria-hidden')).toBe('true');
    expect(rows(page)).toHaveLength(9);
    expect(attr(page, '.lt-grid', 'role')).toBeNull();

    await pending;
    await page.waitForChanges();

    expect(attr(page, '.accessories-card', 'data-phase')).toBe('settled');
    expect(lead(page)).toBe('columns');
    expect(bodyOpen(page)).toBe('true');
    expect(rows(page)).toHaveLength(1);
    expect(text(page, '.accessories-count')).toBe('1 accessory');
  });

  it('goes from rows to a statement and back: the rows are retained while the body shuts, the notice keeps its words as it fades', async () => {
    const page = await newPage();
    await load(page, UNAUTHORIZED_VIN);
    expect(text(page, '.lt-notice')).toBe(englishLocale.unauthorizedNotice);

    await load(page, NINE_VIN);
    // The strip is on the headings now; the notice layer is out, but still carries its words while it fades.
    expect(lead(page)).toBe('columns');
    expect(attr(page, '.lt-notice', 'data-active')).toBe('false');
    expect(text(page, '.lt-notice')).toBe(englishLocale.unauthorizedNotice);
    expect(attr(page, '.lt-notice', 'class')).toContain('is-neutral');

    await load(page, EMPTY_VIN);
    expect(lead(page)).toBe('notice');
    expect(text(page, '.lt-notice')).toBe(englishLocale.noAccessories);
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
    expect(rows(page)).toHaveLength(9);
    expect(count(page)?.textContent).toContain('9');

    await pending;
    await page.waitForChanges();

    expect(internals(page).leaving).toBe(false);
    expect(verdict(page)).toBe('idle');
    expect(count(page)).toBeNull();
    expect(pill(page)).toBeNull();
    expect(lead(page)).toBe('skeleton');
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
    expect(rows(page)).toHaveLength(9);
    expect(text(page, '.accessories-count')).toBe('9 accessories');
  });

  it('lets a lookup started during an error leave win', async () => {
    const page = await newPage();
    await load(page, NINE_VIN);

    const erroring = owner(page).setErrorMessage('wrongResponseFormat');
    const fetching = owner(page).fetchVin(SINGLE_VIN);
    await Promise.all([erroring, fetching]);
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
    expect(rows(core)).toHaveLength(9);
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
    const order = ['base.css', 'lookup-tokens.css', 'image-viewer.css', 'vehicle-info-layout.css', 'lookup-head.css', 'lookup-table.css'].map(file => css.indexOf(`/${file}';`));
    expect(order.every(index => index >= 0)).toBe(true);
    expect([...order].sort((a, b) => a - b)).toEqual(order);
    expect(css).not.toMatch(/@import[^;]*lookup-motion\.css/);
  });

  it('is plain CSS: no Tailwind, no @apply, no reset of the host that would drop the container', () => {
    expect(css.startsWith('/* tailwind: off */')).toBe(true);
    expect(css).not.toContain('@apply');
    expect(css).not.toMatch(/all:\s*initial/);
    expect(css).not.toContain('shift-skeleton');
  });

  it('keeps a 44px touch target on the button, a 56px thumbnail in the table, and a cards layout at 720px and under', () => {
    expect(css).toMatch(/\.accessories-more-button\s*{[^}]*min-height:\s*44px/);
    expect(tableCss).toMatch(/--lt-media-size:\s*56px/);
    expect(tableCss).toContain('@container (max-width: 720px)');
  });
});
