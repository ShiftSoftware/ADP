import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import timelineLocale from '../../../locales/vehicleLookup/warrantyTimeline/en.json';
import standardDealerVehicleLookup from '../../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';
import type { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import CoverageTimeline from './CoverageTimeline';

const SNAPSHOT = '2027-06-01';

const renderTimeline = (vehicleInformation?: Partial<VehicleLookupDTO>, today = SNAPSHOT) =>
  newSpecPage({
    components: [],
    template: () => <CoverageTimeline vehicleInformation={vehicleInformation as VehicleLookupDTO} locale={timelineLocale} isAuthorized today={today} />,
  });

const bandsOf = (page: { body: HTMLElement }) => [...page.body.querySelectorAll('.coverage-entry')];

const variableOf = (element: Element, name: string) => (element as HTMLElement).style.getPropertyValue(name);

/** Three contiguous bands: standard, then two extended coverages from different providers. */
const threeBandVehicle = {
  saleInformation: { companyName: 'Sample Motors' },
  warranty: {
    warrantyStartDate: '2024-02-01',
    warrantyEndDate: '2027-02-01',
    extendedWarranties: [
      { id: 'EW-SECOND', providerCompanyID: '2', providerCompanyLogo: 'https://images.test/b.png', startDate: '2028-02-01', endDate: '2029-02-01' },
      { id: 'EW-FIRST', providerCompanyID: '100', providerCompanyLogo: 'https://images.test/a.png', startDate: '2027-02-01', endDate: '2028-02-01' },
    ],
  },
};

describe('CoverageTimeline', () => {
  it('renders the header and badges but no rail when the vehicle has no warranty dates', async () => {
    const page = await renderTimeline({ saleInformation: { companyName: 'Sample Motors' } } as Partial<VehicleLookupDTO>);

    expect(page.body.querySelector('.activation-title')?.textContent).toContain('Sample Motors');
    expect(page.body.querySelectorAll('.status-badge')).toHaveLength(2);
    expect(page.body.querySelector('.timeline-shell')).toBeNull();
    expect(page.body.querySelector('.total-coverage')).toBeNull();
  });

  it('renders the standard warranty alone when an older API omits the collection', async () => {
    const page = await renderTimeline({
      warranty: { warrantyStartDate: '2024-02-01', warrantyEndDate: '2027-02-01' },
    } as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands).toHaveLength(1);
    expect(bands[0].getAttribute('data-kind')).toBe('standard');
    expect(bands[0].textContent).toContain(timelineLocale.standardWarranty);
  });

  it('orders every coverage by start date and spans the full range', async () => {
    const page = await renderTimeline(threeBandVehicle as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands.map(band => band.getAttribute('data-kind'))).toEqual(['standard', 'extended', 'extended']);
    // Input order was second-then-first; the rail is sorted chronologically.
    expect(bands[1].textContent).toContain('EW-FIRST');
    expect(bands[2].textContent).toContain('EW-SECOND');

    expect(variableOf(bands[0], '--start')).toBe('0.000000%');

    const lastStart = Number.parseFloat(variableOf(bands[2], '--start'));
    const lastSpan = Number.parseFloat(variableOf(bands[2], '--span'));
    expect(lastStart + lastSpan).toBeCloseTo(100, 4);
  });

  it('derives each coverage status from the snapshot date', async () => {
    const page = await renderTimeline(threeBandVehicle as Partial<VehicleLookupDTO>);

    // Snapshot 2027-06-01 sits inside the first extended coverage.
    expect(bandsOf(page).map(band => band.getAttribute('data-status'))).toEqual(['expired', 'active', 'upcoming']);
  });

  it('reports the vehicle as unprotected once every coverage has elapsed', async () => {
    const page = await renderTimeline(threeBandVehicle as Partial<VehicleLookupDTO>, '2035-01-01');

    expect(page.body.textContent).toContain(timelineLocale.notActiveWarranty);
    expect(page.body.textContent).not.toContain(timelineLocale.activeWarranty);
  });

  it('washes the elapsed part of the rail up to the snapshot', async () => {
    const page = await renderTimeline(threeBandVehicle as Partial<VehicleLookupDTO>);

    const wash = page.body.querySelector('.past-wash') as HTMLElement;
    const covered = Number.parseFloat(wash.style.getPropertyValue('--to'));

    // 2027-06-01 of a 2024-02-01 → 2029-02-01 range is a bit past the two-thirds mark.
    expect(covered).toBeGreaterThan(60);
    expect(covered).toBeLessThan(70);
  });

  it('totals the planned protection and splits it by kind', async () => {
    const page = await renderTimeline(threeBandVehicle as Partial<VehicleLookupDTO>);

    const total = page.body.querySelector('.total-coverage');

    expect(total?.querySelector('strong')?.textContent).toBe('5 years');
    expect(total?.querySelector('.coverage-mix')?.textContent).toBe('3 years standard + 2 years extended');
  });

  it('shows a provider logo when one resolved and the provider id when it did not', async () => {
    const page = await renderTimeline({
      warranty: {
        warrantyStartDate: '2024-02-01',
        warrantyEndDate: '2027-02-01',
        extendedWarranties: [
          { id: 'EW-LOGO', providerCompanyID: '100', providerCompanyLogo: 'https://images.test/a.png', startDate: '2027-02-01', endDate: '2028-02-01' },
          { id: 'EW-BARE', providerCompanyID: '2', providerCompanyLogo: '', startDate: '2028-02-01', endDate: '2029-02-01' },
        ],
      },
    } as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands[1].querySelector('.provider-logo')?.getAttribute('src')).toBe('https://images.test/a.png');
    expect(bands[2].querySelector('.provider-logo')).toBeNull();
    expect(bands[2].querySelector('.segment')?.textContent).toBe('2');
  });

  it('renders the checked-in multiple-warranty sample', async () => {
    const page = await renderTimeline(standardDealerVehicleLookup.JTMHX01J8L4198293 as unknown as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands).toHaveLength(3);
    expect(bands[1].textContent).toContain('EW-JTMHX01J8L4198293-DISTRIBUTOR');
    expect(bands[2].textContent).toContain('EW-JTMHX01J8L4198293-PROVIDER');
    expect(page.body.querySelector('.total-coverage strong')?.textContent).toBe('5 years');
  });
});
