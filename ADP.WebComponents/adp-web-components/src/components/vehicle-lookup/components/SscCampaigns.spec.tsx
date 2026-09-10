import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import sscLocale from '../../../locales/vehicleLookup/ssc/en.json';
import standardDealerVehicleLookup from '../../../features/mocks/data/generated/standard-dealer/vehicle-lookup.json';
import type { SscDTO } from '~types/generated/vehicle-lookup/ssc-dto';

import { BodyKind, ManufacturerCheckStatus, SscCampaigns, openDrawerKey, panelBody, panelLead, panelVerdict, sscTraceKey } from './SscCampaigns';

type RenderOptions = {
  vehicleLoaded?: boolean;
  authorized?: boolean;
  campaigns?: SscDTO[];
  skipped?: boolean;
  loading?: boolean;
  checking?: boolean;
  retainedBody?: BodyKind;
  checkAvailable?: boolean;
  checkStatus?: ManufacturerCheckStatus | null;
  showTrace?: boolean;
  onRunLookup?: () => void;
};

const render = ({
  vehicleLoaded = true,
  authorized,
  campaigns,
  skipped = false,
  loading = false,
  checking = false,
  retainedBody = 'none',
  checkAvailable = true,
  checkStatus,
  showTrace = false,
  onRunLookup = () => undefined,
}: RenderOptions) =>
  newSpecPage({
    components: [],
    template: () => (
      <SscCampaigns
        locale={sscLocale}
        vehicleLoaded={vehicleLoaded}
        authorized={authorized}
        campaigns={campaigns}
        skipped={skipped}
        loading={loading}
        checking={checking}
        retainedBody={retainedBody}
        checkAvailable={checkAvailable}
        checkStatus={checkStatus}
        onRunLookup={onRunLookup}
        showTrace={showTrace}
        traces={{}}
        traceLoading={false}
        onToggleTrace={() => undefined}
      >
        <div class="widget-stand-in" />
      </SscCampaigns>
    ),
  });

type Page = { body: HTMLElement };

const badge = (page: Page) => page.body.querySelector('.ssc-summary .status-badge');
const verdictOf = (page: Page) => page.body.querySelector('.ssc-card')?.getAttribute('data-verdict');
const phaseOf = (page: Page) => page.body.querySelector('.ssc-card')?.getAttribute('data-phase');
const leadOf = (page: Page) => page.body.querySelector('.ssc-lead')?.getAttribute('data-lead');
const noticeText = (page: Page) => page.body.querySelector('.ssc-lead-notice')?.textContent?.trim();
const bodyEl = (page: Page) => page.body.querySelector('.ssc-body');
const bodyOpen = (page: Page) => bodyEl(page)?.getAttribute('data-open') === 'true';
const rows = (page: Page) => page.body.querySelectorAll('.ssc-row');

// Through unknown: the JSON fixture is looser than the generated DTO (optional descriptions), and that is fine for what is asserted here.
const fixtures = standardDealerVehicleLookup as unknown as Record<string, { isAuthorized: boolean; ssc?: SscDTO[] | null }>;

/** The generated fixture of a vehicle the distributor has no record of: `isAuthorized: false`, no campaign list. */
const unknownVehicle = fixtures['UNKNOWN_VIN_12345'];

/** The generated fixture of an authorized vehicle with one open and one repaired campaign. */
const authorizedVehicle = fixtures['JTMHX01J8L4198293'];

const state = (overrides: Partial<Parameters<typeof panelVerdict>[0]>) => ({
  locale: sscLocale,
  vehicleLoaded: true,
  skipped: false,
  checkAvailable: true,
  ...overrides,
});

/**
 * Campaign status is a legal matter. These tests pin the rule spelled out on `panelVerdict`: an
 * unauthorized vehicle — one the distributor has no records for — is never given a verdict the
 * distributor's own data cannot support. An empty list on such a vehicle is absence of data, not a
 * clean bill, and only the manufacturer's answer may say otherwise. A vehicle whose lookup skipped
 * this panel is told so, never left blank.
 *
 * They also pin the card's shape: header, lead strip and body exist in every state, the strip's
 * layers only swap which one is active, and the body keeps its outgoing content while it shuts —
 * the structure that lets every state change be a movement (lookup-motion.css).
 */
