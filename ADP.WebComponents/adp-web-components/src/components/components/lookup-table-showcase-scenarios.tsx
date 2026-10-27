import { VNode, h } from '@stencil/core';

import {
  ClosingDrawer,
  LookupPager,
  LookupColumn,
  LookupTable,
  LookupTableProps,
  PageSize,
  PlaceholderKind,
  RowSlot,
  TablePlaceholder,
  isPaginated,
  pageRows,
  placeholderKind,
} from '~features/lookup-table';
import { PLACEHOLDER_GLYPHS } from '../vehicle-lookup/components/glyphs';
import type { VerdictState } from '~features/vehicle-info-layout';
import type { LanguageKeys } from '~features/multi-lingual';

import {
  BLANK_VISITS,
  LABOR_LINES,
  LaborLine,
  MANY_VISITS,
  PART_LINES,
  PART_LINES_FULL,
  PartLine,
  RELOOKUP_DROP,
  RELOOKUP_KEEP,
  SHOWCASE_STRINGS,
  ShowcaseStrings,
  VISITS,
  Visit,
  visitDetails,
} from './lookup-table-showcase-fixtures';

/**
 * The scenarios of the lookup table's dev showcase. A scenario is one entry in `SCENARIOS`: a name
 * (the key, which is also the value of the host's `scenario` prop), a label and a note for the page,
 * the card's busy flag and verdict, and a `render` that draws the card's body from the host's
 * context. **To add one, add an entry here and nothing else**: the host lists whatever is in this
 * table (`catalogue()`), so the page grows a chip for it. The drawer's scenarios (`drawer-*`) are the
 * ones with a `dataset`: the host owns their rows, so it can run the choreography an owner runs
 * (single-open, the closing drawers, a lookup that keeps or drops the open row, paging away).
 */

/**
 * What a placeholder scenario's lookup can end in: a list (`rows`), a vehicle with nothing on file,
 * one that is not in the records, a failed lookup, or nothing asked yet.
 */
export type Outcome = 'idle' | 'rows' | 'empty' | 'unauthorized' | 'error';

/** What the host hands a scenario: the language, its words and the showcase's own state. */
export type ShowcaseContext = {
  lang: LanguageKeys;
  t: ShowcaseStrings;
  /** The key of the row whose drawer (or button, in a scenario with no drawer) is open. */
  expandedKey: string | null;
  /** The key of the row whose button spins. */
  fetchingKey: string | null;
  page: number;
  pageSize: PageSize;
  /** The rows of a scenario that has a `dataset`: the page of it, or the last lookup's answer. Empty for the others. */
  rows: Visit[];
  /** Drawers sliding shut, for `RowActions.closing`. */
  closing: ClosingDrawer<Visit>[];
  /** A "look up again" is in flight: the card is busy and the rows veiled. */
  looking: boolean;
  /** A page change is in progress: the values fade while the open drawer shuts. */
  fading: boolean;
  /** The outcome of a scenario that has `outcomes`: what the last lookup ended in. `idle` elsewhere. */
  outcome: Outcome;
  /** The panel is clearing: the rows are shutting and the idle placeholder opens under the skeleton. */
  clearing: boolean;
  /** The rows an outcome without rows is still shutting over (kept a settle, so they slide away with the body). */
  heldRows: Visit[];
  /** The placeholder's last kind, kept a settle after it went, as an owner keeps it (rule 5). */
  retainedPlaceholder: PlaceholderKind | null;
  toggle: (key: string) => void;
  setPage: (page: number) => void;
  setPageSize: (size: PageSize) => void;
  /** "Look up again": the next answer in the scenario's `lookups` (or the next `outcome`). */
  relookup: () => void;
  /** Clear the panel: the rows leave under the skeleton and the idle placeholder opens. */
  clear: () => void;
};

