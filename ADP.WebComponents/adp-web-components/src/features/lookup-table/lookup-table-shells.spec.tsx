import { Component, Prop, VNode, h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { LookupColumn, LookupTable, RowDrawer, RowVeil } from './lookup-table';

/**
 * A shut drawer shell costs nothing under a lookup: no skeleton class (no cover, no pseudo-elements,
 * no opacity transition to start), no inert. The drawer that is open, or still shutting, keeps both.
 */

type Row = { name: string; photos: string[] };

const ROWS: Row[] = [
  { name: 'Front left', photos: ['a1'] },
  { name: 'Roof', photos: ['b1'] },
  { name: 'Hood', photos: ['c1'] },
];

const strings = { columnsLabel: 'Readings', noImage: 'No image', imageFailed: 'Image failed', openImage: 'Open image' };

const columns: LookupColumn<Row>[] = [
  { field: 'name', label: 'Panel', kind: 'code', identity: true, value: row => row.name },
  { field: 'photos', label: 'Photos', kind: 'action', value: row => `${row.photos.length} photos` },
];

type Model = { open: number | null; shown: number[]; veil: RowVeil };

@Component({ tag: 'lt-shell-host', shadow: true })
class LtShellHost {
  @Prop() model: Model;

  render() {
    const m = this.model;
    const drawer: RowDrawer<Row> = {
      has: row => row.photos.length > 0,
      render: row => (<div class="photos">{row.photos.join(',')}</div>) as VNode,
      label: row => `Show ${row.name} photos`,
      regionLabel: row => `${row.name} photos`,
      open: m.open,
      shown: m.shown,
      onToggle: () => undefined,
      idPrefix: 'pt',
    };

    return (
      <LookupTable<Row>
        columns={columns}
        rows={ROWS}
        rowKey={(_row, index) => `slot-${index}`}
        lead="columns"
        notice={null}
        busy={m.veil === 'cover'}
        body="rows"
        open
        strings={strings}
        slots={ROWS.map(() => ({ open: true, veil: m.veil }))}
        drawer={drawer}
      />
    );
  }
}

const mount = async (model: Model) => {
  const page = await newSpecPage({ components: [LtShellHost], template: () => <lt-shell-host model={model} /> });
  const root = page.root.shadowRoot;
  const set = async (next: Model) => {
    page.root.model = next;
    await page.waitForChanges();
  };
  const content = () => Array.from(root.querySelectorAll<HTMLElement>('.lt-drawer-content'));
  const drawers = () => Array.from(root.querySelectorAll<HTMLElement>('.lt-drawer'));
  return { root, set, content, drawers };
};

describe('shut drawer shells under a lookup', () => {
  it('only the open drawer carries the skeleton and is inert; shut shells carry neither', async () => {
    const { set, content, drawers } = await mount({ open: 1, shown: [1], veil: 'none' });
    await set({ open: 1, shown: [1], veil: 'cover' });

    expect(content().map(block => block.classList.contains('shift-skeleton'))).toEqual([false, true, false]);
    expect(drawers().map(drawer => drawer.hasAttribute('inert'))).toEqual([false, true, false]);
    // A shut shell still takes the row's veil, so a drawer that opens is already under it.
    expect(drawers()[0].classList.contains('loading')).toBe(true);
  });

  it('a drawer that is shutting keeps its skeleton until the owner drops it from shown', async () => {
    const { set, content } = await mount({ open: 1, shown: [1], veil: 'cover' });
    await set({ open: null, shown: [1], veil: 'cover' });
    expect(content()[1].classList.contains('shift-skeleton')).toBe(true);

    await set({ open: null, shown: [], veil: 'cover' });
    expect(content()[1].classList.contains('shift-skeleton')).toBe(false);
  });
});
