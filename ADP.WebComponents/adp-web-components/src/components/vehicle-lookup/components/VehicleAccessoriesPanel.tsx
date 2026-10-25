import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import accessoriesSchema from '~locales/vehicleLookup/accessories/type';
import { AccessoryDTO } from '~types/generated/vehicle-lookup/accessory-dto';

import { SearchIcon } from '~assets/search-icon';
import { PackageIcon } from '~assets/package-icon';
import { ChevronLeftIcon } from '~assets/chevron-left-icon';
import { ChevronRightIcon } from '~assets/chevron-right-icon';

import { LookupHead, PanelVerdict } from '~features/vehicle-info-layout';
import { LeadKind, LookupColumn, LookupTable, NoticeTone, RowSlot, shownBody, visibleColumns } from '~features/lookup-table';

export type AccessoriesLocale = InferType<typeof accessoriesSchema>;

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
  accessories?: AccessoryDTO[] | null;
  hidden: ReadonlySet<string>;
  /** The lookup failed: the sentence the strip says in the negative tone (the pill carries the same words). */
  error?: string;
};

export type PanelNotice = { tone: NoticeTone; text: string };

/** The hiddenFields names, in column order: the host's whole vocabulary for this panel. */
export const FIELDS = ['partNumber', 'description', 'image'] as const;

/** What a host sees without configuring anything: every column. */
export const DEFAULT_HIDDEN_FIELDS = '';

/**
 * The page sizes the reader can pick, in steps of five up to twenty-five, then every row. Five by
 * default (owner, 2026-10-25, replacing the "Show all" disclosure of 2026-10-18): a short card that
 * keeps its height from vehicle to vehicle, and never a list so long that a lookup has a screenful
 * of rows to move.
 */
export const PAGE_SIZES = [5, 10, 15, 20, 25] as const;

export type PageSize = (typeof PAGE_SIZES)[number] | 'all';

export const PAGE_SIZE_OPTIONS: PageSize[] = [...PAGE_SIZES, 'all'];

export const DEFAULT_PAGE_SIZE: PageSize = 5;

/** Where the reader's choice is kept, per browser: the panel opens on it next time. */
export const PAGE_SIZE_STORAGE_KEY = 'shift-components:vehicle-accessories:page-size';

// ---------------------------------------------------------------------------------------------
// Columns
// ---------------------------------------------------------------------------------------------

/** The panel's columns, all of them; hiddenFields takes some away (`visibleColumns`). */
export const accessoryColumns = (locale: AccessoriesLocale): LookupColumn<AccessoryDTO>[] => [
  { field: 'partNumber', label: locale.partNumber, kind: 'code', identity: true, value: accessory => accessory.partNumber },
  { field: 'description', label: locale.description, kind: 'text', grow: true, value: accessory => accessory.description },
  { field: 'image', label: locale.image, kind: 'media', value: accessory => accessory.image },
];

/** How many columns a hidden set leaves. Names the panel does not know are ignored. */
export const columnCount = (hidden: ReadonlySet<string>): number => FIELDS.filter(field => !hidden.has(field)).length;

/** Whether two hidden sets leave the same columns: an unknown name changing is not a layout change. */
export const sameColumns = (a: ReadonlySet<string>, b: ReadonlySet<string>): boolean => FIELDS.every(field => a.has(field) === b.has(field));

// ---------------------------------------------------------------------------------------------
// The three derivations (design language § 20): what the strip says, what the body holds, the notice
// ---------------------------------------------------------------------------------------------

/**
 * The rows the panel may list. An unauthorized vehicle has none whatever the DTO holds: the list
 * is not the distributor's to show (vehicle-lookup-invariants.md), and a fixture that carries one
 * is a data fault the panel does not follow.
 */
export const listed = (state: PanelState): AccessoryDTO[] => (state.vehicleLoaded && state.authorized !== false ? (state.accessories ?? []) : []);

/** Whether the vehicle has accessories on record. Judged on the whole list, not on the rows a disclosure shows. */
export const hasRecords = (state: PanelState): boolean => listed(state).length > 0;

/** `rows` when there is a list and at least one column to draw it in; `none` otherwise. The body shuts for `none`. */
export const panelBody = (state: PanelState): 'rows' | 'none' => (hasRecords(state) && columnCount(state.hidden) > 0 ? 'rows' : 'none');

