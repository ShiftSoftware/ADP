import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { DEFAULT_PAGE_SIZE, LookupPager, LookupPagerProps, PageSize, pageForSize, pageRows, readStoredPageSize, storePageSize } from './lookup-pager';

const KEY = 'test:pager:page-size';

const rows = (count: number) => Array.from({ length: count }, (_, index) => `row-${index + 1}`);

describe('paging', () => {
  it('slices one page, returns every row for All, and clamps a page outside the list', () => {
    expect(pageRows(rows(12), 0, 5)).toEqual(rows(5));
    expect(pageRows(rows(12), 2, 5)).toEqual(['row-11', 'row-12']);
    expect(pageRows(rows(12), 3, 'all')).toEqual(rows(12));
    expect(pageRows(rows(12), 99, 5)).toEqual(['row-11', 'row-12']);
    expect(pageRows(rows(12), -1, 5)).toEqual(rows(5));
  });

  it('keeps the first visible row in view when the size changes, and goes to the first page for All', () => {
    expect(pageForSize(2, 5, 10, 40)).toBe(1);
    expect(pageForSize(2, 10, 5, 40)).toBe(4);
    expect(pageForSize(3, 5, 'all', 40)).toBe(0);
    expect(pageForSize(99, 5, 10, 12)).toBe(1);
  });

  it('remembers the page size, and falls back to the default on junk or storage that throws', () => {
    const original = Object.getOwnPropertyDescriptor(globalThis, 'localStorage');
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

    try {
      fake({});
      storePageSize(KEY, 'all');
      expect(readStoredPageSize(KEY)).toBe('all');

      fake({ [KEY]: 'banana' });
      expect(readStoredPageSize(KEY)).toBe(DEFAULT_PAGE_SIZE);

      fake({}, true);
      expect(readStoredPageSize(KEY)).toBe(DEFAULT_PAGE_SIZE);
      expect(() => storePageSize(KEY, 10)).not.toThrow();
    } finally {
      if (original) Object.defineProperty(globalThis, 'localStorage', original);
      else delete (globalThis as { localStorage?: unknown }).localStorage;
    }
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

  it('disables the buttons at the ends and everything while busy, labels them and writes the range', async () => {
    const first = buttons((await render({ page: 0 })).root);
    expect(first.previous.hasAttribute('disabled')).toBe(true);
    expect(first.next.hasAttribute('disabled')).toBe(false);

    const { root } = await render({ page: 9 });
    expect(buttons(root).next.hasAttribute('disabled')).toBe(true);
    expect(buttons(root).previous.getAttribute('aria-label')).toBe('Previous page');
    expect(buttons(root).previous.getAttribute('aria-controls')).toBe('list-id');
    expect(root.querySelector('.lt-page-range')!.textContent).toBe('11-12 of 12');

    const busy = (await render({ page: 1, busy: true })).root;
    expect(buttons(busy).previous.hasAttribute('disabled')).toBe(true);
    expect(buttons(busy).next.hasAttribute('disabled')).toBe(true);
    expect(busy.querySelector('select')!.hasAttribute('disabled')).toBe(true);
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
});
