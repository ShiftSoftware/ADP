import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import {
  DEFAULT_PAGE_SIZE,
  LookupPager,
  LookupPagerProps,
  PageSize,
  clampPage,
  isPaginated,
  pageCount,
  pageForSize,
  pageRangeText,
  pageRows,
  pagingNeedsReveal,
  parsePageSize,
  readStoredPageSize,
  storePageSize,
} from './lookup-pager';

const KEY = 'test:pager:page-size';

const rows = (count: number) => Array.from({ length: count }, (_, index) => `row-${index + 1}`);

describe('parsePageSize', () => {
  it('reads the listed sizes from strings and numbers', () => {
    expect(parsePageSize('10')).toBe(10);
    expect(parsePageSize(25)).toBe(25);
    expect(parsePageSize('5')).toBe(5);
  });

  it('reads "all"', () => {
    expect(parsePageSize('all')).toBe('all');
  });

  it('falls back to the default on anything else', () => {
    for (const junk of [null, undefined, '', 'abc', '7', 7, 0, -5, NaN, '10px', 'ALL']) {
      expect(parsePageSize(junk as string)).toBe(DEFAULT_PAGE_SIZE);
    }
  });
});

describe('pageCount and clampPage', () => {
  it('counts pages, at least one', () => {
    expect(pageCount(0, 5)).toBe(1);
    expect(pageCount(5, 5)).toBe(1);
    expect(pageCount(6, 5)).toBe(2);
    expect(pageCount(100, 'all')).toBe(1);
  });

  it('brings a page back inside the list', () => {
    expect(clampPage(-3, 12, 5)).toBe(0);
    expect(clampPage(9, 12, 5)).toBe(2);
    expect(clampPage(NaN, 12, 5)).toBe(0);
    expect(clampPage(4, 12, 'all')).toBe(0);
  });
});

describe('pageRows', () => {
  it('slices one page', () => {
    expect(pageRows(rows(12), 0, 5)).toEqual(rows(5));
    expect(pageRows(rows(12), 2, 5)).toEqual(['row-11', 'row-12']);
  });

  it('returns every row for All', () => {
    expect(pageRows(rows(12), 3, 'all')).toEqual(rows(12));
  });

  it('clamps an out-of-range page to the nearest one', () => {
    expect(pageRows(rows(12), 99, 5)).toEqual(['row-11', 'row-12']);
    expect(pageRows(rows(12), -1, 5)).toEqual(rows(5));
  });

  it('is empty for an empty list', () => {
    expect(pageRows([], 0, 5)).toEqual([]);
  });
});

describe('isPaginated', () => {
  it('needs controls only past the smallest page', () => {
    expect(isPaginated(5)).toBe(false);
    expect(isPaginated(6)).toBe(true);
  });
});

describe('pageForSize', () => {
  it('keeps the first visible row in view', () => {
    // Page 3 of 5s starts at row 11; at 10 per page row 11 is on page 2 (index 1).
    expect(pageForSize(2, 5, 10, 40)).toBe(1);
    // Page 2 of 10s starts at row 21; at 5 per page that is page 5 (index 4).
    expect(pageForSize(2, 10, 5, 40)).toBe(4);
  });

  it('goes to the first page when either side is All', () => {
    expect(pageForSize(3, 5, 'all', 40)).toBe(0);
    expect(pageForSize(0, 'all', 5, 40)).toBe(0);
  });

  it('stays inside the list', () => {
    expect(pageForSize(99, 5, 10, 12)).toBe(1);
  });
});

describe('pageRangeText', () => {
  const template = '{from}-{to} of {total}';

  it('writes the rows on screen', () => {
    expect(pageRangeText(0, 5, 23, template)).toBe('1-5 of 23');
    expect(pageRangeText(1, 5, 23, template)).toBe('6-10 of 23');
    expect(pageRangeText(4, 5, 23, template)).toBe('21-23 of 23');
  });

  it('covers All and an empty list', () => {
    expect(pageRangeText(0, 'all', 23, template)).toBe('1-23 of 23');
    expect(pageRangeText(0, 5, 0, template)).toBe('0-0 of 0');
  });
});

describe('pagingNeedsReveal', () => {
  it('asks for a reveal only when the controls would rise off the top', () => {
    expect(pagingNeedsReveal(300, 200)).toBe(false);
    expect(pagingNeedsReveal(200, 200)).toBe(false);
    expect(pagingNeedsReveal(100, 200)).toBe(true);
    expect(pagingNeedsReveal(-10, 0)).toBe(true);
  });
});

