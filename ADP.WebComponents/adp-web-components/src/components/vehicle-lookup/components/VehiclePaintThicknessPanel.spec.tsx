import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import paintLocale from '../../../locales/vehicleLookup/paintThickness/en.json';

import type { PaintThicknessInspectionDTO } from '~types/generated/vehicle-lookup/paint-thickness-inspection-dto';

import { tableSlots } from '~features/lookup-table';

import { ShiftTabs } from '../../components/shift-tabs';

import { PanelState, VehiclePaintThicknessPanel, allPhotoGroups, bodyOpen, certificateKind, panelBody, shownRows, tabsOf } from './VehiclePaintThicknessPanel';
import type { PaintSheet } from './VehiclePaintThicknessPanel';

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
    components: [ShiftTabs],
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
        retainedPlaceholder={null}
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
const layer = (root: HTMLElement, index: number) => root.querySelectorAll('.lt-placeholder .layer')[index]?.getAttribute('data-active');

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
    expect(Array.from(body.querySelectorAll('.shift-tabs-label')).map(label => label.textContent)).toEqual(['PDI', 'Preowned', 'Third']);
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
    expect(layer(body, 3)).toBe('true');
  });

  it('error: the pill carries the message and nothing is listed', async () => {
    const { body } = await render({ state: loaded({ vehicleLoaded: false, error: 'It failed' }), verdictText: 'It failed' });

    expect(body.querySelector('.status-badge.is-negative')?.textContent).toContain('It failed');
    expect(attr(body, '.lt-body', 'data-open')).toBe('false');
    expect(layer(body, 2)).toBe('true');
  });
});

describe('a lookup in flight', () => {
  it('keeps the rows, the tabs and the photos section where they are, under the skeleton', async () => {
    const { body } = await render({ busy: true });

    expect(attr(body, '.paint-card', 'data-phase')).toBe('busy');
    expect(attr(body, '.lt-body', 'data-open')).toBe('true');
    expect(body.querySelectorAll('.lt-row.loading')).toHaveLength(2);
    expect(body.querySelectorAll('.shift-tabs-tab')).toHaveLength(3);
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
    body.querySelectorAll('.shift-tabs-tab')[index].dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));

  it('is a tablist with a roving tabindex; arrows follow the reading direction, Home and End go to the ends', async () => {
    const calls: number[] = [];
    const ltr = await render({ onSelectTab: index => calls.push(index) });

    expect(attr(ltr.body, '.shift-tabs-bar', 'role')).toBe('tablist');
    expect(Array.from(ltr.body.querySelectorAll('.shift-tabs-tab')).map(tab => tab.getAttribute('tabindex'))).toEqual(['0', '-1', '-1']);

    press(ltr.body, 1, 'ArrowRight');
    press(ltr.body, 1, 'End');
    press(ltr.body, 1, 'Home');
    // Home lands on the selected first tab: nothing is asked for.
    expect(calls).toEqual([2, 2]);

    const rtl = await render({ direction: 'rtl', onSelectTab: index => calls.push(index) });
    press(rtl.body, 1, 'ArrowRight');
    press(rtl.body, 1, 'ArrowLeft');
    // The first tab is the selected one here, so the move onto it asks for nothing.
    expect(calls.slice(2)).toEqual([2]);
  });
});

describe('the phone: a bottom sheet instead of the floating menu and the inline drawer', () => {
  const renderPhone = ({ phone = true, busy = false, sheet = null as PaintSheet | null, drawerOpen = null as number | null, certificateOpen = false } = {}) => {
    const state = loaded();
    const rows = shownRows(state);
    const open = bodyOpen(state, false, false);

    return newSpecPage({
      components: [],
      template: () => (
        <VehiclePaintThicknessPanel
          locale={locale}
          language="en"
          direction="ltr"
          state={state}
          verdict={{ state: 'idle', text: '' }}
          busy={busy}
          leaving={false}
          reconfiguring={false}
          reshaping={false}
          swapping={false}
          retainedBody={panelBody(state)}
          retainedPlaceholder={null}
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
          retainedGroups={allPhotoGroups(rows, locale)}
          onTogglePhotos={() => undefined}
          onSelectTab={() => undefined}
          certificateOpen={certificateOpen}
          onToggleCertificate={() => undefined}
          onCloseCertificate={() => undefined}
          phone={phone}
          sheet={sheet}
          onCloseSheet={() => undefined}
        />
      ),
    });
  };

  it("a photos sheet holds the row's photos and a title naming the row and its count; the table drawer stays shut", async () => {
    const { body } = await renderPhone({ sheet: { index: 0, open: true }, drawerOpen: 0 });

    expect(body.querySelector('.paint-photos-popover [slot="heading"]')?.textContent).toBe('Door · Front — 3 photos');
    expect(body.querySelectorAll('shift-popover .paint-sheet-content .paint-photo')).toHaveLength(3);
    expect(attr(body, '.lt-drawer', 'data-open')).toBe('false');
    expect(body.querySelectorAll('.lt-drawer .paint-photo')).toHaveLength(0);
  });

  it('the certificate popover lists the print languages as a sheet; the button names a dialog', async () => {
    const { body } = await renderPhone({ certificateOpen: true });

    expect(Array.from(body.querySelectorAll('shift-popover .paint-certificate-item')).map(item => item.textContent)).toEqual(['English']);
    expect(attr(body, '.paint-certificate-button', 'aria-haspopup')).toBe('dialog');
    expect(attr(body, '.paint-certificate-button', 'aria-expanded')).toBe('true');
    expect(body.querySelector('shift-popover[data-phone="true"] .paint-menu')).not.toBeNull();
  });

  it("in a lookup the sheet's title and content are under the skeleton and inert; off a phone there is no photos sheet and the drawer opens", async () => {
    const phone = await renderPhone({ busy: true, sheet: { index: 0, open: true } });
    expect(phone.body.querySelector('.paint-photos-popover [slot="heading"]')?.classList.contains('shift-skeleton')).toBe(true);
    expect(phone.body.querySelector('shift-popover .paint-sheet-content')?.hasAttribute('inert')).toBe(true);

    const desktop = await renderPhone({ phone: false, drawerOpen: 0 });
    expect(desktop.body.querySelector('shift-popover[data-phone="true"]')).toBeNull();
    expect(attr(desktop.body, '.lt-drawer', 'data-open')).toBe('true');
  });
});