export type Scenario = {
  /** The chip on the page. */
  label: string;
  /** One line under the chips: what to look at. */
  note: string;
  /** A lookup is in flight: the card's `.loading` (the skeleton's switch) and the head's wait spinner. */
  busy?: boolean;
  /** The card's accent bar. */
  verdict?: VerdictState;
  /** Pressing a button fetches first: it spins for a moment and then opens. */
  fetchOnOpen?: boolean;
  /** The drawer scenarios: the list the host owns, so it can page it and swap it. Without it the host manages no rows. */
  dataset?: Visit[];
  /** The drawer scenarios: the dataset is paged by the pager and paging away closes the open drawer. */
  paged?: boolean;
  /** The drawer scenarios: the key that starts open. */
  initialOpen?: string;
  /** The answers "look up again" cycles through, the first being the dataset itself. */
  lookups?: Visit[][];
  /** The placeholder scenarios that are interactive: the outcomes "look up again" cycles through, the first being where it starts. */
  outcomes?: Outcome[];
  render: (context: ShowcaseContext) => VNode;
};

/** The id of the element the pager pages, for its aria-controls. */
export const SHOWCASE_LIST_ID = 'showcase-list';

/** Its own key: the showcase's page size never leaks into a real panel's. */
export const SHOWCASE_PAGE_SIZE_KEY = 'showcase:lookup-table:page-size';

/** A visit's identity: its invoice number, not its position (run decision 2: an open drawer follows its invoice through a lookup). */
export const visitKey = (visit: Visit, index: number): string => visit.invoice ?? `row-${index}`;

/** The main table's columns: the service-history shape, with the three new kinds. */
export const visitColumns = (t: ShowcaseStrings): LookupColumn<Visit>[] => [
  { field: 'invoice', label: t.invoice, kind: 'code', identity: true, value: visit => visit.invoice },
  { field: 'branch', label: t.branch, kind: 'text', value: visit => visit.branch },
  { field: 'date', label: t.date, kind: 'date', value: visit => visit.date },
  { field: 'serviceType', label: t.serviceType, kind: 'text', grow: true, value: visit => visit.type },
  { field: 'odometer', label: t.odometer, kind: 'number', value: visit => visit.odometer },
  { field: 'details', label: t.details, kind: 'action' },
];

export const laborColumns = (t: ShowcaseStrings): LookupColumn<LaborLine>[] => [
  { field: 'laborCode', label: t.laborCode, kind: 'code', identity: true, value: line => line.code },
  { field: 'description', label: t.laborDescription, kind: 'text', grow: true, value: line => line.description },
  { field: 'packageCode', label: t.packageCode, kind: 'code', value: line => line.packageCode },
  { field: 'serviceCode', label: t.serviceCode, kind: 'code', value: line => line.serviceCode },
];

export const partColumns = (t: ShowcaseStrings): LookupColumn<PartLine>[] => [
  { field: 'partNumber', label: t.partNumber, kind: 'code', identity: true, value: line => line.partNumber },
  { field: 'partDescription', label: t.partDescription, kind: 'text', grow: true, value: line => line.partDescription },
  { field: 'packageCode', label: t.packageCode, kind: 'code', value: line => line.packageCode },
  { field: 'quantity', label: t.quantity, kind: 'number', value: line => line.qty },
];

const tableStrings = (t: ShowcaseStrings) => ({
  columnsLabel: t.columnsLabel,
  noImage: t.noImage,
  imageFailed: t.imageFailed,
  openImage: t.openImage,
  expandRow: t.expandRow,
  collapseRow: t.collapseRow,
});

/** The main table over `rows`, every prop a scenario does not name at its settled, open default. */
const visitTable = (context: ShowcaseContext, rows: Visit[], over: Partial<LookupTableProps<Visit>> = {}): VNode => (
  <div id={SHOWCASE_LIST_ID}>
    <LookupTable
      columns={visitColumns(context.t)}
      rows={rows}
      rowKey={(_, index) => `slot-${index}`}
      lead="columns"
      notice={null}
      busy={false}
      body="rows"
      open={true}
      lang={context.lang}
      strings={tableStrings(context.t)}
      actions={{ expandedKey: context.expandedKey, fetchingKey: context.fetchingKey, onToggle: context.toggle, keyOf: visitKey, closing: context.closing }}
      {...over}
    />
  </div>
);

