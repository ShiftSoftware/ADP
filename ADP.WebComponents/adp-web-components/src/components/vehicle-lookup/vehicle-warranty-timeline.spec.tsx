import { readFileSync } from 'fs';
import { join } from 'path';

import { newSpecPage } from '@stencil/core/testing';

import { VehicleWarrantyTimeline } from './vehicle-warranty-timeline';
import type { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

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
});
