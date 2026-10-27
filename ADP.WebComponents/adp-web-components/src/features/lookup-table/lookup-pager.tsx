import { FunctionalComponent, h } from '@stencil/core';

import { ChevronLeftIcon } from '~assets/chevron-left-icon';
import { ChevronRightIcon } from '~assets/chevron-right-icon';

/**
 * The lookup pager: the page controls under a <LookupTable> (rows per page, the range, previous and
 * next) and the pure derivations a panel pages its rows with. Its stylesheet fragment is
 * lookup-pager.css; a panel @imports it after lookup-table.css. The owner keeps the page and size
 * state, the storage key and the swap choreography; this file owns the vocabulary.
 */

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

/** A stored or chosen value read back as a page size; anything else is the default. */
export const parsePageSize = (raw: string | number | null | undefined): PageSize => {
  if (raw === 'all') return 'all';
  const size = Number(raw);
  return (PAGE_SIZES as readonly number[]).includes(size) ? (size as PageSize) : DEFAULT_PAGE_SIZE;
};

/**
 * The reader's stored page size under `key`. Storage can be missing or throw (private windows,
 * blocked site data); the default stands then.
 */
export const readStoredPageSize = (key: string): PageSize => {
  try {
    return parsePageSize(globalThis.localStorage?.getItem(key));
  } catch {
    return DEFAULT_PAGE_SIZE;
  }
};

/** Keeps the reader's page size under `key`; when storage refuses, the choice lasts as long as the panel is on the page. */
export const storePageSize = (key: string, size: PageSize): void => {
  try {
    globalThis.localStorage?.setItem(key, String(size));
  } catch {
    // Not kept: the choice still applies for as long as the panel is on the page.
  }
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

/** "6–10 of 23": the rows on screen and the whole list, in the locale's words (`template` carries {from}, {to} and {total}). */
export const pageRangeText = (page: number, size: PageSize, total: number, template: string): string => {
  const from = total === 0 ? 0 : (size === 'all' ? 0 : clampPage(page, total, size) * size) + 1;
  const to = size === 'all' ? total : Math.min(total, from - 1 + size);
  return template.replace('{from}', String(from)).replace('{to}', String(to)).replace('{total}', String(total));
};

/**
 * Whether a page change that takes `removedHeight` off the list needs the page to follow. The
 * controls are under the rows, so they rise by what the rows took; if that lifts them off the top
 * of the screen, the reader is left looking at nothing and the controls are brought back
 * (motion.md rule 10).
 */
export const pagingNeedsReveal = (controlsTop: number, removedHeight: number): boolean => controlsTop - removedHeight < 0;

export type LookupPagerStrings = {
  rowsPerPage: string;
  pageAll: string;
  previousPage: string;
  nextPage: string;
  /** Carries {from}, {to} and {total}. */
  pageRange: string;
};

export type LookupPagerProps = {
  strings: LookupPagerStrings;
  /** The id of the element the controls page (the table's wrapper), for aria-controls. */
  controls: string;
  page: number;
  pageSize: PageSize;
  /** The whole list's length, not the page's. */
  total: number;
  /** The controls are on screen: the body is open on a list longer than the smallest page. Shut, they slide away; they are always mounted. */
  open: boolean;
  /** A lookup is in flight: the list on screen is the last vehicle's, so it cannot be paged. */
  busy: boolean;
  onPage: (page: number) => void;
  onPageSize: (size: PageSize) => void;
};

/** The page controls. Always mounted, shut when `open` is false: they slide with the body, never pop. */
export const LookupPager: FunctionalComponent<LookupPagerProps> = ({ strings, controls, page, pageSize, total, open, busy, onPage, onPageSize }) => {
  const pages = pageCount(total, pageSize);
  const current = clampPage(page, total, pageSize);

  return (
    <div class="lt-pager collapsible" data-open={open ? 'true' : 'false'} aria-hidden={open ? null : 'true'}>
      <div class="collapsible-body">
        <div class="lt-pager-row">
          <label class="lt-page-size">
            <span class="lt-page-size-label">{strings.rowsPerPage}</span>
            <select class="lt-page-size-select" aria-controls={controls} disabled={busy} onChange={event => onPageSize(parsePageSize((event.target as HTMLSelectElement).value))}>
              {PAGE_SIZE_OPTIONS.map(size => (
                <option value={String(size)} selected={size === pageSize}>
                  {size === 'all' ? strings.pageAll : String(size)}
                </option>
              ))}
            </select>
          </label>

          {/* A value like any other: it goes under the skeleton with the rows while a lookup is in flight. */}
          <span class="lt-page-range shift-skeleton" aria-live="polite">
            {pageRangeText(current, pageSize, total, strings.pageRange)}
          </span>

          <span class="lt-page-nav">
            <button
              type="button"
              class="lt-page-button is-previous"
              aria-label={strings.previousPage}
              title={strings.previousPage}
              aria-controls={controls}
              disabled={busy || current === 0}
              onClick={() => onPage(current - 1)}
            >
              <ChevronLeftIcon class="lt-page-chevron" />
            </button>
            <button
              type="button"
              class="lt-page-button is-next"
              aria-label={strings.nextPage}
              title={strings.nextPage}
              aria-controls={controls}
              disabled={busy || current >= pages - 1}
              onClick={() => onPage(current + 1)}
            >
              <ChevronRightIcon class="lt-page-chevron" />
            </button>
          </span>
        </div>
      </div>
    </div>
  );
};
