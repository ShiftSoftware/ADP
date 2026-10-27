import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import paintLocale from '../../../locales/vehicleLookup/paintThickness/en.json';

import type { PaintThicknessInspectionDTO } from '~types/generated/vehicle-lookup/paint-thickness-inspection-dto';

import { tableSlots } from '~features/lookup-table';

import { PanelState, VehiclePaintThicknessPanel, allPhotoGroups, bodyOpen, certificateKind, panelBody, shownRows, tabsOf } from './VehiclePaintThicknessPanel';

/**
 * The panel's states through the DOM: what each named state renders, what stays in place while a
 * lookup is in flight, and the tab strip's keyboard. The panel is pure, so every case is a props set.
 * There is no skipped state: this panel has no `skipLookup`.
 */

const locale = paintLocale as never;

const inspection = (patch: Partial<PaintThicknessInspectionDTO> = {}): PaintThicknessInspectionDTO => ({
  source: 'PDI',
  inspectionDate: '2025-01-19',
  panels: [
    { panelType: 'Door', panelPosition: 'Front', panelSide: 'Left', measuredThickness: 100, images: ['a.jpg', 'b.jpg'] },
    { panelType: 'Door', panelPosition: 'Front', panelSide: 'Right', measuredThickness: 110, images: ['c.jpg'] },
    { panelType: 'Roof', panelPosition: null, panelSide: null, measuredThickness: 90, images: [] },
  ],
  ...patch,
});

const loaded = (patch: Partial<PanelState> = {}): PanelState => ({
  vehicleLoaded: true,
  authorized: true,
  inspections: [inspection(), inspection({ source: 'Preowned' }), inspection({ source: 'Third' })],
  shownIndex: 0,
  hidden: new Set(),
  showSummary: false,
  showCertificateButton: true,
  certificateAvailable: true,
  certificateUrls: [{ language: 'en', name: 'English', url: 'https://example.test/c' }],
  ...patch,
});

type Options = { state?: PanelState; busy?: boolean; direction?: string; drawerOpen?: number | null; verdictText?: string; onSelectTab?: (index: number) => void };

const render = ({ state = loaded(), busy = false, direction = 'ltr', drawerOpen = null, verdictText = '', onSelectTab = () => undefined }: Options = {}) => {
  const rows = shownRows(state);
  const open = bodyOpen(state, false, false);
  const groups = allPhotoGroups(rows, locale);

  return newSpecPage({
    components: [],
    template: () => (
      <VehiclePaintThicknessPanel
        locale={locale}
        language="en"
        direction={direction}
        state={state}
        verdict={{ state: verdictText ? 'negative' : 'idle', text: verdictText }}
        busy={busy}
        leaving={false}
        reconfiguring={false}
        reshaping={false}
        swapping={false}
        retainedBody={panelBody(state)}
        selectedTab={0}
        retainedTabs={[]}
        retainedCertificate={null}
        retainedSummary={null}
        slotRows={rows}
        slots={tableSlots({ current: rows.length, outgoing: 0, opened: rows.length, bodyOpen: open, currentVeil: busy ? 'cover' : 'none', outgoingVeil: 'none' })}
        drawerOpen={drawerOpen}
        drawerShown={drawerOpen === null ? [] : [drawerOpen]}
        drawerRowFor={row => row}
        onToggleDrawer={() => undefined}
        photosOpen={false}
        photosRendered={false}
        retainedGroups={groups}
        onTogglePhotos={() => undefined}
        onSelectTab={onSelectTab}
        certificateOpen={false}
        onToggleCertificate={() => undefined}
        onCloseCertificate={() => undefined}
      />
    ),
  });
};

const attr = (root: HTMLElement, selector: string, name: string) => root.querySelector(selector)?.getAttribute(name);
const layer = (root: HTMLElement, index: number) => root.querySelectorAll('.paint-placeholder .layer')[index]?.getAttribute('data-active');

