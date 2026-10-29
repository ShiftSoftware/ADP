import { Component, Prop, h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { LookupColumn, LookupTable, RowSlot, RowVeil, placeholderKind } from './lookup-table';

// A table that does not opt in to a drawer staying as it was.

type Row = { name: string; photos: string[] | null };

const ROWS: Row[] = [
  { name: 'Front left', photos: ['a1', 'a2', 'a3'] },
  { name: 'Roof', photos: ['b1'] },
  { name: 'Front right', photos: null },
];

const strings = { columnsLabel: 'Readings', noImage: 'No image', imageFailed: 'Image failed', openImage: 'Open image' };

type Model = {
  rows: Row[];
  busy: boolean;
  veil: RowVeil;
  slots?: RowSlot[];
  columns: LookupColumn<Row>[];
};

const model = (patch: Partial<Model> = {}): Model => ({
  rows: ROWS,
  busy: false,
  veil: 'none',
  columns: [],
  ...patch,
});

@Component({ tag: 'lt-test-host', shadow: true })
class LtTestHost {
  @Prop() model: Model;

  render() {
    const m = this.model;

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
  return { page, root, set };
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
    const { root } = await mount(model({ columns: accessoriesColumns }));

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
