import { FunctionalComponent, h } from '@stencil/core';

import ServiceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';
import type { VehicleLaborDTO } from '~types/generated/vehicle-lookup/vehicle-labor-dto';
import type { VehiclePartDTO } from '~types/generated/vehicle-lookup/vehicle-part-dto';
import type { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import { ComponentLocale } from '~features/multi-lingual';
import { LookupHead, PanelVerdict } from '~features/vehicle-info-layout';
import {
  ClosingDrawer,
  LeadKind,
  LookupColumn,
  LookupPager,
  LookupTable,
  PageSize,
  PlaceholderKind,
  RowSlot,
  TablePlaceholder,
  isPaginated,
  placeholderKind,
  shownBody,
  visibleColumns,
} from '~features/lookup-table';

import { PLACEHOLDER_GLYPHS } from './glyphs';

/** The panel's strings and the shared ones (direction, language code). */
export type ServiceHistoryLocale = ComponentLocale<typeof ServiceHistorySchema>;

/**
 * Everything the panel is decided from, assembled by the owner. `hidden` is the *applied* set (the
 * owner swaps it while the body is shut), so a column change never reaches the render half-done.
 */
export type PanelState = {
  /** A vehicle is on screen and its lookup did not fail. */
  vehicleLoaded: boolean;
  /** `isAuthorized` of that vehicle; `false` means the distributor has no record of it. */
  authorized?: boolean;
  /** The DTO's list, whatever it holds: a list the distributor may not show is dropped by `listed`. */
  history?: VehicleServiceHistoryDTO[] | null;
  hidden: ReadonlySet<string>;
  /** The lookup failed: the sentence the pill carries (the body's placeholder says it in its own words). */
  error?: string;
};

/**
 * The hiddenFields names, in column order: the host's whole vocabulary for this panel. They are the
 * DTO's own property names, as the other panels' are. The invoice number, the date and the expand
 * button are not optional: a row is an invoice on a day, and the button is how its lines are read.
 */
export const FIELDS = ['branchName', 'companyName', 'jobNumber', 'serviceType', 'mileage'] as const;

/**
 * What a host sees without configuring anything (owner, decision 10): every column except the job
 * number, which workshop staff quote but a service advisor rarely reads.
 */
export const DEFAULT_HIDDEN_FIELDS = 'jobNumber';

/** Where the reader's choice is kept, per browser: the panel opens on it next time. Its own key, so it never leaks into accessories'. */
export const PAGE_SIZE_STORAGE_KEY = 'shift-components:vehicle-service-history:page-size';

/** The id of the element the pager pages, for its `aria-controls`. */
export const LIST_ID = 'service-history-list';

// ---------------------------------------------------------------------------------------------
// Columns
// ---------------------------------------------------------------------------------------------

/** A value from the source: trimmed, and null when blank (`""` is the same as nothing, T0b §3). */
export const clean = (value: string | null | undefined): string | null => {
  const text = (value ?? '').trim();
  return text === '' ? null : text;
};

/**
 * The panel's columns, all of them; hiddenFields takes some away (`visibleColumns`). The invoice number
 * is the row's identity (a card's first line, the name the expand button carries); the service type is
 * the one column that grows, and the odometer is a figure. The expand button is last, on the trailing edge.
 */
export const serviceHistoryColumns = (locale: ServiceHistoryLocale): LookupColumn<VehicleServiceHistoryDTO>[] => [
  { field: 'branchName', label: locale.branch, kind: 'text', value: row => row.branchName },
  { field: 'companyName', label: locale.dealer, kind: 'text', value: row => row.companyName },
  { field: 'invoiceNumber', label: locale.invoiceNumber, kind: 'code', identity: true, value: row => row.invoiceNumber },
  { field: 'jobNumber', label: locale.jobNumber, kind: 'code', value: row => row.jobNumber },
  { field: 'serviceDate', label: locale.date, kind: 'date', value: row => row.serviceDate },
  { field: 'serviceType', label: locale.serviceType, kind: 'text', grow: true, value: row => row.serviceType },
  { field: 'mileage', label: locale.odometer, kind: 'number', value: row => row.mileage },
  { field: 'details', label: locale.details, kind: 'action' },
];

/** The drawer's labor table: the line's code names it, the description grows. A package or service code empty on every line is dropped by the table. */
export const laborColumns = (locale: ServiceHistoryLocale): LookupColumn<VehicleLaborDTO>[] => [
  { field: 'laborCode', label: locale.laborCode, kind: 'code', identity: true, value: line => line.laborCode },
  { field: 'serviceDescription', label: locale.serviceDescription, kind: 'text', grow: true, value: line => line.serviceDescription },
  { field: 'packageCode', label: locale.packageCode, kind: 'code', value: line => line.packageCode },
  { field: 'serviceCode', label: locale.serviceCode, kind: 'code', value: line => line.serviceCode },
];

/** The drawer's part table: a part description no line carries (the service never fills it today) is dropped by the table, not shown as a blank column. */
export const partColumns = (locale: ServiceHistoryLocale): LookupColumn<VehiclePartDTO>[] => [
  { field: 'partNumber', label: locale.partNumber, kind: 'code', identity: true, value: line => line.partNumber },
  { field: 'partDescription', label: locale.partDescription, kind: 'text', grow: true, value: line => line.partDescription },
  { field: 'packageCode', label: locale.packageCode, kind: 'code', value: line => line.packageCode },
  { field: 'qty', label: locale.qty, kind: 'number', value: line => line.qty },
];

/** How many columns a hidden set leaves: the optional ones that stay, plus invoice, date and the button. Names the panel does not know are ignored. */
export const columnCount = (hidden: ReadonlySet<string>): number => FIELDS.filter(field => !hidden.has(field)).length + 3;

/** The part of a hidden set that can take a column away: only the optional fields. The invoice, the date and the button are never hidden, whatever the host names. */
export const hideableOnly = (hidden: ReadonlySet<string>): ReadonlySet<string> => new Set(FIELDS.filter(field => hidden.has(field)));

/** Whether two hidden sets leave the same columns: an unknown name changing is not a layout change. */
export const sameColumns = (a: ReadonlySet<string>, b: ReadonlySet<string>): boolean => FIELDS.every(field => a.has(field) === b.has(field));

// ---------------------------------------------------------------------------------------------
// A row's identity, and what the drawer's meta line says
// ---------------------------------------------------------------------------------------------

/**
 * A row's identity: what an open drawer follows through a lookup (run decision 2), what the expand
 * button and the row's tint are found by. An invoice number alone is not unique (two branches, two
 * dealers), so it is company + branch + invoice, plus the job number: the source groups its lines by
 * all four, so that is what makes one record. A missing part is an empty one, so a record with nulls
 * still has a key (and two that differ only in a null are told apart by the job).
 */
export const historyKey = (row: VehicleServiceHistoryDTO): string => [row.companyName, row.branchName, row.invoiceNumber, row.jobNumber].map(part => clean(part) ?? '').join('|');

/** The original invoice of a credit or debit note, when there is one: the source sends `"0"` (or nothing) for "none", which is not an invoice. */
export const realParentInvoice = (row: VehicleServiceHistoryDTO): string | null => {
  const parent = clean(row.parentInvoiceNumber);
  return parent === null || parent === '0' ? null : parent;
};

/** The accounting account number, trimmed (the source pads it with spaces); null when blank. */
export const accountNumber = (row: VehicleServiceHistoryDTO): string | null => clean(row.accountNumber);

export type MetaItem = { key: 'jobNumber' | 'parentInvoice' | 'accountNumber'; label: string; value: string };

/** The drawer's meta line: job number, original invoice (only a real one), account number. Only the ones that have a value, in that order. */
export const metaItems = (row: VehicleServiceHistoryDTO, locale: ServiceHistoryLocale): MetaItem[] =>
  [
    { key: 'jobNumber' as const, label: locale.jobNumber, value: clean(row.jobNumber) },
    { key: 'parentInvoice' as const, label: locale.parentInvoice, value: realParentInvoice(row) },
    { key: 'accountNumber' as const, label: locale.accountNumber, value: accountNumber(row) },
  ].filter((item): item is MetaItem => item.value !== null);

// ---------------------------------------------------------------------------------------------
// The derivations (design language § 20): what the strip says, what the body holds, the placeholder
// ---------------------------------------------------------------------------------------------

/**
 * The rows the panel may list. An unauthorized vehicle has none whatever the DTO holds: the list
 * is not the distributor's to show (vehicle-lookup-invariants.md), and a fixture that carries one
 * is a data fault the panel does not follow.
 */
export const listed = (state: PanelState): VehicleServiceHistoryDTO[] => (state.vehicleLoaded && state.authorized !== false ? (state.history ?? []) : []);

/** Whether the vehicle has service history on file. Judged on the whole list, never on the page on screen. */
export const hasRecords = (state: PanelState): boolean => listed(state).length > 0;

/** `rows` when there is a list and at least one column to draw it in (there always is one); `none` otherwise. The body shuts for `none`. */
export const panelBody = (state: PanelState): 'rows' | 'none' => (hasRecords(state) && columnCount(state.hidden) > 0 ? 'rows' : 'none');

/**
 * The strip's layer: always the column headings (they depend on hiddenFields alone, never on the
 * data, and the invoice, date and button columns cannot be hidden, so there are always some). An
 * outcome without rows is said by the pill and by the body's placeholder, never by covering the
 * headings, so the strip does not say it a third time. It does not read `busy`: in flight the headings stay.
 */
export const panelLead = (state: PanelState): LeadKind => (columnCount(state.hidden) === 0 ? 'notice' : 'columns');

/**
 * What the body says when there is no list: the table's `placeholderKind` fed this panel's state.
 * `idle` before any lookup (asserts nothing about a vehicle), `empty` for an authorized vehicle with
 * nothing on file (never a verdict about the vehicle), `unauthorized` for a vehicle not in the
 * distributor's records (amber; not "none", and never the list), `error` for a failed lookup, and
 * null above rows. It holds through a lookup, so the placeholder in place stays under the skeleton
 * and cross-fades when the answer lands; `leaving` (a clear, or a failure on a settled card) opens
 * the idle one as the rows shut.
 */
export const panelPlaceholder = (state: PanelState, leaving = false): PlaceholderKind | null =>
  placeholderKind({
    columns: columnCount(state.hidden),
    loaded: state.vehicleLoaded,
    failed: !!state.error,
    authorized: state.authorized,
    records: listed(state).length,
    leaving,
  });

/** The glyph and the one line of each outcome, from the panel's locale. Empty has the service panel's own mark. */
export const placeholderContent = (locale: ServiceHistoryLocale): TablePlaceholder['content'] => ({
  idle: { glyph: PLACEHOLDER_GLYPHS.search, text: locale.idleHint },
  empty: { glyph: PLACEHOLDER_GLYPHS.record, text: locale.emptyHint },
  error: { glyph: PLACEHOLDER_GLYPHS.warning, text: locale.errorHint },
  unauthorized: { glyph: PLACEHOLDER_GLYPHS.unknown, text: locale.unauthorizedHint },
});

/** Whether the body is open on rows: a list to show, not shutting for a clear, not swapping columns. In flight it stays open. */
export const bodyOpen = (state: PanelState, leaving: boolean, reconfiguring: boolean): boolean => !leaving && !reconfiguring && panelBody(state) === 'rows';

// ---------------------------------------------------------------------------------------------
// The drawer: the meta line, then the labor lines and the part lines as embedded compact tables
// ---------------------------------------------------------------------------------------------

/** The table's strings: the columns label, the expand button's words, and the media strings this panel has no use for. */
const tableStrings = (locale: ServiceHistoryLocale) => ({
  columnsLabel: locale.columnsLabel,
  noImage: '',
  imageFailed: '',
  openImage: '',
  expandRow: locale.expandRow,
  collapseRow: locale.collapseRow,
});

/**
 * One kind of line, as a titled section: its name and count, then the lines as an embedded compact
 * table (a column empty on every line is dropped), or, for a record that has none of them, a
 * statement of that. The statement is about an invoice that exists, not a verdict about the vehicle.
 */
const LinesSection = <Line,>(props: { title: string; none: string; columns: LookupColumn<Line>[]; lines: Line[]; prefix: string; lang: string; locale: ServiceHistoryLocale }) => {
  const { title, none, columns, lines, prefix, lang, locale } = props;

  return (
    <section class="service-history-lines" data-kind={prefix}>
      <div class="service-history-lines-head">
        <span class="service-history-lines-title">{title}</span>
        <span class="service-history-lines-count">{lines.length}</span>
      </div>

      {lines.length > 0 ? (
        <LookupTable
          columns={columns}
          rows={lines}
          rowKey={(_, index) => `${prefix}-${index}`}
          lead="columns"
          notice={null}
          busy={false}
          body="rows"
          open={true}
          lang={lang}
          strings={tableStrings(locale)}
          embedded={true}
          density="compact"
          label={title}
        />
      ) : (
        <p class="service-history-none">{none}</p>
      )}
    </section>
  );
};

/** What an open row holds. It is built only for the open row and for one that is shutting, never for a shut one. */
export const ServiceHistoryDrawer: FunctionalComponent<{ row: VehicleServiceHistoryDTO; locale: ServiceHistoryLocale }> = ({ row, locale }) => {
  const meta = metaItems(row, locale);
  const lang = locale.sharedLocales.lang;

  return (
    <div class="service-history-drawer">
      {meta.length > 0 && (
        <p class="service-history-meta">
          {meta.map(item => (
            <span class="service-history-meta-item" data-field={item.key}>
              <span class="service-history-meta-label">{item.label}</span> <span class="service-history-meta-value">{item.value}</span>
            </span>
          ))}
        </p>
      )}

      <LinesSection title={locale.laborLines} none={locale.noLaborLines} columns={laborColumns(locale)} lines={row.laborLines ?? []} prefix="labor" lang={lang} locale={locale} />
      <LinesSection title={locale.partLines} none={locale.noPartLines} columns={partColumns(locale)} lines={row.partLines ?? []} prefix="part" lang={lang} locale={locale} />
    </div>
  );
};

// ---------------------------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------------------------

type Props = {
  locale: ServiceHistoryLocale;
  state: PanelState;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  /** A lookup is in flight or the panel is leaving: the head leaves; rows on screen go under the skeleton where they stand. */
  busy: boolean;
  /** The panel is leaving (cleared, or failing from a settled card): the body shuts over its rows. */
  leaving: boolean;
  /** A live hiddenFields change is in progress: the body is shut while the columns swap. */
  reconfiguring: boolean;
  /** The body's last kind, kept a settle after it shut so its content slides away instead of blinking out. */
  retainedBody: string;
  /** The placeholder's last kind, kept a settle after it went (rows opened) so its words slide away with their region. */
  retainedPlaceholder: PlaceholderKind | null;
  /** The rows in the table's slots: the page on screen, then any rows that are leaving (the owner's). */
  slotRows: VehicleServiceHistoryDTO[];
  /** One per slot row: open or shut, and how its values are veiled (the owner's `tableSlots`). */
  slots: RowSlot[];
  /** The row whose drawer is open (`historyKey`), or null. */
  expandedKey: string | null;
  /** Drawers sliding shut: their content stays mounted until the owner drops them. */
  closing: ClosingDrawer<VehicleServiceHistoryDTO>[];
  onToggle: (key: string) => void;
  page: number;
  pageSize: PageSize;
  onPage: (page: number) => void;
  onPageSize: (size: PageSize) => void;
};

export const VehicleServiceHistoryPanel: FunctionalComponent<Props> = props => {
  const {
    locale,
    state,
    verdict,
    busy,
    leaving,
    reconfiguring,
    retainedBody,
    retainedPlaceholder,
    slotRows,
    slots,
    expandedKey,
    closing,
    onToggle,
    page,
    pageSize,
    onPage,
    onPageSize,
  } = props;

  const columns = visibleColumns(serviceHistoryColumns(locale), hideableOnly(state.hidden));
  const total = listed(state).length;

  const body = panelBody(state);
  const lead = panelLead(state);
  const open = bodyOpen(state, leaving, reconfiguring);
  const pagerOpen = open && isPaginated(total);
  const placeholder = panelPlaceholder(state, leaving);

  return (
    <section class="service-history-card" data-verdict={verdict.state} data-phase={busy ? 'busy' : 'settled'} data-lead={lead} data-body={body}>
      <LookupHead title={locale.serviceHistory} verdict={verdict} />

      <div class="lookup-slide-clip">
        <div class="lookup-slide">
          <div id={LIST_ID}>
            <LookupTable
              columns={columns}
              rows={slotRows}
              slots={slots}
              // Keyed by slot, not by invoice: a slot is a place on the card that stays while its values change.
              rowKey={(_, index) => `slot-${index}`}
              lead={lead}
              notice={null}
              busy={busy}
              body={shownBody(body, retainedBody)}
              open={open}
              reshaping={reconfiguring}
              lang={locale.sharedLocales.lang}
              strings={tableStrings(locale)}
              // The drawer follows the invoice, not the slot: a lookup that brings the same record back keeps it open.
              actions={{ expandedKey, onToggle, keyOf: row => historyKey(row), closing }}
              drawer={row => <ServiceHistoryDrawer row={row} locale={locale} />}
              placeholder={{ kind: placeholder, retained: retainedPlaceholder, content: placeholderContent(locale) }}
            />
          </div>

          {/* The page controls. Always mounted, shut when the list fits on the smallest page: they slide with the body, never pop. */}
          <LookupPager
            strings={{ rowsPerPage: locale.rowsPerPage, pageAll: locale.pageAll, previousPage: locale.previousPage, nextPage: locale.nextPage, pageRange: locale.pageRange }}
            controls={LIST_ID}
            page={page}
            pageSize={pageSize}
            total={total}
            open={pagerOpen}
            busy={busy}
            onPage={onPage}
            onPageSize={onPageSize}
          />
        </div>
      </div>
    </section>
  );
};