describe('SscCampaigns', () => {
  it('asserts nothing before a vehicle has been looked up', async () => {
    const page = await render({ vehicleLoaded: false });

    expect(badge(page)?.classList.contains('is-idle')).toBe(true);
    expect(badge(page)?.getAttribute('aria-hidden')).toBe('true');
    expect(verdictOf(page)).toBe('idle');
    // The strip is a skeleton and the body is shut: no headings, no rows, nothing said.
    expect(leadOf(page)).toBe('skeleton');
    expect(bodyOpen(page)).toBe(false);
    expect(rows(page)).toHaveLength(0);
    expect(noticeText(page)).toBe('');
    expect(page.body.textContent).not.toContain(sscLocale.noCampaigns);
    expect(page.body.textContent).not.toContain(sscLocale.unauthorizedCheck);
  });

  it('never turns an unauthorized vehicle’s missing records into a clean bill', async () => {
    expect(unknownVehicle.isAuthorized).toBe(false);
    expect(unknownVehicle.ssc ?? []).toHaveLength(0);

    const page = await render({ authorized: unknownVehicle.isAuthorized, campaigns: unknownVehicle.ssc ?? undefined });

    // No verdict: not green, not a count, not a list — the distributor has nothing to say.
    expect(verdictOf(page)).toBe('neutral');
    expect(badge(page)?.classList.contains('is-neutral')).toBe(true);
    expect(badge(page)?.textContent).toContain(sscLocale.notInRecords);
    expect(page.body.textContent).not.toContain(sscLocale.noCampaigns);
    expect(page.body.textContent).not.toContain(sscLocale.noPendingCampaign);
    expect(leadOf(page)).toBe('notice');
    expect(rows(page)).toHaveLength(0);
    // The headings layer exists (it is one of the strip's layers) but is not the one showing.
    expect(page.body.querySelector('.ssc-lead-columns')?.getAttribute('aria-hidden')).toBe('true');
    // Only the way to get a verdict: the manufacturer check, with the widget the owner supplies.
    expect(noticeText(page)).toBe(sscLocale.unauthorizedCheck);
    expect(bodyOpen(page)).toBe(true);
    expect(bodyEl(page)?.getAttribute('data-body')).toBe('check');
    expect(page.body.querySelector('.ssc-check .widget-stand-in')).not.toBeNull();
  });

  it('ignores a campaign list on an unauthorized vehicle rather than showing it beside the check', async () => {
    // The server derives isAuthorized partly from having SSC records, so this shape is a data fault.
    const page = await render({ authorized: false, campaigns: authorizedVehicle.ssc ?? [] });

    expect(rows(page)).toHaveLength(0);
    expect(leadOf(page)).toBe('notice');
    expect(page.body.textContent).not.toContain(`${sscLocale.open} ·`);
    expect(noticeText(page)).toBe(sscLocale.unauthorizedCheck);
    expect(verdictOf(page)).toBe('neutral');
  });

  it('reports only the manufacturer’s answer for an unauthorized vehicle', async () => {
    const pending = await render({ authorized: false, checkStatus: 'recallExists' });
    expect(verdictOf(pending)).toBe('negative');
    expect(badge(pending)?.textContent).toContain(sscLocale.pendingCampaign);
    expect(noticeText(pending)).toBe(sscLocale.recallExists);
    expect(rows(pending)).toHaveLength(0);
    // Answered: nothing left below the strip.
    expect(bodyOpen(pending)).toBe(false);

    const clear = await render({ authorized: false, checkStatus: 'noRecall' });
    expect(verdictOf(clear)).toBe('positive');
    expect(noticeText(clear)).toBe(sscLocale.noRecall);
    // The distributor's "no campaign affects this vehicle" wording stays reserved for its own records.
    expect(clear.body.textContent).not.toContain(sscLocale.noCampaigns);

    const unknown = await render({ authorized: false, checkStatus: 'noApplicableVehicleFound' });
    expect(verdictOf(unknown)).toBe('neutral');
    expect(noticeText(unknown)).toBe(sscLocale.noApplicableVehicleFound);
  });

  it('keeps the widget on screen while the answer shuts the body over it', async () => {
    const page = await render({ authorized: false, checkStatus: 'noRecall', retainedBody: 'check' });

    expect(bodyOpen(page)).toBe(false);
    expect(bodyEl(page)?.getAttribute('data-body')).toBe('check');
    expect(page.body.querySelector('.ssc-check .widget-stand-in')).not.toBeNull();
  });

  it('says which check is missing when the host configured no manufacturer check', async () => {
    const page = await render({ authorized: false, checkAvailable: false });

    expect(noticeText(page)).toBe(sscLocale.unauthorizedNoCheck);
    expect(page.body.textContent).not.toContain(sscLocale.unauthorizedCheck);
    expect(verdictOf(page)).toBe('neutral');
    expect(bodyOpen(page)).toBe(false);
  });

  it('says no campaign only when the distributor’s own records say so', async () => {
    const page = await render({ authorized: true, campaigns: [] });

    expect(verdictOf(page)).toBe('positive');
    expect(leadOf(page)).toBe('notice');
    expect(noticeText(page)).toBe(sscLocale.noCampaigns);
    expect(rows(page)).toHaveLength(0);
    expect(bodyOpen(page)).toBe(false);
    expect(page.body.textContent).not.toContain(sscLocale.unauthorizedCheck);
  });

  it('lists an authorized vehicle’s campaigns under the column headings, with their counts', async () => {
    expect(authorizedVehicle.isAuthorized).toBe(true);

    const page = await render({ authorized: true, campaigns: authorizedVehicle.ssc ?? [], showTrace: true });

    expect(rows(page)).toHaveLength(2);
    expect(leadOf(page)).toBe('columns');
    expect(page.body.querySelector('.ssc-lead-columns')?.getAttribute('aria-hidden')).toBeNull();
    expect(bodyOpen(page)).toBe(true);
    expect(bodyEl(page)?.getAttribute('data-body')).toBe('rows');
    expect(verdictOf(page)).toBe('negative');
    expect(badge(page)?.textContent).toContain(`1 ${sscLocale.open} · 1 ${sscLocale.repaired}`);
    expect(page.body.querySelectorAll('.ssc-trace-button')).toHaveLength(2);
    // Every drawer names what it is and whose it is, so it cannot be read as the next row.
    expect(page.body.querySelectorAll('.ssc-trace .trace-head-code')).toHaveLength(2);
    expect(page.body.querySelector('.ssc-trace .trace-head-code')?.textContent).toBe(authorizedVehicle.ssc?.[0]?.sscCode);
    expect(noticeText(page)).toBe('');
    expect(page.body.textContent).not.toContain(sscLocale.unauthorizedCheck);
  });

  it('tells the reader the check was skipped for this vehicle, and offers to run it', async () => {
    const onRunLookup = jest.fn();
    const page = await render({ vehicleLoaded: false, skipped: true, onRunLookup });

    // Neither idle nor a verdict: a required action not taken, in a hue of its own — not the amber of
    // "not in the records" / "vehicle not found" (statements), not the red of an open campaign.
    expect(verdictOf(page)).toBe('attention');
    expect(badge(page)?.classList.contains('is-attention')).toBe(true);
    expect(badge(page)?.textContent).toContain(sscLocale.skipped);
    expect(page.body.querySelector('.ssc-lead-notice')?.classList.contains('is-attention')).toBe(true);
    expect(leadOf(page)).toBe('notice');
    expect(noticeText(page)).toBe(sscLocale.skippedNotice);
    expect(page.body.textContent).not.toContain(sscLocale.noCampaigns);
    expect(rows(page)).toHaveLength(0);
    expect(bodyOpen(page)).toBe(true);
    expect(bodyEl(page)?.getAttribute('data-body')).toBe('run');

    (page.body.querySelector('.ssc-run-button') as HTMLElement).click();
    expect(onRunLookup).toHaveBeenCalledTimes(1);
  });

  it('shuts the body over the outgoing rows while a lookup is in flight', async () => {
    const page = await render({ authorized: true, campaigns: authorizedVehicle.ssc ?? [], loading: true, retainedBody: 'rows' });

    expect(phaseOf(page)).toBe('busy');
    expect(leadOf(page)).toBe('skeleton');
    expect(bodyOpen(page)).toBe(false);
    // Still rendered: they slide away with the body instead of vanishing.
    expect(rows(page)).toHaveLength(2);
  });

  it('keeps the body open on the widget while the manufacturer is being asked', async () => {
    const page = await render({ authorized: false, checking: true });

    expect(phaseOf(page)).toBe('busy');
    expect(leadOf(page)).toBe('skeleton');
    expect(bodyOpen(page)).toBe(true);
    expect(page.body.querySelector('.ssc-checking-slot')?.getAttribute('data-open')).toBe('true');
    expect(page.body.textContent).toContain(sscLocale.checkingTMC);
  });

  describe('panelVerdict', () => {
    it('gives no verdict for an unauthorized vehicle until the manufacturer answers', () => {
      expect(panelVerdict(state({ authorized: false, campaigns: [] }))).toEqual({ state: 'neutral', text: sscLocale.notInRecords });
      expect(panelVerdict(state({ authorized: false, campaigns: [], checkStatus: 'noRecall' }))).toEqual({ state: 'positive', text: sscLocale.noPendingCampaign });
    });

    it('treats an empty list as clear only on an authorized vehicle', () => {
      expect(panelVerdict(state({ authorized: true, campaigns: [] })).state).toBe('positive');
      expect(panelVerdict(state({ authorized: false, campaigns: [] })).state).toBe('neutral');
    });

    it('names the skipped check rather than staying idle', () => {
      expect(panelVerdict(state({ vehicleLoaded: false, skipped: true }))).toEqual({ state: 'attention', text: sscLocale.skipped });
      expect(panelVerdict(state({ vehicleLoaded: false }))).toEqual({ state: 'idle', text: '' });
    });
  });

  describe('panelBody and panelLead', () => {
    it('put a list under headings, a check or an action under a notice, and nothing under a verdict', () => {
      const listed = state({ authorized: true, campaigns: authorizedVehicle.ssc ?? [] });
      expect(panelBody(listed)).toBe('rows');
      expect(panelLead(listed, false)).toBe('columns');

      const unauthorized = state({ authorized: false });
      expect(panelBody(unauthorized)).toBe('check');
      expect(panelLead(unauthorized, false)).toBe('notice');
      expect(panelBody(state({ authorized: false, checkAvailable: false }))).toBe('none');
      expect(panelBody(state({ authorized: false, checkStatus: 'recallExists' }))).toBe('none');

      const skipped = state({ vehicleLoaded: false, skipped: true });
      expect(panelBody(skipped)).toBe('run');
      expect(panelLead(skipped, false)).toBe('notice');

      expect(panelBody(state({ authorized: true, campaigns: [] }))).toBe('none');
      expect(panelLead(state({ authorized: true, campaigns: [] }), false)).toBe('notice');
    });

    it('show the skeleton whenever something is in flight, whatever the state', () => {
      expect(panelLead(state({ authorized: true, campaigns: authorizedVehicle.ssc ?? [] }), true)).toBe('skeleton');
      expect(panelLead(state({ authorized: false }), true)).toBe('skeleton');
      expect(panelLead(state({ vehicleLoaded: false }), false)).toBe('skeleton');
    });
  });

  describe('openDrawerKey', () => {
    const campaigns = authorizedVehicle.ssc ?? [];
    const key = sscTraceKey(campaigns[0], 0);
    const bare = campaigns.map(item => ({ ...item, trace: undefined }));

    it('names the wanted row once its evidence is on hand, or once getting it has failed, never while fetching', () => {
      expect(openDrawerKey({ campaigns, showTrace: true, openTraceKey: key, traces: {}, traceLoading: false })).toBe(key);
      expect(openDrawerKey({ campaigns: bare, showTrace: true, openTraceKey: key, traces: { [key]: campaigns[0].trace }, traceLoading: false })).toBe(key);
      expect(openDrawerKey({ campaigns: bare, showTrace: true, openTraceKey: key, traces: {}, traceLoading: true })).toBeUndefined();
      expect(openDrawerKey({ campaigns: bare, showTrace: true, openTraceKey: key, traces: {}, traceLoading: false })).toBe(key);
    });

    it('names nothing without the trace control, a wanted row, or a row to match', () => {
      expect(openDrawerKey({ campaigns, showTrace: false, openTraceKey: key, traces: {}, traceLoading: false })).toBeUndefined();
      expect(openDrawerKey({ campaigns, showTrace: true, openTraceKey: undefined, traces: {}, traceLoading: false })).toBeUndefined();
      expect(openDrawerKey({ campaigns, showTrace: true, openTraceKey: 'missing#9', traces: {}, traceLoading: false })).toBeUndefined();
    });
  });
});
