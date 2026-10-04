import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleWarrantyTimeline } from './vehicle-warranty-timeline';
import type { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import allocationMarket from '../../features/mocks/data/generated/allocation-market/vehicle-lookup.json';
import standardDealer from '../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';
import arLocale from '../../locales/vehicleLookup/warrantyTimeline/ar.json';

const activationOnly = allocationMarket.ZT8LY7CZ0S2959088 as unknown as VehicleLookupDTO;
const invoiceOnly = standardDealer.ZT8P9NAL1LG988010 as unknown as VehicleLookupDTO;
const unauthorized = allocationMarket.ZV9LMW6D75T151181 as unknown as VehicleLookupDTO;

const SETTLE = 480 + 40;
const wait = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

beforeAll(() => {
  Object.defineProperty(global, 'fetch', {
    configurable: true,
    value: (url: string) => {
      const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(url.indexOf('locales/'))), 'utf8'));
      return Promise.resolve({ ok: true, json: () => Promise.resolve(body) });
    },
  });
});

describe('vehicle-warranty-timeline', () => {
  it('forwards its public today prop to the coverage rail', async () => {
    const page = await newSpecPage({
      components: [VehicleWarrantyTimeline],
      html: '<vehicle-warranty-timeline core-only disable-vin-validation today="2026-09-01"></vehicle-warranty-timeline>',
    });

    await (page.rootInstance as VehicleWarrantyTimeline).fetchVin({
      vin: 'SAMPLE-VIN',
      isAuthorized: true,
      warranty: { warrantyStartDate: '2025-01-01', warrantyEndDate: '2028-01-01' },
    } as VehicleLookupDTO);
    await page.waitForChanges();

    expect(page.root.shadowRoot.querySelector('.today-pill')?.textContent).toContain('2026-09-01');
  });
  describe('sale dates line', () => {
    const newPage = async (attributes = '') => {
      const page = await newSpecPage({
        components: [VehicleWarrantyTimeline],
        html: `<vehicle-warranty-timeline core-only disable-vin-validation today="2026-09-01" ${attributes}></vehicle-warranty-timeline>`,
      });
      const load = async (vehicle: VehicleLookupDTO) => {
        await (page.rootInstance as VehicleWarrantyTimeline).fetchVin(vehicle);
        await page.waitForChanges();
      };
      const line = () => page.root.shadowRoot.querySelector('.warranty-dates');
      const values = () => [...line().querySelectorAll('.warranty-date-value')].map(value => value.textContent);
      return { page, load, line, values };
    };

    it('is shut while idle', async () => {
      const { line } = await newPage();

      expect(line().getAttribute('data-open')).toBe('false');
      expect(line().getAttribute('aria-hidden')).toBe('true');
    });

    it('opens on a vehicle with an activation date and one with an invoice date', async () => {
      const { load, line, values } = await newPage();

      await load(activationOnly);
      expect(line().getAttribute('data-open')).toBe('true');
      expect(values()).toEqual(['—', '2026-02-21']);

      await load(invoiceOnly);
      expect(line().getAttribute('data-open')).toBe('true');
      expect(values()).toEqual(['2024-01-15', '—']);
    });

    it('is shut on an unauthorized vehicle and drops the outgoing dates once shut', async () => {
      const { page, load, line, values } = await newPage();

      await load(activationOnly);
      await load(unauthorized);

      expect(line().getAttribute('data-open')).toBe('false');
      expect(values()).toEqual(['—', '2026-02-21']);

      await wait(SETTLE + 20);
      await page.waitForChanges();
      expect(values()).toEqual(['—', '—']);
    });

    it('is shut on an error', async () => {
      const { page, load, line } = await newPage();

      await load(invoiceOnly);
      await (page.rootInstance as VehicleWarrantyTimeline).setErrorMessage('wildCard');
      await page.waitForChanges();

      expect(line().getAttribute('data-open')).toBe('false');
      expect(line().getAttribute('aria-hidden')).toBe('true');
    });

    it('leaves a hidden item out, and stays shut when both are hidden', async () => {
      const oneHidden = await newPage('hidden-fields="warrantyActivationDate"');
      await oneHidden.load(invoiceOnly);
      expect([...oneHidden.line().querySelectorAll('.warranty-date')].map(item => item.getAttribute('data-field'))).toEqual(['invoiceDate']);

      const bothHidden = await newPage('hidden-fields="invoiceDate, warrantyActivationDate"');
      await bothHidden.load(invoiceOnly);
      expect(bothHidden.line().getAttribute('data-open')).toBe('false');
      expect(bothHidden.line().querySelectorAll('.warranty-date')).toHaveLength(0);
    });

    it('shuts, swaps and reopens when hiddenFields changes live', async () => {
      const { page, load, line } = await newPage();
      const fields = () => [...line().querySelectorAll('.warranty-date')].map(item => item.getAttribute('data-field'));

      await load(invoiceOnly);
      page.root.hiddenFields = 'warrantyActivationDate';
      await page.waitForChanges();

      expect(line().getAttribute('data-open')).toBe('false');
      expect(fields()).toEqual(['invoiceDate', 'warrantyActivationDate']);

      await wait(SETTLE + 20);
      await page.waitForChanges();

      expect(line().getAttribute('data-open')).toBe('true');
      expect(fields()).toEqual(['invoiceDate']);
    });

    it('labels the dates in Arabic', async () => {
      const { load, line } = await newPage('language="ar"');

      await load(invoiceOnly);

      expect(line().querySelector('[data-field="invoiceDate"] .warranty-date-label')?.textContent).toBe(arLocale.invoiceDate);
      expect(line().querySelector('[data-field="warrantyActivationDate"] .warranty-date-label')?.textContent).toBe(arLocale.warrantyActivationDate);
      expect(line().querySelector('[data-field="invoiceDate"] bdi')?.getAttribute('dir')).toBe('ltr');
    });
  });
});
