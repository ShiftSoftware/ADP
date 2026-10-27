import { Component, Prop, h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';
import { readFileSync } from 'fs';
import path from 'path';

import * as revealBelowModule from '~lib/reveal-below';

import {
  ClosingDrawer,
  LookupColumn,
  LookupTable,
  LookupTableProps,
  RowSlot,
  cellText,
  closingDrawers,
  dateText,
  drawerAfterLookup,
  drawerAfterPage,
  drawerButtonId,
  drawerId,
  formatNumber,
  nonEmptyColumns,
  PlaceholderKind,
  TablePlaceholder,
  parseHiddenFields,
  placeholderKind,
  revealDrawer,
  shouldToggleOnRowClick,
  shownBody,
  shownPlaceholder,
  tableLead,
  tableRoles,
  tableSlots,
  tableTracks,
  visibleColumns,
} from './lookup-table';

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
const JOB = column({ field: 'jobNumber', label: 'Job', kind: 'text', value: r => r.job });
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
    const CODE = 'minmax(180px, 1.1fr)';
    const GROW = 'minmax(200px, 2.4fr)';
    const TEXT = 'minmax(150px, 1.2fr)';
    const MEDIA = 'calc(var(--lt-media-width) + 24px)';

    it('gives one track per visible column, by kind: a floor and a share of the width, never the content size', () => {
      expect(tableTracks([PART, DESCRIPTION, IMAGE, JOB, COMPANY]).wide).toBe([CODE, GROW, MEDIA, TEXT, TEXT].join(' '));
      expect(tableTracks(ALL).wide).not.toContain('max-content');
      expect(tableTracks(ALL).wide).not.toContain('auto');
    });

    it('does not depend on the rows: the same columns give the same tracks whatever values they hold', () => {
      expect(tableTracks([PART, IMAGE, JOB]).wide).toBe([CODE, MEDIA, TEXT].join(' '));
      expect(tableTracks([PART]).wide).toBe(CODE);
    });

    it('a picture alone still reaches the card edge', () => {
      expect(tableTracks([IMAGE]).wide).toBe('minmax(0, 1fr)');
    });

    it('lets only a text column grow: grow on another kind is ignored', () => {
      const growingCode = column({ field: 'x', kind: 'code', grow: true });
      expect(tableTracks([PART, growingCode]).wide).toBe([CODE, CODE].join(' '));
      expect(tableTracks([PART, growingCode]).stacked).toBeNull();
    });

    it('stacks when the identity is first and a growing column is visible: the identity and growing tracks collapse into one', () => {
      const { stacked } = tableTracks(ALL);
      expect(stacked).toBe(['minmax(160px, 1fr)', MEDIA, TEXT, TEXT].join(' '));
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
    // The headings depend on the configuration alone, so the answer cannot change them (owner, F1).
    const lead = (over: Partial<{ busy: boolean; idle: boolean; body: string; columns: number; placeholder: boolean }> = {}) =>
      tableLead({ busy: false, idle: false, body: 'rows', columns: 3, ...over });

    it('is the headings while idle, whatever the body is', () => {
      expect(lead({ idle: true, body: 'none' })).toBe('columns');
      expect(lead({ idle: true, body: 'rows' })).toBe('columns');
    });

    it('is the headings while busy, whatever the body is', () => {
      expect(lead({ busy: true, body: 'none' })).toBe('columns');
      expect(lead({ busy: true, body: 'rows' })).toBe('columns');
      expect(lead({ busy: true, idle: true, body: 'none' })).toBe('columns');
    });

    it('is the headings above rows', () => {
      expect(lead({ body: 'rows' })).toBe('columns');
    });

    it("is a notice for an outcome without rows: empty, unauthorized, error, or a body of the panel's own", () => {
      expect(lead({ body: 'none' })).toBe('notice');
      expect(lead({ body: 'run' })).toBe('notice');
    });

    it('is a notice when every column is hidden, in every state, because there are no headings to show', () => {
      expect(lead({ columns: 0, body: 'none' })).toBe('notice');
      expect(lead({ columns: 0, idle: true, body: 'none' })).toBe('notice');
      expect(lead({ columns: 0, busy: true, body: 'none' })).toBe('notice');
      expect(lead({ columns: 0, body: 'rows' })).toBe('notice');
    });

    it('never answers skeleton: the union is columns | notice', () => {
      const answers = new Set<string>();
      for (const busy of [false, true])
        for (const idle of [false, true]) for (const body of ['rows', 'none', 'run']) for (const columns of [0, 3]) answers.add(lead({ busy, idle, body, columns }));
      expect([...answers].sort()).toEqual(['columns', 'notice']);
    });

    // Owner, 2026-10-27: the body's placeholder says a row-less outcome in words, so the strip does not.
    it('keeps the headings for an outcome without rows when the body says it in words (placeholder), so it is said once', () => {
      expect(lead({ body: 'none', placeholder: true })).toBe('columns');
      expect(lead({ body: 'run', placeholder: true })).toBe('columns');
      expect(lead({ body: 'none', placeholder: false })).toBe('notice');
    });

    it('still has no headings to keep when every column is hidden, placeholder or not', () => {
      expect(lead({ columns: 0, body: 'none', placeholder: true })).toBe('notice');
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

describe('placeholderKind', () => {
  const kind = (over: Partial<Parameters<typeof placeholderKind>[0]> = {}) => placeholderKind({ columns: 3, loaded: true, failed: false, authorized: true, records: 4, ...over });

  it('is idle before any lookup, and asserts nothing about a vehicle', () => {
    expect(kind({ loaded: false, records: 0, authorized: undefined })).toBe('idle');
    // Whatever stale data the state carries, no vehicle on screen is idle: nothing is claimed about one.
    expect(kind({ loaded: false, records: 9, authorized: false })).toBe('idle');
  });

  it('is empty for an authorized vehicle with nothing on file (and for an authorization the DTO leaves unsaid)', () => {
    expect(kind({ records: 0 })).toBe('empty');
    expect(kind({ records: 0, authorized: undefined })).toBe('empty');
  });

  it('is unauthorized for a vehicle not in the records, whatever its list holds: not-in-records is never "empty" and never the list', () => {
    expect(kind({ authorized: false, records: 0 })).toBe('unauthorized');
    expect(kind({ authorized: false, records: 7 })).toBe('unauthorized');
  });

  it('is error for a failed lookup, whatever else the state holds', () => {
    expect(kind({ failed: true, loaded: false, records: 0 })).toBe('error');
    expect(kind({ failed: true, loaded: true, authorized: false, records: 3 })).toBe('error');
  });

  it('is null above rows', () => {
    expect(kind()).toBeNull();
    expect(kind({ records: 1 })).toBeNull();
  });

  it('is null when every column is hidden, in every state: the strip carries the sentence and the body does not say it again', () => {
    for (const over of [{ loaded: false }, { records: 0 }, { authorized: false }, { failed: true }, {}, { leaving: true }]) expect(kind({ columns: 0, ...over })).toBeNull();
  });

  describe('through a lookup (the placeholder in place stays in place)', () => {
    // The owner's state is the last answer's until the next lands, so the same inputs give the same kind
    // whether or not a lookup is in flight: nothing in the derivation reads `busy`, and nothing may flip.
    it('keeps the idle placeholder through the first lookup, the empty one through a retry, and the error one while the retry is in flight', () => {
      expect(kind({ loaded: false, records: 0 })).toBe('idle');
      expect(kind({ records: 0 })).toBe('empty');
      expect(kind({ failed: true, loaded: false, records: 0 })).toBe('error');
      expect(kind({ authorized: false, records: 0 })).toBe('unauthorized');
    });

    it('has no placeholder under rows in flight: the rows stay, covered, and the region stays shut', () => {
      expect(kind({ records: 4 })).toBeNull();
    });
  });

  describe('leaving (a clear, or a failure raised on a settled card)', () => {
    it('opens the idle placeholder as rows shut, so the region is not empty while the rows go', () => {
      expect(kind({ leaving: true, records: 4 })).toBe('idle');
    });

    it('holds the kind it already has: empty stays empty, unauthorized stays unauthorized, error stays error, until the state itself changes', () => {
      expect(kind({ leaving: true, records: 0 })).toBe('empty');
      expect(kind({ leaving: true, authorized: false, records: 0 })).toBe('unauthorized');
      expect(kind({ leaving: true, failed: true, loaded: false, records: 0 })).toBe('error');
    });
  });

  it('only ever answers one of the four kinds or null', () => {
    const answers = new Set<string | null>();
    for (const columns of [0, 3])
      for (const loaded of [false, true])
        for (const failed of [false, true])
          for (const authorized of [undefined, true, false])
            for (const records of [0, 5]) for (const leaving of [false, true]) answers.add(placeholderKind({ columns, loaded, failed, authorized, records, leaving }));
    expect([...answers].map(String).sort()).toEqual(['empty', 'error', 'idle', 'null', 'unauthorized']);
  });
});

describe('shownPlaceholder', () => {
  it('is the current kind, else the one the region last showed while it slides shut, else nothing', () => {
    expect(shownPlaceholder('empty', 'idle')).toBe('empty');
    expect(shownPlaceholder(null, 'error')).toBe('error');
    expect(shownPlaceholder(null, null)).toBeNull();
    expect(shownPlaceholder(null)).toBeNull();
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
    expect(qa(page, '[data-kind="media"]')).toEqual([]);
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
    expect(qa(page, '.lt-slot:first-child .lt-row > [data-grow="true"]').map(el => el.getAttribute('data-label'))).toEqual(['Description']);
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

  it('carries data-label on every row cell, whatever the kind', async () => {
    const page = await render();
    const cells = qa(page, '.lt-row > .lt-cell');
    expect(cells.length).toBe(ALL.length * 2);
    expect(cells.every(el => !!el.getAttribute('data-label'))).toBe(true);
    expect(qa(page, '.lt-slot:first-child .lt-row > .lt-cell').map(el => el.getAttribute('data-label'))).toEqual(['Part no.', 'Description', 'Image', 'Job', 'Company']);
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
    expect(qa(page, '.lt-slot:first-child .lt-row > [data-identity="true"]').length).toBe(1);
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
    expect(q(await render({ busy: true, lead: 'columns', open: false }), '.lt-grid').getAttribute('data-phase')).toBe('busy');
    expect(q(await render(), '.lt-grid').getAttribute('data-phase')).toBe('settled');
  });

  it('has no skeleton layer: the strip is two layers, the headings and a notice', async () => {
    const page = await render({ busy: true, lead: 'columns', open: false });
    expect(qa(page, '.lt-lead > .layer').length).toBe(2);
    expect(page.body.querySelector('.lt-skeleton')).toBeNull();
  });

  it('shows exactly one layer at a time, and hides each inactive layer from assistive tech', async () => {
    for (const lead of ['columns', 'notice'] as const) {
      const page = await render({ lead, notice: { tone: 'neutral', text: 'Words' }, body: lead === 'notice' ? 'none' : 'rows', open: lead === 'columns' });
      const layers = { notice: '.lt-notice', columns: '.lt-columns' };
      for (const [name, selector] of Object.entries(layers)) {
        const active = name === lead;
        expect(q(page, selector).getAttribute('data-active')).toBe(active ? 'true' : 'false');
        if (!active) expect(q(page, selector).getAttribute('aria-hidden')).toBe('true');
        if (active) expect(q(page, selector).hasAttribute('aria-hidden')).toBe(false);
      }
    }
  });

  describe('the headings while idle and in flight (F1)', () => {
    it('are the active layer, with the same cells as above rows, while busy and the body is shut', async () => {
      const page = await render({ busy: true, lead: 'columns', open: false });
      expect(q(page, '.lt-lead').getAttribute('data-lead')).toBe('columns');
      expect(q(page, '.lt-columns').getAttribute('data-active')).toBe('true');
      expect(qa(page, '.lt-columns > .lt-cell').map(el => el.textContent)).toEqual(ALL.map(column => column.label));
      expect(q(page, '.lt-notice').getAttribute('data-active')).toBe('false');
    });

    it('are the active layer while idle with no rows, and there is nothing else in the strip to be shown', async () => {
      const page = await render({ rows: [], body: 'none', open: false, lead: 'columns' });
      expect(q(page, '.lt-columns').getAttribute('data-active')).toBe('true');
      expect(qa(page, '.lt-columns > .lt-cell').length).toBe(ALL.length);
    });

    it('carry no table roles and are hidden from assistive tech without rows, and get both back when the rows open', async () => {
      const shut = await render({ busy: true, lead: 'columns', open: false });
      expect(q(shut, '.lt-columns').hasAttribute('role')).toBe(false);
      expect(q(shut, '.lt-columns').getAttribute('aria-hidden')).toBe('true');
      expect(qa(shut, '.lt-columns > .lt-cell').every(cell => !cell.hasAttribute('role'))).toBe(true);
      expect(roleAttrs(shut)).toEqual([]);

      const open = await render({ lead: 'columns', open: true });
      expect(q(open, '.lt-columns').getAttribute('role')).toBe('row');
      expect(q(open, '.lt-columns').hasAttribute('aria-hidden')).toBe(false);
      expect(qa(open, '.lt-columns > .lt-cell').every(cell => cell.getAttribute('role') === 'columnheader')).toBe(true);
    });

    it('stay the same elements from idle through busy to rows: the same structure, so nothing re-animates', async () => {
      const model = base({ rows: [], body: 'none', open: false, lead: 'columns' });
      const page = await renderHost(model);
      const strip = q(page, '.lt-columns');
      const heading = qa(page, '.lt-columns > .lt-cell')[0];

      page.root.model = { ...model, busy: true };
      await page.waitForChanges();
      expect(q(page, '.lt-columns')).toBe(strip);
      expect(qa(page, '.lt-columns > .lt-cell')[0]).toBe(heading);
      expect(q(page, '.lt-columns').getAttribute('data-active')).toBe('true');

      page.root.model = base({ lead: 'columns' });
      await page.waitForChanges();
      expect(q(page, '.lt-columns')).toBe(strip);
      expect(qa(page, '.lt-columns > .lt-cell')[0]).toBe(heading);
      expect(q(page, '.lt-columns').getAttribute('data-active')).toBe('true');
    });

    it('give way to a notice and back by switching the active layer, never by unmounting either', async () => {
      const model = base({ busy: true, lead: 'columns', open: false });
      const page = await renderHost(model);
      const notice = q(page, '.lt-notice');
      const strip = q(page, '.lt-columns');

      page.root.model = { ...model, busy: false, lead: 'notice', body: 'none', rows: [], notice: { tone: 'neutral', text: 'Not in records' } };
      await page.waitForChanges();
      expect(q(page, '.lt-notice')).toBe(notice);
      expect(q(page, '.lt-columns')).toBe(strip);
      expect(q(page, '.lt-notice').getAttribute('data-active')).toBe('true');
      expect(q(page, '.lt-columns').getAttribute('data-active')).toBe('false');
      expect(q(page, '.lt-notice').textContent.trim()).toBe('Not in records');

      page.root.model = { ...model, busy: true, lead: 'columns', body: 'none', rows: [], notice: { tone: 'neutral', text: 'Not in records' } };
      await page.waitForChanges();
      expect(q(page, '.lt-notice')).toBe(notice);
      expect(q(page, '.lt-columns').getAttribute('data-active')).toBe('true');
      expect(q(page, '.lt-notice').getAttribute('data-active')).toBe('false');
      // The outgoing sentence stays in the fading layer, so it fades with its words, not blank.
      expect(q(page, '.lt-notice').textContent.trim()).toBe('Not in records');
    });
  });

  it('renders the children when the body is not rows, and no rows', async () => {
    const page = await render({ body: 'run', lead: 'notice', notice: { tone: 'attention', text: 'Run it' } });
    expect(q(page, '.lt-body').getAttribute('data-body')).toBe('run');
    expect(q(page, '.lt-body .child-stand-in').textContent).toBe('child');
    expect(qa(page, '.lt-row')).toEqual([]);
  });

  it('keeps the outgoing rows rendered inside a shut body, so they slide away instead of blinking out', async () => {
    const page = await render({ open: false, lead: 'columns', busy: true });
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
    page.root.model = { ...model, busy: true, lead: 'columns', open: false };
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

// ---------------------------------------------------------------------------------------------
// The service-history modes (spec §§ 7, 9, 14): date, number and action cells, compact density,
// the embedded table and the derivation that drops empty columns. Fictional fixtures.
// ---------------------------------------------------------------------------------------------

type Visit = { invoice: string | null; date: string | null; odometer: number | string | null; type: string | null; part: string | null };

const visit = (over: Partial<Visit> = {}): Visit => ({ invoice: 'INV-001', date: '2025-03-04', odometer: 101328, type: 'Periodic service', part: null, ...over });

const col = <T,>(over: Partial<LookupColumn<T>> & Pick<LookupColumn<T>, 'field' | 'kind'>): LookupColumn<T> => ({ label: over.field, ...over });

const V_INVOICE = col<Visit>({ field: 'invoice', label: 'Invoice', kind: 'code', identity: true, value: v => v.invoice });
const V_DATE = col<Visit>({ field: 'date', label: 'Date', kind: 'date', value: v => v.date });
const V_ODOMETER = col<Visit>({ field: 'odometer', label: 'Odometer', kind: 'number', value: v => v.odometer });
const V_TYPE = col<Visit>({ field: 'type', label: 'Service type', kind: 'text', grow: true, value: v => v.type });
const V_PART = col<Visit>({ field: 'part', label: 'Part', kind: 'text', value: v => v.part });
const V_DETAILS = col<Visit>({ field: 'details', label: 'Details', kind: 'action' });

const visitStrings = { ...strings, expandRow: 'Show details of', collapseRow: 'Hide details of' };

const visitBase = (over: Partial<LookupTableProps<Visit>> = {}): LookupTableProps<Visit> => ({
  columns: [V_INVOICE, V_DATE, V_ODOMETER, V_TYPE, V_DETAILS],
  rows: [visit(), visit({ invoice: 'INV-002', date: '2025-01-20T08:30:00', odometer: '98000', type: null })],
  rowKey: (_, index) => `slot-${index}`,
  lead: 'columns',
  notice: null,
  busy: false,
  body: 'rows',
  open: true,
  strings: visitStrings,
  ...over,
});

const renderVisits = (over: Partial<LookupTableProps<Visit>> = {}) => newSpecPage({ components: [], template: () => <LookupTable {...visitBase(over)} /> });

@Component({ tag: 'lt-visit-host' })
class LtVisitHost {
  @Prop() model: LookupTableProps<Visit>;

  render() {
    return this.model ? (
      <div>
        <LookupTable {...this.model} />
      </div>
    ) : null;
  }
}

const renderVisitHost = async (model: LookupTableProps<Visit>) => {
  const page = await newSpecPage({ components: [LtVisitHost], html: '<lt-visit-host></lt-visit-host>' });
  page.root.model = model;
  await page.waitForChanges();
  return page;
};

describe('date, number and action: derivations', () => {
  describe('dateText', () => {
    it('keeps an ISO date and cuts a timestamp to its day, with no time zone conversion', () => {
      expect(dateText('2025-03-04')).toBe('2025-03-04');
      expect(dateText('2025-03-04T23:59:59Z')).toBe('2025-03-04');
      expect(dateText('2025-03-04T00:00:00+05:00')).toBe('2025-03-04');
    });

    it('trims first', () => {
      expect(dateText('  2025-03-04 ')).toBe('2025-03-04');
    });
  });

  describe('formatNumber', () => {
    it('groups English figures and keeps up to three decimals, so a quantity of 4.2 stays 4.2', () => {
      expect(formatNumber(101328)).toBe('101,328');
      expect(formatNumber(4.2, 'en')).toBe('4.2');
      expect(formatNumber(0.05, 'en')).toBe('0.05');
      expect(formatNumber(1234.56789, 'en')).toBe('1,234.568');
      expect(formatNumber(0)).toBe('0');
    });

    it("speaks the language's grouping and separators (the runtime's own), English for a language it does not know", () => {
      expect(formatNumber(101328, 'ru')).toBe(new Intl.NumberFormat(['ru'], { maximumFractionDigits: 3, numberingSystem: 'latn' }).format(101328));
      expect(formatNumber(101328, 'xx')).toBe('101,328');
    });

    // Owner, 2026-10-27 (decision 19): Latin digits in every language, so a figure sits with the ISO
    // dates, VINs and invoice numbers beside it. Only the digits are pinned, not the separators.
    it('uses Latin digits in Arabic, Kurdish, Russian and English', () => {
      const arabicIndic = /[\u0660-\u0669\u06F0-\u06F9]/;
      for (const lang of ['ar', 'ku', 'ru', 'en']) {
        expect(formatNumber(1234567.891, lang)).toMatch(/1.234.567.891/);
        expect(formatNumber(1234567.891, lang)).not.toMatch(arabicIndic);
        expect(formatNumber(0, lang)).toBe('0');
      }
      expect(formatNumber(101328, 'ar')).toMatch(/^101.328$/);
    });
  });

  describe('cellText by kind', () => {
    const textOf = (kind: LookupColumn<Visit>['kind'], value: Visit['odometer'] | undefined) => cellText(col<Visit>({ field: 'f', kind, value: () => value }), visit());

    it('a date is cut to its day and blank when missing', () => {
      expect(textOf('date', '2025-03-04T10:00:00')).toBe('2025-03-04');
      expect(textOf('date', null)).toBeNull();
      expect(textOf('date', '  ')).toBeNull();
    });

    it('a number is its canonical figure, 0 is a value, and anything that is not a finite number is blank', () => {
      expect(textOf('number', 101328)).toBe('101328');
      expect(textOf('number', '98000')).toBe('98000');
      expect(textOf('number', ' 12 ')).toBe('12');
      expect(textOf('number', 0)).toBe('0');
      expect(textOf('number', 4.2)).toBe('4.2');
      expect(textOf('number', 'abc')).toBeNull();
      expect(textOf('number', '12 km')).toBeNull();
      expect(textOf('number', Infinity)).toBeNull();
      expect(textOf('number', NaN)).toBeNull();
      expect(textOf('number', null)).toBeNull();
      expect(textOf('number', undefined)).toBeNull();
      expect(textOf('number', '')).toBeNull();
    });

    it('an action column has no value', () => {
      expect(cellText(V_DETAILS, visit())).toBeNull();
    });
  });

  describe('tableTracks for the new kinds', () => {
    it('gives a date and a number a floor and a small share, and the button exactly its circle', () => {
      expect(tableTracks([V_DATE]).wide).toBe('minmax(120px, 0.8fr)');
      expect(tableTracks([V_ODOMETER]).wide).toBe('minmax(110px, 0.8fr)');
      expect(tableTracks([V_DETAILS]).wide).toBe('calc(var(--lt-action-size) + 24px)');
    });

    it('does not depend on the rows, and stacks with the action among the other columns', () => {
      const { wide, stacked } = tableTracks([V_INVOICE, V_DATE, V_ODOMETER, V_TYPE, V_DETAILS]);
      expect(wide).toBe(['minmax(180px, 1.1fr)', 'minmax(120px, 0.8fr)', 'minmax(110px, 0.8fr)', 'minmax(200px, 2.4fr)', 'calc(var(--lt-action-size) + 24px)'].join(' '));
      expect(stacked).toBe(['minmax(160px, 1fr)', 'minmax(120px, 0.8fr)', 'minmax(110px, 0.8fr)', 'calc(var(--lt-action-size) + 24px)'].join(' '));
    });
  });

  describe('nonEmptyColumns', () => {
    const rows = [visit({ part: null }), visit({ part: '  ' }), visit({ part: '' })];

    it('drops a column that is empty on every row: null, undefined, empty and whitespace', () => {
      expect(nonEmptyColumns([V_INVOICE, V_DATE, V_PART], rows).map(c => c.field)).toEqual(['invoice', 'date']);
      expect(nonEmptyColumns([V_PART], [visit({ part: undefined })])).toEqual([]);
    });

    it('keeps a column with a value on any one row, and the definition order', () => {
      expect(nonEmptyColumns([V_INVOICE, V_PART, V_DATE], [...rows, visit({ part: 'P-1' })]).map(c => c.field)).toEqual(['invoice', 'part', 'date']);
    });

    it('treats a zero as a value, and a number that is not a number as empty', () => {
      expect(nonEmptyColumns([V_ODOMETER], [visit({ odometer: 0 })]).length).toBe(1);
      expect(nonEmptyColumns([V_ODOMETER], [visit({ odometer: 'n/a' }), visit({ odometer: null })])).toEqual([]);
    });

    it('never drops the identity or an action column, even on empty rows', () => {
      const empty = [visit({ invoice: null }), visit({ invoice: null })];
      expect(nonEmptyColumns([V_INVOICE, V_DETAILS, V_PART], empty).map(c => c.field)).toEqual(['invoice', 'details']);
    });

    it('keeps nothing from an empty list but the identity and the action', () => {
      expect(nonEmptyColumns([V_INVOICE, V_DATE, V_PART, V_DETAILS], []).map(c => c.field)).toEqual(['invoice', 'details']);
    });
  });

  describe('drawerId', () => {
    it('is stable and valid for any key', () => {
      expect(drawerId('INV-001')).toBe('lt-drawer-INV-001');
      expect(drawerId('INV-001')).toBe(drawerId('INV-001'));
      expect(drawerId('a b/c#1')).toMatch(/^lt-drawer-[A-Za-z0-9_-]+$/);
    });

    it('never gives two keys the same id', () => {
      expect(drawerId('a b')).not.toBe(drawerId('a_b'));
      expect(drawerId('a-20b')).not.toBe(drawerId('a b'));
      expect(drawerId('a_20_b')).not.toBe(drawerId('a b'));
    });
  });
});

describe('tableSlots', () => {
  it('opens the first `opened` current rows and starts the rest shut, then appends the outgoing rows', () => {
    const slots = tableSlots({ current: 3, outgoing: 2, opened: 1, bodyOpen: true, currentVeil: 'cover', outgoingVeil: 'cover' });
    expect(slots.map(slot => slot.open)).toEqual([true, false, false, false, false]);
    expect(slots.every(slot => slot.veil === 'cover')).toBe(true);
  });

  it('keeps leaving rows open while the whole body shuts, so the region leaves as one movement', () => {
    const slots = tableSlots({ current: 0, outgoing: 2, opened: 0, bodyOpen: false, currentVeil: 'none', outgoingVeil: 'cover' });
    expect(slots).toEqual([
      { open: true, veil: 'cover' },
      { open: true, veil: 'cover' },
    ]);
  });
});

describe('date cell', () => {
  it('renders the day as the DTO sent it, cut from a timestamp, with the class the date ink and tabular figures key on', async () => {
    const page = await renderVisits();
    const cells = qa(page, '.lt-row > .lt-date');
    expect(cells.map(el => el.textContent)).toEqual(['2025-03-04', '2025-01-20']);
    expect(cells.every(el => el.getAttribute('data-kind') === 'date' && el.getAttribute('data-label') === 'Date')).toBe(true);
  });

  it('is the grey dash when missing, and is not growing', async () => {
    const page = await renderVisits({ rows: [visit({ date: null })] });
    const cell = q(page, '.lt-row > .lt-date');
    expect(cell.querySelector('.lt-none').textContent).toBe('—');
    expect(cell.hasAttribute('data-grow')).toBe(false);
  });

  it('is the same in every language: the ISO digits are a reading, not a locale format', async () => {
    const page = await renderVisits({ lang: 'ar' });
    expect(qa(page, '.lt-row > .lt-date')[0].textContent).toBe('2025-03-04');
  });
});

describe('number cell', () => {
  it('renders a locale-grouped figure, from a number or a numeric string, with no unit', async () => {
    const page = await renderVisits();
    expect(qa(page, '.lt-row > .lt-number').map(el => el.textContent)).toEqual(['101,328', '98,000']);
    expect(qa(page, '.lt-row > .lt-number').every(el => el.getAttribute('data-kind') === 'number')).toBe(true);
  });

  it('follows the table language, in Latin digits in every language', async () => {
    const page = await renderVisits({ lang: 'ar', rows: [visit()] });
    expect(q(page, '.lt-row > .lt-number').textContent).toBe(formatNumber(101328, 'ar'));
    expect(q(page, '.lt-row > .lt-number').textContent).toMatch(/^101.328$/);
  });

  it('shows a zero, and the dash for null and for a value that is not a number', async () => {
    const page = await renderVisits({ rows: [visit({ odometer: 0 }), visit({ odometer: null }), visit({ odometer: 'n/a' })] });
    const cells = qa(page, '.lt-row > .lt-number');
    expect(cells[0].textContent).toBe('0');
    expect(cells[1].textContent).toBe('—');
    expect(cells[2].querySelector('.lt-none').textContent).toBe('—');
  });

  it('marks its heading with the kind, which the stylesheet end-aligns', async () => {
    const page = await renderVisits();
    expect(qa(page, '.lt-columns > [data-kind="number"]').map(el => el.textContent)).toEqual(['Odometer']);
    expect(qa(page, '.lt-columns > [data-kind="date"]').map(el => el.textContent)).toEqual(['Date']);
  });
});

describe('action cell', () => {
  const button = (page: Page, row = 0) => qa(page, '.lt-row')[row].querySelector('.lt-action-button') as HTMLButtonElement;

  it('is a real button of type button, closed, pointing at the drawer the row will own, with a name from the strings and the identity', async () => {
    const page = await renderVisits({ actions: { expandedKey: null, onToggle: () => undefined, keyOf: row => row.invoice } });
    const b = button(page);
    expect(b.tagName).toBe('BUTTON');
    expect(b.getAttribute('type')).toBe('button');
    expect(b.getAttribute('aria-expanded')).toBe('false');
    expect(b.getAttribute('aria-controls')).toBe(drawerId('INV-001'));
    expect(b.getAttribute('aria-label')).toBe('Show details of INV-001');
    expect(b.getAttribute('title')).toBe('Show details of INV-001');
    expect(b.getAttribute('data-loading')).toBe('false');
    expect(b.hasAttribute('aria-busy')).toBe(false);
    expect(button(page, 1).getAttribute('aria-controls')).toBe(drawerId('INV-002'));
  });

  it('says it is open for the row whose key is expanded, relabels, and tints only that row', async () => {
    const page = await renderVisits({ actions: { expandedKey: 'INV-002', onToggle: () => undefined, keyOf: row => row.invoice } });
    expect(button(page, 0).getAttribute('aria-expanded')).toBe('false');
    expect(button(page, 1).getAttribute('aria-expanded')).toBe('true');
    expect(button(page, 1).getAttribute('aria-label')).toBe('Hide details of INV-002');
    expect(qa(page, '.lt-row').map(el => el.getAttribute('data-expanded'))).toEqual([null, 'true']);
  });

  it('spins while fetching: data-loading and aria-busy on that button alone, and the chevron and spinner both stay in it', async () => {
    const page = await renderVisits({ actions: { expandedKey: null, fetchingKey: 'INV-001', onToggle: () => undefined, keyOf: row => row.invoice } });
    expect(button(page, 0).getAttribute('data-loading')).toBe('true');
    expect(button(page, 0).getAttribute('aria-busy')).toBe('true');
    expect(button(page, 1).getAttribute('data-loading')).toBe('false');
    expect(button(page, 0).querySelector('.lt-action-icon')).not.toBeNull();
    expect(button(page, 0).querySelector('.lt-action-spinner').getAttribute('aria-hidden')).toBe('true');
    expect(button(page, 0).querySelector('svg').getAttribute('aria-hidden')).toBe('true');
  });

  it('reports a press with the row key, and is not pressable while a lookup is in flight', async () => {
    const onToggle = jest.fn();
    const page = await renderVisits({ actions: { expandedKey: null, onToggle, keyOf: row => row.invoice } });
    expect(button(page, 0).hasAttribute('disabled')).toBe(false);
    button(page, 1).click();
    expect(onToggle).toHaveBeenCalledWith('INV-002');

    const busy = await renderVisits({ busy: true, actions: { expandedKey: null, onToggle, keyOf: row => row.invoice } });
    expect(button(busy, 0).hasAttribute('disabled')).toBe(true);
  });

  it('keys by the vdom key when the panel gives no identity', async () => {
    const onToggle = jest.fn();
    const page = await renderVisits({ actions: { expandedKey: 'slot-1', onToggle } });
    expect(button(page, 1).getAttribute('aria-expanded')).toBe('true');
    expect(button(page, 1).getAttribute('aria-controls')).toBe(drawerId('slot-1'));
    button(page, 0).click();
    expect(onToggle).toHaveBeenCalledWith('slot-0');
  });

  it('falls back to the column heading as its name when the strings carry none, and to the bare name with no identity column', async () => {
    const bare = await renderVisits({ strings, actions: { expandedKey: null, onToggle: () => undefined } });
    expect(button(bare).getAttribute('aria-label')).toBe('Details INV-001');

    const noIdentity = await renderVisits({ columns: [V_DATE, V_DETAILS], actions: { expandedKey: null, onToggle: () => undefined } });
    expect(button(noIdentity).getAttribute('aria-label')).toBe('Show details of');
  });

  it('is an uncaptioned empty cell, not a button, when the table has no actions', async () => {
    const page = await renderVisits();
    expect(q(page, '.lt-action-button')).toBeNull();
    expect(q(page, '.lt-row > .lt-action')).not.toBeNull();
    expect(q(page, '.lt-row')?.hasAttribute('data-expanded')).toBe(false);
  });

  it('has a heading for assistive tech only: a glyph-only column keeps its name as .sr-only text', async () => {
    const page = await renderVisits();
    const heading = q(page, '.lt-columns > [data-kind="action"]');
    expect(heading.getAttribute('role')).toBe('columnheader');
    expect(heading.querySelector('.sr-only').textContent).toBe('Details');
  });

  it('is not under the skeleton (it is chrome, not a value) but keeps the fade wrapper', async () => {
    const page = await renderVisits({ actions: { expandedKey: null, onToggle: () => undefined } });
    const wrapper = q(page, '.lt-row > .lt-action > .lt-value');
    expect(wrapper.classList.contains('shift-skeleton')).toBe(false);
    expect(q(page, '.lt-row > .lt-date > .lt-value').classList.contains('shift-skeleton')).toBe(true);
  });

  it('keeps each button its own element when the panel opens a drawer: only the state attributes change', async () => {
    const model = visitBase({ actions: { expandedKey: null, onToggle: () => undefined, keyOf: row => row.invoice } });
    const page = await renderVisitHost(model);
    const before = button(page, 0);

    page.root.model = { ...model, actions: { ...model.actions, expandedKey: 'INV-001' } };
    await page.waitForChanges();
    expect(button(page, 0)).toBe(before);
    expect(before.getAttribute('aria-expanded')).toBe('true');

    page.root.model = { ...model, actions: { ...model.actions, expandedKey: 'INV-001', fetchingKey: 'INV-001' } };
    await page.waitForChanges();
    expect(button(page, 0)).toBe(before);
    expect(before.getAttribute('data-loading')).toBe('true');
  });
});

describe('density', () => {
  it('marks a compact grid, which the stylesheet turns into tokens', async () => {
    expect(q(await renderVisits({ density: 'compact' }), '.lt-grid').getAttribute('data-density')).toBe('compact');
  });

  it('leaves a comfortable grid exactly as it was: no attribute, whatever the default is spelled', async () => {
    expect(q(await renderVisits(), '.lt-grid').hasAttribute('data-density')).toBe(false);
    expect(q(await renderVisits({ density: 'comfortable' }), '.lt-grid').hasAttribute('data-density')).toBe(false);
  });

  it('adds nothing to a table of the older kinds: no label, density, expanded or embedded marker', async () => {
    const page = await render();
    const grid = q(page, '.lt-grid');
    for (const name of ['aria-label', 'data-density', 'data-embedded']) expect(grid.hasAttribute(name)).toBe(false);
    expect(qa(page, '[data-expanded]')).toEqual([]);
  });

  it('keeps the strip and the body in a compact table: density changes the rhythm, not the structure', async () => {
    const page = await renderVisits({ density: 'compact' });
    expect(q(page, '.lt-lead')).not.toBeNull();
    expect(q(page, '.lt-body.collapsible')).not.toBeNull();
    expect(qa(page, '.lt-slot').length).toBe(2);
  });
});

describe('label', () => {
  it('names the table while it is a table, and not while it is not', async () => {
    expect(q(await renderVisits({ label: 'Visits' }), '.lt-grid').getAttribute('aria-label')).toBe('Visits');
    expect(q(await renderVisits({ label: 'Visits', open: false }), '.lt-grid').hasAttribute('aria-label')).toBe(false);
  });
});

describe('embedded table', () => {
  const lines = [visit({ invoice: 'L-1', part: null }), visit({ invoice: 'L-2', part: null, odometer: null })];
  const embedded = (over: Partial<LookupTableProps<Visit>> = {}) =>
    renderVisits({ embedded: true, density: 'compact', label: 'Labor lines', columns: [V_INVOICE, V_DATE, V_ODOMETER, V_PART], rows: lines, ...over });

  it('is the headings as its first row and then the rows: no strip, no notice, no collapsible body, no slots', async () => {
    const page = await embedded();
    const grid = q(page, '.lt-grid');
    expect(grid.getAttribute('data-embedded')).toBe('true');
    expect(grid.getAttribute('data-density')).toBe('compact');
    expect(grid.getAttribute('data-stack')).toBe('false');

    for (const selector of ['.lt-lead', '.lt-notice', '.lt-body', '.lt-body-inner', '.lt-slot', '.lt-slot-body', '.collapsible', '.layer', '.lt-columns-label']) {
      expect(q(page, selector)).toBeNull();
    }

    expect(Array.from(grid.children).map(el => el.className)).toEqual(['lt-columns', 'lt-rows']);
    expect(qa(page, '.lt-rows > .lt-row').length).toBe(2);
  });

  it('carries the table roles whenever it has rows, and a name', async () => {
    const page = await embedded();
    expect(q(page, '.lt-grid').getAttribute('role')).toBe('table');
    expect(q(page, '.lt-grid').getAttribute('aria-label')).toBe('Labor lines');
    expect(q(page, '.lt-columns').getAttribute('role')).toBe('row');
    expect(q(page, '.lt-columns').hasAttribute('aria-hidden')).toBe(false);
    expect(qa(page, '.lt-columns > .lt-cell').every(el => el.getAttribute('role') === 'columnheader')).toBe(true);
    expect(q(page, '.lt-rows').getAttribute('role')).toBe('rowgroup');
    expect(qa(page, '.lt-row').every(el => el.getAttribute('role') === 'row')).toBe(true);
    expect(qa(page, '.lt-row > .lt-cell').every(el => el.getAttribute('role') === 'cell')).toBe(true);
  });

  it('drops a column that is empty on every row (decision 12) from the headings, the cells and the tracks', async () => {
    const page = await embedded();
    expect(qa(page, '.lt-columns > .lt-cell').map(el => el.textContent)).toEqual(['Invoice', 'Date', 'Odometer']);
    expect(qa(page, '.lt-row').every(row => row.querySelectorAll('.lt-cell').length === 3)).toBe(true);
    expect(qa(page, '[data-label="Part"]')).toEqual([]);
    expect(q(page, '.lt-grid').style.getPropertyValue('--lt-tracks')).toBe(tableTracks([V_INVOICE, V_DATE, V_ODOMETER]).wide);
  });

  it('keeps the column once any row has a value, and shows blank cells of the others as the dash', async () => {
    const page = await embedded({ rows: [lines[0], visit({ invoice: 'L-3', part: 'P-9', odometer: null })] });
    expect(qa(page, '.lt-columns > .lt-cell').map(el => el.textContent)).toEqual(['Invoice', 'Date', 'Odometer', 'Part']);
    expect(qa(page, '.lt-row > [data-label="Part"]').map(el => el.textContent)).toEqual(['—', 'P-9']);
  });

  it('renders the new kinds like the main table does', async () => {
    const page = await embedded();
    expect(qa(page, '.lt-row > .lt-date').map(el => el.textContent)).toEqual(['2025-03-04', '2025-03-04']);
    expect(qa(page, '.lt-row > .lt-number').map(el => el.textContent)).toEqual(['101,328', '—']);
  });

  it('is an empty grid with no role, no headings and no label for no rows, never a table of headings only', async () => {
    const page = await embedded({ rows: [] });
    const grid = q(page, '.lt-grid');
    expect(grid.children.length).toBe(0);
    expect(roleAttrs(page)).toEqual([]);
    expect(grid.hasAttribute('aria-label')).toBe(false);
  });

  it('ignores the strip props: a notice, a shut body and a lead do not touch it', async () => {
    const page = await embedded({ lead: 'notice', notice: { tone: 'negative', text: 'Failed' }, body: 'none', open: false, reshaping: true });
    expect(q(page, '.lt-notice')).toBeNull();
    expect(qa(page, '.lt-row').length).toBe(2);
    expect(q(page, '.lt-grid').getAttribute('role')).toBe('table');
  });

  it('survives the same column rules as the main table: a hidden field is a missing column', async () => {
    const page = await embedded({ columns: visibleColumns([V_INVOICE, V_DATE, V_ODOMETER], new Set(['date'])) });
    expect(qa(page, '.lt-columns > .lt-cell').map(el => el.textContent)).toEqual(['Invoice', 'Odometer']);
  });

  it('carries data-phase for the panel, and aria-busy while busy', async () => {
    const page = await embedded({ busy: true });
    expect(q(page, '.lt-grid').getAttribute('data-phase')).toBe('busy');
    expect(q(page, '.lt-grid').getAttribute('aria-busy')).toBe('true');
  });

  it('keeps distinct rows for a repeated key, like the main table', async () => {
    const page = await embedded({ rowKey: () => 'same' });
    expect(qa(page, '.lt-row').length).toBe(2);
  });

  it('is not the identity-only fallback: with nothing but the identity it still names its rows', async () => {
    const page = await embedded({ columns: [V_INVOICE, V_PART] });
    expect(qa(page, '.lt-columns > .lt-cell').map(el => el.textContent)).toEqual(['Invoice']);
    expect(qa(page, '.lt-row > .lt-code').map(el => el.textContent)).toEqual(['L-1', 'L-2']);
  });
});

// ---------------------------------------------------------------------------------------------
// The single-open row drawer (spec § 7, design language § 18): its pure derivations and its DOM.
// ---------------------------------------------------------------------------------------------

describe('drawer derivations', () => {
  const rows = [visit({ invoice: 'INV-1' }), visit({ invoice: 'INV-2' }), visit({ invoice: 'INV-3' })];
  const keyOf = (row: Visit, index: number) => row.invoice ?? `row-${index}`;

  describe('drawerAfterLookup', () => {
    it('keeps the drawer open when a row with the same key is among the new rows, wherever it now sits', () => {
      expect(drawerAfterLookup('INV-2', rows, keyOf)).toBe('INV-2');
      expect(drawerAfterLookup('INV-3', [rows[2], rows[0]], keyOf)).toBe('INV-3');
    });

    it('closes it when no new row has the key, including when there are no rows', () => {
      expect(drawerAfterLookup('INV-9', rows, keyOf)).toBeNull();
      expect(drawerAfterLookup('INV-1', [], keyOf)).toBeNull();
    });

    it('is asked about the rows shown, not the whole answer: a key that is in the answer but off the first page closes', () => {
      const answer = [...rows, visit({ invoice: 'INV-4' }), visit({ invoice: 'INV-5' })];
      const firstPage = answer.slice(0, 3);
      expect(drawerAfterLookup('INV-5', answer, keyOf)).toBe('INV-5');
      expect(drawerAfterLookup('INV-5', firstPage, keyOf)).toBeNull();
      expect(drawerAfterLookup('INV-2', firstPage, keyOf)).toBe('INV-2');
    });

    it('has nothing to keep when nothing was open', () => {
      expect(drawerAfterLookup(null, rows, keyOf)).toBeNull();
    });

    it('reads the identity through keyOf, which may fall back on the position', () => {
      const blank = [visit({ invoice: null }), visit({ invoice: 'INV-2' })];
      expect(drawerAfterLookup('row-0', blank, keyOf)).toBe('row-0');
    });
  });

  describe('drawerAfterPage', () => {
    it('closes the drawer when its row is not on the page that shows (paging away), keeps it while it is', () => {
      expect(drawerAfterPage('INV-1', ['INV-6', 'INV-7'])).toBeNull();
      expect(drawerAfterPage('INV-1', ['INV-1', 'INV-2'])).toBe('INV-1');
    });

    it('has nothing to keep when nothing was open', () => {
      expect(drawerAfterPage(null, ['INV-1'])).toBeNull();
    });
  });

  describe('closingDrawers', () => {
    const change = (over: Partial<Parameters<typeof closingDrawers<Visit>>[0]>) =>
      closingDrawers<Visit>({ closing: [], previousKey: null, nextKey: null, previousRows: rows, nextRows: rows, keyOf, ...over });

    it('keeps the drawer that closes, at its slot, with the row it was showing', () => {
      expect(change({ previousKey: 'INV-2', nextKey: null })).toEqual([{ index: 1, row: rows[1] }]);
    });

    it('keeps the one that gives way when another opens (single-open: both animate)', () => {
      expect(change({ previousKey: 'INV-1', nextKey: 'INV-3' })).toEqual([{ index: 0, row: rows[0] }]);
    });

    it('adds nothing when nothing was open, or the key is not in the rows it was open on', () => {
      expect(change({ previousKey: null, nextKey: 'INV-1' })).toEqual([]);
      expect(change({ previousKey: 'INV-9', nextKey: null })).toEqual([]);
    });

    it('adds nothing when the same row stays open in the same slot (a lookup that changed nothing)', () => {
      expect(change({ previousKey: 'INV-2', nextKey: 'INV-2' })).toEqual([]);
    });

    it('closes the old slot when the row kept its key but moved to another slot (the new one opens)', () => {
      const moved = [visit({ invoice: 'INV-0' }), ...rows];
      expect(change({ previousKey: 'INV-2', nextKey: 'INV-2', nextRows: moved })).toEqual([{ index: 1, row: rows[1] }]);
    });

    it('closes the old slot when the row is gone from the new rows (the drop)', () => {
      const other = [visit({ invoice: 'INV-7' }), visit({ invoice: 'INV-8' })];
      expect(change({ previousKey: 'INV-2', nextKey: drawerAfterLookup('INV-2', other, keyOf), nextRows: other })).toEqual([{ index: 1, row: rows[1] }]);
    });

    it('keeps an earlier one that is still shutting, so A to B to C within a settle lets both go', () => {
      const first = change({ previousKey: 'INV-1', nextKey: 'INV-2' });
      const second = change({ closing: first, previousKey: 'INV-2', nextKey: 'INV-3' });
      expect(second.map(entry => entry.index)).toEqual([0, 1]);
    });

    it('replaces the entry still shutting at a slot with the drawer that closes there now (reopened and closed within one settle)', () => {
      const old = visit({ invoice: 'INV-OLD' });
      const closing: ClosingDrawer<Visit>[] = [{ index: 1, row: old }];
      // After a lookup slot 1 holds INV-2; it was opened and is now closed again before the old entry was cleared.
      const next = change({ closing, previousKey: 'INV-2', nextKey: null });
      expect(next).toEqual([{ index: 1, row: rows[1] }]);
      expect(next).toHaveLength(1);
    });

    it('drops an entry that is opened again, and does not list a slot twice', () => {
      const closing: ClosingDrawer<Visit>[] = [{ index: 0, row: rows[0] }];
      expect(change({ closing, previousKey: null, nextKey: 'INV-1' })).toEqual([]);
      expect(change({ closing, previousKey: 'INV-1', nextKey: null })).toEqual(closing);
    });
  });

  describe('drawerButtonId', () => {
    it('is the drawer id with a suffix, so it is valid and stable for any key', () => {
      expect(drawerButtonId('INV 1/2')).toBe(`${drawerId('INV 1/2')}-button`);
    });
  });

  describe('shouldToggleOnRowClick', () => {
    const inside = { contains: () => true } as unknown as Element;
    const targetWith = (interactive: Element | null) => ({ closest: () => interactive }) as unknown as Element;
    const toggles = (over: Partial<Parameters<typeof shouldToggleOnRowClick>[0]> = {}) =>
      shouldToggleOnRowClick({ target: targetWith(null), row: inside, selectedText: '', enabled: true, ...over });

    it('toggles on a click on plain row content', () => {
      expect(toggles()).toBe(true);
    });

    it('ignores a click on something interactive inside the row (a link, the media button, a field)', () => {
      expect(toggles({ target: targetWith({} as Element) })).toBe(false);
    });

    it('does not mind an interactive ancestor that is outside the row (a host that is itself focusable)', () => {
      expect(toggles({ target: targetWith({} as Element), row: { contains: () => false } as unknown as Element })).toBe(true);
    });

    it('ignores a click that ends a text selection', () => {
      expect(toggles({ selectedText: 'INV-100482' })).toBe(false);
    });

    it('ignores a click while the table is busy, and one with no target or no row', () => {
      expect(toggles({ enabled: false })).toBe(false);
      expect(toggles({ target: null })).toBe(false);
      expect(toggles({ row: null })).toBe(false);
    });
  });

  describe('revealDrawer', () => {
    const rowStub = { name: 'row' } as unknown as Element;
    const drawerStub = { closest: () => ({ querySelector: () => rowStub }) } as unknown as Element;

    it('finds the drawer by its id and follows its row, handing back the way to let go', () => {
      const letGo = jest.fn();
      const reveal = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => letGo);
      const querySelector = jest.fn(() => drawerStub);

      try {
        const stop = revealDrawer({ querySelector } as unknown as ParentNode, 'INV 1', 560);
        expect(querySelector).toHaveBeenCalledWith(`#${drawerId('INV 1')}`);
        expect(reveal).toHaveBeenCalledWith(rowStub, 560);
        stop();
        expect(letGo).toHaveBeenCalledTimes(1);
      } finally {
        reveal.mockRestore();
      }
    });

    it('follows the drawer itself when its row cannot be found', () => {
      const reveal = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => () => undefined);
      const bare = { closest: () => null } as unknown as Element;

      try {
        revealDrawer({ querySelector: () => bare } as unknown as ParentNode, 'k', 100);
        expect(reveal).toHaveBeenCalledWith(bare, 100);
      } finally {
        reveal.mockRestore();
      }
    });

    it('does nothing for a key with no drawer in the tree', () => {
      const reveal = jest.spyOn(revealBelowModule, 'revealBelow');

      try {
        revealDrawer({ querySelector: () => null } as unknown as ParentNode, 'gone', 100)();
        expect(reveal).not.toHaveBeenCalled();
      } finally {
        reveal.mockRestore();
      }
    });
  });
});

describe('drawer', () => {
  const drawerRows = [visit({ invoice: 'INV-001' }), visit({ invoice: 'INV-002' }), visit({ invoice: 'INV-003' })];
  const keyOf = (row: Visit) => row.invoice;
  const toggle = jest.fn();

  const withDrawer = (over: Partial<LookupTableProps<Visit>> = {}, actions: Partial<NonNullable<LookupTableProps<Visit>['actions']>> = {}) =>
    visitBase({
      rows: drawerRows,
      actions: { expandedKey: null, onToggle: toggle, keyOf, ...actions },
      drawer: (row, key) => (
        <div class="stand-in" data-for={key}>
          {row.invoice} detail
        </div>
      ),
      ...over,
    });

  const drawerOf = (page: Page, index: number) => qa(page, '.lt-drawer')[index];
  const drawerRowOf = (page: Page, index: number) => qa(page, '.lt-drawer-row')[index];
  const content = (page: Page) => qa(page, '.stand-in').map(el => el.getAttribute('data-for'));

  beforeEach(() => toggle.mockClear());

  it('adds no drawer, no marker and no click behaviour to a table that was not given one', async () => {
    const page = await renderVisits({ actions: { expandedKey: 'INV-001', onToggle: toggle, keyOf } });
    expect(qa(page, '.lt-drawer-row, .lt-drawer')).toEqual([]);
    expect(q(page, '.lt-row')?.hasAttribute('data-clickable')).toBe(false);
    q(page, '.lt-row').click();
    expect(toggle).not.toHaveBeenCalled();
  });

  it('puts one drawer in each row slot, under the row, all shut: aria-hidden, inert, empty', async () => {
    const page = await renderVisitHost(withDrawer());
    expect(qa(page, '.lt-slot').every(slot => slot.querySelector('.lt-slot-body > .lt-row + .lt-drawer-row'))).toBe(true);
    expect(qa(page, '.lt-drawer').length).toBe(3);

    for (let index = 0; index < 3; index++) {
      expect(drawerOf(page, index).getAttribute('data-open')).toBe('false');
      expect(drawerOf(page, index).classList.contains('collapsible')).toBe(true);
      expect(drawerRowOf(page, index).getAttribute('aria-hidden')).toBe('true');
      expect(drawerRowOf(page, index).hasAttribute('inert')).toBe(true);
    }
    expect(content(page)).toEqual([]);
  });

  it('wires the button and the drawer to each other: aria-controls is the drawer id, the region is named by the button', async () => {
    const page = await renderVisitHost(withDrawer());

    qa(page, '.lt-action-button').forEach((button, index) => {
      const drawer = drawerOf(page, index);
      expect(button.getAttribute('aria-controls')).toBe(drawer.id);
      expect(drawer.id).toBe(drawerId(drawerRows[index].invoice));
      expect(button.id).toBe(drawerButtonId(drawerRows[index].invoice));
      expect(drawer.getAttribute('role')).toBe('region');
      expect(drawer.getAttribute('aria-labelledby')).toBe(button.id);
      expect(drawer.hasAttribute('aria-label')).toBe(false);
    });
  });

  it('names the region by the row identity when the action column is hidden, so there is no dangling reference', async () => {
    const page = await renderVisitHost(withDrawer({ columns: [V_INVOICE, V_DATE] }));
    const drawer = drawerOf(page, 0);
    expect(drawer.hasAttribute('aria-labelledby')).toBe(false);
    expect(drawer.getAttribute('aria-label')).toBe('INV-001');
  });

  it('opens the row whose key is expanded: the drawer is open, visible to assistive tech, tinted, and holds the content', async () => {
    const page = await renderVisitHost(withDrawer({}, { expandedKey: 'INV-002' }));

    expect(drawerOf(page, 1).getAttribute('data-open')).toBe('true');
    expect(drawerRowOf(page, 1).getAttribute('data-open')).toBe('true');
    expect(drawerRowOf(page, 1).hasAttribute('aria-hidden')).toBe(false);
    expect(drawerRowOf(page, 1).hasAttribute('inert')).toBe(false);
    expect(qa(page, '.lt-row')[1].getAttribute('data-expanded')).toBe('true');
    expect(qa(page, '.lt-action-button')[1].getAttribute('aria-expanded')).toBe('true');
    expect(content(page)).toEqual(['INV-002']);
    expect(drawerOf(page, 1).querySelector('.lt-drawer-panel .lt-drawer-content.shift-skeleton .stand-in')).not.toBeNull();
    expect(drawerOf(page, 0).getAttribute('data-open')).toBe('false');
  });

  it('is single-open: moving the key shuts the first drawer and opens the second, and the same elements animate', async () => {
    const model = withDrawer({}, { expandedKey: 'INV-001' });
    const page = await renderVisitHost(model);
    const first = drawerOf(page, 0);
    const second = drawerOf(page, 1);
    expect([first.getAttribute('data-open'), second.getAttribute('data-open')]).toEqual(['true', 'false']);

    page.root.model = {
      ...model,
      actions: {
        ...model.actions,
        expandedKey: 'INV-002',
        closing: closingDrawers({ closing: [], previousKey: 'INV-001', nextKey: 'INV-002', previousRows: drawerRows, nextRows: drawerRows, keyOf }),
      },
    };
    await page.waitForChanges();

    expect(drawerOf(page, 0)).toBe(first);
    expect(drawerOf(page, 1)).toBe(second);
    expect([first.getAttribute('data-open'), second.getAttribute('data-open')]).toEqual(['false', 'true']);
    // The one that is shutting still shows its content (it slides away, it does not blink out), and the one opening has it.
    expect(content(page)).toEqual(['INV-001', 'INV-002']);
    expect(
      qa(page, '.lt-action-button')
        .map(button => button.getAttribute('aria-expanded'))
        .slice(0, 2),
    ).toEqual(['false', 'true']);

    // The owner clears the list a settle later: the shut one drops its content, nothing else changes.
    page.root.model = { ...model, actions: { ...model.actions, expandedKey: 'INV-002', closing: [] } };
    await page.waitForChanges();
    expect(content(page)).toEqual(['INV-002']);
    expect(drawerOf(page, 0)).toBe(first);
  });

  it('builds the content only for the open and the closing rows, never for a shut one', async () => {
    const drawer = jest.fn((row: Visit, key: string) => <span data-key={key}>{row.invoice}</span>);
    await renderVisitHost(withDrawer({ drawer }, { expandedKey: 'INV-003', closing: [{ index: 0, row: drawerRows[0] }] }));
    expect(drawer.mock.calls.map(call => call[0].invoice).sort()).toEqual(['INV-001', 'INV-003']);
    expect(drawer.mock.calls.every(call => typeof call[1] === 'string')).toBe(true);
  });

  it('a drawer that returns nothing yet is an open, empty region, not a crash', async () => {
    const page = await renderVisitHost(withDrawer({ drawer: () => null }, { expandedKey: 'INV-001' }));
    expect(drawerOf(page, 0).getAttribute('data-open')).toBe('true');
    expect(drawerOf(page, 0).querySelector('.lt-drawer-content').children.length).toBe(0);
  });

  describe('whole-row click', () => {
    it('toggles the row it is on, by its key', async () => {
      const page = await renderVisitHost(withDrawer());
      qa(page, '.lt-row')[1].click();
      expect(toggle).toHaveBeenCalledTimes(1);
      expect(toggle).toHaveBeenCalledWith('INV-002');
    });

    it('is pointer-only: the row is not a tab stop and carries no role of its own beyond the table row', async () => {
      const page = await renderVisitHost(withDrawer());
      const row = q(page, '.lt-row');
      expect(row.hasAttribute('tabindex')).toBe(false);
      expect(row.getAttribute('data-clickable')).toBe('true');
    });

    it('counts a press on the action button once, not twice (the button is interactive, so the row steps aside)', async () => {
      const page = await renderVisitHost(withDrawer());
      qa(page, '.lt-action-button')[0].click();
      expect(toggle).toHaveBeenCalledTimes(1);
    });

    it('ignores a click that ends a text selection', async () => {
      const page = await renderVisitHost(withDrawer());
      const original = window.getSelection;
      (window as unknown as { getSelection: () => { toString: () => string } }).getSelection = () => ({ toString: () => 'INV-002' });

      try {
        qa(page, '.lt-row')[1].click();
        expect(toggle).not.toHaveBeenCalled();
      } finally {
        window.getSelection = original;
      }
    });

    it('does not fire from the drawer: a click inside the open content is not a click on the row', async () => {
      const page = await renderVisitHost(withDrawer({}, { expandedKey: 'INV-001' }));
      q(page, '.stand-in').click();
      expect(toggle).not.toHaveBeenCalled();
    });

    it('is off while the table is busy, like the button, and says so (no pointer cursor hook)', async () => {
      const page = await renderVisitHost(withDrawer({ busy: true }));
      expect(q(page, '.lt-row').hasAttribute('data-clickable')).toBe(false);
      q(page, '.lt-row').click();
      expect(toggle).not.toHaveBeenCalled();
    });
  });

  describe('through a lookup (run decision 2)', () => {
    const covered: RowSlot[] = drawerRows.map(() => ({ open: true, veil: 'cover' }));

    it('stays open while the table is busy, veiled with its row, and its content is not unmounted', async () => {
      const page = await renderVisitHost(withDrawer({ busy: true, slots: covered }, { expandedKey: 'INV-002' }));

      expect(drawerOf(page, 1).getAttribute('data-open')).toBe('true');
      expect(content(page)).toEqual(['INV-002']);
      // The panel's `.loading` is the switch for the content's skeleton; a covered slot carries its own for a row that leaves.
      expect(drawerRowOf(page, 1).classList.contains('loading')).toBe(true);
      expect(qa(page, '.lt-row')[1].classList.contains('loading')).toBe(true);
      expect(drawerOf(page, 1).querySelector('.lt-drawer-content').classList.contains('shift-skeleton')).toBe(true);
    });

    it('puts only a drawer that has content under the cover: a shut drawer shell is not .loading, so no hidden sweep runs on every row', async () => {
      const page = await renderVisitHost(withDrawer({ busy: true, slots: covered }, { expandedKey: 'INV-002' }));
      expect(qa(page, '.lt-drawer-row').map(row => row.classList.contains('loading'))).toEqual([false, true, false]);

      const none = await renderVisitHost(withDrawer({ busy: true, slots: covered }, { expandedKey: null }));
      expect(qa(none, '.lt-drawer-row').some(row => row.classList.contains('loading'))).toBe(false);
    });

    it('keeps a closing drawer (content still mounted while it shuts) under the cover with its row', async () => {
      const closing = closingDrawers({ closing: [], previousKey: 'INV-001', nextKey: null, previousRows: drawerRows, nextRows: drawerRows, keyOf });
      const page = await renderVisitHost(withDrawer({ busy: true, slots: covered }, { expandedKey: null, closing }));
      expect(qa(page, '.lt-drawer-row').map(row => row.classList.contains('loading'))).toEqual([true, false, false]);
    });

    it('is not veiled once the slots are not: the cover lifts on the drawer with its row', async () => {
      const page = await renderVisitHost(withDrawer({}, { expandedKey: 'INV-002' }));
      expect(drawerRowOf(page, 1).classList.contains('loading')).toBe(false);
    });

    it('keeps it open when the row is among the new rows (here it moved), shutting the old slot and opening the new one', async () => {
      const model = withDrawer({ busy: true, slots: covered }, { expandedKey: 'INV-002' });
      const page = await renderVisitHost(model);

      const arrived = [visit({ invoice: 'INV-000' }), drawerRows[0], drawerRows[1]];
      const next = drawerAfterLookup('INV-002', arrived, keyOf);
      expect(next).toBe('INV-002');

      page.root.model = {
        ...model,
        busy: false,
        rows: arrived,
        slots: undefined,
        actions: {
          ...model.actions,
          expandedKey: next,
          closing: closingDrawers({ closing: [], previousKey: 'INV-002', nextKey: next, previousRows: drawerRows, nextRows: arrived, keyOf }),
        },
      };
      await page.waitForChanges();

      expect(drawerOf(page, 2).getAttribute('data-open')).toBe('true');
      expect(drawerOf(page, 1).getAttribute('data-open')).toBe('false');
      // The old slot still shows what it was showing while it shuts; the new slot shows the row that is there now.
      expect(content(page)).toEqual(['INV-002', 'INV-002']);
      expect(qa(page, '.lt-action-button')[2].getAttribute('aria-expanded')).toBe('true');
    });

    it('slides it shut when no new row has the key: the drawer closes with its old content, under the new rows', async () => {
      const model = withDrawer({ busy: true, slots: covered }, { expandedKey: 'INV-002' });
      const page = await renderVisitHost(model);

      const arrived = [visit({ invoice: 'INV-010' }), visit({ invoice: 'INV-011' }), visit({ invoice: 'INV-012' })];
      const next = drawerAfterLookup('INV-002', arrived, keyOf);
      expect(next).toBeNull();

      page.root.model = {
        ...model,
        busy: false,
        rows: arrived,
        slots: undefined,
        actions: {
          ...model.actions,
          expandedKey: next,
          closing: closingDrawers({ closing: [], previousKey: 'INV-002', nextKey: next, previousRows: drawerRows, nextRows: arrived, keyOf }),
        },
      };
      await page.waitForChanges();

      expect(qa(page, '.lt-drawer').map(drawer => drawer.getAttribute('data-open'))).toEqual(['false', 'false', 'false']);
      expect(content(page)).toEqual(['INV-002']);

      page.root.model = { ...page.root.model, actions: { ...model.actions, expandedKey: null, closing: [] } };
      await page.waitForChanges();
      expect(content(page)).toEqual([]);
    });
  });

  describe('the key of a row (actions.keyOf)', () => {
    it('warns once per render, naming the key, when two rows share one', async () => {
      const warn = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
      try {
        const twins = [visit({ invoice: 'INV-001' }), visit({ invoice: 'INV-001' }), visit({ invoice: 'INV-001' }), visit({ invoice: 'INV-002' }), visit({ invoice: 'INV-002' })];
        await renderVisitHost(withDrawer({ rows: twins }));

        expect(warn).toHaveBeenCalledTimes(1);
        const message = String(warn.mock.calls[0][0]);
        expect(message).toContain('"INV-001"');
        expect(message).toContain('"INV-002"');
        expect(message).toContain('keyOf');
      } finally {
        warn.mockRestore();
      }
    });

    it('is silent when every key is unique, and when the table has no keyOf of its own', async () => {
      const warn = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
      try {
        await renderVisitHost(withDrawer());
        await renderVisits({ rows: [visit({ invoice: 'INV-001' }), visit({ invoice: 'INV-001' })], actions: { expandedKey: null, onToggle: toggle } });
        expect(warn).not.toHaveBeenCalled();
      } finally {
        warn.mockRestore();
      }
    });
  });

  describe('paging away (run decision 9)', () => {
    it('closes the drawer with the row: the open key is cleared by drawerAfterPage and the old slot keeps its content while it shuts', async () => {
      const model = withDrawer({}, { expandedKey: 'INV-001' });
      const page = await renderVisitHost(model);
      const nextPage = [visit({ invoice: 'INV-051' }), visit({ invoice: 'INV-052' })];
      const next = drawerAfterPage('INV-001', nextPage.map(keyOf));
      expect(next).toBeNull();

      page.root.model = {
        ...model,
        actions: {
          ...model.actions,
          expandedKey: next,
          closing: closingDrawers({ closing: [], previousKey: 'INV-001', nextKey: next, previousRows: drawerRows, nextRows: drawerRows, keyOf }),
        },
        slots: drawerRows.map(() => ({ open: true, veil: 'fade' as const })),
      };
      await page.waitForChanges();

      expect(drawerOf(page, 0).getAttribute('data-open')).toBe('false');
      expect(content(page)).toEqual(['INV-001']);
      expect(qa(page, '.lt-slot').every(slot => slot.getAttribute('data-veil') === 'fade')).toBe(true);

      // Back on the first page the drawer is shut: nothing was kept.
      page.root.model = { ...model, actions: { ...model.actions, expandedKey: null, closing: [] } };
      await page.waitForChanges();
      expect(qa(page, '.lt-drawer').every(drawer => drawer.getAttribute('data-open') === 'false')).toBe(true);
    });
  });

  describe('an embedded compact table in the drawer', () => {
    const laneRows = [visit({ invoice: 'L-1', part: null, odometer: 5 }), visit({ invoice: 'L-2', part: null, odometer: 7 })];
    const inner = () => (
      <LookupTable {...visitBase({ columns: [V_INVOICE, V_ODOMETER, V_PART], rows: laneRows, embedded: true, density: 'compact', label: 'Lines', actions: undefined })} />
    );

    it('sits inside the drawer content, keeps its own roles and drops a column that is empty on every line', async () => {
      const page = await renderVisitHost(withDrawer({ drawer: inner }, { expandedKey: 'INV-001' }));
      const embedded = q(page, '.lt-drawer .lt-drawer-content .lt-grid[data-embedded="true"]');

      expect(embedded).not.toBeNull();
      expect(embedded.getAttribute('data-density')).toBe('compact');
      expect(embedded.getAttribute('aria-label')).toBe('Lines');
      expect(Array.from(embedded.querySelectorAll('.lt-columns > .lt-cell')).map(cell => cell.textContent)).toEqual(['Invoice', 'Odometer']);
      // Two tables on the page, each with its own table role.
      expect(qa(page, '[role="table"]').length).toBe(2);
    });

    it('has no skeleton of its own: the drawer content is the one block the cover goes over', async () => {
      const page = await renderVisitHost(withDrawer({ drawer: inner }, { expandedKey: 'INV-001' }));
      expect(qa(page, '.lt-drawer .lt-grid[data-embedded="true"] .shift-skeleton')).toEqual([]);
      expect(qa(page, '.lt-drawer .lt-drawer-content.shift-skeleton .lt-grid[data-embedded="true"]').length).toBe(1);
    });
  });

  describe('roles', () => {
    it('a drawer is a row of one cell spanning the columns while it is there, and carries no role without a table', async () => {
      const page = await renderVisitHost(withDrawer({}, { expandedKey: 'INV-001' }));
      expect(drawerRowOf(page, 0).getAttribute('role')).toBe('row');
      const cell = q(page, '.lt-drawer-row > .lt-drawer-cell');
      expect(cell.getAttribute('role')).toBe('cell');
      expect(cell.getAttribute('aria-colspan')).toBe('5');

      const shut = await renderVisitHost(withDrawer({ open: false }, { expandedKey: 'INV-001' }));
      expect(qa(shut, '.lt-drawer-row, .lt-drawer-cell').every(el => !el.hasAttribute('role'))).toBe(true);
    });
  });
});

// ---------------------------------------------------------------------------------------------
// Scope of the table's own rules: an embedded table sits inside a drawer, which is inside the table's
// own grid, so a descendant selector from the grid reaches its rows. The rules that are for the
// table's own rows and headings are anchored with child combinators instead.
// ---------------------------------------------------------------------------------------------

describe('placeholder', () => {
  const GLYPHS: Record<PlaceholderKind, string> = { idle: 'M1 1h2v2z', empty: 'M2 2h2v2z', error: 'M3 3h2v2z', unauthorized: 'M4 4h2v2z' };
  const TEXT: Record<PlaceholderKind, string> = {
    idle: 'Look up a vehicle to see its items.',
    empty: 'No items on file for this vehicle.',
    error: "Couldn't load the items.",
    unauthorized: 'This vehicle is not in the distributor\u2019s records.',
  };
  const content = Object.fromEntries((Object.keys(TEXT) as PlaceholderKind[]).map(kind => [kind, { glyph: GLYPHS[kind], text: TEXT[kind] }])) as TablePlaceholder['content'];

  const withPlaceholder = (kind: PlaceholderKind | null, over: Partial<LookupTableProps<Item>> = {}, retained?: PlaceholderKind | null) =>
    render({ rows: [], body: 'none', open: false, placeholder: { kind, retained, content }, ...over });

  const layer = (page: Page, kind: PlaceholderKind) => q(page, `.lt-placeholder-layer.is-${kind}`);
  const active = (page: Page) => qa(page, '.lt-placeholder-layer').filter(el => el.getAttribute('data-active') === 'true');

  it('is not rendered by a table that has no placeholder prop, and then the body keeps its floor', async () => {
    const page = await render({ rows: [], body: 'none', open: false });
    expect(q(page, '.lt-placeholder')).toBeNull();
    expect(q(page, '.lt-grid').getAttribute('data-placeholder')).toBeNull();
  });

  it('is always mounted for a panel that has one: four layers, one per outcome, in every state', async () => {
    for (const kind of [null, 'idle', 'empty', 'error', 'unauthorized'] as const) {
      const page = await withPlaceholder(kind);
      expect(qa(page, '.lt-placeholder-layer').map(el => el.getAttribute('data-kind'))).toEqual(['idle', 'empty', 'error', 'unauthorized']);
      expect(q(page, '.lt-grid').getAttribute('data-placeholder')).toBe('true');
    }
  });

  it.each(['idle', 'empty', 'error', 'unauthorized'] as const)('says %s: its region is open, only its layer is active, with its own glyph and its line', async kind => {
    const page = await withPlaceholder(kind);
    expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('true');
    expect(q(page, '.lt-placeholder').getAttribute('data-kind')).toBe(kind);
    expect(active(page).map(el => el.getAttribute('data-kind'))).toEqual([kind]);
    expect(layer(page, kind).classList.contains(`is-${kind}`)).toBe(true);
    expect(layer(page, kind).querySelector('.lt-placeholder-text').textContent).toBe(TEXT[kind]);
    expect(layer(page, kind).querySelector('.lt-placeholder-glyph path').getAttribute('d')).toBe(GLYPHS[kind]);
  });

  it("keeps every outcome's words and glyph mounted, so a cross-fade has both ends", async () => {
    const page = await withPlaceholder('idle');
    for (const kind of ['idle', 'empty', 'error', 'unauthorized'] as const) {
      expect(layer(page, kind).querySelector('.lt-placeholder-text').textContent).toBe(TEXT[kind]);
      expect(layer(page, kind).querySelector('.lt-placeholder-glyph path').getAttribute('d')).toBe(GLYPHS[kind]);
    }
  });

  it('draws the icon as decoration: aria-hidden, not focusable', async () => {
    const page = await withPlaceholder('error');
    const icon = q(page, '.lt-placeholder-layer.is-error svg');
    expect(icon.getAttribute('aria-hidden')).toBe('true');
    expect(icon.getAttribute('focusable')).toBe('false');
    expect(icon.getAttribute('viewBox')).toBe('0 0 512 512');
  });

  describe('announced once per outcome', () => {
    it("exposes one status, the active layer's, and hides the other three from assistive technology", async () => {
      for (const kind of ['idle', 'empty', 'error', 'unauthorized'] as const) {
        const page = await withPlaceholder(kind);
        const statuses = qa(page, '.lt-placeholder [role="status"]');
        expect(statuses.length).toBe(4);
        const exposed = statuses.filter(el => el.getAttribute('aria-hidden') === null);
        expect(exposed.map(el => el.getAttribute('data-kind'))).toEqual([kind]);
        expect(exposed[0].textContent).toBe(TEXT[kind]);
      }
    });

    it("does not repeat the outcome in the strip: the headings stay (lead columns) and the strip's notice is hidden", async () => {
      const page = await withPlaceholder('empty', { lead: 'columns', notice: { tone: 'idle', text: 'Said in the strip' } });
      expect(q(page, '.lt-notice').getAttribute('aria-hidden')).toBe('true');
      expect(q(page, '.lt-notice').getAttribute('data-active')).toBe('false');
      // The only exposed status in the whole table is the placeholder's.
      const exposed = qa(page, '[role="status"]').filter(el => el.getAttribute('aria-hidden') === null);
      expect(exposed.length).toBe(1);
      expect(exposed[0].classList.contains('lt-placeholder-layer')).toBe(true);
    });

    it('is out of the accessibility tree with its region when there is no outcome to say', async () => {
      const page = await withPlaceholder(null);
      expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('false');
      expect(q(page, '.lt-placeholder').getAttribute('aria-hidden')).toBe('true');
      expect(active(page)).toEqual([]);
    });
  });

  describe("every state pair (the cross-fade is the layers' data-active, on one clock)", () => {
    const settle = async (from: PlaceholderKind | null, to: PlaceholderKind | null, retained?: PlaceholderKind | null) => {
      const model = base({ rows: [], body: 'none', open: false, placeholder: { kind: from, content } });
      const page = await renderHost(model);
      expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe(from === null ? 'false' : 'true');
      page.root.model = { ...model, placeholder: { kind: to, retained, content } };
      await page.waitForChanges();
      return page;
    };

    const kinds = ['idle', 'empty', 'error', 'unauthorized'] as const;

    for (const from of kinds)
      for (const to of kinds)
        if (from !== to)
          it(`${from} -> ${to}: one region stays open, the active layer moves, and nothing mounts or unmounts`, async () => {
            const page = await settle(from, to);
            expect(qa(page, '.lt-placeholder').length).toBe(1);
            expect(qa(page, '.lt-placeholder-layer').length).toBe(4);
            expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('true');
            expect(active(page).map(el => el.getAttribute('data-kind'))).toEqual([to]);
          });

    it('idle -> rows (a lookup landed with records): the region slides shut and the outgoing words stay mounted and active while it does', async () => {
      const page = await settle('idle', null, 'idle');
      expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('false');
      expect(active(page).map(el => el.getAttribute('data-kind'))).toEqual(['idle']);
      expect(layer(page, 'idle').querySelector('.lt-placeholder-text').textContent).toBe(TEXT.idle);
    });

    it('once the owner lets the retained kind go, nothing is active under the shut region, so the next outcome fades in alone', async () => {
      const page = await settle('error', null, null);
      expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('false');
      expect(active(page)).toEqual([]);
    });

    it('rows -> empty: the region opens on the empty layer alone', async () => {
      const page = await settle(null, 'empty');
      expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('true');
      expect(active(page).map(el => el.getAttribute('data-kind'))).toEqual(['empty']);
    });
  });

  describe('busy over a placeholder (a lookup in flight)', () => {
    it.each(['idle', 'empty', 'error', 'unauthorized'] as const)(
      'keeps the %s placeholder where it stands, its icon and words each on the skeleton class (the cover itself comes from a .loading ancestor), and the region aria-busy',
      async kind => {
        const page = await withPlaceholder(kind, { busy: true });
        expect(q(page, '.lt-grid').getAttribute('data-phase')).toBe('busy');
        expect(q(page, '.lt-placeholder').getAttribute('aria-busy')).toBe('true');
        expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('true');
        expect(active(page).map(el => el.getAttribute('data-kind'))).toEqual([kind]);
        expect(layer(page, kind).querySelector('.lt-placeholder-mark').classList.contains('shift-skeleton')).toBe(true);
        expect(layer(page, kind).querySelector('.lt-placeholder-text').classList.contains('shift-skeleton')).toBe(true);
      },
    );

    it('aria-busy on the region follows busy: set in flight, gone once settled', async () => {
      const model = base({ rows: [], body: 'none', open: false, placeholder: { kind: 'empty', content } });
      const page = await renderHost(model);
      expect(q(page, '.lt-placeholder').hasAttribute('aria-busy')).toBe(false);
      page.root.model = { ...model, busy: true };
      await page.waitForChanges();
      expect(q(page, '.lt-placeholder').getAttribute('aria-busy')).toBe('true');
      page.root.model = { ...model, busy: false };
      await page.waitForChanges();
      expect(q(page, '.lt-placeholder').hasAttribute('aria-busy')).toBe(false);
    });

    it('puts the skeleton on the icon and the words only, never on the region or the layers (chrome)', async () => {
      const page = await withPlaceholder('empty', { busy: true });
      expect(
        qa(page, '.shift-skeleton')
          .filter(el => el.closest('.lt-placeholder'))
          .every(el => /lt-placeholder-(mark|text)/.test(el.className)),
      ).toBe(true);
      expect(q(page, '.lt-placeholder').classList.contains('shift-skeleton')).toBe(false);
      expect(layer(page, 'empty').classList.contains('shift-skeleton')).toBe(false);
    });

    it('is the same region before, during and after: idle -> busy -> empty changes only the active layer', async () => {
      const model = base({ rows: [], body: 'none', open: false, placeholder: { kind: 'idle', content } });
      const page = await renderHost(model);
      const region = q(page, '.lt-placeholder');

      page.root.model = { ...model, busy: true };
      await page.waitForChanges();
      expect(q(page, '.lt-placeholder')).toBe(region);
      expect(active(page).map(el => el.getAttribute('data-kind'))).toEqual(['idle']);

      page.root.model = { ...model, busy: false, placeholder: { kind: 'empty', content } };
      await page.waitForChanges();
      expect(q(page, '.lt-placeholder')).toBe(region);
      expect(active(page).map(el => el.getAttribute('data-kind'))).toEqual(['empty']);
    });
  });

  it('is under the rows of a body that holds them only as a region that is shut', async () => {
    const page = await render({ placeholder: { kind: null, content } });
    expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('false');
    expect(qa(page, '.lt-row').length).toBe(2);
    expect(q(page, '.lt-body').getAttribute('data-open')).toBe('true');
  });

  it('is not drawn by an embedded table', async () => {
    const page = await render({ embedded: true, placeholder: { kind: 'idle', content } });
    expect(q(page, '.lt-placeholder')).toBeNull();
  });

  describe('stylesheet', () => {
    const css = readFileSync(path.resolve(__dirname, 'lookup-table.css'), 'utf8');
    const section = css.slice(css.indexOf('Placeholder: what the body says'), css.indexOf('Rows\n   ---'));

    it('colours the four outcomes by role: grey (muted) for idle and empty, amber for not-in-records, red for a failed lookup', () => {
      const rule = (selector: string) => section.slice(section.indexOf(selector)).split('}')[0];
      expect(rule('.lt-placeholder-layer.is-idle,')).toContain('--muted');
      expect(rule('.lt-placeholder-layer.is-unauthorized {')).toContain('--amber');
      expect(rule('.lt-placeholder-layer.is-error {')).toContain('--red');
    });

    it('uses tokens only: no hex colour in its rules', () => {
      const code = section.replace(/\/\*[\s\S]*?\*\//g, '');
      expect(code).not.toMatch(/#[0-9a-fA-F]{3,8}\b/);
    });

    it("has no transition of its own: it moves on the collapsible and layer primitives, and the skeleton's file", () => {
      expect(section.replace(/\/\*[\s\S]*?\*\//g, '')).not.toMatch(/transition|animation/);
    });
  });
});

describe('lookup-table.css scope (the embedded tables in a drawer are not the table)', () => {
  const css = readFileSync(path.resolve(__dirname, 'lookup-table.css'), 'utf8').replace(/\/\*[\s\S]*?\*\//g, '');
  const selectors = Array.from(css.matchAll(/([^{}]+)\{/g)).flatMap(match => match[1].split(',').map(selector => selector.trim()));

  it("anchors every stacked-layout rule down the table's own structure, never as a descendant of the grid", () => {
    const stacked = selectors.filter(selector => selector.includes("[data-stack='true']") && /\.lt-(row|columns)/.test(selector));
    expect(stacked.length).toBeGreaterThan(0);
    for (const selector of stacked) expect(selector).toMatch(/^\.lt-grid\[data-stack='true'\] > /);
    expect(stacked.filter(selector => /\.lt-grid\[data-stack='true'\] \.lt-/.test(selector))).toEqual([]);
  });

  it("scopes the row hover and the page-change fade of the values to the table's own rows", () => {
    expect(selectors.filter(selector => /^\.lt-row:hover/.test(selector))).toEqual([]);
    expect(selectors).toContain(':where(.lt-slot > .lt-slot-body) > .lt-row:hover');
    expect(selectors.filter(selector => /^\.lt-slot\[data-veil='fade'\] \.lt-value/.test(selector))).toEqual([]);
    expect(selectors).toContain(".lt-slot[data-veil='fade'] > .lt-slot-body > .lt-row > .lt-cell > .lt-value");
  });

  it("scopes the compact card spacing to the table's own rows or the embedded table's, not to any descendant row", () => {
    const compact = selectors.filter(selector => selector.startsWith(".lt-grid[data-density='compact']") && selector.includes('.lt-row'));
    expect(compact.length).toBeGreaterThan(0);
    expect(compact.filter(selector => /^\.lt-grid\[data-density='compact'\] \.lt-row/.test(selector))).toEqual([]);
  });
});