/**
 * The strip's layer: always the column headings (they depend on hiddenFields alone, never on the
 * data). An outcome without rows is said by the pill, never by covering the headings, and there is no
 * band repeating it (owner, 2026-10-25). The one exception is a host that hid every column: there are
 * no headings to show, and the strip carries the sentence.
 */
export const panelLead = (state: PanelState, _busy: boolean): LeadKind => (columnCount(state.hidden) === 0 ? 'notice' : 'columns');

/**
 * The strip's sentence when there is nothing to list. Never a verdict: an unauthorized vehicle is
 * amber ("not in the records"), an authorized one with nothing on file is grey and says only that,
 * and a host that hid every column is told so in grey -- before a lookup too, since the headings it
 * would otherwise show do not exist. A failed lookup is the one negative sentence. Null above rows
 * and before a lookup.
 */
export const leadNotice = (state: PanelState, locale: AccessoriesLocale): PanelNotice | null => {
  if (state.error) return { tone: 'negative', text: state.error };
  if (!state.vehicleLoaded) return columnCount(state.hidden) === 0 ? { tone: 'idle', text: locale.noFieldsShown } : null;
  if (state.authorized === false) return { tone: 'neutral', text: locale.unauthorizedNotice };
  if (!hasRecords(state)) return { tone: 'idle', text: locale.noAccessories };
  if (columnCount(state.hidden) === 0) return { tone: 'idle', text: locale.noFieldsShown };
  return null;
};

/**
 * What fills the space under the strip when there is no list: `idle` before any lookup, after a failed
 * one and for a vehicle not in the distributor's records ("look up a vehicle" -- the pill says what
 * happened), `empty` for an authorized vehicle with nothing on file (what would be listed here), and
 * nothing when a host hid every column, where there is no list to describe. It holds through a lookup (the
 * state is still the last one), so the space does not collapse and reopen while the answer is on its way.
 * Owner, 2026-10-25: a strip over a blank band read as "nothing is shown".
 */
export const panelPlaceholder = (state: PanelState): 'idle' | 'empty' | null => {
  if (columnCount(state.hidden) === 0) return null;
  // Before a lookup, after a failed one, and for a vehicle not in the distributor's records: the pill
  // says what happened, and the table is in its idle look (owner, 2026-10-25).
  if (!state.vehicleLoaded || state.authorized === false) return 'idle';
  if (!hasRecords(state)) return 'empty';
  return null;
};

// ---------------------------------------------------------------------------------------------
// The count, in real plural forms
// ---------------------------------------------------------------------------------------------

/**
 * The total, only when there is something to count: loaded, the distributor's own, at least one
 * accessory. Never for idle, unauthorized, an error or an empty list: "0 accessories" would be a
 * claim, and the empty list already has its statement. It does not move with hiddenFields or with
 * the disclosure: it is the vehicle's total, not the rows on screen.
 */
export const totalCount = (state: PanelState): number | null => {
  const total = listed(state).length;
  return total > 0 ? total : null;
};

type PluralKey = 'accessoryCountZero' | 'accessoryCountOne' | 'accessoryCountTwo' | 'accessoryCountFew' | 'accessoryCountMany' | 'accessoryCountOther';

const PLURAL_KEYS: Record<Intl.LDMLPluralRule, PluralKey> = {
  zero: 'accessoryCountZero',
  one: 'accessoryCountOne',
  two: 'accessoryCountTwo',
  few: 'accessoryCountFew',
  many: 'accessoryCountMany',
  other: 'accessoryCountOther',
};

/** The language's plural category for a whole number; `one` / `other` if the runtime does not know the language. */
export const pluralCategory = (count: number, language: string): Intl.LDMLPluralRule => {
  try {
    return new Intl.PluralRules(language).select(count);
  } catch {
    return count === 1 ? 'one' : 'other';
  }
};

/** "9 accessories", in the locale's form for that number. A category the locale left blank falls back to `other`. */
export const countText = (count: number, locale: AccessoriesLocale, language: string): string => {
  const template = locale[PLURAL_KEYS[pluralCategory(count, language)]] || locale.accessoryCountOther;
  return template.replace('{count}', String(count));
};

