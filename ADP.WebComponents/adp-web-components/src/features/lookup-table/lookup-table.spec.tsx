import { Component, Prop, VNode, h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { LookupColumn, LookupTable, RowDrawer, RowSlot, RowVeil, drawerId, placeholderKind } from './lookup-table';

// The table's drawer mode, and a table that does not opt in staying as it was.

type Row = { name: string; photos: string[] | null };

const ROWS: Row[] = [
  { name: 'Front left', photos: ['a1', 'a2', 'a3'] },
  { name: 'Roof', photos: ['b1'] },
  { name: 'Front right', photos: null },
];

const strings = { columnsLabel: 'Readings', noImage: 'No image', imageFailed: 'Image failed', openImage: 'Open image' };

const columns: LookupColumn<Row>[] = [
  { field: 'name', label: 'Panel', kind: 'code', identity: true, value: row => row.name },
  { field: 'photos', label: 'Photos', kind: 'action', value: row => (row.photos ? `${row.photos.length} photos` : null) },
];

type Model = {
  rows: Row[];
  open: number | null;
  shown: number[];
  busy: boolean;
  veil: RowVeil;
  slots?: RowSlot[];
  withDrawer: boolean;
  /** The columns: the action column is dropped for the "accessories" shape. */
  columns: LookupColumn<Row>[];
  render: jest.Mock;
  toggled: jest.Mock;
};

const model = (patch: Partial<Model> = {}): Model => ({
  rows: ROWS,
  open: null,
  shown: [],
  busy: false,
  veil: 'none',
  withDrawer: true,
  columns,
  render: jest.fn((row: Row) => <div class="photos">{(row.photos ?? []).join(',')}</div>),
  toggled: jest.fn(),
  ...patch,
});

@Component({ tag: 'lt-test-host', shadow: true })
class LtTestHost {
  @Prop() model: Model;

  render() {
    const m = this.model;
    const drawer: RowDrawer<Row> | undefined = m.withDrawer
      ? {
          has: row => !!row.photos?.length,
          render: (row, index) => m.render(row, index) as VNode,
          label: (row, _index, expanded) => `${expanded ? 'Hide' : 'Show'} ${row.name} photos`,
          regionLabel: row => `${row.name} photos`,
          open: m.open,
          shown: m.shown,
          onToggle: m.toggled,
          idPrefix: 'pt',
        }
      : undefined;

    return (
      <LookupTable<Row>
        columns={m.columns}
        rows={m.rows}
        rowKey={(_row, index) => `slot-${index}`}
        lead="columns"
        notice={null}
        busy={m.busy}
        body="rows"
        open
        strings={strings}
        slots={m.slots ?? m.rows.map(() => ({ open: true, veil: m.veil }))}
        drawer={drawer}
      />
    );
  }
}

const mount = async (initial: Model) => {
  const page = await newSpecPage({ components: [LtTestHost], template: () => <lt-test-host model={initial} /> });
  const root = page.root.shadowRoot;
  const set = async (next: Model) => {
    page.root.model = next;
    await page.waitForChanges();
  };
  const drawers = () => Array.from(root.querySelectorAll<HTMLElement>('.lt-drawer'));
  const buttons = () => Array.from(root.querySelectorAll<HTMLButtonElement>('.lt-action-button'));
  return { page, root, set, drawers, buttons };
};

// ---------------------------------------------------------------------------------------------
// A table that does not opt in is the table it always was (the accessories shape)
// ---------------------------------------------------------------------------------------------

describe('a table with no drawer', () => {
  const accessoriesColumns: LookupColumn<Row>[] = [
    { field: 'name', label: 'Part', kind: 'code', identity: true, value: row => row.name },
    { field: 'desc', label: 'Description', kind: 'text', grow: true, value: row => (row.photos ? 'with photos' : null) },
  ];

  it('renders no drawer, no toggle, no action cell and no new attribute on a row', async () => {
    const { root } = await mount(model({ withDrawer: false, columns: accessoriesColumns }));

    expect(root.querySelector('.lt-drawer')).toBeNull();
    expect(root.querySelector('.lt-drawer-panel')).toBeNull();
    expect(root.querySelector('.lt-action')).toBeNull();
    expect(root.querySelector('.lt-action-button')).toBeNull();
    expect(root.querySelector('[data-shape]')).toBeNull();
    expect(root.querySelector('[aria-expanded]')).toBeNull();
    expect(root.querySelector('[aria-controls]')).toBeNull();

    const row = root.querySelector('.lt-row');
    // The row's own tag, attributes and nothing more: no data-open, no new class.
    expect(row.outerHTML.slice(0, row.outerHTML.indexOf('>') + 1)).toBe('<div class="lt-row" role="row">');
    // The slot's body holds the row and nothing else.
    expect(root.querySelector('.lt-slot-body').children).toHaveLength(1);
  });
});

// ---------------------------------------------------------------------------------------------
// Drawer mode
// ---------------------------------------------------------------------------------------------

describe('open and shut', () => {
  it('opens: aria-expanded, aria-controls matches the region id, the label says hide, the row tints, the content renders once', async () => {
    const initial = model();
    const { set, buttons, drawers, root } = await mount(initial);

    const next = model({ open: 1, shown: [1], render: initial.render });
    await set(next);

    const [first, second] = buttons();
    expect(first.getAttribute('aria-expanded')).toBe('false');
    expect(second.getAttribute('aria-expanded')).toBe('true');
    expect(second.getAttribute('aria-label')).toBe('Hide Roof photos');

    const drawer = drawers()[1];
    expect(drawer.getAttribute('data-open')).toBe('true');
    expect(drawer.getAttribute('aria-hidden')).toBeNull();

    const region = drawer.querySelector('.lt-drawer-panel');
    expect(region.getAttribute('role')).toBe('region');
    // A row without drawer content has no region role (a nameless region is invalid).
    expect(Array.from(root.querySelectorAll('.lt-drawer-panel')).every(p => (p.getAttribute('role') === 'region') === (p.getAttribute('aria-label') !== null))).toBe(true);
    expect(region.getAttribute('aria-label')).toBe('Roof photos');
    expect(region.getAttribute('id')).toBe(drawerId('pt', 1));
    expect(second.getAttribute('aria-controls')).toBe(region.getAttribute('id'));
    expect(root.querySelector(`#${second.getAttribute('aria-controls')}`)).toBe(region);

    expect(region.textContent).toBe('b1');
    expect(root.querySelectorAll('.lt-row')[1].getAttribute('data-open')).toBe('true');
    expect(root.querySelectorAll('.lt-row')[0].getAttribute('data-open')).toBeNull();
    // Only the open drawer's content was asked for.
    expect(initial.render).toHaveBeenCalledTimes(1);
    expect(initial.render.mock.calls[0][1]).toBe(1);
  });
});

describe('placeholderKind', () => {
  const base = { columns: 4, loaded: true, failed: false, authorized: true, records: 0 };

  it('picks the outcome without rows, and is null above rows or with no column to head', () => {
    expect(placeholderKind({ ...base, loaded: false })).toBe('idle');
    expect(placeholderKind(base)).toBe('empty');
    expect(placeholderKind({ ...base, authorized: false, records: 3 })).toBe('unauthorized');
    expect(placeholderKind({ ...base, loaded: false, failed: true })).toBe('error');
    expect(placeholderKind({ ...base, records: 3 })).toBeNull();
    expect(placeholderKind({ ...base, records: 3, leaving: true })).toBe('idle');
    expect(placeholderKind({ ...base, columns: 0 })).toBeNull();
  });
});
