import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { ShiftTabs } from './shift-tabs';

/**
 * What the two hosts rely on: the original props still drive it (labels, `activeTabLabel`, `changeActiveTab`),
 * and the keyboard and disabled contract holds. The underline's slide is geometry, checked in a browser.
 */
const mount = (props: Record<string, unknown>) =>
  newSpecPage({
    components: [ShiftTabs],
    template: () => <shift-tabs bare {...props} />,
  });

const tabs = (page: { body: HTMLElement }) => Array.from(page.body.querySelectorAll<HTMLElement>('[role="tab"]'));
const press = (tab: HTMLElement, key: string) => tab.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));

describe('shift-tabs', () => {
  it('keeps the original API: string labels, the active label, changeActiveTab on a click of another tab only', async () => {
    const calls: { label: string; idx: number }[] = [];
    const page = await mount({ tabs: ['A', 'B', 'C'], activeTabLabel: 'B', changeActiveTab: (status: { label: string; idx: number }) => calls.push(status) });

    expect(tabs(page).map(tab => tab.getAttribute('aria-selected'))).toEqual(['false', 'true', 'false']);
    expect(tabs(page).map(tab => tab.getAttribute('tabindex'))).toEqual(['-1', '0', '-1']);

    tabs(page)[1].click();
    tabs(page)[2].click();
    expect(calls).toEqual([{ label: 'C', idx: 2 }]);
  });

  it('answers to the arrows (reversed in rtl), Home and End, wrapping; selection follows focus', async () => {
    const calls: number[] = [];
    const changeActiveTab = ({ idx }: { idx: number }) => calls.push(idx);
    const ltr = await mount({ tabs: ['A', 'B', 'C'], activeTabIndex: 0, changeActiveTab });

    press(tabs(ltr)[2], 'ArrowRight');
    press(tabs(ltr)[0], 'ArrowLeft');
    press(tabs(ltr)[0], 'End');
    press(tabs(ltr)[2], 'Home');
    // Right from the last wraps onto the selected first tab and Home from the last lands on it too: a key that lands on the selected tab asks for nothing.
    expect(calls).toEqual([2, 2]);

    const rtl = await mount({ tabs: ['A', 'B', 'C'], activeTabIndex: 1, direction: 'rtl', changeActiveTab });
    press(tabs(rtl)[1], 'ArrowRight');
    press(tabs(rtl)[1], 'ArrowLeft');
    expect(calls.slice(2)).toEqual([0, 2]);
  });

  it('is disabled while a lookup is in flight: aria-disabled, and nothing activates; a caption slot is drawn only for a tab that has one', async () => {
    const calls: number[] = [];
    const page = await mount({
      tabs: [
        { label: 'PDI', caption: '2025-01-19' },
        { label: 'Preowned', caption: null },
      ],
      activeTabIndex: 0,
      disabled: true,
      changeActiveTab: ({ idx }: { idx: number }) => calls.push(idx),
    });

    tabs(page)[1].click();
    press(tabs(page)[0], 'ArrowRight');
    expect(calls).toEqual([]);
    expect(tabs(page).every(tab => tab.getAttribute('aria-disabled') === 'true')).toBe(true);
    expect(Array.from(page.body.querySelectorAll('.shift-tabs-caption')).map(caption => caption.getAttribute('data-open'))).toEqual(['true', 'false']);
  });
});