// ---------------------------------------------------------------------------------------------
// Pages
// ---------------------------------------------------------------------------------------------

/** A stored or chosen value read back as a page size; anything else is the default. */
export const parsePageSize = (raw: string | number | null | undefined): PageSize => {
  if (raw === 'all') return 'all';
  const size = Number(raw);
  return (PAGE_SIZES as readonly number[]).includes(size) ? (size as PageSize) : DEFAULT_PAGE_SIZE;
};

/** How many pages a list makes; at least one, so an empty list is page 1 of 1, never page 1 of 0. */
export const pageCount = (total: number, size: PageSize): number => (size === 'all' ? 1 : Math.max(1, Math.ceil(total / size)));

/** A page index brought back inside the list: a smaller list or a bigger page can leave it past the end. */
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

/** "6–10 of 23": the rows on screen and the whole list, in the locale's words. */
export const pageRangeText = (page: number, size: PageSize, total: number, locale: AccessoriesLocale): string => {
  const from = total === 0 ? 0 : (size === 'all' ? 0 : clampPage(page, total, size) * size) + 1;
  const to = size === 'all' ? total : Math.min(total, from - 1 + size);
  return locale.pageRange.replace('{from}', String(from)).replace('{to}', String(to)).replace('{total}', String(total));
};

/**
 * Whether a page change that takes `removedHeight` off the list needs the page to follow. The
 * controls are under the rows, so they rise by what the rows took; if that lifts them off the top
 * of the screen, the reader is left looking at nothing and the controls are brought back
 * (motion.md rule 10).
 */
export const pagingNeedsReveal = (controlsTop: number, removedHeight: number): boolean => controlsTop - removedHeight < 0;

// ---------------------------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------------------------

type Props = {
  locale: AccessoriesLocale;
  /** The language key, for plural rules. */
  language: string;
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
  /** The strip's last sentence, kept so a notice that is leaving fades out with its words, not blank. */
  retainedNotice: PanelNotice | null;
  /** The rows in the table's slots: the page on screen, then any rows that are leaving (the owner's). */
  slotRows: AccessoryDTO[];
  /** One per slot row: open or shut, and how its values are veiled (the owner's `tableSlots`). */
  slots: RowSlot[];
  page: number;
  pageSize: PageSize;
  onPage: (page: number) => void;
  onPageSize: (size: PageSize) => void;
  onOpenMedia?: (image: HTMLImageElement, src: string) => void;
};

/** Whether the body is open on rows: a list to show, not shutting for a clear, not swapping columns. In flight it stays open. */
export const bodyOpen = (state: PanelState, leaving: boolean, reconfiguring: boolean): boolean => !leaving && !reconfiguring && panelBody(state) === 'rows';

