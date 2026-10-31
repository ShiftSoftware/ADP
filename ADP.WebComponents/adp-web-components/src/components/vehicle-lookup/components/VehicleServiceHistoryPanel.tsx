import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import serviceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import { SearchIcon } from '~assets/search-icon';
import { CalendarDaysIcon } from '~assets/calendar-days-icon';
import { ChevronLeftIcon } from '~assets/chevron-left-icon';
import { ChevronRightIcon } from '~assets/chevron-right-icon';

import { SharedLocales } from '~features/multi-lingual';
import { LookupHead, PanelVerdict, recordVerdict } from '~features/vehicle-info-layout';
import { LeadKind, LookupColumn, LookupTable, RowSlot, shownBody } from '~features/lookup-table';

import { ServiceHistoryDrawer } from './ServiceHistoryDrawer';

export type ServiceHistoryLocale = InferType<typeof serviceHistorySchema>;

/** Everything the panel is decided from, assembled by the owner. */
export type PanelState = {
  /** A vehicle is on screen and its lookup did not fail. */
  vehicleLoaded: boolean;
  /** `isAuthorized` of that vehicle; `false` means the distributor has no record of it. */
  authorized?: boolean;
  /** The DTO's list, whatever it holds: a list the distributor may not show is dropped by `listed`. */
  visits?: VehicleServiceHistoryDTO[] | null;
  /** The lookup failed: the sentence the pill carries in the negative tone. */
  error?: string;
};

export const PAGE_SIZES = [5, 10, 15, 20, 25] as const;

export type PageSize = (typeof PAGE_SIZES)[number] | 'all';

export const PAGE_SIZE_OPTIONS: PageSize[] = [...PAGE_SIZES, 'all'];

export const DEFAULT_PAGE_SIZE: PageSize = 5;

export const PAGE_SIZE_STORAGE_KEY = 'shift-components:vehicle-service-history:page-size:v2';

// ---------------------------------------------------------------------------------------------
// Columns
// ---------------------------------------------------------------------------------------------

/** Latin digits with grouping in every locale, like the VINs and dates beside them: "71,828". */
const MILEAGE_FORMAT = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 });

/** ISO `yyyy-MM-dd`, as the DTO sends it, tolerant of a full timestamp. */
export const visitDate = (visit: VehicleServiceHistoryDTO): string | null => visit.serviceDate?.slice(0, 10) ?? null;

/** The odometer, grouped. 0 is a reading; only a missing one is blank. */
export const visitMileage = (visit: VehicleServiceHistoryDTO): string | null =>
  typeof visit.mileage === 'number' && Number.isFinite(visit.mileage) ? MILEAGE_FORMAT.format(visit.mileage) : null;

// Matches the live sites; a client asked for this order.
export const serviceHistoryColumns = (locale: ServiceHistoryLocale): LookupColumn<VehicleServiceHistoryDTO>[] => [
  { field: 'branchName', label: locale.branch, kind: 'text', value: visit => visit.branchName },
  { field: 'companyName', label: locale.dealer, kind: 'text', value: visit => visit.companyName },
  { field: 'invoiceNumber', label: locale.invoiceNumber, kind: 'code', value: visit => visit.invoiceNumber },
  { field: 'serviceDate', label: locale.date, kind: 'date', identity: true, value: visitDate },
  { field: 'serviceType', label: locale.serviceType, kind: 'text', grow: true, value: visit => visit.serviceType },
  { field: 'mileage', label: locale.odometer, kind: 'number', value: visitMileage },
];

// ---------------------------------------------------------------------------------------------
// The derivations (design language § 20)
// ---------------------------------------------------------------------------------------------

export const listed = (state: PanelState): VehicleServiceHistoryDTO[] => (state.vehicleLoaded ? (state.visits ?? []) : []);

export const hasRecords = (state: PanelState): boolean => listed(state).length > 0;

/** The pill and the accent bar: a record panel's (`recordVerdict`), judged on the visits it may list. */
export const panelVerdict = (state: PanelState, shared: SharedLocales): PanelVerdict =>
  recordVerdict({ locale: shared, error: state.error, vehicleLoaded: state.vehicleLoaded, authorized: state.authorized, hasRecords: hasRecords(state) });

