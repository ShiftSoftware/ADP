import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleSpecification } from './vehicle-specification';
import {
  detailGroups,
  detailsSummary,
  exteriorColour,
  hasRecords,
  headValue,
  identityCells,
  modelYearOf,
  normalise,
  panelLead,
  productionMonth,
} from './components/VehicleSpecificationPanel';

import specificationLocale from '../../locales/vehicleLookup/specification/en.json';
import arabicLocale from '../../locales/vehicleLookup/specification/ar.json';
import kurdishLocale from '../../locales/vehicleLookup/specification/ku.json';
import arabicShared from '../../locales/ar.json';

import vehicleLookupMocks from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';
import brokerMarketMocks from '../../features/mocks/data/generated/broker-market/vehicle-lookup.json';
import edgeCaseMocks from '../../features/mocks/data/generated/edge-cases/vehicle-lookup.json';

/**
 * A record panel under the wrapper: the head is the statement about the vehicle, the strip is an
 * anchor in every state, the verdict follows the honesty rule, and every change of verdict is
 * announced so a composite can colour its one card from the active panel — including back to idle.
 * Lookups run on real timers (the shared helper waits a second before it answers).
 */

const AUTHORIZED_VIN = 'ZT8P9NAL1LG988010';
const UNKNOWN_VIN = 'UNKNOWN_VIN_12345';
/** Authorized, and every sub-object arrives as `{}`: an empty record, which is not a record. */
const EMPTY_RECORD_VIN = 'ZU8ZL7VAXG2426255';
/** A full build record: a model description, a grade and a year. */
const RICH_VIN = 'ZT8APGED9RB475247';

beforeAll(() => {
  (global as any).fetch = (url: string) => {
    const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
    return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
  };
});

const newPage = async (mocks: unknown = vehicleLookupMocks) => {
  const page = await newSpecPage({
    components: [VehicleSpecification],
    html: '<vehicle-specification is-dev="true"></vehicle-specification>',
  });
  await (page.rootInstance as VehicleSpecification).setMockData(mocks as any);
  return page;
};

const shadow = (page: Awaited<ReturnType<typeof newPage>>) => page.root.shadowRoot;
const text = (page: Awaited<ReturnType<typeof newPage>>, selector: string) => shadow(page).querySelector(selector)?.textContent?.trim();