describe('the named states', () => {
  it('idle: no rows, the "look a vehicle up" placeholder, no tabs, no certificate', async () => {
    const { body } = await render({ state: loaded({ vehicleLoaded: false, inspections: undefined }) });

    expect(attr(body, '.lt-body', 'data-open')).toBe('false');
    expect(layer(body, 0)).toBe('true');
    expect(attr(body, '.paint-bar', 'data-open')).toBe('false');
    expect(attr(body, '.paint-certificate-slot', 'data-open')).toBe('false');
    expect(body.querySelector('.status-badge')).toBeNull();
  });

  it('loaded: a row per panel, one tab per inspection, the certificate in the head and the photos section', async () => {
    const { body } = await render();

    expect(body.querySelectorAll('.lt-row')).toHaveLength(2);
    expect(Array.from(body.querySelectorAll('.paint-tabs-label')).map(label => label.textContent)).toEqual(['PDI', 'Preowned', 'Third']);
    expect(attr(body, '.lt-body', 'data-open')).toBe('true');
    expect(attr(body, '.paint-photos', 'data-open')).toBe('true');
    expect(body.querySelector('.lookup-head .paint-certificate-button')).not.toBeNull();
  });

  it('empty: an authorized vehicle with nothing on file says so in the placeholder', async () => {
    const { body } = await render({ state: loaded({ inspections: [] }) });

    expect(attr(body, '.lt-body', 'data-open')).toBe('false');
    expect(layer(body, 1)).toBe('true');
    expect(attr(body, '.paint-bar', 'data-open')).toBe('false');
  });

  it('unauthorized: lists nothing whatever the DTO holds', async () => {
    const state = loaded({ authorized: false });
    const { body } = await render({ state });

    expect(tabsOf(state, locale)).toEqual([]);
    expect(certificateKind(state)).toBe('hidden');
    expect(attr(body, '.lt-body', 'data-open')).toBe('false');
    expect(layer(body, 0)).toBe('true');
  });

  it('error: the pill carries the message and nothing is listed', async () => {
    const { body } = await render({ state: loaded({ vehicleLoaded: false, error: 'It failed' }), verdictText: 'It failed' });

    expect(body.querySelector('.status-badge.is-negative')?.textContent).toContain('It failed');
    expect(attr(body, '.lt-body', 'data-open')).toBe('false');
  });
});

describe('a lookup in flight', () => {
  it('keeps the rows, the tabs and the photos section where they are, under the skeleton', async () => {
    const { body } = await render({ busy: true });

    expect(attr(body, '.paint-card', 'data-phase')).toBe('busy');
    expect(attr(body, '.lt-body', 'data-open')).toBe('true');
    expect(body.querySelectorAll('.lt-row.loading')).toHaveLength(2);
    expect(body.querySelectorAll('.paint-tabs-tab')).toHaveLength(3);
    expect(attr(body, '.paint-bar', 'data-open')).toBe('true');
    expect(attr(body, '.paint-photos', 'data-open')).toBe('true');
  });

  it('an open drawer stays open and keeps its photos', async () => {
    const { body } = await render({ busy: true, drawerOpen: 0 });

    expect(attr(body, '.lt-drawer', 'data-open')).toBe('true');
    expect(body.querySelectorAll('.lt-drawer .paint-photo')).toHaveLength(3);
  });
});

describe('the tab strip', () => {
  const press = (body: HTMLElement, index: number, key: string) =>
    body.querySelectorAll('.paint-tabs-tab')[index].dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));

  it('is a tablist with a roving tabindex; arrows follow the reading direction, Home and End go to the ends', async () => {
    const calls: number[] = [];
    const ltr = await render({ onSelectTab: index => calls.push(index) });

    expect(attr(ltr.body, '.paint-tabs', 'role')).toBe('tablist');
    expect(Array.from(ltr.body.querySelectorAll('.paint-tabs-tab')).map(tab => tab.getAttribute('tabindex'))).toEqual(['0', '-1', '-1']);

    press(ltr.body, 1, 'ArrowRight');
    press(ltr.body, 1, 'End');
    press(ltr.body, 1, 'Home');
    expect(calls).toEqual([2, 2, 0]);

    const rtl = await render({ direction: 'rtl', onSelectTab: index => calls.push(index) });
    press(rtl.body, 1, 'ArrowRight');
    press(rtl.body, 1, 'ArrowLeft');
    expect(calls.slice(3)).toEqual([0, 2]);
  });
});
