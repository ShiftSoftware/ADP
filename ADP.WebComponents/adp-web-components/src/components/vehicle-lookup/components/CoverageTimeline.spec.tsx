import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import timelineLocale from '../../../locales/vehicleLookup/warrantyTimeline/en.json';
import standardDealerVehicleLookup from '../../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';
import type { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import CoverageTimeline from './CoverageTimeline';

const SNAPSHOT = '2027-06-01';

const renderTimeline = (vehicleInformation?: Partial<VehicleLookupDTO>, today = SNAPSHOT, isAuthorized = true) =>
  newSpecPage({
    components: [],
    template: () => <CoverageTimeline vehicleInformation={vehicleInformation as VehicleLookupDTO} locale={timelineLocale} isAuthorized={isAuthorized} today={today} />,
  });

const bandsOf = (page: { body: HTMLElement }) => [...page.body.querySelectorAll('.coverage-entry')];

const variableOf = (element: Element, name: string) => (element as HTMLElement).style.getPropertyValue(name);

/**
 * The notice stays in the DOM whichever vehicle is loaded so its row can slide open and shut, so
 * "not shown" is a closed row rather than a missing element.
 */
const isShown = (page: { body: HTMLElement }, selector: string) => page.body.querySelector(selector)?.closest('.collapsible')?.getAttribute('data-open') === 'true';

/** Blocks with nothing to put in them keep their box and fade, so emptiness is a flag, not an absence. */
const isFaded = (page: { body: HTMLElement }, selector: string) => page.body.querySelector(selector)?.getAttribute('data-empty') === 'true';

/** Three contiguous bands: standard, then two extended coverages from different providers. */
const threeBandVehicle = {
  saleInformation: { companyName: 'Sample Motors' },
  warranty: {
    warrantyStartDate: '2024-02-01',
    warrantyEndDate: '2027-02-01',
    extendedWarranties: [
      { id: 'EW-SECOND', name: 'Second Extension', providerCompanyID: '2', providerCompanyLogo: 'https://images.test/b.png', startDate: '2028-02-01', endDate: '2029-02-01' },
      { id: 'EW-FIRST', name: 'First Extension', providerCompanyID: '100', providerCompanyLogo: 'https://images.test/a.png', startDate: '2027-02-01', endDate: '2028-02-01' },
    ],
  },
};

describe('CoverageTimeline', () => {
  // Opening the panel is not an accusation. With nothing looked up yet there is no vehicle to judge,
  // and an absent isAuthorized reading as false used to greet the reader with two red chips.
  it('gives no verdict before a vehicle has been looked up', async () => {
    const page = await renderTimeline(undefined);

    const badges = [...page.body.querySelectorAll('.status-badge')];

    // The chips are still there, keeping the row's height for the verdicts to come.
    expect(badges).toHaveLength(2);
    expect(badges.every(badge => badge.classList.contains('is-idle'))).toBe(true);
    expect(page.body.textContent).not.toContain(timelineLocale.unauthorized);
    expect(page.body.textContent).not.toContain(timelineLocale.notActiveWarranty);
    // Nothing to announce either: an empty chip is furniture, not information.
    expect(badges.every(badge => badge.getAttribute('aria-hidden') === 'true')).toBe(true);
  });

  it('gives both verdicts once a vehicle has loaded, uncovered included', async () => {
    const page = await renderTimeline({ saleInformation: { companyName: 'Sample Motors' } } as Partial<VehicleLookupDTO>);

    const badges = [...page.body.querySelectorAll('.status-badge')];

    // The harness authorizes the lookup, so the first chip passes; the vehicle carries no dates,
    // so the second fails. A loaded vehicle with nothing to show still gets a verdict.
    expect(badges.map(badge => badge.className)).toEqual(['status-badge is-positive', 'status-badge is-negative']);
    expect(page.body.textContent).toContain(timelineLocale.notActiveWarranty);
  });

  // The rail and the totals hold their box and fade rather than being dropped, so moving from a
  // covered vehicle to an uncovered one does not resize the card out from under the reader.
  it('keeps the frame and fades the rail when the vehicle has no warranty dates', async () => {
    const page = await renderTimeline({ saleInformation: { companyName: 'Sample Motors' } } as Partial<VehicleLookupDTO>);

    expect(page.body.querySelector('.activation-title')?.textContent).toContain('Sample Motors');
    expect(page.body.querySelectorAll('.status-badge')).toHaveLength(2);
    expect(bandsOf(page)).toHaveLength(0);
    expect(isFaded(page, '.timeline-shell')).toBe(true);
    expect(isFaded(page, '.total-slot')).toBe(true);
  });

  it('renders the standard warranty alone when an older API omits the collection', async () => {
    const page = await renderTimeline({
      warranty: { warrantyStartDate: '2024-02-01', warrantyEndDate: '2027-02-01' },
    } as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands).toHaveLength(1);
    expect(bands[0].getAttribute('data-kind')).toBe('standard');
    // Two different strings, deliberately: the band is marked "Standard Warranty" while the
    // label beneath names the product. Reusing one string read as the same phrase twice.
    expect(bands[0].querySelector('.segment')?.textContent).toBe(timelineLocale.standardWarrantyMark);
    expect(bands[0].querySelector('.coverage-label')?.textContent).toBe(timelineLocale.standardWarranty);
    expect(timelineLocale.standardWarrantyMark).not.toBe(timelineLocale.standardWarranty);
  });

  it('orders every coverage by start date and spans the full range', async () => {
    const page = await renderTimeline(threeBandVehicle as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands.map(band => band.getAttribute('data-kind'))).toEqual(['standard', 'extended', 'extended']);
    // Input order was second-then-first; the rail is sorted chronologically.
    expect(bands[1].textContent).toContain('First Extension');
    expect(bands[2].textContent).toContain('Second Extension');

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

  it('shows a provider logo when one resolved and nothing at all when it did not', async () => {
    const page = await renderTimeline({
      warranty: {
        warrantyStartDate: '2024-02-01',
        warrantyEndDate: '2027-02-01',
        extendedWarranties: [
          {
            id: 'EW-LOGO',
            name: 'Logo Coverage',
            providerCompanyID: '100',
            providerCompanyName: 'Sample Distributor',
            providerCompanyLogo: 'https://images.test/a.png',
            startDate: '2027-02-01',
            endDate: '2028-02-01',
          },
          {
            id: 'EW-BARE',
            name: 'Bare Coverage',
            providerCompanyID: '2',
            providerCompanyName: 'City Auto',
            providerCompanyLogo: '',
            startDate: '2028-02-01',
            endDate: '2029-02-01',
          },
        ],
      },
    } as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands[1].querySelector('.provider-logo')?.getAttribute('src')).toBe('https://images.test/a.png');
    expect(bands[2].querySelector('.provider-logo')).toBeNull();
    // Never the provider name: on a persisted coverage that is whichever company stored the
    // row, not the one standing behind the cover, so an empty band beats a wrong one.
    expect(bands[2].querySelector('.segment')?.textContent).toBe('');
    expect(bands[2].textContent).not.toContain('City Auto');
    // It stays available to a screen reader through the entry's label.
    expect(bands[2].getAttribute('aria-label')).toContain('City Auto');
    // The provider id is a hash and was never meaningful on screen.
    expect(page.body.textContent).not.toContain('EW-BARE');
    expect(page.body.textContent).not.toContain('EW-LOGO');
  });

  it('falls back to the generic label when a coverage carries no name', async () => {
    const page = await renderTimeline({
      warranty: {
        warrantyStartDate: '2024-02-01',
        warrantyEndDate: '2027-02-01',
        // A persisted entry: an identifier and a provider, but no display name.
        extendedWarranties: [{ id: 'EW-JTMHX01J8L4198293-PROVIDER', providerCompanyID: '2', startDate: '2027-02-01', endDate: '2028-02-01' }],
      },
    } as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands[1].querySelector('.coverage-label')?.textContent).toBe(timelineLocale.extendedWarranty);
    expect(page.body.textContent).not.toContain('EW-JTMHX01J8L4198293-PROVIDER');
  });

  it('declares that an un-invoiced broker is why the warranty has not started', async () => {
    const page = await renderTimeline({
      saleInformation: { companyName: 'Sample Motors', broker: { brokerName: 'Rivera Trading' } },
      warranty: { startState: 'AwaitingBrokerInvoice' },
    } as Partial<VehicleLookupDTO>);

    const notice = page.body.querySelector('.warranty-notice');

    // The state the declaration exists for: no dates, so nothing on the rail to hang it off.
    expect(isFaded(page, '.timeline-shell')).toBe(true);
    expect(isShown(page, '.warranty-notice')).toBe(true);
    expect(notice?.textContent).toContain(timelineLocale.awaitingBrokerInvoice);
    expect(notice?.textContent).toContain('Rivera Trading');
    // Nothing activated it, so the company is only the dealer holding it.
    expect(page.body.querySelector('.activation-title')?.textContent).toContain(timelineLocale.dealer);
    expect(page.body.querySelector('.activation-title')?.textContent).not.toContain(timelineLocale.activatedBy);
  });

  // Falling through to the stylesheet default would paint the green/blue/violet coverage gradient
  // across the top of a vehicle that has no coverage at all.
  // Asserted one page at a time on purpose: successive newSpecPage calls share a document, so
  // reading an earlier page's body after rendering a later one returns the later one's DOM.
  it('paints a blocked accent when it is saying why coverage has not started', async () => {
    const page = await renderTimeline({ warranty: { startState: 'AwaitingBrokerInvoice' } } as Partial<VehicleLookupDTO>);

    expect((page.body.querySelector('.warranty-card') as HTMLElement).style.getPropertyValue('--card-accent')).toBe('var(--red)');
  });

  it('paints an inert accent on a bare card with nothing to report', async () => {
    const page = await renderTimeline({ saleInformation: { companyName: 'Sample Motors' } } as Partial<VehicleLookupDTO>);

    expect((page.body.querySelector('.warranty-card') as HTMLElement).style.getPropertyValue('--card-accent')).toBe('var(--line)');
  });

  it('declares supply-chain possession without naming a broker', async () => {
    const page = await renderTimeline({
      saleInformation: { companyName: 'Sample Distributor' },
      warranty: { startState: 'AwaitingEndCustomerSale' },
    } as Partial<VehicleLookupDTO>);

    expect(page.body.querySelector('.warranty-notice')?.textContent).toBe(timelineLocale.awaitingEndCustomerSale);
    expect(isFaded(page, '.broker-slot')).toBe(true);
  });

  it('distinguishes a missing activation from possession', async () => {
    const page = await renderTimeline({
      saleInformation: { companyName: 'Sample Motors' },
      warranty: { startState: 'AwaitingActivation' },
    } as Partial<VehicleLookupDTO>);

    expect(page.body.querySelector('.warranty-notice')?.textContent).toBe(timelineLocale.awaitingActivation);
  });

  it('does not promise an activation to a vehicle it is not authorized for', async () => {
    const page = await renderTimeline({ saleInformation: { companyName: 'Sample Motors' }, warranty: { startState: 'AwaitingActivation' } } as Partial<VehicleLookupDTO>, SNAPSHOT, false);

    expect(isShown(page, '.warranty-notice')).toBe(false);
  });

  it('still states supply-chain possession when unauthorized, because that does not depend on who is asking', async () => {
    const page = await renderTimeline(
      { saleInformation: { companyName: 'Sample Distributor' }, warranty: { startState: 'AwaitingEndCustomerSale' } } as Partial<VehicleLookupDTO>,
      SNAPSHOT,
      false,
    );

    expect(page.body.querySelector('.warranty-notice')?.textContent).toBe(timelineLocale.awaitingEndCustomerSale);
  });

  it('names both the dealer and the broker when a broker invoice started the warranty', async () => {
    const page = await renderTimeline({
      saleInformation: { companyName: 'Sample Motors', broker: { brokerName: 'Rivera Trading' } },
      warranty: {
        startState: 'Started',
        activatedByBrokerName: 'Rivera Trading',
        warrantyStartDate: '2024-02-10',
        warrantyEndDate: '2027-02-10',
      },
    } as Partial<VehicleLookupDTO>);

    expect(isShown(page, '.warranty-notice')).toBe(false);
    expect(page.body.querySelector('.activation-title')?.textContent).toContain('Sample Motors');
    expect(isFaded(page, '.broker-slot')).toBe(false);
    expect(page.body.querySelector('.activation-broker')?.textContent).toContain('Rivera Trading');
    // Both parties are named, so the dealer line stays "Dealer" rather than claiming activation.
    expect(page.body.querySelector('.activation-title')?.textContent).toContain(timelineLocale.dealer);
  });

  it('credits the company directly when nothing but a dealer sale started the warranty', async () => {
    const page = await renderTimeline({
      saleInformation: { companyName: 'Sample Motors' },
      warranty: { startState: 'Started', warrantyStartDate: '2024-02-01', warrantyEndDate: '2027-02-01' },
    } as Partial<VehicleLookupDTO>);

    expect(page.body.querySelector('.activation-title')?.textContent).toContain(timelineLocale.activatedBy);
    expect(isFaded(page, '.broker-slot')).toBe(true);
  });

  it('stays silent for an older API response that carries no start state', async () => {
    const page = await renderTimeline(threeBandVehicle as Partial<VehicleLookupDTO>);

    expect(isShown(page, '.warranty-notice')).toBe(false);
  });

  // Which blocks wear the shared skeleton is a design split, not an accident, and getting it wrong
  // is invisible to every other assertion here. The rail is graphical, so it shimmers. The prose
  // blocks animate their own text instead, which they cannot do underneath an opaque overlay. And
  // the shell is excluded because the skeleton drops borders, and its 1px border is layout-bearing
  // — shimmering it there shrank the whole card by 2px as it loaded.
  it('shimmers the rail and leaves the prose blocks to animate their own text', async () => {
    const page = await renderTimeline({
      saleInformation: { companyName: 'Sample Motors', broker: { brokerName: 'Rivera Trading' } },
      warranty: { startState: 'AwaitingBrokerInvoice', activatedByBrokerName: 'Rivera Trading', warrantyStartDate: '2024-02-01', warrantyEndDate: '2027-02-01' },
    } as Partial<VehicleLookupDTO>);

    expect(page.body.querySelector('.timeline')?.classList.contains('shift-skeleton')).toBe(true);

    for (const selector of ['.timeline-shell', '.activation-title', '.activation-broker', '.total-coverage', '.warranty-notice'])
      expect(page.body.querySelector(selector)?.classList.contains('shift-skeleton')).toBe(false);

    expect([...page.body.querySelectorAll('.status-badge')].some(badge => badge.classList.contains('shift-skeleton'))).toBe(false);
  });

  // A block leaving and the text inside it arriving are separate fades that multiply, so the pair
  // ghosts back into view mid-exit unless the wrapper's state can reach the text and pin it down.
  // That rule is keyed on this nesting; flatten it and the rule silently stops applying, with
  // nothing else in this file to notice.
  it('keeps the text of each block inside the wrapper that reports its state', async () => {
    const page = await renderTimeline({
      saleInformation: { companyName: 'Sample Motors', broker: { brokerName: 'Rivera Trading' } },
      warranty: { startState: 'AwaitingBrokerInvoice', activatedByBrokerName: 'Rivera Trading', warrantyStartDate: '2024-02-01', warrantyEndDate: '2027-02-01' },
    } as Partial<VehicleLookupDTO>);

    expect(page.body.querySelector('.total-coverage')?.closest('.total-slot')?.hasAttribute('data-empty')).toBe(true);
    expect(page.body.querySelector('.activation-broker')?.closest('.broker-slot')?.hasAttribute('data-empty')).toBe(true);
    expect(page.body.querySelector('.warranty-notice')?.closest('.collapsible')?.hasAttribute('data-open')).toBe(true);
  });

  // The rail runs through to today rather than stopping at the last expiry date. Stopping there
  // put today hard against the rail's edge and threw away the one thing a lapsed warranty most
  // needs to show — how long ago cover ran out. Here that becomes what it is: empty track.
  it('draws the rail through to today when the warranty has lapsed', async () => {
    const page = await renderTimeline({ warranty: { warrantyStartDate: '2020-11-23', warrantyEndDate: '2023-11-23' } } as Partial<VehicleLookupDTO>, '2026-08-15');

    const band = bandsOf(page)[0];
    const bandEnd = parseFloat(variableOf(band, '--start')) + parseFloat(variableOf(band, '--span'));
    const today = parseFloat(variableOf(page.body.querySelector('.today-marker')!, '--at'));

    // Three years of cover against nearly three more since it lapsed: about half the rail each.
    expect(bandEnd).toBeCloseTo(50.35, 1);
    expect(today).toBeCloseTo(96.15, 1);
    // Every part of the rail is behind us, the empty stretch included.
    expect(parseFloat(variableOf(page.body.querySelector('.past-wash')!, '--to'))).toBeCloseTo(96.15, 1);

    // The closing date no longer sits at the rail's end, so it is centred on its tick rather than
    // set against the right edge.
    const dates = [...page.body.querySelectorAll('.axis-date')];
    expect(dates[dates.length - 1].getAttribute('data-align')).toBe('mid');
  });

  it('draws the rail back to today when cover has not begun', async () => {
    const page = await renderTimeline({ warranty: { warrantyStartDate: '2030-01-01', warrantyEndDate: '2033-01-01' } } as Partial<VehicleLookupDTO>, '2026-08-15');

    const today = parseFloat(variableOf(page.body.querySelector('.today-marker')!, '--at'));

    expect(today).toBeCloseTo(3.85, 1);
    expect(today).toBeLessThan(parseFloat(variableOf(bandsOf(page)[0], '--start')));
    expect(page.body.querySelector('.axis-date')?.getAttribute('data-align')).toBe('mid');
  });

  it('fits the rail to the cover exactly while it is running', async () => {
    const page = await renderTimeline({ warranty: { warrantyStartDate: '2026-01-01', warrantyEndDate: '2028-01-01' } } as Partial<VehicleLookupDTO>, '2027-01-01');

    const band = bandsOf(page)[0];

    expect(variableOf(band, '--start')).toBe('0.000000%');
    expect(variableOf(band, '--span')).toBe('100.000000%');
    expect(variableOf(page.body.querySelector('.today-marker')!, '--at')).toBe('50.000000%');
    expect(page.body.querySelector('.axis-date')?.getAttribute('data-align')).toBe('start');
  });

  it('renders the checked-in multiple-warranty sample', async () => {
    const page = await renderTimeline(standardDealerVehicleLookup.JTMHX01J8L4198293 as unknown as Partial<VehicleLookupDTO>);

    const bands = bandsOf(page);

    expect(bands).toHaveLength(3);
    // Both are persisted entries, so neither carries a name and both take the generic
    // label; the providers are told apart by their resolved logos.
    expect(bands[1].querySelector('.coverage-label')?.textContent).toBe(timelineLocale.extendedWarranty);
    expect(bands[2].querySelector('.coverage-label')?.textContent).toBe(timelineLocale.extendedWarranty);
    expect(bands[1].querySelector('.provider-logo')).not.toBeNull();
    expect(bands[2].querySelector('.provider-logo')).not.toBeNull();
    // The resolved provider names reach the accessible description.
    expect(bands[1].getAttribute('aria-label')).toContain('Sample Distributor');
    expect(bands[2].getAttribute('aria-label')).toContain('City Auto');
    expect(page.body.textContent).not.toContain('EW-JTMHX01J8L4198293');
    expect(page.body.querySelector('.total-coverage strong')?.textContent).toBe('5 years');
  });
});
