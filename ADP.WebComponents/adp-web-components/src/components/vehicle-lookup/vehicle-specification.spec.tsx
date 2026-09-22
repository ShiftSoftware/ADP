import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleSpecification } from './vehicle-specification';
import { hasRecords, headValue, modelYearOf, normalise, panelLead } from './components/VehicleSpecificationPanel';

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
