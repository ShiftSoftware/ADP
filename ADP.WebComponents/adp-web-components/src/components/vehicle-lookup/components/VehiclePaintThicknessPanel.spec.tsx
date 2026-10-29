import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import paintLocale from '../../../locales/vehicleLookup/paintThickness/en.json';

import type { PaintThicknessInspectionDTO } from '~types/generated/vehicle-lookup/paint-thickness-inspection-dto';

import { tableSlots } from '~features/lookup-table';

import { ShiftTabs } from '../../components/shift-tabs';

import { PanelState, VehiclePaintThicknessPanel, allPhotoGroups, bodyOpen, panelBody, shownRows } from './VehiclePaintThicknessPanel';

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

describe('a failed lookup and a vehicle not in the records', () => {
  it('show the idle look under the headings, with no notice of their own', async () => {
    for (const state of [loaded({ vehicleLoaded: false, error: 'Failed' }), loaded({ authorized: false })]) {
      const { body } = await render({ state, verdictText: 'Said by the pill' });

      expect(attr(body, '.lt-lead', 'data-lead')).toBe('columns');
      expect(attr(body, '.lt-placeholder', 'data-open')).toBe('true');
      expect(body.querySelector('.lt-placeholder .layer[data-active="true"]')?.textContent).toBe(paintLocale.idleHint);
    }
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
