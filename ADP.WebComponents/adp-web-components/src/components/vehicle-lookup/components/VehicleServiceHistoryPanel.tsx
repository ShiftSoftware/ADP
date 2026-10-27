import { FunctionalComponent, h } from '@stencil/core';

import { EmptyTableIcon } from '~assets/empty-table-icon';
import { QuestionMark } from '~assets/question-mark';
import { SearchIcon } from '~assets/search-icon';
import { TriangleAlertIcon } from '~assets/triangle-alert';
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

export type ServiceHistoryLocale = ComponentLocale<typeof ServiceHistorySchema>;

export type PanelState = {
  vehicleLoaded: boolean;
  authorized?: boolean;
  history?: VehicleServiceHistoryDTO[] | null;
  hidden: ReadonlySet<string>;
  error?: string;
};

export const FIELDS = ['branchName', 'companyName', 'jobNumber', 'serviceType', 'mileage'] as const;

export const DEFAULT_HIDDEN_FIELDS = 'jobNumber';

export const PAGE_SIZE_STORAGE_KEY = 'shift-components:vehicle-service-history:page-size';

export const LIST_ID = 'service-history-list';

export const clean = (value: string | null | undefined): string | null => {
  const text = (value ?? '').trim();
  return text === '' ? null : text;
};

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

export const laborColumns = (locale: ServiceHistoryLocale): LookupColumn<VehicleLaborDTO>[] => [
  { field: 'laborCode', label: locale.laborCode, kind: 'code', identity: true, value: line => line.laborCode },
  { field: 'serviceDescription', label: locale.serviceDescription, kind: 'text', grow: true, value: line => line.serviceDescription },
  { field: 'packageCode', label: locale.packageCode, kind: 'code', value: line => line.packageCode },
  { field: 'serviceCode', label: locale.serviceCode, kind: 'code', value: line => line.serviceCode },
];

export const partColumns = (locale: ServiceHistoryLocale): LookupColumn<VehiclePartDTO>[] => [
  { field: 'partNumber', label: locale.partNumber, kind: 'code', identity: true, value: line => line.partNumber },
  { field: 'partDescription', label: locale.partDescription, kind: 'text', grow: true, value: line => line.partDescription },
  { field: 'packageCode', label: locale.packageCode, kind: 'code', value: line => line.packageCode },
  { field: 'qty', label: locale.qty, kind: 'number', value: line => line.qty },
];

// The invoice, the date and the expand button are never hidden.
export const columnCount = (hidden: ReadonlySet<string>): number => FIELDS.filter(field => !hidden.has(field)).length + 3;

export const hideableOnly = (hidden: ReadonlySet<string>): ReadonlySet<string> => new Set(FIELDS.filter(field => hidden.has(field)));

export const sameColumns = (a: ReadonlySet<string>, b: ReadonlySet<string>): boolean => FIELDS.every(field => a.has(field) === b.has(field));

/** One invoice number can recur across dealers and branches, so the key takes all four. */
export const historyKey = (row: VehicleServiceHistoryDTO): string => [row.companyName, row.branchName, row.invoiceNumber, row.jobNumber].map(part => clean(part) ?? '').join('|');

/** Some systems send "0" for no parent invoice. */
export const realParentInvoice = (row: VehicleServiceHistoryDTO): string | null => {
  const parent = clean(row.parentInvoiceNumber);
  return parent === null || parent === '0' ? null : parent;
};

export const accountNumber = (row: VehicleServiceHistoryDTO): string | null => clean(row.accountNumber);

export type MetaItem = { key: 'jobNumber' | 'parentInvoice' | 'accountNumber'; label: string; value: string };

export const metaItems = (row: VehicleServiceHistoryDTO, locale: ServiceHistoryLocale): MetaItem[] =>
  [
    { key: 'jobNumber' as const, label: locale.jobNumber, value: clean(row.jobNumber) },
    { key: 'parentInvoice' as const, label: locale.parentInvoice, value: realParentInvoice(row) },
    { key: 'accountNumber' as const, label: locale.accountNumber, value: accountNumber(row) },
  ].filter((item): item is MetaItem => item.value !== null);

/** A vehicle not in the records never shows a list, even when the DTO carries one. */
export const listed = (state: PanelState): VehicleServiceHistoryDTO[] => (state.vehicleLoaded && state.authorized !== false ? (state.history ?? []) : []);

export const hasRecords = (state: PanelState): boolean => listed(state).length > 0;

export const panelBody = (state: PanelState): 'rows' | 'none' => (hasRecords(state) && columnCount(state.hidden) > 0 ? 'rows' : 'none');

export const panelLead = (state: PanelState): LeadKind => (columnCount(state.hidden) === 0 ? 'notice' : 'columns');

export const panelPlaceholder = (state: PanelState, leaving = false): PlaceholderKind | null =>
  placeholderKind({
    columns: columnCount(state.hidden),
    loaded: state.vehicleLoaded,
    failed: !!state.error,
    authorized: state.authorized,
    records: listed(state).length,
    leaving,
  });

export const placeholderContent = (locale: ServiceHistoryLocale): TablePlaceholder['content'] => ({
  idle: { icon: SearchIcon, text: locale.idleHint },
  empty: { icon: EmptyTableIcon, text: locale.emptyHint },
  error: { icon: TriangleAlertIcon, text: locale.errorHint },
  unauthorized: { icon: QuestionMark, text: locale.unauthorizedHint },
});

export const bodyOpen = (state: PanelState, leaving: boolean, reconfiguring: boolean): boolean => !leaving && !reconfiguring && panelBody(state) === 'rows';

const tableStrings = (locale: ServiceHistoryLocale) => ({
  columnsLabel: locale.columnsLabel,
  noImage: '',
  imageFailed: '',
  openImage: '',
  expandRow: locale.expandRow,
  collapseRow: locale.collapseRow,
});

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

type Props = {
  locale: ServiceHistoryLocale;
  state: PanelState;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  busy: boolean;
  leaving: boolean;
  reconfiguring: boolean;
  retainedBody: string;
  retainedPlaceholder: PlaceholderKind | null;
  slotRows: VehicleServiceHistoryDTO[];
  slots: RowSlot[];
  expandedKey: string | null;
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
              rowKey={(_, index) => `slot-${index}`}
              lead={lead}
              notice={null}
              busy={busy}
              body={shownBody(body, retainedBody)}
              open={open}
              reshaping={reconfiguring}
              lang={locale.sharedLocales.lang}
              strings={tableStrings(locale)}
              actions={{ expandedKey, onToggle, keyOf: row => historyKey(row), closing }}
              drawer={row => <ServiceHistoryDrawer row={row} locale={locale} />}
              placeholder={{ kind: placeholder, retained: retainedPlaceholder, content: placeholderContent(locale) }}
            />
          </div>

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
