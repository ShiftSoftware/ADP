import { readFileSync } from 'node:fs';
import path from 'node:path';

import { panelVerdict as warrantyVerdict } from '../components/vehicle-lookup/components/CoverageTimeline';
import { panelVerdict as sscVerdict } from '../components/vehicle-lookup/components/SscCampaigns';
import { hasRecords as saleHasRecords } from '../components/vehicle-lookup/components/VehicleSaleInformationPanel';
import { specificationVerdict } from '../components/vehicle-lookup/components/VehicleSpecificationPanel';
import { recordVerdict } from '../features/vehicle-info-layout/lookup-head';
import warrantyTimelineSchema from '../locales/vehicleLookup/warrantyTimeline/type';

const root = process.cwd();
const tagsSource = readFileSync(path.resolve(root, 'src', 'templates', 'fixture-tags.js'), 'utf8').replaceAll('export ', '');
const FIXTURE_TAGS = new Function(`${tagsSource}\nreturn FIXTURE_TAGS;`)() as Record<string, Record<string, string | [string, string]>>;

// Each panel's own accent for a fixture, as its render() derives it; the locales only carry the pill's
// words, which the accent ignores — except the timeline's, whose notices count only when they have text.
const records = (vehicle: any, hasRecords: boolean) => recordVerdict({ locale: {} as any, vehicleLoaded: !!vehicle?.vin, authorized: vehicle?.isAuthorized, hasRecords }).accent;
const timelineLocale = { ...warrantyTimelineSchema.getDefault(), awaitingActivation: '-', awaitingBrokerInvoice: '-', awaitingEndCustomerSale: '-' } as any;
const ACCENTS: Record<string, (vehicle: any) => string> = {
  'vehicle-specification': vehicle => specificationVerdict(vehicle, { sharedLocales: {} } as any).accent,
  'vehicle-sale-information': vehicle => records(vehicle, saleHasRecords(vehicle)),
  'vehicle-accessories': vehicle => records(vehicle, (vehicle?.accessories?.length ?? 0) > 0),
  'vehicle-warranty-timeline': vehicle => warrantyVerdict({ locale: timelineLocale, vehicleInformation: vehicle, isAuthorized: vehicle?.isAuthorized, today: '2026-09-01' }),
  'vehicle-ssc': vehicle => sscVerdict({ locale: {} as any, vehicleLoaded: !!vehicle, authorized: vehicle?.isAuthorized, campaigns: vehicle?.ssc, skipped: false } as any).state,
  'vehicle-service-history': vehicle => records(vehicle, (vehicle?.serviceHistory?.length ?? 0) > 0),
  'vehicle-paint-thickness': vehicle => records(vehicle, (vehicle?.paintThicknessInspections?.length ?? 0) > 0),
  'vehicle-claimable-items': vehicle => records(vehicle, (vehicle?.serviceItems?.length ?? 0) > 0),
};

const website = JSON.parse(readFileSync(path.resolve(root, 'src', 'features', 'mocks', 'data', 'generated', 'website', 'vehicle-lookup.json'), 'utf8'));
const websiteVins = Object.keys(website);
const isWebsiteVin = (vin: string) => vin.startsWith('ZDW');

describe('the website environment', () => {
  it('has every tagged website VIN', () => {
    const missing = Object.values(FIXTURE_TAGS)
      .flatMap(tags => Object.keys(tags))
      .filter(vin => isWebsiteVin(vin) && !websiteVins.includes(vin));

    expect(missing).toEqual([]);
  });

  it('uses every website VIN for at least one case', () => {
    const tagged = new Set(Object.values(FIXTURE_TAGS).flatMap(tags => Object.keys(tags)));

    expect(websiteVins.filter(vin => !tagged.has(vin))).toEqual([]);
  });

  it('colours each tag with the accent its panel draws for that vehicle', () => {
    const wrong = Object.entries(ACCENTS).flatMap(([component, accentOf]) =>
      Object.entries(FIXTURE_TAGS[component])
        .filter(([vin, tag]) => isWebsiteVin(vin) && Array.isArray(tag) && tag[0] !== accentOf(website[vin]))
        .map(([vin, tag]) => `${component} ${vin}: tagged ${tag[0]}, panel draws ${accentOf(website[vin])}`),
    );

    expect(wrong).toEqual([]);
  });
});