const embeddedLines = <Line,>(context: ShowcaseContext, name: string, columns: LookupColumn<Line>[], lines: Line[], keyPrefix: string): VNode => (
  <LookupTable
    columns={columns}
    rows={lines}
    rowKey={(_, index) => `${keyPrefix}-${index}`}
    lead="columns"
    notice={null}
    busy={false}
    body="rows"
    open={true}
    lang={context.lang}
    strings={tableStrings(context.t)}
    embedded={true}
    density="compact"
    label={name}
  />
);

/**
 * What the drawer holds: a meta line (only the facts that exist, trimmed) above the labor and part
 * tables, two embedded compact tables built with the table's own modes. The part table's description
 * column is dropped on a visit that never fills it; a visit with no labor lines says so in words (a
 * statement about a record that exists, not a verdict).
 */
const visitDrawer = (context: ShowcaseContext, visit: Visit): VNode => {
  const { t } = context;
  const details = visitDetails(visit);
  const meta = [
    { label: t.jobNumber, value: details.job },
    { label: t.parentInvoice, value: details.parentInvoice },
    { label: t.accountNumber, value: details.account?.trim() },
  ].filter(item => item.value);

  return (
    <div class="showcase-drawer">
      {meta.length > 0 && (
        <p class="showcase-meta">
          {meta.map(item => (
            <span class="showcase-meta-item">
              <span class="showcase-meta-label">{item.label}</span> <span class="showcase-meta-value">{item.value}</span>
            </span>
          ))}
        </p>
      )}
      <section class="showcase-lines">
        <p class="showcase-stand-caption">{t.laborLines}</p>
        {details.labor.length > 0 ? embeddedLines(context, t.laborLines, laborColumns(t), details.labor, 'labor') : <p class="showcase-none">{t.noLaborLines}</p>}
      </section>
      <section class="showcase-lines">
        <p class="showcase-stand-caption">{t.partLines}</p>
        {embeddedLines(context, t.partLines, partColumns(t), details.parts, 'part')}
      </section>
    </div>
  );
};

/** The drawer scenarios' table: the host's rows, slots veiled by what the host is doing, and the drawer. */
const drawerTable = (context: ShowcaseContext, over: Partial<LookupTableProps<Visit>> = {}): VNode => {
  const veil: RowSlot['veil'] = context.looking ? 'cover' : context.fading ? 'fade' : 'none';

  return visitTable(context, context.rows, {
    busy: context.looking,
    slots: context.rows.map(() => ({ open: true, veil })),
    drawer: row => visitDrawer(context, row),
    ...over,
  });
};

/** The pager over a dataset, as the real panels hang it under their table. */
const pagerFor = (context: ShowcaseContext, total: number, busy = false): VNode => (
  <LookupPager
    strings={{
      rowsPerPage: context.t.rowsPerPage,
      pageAll: context.t.pageAll,
      previousPage: context.t.previousPage,
      nextPage: context.t.nextPage,
      pageRange: context.t.pageRange,
    }}
    controls={SHOWCASE_LIST_ID}
    page={context.page}
    pageSize={context.pageSize}
    total={total}
    open={isPaginated(total)}
    busy={busy}
    onPage={context.setPage}
    onPageSize={context.setPageSize}
  />
);

/** The dev control that stands in for a panel's next lookup. Not part of the table. */
const lookupControl = (context: ShowcaseContext): VNode => (
  <div class="showcase-controls">
    <button type="button" class="showcase-lookup" disabled={context.looking} onClick={context.relookup}>
      {context.t.lookUpAgain}
    </button>
  </div>
);

/** A frame that stands in for a drawer, so the embedded table is seen where it will live. The real drawer replaces it. */
const stand = (caption: string, table: VNode): VNode => (
  <section class="showcase-stand">
    <p class="showcase-stand-caption">{caption}</p>
    {table}
  </section>
);

