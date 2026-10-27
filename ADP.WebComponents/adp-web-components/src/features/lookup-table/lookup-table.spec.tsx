import { Component, Prop, VNode, h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { LookupColumn, LookupTable, RowDrawer, RowSlot, RowVeil, drawerId, retainDrawers, settleDrawer, shownDrawers, tableTracks, toggleDrawer } from './lookup-table';

/**
 * The drawer mode of the shared table, and the proof that a table which does not opt in is the table
 * it always was. The table is pure: every state below is a props change on a tiny host element, the
 * way a panel's owner drives it, and the DOM is read back.
 */

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
// The derivations
// ---------------------------------------------------------------------------------------------

describe('toggleDrawer: single open', () => {
  it('opens one when none is open', () => expect(toggleDrawer(null, 2)).toBe(2));
  it('opening another shuts the first (one value, so one open)', () => expect(toggleDrawer(2, 0)).toBe(0));
  it('the open one shuts itself', () => expect(toggleDrawer(2, 2)).toBeNull());
  it('index 0 is a drawer like any other', () => {
    expect(toggleDrawer(null, 0)).toBe(0);
    expect(toggleDrawer(0, 0)).toBeNull();
  });
});

describe('settleDrawer: what an answer, a page change or a clear does to the open drawer', () => {
  it('keeps the drawer open when its slot still has content in the new answer', () => expect(settleDrawer({ open: 1, hasAt: [true, true, false] })).toBe(1));
  it('shuts it when its slot has no drawer content in the new answer', () => expect(settleDrawer({ open: 2, hasAt: [true, true, false] })).toBeNull());
  it('shuts it when the slot itself goes (the new answer has fewer rows)', () => expect(settleDrawer({ open: 3, hasAt: [true, true] })).toBeNull());
  it('a clear shuts it at once, whatever the slot holds', () => expect(settleDrawer({ open: 0, hasAt: [true], cleared: true })).toBeNull());
  it('nothing open stays nothing open', () => expect(settleDrawer({ open: null, hasAt: [true, true] })).toBeNull());
  it('is not told about busy: an open drawer is only ever touched by an answer, a page or a clear', () => {
    // The signature has no `busy`: calling it is the owner's decision, made at the answer.
    expect(Object.keys({ open: 1, hasAt: [true, true] })).not.toContain('busy');
    expect(settleDrawer({ open: 1, hasAt: [true, true] })).toBe(1);
  });
});

describe('retainDrawers and shownDrawers: a drawer keeps its content while it shuts', () => {
  it('retains the one that just shut', () => expect(retainDrawers([], 1, null)).toEqual([1]));
  it('retains the one another opened over', () => expect(retainDrawers([], 1, 0)).toEqual([1]));
  it('retains nothing when nothing was open or the open one did not change', () => {
    expect(retainDrawers([], null, 1)).toEqual([]);
    expect(retainDrawers([], 1, 1)).toEqual([]);
  });
  it('a drawer that reopens is no longer retained, and one is never listed twice', () => {
    expect(retainDrawers([1], null, 1)).toEqual([]);
    expect(retainDrawers([1], 0, null)).toEqual([1, 0]);
    expect(retainDrawers([1], 1, null)).toEqual([1]);
  });
  it('shown is the open one plus the shutting ones', () => {
    expect(shownDrawers(null, [])).toEqual([]);
    expect(shownDrawers(0, [1])).toEqual([1, 0]);
    expect(shownDrawers(null, [1])).toEqual([1]);
    expect(shownDrawers(1, [1])).toEqual([1]);
  });
  it('drawerId is per prefix and per slot', () => {
    expect(drawerId('pt', 2)).not.toBe(drawerId('pt', 3));
    expect(drawerId('pt', 2)).not.toBe(drawerId('other', 2));
  });
});

describe('tableTracks with an action column', () => {
  it('is a fixed share like the others, so the tracks never move with the data', () => {
    expect(tableTracks(columns).wide).toBe('minmax(180px, 1.1fr) minmax(150px, 0.9fr)');
  });

  it("keeps the stacked layout's other tracks the same recipe", () => {
    const stackable: LookupColumn<Row>[] = [
      { field: 'name', label: 'Panel', kind: 'code', identity: true, value: row => row.name },
      { field: 'desc', label: 'Description', kind: 'text', grow: true, value: () => 'x' },
      columns[1],
    ];
    expect(tableTracks(stackable).stacked).toBe('minmax(160px, 1fr) minmax(150px, 0.9fr)');
  });

  it('leaves the tracks of a table with no action column exactly as they were', () => {
    const accessories: LookupColumn<Row>[] = [
      { field: 'name', label: 'Part', kind: 'code', identity: true, value: row => row.name },
      { field: 'desc', label: 'Description', kind: 'text', grow: true, value: () => 'x' },
      { field: 'image', label: 'Image', kind: 'media', value: () => null },
      { field: 'company', label: 'Company', kind: 'text', value: () => 'x' },
    ];
    expect(tableTracks(accessories).wide).toBe('minmax(180px, 1.1fr) minmax(200px, 2.4fr) calc(var(--lt-media-width) + 24px) minmax(150px, 1.2fr)');
  });
});

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

  it('an action column without a drawer prop is plain text or the blank marker, never a button', async () => {
    const { root, buttons } = await mount(model({ withDrawer: false }));
    expect(buttons()).toHaveLength(0);
    const cells = Array.from(root.querySelectorAll('.lt-action'));
    expect(cells.map(cell => cell.textContent.trim())).toEqual(['3 photos', '1 photos', '—']);
    expect(cells[0].querySelector('.lt-action-plain')).not.toBeNull();
    expect(cells[2].querySelector('.lt-none')).not.toBeNull();
  });
});