/** `rows` when there are visits to list; `none` otherwise, and the body shuts. */
export const panelBody = (state: PanelState): 'rows' | 'none' => (hasRecords(state) ? 'rows' : 'none');

/** The strip always carries the headings: the columns are fixed, and an outcome without rows is said by the pill (owner, 2026-10-25). */
export const panelLead = (): LeadKind => 'columns';

/** Whether the body is open on rows. In flight it stays open: the rows stay under the skeleton (§ 21a). */
export const bodyOpen = (state: PanelState, leaving: boolean): boolean => !leaving && panelBody(state) === 'rows';

/**
 * What fills the space under the strip when there is no list: `idle` before a lookup and after a
 * failed one (the pill says what happened); `empty` for a vehicle with no visits on file. It holds
 * through a lookup.
 */
export const panelPlaceholder = (state: PanelState): 'idle' | 'empty' | null => {
  if (!state.vehicleLoaded) return 'idle';
  return hasRecords(state) ? null : 'empty';
};

// ---------------------------------------------------------------------------------------------
// The count, in real plural forms
// ---------------------------------------------------------------------------------------------

/** The number of visits, only when there is one to count: "0 visits" would be a claim the empty state already makes. */
export const totalCount = (state: PanelState): number | null => {
  const total = listed(state).length;
  return total > 0 ? total : null;
};

type PluralKey = 'visitCountZero' | 'visitCountOne' | 'visitCountTwo' | 'visitCountFew' | 'visitCountMany' | 'visitCountOther';

const PLURAL_KEYS: Record<Intl.LDMLPluralRule, PluralKey> = {
  zero: 'visitCountZero',
  one: 'visitCountOne',
  two: 'visitCountTwo',
  few: 'visitCountFew',
  many: 'visitCountMany',
  other: 'visitCountOther',
};

/** The language's plural category for a whole number; `one` / `other` if the runtime does not know the language. */
export const pluralCategory = (count: number, language: string): Intl.LDMLPluralRule => {
  try {
    return new Intl.PluralRules(language).select(count);
  } catch {
    return count === 1 ? 'one' : 'other';
  }
};

/** "51 visits", in the locale's form for that number. A category the locale left blank falls back to `other`. */
export const countText = (count: number, locale: ServiceHistoryLocale, language: string): string => {
  const template = locale[PLURAL_KEYS[pluralCategory(count, language)]] || locale.visitCountOther;
  return template.replace('{count}', String(count));
};

/** The drawer toggle's name, which says which visit it opens: by its date, or "this visit" when it has none. */
export const toggleLabel = (visit: VehicleServiceHistoryDTO, open: boolean, locale: ServiceHistoryLocale): string => {
  const date = visitDate(visit);
  if (date === null) return open ? locale.hideDetailsUndated : locale.showDetailsUndated;
  return (open ? locale.hideDetails : locale.showDetails).replace('{date}', date);
};

// ---------------------------------------------------------------------------------------------
// Pages: restated from the accessories panel, not shared with it (owner's scope rule)
// ---------------------------------------------------------------------------------------------

/** A stored or chosen value read back as a page size; anything else is the default. */
export const parsePageSize = (raw: string | number | null | undefined): PageSize => {
  if (raw === 'all') return 'all';
  const size = Number(raw);
  return (PAGE_SIZES as readonly number[]).includes(size) ? (size as PageSize) : DEFAULT_PAGE_SIZE;
};

/** How many pages a list makes; at least one, so an empty list is page 1 of 1, never page 1 of 0. */
export const pageCount = (total: number, size: PageSize): number => (size === 'all' ? 1 : Math.max(1, Math.ceil(total / size)));

/** A page index brought back inside the list: a shorter list or a bigger page can leave it past the end. */
export const clampPage = (page: number, total: number, size: PageSize): number => Math.min(Math.max(0, Math.floor(page) || 0), pageCount(total, size) - 1);

