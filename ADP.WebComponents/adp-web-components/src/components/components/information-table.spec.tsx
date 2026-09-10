import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { InformationTable } from './information-table';

import * as revealBelowModule from '~lib/reveal-below';

/**
 * Expanding a line with `scrollExpandedIntoView` (the service-history table) brings it to the top
 * of the page and holds it there while its sub-row slides open. The follow starts on expand, lets
 * go on collapse or when another line opens, and never runs unless the table was asked for it.
 */

const headers = [{ key: 'name', label: 'Name' }];
const rows = [{ name: 'first' }, { name: 'second' }];

const newTable = (scrollExpandedIntoView: boolean) =>
  newSpecPage({
    components: [InformationTable],
    template: () => (
      <information-table
        allowAutoWidth
        expandUsingEntireRow
        scrollExpandedIntoView={scrollExpandedIntoView}
        headers={headers}
        rows={rows}
        subRowRenderer={(row: { name: string }) => <div class="sub-row">{row.name}</div>}
      />
    ),
  });

describe('information-table', () => {
  it('follows an expanding line to the top of the page, and lets go of it when it collapses or another line opens', async () => {
    const letGo = jest.fn();
    const reveal = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => letGo);

    try {
      const page = await newTable(true);
      const lines = page.root.querySelectorAll('.information-table-row');
      expect(lines).toHaveLength(2);

      (lines[1] as HTMLElement).click();
      await page.waitForChanges();
      expect(reveal).toHaveBeenCalledTimes(1);
      expect(reveal.mock.calls[0][0]).toBe(lines[1]);
      // At least as long as the sub-row's container takes to open: its debounce and its transition.
      expect(reveal.mock.calls[0][1]).toBeGreaterThanOrEqual(550);
      expect(letGo).not.toHaveBeenCalled();

      (lines[0] as HTMLElement).click();
      await page.waitForChanges();
      expect(letGo).toHaveBeenCalledTimes(1);
      expect(reveal).toHaveBeenCalledTimes(2);
      expect(reveal.mock.calls[1][0]).toBe(lines[0]);

      (lines[0] as HTMLElement).click();
      await page.waitForChanges();
      expect(letGo).toHaveBeenCalledTimes(2);
      expect(reveal).toHaveBeenCalledTimes(2);
    } finally {
      reveal.mockRestore();
    }
  });

  it('leaves the page where it is unless asked to follow', async () => {
    const reveal = jest.spyOn(revealBelowModule, 'revealBelow').mockImplementation(() => () => {});

    try {
      const page = await newTable(false);
      (page.root.querySelector('.information-table-row') as HTMLElement).click();
      await page.waitForChanges();
      expect(reveal).not.toHaveBeenCalled();
    } finally {
      reveal.mockRestore();
    }
  });
});