// ---------------------------------------------------------------------------------------------
// Drawer mode
// ---------------------------------------------------------------------------------------------

describe('closed by default, and the toggle', () => {
  it('every drawer is shut, empty, aria-hidden, and the content is never asked for', async () => {
    const initial = model();
    const { drawers, root } = await mount(initial);

    expect(drawers()).toHaveLength(3);
    for (const drawer of drawers()) {
      expect(drawer.getAttribute('data-open')).toBe('false');
      expect(drawer.getAttribute('aria-hidden')).toBe('true');
      expect(drawer.classList.contains('collapsible')).toBe(true);
      expect(drawer.querySelector('.lt-drawer-content').children).toHaveLength(0);
    }
    expect(initial.render).not.toHaveBeenCalled();
    expect(root.querySelector('.photos')).toBeNull();
    expect(root.querySelector('.lt-row[data-open]')).toBeNull();
  });

  it('a row with drawer content has a real button: type=button, aria-expanded=false, a label from the consumer', async () => {
    const { buttons } = await mount(model());
    const [first, second, third] = buttons();

    expect(buttons()).toHaveLength(2);
    expect(first.tagName).toBe('BUTTON');
    expect(first.getAttribute('type')).toBe('button');
    expect(first.getAttribute('aria-expanded')).toBe('false');
    expect(first.getAttribute('aria-label')).toBe('Show Front left photos');
    expect(first.textContent.trim()).toBe('3 photos');
    expect(first.getAttribute('data-shape')).toBe('pill');
    expect(second.getAttribute('aria-label')).toBe('Show Roof photos');
    expect(third).toBeUndefined();
  });

  it('a click reports the slot index and nothing else; the owner holds the state', async () => {
    const initial = model();
    const { buttons, drawers } = await mount(initial);
    buttons()[1].click();
    expect(initial.toggled).toHaveBeenCalledTimes(1);
    expect(initial.toggled).toHaveBeenCalledWith(1);
    expect(drawers()[1].getAttribute('data-open')).toBe('false');
  });

  it('a row without drawer content has no live toggle: muted blank marker, drawer shell shut', async () => {
    const { root, buttons, drawers } = await mount(model({ open: 2, shown: [2] }));
    const cell = root.querySelectorAll('.lt-action')[2];

    expect(buttons()).toHaveLength(2);
    expect(cell.querySelector('button')).toBeNull();
    expect(cell.querySelector('.lt-none').textContent).toBe('—');
    expect(cell.getAttribute('data-shape')).toBe('plain');
    // Even a stale `open` on a slot with nothing to show cannot open an empty drawer.
    expect(drawers()[2].getAttribute('data-open')).toBe('false');
    expect(drawers()[2].getAttribute('aria-hidden')).toBe('true');
  });

  it('a row with a drawer but no text gets the 30px round icon-only button, named by the consumer', async () => {
    const iconColumns: LookupColumn<Row>[] = [columns[0], { ...columns[1], value: () => null, glyph: 'M0 0h512v512H0z' }];
    const { buttons } = await mount(model({ columns: iconColumns }));
    const [first] = buttons();

    expect(first.getAttribute('data-shape')).toBe('round');
    expect(first.getAttribute('aria-label')).toBe('Show Front left photos');
    expect(first.querySelector('.lt-action-text')).toBeNull();
    expect(first.querySelector('.lt-action-glyph')).not.toBeNull();
    expect(first.querySelector('.lt-action-chevron')).toBeNull();
  });
});

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

  it('the drawer is the next element after its row, so it follows it in focus and reading order', async () => {
    const { root } = await mount(model({ open: 0, shown: [0] }));
    const body = root.querySelector('.lt-slot-body');
    expect(body.children[0].classList.contains('lt-row')).toBe(true);
    expect(body.children[1].classList.contains('lt-drawer')).toBe(true);
  });

  it('single open: opening another shuts the first, which keeps its content while it slides away', async () => {
    const { set, drawers, buttons } = await mount(model({ open: 0, shown: [0] }));

    // The owner opened row 1: open = 1, and row 0 is retained while it shuts.
    await set(model({ open: 1, shown: shownDrawers(1, retainDrawers([], 0, 1)) }));

    expect(drawers()[0].getAttribute('data-open')).toBe('false');
    expect(drawers()[1].getAttribute('data-open')).toBe('true');
    expect(drawers()[0].querySelector('.photos').textContent).toBe('a1,a2,a3');
    expect(drawers()[1].querySelector('.photos').textContent).toBe('b1');
    expect(buttons().map(button => button.getAttribute('aria-expanded'))).toEqual(['false', 'true']);

    // A settle later the owner drops it: its content goes, shut.
    await set(model({ open: 1, shown: [1] }));
    expect(drawers()[0].querySelector('.lt-drawer-content').children).toHaveLength(0);
    expect(drawers()[0].getAttribute('aria-hidden')).toBe('true');
  });

  it('shuts: data-open false and aria-expanded false at once, the content kept until the owner drops it', async () => {
    const { set, drawers, buttons } = await mount(model({ open: 1, shown: [1] }));

    await set(model({ open: null, shown: shownDrawers(null, retainDrawers([], 1, null)) }));
    expect(drawers()[1].getAttribute('data-open')).toBe('false');
    expect(buttons()[1].getAttribute('aria-expanded')).toBe('false');
    expect(drawers()[1].querySelector('.photos')).not.toBeNull();

    await set(model({ open: null, shown: [] }));
    expect(drawers()[1].querySelector('.photos')).toBeNull();
  });
});

