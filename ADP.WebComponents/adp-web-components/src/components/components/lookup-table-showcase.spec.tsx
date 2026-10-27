import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import * as revealBelowModule from '~lib/reveal-below';

import { LookupTableShowcase } from './lookup-table-showcase';
import { MANY_VISITS } from './lookup-table-showcase-fixtures';
import { SCENARIOS } from './lookup-table-showcase-scenarios';

/**
 * The showcase host: every scenario in the catalogue renders a card without throwing, the page's
 * chips come from the catalogue, and the interactive bits (single-open, fetch-then-open, the
 * language's direction) work. The host is dev-only; this keeps a scenario that someone adds from
 * silently breaking the page.
 */

const mount = async (html = '<lookup-table-showcase></lookup-table-showcase>') => {
  const page = await newSpecPage({ components: [LookupTableShowcase], html });
  const root = page.root.shadowRoot;
  return {
    page,
    root,
    q: (selector: string) => root.querySelector(selector) as HTMLElement | null,
    qa: (selector: string) => Array.from(root.querySelectorAll(selector)) as HTMLElement[],
  };
};

describe('lookup-table-showcase', () => {
  it('lists every scenario in the catalogue, in order, with a label and a note', async () => {
    const { page } = await mount();
    const catalogue = await page.root.catalogue();
    expect(catalogue.map(item => item.name)).toEqual(Object.keys(SCENARIOS));
    expect(catalogue.every(item => item.label && item.note)).toBe(true);
  });

  it.each(Object.keys(SCENARIOS))('renders the %s scenario as a card with a table', async name => {
    const { q, qa } = await mount(`<lookup-table-showcase scenario="${name}"></lookup-table-showcase>`);
    expect(q('.lookup-card')).not.toBeNull();
    expect(qa('.lt-grid').length).toBeGreaterThan(0);
  });

  it('shows the first scenario for a name it does not know', async () => {
    const { qa } = await mount('<lookup-table-showcase scenario="nonsense"></lookup-table-showcase>');
    expect(qa('.lt-grid').length).toBe(1);
  });

  it('is busy (the card is on the skeleton) only in the busy scenario', async () => {
    expect((await mount('<lookup-table-showcase scenario="busy"></lookup-table-showcase>')).q('.lookup-panel').classList.contains('loading')).toBe(true);
    expect((await mount('<lookup-table-showcase scenario="loaded"></lookup-table-showcase>')).q('.lookup-panel').classList.contains('loading')).toBe(false);
  });

  it('flips the card to right-to-left for Arabic and Kurdish only', async () => {
    expect((await mount('<lookup-table-showcase language="ar"></lookup-table-showcase>')).q('.lookup-panel').getAttribute('dir')).toBe('rtl');
    expect((await mount('<lookup-table-showcase language="ku"></lookup-table-showcase>')).q('.lookup-panel').getAttribute('dir')).toBe('rtl');
    expect((await mount('<lookup-table-showcase language="en"></lookup-table-showcase>')).q('.lookup-panel').getAttribute('dir')).toBe('ltr');
  });

  it('opens one row at a time, and shuts it on a second press', async () => {
    const { page, qa } = await mount();
    const press = async (index: number) => {
      (qa('.lt-action-button')[index] as HTMLButtonElement).click();
      await page.waitForChanges();
    };
    const open = () => qa('.lt-action-button').map(button => button.getAttribute('aria-expanded'));

    await press(0);
    expect(open().slice(0, 3)).toEqual(['true', 'false', 'false']);
    await press(1);
    expect(open().slice(0, 3)).toEqual(['false', 'true', 'false']);
    await press(1);
    expect(open().slice(0, 3)).toEqual(['false', 'false', 'false']);
  });

  it('spins on a press in the fetching scenario (and does not open yet), and a new scenario starts from rest', async () => {
    const { page, qa } = await mount('<lookup-table-showcase scenario="fetching"></lookup-table-showcase>');
    const loading = () => qa('.lt-action-button').map(button => button.getAttribute('data-loading'));
    const expanded = () => qa('.lt-action-button').map(button => button.getAttribute('aria-expanded'));

    (qa('.lt-action-button')[0] as HTMLButtonElement).click();
    await page.waitForChanges();
    expect(loading()[0]).toBe('true');
    expect(expanded()[0]).toBe('false');

    // Leaving the scenario clears the pending fetch, so it never opens a row of the next one.
    page.root.scenario = 'loaded';
    await page.waitForChanges();
    expect(loading().every(state => state === 'false')).toBe(true);
    expect(expanded().every(state => state === 'false')).toBe(true);
  });

  describe('placeholder scenarios', () => {
    const mountPlaceholder = (name: string, extra = '') => mount(`<lookup-table-showcase scenario="${name}" ${extra}></lookup-table-showcase>`);
    const kinds = (qa: (s: string) => HTMLElement[]) =>
      qa('.lt-placeholder-layer')
        .filter(layer => layer.getAttribute('data-active') === 'true')
        .map(layer => layer.getAttribute('data-kind'));
    const settle = async (page: { waitForChanges: () => Promise<void> }, ms = 650) => {
      await new Promise(resolve => setTimeout(resolve, ms));
      await page.waitForChanges();
    };

    it.each([
      ['placeholder-idle', 'idle'],
      ['placeholder-empty', 'empty'],
      ['placeholder-error', 'error'],
      ['placeholder-unauthorized', 'unauthorized'],
    ])('%s shows the %s placeholder, open, with its line, the headings kept on the strip', async (name, kind) => {
      const { q, qa } = await mountPlaceholder(name);
      expect(kinds(qa)).toEqual([kind]);
      expect(q('.lt-placeholder').getAttribute('data-open')).toBe('true');
      expect(q(`.lt-placeholder-layer.is-${kind} .lt-placeholder-text`).textContent.length).toBeGreaterThan(10);
      expect(q('.lt-lead').getAttribute('data-lead')).toBe('columns');
      expect(q('.lt-body').getAttribute('data-open')).toBe('false');
    });

    it('placeholder-busy keeps the empty placeholder in place under the skeleton', async () => {
      const { q, qa } = await mountPlaceholder('placeholder-busy');
      expect(q('.lookup-panel').classList.contains('loading')).toBe(true);
      expect(kinds(qa)).toEqual(['empty']);
      expect(q('.lt-placeholder').getAttribute('data-open')).toBe('true');
      expect(q('.lt-placeholder-layer.is-empty .lt-placeholder-text').classList.contains('shift-skeleton')).toBe(true);
    });

    it('speaks Arabic and flips to right-to-left', async () => {
      const { q } = await mountPlaceholder('placeholder-unauthorized', 'language="ar"');
      expect(q('.lookup-panel').getAttribute('dir')).toBe('rtl');
      expect(q('.lt-placeholder-layer.is-unauthorized .lt-placeholder-text').textContent).toMatch(/[\u0600-\u06FF]/);
    });

    it('placeholder-cycle starts idle; a lookup keeps it under the skeleton, then rows open and the region shuts', async () => {
      const { page, q, qa } = await mountPlaceholder('placeholder-cycle');
      expect(kinds(qa)).toEqual(['idle']);

      (q('.showcase-lookup') as HTMLButtonElement).click();
      await page.waitForChanges();
      expect(q('.lookup-panel').classList.contains('loading')).toBe(true);
      expect(kinds(qa)).toEqual(['idle']);
      expect(q('.lt-placeholder').getAttribute('data-open')).toBe('true');

      await settle(page, 1700);
      expect(q('.lookup-panel').classList.contains('loading')).toBe(false);
      expect(q('.lt-placeholder').getAttribute('data-open')).toBe('false');
      expect(q('.lt-body').getAttribute('data-open')).toBe('true');
      expect(qa('.lt-row').length).toBeGreaterThan(0);
    });

    it('placeholder-cycle: rows -> empty keeps the rows covered while the body shuts, then lets them go; empty -> not in records is a cross-fade', async () => {
      const { page, q, qa } = await mountPlaceholder('placeholder-cycle');
      const lookup = async () => {
        (q('.showcase-lookup') as HTMLButtonElement).click();
        await settle(page, 1700);
      };

      await lookup(); // rows
      await lookup(); // empty
      expect(kinds(qa)).toEqual(['empty']);
      expect(q('.lt-body').getAttribute('data-open')).toBe('false');
      expect(q('.lt-placeholder').getAttribute('data-open')).toBe('true');
      // The rows are still mounted, shutting inside the body.
      expect(qa('.lt-row').length).toBeGreaterThan(0);
      await settle(page);
      expect(qa('.lt-row').length).toBe(0);

      await lookup(); // not in records
      expect(kinds(qa)).toEqual(['unauthorized']);
      expect(qa('.lt-placeholder').length).toBe(1);
      await lookup(); // error
      expect(kinds(qa)).toEqual(['error']);
    });

    it('placeholder-cycle: Clear from rows covers them, opens the idle placeholder, and ends idle', async () => {
      const { page, q, qa } = await mountPlaceholder('placeholder-cycle');
      (q('.showcase-lookup') as HTMLButtonElement).click();
      await settle(page, 1700);
      expect(q('.lt-body').getAttribute('data-open')).toBe('true');

      const clear = qa('.showcase-lookup')[1] as HTMLButtonElement;
      clear.click();
      await page.waitForChanges();
      expect(q('.lookup-panel').classList.contains('loading')).toBe(true);
      expect(kinds(qa)).toEqual(['idle']);
      expect(q('.lt-body').getAttribute('data-open')).toBe('false');

      await settle(page);
      expect(q('.lookup-panel').classList.contains('loading')).toBe(false);
      expect(kinds(qa)).toEqual(['idle']);
      expect(qa('.showcase-lookup')[1].hasAttribute('disabled')).toBe(true);
    });
  });

  describe('drawer scenarios', () => {
    let reveal: jest.SpyInstance;
    beforeEach(() => {
      reveal = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => () => undefined);
    });
    afterEach(() => {
      reveal.mockRestore();
    });

    const mountDrawer = (name: string) => mount(`<lookup-table-showcase scenario="${name}"></lookup-table-showcase>`);
    const drawers = (qa: (s: string) => HTMLElement[]) => qa('.lt-drawer').map(drawer => drawer.getAttribute('data-open'));
    // Real timers: the host's own (a settle is about 520ms, a lookup answers in 1.6s), then a render.
    const settle = async (page: { waitForChanges: () => Promise<void> }, ms = 650) => {
      await new Promise(resolve => setTimeout(resolve, ms));
      await page.waitForChanges();
    };

    it('starts every drawer shut in drawer-closed, and opens one on a press, then moves it on a second press (single-open)', async () => {
      const { page, qa } = await mountDrawer('drawer-closed');
      expect(drawers(qa).every(state => state === 'false')).toBe(true);

      (qa('.lt-action-button')[0] as HTMLButtonElement).click();
      await page.waitForChanges();
      expect(drawers(qa).slice(0, 3)).toEqual(['true', 'false', 'false']);
      expect(qa('.lt-action-button')[0].getAttribute('aria-expanded')).toBe('true');

      (qa('.lt-action-button')[2] as HTMLButtonElement).click();
      await page.waitForChanges();
      expect(drawers(qa).slice(0, 3)).toEqual(['false', 'false', 'true']);
      // The first one is shutting: it still holds its content until a settle has passed.
      expect(qa('.lt-drawer-content').filter(content => content.children.length > 0).length).toBe(2);
      await settle(page);
      expect(qa('.lt-drawer-content').filter(content => content.children.length > 0).length).toBe(1);
    });

    it('opens on a click on the row itself, and follows the row to the top of the page', async () => {
      const { page, qa } = await mountDrawer('drawer-closed');
      (qa('.lt-row')[1] as HTMLElement).click();
      await page.waitForChanges();

      expect(drawers(qa)[1]).toBe('true');
      expect(reveal).toHaveBeenCalledTimes(1);
      expect((reveal.mock.calls[0][0] as HTMLElement).classList.contains('lt-row')).toBe(true);
    });

    it('draws the drawer content: a meta line and two embedded compact tables, one with a column dropped', async () => {
      const { qa } = await mountDrawer('drawer-open');
      const open = qa('.lt-drawer[data-open="true"]')[0];

      expect(open).toBeDefined();
      expect(open.querySelectorAll('.lt-grid[data-embedded="true"][data-density="compact"]').length).toBe(2);
      expect(open.querySelector('.showcase-meta')).not.toBeNull();
      // This invoice's part lines have no description on any line, so the table drops that column by itself.
      const headings = Array.from(open.querySelectorAll('.lt-grid[data-embedded="true"]')).map(grid =>
        Array.from(grid.querySelectorAll('.lt-columns > .lt-cell')).map(cell => cell.textContent),
      );
      expect(headings[1]).not.toContain('Part description');
      expect(headings[1]).toContain('Part number');
      expect(headings[0]).toContain('Labor code');
    });

    it('does not scroll the page on load for a drawer that starts open', async () => {
      await mountDrawer('drawer-open');
      expect(reveal).not.toHaveBeenCalled();
    });

    it('keeps the drawer open, veiled, in drawer-veiled, with the buttons disabled', async () => {
      const { q, qa } = await mountDrawer('drawer-veiled');
      expect(q('.lookup-panel').classList.contains('loading')).toBe(true);
      expect(drawers(qa).filter(state => state === 'true').length).toBe(1);
      // Only the open drawer sweeps: a shut drawer shell is not .loading, or every row would run a hidden skeleton.
      expect(qa('.lt-drawer-row.loading').length).toBe(1);
      expect(q('.lt-drawer-row.loading').getAttribute('data-open')).toBe('true');
      expect(qa('.lt-action-button').every(button => button.hasAttribute('disabled'))).toBe(true);
    });

    it('looks up again: busy and veiled with the drawer open; then kept open on the moved invoice', async () => {
      const { page, q, qa } = await mountDrawer('drawer-relookup-keep');
      const openInvoice = () => {
        const index = drawers(qa).indexOf('true');
        return index < 0 ? null : qa('.lt-drawer')[index].id;
      };
      const before = openInvoice();

      (q('.showcase-lookup') as HTMLButtonElement).click();
      await page.waitForChanges();
      expect(q('.lookup-panel').classList.contains('loading')).toBe(true);
      expect(openInvoice()).toBe(before);
      expect(qa('.lt-slot-body > .lt-row').every(row => row.classList.contains('loading'))).toBe(true);

      await settle(page, 1700);
      expect(q('.lookup-panel').classList.contains('loading')).toBe(false);
      // The same invoice is open, one slot lower.
      expect(openInvoice()).toBe(before);
      expect(drawers(qa).indexOf('true')).toBe(2);
      await settle(page);
      expect(qa('.lt-drawer-content').filter(content => content.children.length > 0).length).toBe(1);
    });

    it('looks up again and drops the drawer when the invoice is not in the answer: it slides shut, then lets go of its content', async () => {
      const { page, q, qa } = await mountDrawer('drawer-relookup-drop');
      expect(drawers(qa).filter(state => state === 'true').length).toBe(1);

      (q('.showcase-lookup') as HTMLButtonElement).click();
      await settle(page, 1700);
      expect(drawers(qa).every(state => state === 'false')).toBe(true);
      expect(qa('.lt-drawer-content').filter(content => content.children.length > 0).length).toBe(1);
      await settle(page);
      expect(qa('.lt-drawer-content').filter(content => content.children.length > 0).length).toBe(0);
    });

    it('closes the drawer when paging away, fading the values as it shuts, and finds it shut on coming back', async () => {
      const { page, qa } = await mountDrawer('drawer-paged');
      const next = () => qa('.lt-page-button.is-next')[0] as HTMLButtonElement;
      const previous = () => qa('.lt-page-button.is-previous')[0] as HTMLButtonElement;
      expect(drawers(qa)[0]).toBe('true');

      next().click();
      await page.waitForChanges();
      expect(drawers(qa)[0]).toBe('false');
      expect(qa('.lt-slot').every(slot => slot.getAttribute('data-veil') === 'fade')).toBe(true);
      await settle(page);
      expect(qa('.lt-slot').every(slot => slot.getAttribute('data-veil') === 'none')).toBe(true);
      expect(drawers(qa).every(state => state === 'false')).toBe(true);

      previous().click();
      await settle(page);
      expect(drawers(qa).every(state => state === 'false')).toBe(true);
    });

    it('looks up again on a paged scenario against the rows shown (the first page), not the whole answer: an invoice now on page 3 does not stay open', async () => {
      const paged = SCENARIOS['drawer-paged'];
      // A paged scenario that can look up again: the next answer is the same invoices in reverse, so the open invoice (first on page 1) now sits last, on page 3.
      SCENARIOS['drawer-paged-relookup-test'] = {
        ...paged,
        lookups: [MANY_VISITS, [...MANY_VISITS].reverse()],
        render: context => (
          <div>
            {paged.render(context)}
            <button class="showcase-lookup" onClick={context.relookup} />
          </div>
        ),
      };
      try {
        const { page, q, qa } = await mountDrawer('drawer-paged-relookup-test');
        // Page 2, with its first invoice open (the one on page 1 is shut by paging away).
        (qa('.lt-page-button.is-next')[0] as HTMLButtonElement).click();
        await settle(page);
        const secondPageFirst = q('.lt-row .lt-code')?.textContent;
        expect(secondPageFirst).not.toBe(MANY_VISITS[0].invoice);
        (qa('.lt-action-button')[0] as HTMLButtonElement).click();
        await page.waitForChanges();
        expect(drawers(qa)[0]).toBe('true');

        (q('.showcase-lookup') as HTMLButtonElement).click();
        await settle(page, 1700);

        // The lookup goes back to the first page: the rows shown are the new answer's first page (its first row is the old last), and an invoice that is not among them is shut.
        expect(q('.lt-row .lt-code')?.textContent).toBe(MANY_VISITS[MANY_VISITS.length - 1].invoice);
        expect(drawers(qa).every(state => state === 'false')).toBe(true);
        await settle(page);
        expect(qa('.lt-drawer-content').filter(content => content.children.length > 0).length).toBe(0);
      } finally {
        delete SCENARIOS['drawer-paged-relookup-test'];
      }
    });
  });
});
