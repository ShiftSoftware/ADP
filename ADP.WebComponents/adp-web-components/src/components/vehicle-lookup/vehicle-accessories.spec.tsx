import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleAccessories } from './vehicle-accessories';
import { AccessoriesLocale, PanelState, panelPlaceholder, placeholderContent } from './components/VehicleAccessoriesPanel';
import { AccessoryDTO } from '~types/generated/vehicle-lookup/accessory-dto';
import { ErrorKeys } from '~features/multi-lingual';

import englishLocale from '../../locales/vehicleLookup/accessories/en.json';
import arabicLocale from '../../locales/vehicleLookup/accessories/ar.json';
import kurdishLocale from '../../locales/vehicleLookup/accessories/ku.json';
import russianLocale from '../../locales/vehicleLookup/accessories/ru.json';
import arabicShared from '../../locales/ar.json';
import englishShared from '../../locales/en.json';

import brokerMarketMocks from '../../features/mocks/data/generated/broker-market/vehicle-lookup.json';

/**
 * The accessories owner through the DOM: the body's placeholder (owner, 2026-10-27) in every state the
 * wrapper can put it in -- idle, loading, loaded, empty, unauthorized, error, retry, cleared -- and the
 * states that must not have changed (rows, rows under the skeleton). The derivation itself is the
 * table's (`placeholderKind`, in features/lookup-table); here it is fed this panel's state.
 */

const LOCALES = { en: englishLocale, ar: arabicLocale, ku: kurdishLocale, ru: russianLocale } as Record<'en' | 'ar' | 'ku' | 'ru', Record<string, string>>;

const WITH_ACCESSORIES_VIN = 'ZS8SV9KXXST918958';
const NO_ACCESSORIES_VIN = 'ZU8ZL7VAXG2426255';
const UNAUTHORIZED_VIN = 'ZV8GHHHP37P214642';

