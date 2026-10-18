import { Component, Prop, h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { LookupColumn, LookupTable, LookupTableProps, cellText, parseHiddenFields, shownBody, tableLead, tableRoles, tableTracks, visibleColumns } from './lookup-table';

/**
 * The lookup table: its pure derivations, and the structure the stylesheet relies on, through the
 * DOM. jsdom (mock-doc here) cannot evaluate container queries, so the layouts are pinned by the
 * attributes, classes and style properties the CSS keys on -- data-stack, data-label, the tracks.
 * Fixtures are fictional.
 */

type Item = { pn: string | null; desc: string | null; img: string | null; job: string | null; company: string | null };

const column = (over: Partial<LookupColumn<Item>> & Pick<LookupColumn<Item>, 'field' | 'kind'>): LookupColumn<Item> => ({
  label: over.field,
  value: () => null,
  ...over,
});

const PART = column({ field: 'partNumber', label: 'Part no.', kind: 'code', identity: true, value: r => r.pn });
const DESCRIPTION = column({ field: 'description', label: 'Description', kind: 'text', grow: true, value: r => r.desc });
const IMAGE = column({ field: 'image', label: 'Image', kind: 'media', value: r => r.img });
const JOB = column({ field: 'jobNumber', label: 'Job', kind: 'ref', value: r => r.job });
const COMPANY = column({ field: 'companyName', label: 'Company', kind: 'text', value: r => r.company });

const ALL = [PART, DESCRIPTION, IMAGE, JOB, COMPANY];

const strings = { columnsLabel: 'Item list', noImage: 'No picture on record', imageFailed: 'Picture did not load', openImage: 'Open picture of' };

const item = (over: Partial<Item> = {}): Item => ({ pn: 'TST-0001', desc: 'Sample widget', img: 'https://example.invalid/a.png', job: 'J-100', company: 'Sample Co', ...over });

const base = (over: Partial<LookupTableProps<Item>> = {}): LookupTableProps<Item> => ({
  columns: ALL,
  rows: [item(), item({ pn: 'TST-0002', desc: 'Another widget', img: null, job: null })],
  rowKey: (row, index) => `${row.pn ?? ''}#${index}`,
  lead: 'columns',
  notice: null,
  busy: false,
  body: 'rows',
  open: true,
  strings,
  ...over,
});

const render = (over: Partial<LookupTableProps<Item>> = {}) =>
  newSpecPage({
    components: [],
    template: () => <LookupTable {...base(over)}>{over.body && over.body !== 'rows' ? <div class="child-stand-in">child</div> : null}</LookupTable>,
  });

/** A host that re-renders the table, to prove what a parent's re-render does to it. Test-only: it lives in this file and nowhere in src. */
@Component({ tag: 'lt-test-host' })
class LtTestHost {
  @Prop() model: LookupTableProps<Item>;

  render() {
    // Nothing until the test hands it a model: the table is not defined without props.
    if (!this.model) return null;

    return (
      <div>
        <LookupTable {...this.model} />
      </div>
    );
  }
}

const renderHost = async (model: LookupTableProps<Item>) => {
  const page = await newSpecPage({ components: [LtTestHost], html: '<lt-test-host></lt-test-host>' });
  page.root.model = model;
  await page.waitForChanges();
  return page;
};

type Page = { body: HTMLElement };

const q = (page: Page, selector: string) => page.body.querySelector(selector) as HTMLElement | null;
const qa = (page: Page, selector: string) => Array.from(page.body.querySelectorAll(selector)) as HTMLElement[];
const roleAttrs = (page: Page) => qa(page, '[role]').filter(el => ['table', 'row', 'rowgroup', 'columnheader', 'cell'].includes(el.getAttribute('role')));

describe('derivations', () => {
  describe('parseHiddenFields', () => {
    it('splits on commas and trims spaces', () => {
      expect([...parseHiddenFields(' companyName , jobNumber ,invoiceNumber')]).toEqual(['companyName', 'jobNumber', 'invoiceNumber']);
    });

    it('drops empty names', () => {
      expect([...parseHiddenFields('a,,b, ,')]).toEqual(['a', 'b']);
    });

    it('is empty for undefined, an empty string and a string of separators', () => {
      expect(parseHiddenFields(undefined).size).toBe(0);
      expect(parseHiddenFields('').size).toBe(0);
      expect(parseHiddenFields(' , ').size).toBe(0);
    });

    it('is case-sensitive: a field name is an identifier', () => {
      expect(parseHiddenFields('CompanyName').has('companyName')).toBe(false);
    });
  });

  describe('visibleColumns', () => {
    it('removes a hidden field', () => {
      expect(visibleColumns(ALL, new Set(['jobNumber'])).map(c => c.field)).toEqual(['partNumber', 'description', 'image', 'companyName']);
    });

    it('keeps the definition order whatever order the names were hidden in', () => {
      expect(visibleColumns(ALL, new Set(['companyName', 'description'])).map(c => c.field)).toEqual(['partNumber', 'image', 'jobNumber']);
    });

    it('ignores names that are not columns', () => {
      expect(visibleColumns(ALL, new Set(['nonsense'])).length).toBe(ALL.length);
    });

    it('is empty when every field is hidden, so the panel can tell it is not a table', () => {
      expect(visibleColumns(ALL, new Set(ALL.map(c => c.field)))).toEqual([]);
    });
  });

  describe('tableTracks', () => {
    it('gives one track per visible column, by kind, with the growing text column floored', () => {
      expect(tableTracks([PART, DESCRIPTION, IMAGE, JOB]).wide).toBe('minmax(auto, max-content) minmax(200px, 1fr) minmax(auto, max-content) minmax(auto, max-content)');
    });

    it('with no growing column, the last column takes the slack so the strip reaches the card edge', () => {
      expect(tableTracks([PART, IMAGE, JOB]).wide).toBe('minmax(auto, max-content) minmax(auto, max-content) minmax(auto, 1fr)');
      expect(tableTracks([PART]).wide).toBe('minmax(auto, 1fr)');
    });

    it('with a growing column, the last column does not also take the slack', () => {
      expect(tableTracks([PART, DESCRIPTION]).wide).toBe('minmax(auto, max-content) minmax(200px, 1fr)');
    });

    it('lets only a text column grow: grow on another kind is ignored', () => {
      const growingCode = column({ field: 'x', kind: 'code', grow: true });
      expect(tableTracks([PART, growingCode]).wide).toBe('minmax(auto, max-content) minmax(auto, 1fr)');
      expect(tableTracks([PART, growingCode]).stacked).toBeNull();
    });

    it('stacks when the identity is first and a growing column is visible: the identity and growing tracks collapse into one', () => {
      const { stacked } = tableTracks(ALL);
      expect(stacked).toBe('minmax(160px, 1fr) minmax(auto, max-content) minmax(auto, max-content) minmax(auto, max-content)');
      expect(stacked.startsWith('minmax(160px, 1fr)')).toBe(true);
    });

    it('does not stack when the identity is not the first visible column', () => {
      expect(tableTracks([IMAGE, PART, DESCRIPTION]).stacked).toBeNull();
    });

    it('does not stack when the growing column is hidden', () => {
      expect(tableTracks([PART, IMAGE, JOB]).stacked).toBeNull();
    });

    it('does not stack when no column is the identity', () => {
      expect(tableTracks([DESCRIPTION, IMAGE]).stacked).toBeNull();
    });

    it('does not stack a column that is both identity and growing: there is no description to put under it', () => {
      const both = column({ field: 'b', kind: 'text', identity: true, grow: true });
      expect(tableTracks([both, IMAGE]).stacked).toBeNull();
    });

    it('keeps the strip one track wide when no column is visible', () => {
      expect(tableTracks([])).toEqual({ wide: 'minmax(0, 1fr)', stacked: null });
    });
  });

  describe('cellText', () => {
    const textOf = (value: string | number | null | undefined) => cellText(column({ field: 'f', kind: 'text', value: () => value }), item());

    it('is null for null, undefined, an empty string and whitespace', () => {
      expect(textOf(null)).toBeNull();
      expect(textOf(undefined)).toBeNull();
      expect(textOf('')).toBeNull();
      expect(textOf('  ')).toBeNull();
    });

    it('treats 0 and "0" as values: a zero meaning "no value" is mapped upstream, never guessed here', () => {
      expect(textOf(0)).toBe('0');
      expect(textOf('0')).toBe('0');
    });

    it('returns the value as a string otherwise', () => {
      expect(textOf('J-100')).toBe('J-100');
      expect(textOf(42)).toBe('42');
    });
  });

  describe('tableRoles', () => {
    it('is true only for rows, open, not reshaping', () => {
      expect(tableRoles('rows', true, false)).toBe(true);
    });

    it('is false when shut, when the body is another kind, and while reshaping', () => {
      expect(tableRoles('rows', false, false)).toBe(false);
      expect(tableRoles('run', true, false)).toBe(false);
      expect(tableRoles('none', true, false)).toBe(false);
      expect(tableRoles('rows', true, true)).toBe(false);
      expect(tableRoles('rows', false, true)).toBe(false);
    });
  });

  describe('tableLead', () => {
    it('is the skeleton while busy, whatever the body is', () => {
      expect(tableLead({ busy: true, idle: false, body: 'rows' })).toBe('skeleton');
      expect(tableLead({ busy: true, idle: false, body: 'none' })).toBe('skeleton');
    });

    it('is the skeleton while idle, whatever the body is', () => {
      expect(tableLead({ busy: false, idle: true, body: 'rows' })).toBe('skeleton');
      expect(tableLead({ busy: false, idle: true, body: 'none' })).toBe('skeleton');
    });

    it('is the columns only above rows', () => {
      expect(tableLead({ busy: false, idle: false, body: 'rows' })).toBe('columns');
    });

    it('is a notice for any other body', () => {
      expect(tableLead({ busy: false, idle: false, body: 'none' })).toBe('notice');
      expect(tableLead({ busy: false, idle: false, body: 'run' })).toBe('notice');
    });
  });

  describe('shownBody', () => {
    it('keeps rendering the retained body while the new state has none', () => {
      expect(shownBody('none', 'rows')).toBe('rows');
      expect(shownBody('none', 'none')).toBe('none');
    });

    it('renders the current body when there is one', () => {
      expect(shownBody('rows', 'run')).toBe('rows');
      expect(shownBody('run', 'rows')).toBe('run');
    });
  });
});

describe('roles', () => {
  it('are all present when the body is rows and open', async () => {
    const page = await render();
    expect(q(page, '.lt-grid').getAttribute('role')).toBe('table');
    expect(q(page, '.lt-columns').getAttribute('role')).toBe('row');
    expect(q(page, '.lt-body').getAttribute('role')).toBe('rowgroup');
    expect(qa(page, '.lt-row').map(el => el.getAttribute('role'))).toEqual(['row', 'row']);
    expect(qa(page, '.lt-row > .lt-cell').every(el => el.getAttribute('role') === 'cell')).toBe(true);
    expect(qa(page, '.lt-columns > .lt-cell').every(el => el.getAttribute('role') === 'columnheader')).toBe(true);
  });

  it('count one columnheader per visible column', async () => {
    const page = await render({ columns: [PART, JOB] });
    expect(qa(page, '[role="columnheader"]').length).toBe(2);
    expect(qa(page, '[role="columnheader"]').map(el => el.textContent)).toEqual(['Part no.', 'Job']);
  });

  it('are absent on every element when the body is shut', async () => {
    const page = await render({ open: false });
    expect(roleAttrs(page)).toEqual([]);
    expect(q(page, '.lt-body').getAttribute('aria-hidden')).toBe('true');
  });

  it('are absent on every element when the body is another kind', async () => {
    const page = await render({ body: 'run', lead: 'notice', notice: { tone: 'attention', text: 'Run the check' } });
    expect(roleAttrs(page)).toEqual([]);
  });

  it('are absent on every element while reshaping, though the headings stay on screen', async () => {
    const page = await render({ reshaping: true });
    expect(roleAttrs(page)).toEqual([]);
    expect(q(page, '.lt-columns').getAttribute('data-reshaping')).toBe('true');
    expect(q(page, '.lt-columns').getAttribute('data-active')).toBe('true');
    expect(qa(page, '.lt-columns > .lt-cell').length).toBe(ALL.length);
  });

  it('come back when the table reopens after reshaping', async () => {
    const page = await renderHost(base({ reshaping: true }));
    expect(roleAttrs(page)).toEqual([]);
    page.root.model = base({ reshaping: false });
    await page.waitForChanges();
    expect(q(page, '.lt-grid').getAttribute('role')).toBe('table');
    expect(roleAttrs(page).length).toBeGreaterThan(0);
  });

  it('show a notice instead of the columns when the lead is a notice', async () => {
    const page = await render({ lead: 'notice', notice: { tone: 'neutral', text: 'Not in records' }, open: false, body: 'none' });
    expect(q(page, '.lt-lead').getAttribute('data-lead')).toBe('notice');
    expect(q(page, '.lt-notice').getAttribute('data-active')).toBe('true');
    expect(q(page, '.lt-notice').textContent.trim()).toBe('Not in records');
    expect(q(page, '.lt-columns').getAttribute('data-active')).toBe('false');
    expect(q(page, '.lt-columns').getAttribute('aria-hidden')).toBe('true');
  });

  it('re-render the headings and the cells when the columns change', async () => {
    const page = await renderHost(base());
    expect(qa(page, '.lt-columns > .lt-cell').length).toBe(5);

    page.root.model = base({ columns: [PART, JOB] });
    await page.waitForChanges();

    expect(qa(page, '.lt-columns > .lt-cell').map(el => el.textContent)).toEqual(['Part no.', 'Job']);
    expect(qa(page, '.lt-row').every(row => row.querySelectorAll('.lt-cell').length === 2)).toBe(true);
    expect(q(page, '.lt-grid').style.getPropertyValue('--lt-tracks')).toBe(tableTracks([PART, JOB]).wide);
  });
});

describe('hidden columns', () => {
  it('leave no heading, no cell and no data-label in any row', async () => {
    const hidden = new Set(['jobNumber', 'image']);
    const page = await render({ columns: visibleColumns(ALL, hidden) });

    expect(qa(page, '.lt-columns > .lt-cell').map(el => el.textContent)).toEqual(['Part no.', 'Description', 'Company']);
    expect(qa(page, '.lt-row').every(row => row.querySelectorAll('.lt-cell').length === 3)).toBe(true);
    expect(qa(page, '[data-label]').some(el => ['Job', 'Image'].includes(el.getAttribute('data-label')))).toBe(false);
    expect(qa(page, '[data-kind="ref"], [data-kind="media"]')).toEqual([]);
    expect(page.body.textContent).not.toContain('J-100');
  });
});

describe('empty input', () => {
  it('renders no row, no role and no notice of its own', async () => {
    const page = await render({ rows: [] });
    expect(qa(page, '.lt-row')).toEqual([]);
    expect(roleAttrs(page)).toEqual([]);
    // The panel owns the sentence: the notice layer is empty and inactive, and no tone was chosen from the empty list.
    expect(q(page, '.lt-notice').textContent.trim()).toBe('');
    expect(q(page, '.lt-notice').getAttribute('data-active')).toBe('false');
    expect(q(page, '.lt-notice .notice-icon')).toBeNull();
  });
});

describe('cell kinds', () => {
  it('text renders its value, and a blank value is the grey em dash', async () => {
    const page = await render({ rows: [item({ company: 'Sample Co' }), item({ company: '  ' })] });
    const cells = qa(page, '.lt-row > [data-kind="text"]').filter(el => el.getAttribute('data-label') === 'Company');
    expect(cells[0].textContent).toBe('Sample Co');
    expect(cells[1].querySelector('.lt-none').textContent).toBe('—');
    expect(cells[1].textContent).toBe('—');
  });

  it('text marks only the growing column as growing', async () => {
    const page = await render();
    expect(qa(page, '.lt-row:first-child > [data-grow="true"]').map(el => el.getAttribute('data-label'))).toEqual(['Description']);
    expect(qa(page, '.lt-columns > [data-grow="true"]').map(el => el.textContent)).toEqual(['Description']);
  });

  it('code renders as the identity, kept out of translation, with a dash when blank', async () => {
    const page = await render({ rows: [item({ pn: 'TST-0001' }), item({ pn: null })] });
    const cells = qa(page, '.lt-row > .lt-code');
    expect(cells[0].textContent).toBe('TST-0001');
    expect(cells[0].getAttribute('data-identity')).toBe('true');
    expect(cells[0].getAttribute('translate')).toBe('no');
    expect(cells[1].querySelector('.lt-none').textContent).toBe('—');
  });

  it('ref renders its value verbatim, not formatted, with a dash when blank', async () => {
    const page = await render({ rows: [item({ job: '0012345' }), item({ job: null })] });
    const cells = qa(page, '.lt-row > .lt-ref');
    expect(cells[0].textContent).toBe('0012345');
    expect(cells[1].textContent).toBe('—');
  });

  it('a zero is shown, not replaced by the dash', async () => {
    const zero = column({ field: 'qty', kind: 'ref', label: 'Qty', value: () => 0 });
    const page = await render({ columns: [PART, zero] });
    expect(qa(page, '.lt-row > .lt-ref')[0].textContent).toBe('0');
  });

  it('carries data-label on every row cell, whatever the kind', async () => {
    const page = await render();
    const cells = qa(page, '.lt-row > .lt-cell');
    expect(cells.length).toBe(ALL.length * 2);
    expect(cells.every(el => !!el.getAttribute('data-label'))).toBe(true);
    expect(qa(page, '.lt-row:first-child > .lt-cell').map(el => el.getAttribute('data-label'))).toEqual(['Part no.', 'Description', 'Image', 'Job', 'Company']);
  });

  it('takes its notice tone from the panel, with a glyph for every tone but idle', async () => {
    const tones = ['positive', 'negative', 'neutral', 'attention', 'idle'] as const;

    for (const tone of tones) {
      const page = await render({ lead: 'notice', open: false, body: 'none', notice: { tone, text: 'Said in words' } });
      expect(q(page, '.lt-notice').classList.contains(`is-${tone}`)).toBe(true);
      expect(q(page, '.lt-notice').getAttribute('role')).toBe('status');
      expect(!!q(page, '.lt-notice .notice-icon')).toBe(tone !== 'idle');
    }
  });
});

describe('layouts', () => {
  it('writes the wide tracks to the grid, so every cell lands on one set of columns', async () => {
    const page = await render();
    expect(q(page, '.lt-grid').style.getPropertyValue('--lt-tracks')).toBe(tableTracks(ALL).wide);
  });

  it('is stackable (data-stack) with identity first and a growing column, and carries the stacked tracks', async () => {
    const page = await render();
    expect(q(page, '.lt-grid').getAttribute('data-stack')).toBe('true');
    expect(q(page, '.lt-grid').style.getPropertyValue('--lt-tracks-stacked')).toBe(tableTracks(ALL).stacked);
  });

  it('is not stackable without a growing column, and carries no stacked tracks', async () => {
    const page = await render({ columns: visibleColumns(ALL, new Set(['description'])) });
    expect(q(page, '.lt-grid').getAttribute('data-stack')).toBe('false');
    expect(q(page, '.lt-grid').style.getPropertyValue('--lt-tracks-stacked')).toBe('');
  });

  it('is not stackable when the identity is not first', async () => {
    const page = await render({ columns: [IMAGE, PART, DESCRIPTION] });
    expect(q(page, '.lt-grid').getAttribute('data-stack')).toBe('false');
  });

  it('marks the cells the stacked layout places by hand: the identity and the growing column, on rows and on headings', async () => {
    const page = await render();
    expect(qa(page, '.lt-row:first-child > [data-identity="true"]').length).toBe(1);
    expect(qa(page, '.lt-columns > [data-identity="true"]').map(el => el.textContent)).toEqual(['Part no.']);
    expect(qa(page, '.lt-columns > [data-grow="true"]').map(el => el.textContent)).toEqual(['Description']);
  });

  it('gives the cards what they caption from: the one-word strip label, and a data-label on every cell', async () => {
    const page = await render();
    const label = q(page, '.lt-columns-label');
    expect(label.textContent).toBe('Item list');
    expect(label.getAttribute('aria-hidden')).toBe('true');
    expect(qa(page, '.lt-row > .lt-cell').every(el => el.hasAttribute('data-label'))).toBe(true);
  });

  it('nests the grid as subgrids of one set of tracks: strip, columns, body and rows are all inside the one grid', async () => {
    const page = await render();
    const grid = q(page, '.lt-grid');
    for (const selector of ['.lt-lead', '.lt-columns', '.lt-body', '.lt-body-inner', '.lt-rows', '.lt-row']) {
      expect(grid.querySelector(selector)).not.toBeNull();
    }
    expect(q(page, '.lt-body').classList.contains('collapsible')).toBe(true);
    expect(q(page, '.lt-body-inner').classList.contains('collapsible-body')).toBe(true);
    expect(q(page, '.lt-lead').classList.contains('layer-stack')).toBe(true);
  });
});

describe('strip, body and busy state', () => {
  it('sets data-phase="busy" while busy, and "settled" otherwise', async () => {
    expect(q(await render({ busy: true, lead: 'skeleton', open: false }), '.lt-grid').getAttribute('data-phase')).toBe('busy');
    expect(q(await render(), '.lt-grid').getAttribute('data-phase')).toBe('settled');
  });

  it('always hides the skeleton layer from assistive tech, and hides each inactive layer', async () => {
    for (const lead of ['skeleton', 'columns', 'notice'] as const) {
      const page = await render({ lead, notice: { tone: 'neutral', text: 'Words' } });
      expect(q(page, '.lt-skeleton').getAttribute('aria-hidden')).toBe('true');

      const layers = { skeleton: '.lt-skeleton', notice: '.lt-notice', columns: '.lt-columns' };
      for (const [name, selector] of Object.entries(layers)) {
        const active = name === lead;
        expect(q(page, selector).getAttribute('data-active')).toBe(active ? 'true' : 'false');
        if (!active) expect(q(page, selector).getAttribute('aria-hidden')).toBe('true');
        if (active && name !== 'skeleton') expect(q(page, selector).hasAttribute('aria-hidden')).toBe(false);
      }
    }
  });

  it('renders the children when the body is not rows, and no rows', async () => {
    const page = await render({ body: 'run', lead: 'notice', notice: { tone: 'attention', text: 'Run it' } });
    expect(q(page, '.lt-body').getAttribute('data-body')).toBe('run');
    expect(q(page, '.lt-body .child-stand-in').textContent).toBe('child');
    expect(qa(page, '.lt-row')).toEqual([]);
  });

  it('keeps the outgoing rows rendered inside a shut body, so they slide away instead of blinking out', async () => {
    const page = await render({ open: false, lead: 'skeleton', busy: true });
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('false');
    expect(qa(page, '.lt-row').length).toBe(2);
  });

  it('keeps distinct rows for duplicate part numbers, and survives a rowKey that repeats', async () => {
    const duplicate = [item({ pn: 'TST-DUP' }), item({ pn: 'TST-DUP' })];

    const keyed = base({ rows: duplicate });
    expect(keyed.rowKey(duplicate[0], 0)).not.toBe(keyed.rowKey(duplicate[1], 1));
    expect(qa(await render({ rows: duplicate }), '.lt-row').length).toBe(2);

    // A host key that is not unique must not corrupt the DOM: the table suffixes it.
    const page = await render({ rows: duplicate, rowKey: () => 'same' });
    expect(qa(page, '.lt-row').length).toBe(2);
    expect(qa(page, '.lt-row > .lt-code').map(el => el.textContent)).toEqual(['TST-DUP', 'TST-DUP']);
  });
});

describe('thumbnail', () => {
  const mediaCell = (page: Page, rowIndex = 0) => qa(page, '.lt-row')[rowIndex].querySelector('.lt-media') as HTMLElement;
  const imageOf = (page: Page, rowIndex = 0) => mediaCell(page, rowIndex).querySelector('img') as HTMLImageElement;
  const fire = (target: HTMLElement, type: 'load' | 'error') => target.dispatchEvent(new Event(type));

  it('starts at loading for a URL: the open button is disabled, the failed stand-in is hidden, the picture is lazy', async () => {
    const page = await render();
    const cell = mediaCell(page);
    expect(cell.getAttribute('data-media')).toBe('loading');
    expect(cell.querySelector('.lt-media-open').hasAttribute('disabled')).toBe(true);
    expect(cell.querySelector('.lt-media-stand').getAttribute('aria-hidden')).toBe('true');
    expect(cell.querySelector('.lt-media-open').getAttribute('aria-hidden')).toBe('true');
    expect(imageOf(page).getAttribute('alt')).toBe('');
    expect(imageOf(page).getAttribute('loading')).toBe('lazy');
    expect(imageOf(page).getAttribute('decoding')).toBe('async');
    expect(imageOf(page).getAttribute('src')).toBe('https://example.invalid/a.png');
  });

  it('is ready once the picture loads: the button opens, and its label names the row by its identity value', async () => {
    const page = await render();
    fire(imageOf(page), 'load');
    const cell = mediaCell(page);
    expect(cell.getAttribute('data-media')).toBe('ready');

    const button = cell.querySelector('.lt-media-open') as HTMLButtonElement;
    expect(button.hasAttribute('disabled')).toBe(false);
    expect(button.getAttribute('aria-label')).toContain('TST-0001');
    expect(button.getAttribute('aria-label')).toBe('Open picture of TST-0001');
    expect(cell.querySelector('.lt-media-stand').getAttribute('aria-hidden')).toBe('true');
  });

  it('reports the picture it was asked to open, through onOpenMedia', async () => {
    const onOpenMedia = jest.fn();
    const page = await render({ onOpenMedia });
    fire(imageOf(page), 'load');
    (mediaCell(page).querySelector('.lt-media-open') as HTMLButtonElement).click();
    expect(onOpenMedia).toHaveBeenCalledTimes(1);
    expect(onOpenMedia.mock.calls[0][1]).toBe('https://example.invalid/a.png');
    expect(onOpenMedia.mock.calls[0][0].tagName).toBe('IMG');
  });

  it('labels the button with the picture alone when no column is the identity', async () => {
    const page = await render({ columns: [IMAGE, DESCRIPTION] });
    fire(imageOf(page), 'load');
    expect(mediaCell(page).querySelector('.lt-media-open').getAttribute('aria-label')).toBe('Open picture of');
  });

  it('with no URL on record is the none box: an image role with the label, not a dash, not blank, not a button', async () => {
    const page = await render();
    const cell = mediaCell(page, 1);
    expect(cell.getAttribute('data-media')).toBe('none');
    const stand = cell.querySelector('.lt-media-stand');
    expect(stand.getAttribute('role')).toBe('img');
    expect(stand.getAttribute('aria-label')).toBe('No picture on record');
    expect(stand.getAttribute('title')).toBe('No picture on record');
    expect(stand.hasAttribute('aria-hidden')).toBe(false);
    expect(stand.querySelector('svg').getAttribute('aria-hidden')).toBe('true');
    expect(cell.querySelector('.lt-media-open')).toBeNull();
    expect(cell.querySelector('img')).toBeNull();
    expect(cell.querySelector('.lt-none')).toBeNull();
  });

  it('treats a whitespace URL as no URL', async () => {
    const page = await render({ rows: [item({ img: '   ' })] });
    expect(mediaCell(page).getAttribute('data-media')).toBe('none');
  });

  it('is failed when the picture errors: the stand-in is announced, the button is dead', async () => {
    const page = await render();
    fire(imageOf(page), 'error');
    const cell = mediaCell(page);
    expect(cell.getAttribute('data-media')).toBe('failed');
    const stand = cell.querySelector('.lt-media-stand');
    expect(stand.hasAttribute('aria-hidden')).toBe(false);
    expect(stand.getAttribute('role')).toBe('img');
    expect(stand.getAttribute('aria-label')).toBe('Picture did not load');
    expect(cell.querySelector('.lt-media-open').hasAttribute('disabled')).toBe(true);
    expect(cell.querySelector('.lt-media-open').getAttribute('aria-hidden')).toBe('true');
  });

  describe('a picture already complete before the listener', () => {
    const stub = (complete: boolean, naturalWidth: number) => {
      const proto = Object.getPrototypeOf(document.createElement('img'));
      Object.defineProperty(proto, 'complete', { configurable: true, get: () => complete });
      Object.defineProperty(proto, 'naturalWidth', { configurable: true, get: () => naturalWidth });
      return () => {
        delete proto.complete;
        delete proto.naturalWidth;
      };
    };

    it('settles to ready without a load event: the button opens and is no longer hidden', async () => {
      const restore = stub(true, 40);
      try {
        const page = await render();
        await page.waitForChanges();
        const cell = mediaCell(page);
        expect(cell.getAttribute('data-media')).toBe('ready');
        expect(cell.querySelector('.lt-media-open').hasAttribute('disabled')).toBe(false);
        expect(cell.querySelector('.lt-media-open').hasAttribute('aria-hidden')).toBe(false);
      } finally {
        restore();
      }
    });

    it('stays at loading when complete with no width: the error event is what fails it', async () => {
      const restore = stub(true, 0);
      try {
        const page = await render();
        await page.waitForChanges();
        expect(mediaCell(page).getAttribute('data-media')).toBe('loading');
      } finally {
        restore();
      }
    });
  });

  it('stays failed when the parent re-renders with the same rows, and returns to loading for a different src', async () => {
    const model = base();
    const page = await renderHost(model);
    const cellBefore = mediaCell(page);

    fire(imageOf(page), 'error');
    expect(mediaCell(page).getAttribute('data-media')).toBe('failed');

    // The same rows, a new model object: the host re-renders and the table is rebuilt from its props.
    page.root.model = { ...model, rows: model.rows.map(row => ({ ...row })) };
    await page.waitForChanges();
    expect(mediaCell(page)).toBe(cellBefore);
    expect(mediaCell(page).getAttribute('data-media')).toBe('failed');
    expect(mediaCell(page).querySelector('.lt-media-stand').hasAttribute('aria-hidden')).toBe(false);

    // And again, with an unrelated change (the table goes busy): still failed.
    page.root.model = { ...model, busy: true, lead: 'skeleton', open: false };
    await page.waitForChanges();
    expect(mediaCell(page).getAttribute('data-media')).toBe('failed');

    // A different picture on the same row is a new element, starting over at loading.
    page.root.model = { ...model, rows: [item({ img: 'https://example.invalid/b.png' }), model.rows[1]] };
    await page.waitForChanges();
    expect(mediaCell(page)).not.toBe(cellBefore);
    expect(mediaCell(page).getAttribute('data-media')).toBe('loading');
    expect(imageOf(page).getAttribute('src')).toBe('https://example.invalid/b.png');
    expect(mediaCell(page).querySelector('.lt-media-open').hasAttribute('disabled')).toBe(true);
  });

  it('stays ready across a re-render with the same rows', async () => {
    const model = base();
    const page = await renderHost(model);
    fire(imageOf(page), 'load');

    page.root.model = { ...model };
    await page.waitForChanges();
    expect(mediaCell(page).getAttribute('data-media')).toBe('ready');
    expect(mediaCell(page).querySelector('.lt-media-open').hasAttribute('disabled')).toBe(false);
  });

  it('does not move on from failed when a late load event arrives, nor from ready when a late error does', async () => {
    const page = await render();
    fire(imageOf(page), 'error');
    fire(imageOf(page), 'load');
    expect(mediaCell(page).getAttribute('data-media')).toBe('failed');

    const other = await render();
    fire(imageOf(other), 'load');
    fire(imageOf(other), 'error');
    expect(mediaCell(other).getAttribute('data-media')).toBe('ready');
  });

  it('keys a picture cell by its src, so one failing picture does not mark its neighbours', async () => {
    const page = await render({ rows: [item({ img: 'https://example.invalid/a.png' }), item({ pn: 'TST-0002', img: 'https://example.invalid/b.png' })] });
    fire(imageOf(page, 0), 'error');
    expect(mediaCell(page, 0).getAttribute('data-media')).toBe('failed');
    expect(mediaCell(page, 1).getAttribute('data-media')).toBe('loading');
  });
});
