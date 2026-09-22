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

import vehicleLookupMocks from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';
import brokerMarketMocks from '../../features/mocks/data/generated/broker-market/vehicle-lookup.json';

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
    expect(text(page, '.spec-cell[data-label="Exterior colour"] .spec-value')).toBe('923 · HARBOR GREY METALLIC');
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
