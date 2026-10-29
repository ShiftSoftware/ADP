import { h } from '@stencil/core';
import { newSpecPage } from '@stencil/core/testing';

import { ShiftTabs } from './shift-tabs';

const mount = (props: Record<string, unknown>) =>
  newSpecPage({
    components: [ShiftTabs],
    template: () => <shift-tabs bare {...props} />,
  });

const tabs = (page: { body: HTMLElement }) => Array.from(page.body.querySelectorAll<HTMLElement>('[role="tab"]'));
const press = (tab: HTMLElement, key: string) => tab.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));

describe('shift-tabs', () => {
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
});