describe('vehicle-specification — the derivations', () => {
  it('trims, and reads the feed placeholder zero as nothing for the numeric fields only', () => {
    expect(normalise(' 3500 ')).toBe('3500');
    expect(normalise('  ')).toBe('');
    expect(normalise(null)).toBe('');
    expect(normalise(undefined)).toBe('');
    // A zero is a figure until it is one of the fields the feed pads with " 0 ".
    expect(normalise(' 0 ')).toBe('0');
    expect(normalise(' 0 ', true)).toBe('');
    expect(normalise('4', true)).toBe('4');
    // A sentinel the record carries is shown as sent; the dash is for a field left empty.
    expect(normalise('UNKNOWN')).toBe('UNKNOWN');
  });

  it('prefers the variant parse for the model year and keeps the record figure when they disagree', () => {
    expect(modelYearOf({ vehicleVariantInfo: { modelYear: 2025 } } as any)).toEqual({ year: 2025 });
    // The branch the API cannot take: the parse gave up, but the entry carries a year.
    expect(modelYearOf({ vehicleSpecification: { modelYear: 2026 } } as any)).toEqual({ year: 2026 });
    expect(modelYearOf({ vehicleVariantInfo: { modelYear: 2025 }, vehicleSpecification: { modelYear: 2025 } } as any)).toEqual({ year: 2025 });
    expect(modelYearOf({ vehicleVariantInfo: { modelYear: 2025 }, vehicleSpecification: { modelYear: 2024 } } as any)).toEqual({ year: 2025, recordYear: 2024 });
    expect(modelYearOf(undefined)).toEqual({ year: undefined });
  });

  it('falls back through description, model code and katashiki for the head value', () => {
    expect(headValue({ vehicleSpecification: { modelDescription: 'TALORA', modelCode: '24207' }, vehicleVariantInfo: { modelCode: '24207' } } as any)).toBe('TALORA');
    expect(headValue({ vehicleVariantInfo: { modelCode: '24207' } } as any)).toBe('24207');
    expect(headValue({ vehicleSpecification: { modelCode: '24207' } } as any)).toBe('24207');
    expect(headValue({ identifiers: { katashiki: 'MXAA52L-ANXGP' } } as any)).toBe('MXAA52L-ANXGP');
    expect(headValue({} as any)).toBe('');
  });

  it('counts values, not objects, for hasRecords — an empty sub-object is not a record', () => {
    expect(hasRecords({ identifiers: {}, vehicleSpecification: {}, vehicleVariantInfo: {} } as any)).toBe(false);
    expect(hasRecords({ identifiers: { vin: 'X', brandID: '1' } } as any)).toBe(false);
    // The feed's placeholder zeroes are not a record either.
    expect(hasRecords({ vehicleSpecification: { cylinders: ' 0 ', doors: ' 0 ', tankCap: ' 0 ' } } as any)).toBe(false);
    expect(hasRecords({ vehicleSpecification: { side: 'LHD' } } as any)).toBe(true);
    expect(hasRecords(undefined)).toBe(false);
  });

  it('shows the skeleton while busy, idle and on an error; a notice only for an unauthorized vehicle', () => {
    expect(panelLead({ vehicleLoaded: false }, false)).toBe('skeleton');
    expect(panelLead({ vehicleLoaded: true, authorized: true }, true)).toBe('skeleton');
    expect(panelLead({ vehicleLoaded: false, error: 'Wrong response format' }, false)).toBe('skeleton');
    expect(panelLead({ vehicleLoaded: true, authorized: false }, false)).toBe('notice');
    expect(panelLead({ vehicleLoaded: true, authorized: true }, false)).toBe('caption');
    // Never a notice while a lookup is in flight: a tone change is two cross-fades with the
    // skeleton between them, so a notice is never seen changing tone in place.
    expect(panelLead({ vehicleLoaded: true, authorized: false }, true)).toBe('skeleton');
  });

  it('keeps every tier-1 slot in every state and dashes the model code that repeats the head', () => {
    const locale = specificationLocale as any;

    const cells = identityCells({ vehicleSpecification: { modelCode: 'UMBREL HEV', modelDescription: 'UMBREL HEV' }, identifiers: { variant: 'V1' } } as any, locale, 'en', true);
    expect(cells.map(cell => cell.key)).toEqual(['modelCode', 'variant', 'katashiki', 'modelYear', 'productionDate', 'sfx', 'exteriorColour', 'interiorColour']);
    // The same string under two labels is two labels for one thing.
    expect(cells[0].text).toBe('');
    expect(cells[1].text).toBe('V1');

    // A vehicle the panel may not speak for keeps all eight slots and empties every one.
    const silent = identityCells({ vehicleSpecification: { modelCode: 'X' }, identifiers: { variant: 'V1' } } as any, locale, 'en', false);
    expect(silent).toHaveLength(8);
    expect(silent.every(cell => !cell.text && !cell.colour?.code && !cell.colour?.name)).toBe(true);

    // The record's own year rides inside the value block as a note when the two disagree.
    const disagreeing = identityCells({ vehicleVariantInfo: { modelYear: 2025 }, vehicleSpecification: { modelYear: 2024 } } as any, locale, 'en', true);
    expect(disagreeing[3].text).toBe('2025');
    expect(disagreeing[3].note).toBe('record: 2024');
  });

  it('resolves the paint colour by the four branches, and never guesses one', () => {
    const locale = specificationLocale as any;
    const table = {
      '1G3': { name: 'Magnetic Gray Metallic', approxHex: '#59585d', finish: 'metallic' as const },
      '042': { name: 'White Pearl', finish: 'pearl' as const },
    };
    const colour = (identifiers: object, spec: object = {}, catalogue = table) => exteriorColour({ identifiers, vehicleSpecification: spec } as any, locale, catalogue);

    // Resolved, with a swatch: the catalogue knew the code and had a hex for it.
    expect(colour({ color: '1G3' })).toEqual({ code: '1G3', name: 'Magnetic Gray Metallic', swatch: { hex: '#59585d', finish: 'metallic', caveat: locale.swatchCaveat } });

    // The backend's own name always wins; the catalogue is only ever the swatch and a fallback name.
    expect(colour({ color: '1G3' }, { exteriorColor: 'GRAPHITE METALLIC' }).name).toBe('GRAPHITE METALLIC');

    // Resolved by name only: an entry with no defensible hex draws no swatch, which is complete.
    expect(colour({ color: '042' })).toEqual({ code: '042', name: 'White Pearl', swatch: undefined });

    // A code nothing resolved reads as the code alone — no swatch, no invented colour, no label.
    expect(colour({ color: '254' })).toEqual({ code: '254', name: '', swatch: undefined });
    // ... and so does every code when the host configured no catalogue at all.
    expect(exteriorColour({ identifiers: { color: '1G3' } } as any, locale, undefined)).toEqual({ code: '1G3', name: '', swatch: undefined });

    // A host that resolves a name without a code, and a slot the record left empty.
    expect(colour({}, { exteriorColor: 'Storm Grey' })).toEqual({ code: '', name: 'Storm Grey', swatch: undefined });
    expect(colour({})).toEqual({ code: '', name: '', swatch: undefined });

    // The interior cell has no catalogue and never a swatch, whatever the exterior resolved.
    expect(identityCells({ identifiers: { color: '1G3', trim: 'LA20' } } as any, locale, 'en', true, table)[7].colour).toEqual({ code: 'LA20', name: '' });
  });

  it('draws the swatch inside the covered value block, with the caveat and no verdict shape', async () => {
    const page = await newSpecPage({
      components: [VehicleSpecification],
      html: `<vehicle-specification is-dev="true" brand-slugs='{"1":"toyota"}'></vehicle-specification>`,
    });
    await (page.rootInstance as VehicleSpecification).setMockData(vehicleLookupMocks as any);

    await (page.rootInstance as VehicleSpecification).fetchVin(AUTHORIZED_VIN);
    await page.waitForChanges();

    const cell = shadow(page).querySelector('.spec-cell[data-label="Exterior colour"]');
    const value = cell?.querySelector('.spec-value');
    const swatch = cell?.querySelector('.spec-swatch') as HTMLElement;

    expect(cell?.textContent).toContain('1G3 · Magnetic Gray Metallic');
    // Inside the one covered block, so it comes up with its row and never on a beat of its own.
    expect(value?.classList.contains('shift-skeleton')).toBe(true);
    expect(value?.querySelector('.spec-swatch')).toBe(swatch);
    expect(swatch?.style.background).toBe('#59585d');
    expect(swatch?.getAttribute('data-finish')).toBe('metallic');
    // A depiction: hidden from the reader who cannot see it, its meaning the words beside it.
    expect(swatch?.getAttribute('aria-hidden')).toBe('true');
    expect(swatch?.getAttribute('title')).toBe((specificationLocale as any).swatchCaveat);
    expect(value?.getAttribute('aria-description')).toBe((specificationLocale as any).swatchCaveat);

    // The interior code is real and the catalogue has no interior table: code, no swatch.
    expect(shadow(page).querySelectorAll('.spec-swatch')).toHaveLength(1);
  });

  it('shows the production date at the granularity the record means', () => {
    expect(productionMonth('2025-05-01T00:00:00', 'en')).toBe('May 2025');
    expect(productionMonth('  ', 'en')).toBe('');
    expect(productionMonth('not a date', 'en')).toBe('');
  });

  it('drops empty tier-2 fields and empty groups, dedupes style against body type, and composes the count', () => {
    const locale = specificationLocale as any;

    const groups = detailGroups(
      {
        vehicleSpecification: {
          engine: ' 2400 ',
          cylinders: ' 0 ',
          fuel: 'P',
          tankCap: ' 0 ',
          transmission: 'Automatic',
          class: 'P',
          bodyType: 'SUV',
          style: 'suv',
          doors: ' 5 ',
          side: '1',
        },
      } as any,
      locale,
      true,
    );

    expect(groups.map(group => group.key)).toEqual(['powertrain', 'body']);
    expect(groups[0].cells.map(cell => cell.key)).toEqual(['engine', 'fuel', 'transmission']);
    // Style repeats the body type case-insensitively, so it is not shown twice.
    expect(groups[1].cells.map(cell => cell.key)).toEqual(['class', 'bodyType', 'doors', 'steering']);
    // Coded values verbatim: the panel is not a decoder.
    expect(groups[1].cells.find(cell => cell.key === 'steering')?.text).toBe('1');
    expect(detailsSummary(groups, locale)).toEqual({ names: 'Powertrain · Body', count: '— 7 details' });

    // One field, one group, the singular word.
    const single = detailGroups({ vehicleSpecification: { fuel: 'Petrol' } } as any, locale, true);
    expect(detailsSummary(single, locale)).toEqual({ names: 'Powertrain', count: '— 1 detail' });

    // The fuel capacity composes its unit from the locale and falls back to the litre field.
    expect(detailGroups({ vehicleSpecification: { tankCap: '60' } } as any, locale, true)[0].cells[0].text).toBe('60 L');
    expect(detailGroups({ vehicleSpecification: { fuelLiter: 55 } } as any, locale, true)[0].cells[0].text).toBe('55 L');

    expect(detailGroups({ vehicleSpecification: { engine: 'i4' } } as any, locale, false)).toEqual([]);
    expect(detailGroups(undefined, locale, true)).toEqual([]);
  });
});