/** The rows of one page. */
export const pageRows = <Row,>(rows: Row[], page: number, size: PageSize): Row[] => {
  if (size === 'all') return rows;
  const start = clampPage(page, rows.length, size) * size;
  return rows.slice(start, start + size);
};

/** Whether the list needs the page controls: longer than the smallest page. */
export const isPaginated = (total: number): boolean => total > PAGE_SIZES[0];

/** The page that keeps the first row on screen in view when the page size changes. */
export const pageForSize = (page: number, from: PageSize, to: PageSize, total: number): number => {
  if (to === 'all' || from === 'all') return 0;
  return clampPage(Math.floor((clampPage(page, total, from) * from) / to), total, to);
};

/** "6–10 of 51": the visits on screen and the whole list, in the locale's words. */
export const pageRangeText = (page: number, size: PageSize, total: number, locale: ServiceHistoryLocale): string => {
  const from = total === 0 ? 0 : (size === 'all' ? 0 : clampPage(page, total, size) * size) + 1;
  const to = size === 'all' ? total : Math.min(total, from - 1 + size);
  return locale.pageRange.replace('{from}', String(from)).replace('{to}', String(to)).replace('{total}', String(total));
};

// ---------------------------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------------------------

type Props = {
  locale: ServiceHistoryLocale;
  /** The language key, for plural rules. */
  language: string;
  state: PanelState;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  /** A lookup is in flight or the panel is leaving: the head leaves; rows on screen go under the skeleton where they stand. */
  busy: boolean;
  /** The panel is leaving (cleared, or failing from a settled card): the body shuts over its rows. */
  leaving: boolean;
  /** The body's last kind, kept a settle after it shut so its rows slide away instead of blinking out. */
  retainedBody: string;
  /** The rows in the table's slots: the visits on screen, then any that are leaving (the owner's). */
  slotRows: VehicleServiceHistoryDTO[];
  /** One per slot row (the owner's `tableSlots`). */
  slots: RowSlot[];
  /** The slot whose drawer is open, and the one still shutting (the owner's). */
  openKey: string | null;
  retainedKey: string | null;
  onToggleDrawer: (key: string) => void;
  pagination: boolean;
  page: number;
  pageSize: PageSize;
  onPage: (page: number) => void;
  onPageSize: (size: PageSize) => void;
};

