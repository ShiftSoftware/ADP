import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import warrantyLocale from '../../../locales/vehicleLookup/warranty/en.json';
import standardDealerVehicleLookup from '../../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';
import type { VehicleExtendedWarrantyDTO } from '~types/generated/vehicle-lookup/vehicle-extended-warranty-dto';

import ExtendedWarrantyCards from './ExtendedWarrantyCards';

const renderCards = (extendedWarranties?: VehicleExtendedWarrantyDTO[]) =>
  newSpecPage({
    components: [],
    template: () => <ExtendedWarrantyCards extendedWarranties={extendedWarranties} warrantyLocale={warrantyLocale} />,
  });

describe('ExtendedWarrantyCards', () => {
  it('renders nothing when an older API omits the collection', async () => {
    const page = await renderCards(undefined);

    expect(page.body.querySelector('.extended-warranties')).toBeNull();
  });

  it('renders every provider and its individual coverage dates', async () => {
    const page = await renderCards([
      {
        id: 'coverage-1',
        providerCompanyID: 'provider-1',
        providerCompanyLogo: 'https://cdn.example.com/provider-1.svg',
        startDate: '2026-01-01',
        endDate: '2027-01-01',
      },
      {
        id: 'coverage-2',
        providerCompanyID: 'provider-2',
        providerCompanyLogo: '',
        startDate: '2027-01-02',
        endDate: '2029-01-01',
      },
    ]);

    const cards = page.body.querySelectorAll('.extended-warranty-card');

    expect(cards).toHaveLength(2);
    expect(cards[0].textContent).toContain('coverage-1');
    expect(cards[0].textContent).toContain('provider-1');
    expect(cards[0].textContent).toContain('2026-01-01');
    expect(cards[0].textContent).toContain('2027-01-01');
    expect(cards[0].querySelector('.extended-warranty-provider-logo')?.getAttribute('src')).toBe('https://cdn.example.com/provider-1.svg');
    expect(cards[1].textContent).toContain('coverage-2');
    expect(cards[1].textContent).toContain('provider-2');
    expect(cards[1].textContent).toContain('2027-01-02');
    expect(cards[1].textContent).toContain('2029-01-01');
    expect(cards[1].querySelector('.extended-warranty-provider-logo-placeholder')).not.toBeNull();
  });

  it('renders the checked-in multiple-warranty example', async () => {
    const sample = standardDealerVehicleLookup.JTMHX01J8L4198293.warranty.extendedWarranties;
    const page = await renderCards(sample);

    const cards = page.body.querySelectorAll('.extended-warranty-card');

    expect(cards).toHaveLength(2);
    expect(cards[0].textContent).toContain('EW-JTMHX01J8L4198293-DISTRIBUTOR');
    expect(cards[0].textContent).toContain('100');
    expect(cards[1].textContent).toContain('EW-JTMHX01J8L4198293-PROVIDER');
    expect(cards[1].textContent).toContain('2');
  });
});