describe('table semantics with a drawer', () => {
  it('while the table is a table, a drawer is a row holding one cell, so no stray element sits in the rowgroup', async () => {
    const { root, drawers } = await mount(model({ open: 0, shown: [0] }));
    expect(root.querySelector('.lt-grid').getAttribute('role')).toBe('table');

    const drawer = drawers()[0];
    expect(drawer.getAttribute('role')).toBe('row');
    const cells = drawer.querySelectorAll('[role="cell"]');
    expect(cells).toHaveLength(1);
    expect(cells[0].querySelector('[role="region"]')).not.toBeNull();
    // Every direct child of a row is a cell.
    expect(drawer.querySelector('.lt-drawer-body').children).toHaveLength(1);
  });

  it('while a slot is shut (a row leaving) its drawer carries no table role either', async () => {
    const { drawers } = await mount(
      model({
        slots: [
          { open: false, veil: 'cover' },
          { open: true, veil: 'none' },
          { open: true, veil: 'none' },
        ],
      }),
    );
    expect(drawers()[0].getAttribute('role')).toBeNull();
    expect(drawers()[0].querySelector('.lt-drawer-cell').getAttribute('role')).toBeNull();
    expect(drawers()[1].getAttribute('role')).toBe('row');
  });
});

// ---------------------------------------------------------------------------------------------
// A drawer through a lookup, a page change and a clear (owner, 2026-10-27)
// ---------------------------------------------------------------------------------------------