/** The words and glyph of each outcome, as a panel hands them to the table. */
export const placeholderContent = (t: ShowcaseStrings): TablePlaceholder['content'] => ({
  idle: { glyph: PLACEHOLDER_GLYPHS.search, text: t.placeholderIdle },
  empty: { glyph: PLACEHOLDER_GLYPHS.box, text: t.placeholderEmpty },
  error: { glyph: PLACEHOLDER_GLYPHS.warning, text: t.placeholderError },
  unauthorized: { glyph: PLACEHOLDER_GLYPHS.unknown, text: t.placeholderUnauthorized },
});

/** What the placeholder says for an outcome, by the table's own derivation, as a panel with all six columns shown would ask it. */
export const placeholderFor = (outcome: Outcome, leaving = false): PlaceholderKind | null =>
  placeholderKind({
    columns: visitColumns(SHOWCASE_STRINGS.en).length,
    loaded: outcome !== 'idle' && outcome !== 'error',
    failed: outcome === 'error',
    authorized: outcome !== 'unauthorized',
    records: outcome === 'rows' ? VISITS.length : 0,
    leaving,
  });

/** The main table with the body's placeholder, for the outcome-less scenarios: no rows, the body shut, the placeholder open on `kind`. */
const placeholderTable = (context: ShowcaseContext, kind: PlaceholderKind | null, over: Partial<LookupTableProps<Visit>> = {}): VNode =>
  visitTable(context, [], { body: 'none', open: false, placeholder: { kind, retained: kind, content: placeholderContent(context.t) }, ...over });

/**
 * The interactive placeholder scenario: the table as an owner drives it through "look up again" and
 * "clear". Rows stay under the skeleton while a lookup is in flight; an outcome without rows shuts the
 * body over them (they are kept a settle in `heldRows`) as the placeholder opens, and every other pair
 * is a cross-fade of the placeholder's layers.
 */
const outcomeTable = (context: ShowcaseContext): VNode => {
  const listed = context.outcome === 'rows';
  const rows = listed ? VISITS : context.heldRows;
  const open = listed && !context.clearing;

  return visitTable(context, rows, {
    busy: context.looking,
    body: rows.length > 0 ? 'rows' : 'none',
    open,
    slots: rows.map(() => ({ open: true, veil: context.looking ? ('cover' as const) : ('none' as const) })),
    placeholder: { kind: placeholderFor(context.outcome, context.clearing), retained: context.retainedPlaceholder, content: placeholderContent(context.t) },
  });
};

const outcomeControls = (context: ShowcaseContext): VNode => (
  <div class="showcase-controls">
    <button type="button" class="showcase-lookup" disabled={context.looking} onClick={context.relookup}>
      {context.t.lookUpAgain}
    </button>
    <button type="button" class="showcase-lookup" disabled={context.looking || context.outcome === 'idle'} onClick={context.clear}>
      {context.t.clear}
    </button>
  </div>
);

