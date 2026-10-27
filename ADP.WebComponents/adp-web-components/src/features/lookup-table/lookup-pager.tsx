import { FunctionalComponent, h } from '@stencil/core';

import { ChevronLeftIcon } from '~assets/chevron-left-icon';
import { ChevronRightIcon } from '~assets/chevron-right-icon';

export const PAGE_SIZES = [5, 10, 15, 20, 25] as const;

export type PageSize = (typeof PAGE_SIZES)[number] | 'all';

export const PAGE_SIZE_OPTIONS: PageSize[] = [...PAGE_SIZES, 'all'];

export const DEFAULT_PAGE_SIZE: PageSize = 5;

export const parsePageSize = (raw: string | number | null | undefined): PageSize => {
  if (raw === 'all') return 'all';
  const size = Number(raw);
  return (PAGE_SIZES as readonly number[]).includes(size) ? (size as PageSize) : DEFAULT_PAGE_SIZE;
};

export const readStoredPageSize = (key: string): PageSize => {
  try {
    return parsePageSize(globalThis.localStorage?.getItem(key));
  } catch {
    return DEFAULT_PAGE_SIZE;
  }
};

export const storePageSize = (key: string, size: PageSize): void => {
  try {
    globalThis.localStorage?.setItem(key, String(size));
  } catch {
    // Blocked storage: the size is simply not remembered.
  }
};

export const pageCount = (total: number, size: PageSize): number => (size === 'all' ? 1 : Math.max(1, Math.ceil(total / size)));

export const clampPage = (page: number, total: number, size: PageSize): number => Math.min(Math.max(0, Math.floor(page) || 0), pageCount(total, size) - 1);

export const pageRows = <Row,>(rows: Row[], page: number, size: PageSize): Row[] => {
  if (size === 'all') return rows;
  const start = clampPage(page, rows.length, size) * size;
  return rows.slice(start, start + size);
};

export const isPaginated = (total: number): boolean => total > PAGE_SIZES[0];

/** The page that keeps the first row on screen in view when the size changes. */
export const pageForSize = (page: number, from: PageSize, to: PageSize, total: number): number => {
  if (to === 'all' || from === 'all') return 0;
  return clampPage(Math.floor((clampPage(page, total, from) * from) / to), total, to);
};

export const pageRangeText = (page: number, size: PageSize, total: number, template: string): string => {
  const from = total === 0 ? 0 : (size === 'all' ? 0 : clampPage(page, total, size) * size) + 1;
  const to = size === 'all' ? total : Math.min(total, from - 1 + size);
  return template.replace('{from}', String(from)).replace('{to}', String(to)).replace('{total}', String(total));
};

/** Whether a shorter page lifts the controls off the top of the screen. */
export const pagingNeedsReveal = (controlsTop: number, removedHeight: number): boolean => controlsTop - removedHeight < 0;

export type LookupPagerStrings = {
  rowsPerPage: string;
  pageAll: string;
  previousPage: string;
  nextPage: string;
  pageRange: string;
};

export type LookupPagerProps = {
  strings: LookupPagerStrings;
  controls: string;
  page: number;
  pageSize: PageSize;
  total: number;
  open: boolean;
  busy: boolean;
  onPage: (page: number) => void;
  onPageSize: (size: PageSize) => void;
};

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