describe('page size storage', () => {
  const original = Object.getOwnPropertyDescriptor(globalThis, 'localStorage');

  afterEach(() => {
    if (original) Object.defineProperty(globalThis, 'localStorage', original);
    else delete (globalThis as { localStorage?: unknown }).localStorage;
  });

  const fake = (store: Record<string, string>, throws = false) =>
    Object.defineProperty(globalThis, 'localStorage', {
      configurable: true,
      value: {
        getItem: (key: string) => {
          if (throws) throw new Error('blocked');
          return key in store ? store[key] : null;
        },
        setItem: (key: string, value: string) => {
          if (throws) throw new Error('blocked');
          store[key] = value;
        },
      },
    });

  it('round-trips a size under its own key', () => {
    const store: Record<string, string> = {};
    fake(store);
    storePageSize(KEY, 15);
    expect(store[KEY]).toBe('15');
    expect(readStoredPageSize(KEY)).toBe(15);
    storePageSize(KEY, 'all');
    expect(readStoredPageSize(KEY)).toBe('all');
    expect(readStoredPageSize('other:key')).toBe(DEFAULT_PAGE_SIZE);
  });

  it('reads the default from junk', () => {
    fake({ [KEY]: 'banana' });
    expect(readStoredPageSize(KEY)).toBe(DEFAULT_PAGE_SIZE);
  });

  it('survives storage that throws', () => {
    fake({}, true);
    expect(readStoredPageSize(KEY)).toBe(DEFAULT_PAGE_SIZE);
    expect(() => storePageSize(KEY, 10)).not.toThrow();
  });

  it('survives storage that is missing', () => {
    Object.defineProperty(globalThis, 'localStorage', { configurable: true, value: undefined });
    expect(readStoredPageSize(KEY)).toBe(DEFAULT_PAGE_SIZE);
    expect(() => storePageSize(KEY, 10)).not.toThrow();
  });
});

describe('<LookupPager>', () => {
  const strings = { rowsPerPage: 'Rows per page', pageAll: 'All', previousPage: 'Previous page', nextPage: 'Next page', pageRange: '{from}-{to} of {total}' };

  const render = async (overrides: Partial<LookupPagerProps> = {}) => {
    const calls = { page: [] as number[], size: [] as PageSize[] };
    const props: LookupPagerProps = {
      strings,
      controls: 'list-id',
      page: 0,
      pageSize: 5,
      total: 12,
      open: true,
      busy: false,
      onPage: page => calls.page.push(page),
      onPageSize: size => calls.size.push(size),
      ...overrides,
    };
    const page = await newSpecPage({ components: [], template: () => <LookupPager {...props} /> });
    return { page, calls, root: page.body.querySelector('.lt-pager') as HTMLElement };
  };

  const buttons = (root: HTMLElement) => ({
    previous: root.querySelector('.lt-page-button.is-previous') as HTMLButtonElement,
    next: root.querySelector('.lt-page-button.is-next') as HTMLButtonElement,
  });

  it('disables previous on the first page and next on the last', async () => {
    const first = buttons((await render({ page: 0 })).root);
    expect(first.previous.hasAttribute('disabled')).toBe(true);
    expect(first.next.hasAttribute('disabled')).toBe(false);

    const last = buttons((await render({ page: 2 })).root);
    expect(last.previous.hasAttribute('disabled')).toBe(false);
    expect(last.next.hasAttribute('disabled')).toBe(true);
  });

  it('disables everything while busy', async () => {
    const { root } = await render({ page: 1, busy: true });
    const { previous, next } = buttons(root);
    expect(previous.hasAttribute('disabled')).toBe(true);
    expect(next.hasAttribute('disabled')).toBe(true);
    expect(root.querySelector('select')!.hasAttribute('disabled')).toBe(true);
  });

  it('reflects open state and hides shut controls from assistive tech', async () => {
    const open = (await render({ open: true })).root;
    expect(open.getAttribute('data-open')).toBe('true');
    expect(open.hasAttribute('aria-hidden')).toBe(false);

    const shut = (await render({ open: false })).root;
    expect(shut.getAttribute('data-open')).toBe('false');
    expect(shut.getAttribute('aria-hidden')).toBe('true');
  });

  it('labels the buttons, points them at the list and writes the range', async () => {
    const { root } = await render({ page: 1 });
    const { previous, next } = buttons(root);
    expect(previous.getAttribute('aria-label')).toBe('Previous page');
    expect(next.getAttribute('aria-label')).toBe('Next page');
    expect(previous.getAttribute('aria-controls')).toBe('list-id');
    expect(root.querySelector('select')!.getAttribute('aria-controls')).toBe('list-id');
    expect(root.querySelector('.lt-page-range')!.textContent).toBe('6-10 of 12');
    expect(root.querySelector('.lt-page-range')!.getAttribute('aria-live')).toBe('polite');
  });

  it('lists the sizes with the current one selected and All in the locale', async () => {
    const { root } = await render({ pageSize: 15 });
    const options = Array.from(root.querySelectorAll('option')) as HTMLOptionElement[];
    expect(options.map(option => option.getAttribute('value'))).toEqual(['5', '10', '15', '20', '25', 'all']);
    expect(options.map(option => option.textContent)).toEqual(['5', '10', '15', '20', '25', 'All']);
    expect(options.filter(option => option.hasAttribute('selected')).map(option => option.getAttribute('value'))).toEqual(['15']);
  });

  it('calls back with the neighbouring page and the chosen size', async () => {
    const { root, calls, page } = await render({ page: 1 });
    const { previous, next } = buttons(root);
    previous.click();
    next.click();
    expect(calls.page).toEqual([0, 2]);

    const select = root.querySelector('select') as HTMLSelectElement;
    select.value = 'all';
    select.dispatchEvent(new Event('change'));
    await page.waitForChanges();
    expect(calls.size).toEqual(['all']);
  });

  it('clamps a page past the end for the buttons and the range', async () => {
    const { root } = await render({ page: 9 });
    expect(buttons(root).next.hasAttribute('disabled')).toBe(true);
    expect(root.querySelector('.lt-page-range')!.textContent).toBe('11-12 of 12');
  });
});