describe('an open drawer through a lookup', () => {
  it('stays open into loading and its content goes under the skeleton with its row', async () => {
    const { set, drawers, root } = await mount(model({ open: 1, shown: [1] }));

    // A lookup starts: busy, every row's veil is cover. `open` is untouched.
    await set(model({ open: 1, shown: [1], busy: true, veil: 'cover' }));

    const drawer = drawers()[1];
    expect(drawer.getAttribute('data-open')).toBe('true');
    expect(drawer.classList.contains('loading')).toBe(true);
    expect(drawer.querySelector('.photos')).not.toBeNull();
    // The cover is the shift-skeleton's: the content block carries the class, inside .loading.
    expect(drawer.querySelector('.lt-drawer-content').classList.contains('shift-skeleton')).toBe(true);
    expect(root.querySelectorAll('.lt-row')[1].classList.contains('loading')).toBe(true);
    expect(root.querySelectorAll('.lt-row')[1].getAttribute('data-open')).toBe('true');
    // The other drawers are covered too, shut and empty.
    expect(drawers()[0].classList.contains('loading')).toBe(true);
    expect(drawers()[0].getAttribute('data-open')).toBe('false');
    // Covered: the toggle and the drawer are not operable from the keyboard either.
    expect(root.querySelectorAll('.lt-action-button')[1].hasAttribute('inert')).toBe(true);
    expect(drawer.hasAttribute('inert')).toBe(true);
    // Not covered: no inert anywhere.
    await set(model({ open: 1, shown: [1] }));
    expect(root.querySelector('[inert]')).toBeNull();
    // The toggle is under the skeleton like any value.
    expect(root.querySelectorAll('.lt-action')[1].querySelector('.lt-value').classList.contains('shift-skeleton')).toBe(true);
  });

  it('on the answer its content shows the new record for its slot and the cover lifts', async () => {
    const { set, drawers } = await mount(model({ open: 1, shown: [1], busy: true, veil: 'cover' }));

    const answer: Row[] = [
      { name: 'Hood', photos: ['h1'] },
      { name: 'Trunk', photos: ['t1', 't2'] },
    ];
    await set(model({ rows: answer, open: settleDrawer({ open: 1, hasAt: answer.map(row => !!row.photos?.length) }), shown: [1], busy: false, veil: 'none' }));

    const drawer = drawers()[1];
    expect(drawer.getAttribute('data-open')).toBe('true');
    expect(drawer.classList.contains('loading')).toBe(false);
    expect(drawer.querySelector('.photos').textContent).toBe('t1,t2');
  });

  it('shuts at the answer when its slot has no drawer content in the new answer', async () => {
    const { set, drawers, buttons } = await mount(model({ open: 1, shown: [1], busy: true, veil: 'cover' }));

    const answer: Row[] = [
      { name: 'Hood', photos: ['h1'] },
      { name: 'Trunk', photos: null },
    ];
    const open = settleDrawer({ open: 1, hasAt: answer.map(row => !!row.photos?.length) });
    expect(open).toBeNull();

    // The owner retains it while it shuts and hands back the content it last showed.
    const last = <div class="photos">b1</div>;
    await set(model({ rows: answer, open, shown: [1], render: jest.fn(() => last), busy: false, veil: 'none' }));

    expect(drawers()[1].getAttribute('data-open')).toBe('false');
    expect(drawers()[1].querySelector('.photos').textContent).toBe('b1');
    expect(buttons()).toHaveLength(1);

    await set(model({ rows: answer, open, shown: [] }));
    expect(drawers()[1].querySelector('.photos')).toBeNull();
  });

  it('even before the owner has settled, a drawer whose slot lost its content is shut on that very render', async () => {
    const answer: Row[] = [
      { name: 'Hood', photos: ['h1'] },
      { name: 'Trunk', photos: null },
    ];
    const { drawers } = await mount(model({ rows: answer, open: 1, shown: [1] }));
    expect(drawers()[1].getAttribute('data-open')).toBe('false');
  });

  it('shuts when its row goes: the answer has fewer rows, so the slot leaves and the drawer goes with it', async () => {
    const answer: Row[] = [{ name: 'Hood', photos: ['h1'] }];
    expect(settleDrawer({ open: 1, hasAt: answer.map(row => !!row.photos?.length) })).toBeNull();

    // The leaving row stays rendered in its slot while the slot shuts (`slots[1].open` false); its drawer is shut.
    const { set, drawers, root } = await mount(model({ open: 1, shown: [1], busy: true, veil: 'cover' }));
    await set(
      model({
        rows: [answer[0], ROWS[1]],
        open: null,
        shown: [1],
        slots: [
          { open: true, veil: 'none' },
          { open: false, veil: 'cover' },
        ],
      }),
    );
    expect(drawers()[1].getAttribute('data-open')).toBe('false');
    expect(root.querySelectorAll('.lt-slot')[1].getAttribute('data-open')).toBe('false');
    expect(drawers()[1].querySelector('.photos')).not.toBeNull();
  });

  it('a clear shuts it at once', async () => {
    const { set, drawers } = await mount(model({ open: 1, shown: [1], busy: true, veil: 'cover' }));
    const open = settleDrawer({ open: 1, hasAt: [true, true, false], cleared: true });
    expect(open).toBeNull();
    await set(model({ open, shown: shownDrawers(open, retainDrawers([], 1, open)), busy: true, veil: 'cover' }));
    expect(drawers().every(drawer => drawer.getAttribute('data-open') === 'false')).toBe(true);
  });
});