export const VehicleServiceHistoryPanel: FunctionalComponent<Props> = ({
  locale,
  language,
  state,
  verdict,
  busy,
  leaving,
  retainedBody,
  slotRows,
  slots,
  openKey,
  retainedKey,
  onToggleDrawer,
  pagination,
  page,
  pageSize,
  onPage,
  onPageSize,
}) => {
  const body = panelBody(state);
  const lead = panelLead();
  const open = bodyOpen(state, leaving);
  const count = totalCount(state);
  const total = listed(state).length;
  const pagerOpen = pagination && open && isPaginated(total);
  const pages = pageCount(total, pageSize);
  const current = clampPage(page, total, pageSize);
  // A leave always ends idle, so the placeholder is the idle one for the whole leave: from rows it
  // opens while they shut, as one movement.
  const placeholder = leaving ? 'idle' : panelPlaceholder(state);

  return (
    <section class="service-history-card" data-verdict={verdict.state} data-phase={busy ? 'busy' : 'settled'} data-lead={lead} data-body={body}>
      <LookupHead title={locale.serviceHistory} verdict={verdict}>
        {/* A fact, not a verdict: it rides in the head's content block, which leaves and returns with every lookup. */}
        {count !== null && (
          <span class="service-history-count" data-count={String(count)}>
            {countText(count, locale, language)}
          </span>
        )}
      </LookupHead>

      <div class="lookup-slide-clip">
        <div class="lookup-slide">
          <div id="service-history-list">
            <LookupTable
              columns={serviceHistoryColumns(locale)}
              rows={slotRows}
              slots={slots}
              // Keyed by slot, not by visit: a slot is a place on the card that stays while its values change.
              rowKey={(_, index) => `slot-${index}`}
              lead={lead}
              notice={null}
              busy={busy}
              body={shownBody(body, retainedBody)}
              open={open}
              // No media column, so the picture strings are never read.
              strings={{ columnsLabel: locale.columnsLabel, noImage: '', imageFailed: '', openImage: '' }}
              drawer={{
                openKey,
                retainedKey,
                render: visit => <ServiceHistoryDrawer visit={visit} locale={locale} />,
                label: locale.visitDetails,
                toggleLabel: (visit, open) => toggleLabel(visit, open, locale),
                onToggle: onToggleDrawer,
              }}
            />
          </div>

          {/* Always mounted: it slides open and shut and its two sentences cross-fade. Its words go under
              the skeleton in flight, where "look a vehicle up" is not true. */}
          <div class="service-history-placeholder collapsible" data-open={placeholder ? 'true' : 'false'} aria-hidden={placeholder ? null : 'true'}>
            <div class="collapsible-body">
              <div class="service-history-placeholder-stack layer-stack">
                <div class="layer service-history-placeholder-layer" data-active={placeholder === 'empty' ? 'false' : 'true'} aria-hidden={placeholder === 'idle' ? null : 'true'}>
                  <span class="service-history-placeholder-mark" aria-hidden="true">
                    <SearchIcon class="service-history-placeholder-glyph" />
                  </span>
                  <p class="service-history-placeholder-text shift-skeleton">{locale.idleHint}</p>
                </div>
                <div class="layer service-history-placeholder-layer" data-active={placeholder === 'empty' ? 'true' : 'false'} aria-hidden={placeholder === 'empty' ? null : 'true'}>
                  <span class="service-history-placeholder-mark" aria-hidden="true">
                    <CalendarDaysIcon class="service-history-placeholder-glyph" />
                  </span>
                  <p class="service-history-placeholder-text shift-skeleton">{locale.emptyHint}</p>
                </div>
              </div>
            </div>
          </div>

          {/* The page controls. Always mounted, shut when the list fits on the smallest page: they slide with the body, never pop. */}
          <div class="service-history-pager collapsible" data-open={pagerOpen ? 'true' : 'false'} aria-hidden={pagerOpen ? null : 'true'}>
            <div class="collapsible-body">
              <div class="service-history-pager-row">
                <label class="service-history-page-size">
                  <span class="service-history-page-size-label">{locale.rowsPerPage}</span>
                  <span class="service-history-page-size-field">
                    <select
                      class="service-history-page-size-select"
                      aria-controls="service-history-list"
                      disabled={busy}
                      onChange={event => onPageSize(parsePageSize((event.target as HTMLSelectElement).value))}
                    >
                      {PAGE_SIZE_OPTIONS.map(size => (
                        <option value={String(size)} selected={size === pageSize}>
                          {size === 'all' ? locale.pageAll : String(size)}
                        </option>
                      ))}
                    </select>
                    <span class="service-history-page-size-caret" aria-hidden="true">
                      <ChevronRightIcon class="service-history-page-size-chevron" />
                    </span>
                  </span>
                </label>

                {/* A value like any other: it goes under the skeleton with the rows while a lookup is in flight. */}
                <span class="service-history-page-range shift-skeleton" aria-live="polite">
                  {pageRangeText(current, pageSize, total, locale)}
                </span>

                <span class="service-history-page-nav">
                  <button
                    type="button"
                    class="service-history-page-button is-previous"
                    aria-label={locale.previousPage}
                    title={locale.previousPage}
                    aria-controls="service-history-list"
                    disabled={busy || current === 0}
                    onClick={() => onPage(current - 1)}
                  >
                    <ChevronLeftIcon class="service-history-page-chevron" />
                  </button>
                  <button
                    type="button"
                    class="service-history-page-button is-next"
                    aria-label={locale.nextPage}
                    title={locale.nextPage}
                    aria-controls="service-history-list"
                    disabled={busy || current >= pages - 1}
                    onClick={() => onPage(current + 1)}
                  >
                    <ChevronRightIcon class="service-history-page-chevron" />
                  </button>
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