beforeAll(() => {
  (globalThis as unknown as { fetch: unknown }).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

const newPage = async (attributes = '') => {
  const page = await newSpecPage({
    components: [VehicleAccessories],
    html: `<vehicle-accessories is-dev="true" ${attributes}></vehicle-accessories>`,
  });
  await (page.rootInstance as VehicleAccessories).setMockData(brokerMarketMocks as unknown as Parameters<VehicleAccessories['setMockData']>[0]);
  await page.waitForChanges();
  return page;
};

type Page = Awaited<ReturnType<typeof newPage>>;

const owner = (page: Page) => page.rootInstance as VehicleAccessories;
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

const active = (page: Page) => qa(page, '.lt-placeholder-layer').filter(layer => layer.getAttribute('data-active') === 'true');
const kind = (page: Page) => active(page).map(layer => layer.getAttribute('data-kind'));
const regionOpen = (page: Page) => q(page, '.lt-placeholder').getAttribute('data-open');
const words = (page: Page, which: string) => squash(q(page, `.lt-placeholder-layer.is-${which} .lt-placeholder-text`)?.textContent);
const exposedStatuses = (page: Page) => qa(page, '[role="status"]').filter(el => el.getAttribute('aria-hidden') === null);

describe("vehicle-accessories — the placeholder, fed the panel's state", () => {
  const state = (over: Partial<PanelState> = {}): PanelState => ({
    vehicleLoaded: true,
    authorized: true,
    accessories: [{ partNumber: 'TST-1', description: 'Sample', image: null } as AccessoryDTO],
    hidden: new Set(),
    ...over,
  });

  it('is idle before a lookup', () => {
    expect(panelPlaceholder(state({ vehicleLoaded: false, accessories: undefined }))).toBe('idle');
  });

  it('is empty for an authorized vehicle with nothing on file, and for a null list', () => {
    expect(panelPlaceholder(state({ accessories: [] }))).toBe('empty');
    expect(panelPlaceholder(state({ accessories: null }))).toBe('empty');
  });

  it('is unauthorized for a vehicle not in the records, even if the DTO carries a list (a data fault the panel does not follow)', () => {
    expect(panelPlaceholder(state({ authorized: false }))).toBe('unauthorized');
    expect(panelPlaceholder(state({ authorized: false, accessories: [] }))).toBe('unauthorized');
  });

  it('is error for a failed lookup', () => {
    expect(panelPlaceholder(state({ vehicleLoaded: false, accessories: undefined, error: 'The lookup failed' }))).toBe('error');
  });

  it('is nothing above rows, and nothing when the host hid every column (the strip carries the sentence)', () => {
    expect(panelPlaceholder(state())).toBeNull();
    for (const over of [{ vehicleLoaded: false }, { accessories: [] }, { authorized: false }, { error: 'x' }])
      expect(panelPlaceholder(state({ hidden: new Set(['partNumber', 'description', 'image']), ...over }))).toBeNull();
  });

  it('opens the idle one while rows leave, and keeps the kind it has otherwise', () => {
    expect(panelPlaceholder(state(), true)).toBe('idle');
    expect(panelPlaceholder(state({ accessories: [] }), true)).toBe('empty');
    expect(panelPlaceholder(state({ authorized: false }), true)).toBe('unauthorized');
  });

  it('gives every outcome its own glyph and its own line, from the locale', () => {
    const content = placeholderContent(englishLocale as unknown as AccessoriesLocale);
    const glyphs = Object.values(content).map(item => item.glyph);
    expect(new Set(glyphs).size).toBe(4);
    expect(glyphs.every(path => /^M[\d. -]/.test(path))).toBe(true);
    expect(content.idle.text).toBe(englishLocale.idleHint);
    expect(content.empty.text).toBe(englishLocale.emptyHint);
    expect(content.error.text).toBe(englishLocale.errorHint);
    expect(content.unauthorized.text).toBe(englishLocale.unauthorizedHint);
  });
});

describe('vehicle-accessories — the four lines, in every language', () => {
  const KEYS = ['idleHint', 'emptyHint', 'errorHint', 'unauthorizedHint'];

  it.each(Object.keys(LOCALES))('has all four in %s, each non-empty and different from the others', lang => {
    const words = LOCALES[lang as keyof typeof LOCALES];
    for (const key of KEYS) expect([lang, key, (words[key] ?? '').trim().length > 0]).toEqual([lang, key, true]);
    expect(new Set(KEYS.map(key => words[key])).size).toBe(4);
  });

  it("is one line in English: short enough for the placeholder's 52-character measure", () => {
    for (const key of KEYS) expect(englishLocale[key as keyof typeof englishLocale].length).toBeLessThanOrEqual(52);
  });

  it('says not-in-records, never "none", for a vehicle the distributor has no record of, and says nothing about a vehicle before a lookup', () => {
    expect(englishLocale.unauthorizedHint).toMatch(/not in the distributor's records/);
    expect(englishLocale.unauthorizedHint).not.toMatch(/\bno accessories\b|\bnone\b/i);
    expect(englishLocale.idleHint).not.toMatch(/\bno\b|\bnone\b|not in/i);
  });

  it('keeps the schema and the four files in step', () => {
    const typeSource = readFileSync(join(__dirname, '../../locales/vehicleLookup/accessories/type.ts'), 'utf8');
    const schemaKeys = (typeSource.match(/yupTypeMapper\(\[([\s\S]*?)\]\)/)?.[1].match(/'([^']+)'/g) ?? []).map(key => key.slice(1, -1)).sort();
    for (const [lang, words] of Object.entries(LOCALES)) expect([lang, Object.keys(words).sort()]).toEqual([lang, schemaKeys]);
  });
});

describe('vehicle-accessories — the placeholder through the owner', () => {
  it('says idle before a lookup: the headings on the strip, the body shut, one status exposed', async () => {
    const page = await newPage();
    expect(kind(page)).toEqual(['idle']);
    expect(regionOpen(page)).toBe('true');
    expect(words(page, 'idle')).toBe(englishLocale.idleHint);
    expect(q(page, '.lt-lead').getAttribute('data-lead')).toBe('columns');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('false');
    expect(qa(page, '.lt-row').length).toBe(0);
    expect(exposedStatuses(page).map(el => el.getAttribute('data-kind'))).toEqual(['idle']);
  });

  it('keeps the idle placeholder through the first lookup, under the skeleton, then shuts it for rows (rows slide in; nothing else changes)', async () => {
    const page = await newPage();
    const pending = owner(page).fetchVin(WITH_ACCESSORIES_VIN);
    await page.waitForChanges();

    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(kind(page)).toEqual(['idle']);
    expect(regionOpen(page)).toBe('true');
    expect(q(page, '.lt-placeholder-layer.is-idle .lt-placeholder-text').classList.contains('shift-skeleton')).toBe(true);
    expect(q(page, '.lt-placeholder-layer.is-idle .lt-placeholder-mark').classList.contains('shift-skeleton')).toBe(true);

    await pending;
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(false);
    expect(regionOpen(page)).toBe('false');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('true');
    expect(qa(page, '.lt-slot').length).toBeGreaterThan(0);
    // The words stay under the shutting region for a settle, then go, so none is under it when it next opens.
    expect(kind(page)).toEqual(['idle']);
    await settle(page);
    expect(kind(page)).toEqual([]);
    expect(exposedStatuses(page)).toEqual([]);
  });

  it('keeps rows under the skeleton on a lookup from rows, with the placeholder shut: the loading and relookup states are as they were', async () => {
    const page = await newPage();
    await load(page, WITH_ACCESSORIES_VIN);
    await settle(page);
    const pending = owner(page).fetchVin(WITH_ACCESSORIES_VIN);
    await page.waitForChanges();

    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(regionOpen(page)).toBe('false');
    expect(kind(page)).toEqual([]);
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('true');
    expect(qa(page, '.lt-slot-body > .lt-row').every(row => row.classList.contains('loading'))).toBe(true);
    await pending;
  });

  it('says empty for an authorized vehicle with nothing on file, in grey, and never as a verdict (the accent is idle)', async () => {
    const page = await newPage();
    await load(page, NO_ACCESSORIES_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['empty']);
    expect(regionOpen(page)).toBe('true');
    expect(words(page, 'empty')).toBe(englishLocale.emptyHint);
    expect(q(page, '.lookup-card').getAttribute('data-verdict')).toBe('idle');
    expect(q(page, '.lt-lead').getAttribute('data-lead')).toBe('columns');
    expect(exposedStatuses(page).map(el => el.getAttribute('data-kind'))).toEqual(['empty']);
  });

  it('says not-in-records for the broker-market unauthorized vehicle, in amber, and never "none"', async () => {
    const page = await newPage();
    await load(page, UNAUTHORIZED_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['unauthorized']);
    expect(words(page, 'unauthorized')).toBe(englishLocale.unauthorizedHint);
    expect(q(page, '.lookup-card').getAttribute('data-verdict')).toBe('neutral');
    expect(qa(page, '.lt-row').length).toBe(0);
    expect(exposedStatuses(page).map(el => el.getAttribute('data-kind'))).toEqual(['unauthorized']);
  });

  it('says error for a failed lookup, keeps it in place under the skeleton on a retry, and goes when the retry lands rows', async () => {
    const page = await newPage();
    await owner(page).setErrorMessage('noBaseUrl' as ErrorKeys);
    await page.waitForChanges();
    await settle(page);
    expect(kind(page)).toEqual(['error']);
    expect(regionOpen(page)).toBe('true');
    expect(words(page, 'error')).toBe(englishLocale.errorHint);
    expect(q(page, '.lookup-card').getAttribute('data-verdict')).toBe('negative');

    const pending = owner(page).fetchVin(WITH_ACCESSORIES_VIN);
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(kind(page)).toEqual(['error']);
    expect(regionOpen(page)).toBe('true');
    expect(q(page, '.lt-placeholder-layer.is-error .lt-placeholder-text').classList.contains('shift-skeleton')).toBe(true);

    await pending;
    await page.waitForChanges();
    expect(regionOpen(page)).toBe('false');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('true');
  });

  it('rows -> error: during the leave the region is aria-busy, so the idle line is never the exposed status; after the settle only error is exposed', async () => {
    const page = await newPage();
    await load(page, WITH_ACCESSORIES_VIN);
    await settle(page);

    const failing = owner(page).setErrorMessage('noBaseUrl' as ErrorKeys);
    await page.waitForChanges();
    // Leaving, state.error not yet set: the derivation answers idle, but the region is busy.
    expect(q(page, '.lt-placeholder').getAttribute('aria-busy')).toBe('true');
    expect(regionOpen(page)).toBe('true');

    const readable = () =>
      exposedStatuses(page)
        .filter(el => !el.closest('[aria-busy="true"]'))
        .map(el => el.getAttribute('data-kind'));
    expect(readable()).not.toContain('idle');

    await failing;
    await settle(page);
    expect(q(page, '.lt-placeholder').hasAttribute('aria-busy')).toBe(false);
    expect(kind(page)).toEqual(['error']);
    expect(exposedStatuses(page).map(el => el.getAttribute('data-kind'))).toEqual(['error']);
    expect(readable()).toEqual(['error']);
  });

  it('empty -> loaded: the region shuts as the body opens, the words stay for a settle, then go with nothing exposed', async () => {
    const page = await newPage();
    await load(page, NO_ACCESSORIES_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['empty']);
    expect(regionOpen(page)).toBe('true');

    const pending = owner(page).fetchVin(WITH_ACCESSORIES_VIN);
    await page.waitForChanges();
    expect(q(page, '.lt-placeholder').getAttribute('aria-busy')).toBe('true');
    expect(kind(page)).toEqual(['empty']);

    await pending;
    await page.waitForChanges();
    expect(regionOpen(page)).toBe('false');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('true');
    expect(q(page, '.lt-placeholder').hasAttribute('aria-busy')).toBe(false);
    expect(kind(page)).toEqual(['empty']);

    await settle(page);
    expect(kind(page)).toEqual([]);
    expect(exposedStatuses(page)).toEqual([]);
  });

  it('cross-fades between outcomes in one region: rows -> empty -> not in records -> idle (cleared), nothing mounted or unmounted', async () => {
    const page = await newPage();
    await load(page, WITH_ACCESSORIES_VIN);
    await settle(page);
    expect(regionOpen(page)).toBe('false');

    const region = q(page, '.lt-placeholder');
    const layers = qa(page, '.lt-placeholder-layer');

    await load(page, NO_ACCESSORIES_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['empty']);

    await load(page, UNAUTHORIZED_VIN);
    await settle(page);
    expect(kind(page)).toEqual(['unauthorized']);

    await owner(page).clearData();
    await page.waitForChanges();
    await settle(page);
    expect(kind(page)).toEqual(['idle']);
    expect(regionOpen(page)).toBe('true');

    expect(q(page, '.lt-placeholder')).toBe(region);
    expect(qa(page, '.lt-placeholder-layer')).toEqual(layers);
  });

  it('opens the idle placeholder under the skeleton as rows leave on a clear, then is idle', async () => {
    const page = await newPage();
    await load(page, WITH_ACCESSORIES_VIN);
    await settle(page);

    const cleared = owner(page).clearData();
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(true);
    expect(kind(page)).toEqual(['idle']);
    expect(regionOpen(page)).toBe('true');
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('false');

    await cleared;
    await settle(page);
    expect(q(page, '.lookup-panel').classList.contains('loading')).toBe(false);
    expect(kind(page)).toEqual(['idle']);
  });

  it('says nothing in the body when the host hid every column: the strip carries the sentence', async () => {
    const page = await newPage('hidden-fields="partNumber,description,image"');
    expect(regionOpen(page)).toBe('false');
    expect(q(page, '.lt-lead').getAttribute('data-lead')).toBe('notice');
    expect(kind(page)).toEqual([]);
  });

  it('speaks Arabic: the same four layers, the Arabic line, right to left', async () => {
    const page = await newPage('language="ar"');
    await page.waitForChanges();
    expect(q(page, '.lookup-panel').getAttribute('dir')).toBe(arabicShared.direction);
    expect(words(page, 'idle')).toBe(arabicLocale.idleHint);
    expect(words(page, 'error')).toBe(arabicLocale.errorHint);
    expect(words(page, 'unauthorized')).toBe(arabicLocale.unauthorizedHint);
    expect(englishShared.direction).toBe('ltr');
  });
});