describe('a change of answer without a fetch (a page, another record)', () => {
  it('the fade veil keeps the drawer open and fades its content with the row, with no skeleton', async () => {
    const { set, drawers, root } = await mount(model({ open: 1, shown: [1] }));
    await set(model({ open: 1, shown: [1], veil: 'fade' }));

    const drawer = drawers()[1];
    expect(drawer.getAttribute('data-open')).toBe('true');
    expect(drawer.classList.contains('loading')).toBe(false);
    expect(root.querySelectorAll('.lt-slot')[1].getAttribute('data-veil')).toBe('fade');
    // The slot's veil is what the stylesheet reads to fade `.lt-drawer-content`; the content is still there while it is out.
    expect(drawer.querySelector('.photos')).not.toBeNull();
  });

  it('swaps while out, fades back, and shuts if the new slot has no content', async () => {
    const { set, drawers } = await mount(model({ open: 1, shown: [1], veil: 'fade' }));

    const next: Row[] = [
      { name: 'Hood', photos: ['h1'] },
      { name: 'Trunk', photos: ['t1', 't2', 't3'] },
    ];
    await set(model({ rows: next, open: settleDrawer({ open: 1, hasAt: [true, true] }), shown: [1], veil: 'fade' }));
    expect(drawers()[1].querySelector('.photos').textContent).toBe('t1,t2,t3');
    expect(drawers()[1].getAttribute('data-open')).toBe('true');

    await set(model({ rows: next, open: 1, shown: [1], veil: 'none' }));
    expect(drawers()[1].getAttribute('data-open')).toBe('true');
  });
});

describe('a closed drawer costs nothing', () => {
  it("the consumer's render function is never called while no drawer is open or shutting", async () => {
    const initial = model();
    const { set } = await mount(initial);
    await set(model({ render: initial.render, busy: true, veil: 'cover' }));
    await set(model({ render: initial.render, veil: 'fade' }));
    expect(initial.render).not.toHaveBeenCalled();
  });

  it('with 40 rows, one open, the render function is called for that one only', async () => {
    const many: Row[] = Array.from({ length: 40 }, (_, index) => ({ name: `Row ${index}`, photos: ['x', 'y'] }));
    const initial = model({ rows: many, open: 7, shown: [7] });
    const { root } = await mount(initial);
    expect(initial.render).toHaveBeenCalledTimes(1);
    expect(root.querySelectorAll('.photos')).toHaveLength(1);
  });
});
