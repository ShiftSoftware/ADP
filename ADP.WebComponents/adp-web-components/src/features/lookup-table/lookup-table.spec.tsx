import { Component, Prop, h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';
import { EmptyTableIcon } from '~assets/empty-table-icon';
import { QuestionMark } from '~assets/question-mark';
import { SearchIcon } from '~assets/search-icon';
import { TriangleAlertIcon } from '~assets/triangle-alert';

import {
  LookupColumn,
  LookupTable,
  LookupTableProps,
  PlaceholderKind,
  RowSlot,
  TablePlaceholder,
  closingDrawers,
  drawerAfterLookup,
  drawerId,
  placeholderKind,
} from './lookup-table';

type Visit = { invoice: string | null; date: string | null; odometer: number | string | null; type: string | null; part: string | null };

const visit = (over: Partial<Visit> = {}): Visit => ({ invoice: 'INV-001', date: '2025-03-04', odometer: 101328, type: 'Periodic service', part: null, ...over });

const col = (over: Partial<LookupColumn<Visit>> & Pick<LookupColumn<Visit>, 'field' | 'kind'>): LookupColumn<Visit> => ({ label: over.field, ...over });

const INVOICE = col({ field: 'invoice', label: 'Invoice', kind: 'code', identity: true, value: v => v.invoice });
const DATE = col({ field: 'date', label: 'Date', kind: 'date', value: v => v.date });
const ODOMETER = col({ field: 'odometer', label: 'Odometer', kind: 'number', value: v => v.odometer });
const TYPE = col({ field: 'type', label: 'Service type', kind: 'text', grow: true, value: v => v.type });
const PART = col({ field: 'part', label: 'Part', kind: 'text', value: v => v.part });
const DETAILS = col({ field: 'details', label: 'Details', kind: 'action' });

const strings = {
  columnsLabel: 'Visits',
  noImage: 'No picture',
  imageFailed: 'Picture did not load',
  openImage: 'Open picture of',
  expandRow: 'Show details of',
  collapseRow: 'Hide details of',
};

const base = (over: Partial<LookupTableProps<Visit>> = {}): LookupTableProps<Visit> => ({
  columns: [INVOICE, DATE, ODOMETER, TYPE, DETAILS],
  rows: [visit(), visit({ invoice: 'INV-002', date: '2025-01-20T08:30:00', odometer: '98000', type: null })],
  rowKey: (_, index) => `slot-${index}`,
  lead: 'columns',
  notice: null,
  busy: false,
  body: 'rows',
  open: true,
  strings,
  ...over,
});

const render = (over: Partial<LookupTableProps<Visit>> = {}) => newSpecPage({ components: [], template: () => <LookupTable {...base(over)} /> });

@Component({ tag: 'lt-test-host' })
class LtTestHost {
  @Prop() model: LookupTableProps<Visit>;

  render() {
    return this.model ? (
      <div>
        <LookupTable {...this.model} />
      </div>
    ) : null;
  }
}

const renderHost = async (model: LookupTableProps<Visit>) => {
  const page = await newSpecPage({ components: [LtTestHost], html: '<lt-test-host></lt-test-host>' });
  page.root.model = model;
  await page.waitForChanges();
  return page;
};

type Page = { body: HTMLElement };

const q = (page: Page, selector: string) => page.body.querySelector(selector) as HTMLElement | null;
const qa = (page: Page, selector: string) => Array.from(page.body.querySelectorAll(selector)) as HTMLElement[];

describe('cells', () => {
  it('renders a date as the day the DTO sent, in every language, and the dash when missing', async () => {
    const page = await render({ rows: [visit(), visit({ date: '2025-01-20T08:30:00' }), visit({ date: null })] });
    expect(qa(page, '.lt-row > .lt-date').map(el => el.textContent)).toEqual(['2025-03-04', '2025-01-20', '—']);

    const ar = await render({ lang: 'ar' });
    expect(qa(ar, '.lt-row > .lt-date')[0].textContent).toBe('2025-03-04');
  });

  it('renders a number grouped in Latin digits, shows a zero, and the dash for null or a value that is not a number', async () => {
    const page = await render({ rows: [visit(), visit({ odometer: '98000' }), visit({ odometer: 0 }), visit({ odometer: null }), visit({ odometer: 'n/a' })] });
    expect(qa(page, '.lt-row > .lt-number').map(el => el.textContent)).toEqual(['101,328', '98,000', '0', '—', '—']);

    const ar = await render({ lang: 'ar', rows: [visit()] });
    expect(q(ar, '.lt-row > .lt-number').textContent).toMatch(/^101.328$/);
  });

  it('drops a column that is empty on every row from an embedded table, and keeps it once any row has a value', async () => {
    const embedded = (rows: Visit[]) => render({ embedded: true, density: 'compact', label: 'Lines', columns: [INVOICE, DATE, ODOMETER, PART], rows });

    const page = await embedded([visit({ invoice: 'L-1' }), visit({ invoice: 'L-2', odometer: null })]);
    expect(qa(page, '.lt-columns > .lt-cell').map(el => el.textContent)).toEqual(['Invoice', 'Date', 'Odometer']);
    expect(qa(page, '[data-label="Part"]')).toEqual([]);

    const kept = await embedded([visit({ invoice: 'L-1' }), visit({ invoice: 'L-3', part: 'P-9' })]);
    expect(qa(kept, '.lt-row > [data-label="Part"]').map(el => el.textContent)).toEqual(['—', 'P-9']);
  });

  it('carries the table roles only while the rows are open', async () => {
    const page = await render();
    expect(q(page, '.lt-grid').getAttribute('role')).toBe('table');
    expect(qa(page, '.lt-row').map(el => el.getAttribute('role'))).toEqual(['row', 'row']);
    expect(qa(page, '.lt-columns > .lt-cell').every(el => el.getAttribute('role') === 'columnheader')).toBe(true);

    const shut = await render({ open: false });
    expect(qa(shut, '[role="table"], [role="row"], [role="cell"], [role="columnheader"]')).toEqual([]);
  });
});

describe('drawer', () => {
  const rows = [visit({ invoice: 'INV-001' }), visit({ invoice: 'INV-002' }), visit({ invoice: 'INV-003' })];
  const keyOf = (row: Visit) => row.invoice;
  const toggle = jest.fn();
  const covered: RowSlot[] = rows.map(() => ({ open: true, veil: 'cover' }));

  const withDrawer = (over: Partial<LookupTableProps<Visit>> = {}, actions: Partial<NonNullable<LookupTableProps<Visit>['actions']>> = {}) =>
    base({
      rows,
      actions: { expandedKey: null, onToggle: toggle, keyOf, ...actions },
      drawer: (row, key) => (
        <div class="stand-in" data-for={key}>
          {row.invoice} detail
        </div>
      ),
      ...over,
    });

  const opened = (page: Page) => qa(page, '.lt-drawer').map(el => el.getAttribute('data-open'));
  const content = (page: Page) => qa(page, '.stand-in').map(el => el.getAttribute('data-for'));
  const button = (page: Page, row: number) => qa(page, '.lt-action-button')[row];

  beforeEach(() => toggle.mockClear());

  it('has a button per row that names the row, points at its drawer and reports a press once, and is disabled while busy', async () => {
    const page = await renderHost(withDrawer({}, { expandedKey: 'INV-002' }));
    expect(button(page, 0).getAttribute('aria-expanded')).toBe('false');
    expect(button(page, 0).getAttribute('aria-controls')).toBe(drawerId('INV-001'));
    expect(button(page, 0).getAttribute('aria-label')).toBe('Show details of INV-001');
    expect(button(page, 1).getAttribute('aria-expanded')).toBe('true');
    expect(button(page, 1).getAttribute('aria-label')).toBe('Hide details of INV-002');

    button(page, 0).click();
    expect(toggle).toHaveBeenCalledTimes(1);
    expect(toggle).toHaveBeenCalledWith('INV-001');

    const busy = await renderHost(withDrawer({ busy: true }));
    expect(button(busy, 0).hasAttribute('disabled')).toBe(true);
  });

  it('toggles on a click anywhere on the row, but not from inside the open drawer or while busy', async () => {
    const page = await renderHost(withDrawer({}, { expandedKey: 'INV-001' }));
    qa(page, '.lt-row')[1].click();
    expect(toggle).toHaveBeenCalledWith('INV-002');

    toggle.mockClear();
    q(page, '.stand-in').click();
    expect(toggle).not.toHaveBeenCalled();

    const busy = await renderHost(withDrawer({ busy: true }));
    q(busy, '.lt-row').click();
    expect(toggle).not.toHaveBeenCalled();
  });

  it('is single-open: moving the key shuts the first drawer as the second opens, on the same elements', async () => {
    const model = withDrawer({}, { expandedKey: 'INV-001' });
    const page = await renderHost(model);
    const first = qa(page, '.lt-drawer')[0];
    expect(opened(page)).toEqual(['true', 'false', 'false']);

    page.root.model = {
      ...model,
      actions: {
        ...model.actions,
        expandedKey: 'INV-002',
        closing: closingDrawers({ closing: [], previousKey: 'INV-001', nextKey: 'INV-002', previousRows: rows, nextRows: rows, keyOf }),
      },
    };
    await page.waitForChanges();

    expect(qa(page, '.lt-drawer')[0]).toBe(first);
    expect(opened(page)).toEqual(['false', 'true', 'false']);
    expect(content(page)).toEqual(['INV-001', 'INV-002']);
  });

  it('stays open through a lookup that returns the same record, wherever the row now sits', async () => {
    const model = withDrawer({ busy: true, slots: covered }, { expandedKey: 'INV-002' });
    const page = await renderHost(model);
    expect(opened(page)).toEqual(['false', 'true', 'false']);
    expect(content(page)).toEqual(['INV-002']);

    const arrived = [visit({ invoice: 'INV-000' }), rows[0], rows[1]];
    const next = drawerAfterLookup('INV-002', arrived, keyOf);
    page.root.model = {
      ...model,
      busy: false,
      rows: arrived,
      slots: undefined,
      actions: {
        ...model.actions,
        expandedKey: next,
        closing: closingDrawers({ closing: [], previousKey: 'INV-002', nextKey: next, previousRows: rows, nextRows: arrived, keyOf }),
      },
    };
    await page.waitForChanges();

    expect(next).toBe('INV-002');
    expect(opened(page)).toEqual(['false', 'false', 'true']);
    expect(button(page, 2).getAttribute('aria-expanded')).toBe('true');
  });

  it('slides shut when the new rows do not have its record', async () => {
    const model = withDrawer({ busy: true, slots: covered }, { expandedKey: 'INV-002' });
    const page = await renderHost(model);

    const arrived = [visit({ invoice: 'INV-010' }), visit({ invoice: 'INV-011' })];
    const next = drawerAfterLookup('INV-002', arrived, keyOf);
    page.root.model = {
      ...model,
      busy: false,
      rows: arrived,
      slots: undefined,
      actions: {
        ...model.actions,
        expandedKey: next,
        closing: closingDrawers({ closing: [], previousKey: 'INV-002', nextKey: next, previousRows: rows, nextRows: arrived, keyOf }),
      },
    };
    await page.waitForChanges();

    expect(next).toBeNull();
    expect(opened(page).every(open => open === 'false')).toBe(true);
  });
});

describe('placeholder', () => {
  const TEXT: Record<PlaceholderKind, string> = {
    idle: 'Look up a vehicle.',
    empty: 'Nothing on file.',
    error: 'Could not load.',
    unauthorized: 'Not in the records.',
  };
  const content: TablePlaceholder['content'] = {
    idle: { icon: SearchIcon, text: TEXT.idle },
    empty: { icon: EmptyTableIcon, text: TEXT.empty },
    error: { icon: TriangleAlertIcon, text: TEXT.error },
    unauthorized: { icon: QuestionMark, text: TEXT.unauthorized },
  };
  const kinds = Object.keys(TEXT) as PlaceholderKind[];
  const model = (kind: PlaceholderKind | null, over: Partial<LookupTableProps<Visit>> = {}) =>
    base({ rows: [], body: 'none', open: false, placeholder: { kind, content }, ...over });
  const active = (page: Page) =>
    qa(page, '.lt-placeholder-layer')
      .filter(el => el.getAttribute('data-active') === 'true')
      .map(el => el.getAttribute('data-kind'));

  it('derives the outcome: idle before a lookup, empty, not in the records, error, and none above rows', () => {
    const kind = (over: Partial<Parameters<typeof placeholderKind>[0]> = {}) => placeholderKind({ columns: 3, loaded: true, failed: false, authorized: true, records: 4, ...over });
    expect(kind({ loaded: false, records: 9, authorized: false })).toBe('idle');
    expect(kind({ records: 0 })).toBe('empty');
    expect(kind({ authorized: false, records: 7 })).toBe('unauthorized');
    expect(kind({ failed: true, authorized: false, records: 3 })).toBe('error');
    expect(kind()).toBeNull();
    expect(kind({ leaving: true })).toBe('idle');
    expect(kind({ columns: 0, records: 0 })).toBeNull();
  });

  it('says one outcome at a time: its region is open and only its layer is an exposed status', async () => {
    for (const kind of kinds) {
      const page = await render(model(kind));
      expect(q(page, '.lt-placeholder').getAttribute('data-open')).toBe('true');
      expect(active(page)).toEqual([kind]);
      const exposed = qa(page, '.lt-placeholder [role="status"]').filter(el => el.getAttribute('aria-hidden') === null);
      expect(exposed.map(el => el.textContent)).toEqual([TEXT[kind]]);
    }

    const none = await render(model(null));
    expect(q(none, '.lt-placeholder').getAttribute('aria-hidden')).toBe('true');
    expect(active(none)).toEqual([]);
  });

  it('keeps one region through a lookup: the outcome stays under the skeleton while busy, then only the active layer moves', async () => {
    const page = await renderHost(model('idle'));
    const region = q(page, '.lt-placeholder');

    page.root.model = model('idle', { busy: true });
    await page.waitForChanges();
    expect(q(page, '.lt-placeholder')).toBe(region);
    expect(region.getAttribute('aria-busy')).toBe('true');
    expect(active(page)).toEqual(['idle']);
    expect(q(page, '.lt-placeholder-layer.is-idle .lt-placeholder-text').classList.contains('shift-skeleton')).toBe(true);

    page.root.model = model('empty');
    await page.waitForChanges();
    expect(q(page, '.lt-placeholder')).toBe(region);
    expect(region.hasAttribute('aria-busy')).toBe(false);
    expect(active(page)).toEqual(['empty']);
  });
});
