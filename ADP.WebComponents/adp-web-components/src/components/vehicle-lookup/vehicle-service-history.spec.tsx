import { SharedLocales } from '~features/multi-lingual';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import { PanelState, listed, panelBody, panelPlaceholder, panelVerdict } from './components/VehicleServiceHistoryPanel';

import englishShared from '../../locales/en.json';

const shared = englishShared as unknown as SharedLocales;

const visit = {
  serviceType: 'Periodic maintenance',
  serviceDate: '2025-03-14',
  mileage: 71828,
  companyName: 'Dealer',
  branchName: 'Branch',
  invoiceNumber: 'INV-1',
} as VehicleServiceHistoryDTO;

describe('vehicle-service-history — the honesty rule', () => {
  it('lists nothing for an unauthorized vehicle, even when the data carries visits, and is amber', () => {
    const state: PanelState = { vehicleLoaded: true, authorized: false, visits: [visit, visit] };
    const verdict = panelVerdict(state, shared);

    expect(listed(state)).toEqual([]);
    expect(panelBody(state)).toBe('none');
    expect(verdict.state).toBe('neutral');
    expect(verdict.text).toBe(shared.notInRecords);
    expect(verdict.accent).toBe('neutral');
  });

  it('greys an authorized vehicle with no visits, never green, and shows the empty placeholder', () => {
    const state: PanelState = { vehicleLoaded: true, authorized: true, visits: [] };
    const verdict = panelVerdict(state, shared);

    expect(verdict).toEqual({ state: 'idle', text: shared.noRecords, accent: 'idle' });
    expect(panelPlaceholder(state)).toBe('empty');
  });

  it('has no pill and no row before a lookup', () => {
    const state: PanelState = { vehicleLoaded: false };

    expect(panelVerdict(state, shared).text).toBe('');
    expect(listed(state)).toEqual([]);
    expect(panelPlaceholder(state)).toBe('idle');
  });

  it('is negative on a failed lookup and shows the idle placeholder', () => {
    const state: PanelState = { vehicleLoaded: false, error: 'Something went wrong' };
    const verdict = panelVerdict(state, shared);

    expect(verdict.state).toBe('negative');
    expect(verdict.accent).toBe('negative');
    expect(panelPlaceholder(state)).toBe('idle');
  });
});