export const VehicleAccessoriesPanel: FunctionalComponent<Props> = props => {
  const { locale, language, state, verdict, busy, leaving, reconfiguring, retainedBody, retainedNotice, slotRows, slots, page, pageSize, onPage, onPageSize, onOpenMedia } = props;

  const columns = visibleColumns(accessoryColumns(locale), state.hidden);
  const total = listed(state).length;
  const count = totalCount(state);

  const body = panelBody(state);
  const lead = panelLead(state, busy);
  const open = bodyOpen(state, leaving, reconfiguring);
  const pagerOpen = open && isPaginated(total);
  const pages = pageCount(total, pageSize);
  const current = clampPage(page, total, pageSize);

  const notice = leadNotice(state, locale) ?? retainedNotice;
  // Leaving (a clear, or an error raised on a settled card) always ends idle, so the placeholder is
  // the idle one for the whole leave: from idle or an empty list it stays open (its words cross-fade
  // at most) instead of shutting and reopening, and from rows it opens while the rows shut, as one movement.
  const placeholder = leaving && columnCount(state.hidden) > 0 ? 'idle' : panelPlaceholder(state);

  return (
    <section class="accessories-card" data-verdict={verdict.state} data-phase={busy ? 'busy' : 'settled'} data-lead={lead} data-body={body}>
      <LookupHead title={locale.vehicleAccessories} verdict={verdict}>
        {/* A fact, not a verdict: it rides in the head's content block, which leaves and returns with every lookup, so it never changes in view. */}
        {count !== null && (
          <span class="accessories-count" data-count={String(count)}>
            {countText(count, locale, language)}
          </span>
        )}
      </LookupHead>

      <div class="lookup-slide-clip">
        <div class="lookup-slide">
          <div id="accessories-list">
            <LookupTable
              columns={columns}
              rows={slotRows}
              slots={slots}
              // Keyed by slot, not by part: a slot is a place on the card that stays while its values change.
              rowKey={(_, index) => `slot-${index}`}
              lead={lead}
              notice={notice}
              busy={busy}
              body={shownBody(body, retainedBody)}
              open={open}
              reshaping={reconfiguring}
              strings={{ columnsLabel: locale.columnsLabel, noImage: locale.noImage, imageFailed: locale.imageFailed, openImage: locale.openImage }}
              onOpenMedia={onOpenMedia}
            />
          </div>

          {/* The placeholder under the strip when there is no list (panelPlaceholder). Always mounted; it
              slides open and shut, and its two sentences cross-fade, so it never pops. Its words go
              under the skeleton while a lookup is in flight: "look a vehicle up" is not true then. */}
          <div class="accessories-placeholder collapsible" data-open={placeholder ? 'true' : 'false'} aria-hidden={placeholder ? null : 'true'}>
            <div class="collapsible-body">
              <div class="accessories-placeholder-stack layer-stack">
                <div class="layer accessories-placeholder-layer" data-active={placeholder === 'empty' ? 'false' : 'true'} aria-hidden={placeholder === 'idle' ? null : 'true'}>
                  <span class="accessories-placeholder-mark">
                    <SearchIcon class="accessories-placeholder-glyph" />
                  </span>
                  <p class="accessories-placeholder-text shift-skeleton">{locale.idleHint}</p>
                </div>
                <div class="layer accessories-placeholder-layer" data-active={placeholder === 'empty' ? 'true' : 'false'} aria-hidden={placeholder === 'empty' ? null : 'true'}>
                  <span class="accessories-placeholder-mark">
                    <PackageIcon class="accessories-placeholder-glyph" />
                  </span>
                  <p class="accessories-placeholder-text shift-skeleton">{locale.emptyHint}</p>
                </div>
              </div>
            </div>
          </div>

          {/* The page controls. Always mounted, shut when the list fits on the smallest page: they slide with the body, never pop. */}
          <div class="accessories-pager collapsible" data-open={pagerOpen ? 'true' : 'false'} aria-hidden={pagerOpen ? null : 'true'}>
            <div class="collapsible-body">
              <div class="accessories-pager-row">
                <label class="accessories-page-size">
                  <span class="accessories-page-size-label">{locale.rowsPerPage}</span>
                  <select
                    class="accessories-page-size-select"
                    aria-controls="accessories-list"
                    disabled={busy}
                    onChange={event => onPageSize(parsePageSize((event.target as HTMLSelectElement).value))}
                  >
                    {PAGE_SIZE_OPTIONS.map(size => (
                      <option value={String(size)} selected={size === pageSize}>
                        {size === 'all' ? locale.pageAll : String(size)}
                      </option>
                    ))}
                  </select>
                </label>

                {/* A value like any other: it goes under the skeleton with the rows while a lookup is in flight. */}
                <span class="accessories-page-range shift-skeleton" aria-live="polite">
                  {pageRangeText(current, pageSize, total, locale)}
                </span>

                <span class="accessories-page-nav">
                  <button
                    type="button"
                    class="accessories-page-button is-previous"
                    aria-label={locale.previousPage}
                    title={locale.previousPage}
                    aria-controls="accessories-list"
                    disabled={busy || current === 0}
                    onClick={() => onPage(current - 1)}
                  >
                    <ChevronLeftIcon class="accessories-page-chevron" />
                  </button>
                  <button
                    type="button"
                    class="accessories-page-button is-next"
                    aria-label={locale.nextPage}
                    title={locale.nextPage}
                    aria-controls="accessories-list"
                    disabled={busy || current >= pages - 1}
                    onClick={() => onPage(current + 1)}
                  >
                    <ChevronRightIcon class="accessories-page-chevron" />
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