describe('vehicle-specification', () => {
  it('opens with the statement head, no pill and an idle accent, goes green on records, and announces each verdict it reaches', async () => {
    const page = await newPage();
    const heard: string[] = [];
    page.root.addEventListener('verdictChange', (event: CustomEvent<string>) => heard.push(event.detail));

    // Idle: the label names the question and is truthful, the value is a dash, and there is no pill.
    expect(text(page, '.spec-title-label')).toBe('Model:');
    expect(text(page, '.spec-title-value')).toBe('—');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('idle');
    expect(shadow(page).querySelector('.spec-lead')?.getAttribute('data-lead')).toBe('skeleton');

    // Records on file: the records are the statement — no pill — and the accent goes green.
    await (page.rootInstance as VehicleSpecification).fetchVin(AUTHORIZED_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('positive');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();
    expect(text(page, '.spec-title-value')).toBe('RAV4 2.0L 4WD');
    expect(shadow(page).querySelector('.spec-lead')?.getAttribute('data-lead')).toBe('caption');

    // Not in the distributor's records: amber, the notice on the strip, and the head says nothing
    // about the vehicle whatever the response carried.
    await (page.rootInstance as VehicleSpecification).fetchVin(UNKNOWN_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('neutral');
    expect(text(page, '.spec-title-value')).toBe('—');
    expect(shadow(page).querySelector('.spec-lead')?.getAttribute('data-lead')).toBe('notice');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-neutral')).toBe(true);

    // Cleared: back to idle, and said so.
    await (page.rootInstance as VehicleSpecification).clearData();
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('idle');

    // A failed lookup: no band — the accent goes negative and the pill carries the message.
    await (page.rootInstance as VehicleSpecification).setErrorMessage('wrongResponseFormat');
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('negative');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-negative')).toBe(true);
    expect(text(page, '.lookup-summary .status-badge > span')).toBe('Wrong response format');
    expect(shadow(page).querySelector('.lookup-error')).toBeNull();

    expect(heard).toEqual(['positive', 'neutral', 'idle', 'negative']);
  });

  it('says "no records" in grey for an authorized vehicle whose record is empty, and never paints it green', async () => {
    const page = await newPage(brokerMarketMocks);

    await (page.rootInstance as VehicleSpecification).fetchVin(EMPTY_RECORD_VIN);
    await page.waitForChanges();

    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('idle');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-idle')).toBe(true);
    expect(text(page, '.lookup-summary .status-badge > span')).toBe('No records');
    expect(text(page, '.spec-title-value')).toBe('—');
    // A caption, not a notice: records are shown, not judged, and the fact is said by the pill.
    expect(shadow(page).querySelector('.spec-lead')?.getAttribute('data-lead')).toBe('caption');
  });

  it('carries the model, the year and the grade in the head, and keeps the grade slot when there is none', async () => {
    const page = await newPage(brokerMarketMocks);

    await (page.rootInstance as VehicleSpecification).fetchVin(RICH_VIN);
    await page.waitForChanges();

    expect(text(page, '.spec-title-value')).toBe('TALORA');
    expect(text(page, '.spec-title-year')).toBe('· 2024');
    expect(text(page, '.spec-grade')).toBe('2.4T/ KMQ Grad/ High');
    expect(shadow(page).querySelector('.spec-grade-slot')?.getAttribute('data-empty')).toBe('false');

    // A vehicle with no grade keeps the slot — it goes invisible, so the head never resizes.
    await (page.rootInstance as VehicleSpecification).fetchVin(EMPTY_RECORD_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelector('.spec-grade-slot')?.getAttribute('data-empty')).toBe('true');
    expect(shadow(page).querySelector('.spec-grade')).not.toBeNull();
    expect(shadow(page).querySelector('.spec-title-year')).toBeNull();
  });

  it('carries the identity grid in every state, with the covers on the values and on nothing else', async () => {
    const page = await newPage(brokerMarketMocks);

    const values = () => Array.from(shadow(page).querySelectorAll('.spec-identity .spec-value'));

    // Idle: eight slots, blank, never unmounted — and the dash is not used, because there is no
    // vehicle for the record to have nothing about.
    expect(values()).toHaveLength(8);
    expect(values().every(value => value.classList.contains('shift-skeleton') && value.classList.contains('resize-settle'))).toBe(true);
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value-content[data-empty="true"]')).toHaveLength(8);
    expect(shadow(page).querySelector('.spec-identity')?.textContent).not.toContain('—');
    // The cover goes on the smallest block that is exactly the value.
    expect(shadow(page).querySelectorAll('.spec-cell-label.shift-skeleton')).toHaveLength(0);
    expect(shadow(page).querySelectorAll('.spec-cell.shift-skeleton')).toHaveLength(0);
    expect(shadow(page).querySelectorAll('.spec-identity.shift-skeleton')).toHaveLength(0);

    await (page.rootInstance as VehicleSpecification).fetchVin(RICH_VIN);
    await page.waitForChanges();

    expect(values()).toHaveLength(8);
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value-content[data-empty="true"]')).toHaveLength(0);
    expect(text(page, '.spec-cell[data-label="Katashiki"] .spec-value')).toBe('DTI942Z-PCMDJC');
    // The distributor's own name for a real paint code: the name the backend resolved, which wins
    // over the catalogue's wording for the same code. No swatch here — this page sets no brandSlugs.
    expect(text(page, '.spec-cell[data-label="Exterior colour"] .spec-value')).toBe('1G3 · GRAPHITE METALLIC');
    // A loaded vehicle with nothing in the slot reads as a dash, and the block says so.
    expect(shadow(page).querySelector('.spec-cell[data-label="Production date"] .spec-value')?.getAttribute('data-role')).toBe('empty');
  });

  it('opens the details expanded, keeps the reader s choice across a lookup, and resets it on a clear', async () => {
    const page = await newPage(brokerMarketMocks);
    const owner = page.rootInstance as VehicleSpecification;

    await owner.fetchVin(RICH_VIN);
    await page.waitForChanges();

    const shell = () => shadow(page).querySelector('.spec-details');
    const region = () => shadow(page).querySelector('#spec-details-region');
    const trigger = () => shadow(page).querySelector('.spec-details-button') as HTMLButtonElement;

    expect(shell()?.getAttribute('data-open')).toBe('true');
    expect(region()?.getAttribute('data-open')).toBe('true');
    expect(trigger()?.getAttribute('aria-expanded')).toBe('true');
    expect(trigger()?.getAttribute('aria-controls')).toBe('spec-details-region');
    expect(trigger()?.getAttribute('aria-label')).toBe('Hide the details');
    expect(text(page, '.spec-details-names')).toBe('Powertrain · Body');

    // The reader folds it away.
    trigger().click();
    await page.waitForChanges();
    expect(region()?.getAttribute('data-open')).toBe('false');
    expect(region()?.getAttribute('aria-hidden')).toBe('true');
    expect(trigger()?.getAttribute('aria-label')).toBe('Show the details');
    // The summary row stays: only the region moves.
    expect(shadow(page).querySelector('.spec-details-summary')).not.toBeNull();

    // A new vehicle does not undo the reader's choice.
    await owner.fetchVin('ZS8AJAYC9P6174790');
    await page.waitForChanges();
    expect(region()?.getAttribute('data-open')).toBe('false');

    // A clear puts the panel back to no vehicle, so it is back to its default — while shut.
    await owner.clearData();
    await page.waitForChanges();
    expect((owner as unknown as { detailsOpen: boolean }).detailsOpen).toBe(true);
  });

  it('shuts the details shell over the outgoing groups rather than dropping them', async () => {
    const page = await newPage(brokerMarketMocks);
    const owner = page.rootInstance as VehicleSpecification;

    await owner.fetchVin(RICH_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelectorAll('.spec-group')).toHaveLength(2);

    // A vehicle with no tier-2 value at all: the shell shuts, and the outgoing groups are still
    // rendered inside it so they slide away with it.
    await owner.fetchVin(EMPTY_RECORD_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelector('.spec-details')?.getAttribute('data-open')).toBe('false');
    expect(shadow(page).querySelector('.spec-details')?.getAttribute('data-empty')).toBe('true');
    expect(shadow(page).querySelector('.spec-details')?.getAttribute('aria-hidden')).toBe('true');
    expect(shadow(page).querySelectorAll('.spec-group').length).toBeGreaterThan(0);
  });

  it('does not touch a loading flag when the language changes', async () => {
    const page = await newPage(brokerMarketMocks);
    const owner = page.rootInstance as VehicleSpecification;

    await owner.fetchVin(RICH_VIN);
    await page.waitForChanges();

    await owner.changeLanguage('ar');
    await page.waitForChanges();

    expect(owner.isLoading).toBe(false);
    expect((owner as unknown as { leaving: boolean }).leaving).toBe(false);
    // The covers never came up and the region never shut for a change of words.
    expect(shadow(page).querySelector('#spec-details-region')?.getAttribute('data-open')).toBe('true');
    expect(text(page, '.spec-title-label')).toBe('الموديل:');
  });

  it('enters an error raised on a settled card through the leave', async () => {
    const page = await newPage(brokerMarketMocks);
    const owner = page.rootInstance as VehicleSpecification;

    await owner.fetchVin(RICH_VIN);
    await page.waitForChanges();

    const seen: boolean[] = [];
    const pending = owner.setErrorMessage('wrongResponseFormat');
    // The head has to be out of the band before the red pill lands, so `leaving` is true while the
    // method is still running and the pill has not been assigned yet.
    seen.push((owner as unknown as { leaving: boolean }).leaving);
    seen.push(owner.isError);
    await pending;
    await page.waitForChanges();

    expect(seen).toEqual([true, false]);
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-negative')).toBe(true);
  });

  it('renders nothing from the sub-objects outside the barrier', async () => {
    const page = await newPage();

    await (page.rootInstance as VehicleSpecification).fetchVin({
      vin: 'ZT8P9NAL1LG988010',
      isAuthorized: true,
      identifiers: { vin: 'ZT8P9NAL1LG988010', katashiki: 'MXAA52L-ANXGP', brandID: 'BRAND-HASH-ID' },
      vehicleSpecification: { modelDescription: 'TALORA' },
      warranty: { hasActiveWarranty: true, warrantyStartDate: '2024-01-01' },
      ssc: [{ sscCode: 'SSC-OPEN-1', description: 'A campaign nobody may read here' }],
      serviceHistory: [{ serviceDate: '2025-01-01', dealerName: 'A dealer nobody may read here' }],
      saleInformation: { saleDate: '2024-02-02', dealerName: 'A dealer nobody may read here' },
      accessories: [{ partNumber: 'ACC-1' }],
      serviceItems: [{ name: 'An item nobody may read here' }],
      paintThickness: [{ panel: 'HOOD', value: 120 }],
      nextServiceDate: '2026-12-12',
    } as any);
    await page.waitForChanges();

    const rendered = shadow(page).textContent ?? '';
    ['SSC-OPEN-1', 'nobody may read here', 'ACC-1', '2026-12-12', '2024-02-02', 'BRAND-HASH-ID'].forEach(forbidden => expect(rendered).not.toContain(forbidden));
    expect(text(page, '.spec-title-value')).toBe('TALORA');
  });
});

/**
 * The tester's round, added against the approved specification rather than against the build: the
 * head's four-step fallback through the DOM, the two empties kept apart, the invariants for a
 * vehicle the distributor has no record of, the model-year precedence on the fixtures that carry
 * it, every colour branch including the ones that must draw nothing, the strip's three anchors,
 * right-to-left, and a barrier audit that reads attributes and titles as well as text.
 */

/** A page with attributes of its own — a host map for the colour catalogue, mostly. */
const newPageWith = async (attributes: string, mocks: unknown = vehicleLookupMocks) => {
  const page = await newSpecPage({
    components: [VehicleSpecification],
    html: `<vehicle-specification is-dev="true" ${attributes}></vehicle-specification>`,
  });
  await (page.rootInstance as VehicleSpecification).setMockData(mocks as any);
  return page;
};

const owner = (page: Awaited<ReturnType<typeof newPage>>) => page.rootInstance as VehicleSpecification;

/** The exterior colour cell, by the label the locale gives it. */
const colourCell = (page: Awaited<ReturnType<typeof newPage>>, label = 'Exterior colour') => shadow(page).querySelector(`.spec-cell[data-label="${label}"]`);

/** What the exterior colour cell's value block reads, whitespace collapsed. */
const colourValue = (page: Awaited<ReturnType<typeof newPage>>, label = 'Exterior colour') =>
  colourCell(page, label)?.querySelector('.spec-value')?.textContent?.replace(/\s+/g, ' ').trim();

/** What the eight tier-1 value blocks read, in order. */
const identityValues = (page: Awaited<ReturnType<typeof newPage>>) =>
  Array.from(shadow(page).querySelectorAll('.spec-identity .spec-value')).map(value => value.textContent?.replace(/\s+/g, ' ').trim());

/**
 * Everything the shadow root says, by any route a reader or a tool could reach: its text, and the
 * value of every attribute on every element — which covers `title`, `aria-label`,
 * `aria-description`, `data-label` and the swatch's inline background. A value leaks just as
 * completely through a title as through a paragraph, and the text alone would not see it.
 */
const everythingSaid = (page: Awaited<ReturnType<typeof newPage>>): string => {
  const said: string[] = [shadow(page).textContent ?? ''];

  shadow(page)
    .querySelectorAll('*')
    .forEach(element => {
      const attributes = (element as Element).attributes;
      for (let index = 0; index < (attributes?.length ?? 0); index++) {
        const attribute = attributes.item(index);
        if (attribute) said.push(`${attribute.name}="${attribute.value}"`);
      }
    });

  return said.join('\n');
};

describe('vehicle-specification — the head statement in every state', () => {
  it('falls through description, parsed code, record code and katashiki, and lands on a dash', async () => {
    const page = await newPage();
    const value = () => text(page, '.spec-title-value');
    const base = { vin: 'ZT8P9NAL1LG988010', isAuthorized: true };

    // 1. The distributor's own description.
    await owner(page).fetchVin({ ...base, vehicleSpecification: { modelDescription: 'TALORA', modelCode: '24207' }, vehicleVariantInfo: { modelCode: '24207' } } as any);
    await page.waitForChanges();
    expect(value()).toBe('TALORA');

    // 2. The variant parse's model code, when nothing resolved a description.
    await owner(page).fetchVin({ ...base, vehicleVariantInfo: { modelCode: '24207' }, vehicleSpecification: { modelCode: '99999' } } as any);
    await page.waitForChanges();
    expect(value()).toBe('24207');
    // A code standing in for a name still reads in the title's typography, not the code role's.
    expect(shadow(page).querySelector('.spec-title-value')?.tagName?.toLowerCase()).toBe('strong');

    // 3. The record's own model code, when the variant did not parse.
    await owner(page).fetchVin({ ...base, vehicleSpecification: { modelCode: '99999' } } as any);
    await page.waitForChanges();
    expect(value()).toBe('99999');

    // 4. The katashiki — the last honest answer to "what is this vehicle".
    await owner(page).fetchVin({ ...base, identifiers: { katashiki: 'MXAA52L-ANXGP' } } as any);
    await page.waitForChanges();
    expect(value()).toBe('MXAA52L-ANXGP');

    // Every field but the VIN empty: a dash, and the label still names the question.
    await owner(page).fetchVin({ ...base, identifiers: { vin: base.vin, brandID: '1' }, vehicleSpecification: {}, vehicleVariantInfo: {} } as any);
    await page.waitForChanges();
    expect(text(page, '.spec-title-label')).toBe('Model:');
    expect(value()).toBe('—');
    expect(shadow(page).querySelector('.spec-title-year')).toBeNull();
  });

  it('reads a sparse record as records, not as an absence', async () => {
    const page = await newPage();

    // One value on the whole vehicle. It is still a record, so the accent is green and the pill
    // stays silent — the records are the statement.
    await owner(page).fetchVin({ vin: 'ZT8P9NAL1LG988010', isAuthorized: true, identifiers: { katashiki: 'MXAA52L-ANXGP' } } as any);
    await page.waitForChanges();

    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('positive');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')).toBeNull();
    expect(text(page, '.spec-title-value')).toBe('MXAA52L-ANXGP');
    // The seven slots the record has nothing for read as dashes, not as blanks: there is a vehicle.
    expect(identityValues(page).filter(value => value === '—')).toHaveLength(7);
    // And nothing in tier 2, so the block is shut and says it is empty.
    expect(shadow(page).querySelector('.spec-details')?.getAttribute('data-empty')).toBe('true');
  });

  it('never paints a placeholder-only record green', async () => {
    const page = await newPage();

    // Sub-objects that are present but hold nothing: the feed's padded zeroes and blank strings.
    // An absence dressed as data is still an absence, and it may not be read as a verdict.
    await owner(page).fetchVin({
      vin: 'ZT8P9NAL1LG988010',
      isAuthorized: true,
      identifiers: { vin: 'ZT8P9NAL1LG988010', brandID: '1', variant: '   ' },
      vehicleVariantInfo: {},
      vehicleSpecification: { cylinders: ' 0 ', doors: ' 0 ', tankCap: ' 0 ', fuelLiter: 0, style: '  ' },
    } as any);
    await page.waitForChanges();

    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('idle');
    expect(text(page, '.lookup-summary .status-badge > span')).toBe('No records');
    expect(shadow(page).querySelector('.spec-lead')?.getAttribute('data-lead')).toBe('caption');
  });
});

describe('vehicle-specification — the invariants', () => {
  it('says nothing about a vehicle the distributor has no record of, whatever the response carries', async () => {
    const page = await newPageWith(`brand-slugs='{"1":"toyota"}'`);

    // Everything populated, and every bit of it the distributor's to withhold: the VIN is not in
    // its systems, so nothing in the response is its to assert.
    await owner(page).fetchVin({
      vin: 'ZZ9NOTOURS0000001',
      isAuthorized: false,
      identifiers: { vin: 'ZZ9NOTOURS0000001', variant: 'VAR-LEAK', katashiki: 'KAT-LEAK', color: '1G3', trim: 'TRIM-LEAK', brandID: '1' },
      vehicleVariantInfo: { modelCode: 'MC-LEAK', sfx: 'SF', modelYear: 2025 },
      vehicleSpecification: { modelDescription: 'DESC-LEAK', variantDescription: 'GRADE-LEAK', engine: 'ENGINE-LEAK', fuel: 'FUEL-LEAK', exteriorColor: 'COLOUR-LEAK' },
    } as any);
    await page.waitForChanges();

    expect(shadow(page).querySelector('.lookup-card')?.getAttribute('data-verdict')).toBe('neutral');
    expect(shadow(page).querySelector('.lookup-summary .status-badge')?.classList.contains('is-neutral')).toBe(true);
    expect(text(page, '.lookup-summary .status-badge > span')).toBe('Not in distributor records');

    // The strip says it in words, in the neutral tone, and is a live region.
    const notice = shadow(page).querySelector('.spec-lead-notice');
    expect(shadow(page).querySelector('.spec-lead')?.getAttribute('data-lead')).toBe('notice');
    expect(notice?.getAttribute('data-active')).toBe('true');
    expect(notice?.getAttribute('role')).toBe('status');
    expect(notice?.classList.contains('is-neutral')).toBe(true);
    expect(notice?.textContent).toContain((specificationLocale as any).unauthorizedNotice);

    // The head says nothing, the grade slot is empty but kept, and all eight slots read as dashes —
    // a vehicle with nothing on record, not a panel with no vehicle.
    expect(text(page, '.spec-title-value')).toBe('—');
    expect(shadow(page).querySelector('.spec-grade-slot')?.getAttribute('data-empty')).toBe('true');
    expect(identityValues(page)).toEqual(['—', '—', '—', '—', '—', '—', '—', '—']);
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value[data-role="empty"]')).toHaveLength(8);
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value-content[data-empty="true"]')).toHaveLength(0);

    // No details block, and no swatch — the paint code is real and the catalogue is configured, and
    // it is still not the distributor's to draw.
    expect(shadow(page).querySelector('.spec-details')?.getAttribute('data-empty')).toBe('true');
    expect(shadow(page).querySelector('.spec-details')?.getAttribute('aria-hidden')).toBe('true');
    expect(shadow(page).querySelectorAll('.spec-swatch')).toHaveLength(0);

    // And not one of the values reaches the reader by any route.
    const said = everythingSaid(page);
    ['VAR-LEAK', 'KAT-LEAK', 'TRIM-LEAK', 'MC-LEAK', 'DESC-LEAK', 'GRADE-LEAK', 'ENGINE-LEAK', 'FUEL-LEAK', 'COLOUR-LEAK', '1G3', '2025'].forEach(leak =>
      expect(said).not.toContain(leak),
    );
  });

  it('keeps the two empties apart: a blank before a lookup, a dash for a vehicle with nothing in the slot', async () => {
    const page = await newPage(brokerMarketMocks);

    // Idle: eight boxes, each holding a non-breaking space pinned at 0. A dash here would say the
    // record has nothing about a vehicle that does not exist yet.
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value-content[data-empty="true"]')).toHaveLength(8);
    expect(shadow(page).querySelector('.spec-identity')?.textContent).not.toContain('—');

    await owner(page).fetchVin(EMPTY_RECORD_VIN);
    await page.waitForChanges();

    // A vehicle with nothing on record: eight dashes, no blanks.
    expect(identityValues(page)).toEqual(['—', '—', '—', '—', '—', '—', '—', '—']);
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value-content[data-empty="true"]')).toHaveLength(0);

    // Cleared: back to the blanks, never to the dashes.
    await owner(page).clearData();
    await page.waitForChanges();
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value-content[data-empty="true"]')).toHaveLength(8);
    expect(shadow(page).querySelector('.spec-identity')?.textContent).not.toContain('—');
  });

  it('keeps the strip s three layers mounted in every state, with exactly one active', async () => {
    const page = await newPage(brokerMarketMocks);

    const layers = () => Array.from(shadow(page).querySelectorAll('.spec-lead .layer'));
    const active = () => layers().filter(layer => layer.getAttribute('data-active') === 'true');
    const hiddenIsInactive = () => layers().every(layer => (layer.getAttribute('data-active') === 'true') === (layer.getAttribute('aria-hidden') !== 'true'));

    // Idle. The skeleton layer is aria-hidden in every state — it is a decoration, not a statement.
    expect(layers()).toHaveLength(3);
    expect(active().map(layer => layer.className.includes('skeleton'))).toEqual([true]);
    expect(shadow(page).querySelector('.spec-lead-skeleton')?.getAttribute('aria-hidden')).toBe('true');
    // The band's wait spinner is an anchor too: the head's last child in every state.
    expect(shadow(page).querySelectorAll('.spec-head .lookup-head-wait')).toHaveLength(1);

    await owner(page).fetchVin(RICH_VIN);
    await page.waitForChanges();
    expect(layers()).toHaveLength(3);
    expect(active()).toHaveLength(1);
    expect(shadow(page).querySelector('.spec-lead-caption')?.getAttribute('data-active')).toBe('true');
    expect(shadow(page).querySelector('.spec-lead-notice')?.getAttribute('aria-hidden')).toBe('true');

    await owner(page).fetchVin('ZV8GHHHP37P214642');
    await page.waitForChanges();
    expect(layers()).toHaveLength(3);
    expect(active()).toHaveLength(1);
    expect(hiddenIsInactive()).toBe(true);
    // The caption layer is still mounted under the notice, so the tone never changes in place.
    expect(shadow(page).querySelector('.spec-lead-caption')).not.toBeNull();
  });
});

describe('vehicle-specification — the model year', () => {
  it('shows the parse as the value and the record as a note when the two disagree', async () => {
    const page = await newPage(edgeCaseMocks);

    // The fixture written for this branch: the variant parses to 2025, the entry column says 2024.
    await owner(page).fetchVin('ZS8QK3WR5TD881204');
    await page.waitForChanges();

    const cell = shadow(page).querySelector('.spec-cell[data-label="Model year"]');
    expect(cell?.querySelector('.spec-value-text')?.textContent).toBe('2025');
    expect(cell?.querySelector('.spec-value-note')?.textContent).toBe('record: 2024');
    // The head shows the parse alone: the note is a fact about the record, not part of the sentence.
    expect(text(page, '.spec-title-year')).toBe('· 2025');
    // No hue and no glyph — a data-quality fault in a record is not a verdict.
    expect(cell?.querySelector('svg')).toBeNull();
    expect(cell?.querySelector('.status-badge')).toBeNull();
  });

  it('takes the record s own year when the variant did not parse at all', async () => {
    const page = await newPage(edgeCaseMocks);

    // `variant: "VAR001"` gives the API no parse, so there is no vehicleVariantInfo; the entry
    // carries 2026. This is the one fallback the API itself cannot make.
    await owner(page).fetchVin('ZS8Z4RNS9TC073619');
    await page.waitForChanges();

    const cell = shadow(page).querySelector('.spec-cell[data-label="Model year"]');
    expect(cell?.querySelector('.spec-value-text')?.textContent).toBe('2026');
    expect(cell?.querySelector('.spec-value-note')).toBeNull();
    expect(text(page, '.spec-title-year')).toBe('· 2026');
  });

  it('leaves the year out of the head and dashes the cell when the record has none', async () => {
    const page = await newPage(edgeCaseMocks);

    await owner(page).fetchVin('ZW8UWF8J4TJ368365');
    await page.waitForChanges();

    expect(shadow(page).querySelector('.spec-title-year')).toBeNull();
    expect(shadow(page).querySelector('.spec-cell[data-label="Model year"] .spec-value')?.getAttribute('data-role')).toBe('empty');
  });
});

describe('vehicle-specification — the colour cell, branch by branch', () => {
  it('draws a swatch only where the catalogue has a defensible hex, and never invents one', async () => {
    const page = await newPageWith(`brand-slugs='{"1":"toyota"}'`, edgeCaseMocks);
    const read = () => colourValue(page);
    const swatch = () => colourCell(page)?.querySelector('.spec-swatch') as HTMLElement | null;

    // A metallic code the catalogue knows, with a hex: the catalogue supplies the name the backend
    // left empty, and the swatch carries the finish.
    await owner(page).fetchVin('ZW8UWF8J4TJ368365');
    await page.waitForChanges();
    expect(read()).toBe('1G3 · Magnetic Gray Metallic');
    expect(swatch()?.getAttribute('data-finish')).toBe('metallic');
    expect(swatch()?.style.background).toBe('#59585d');

    // A solid finish: the same recipe, a different data-finish, no highlight to claim.
    await owner(page).fetchVin('ZS8Z4RNS9TC073619');
    await page.waitForChanges();
    expect(read()).toBe('040 · Super White');
    expect(swatch()?.getAttribute('data-finish')).toBe('solid');

    // A pearl finish.
    await owner(page).fetchVin('ZS8QK3WR5TD881204');
    await page.waitForChanges();
    expect(read()).toBe('070 · Blizzard Pearl');
    expect(swatch()?.getAttribute('data-finish')).toBe('pearl');

    // A code the catalogue names but has no defensible hex for: a name and no swatch, which is a
    // complete answer and not a degraded one.
    await owner(page).fetchVin('ZW8PD9FK1T6034557');
    await page.waitForChanges();
    expect(read()).toBe('042 · White Pearl');
    expect(swatch()).toBeNull();

    // A real code the catalogue does not carry: the code alone. No swatch, no guessed colour, and —
    // the part that has to be asserted rather than assumed — no "unknown" label of any kind.
    await owner(page).fetchVin('ZU9HB6TM3T4719068');
    await page.waitForChanges();
    expect(read()).toBe('254');
    expect(swatch()).toBeNull();
    expect(colourCell(page)?.querySelector('.spec-value')?.getAttribute('data-role')).toBe('code');
    expect(colourCell(page)?.textContent?.toLowerCase()).not.toContain('unknown');
    expect(colourCell(page)?.textContent).not.toContain('—');

    // A code nothing anywhere can know.
    await owner(page).fetchVin('ZT8VC4MH7TB552731');
    await page.waitForChanges();
    expect(read()).toBe('Q23');
    expect(swatch()).toBeNull();
  });

  it('prints the name the distributor resolved over the catalogue s, and still draws the catalogue s chip', async () => {
    const page = await newPageWith(`brand-slugs='{"1":"toyota"}'`, brokerMarketMocks);

    // The same real code the catalogue calls "Magnetic Gray Metallic": the distributor's own word
    // is printed, and the chip beside it is still the catalogue's, because there is no hex anywhere
    // in the response.
    await owner(page).fetchVin(RICH_VIN);
    await page.waitForChanges();

    expect(colourValue(page)).toBe('1G3 · GRAPHITE METALLIC');
    expect(colourCell(page)?.querySelector('.spec-swatch')?.getAttribute('data-finish')).toBe('metallic');
    expect(colourCell(page)?.textContent).not.toContain('Magnetic Gray Metallic');
  });

  it('draws nothing for a brand the host did not map, and nothing for a response with no brand id', async () => {
    // The map carries another brand's id. There is no fallback and no guess: an unmapped id has no
    // slug, a brand with no slug has no table, and a vehicle with no table has no chip.
    const unmapped = await newPageWith(`brand-slugs='{"9":"toyota"}'`, edgeCaseMocks);
    await owner(unmapped).fetchVin('ZW8UWF8J4TJ368365');
    await unmapped.waitForChanges();
    expect(colourValue(unmapped)).toBe('1G3');
    expect(unmapped.root.shadowRoot.querySelectorAll('.spec-swatch')).toHaveLength(0);

    // A response with no brandID at all — the same answer, reached without a guess.
    const missing = await newPageWith(`brand-slugs='{"1":"toyota"}'`);
    await owner(missing).fetchVin({ vin: 'ZT8P9NAL1LG988010', isAuthorized: true, identifiers: { color: '1G3' } } as any);
    await missing.waitForChanges();
    expect(colourValue(missing)).toBe('1G3');
    expect(missing.root.shadowRoot.querySelectorAll('.spec-swatch')).toHaveLength(0);
  });

  it('puts no catalogue output on a vehicle the distributor has no records for', async () => {
    const page = await newPageWith(`brand-slugs='{"1":"toyota"}'`, brokerMarketMocks);

    // Authorized, but the record is empty. There is no code to look up, so there is no name and no
    // chip — the cell dashes like every other, and the catalogue is not consulted for a fallback.
    await owner(page).fetchVin(EMPTY_RECORD_VIN);
    await page.waitForChanges();

    expect(colourCell(page)?.querySelector('.spec-value')?.getAttribute('data-role')).toBe('empty');
    expect(colourValue(page)).toBe('—');
    expect(shadow(page).querySelectorAll('.spec-swatch')).toHaveLength(0);
    expect(shadow(page).querySelector('.spec-value[aria-description]')).toBeNull();
  });
});

describe('vehicle-specification — the details block', () => {
  it('promises exactly what opening it delivers, in the singular when there is one', async () => {
    const page = await newPage(edgeCaseMocks);

    // One field survives normalisation on this vehicle: cylinders, doors and tankCap are the feed's
    // padded zeroes, which are placeholders rather than figures.
    await owner(page).fetchVin('ZU9HB6TM3T4719068');
    await page.waitForChanges();

    expect(text(page, '.spec-details-names')).toBe('Powertrain');
    expect(text(page, '.spec-details-count')).toBe('— 1 detail');
    expect(shadow(page).querySelectorAll('#spec-details-region .spec-cell')).toHaveLength(1);
    expect(shadow(page).querySelectorAll('.spec-group')).toHaveLength(1);

    // And on the vehicle that carries every field, the plural and both groups.
    await owner(page).fetchVin('ZT8VC4MH7TB552731');
    await page.waitForChanges();
    expect(text(page, '.spec-details-names')).toBe('Powertrain · Body');
    expect(text(page, '.spec-details-count')).toBe(`— ${shadow(page).querySelectorAll('#spec-details-region .spec-cell').length} details`);
    expect(shadow(page).querySelectorAll('.spec-group')).toHaveLength(2);
  });

  it('carries no block, no summary row and no trigger for a vehicle with no details', async () => {
    const page = await newPage(edgeCaseMocks);

    await owner(page).fetchVin('ZT8VC4MH7TB552731');
    await page.waitForChanges();
    expect(shadow(page).querySelector('.spec-details')?.getAttribute('data-open')).toBe('true');

    // Identity, and nothing in tier 2. The shell shuts, so the band, its rule, the summary row and
    // the trigger are all inside a region at zero height and behind aria-hidden — "nothing renders",
    // reached by sliding rather than by vanishing.
    await owner(page).fetchVin('ZW8PD9FK1T6034557');
    await page.waitForChanges();
    const shell = shadow(page).querySelector('.spec-details');
    expect(shell?.getAttribute('data-open')).toBe('false');
    expect(shell?.getAttribute('data-empty')).toBe('true');
    expect(shell?.getAttribute('aria-hidden')).toBe('true');
    expect(shadow(page).querySelector('.spec-details-button')?.getAttribute('aria-expanded')).toBe('false');
    // The identity grid is unaffected: it is the fixed structure and never shuts.
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value')).toHaveLength(8);
  });

  it('brings the next vehicle back expanded after a clear', async () => {
    const page = await newPage(brokerMarketMocks);

    await owner(page).fetchVin(RICH_VIN);
    await page.waitForChanges();
    (shadow(page).querySelector('.spec-details-button') as HTMLButtonElement).click();
    await page.waitForChanges();
    expect(shadow(page).querySelector('#spec-details-region')?.getAttribute('data-open')).toBe('false');

    // The clear resets the reader's choice while the block is shut, so nothing moves for it...
    await owner(page).clearData();
    await page.waitForChanges();

    // ... and the next vehicle arrives showing everything the panel holds.
    await owner(page).fetchVin(RICH_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelector('#spec-details-region')?.getAttribute('data-open')).toBe('true');
    expect(shadow(page).querySelector('.spec-details-button')?.getAttribute('aria-label')).toBe('Hide the details');
  });
});

describe('vehicle-specification — direction and language', () => {
  it('renders right to left in ar and ku, in the locale s words, and declares no direction of its own', async () => {
    const page = await newPage(brokerMarketMocks);

    await owner(page).fetchVin(RICH_VIN);
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-panel')?.getAttribute('dir')).toBe('ltr');

    await owner(page).changeLanguage('ar');
    await page.waitForChanges();

    // The direction is the wrapper's root and the panel's own card declares none: the face and the
    // mirrored layout follow `dir` through that one rule, so the panel needs no rule of its own.
    expect(shadow(page).querySelector('.lookup-panel')?.getAttribute('dir')).toBe('rtl');
    expect(shadow(page).querySelector('.spec-card')?.getAttribute('dir')).toBeNull();
    expect(shadow(page).querySelector('.spec-identity')?.getAttribute('dir')).toBeNull();
    // Codes and VINs must not be machine-translated.
    expect(page.root.getAttribute('translate')).toBe('no');

    // Every label is the locale's, at both tiers and on the strip.
    expect(text(page, '.spec-title-label')).toBe(`${arabicLocale.model}:`);
    expect(text(page, '.spec-lead-caption-label')).toBe(arabicLocale.identity);
    expect(shadow(page).querySelector(`.spec-cell[data-label="${arabicLocale.exteriorColour}"]`)).not.toBeNull();
    expect(text(page, '.spec-details-names')).toBe(`${arabicLocale.powertrain} · ${arabicLocale.body}`);
    expect(shadow(page).querySelector('.spec-details-button')?.getAttribute('aria-label')).toBe(arabicLocale.collapseDetails);
    // The identity grid is still the same eight slots: a language is a re-layout, not a state.
    expect(shadow(page).querySelectorAll('.spec-identity .spec-value')).toHaveLength(8);

    await owner(page).changeLanguage('ku');
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-panel')?.getAttribute('dir')).toBe('rtl');
    expect(text(page, '.spec-lead-caption-label')).toBe(kurdishLocale.identity);

    await owner(page).changeLanguage('en');
    await page.waitForChanges();
    expect(shadow(page).querySelector('.lookup-panel')?.getAttribute('dir')).toBe('ltr');
    expect(text(page, '.spec-lead-caption-label')).toBe((specificationLocale as any).identity);
  });

  it('says the unauthorized notice and the no-records pill in the locale s words', async () => {
    const page = await newPage(brokerMarketMocks);

    await owner(page).changeLanguage('ar');
    await owner(page).fetchVin('ZV8GHHHP37P214642');
    await page.waitForChanges();
    expect(shadow(page).querySelector('.spec-lead-notice')?.textContent).toContain(arabicLocale.unauthorizedNotice);
    expect(text(page, '.lookup-summary .status-badge > span')).toBe(arabicShared.notInRecords);

    await owner(page).fetchVin(EMPTY_RECORD_VIN);
    await page.waitForChanges();
    expect(text(page, '.lookup-summary .status-badge > span')).toBe(arabicShared.noRecords);
  });

  /**
   * The defect above, pinned as it actually renders, so that the `it.failing` below cannot pass for
   * the wrong reason: the cell *is* found under the Arabic label, it *is* populated, and what it
   * holds is the English month.
   */
  it('renders the production date under the locale s label — in English, whatever the language', async () => {
    const page = await newPage(edgeCaseMocks);
    const english = new Date('2024-11-01T00:00:00').toLocaleDateString('en', { year: 'numeric', month: 'long' });

    await owner(page).fetchVin('ZS8QK3WR5TD881204');
    await page.waitForChanges();
    expect(text(page, `.spec-cell[data-label="${(specificationLocale as any).productionDate}"] .spec-value`)).toBe(english);

    await owner(page).changeLanguage('ar');
    await page.waitForChanges();
    // The label translated; the figure beside it did not.
    expect(shadow(page).querySelector(`.spec-cell[data-label="${arabicLocale.productionDate}"]`)).not.toBeNull();
    expect(text(page, `.spec-cell[data-label="${arabicLocale.productionDate}"] .spec-value`)).toBe(english);
  });

  /**
   * DEFECT, recorded rather than hidden. The panel formats the production date with
   * `sharedLocales.language`, which is the word "english" / "arabic" / "kurdish" / "russian" and not
   * a BCP-47 tag. `Intl` accepts each of those as a structurally valid language subtag it has no
   * data for, so it resolves every one of them to the runtime's default locale instead of throwing.
   * The cell therefore reads "November 2024" in Arabic, Kurdish and Russian, against § 3 of the
   * specification ("month name + year in the locale").
   *
   * Written with `it.failing` so the suite stays honest in both directions: it passes while the
   * defect is there and fails the moment somebody fixes it without coming back to this test.
   */
  it.failing('formats the production date in the reader s language', async () => {
    const page = await newPage(edgeCaseMocks);

    await owner(page).changeLanguage('ar');
    await owner(page).fetchVin('ZS8QK3WR5TD881204');
    await page.waitForChanges();

    expect(text(page, `.spec-cell[data-label="${arabicLocale.productionDate}"] .spec-value`)).toBe(
      new Date('2024-11-01T00:00:00').toLocaleDateString('ar', { year: 'numeric', month: 'long' }),
    );
  });
});

describe('vehicle-specification — the barrier', () => {
  it('leaks nothing from outside the five sub-objects through text, an attribute or a title', async () => {
    // The host maps the leak-marker brand id itself, so the panel really does read `brandID` on
    // this render — it is the key that reaches the catalogue and draws the chip below — and the
    // audit is therefore about a value the panel *held*, not one it never touched.
    const page = await newPageWith(`brand-slugs='{"BRAND-HASH-ID":"toyota"}'`);

    // Every sub-object the barrier names, populated with a string that exists nowhere else, plus the
    // brand id — which the panel is allowed to *read* (it keys the colour catalogue) and forbidden
    // to put anywhere a reader, a screen reader or a log could find it.
    await owner(page).fetchVin({
      vin: 'ZT8P9NAL1LG988010',
      isAuthorized: true,
      identifiers: { vin: 'ZT8P9NAL1LG988010', katashiki: 'MXAA52L-ANXGP', variant: '61840KP202501', color: '1G3', trim: 'LA20', brandID: 'BRAND-HASH-ID' },
      vehicleVariantInfo: { modelCode: '61840', sfx: 'KP', modelYear: 2025 },
      vehicleSpecification: { modelDescription: 'TALORA', engine: '2.5L A25A-FKS', fuel: 'Petrol' },
      basicModelCode: 'BASIC-MODEL-CODE-LEAK',
      sscLogId: 'SSC-LOG-ID-LEAK',
      warranty: { hasActiveWarranty: true, warrantyStartDate: 'WARRANTY-START-LEAK', warrantyEndDate: 'WARRANTY-END-LEAK' },
      ssc: [{ sscCode: 'SSC-CODE-LEAK', description: 'SSC-DESCRIPTION-LEAK', status: 'open' }],
      serviceHistory: [{ serviceDate: 'SERVICE-DATE-LEAK', dealerName: 'SERVICE-HISTORY-LEAK', mileage: 'MILEAGE-LEAK' }],
      saleInformation: { saleDate: 'SALE-DATE-LEAK', dealerName: 'SALE-DEALER-LEAK', customerName: 'CUSTOMER-LEAK' },
      paintThickness: [{ panel: 'PAINT-PANEL-LEAK', value: 'PAINT-VALUE-LEAK' }],
      paintThicknessInspections: [{ inspectionDate: 'INSPECTION-DATE-LEAK', dealerName: 'PAINT-INSPECTION-LEAK' }],
      paintThicknessCertificateAvailable: true,
      paintThicknessCertificateUrls: ['https://example.invalid/PAINT-CERT-LEAK.pdf'],
      accessories: [{ partNumber: 'ACCESSORY-LEAK', description: 'ACCESSORY-DESCRIPTION-LEAK' }],
      serviceItems: [{ name: 'SERVICE-ITEM-LEAK', price: 'PRICE-LEAK' }],
      serviceMenu: [{ name: 'SERVICE-MENU-LEAK', items: ['MENU-ITEM-LEAK'] }],
      nextServiceDate: 'NEXT-SERVICE-LEAK',
    } as any);
    await page.waitForChanges();

    // The panel did render this vehicle — so the audit below is about what it left out, not about a
    // card that happened to draw nothing at all.
    expect(text(page, '.spec-title-value')).toBe('TALORA');
    expect(colourCell(page)?.querySelector('.spec-swatch')).not.toBeNull();

    const said = everythingSaid(page);
    [
      'BRAND-HASH-ID',
      'BASIC-MODEL-CODE-LEAK',
      'SSC-LOG-ID-LEAK',
      'WARRANTY-START-LEAK',
      'WARRANTY-END-LEAK',
      'SSC-CODE-LEAK',
      'SSC-DESCRIPTION-LEAK',
      'SERVICE-DATE-LEAK',
      'SERVICE-HISTORY-LEAK',
      'MILEAGE-LEAK',
      'SALE-DATE-LEAK',
      'SALE-DEALER-LEAK',
      'CUSTOMER-LEAK',
      'PAINT-PANEL-LEAK',
      'PAINT-VALUE-LEAK',
      'INSPECTION-DATE-LEAK',
      'PAINT-INSPECTION-LEAK',
      'PAINT-CERT-LEAK',
      'ACCESSORY-LEAK',
      'ACCESSORY-DESCRIPTION-LEAK',
      'SERVICE-ITEM-LEAK',
      'PRICE-LEAK',
      'SERVICE-MENU-LEAK',
      'MENU-ITEM-LEAK',
      'NEXT-SERVICE-LEAK',
    ].forEach(leak => expect(said).not.toContain(leak));

    // The audit is only as good as its reach, so the reach is asserted too: the walk must have read
    // attributes and titles, not only text, and the values the panel *is* allowed to say must be in
    // what it read — otherwise an empty string would pass every assertion above.
    expect(said).toContain('data-label="Katashiki"');
    expect(said).toContain('title="On-screen colour is approximate');
    expect(said).toContain('MXAA52L-ANXGP');
    expect(said.split('\n').length).toBeGreaterThan(50);
  });
});