export const SCENARIOS: Record<string, Scenario> = {
  'idle': {
    label: 'Idle',
    note: 'Before a lookup: the headings are on the strip (they depend on the columns, not the data) and the body is shut.',
    render: context => visitTable(context, [], { body: 'none', open: false }),
  },
  'busy': {
    label: 'Busy',
    note: 'A lookup in flight: every row stays where it is, its values under the skeleton, and the buttons cannot be pressed.',
    busy: true,
    render: context =>
      visitTable(context, VISITS, {
        busy: true,
        slots: VISITS.map(() => ({ open: true, veil: 'cover' as const })),
      }),
  },
  'loaded': {
    label: 'Loaded',
    note: 'Date, number and action columns. Press a button: it opens (the chevron turns, the row tints). No drawer yet.',
    render: context => visitTable(context, VISITS),
  },
  'expanded': {
    label: 'Expanded',
    note: 'One row open: aria-expanded, the chevron turned and the row tinted. Single-open: pressing another moves it.',
    render: context => visitTable({ ...context, expandedKey: context.expandedKey ?? 'INV-100377' }, VISITS),
  },
  'fetching': {
    label: 'Fetching',
    note: 'A button that fetches before it opens: press one and it spins for a moment, then opens.',
    fetchOnOpen: true,
    render: context => visitTable({ ...context, fetchingKey: context.fetchingKey ?? 'INV-100119' }, VISITS),
  },
  'blank': {
    label: 'Blank values',
    note: 'A missing date, a zero, a reading that is not a number, no service type and no invoice: each empty cell is the grey dash and a zero is a value.',
    render: context => visitTable(context, BLANK_VISITS),
  },
  'compact': {
    label: 'Compact',
    note: 'The same table at compact density: 6px cells and the small note type.',
    render: context => visitTable(context, VISITS, { density: 'compact' }),
  },
  'embedded': {
    label: 'Embedded',
    note: 'Two embedded compact tables where a drawer would hold them. The part table has no description on any line, so its column is not there.',
    render: context => (
      <div class="showcase-stands">
        {stand(
          context.t.laborLines,
          <LookupTable
            columns={laborColumns(context.t)}
            rows={LABOR_LINES}
            rowKey={(_, index) => `labor-${index}`}
            lead="columns"
            notice={null}
            busy={false}
            body="rows"
            open={true}
            lang={context.lang}
            strings={tableStrings(context.t)}
            embedded={true}
            density="compact"
            label={context.t.laborLines}
          />,
        )}
        {stand(
          context.t.partLines,
          <LookupTable
            columns={partColumns(context.t)}
            rows={PART_LINES}
            rowKey={(_, index) => `part-${index}`}
            lead="columns"
            notice={null}
            busy={false}
            body="rows"
            open={true}
            lang={context.lang}
            strings={tableStrings(context.t)}
            embedded={true}
            density="compact"
            label={context.t.partLines}
          />,
        )}
      </div>
    ),
  },
  'embedded-full': {
    label: 'Embedded, all columns',
    note: 'The same part table with a description on some line: every column is kept.',
    render: context =>
      stand(
        context.t.partLines,
        <LookupTable
          columns={partColumns(context.t)}
          rows={PART_LINES_FULL}
          rowKey={(_, index) => `part-${index}`}
          lead="columns"
          notice={null}
          busy={false}
          body="rows"
          open={true}
          lang={context.lang}
          strings={tableStrings(context.t)}
          embedded={true}
          density="compact"
          label={context.t.partLines}
        />,
      ),
  },
  'paged': {
    label: 'Paged',
    note: 'Twenty-eight rows under the shared pager (its own storage key). The page size is kept in localStorage.',
    render: context => {
      const rows = pageRows(MANY_VISITS, context.page, context.pageSize);

      return (
        <div>
          {visitTable(context, rows)}
          {pagerFor(context, MANY_VISITS.length)}
        </div>
      );
    },
  },
  'drawer-closed': {
    label: 'Drawer, closed',
    note: 'Every row has a drawer, all shut. Press a row (or its button): its drawer slides open under it, the row tints, and the page follows. Press another: the first slides shut as the second opens.',
    dataset: VISITS,
    render: context => drawerTable(context),
  },
  'drawer-open': {
    label: 'Drawer, open',
    note: 'One drawer open: a meta line and two embedded compact tables (the part table has no description on any line, so that column is not there). Press the button to shut it.',
    dataset: VISITS,
    initialOpen: 'INV-099430',
    render: context => drawerTable(context),
  },
  'drawer-veiled': {
    label: 'Drawer, veiled',
    note: 'A lookup in flight with a drawer open: the drawer stays open, its content under the skeleton like its row, and the buttons cannot be pressed.',
    busy: true,
    dataset: VISITS,
    initialOpen: 'INV-099430',
    render: context => drawerTable(context, { busy: true, slots: context.rows.map(() => ({ open: true, veil: 'cover' as const })) }),
  },
  'drawer-relookup-keep': {
    label: 'Look up again, keeps',
    note: 'Open a drawer, then look up again: it stays open, veiled, through the lookup. The next answer has the same invoice one row lower (and a new visit on top), so the drawer ends open on it.',
    dataset: VISITS,
    initialOpen: 'INV-100377',
    lookups: [VISITS, RELOOKUP_KEEP],
    render: context => (
      <div>
        {drawerTable(context)}
        {lookupControl(context)}
      </div>
    ),
  },
  'drawer-relookup-drop': {
    label: 'Look up again, drops',
    note: 'Open a drawer, then look up again: it stays open, veiled, through the lookup. The next answer is another vehicle, so the invoice is gone and the drawer slides shut as the new rows arrive.',
    dataset: VISITS,
    initialOpen: 'INV-100377',
    lookups: [VISITS, RELOOKUP_DROP],
    render: context => (
      <div>
        {drawerTable(context)}
        {lookupControl(context)}
      </div>
    ),
  },
  'drawer-paged': {
    label: 'Drawer, paged',
    note: 'Twenty-eight rows under the pager, the first row open. Paging away shuts the drawer with its row (the values fade as it slides shut); coming back shows it shut. A page size that keeps the row on screen leaves it open.',
    dataset: MANY_VISITS,
    paged: true,
    initialOpen: 'INV-300000',
    render: context => (
      <div>
        {drawerTable(context)}
        {pagerFor(context, MANY_VISITS.length, context.fading)}
      </div>
    ),
  },
  'empty': {
    label: 'Empty',
    note: 'A vehicle with no visits on record: the strip says so in grey. The table never decides this from an empty list; the host does.',
    render: context => visitTable(context, [], { lead: 'notice', body: 'none', open: false, notice: { tone: 'idle', text: context.t.emptyNotice } }),
  },
  'error': {
    label: 'Error',
    note: 'A failed lookup: a negative notice replaces the headings, the body is shut.',
    verdict: 'negative',
    render: context => visitTable(context, [], { lead: 'notice', body: 'none', open: false, notice: { tone: 'negative', text: context.t.errorNotice } }),
  },
  'placeholder-idle': {
    label: 'Placeholder, idle',
    note: 'Before a lookup: the headings stay on the strip and the body holds a large grey magnifier and one line. It asserts nothing about a vehicle.',
    render: context => placeholderTable(context, 'idle'),
  },
  'placeholder-empty': {
    label: 'Placeholder, empty',
    note: 'An authorized vehicle with nothing on file: a grey box and "no records". The table never reads a verdict off the empty list.',
    render: context => placeholderTable(context, 'empty'),
  },
  'placeholder-error': {
    label: 'Placeholder, error',
    note: 'A failed lookup: a red warning triangle and one line; the pill and the accent bar carry the reason. The headings stay.',
    verdict: 'negative',
    render: context => placeholderTable(context, 'error'),
  },
  'placeholder-unauthorized': {
    label: 'Placeholder, not in records',
    note: 'A vehicle the distributor has no record of: an amber question mark and one line. It is not "none" and never the list.',
    verdict: 'neutral',
    render: context => placeholderTable(context, 'unauthorized'),
  },
  'placeholder-busy': {
    label: 'Placeholder, busy',
    note: 'A lookup in flight over an empty answer: the placeholder stays where it stands, its icon and words under the skeleton, and the body does not move.',
    busy: true,
    render: context => placeholderTable(context, 'empty', { busy: true }),
  },
  'placeholder-cycle': {
    label: 'Placeholder, every pair',
    note: 'Look up again walks idle, rows, empty, not in records, error, empty and rows, each over a 1.6 second lookup; Clear goes back to idle. Rows stay under the skeleton, the placeholder cross-fades or slides with the body, and nothing below it jumps.',
    outcomes: ['idle', 'rows', 'empty', 'unauthorized', 'error', 'empty', 'rows'],
    render: context => (
      <div>
        {outcomeTable(context)}
        {outcomeControls(context)}
      </div>
    ),
  },
};
