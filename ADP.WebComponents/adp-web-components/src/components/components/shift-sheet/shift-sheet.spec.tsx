import { newSpecPage } from '@stencil/core/testing';

import { ShiftSheet } from './shift-sheet';

/**
 * The sheet's contract that does not need a browser: it is a labelled modal dialog, it draws its own
 * close control, and a tap on the scrim or the button asks to close (the page decides). The motion,
 * focus and scroll lock are checked in a real browser.
 */
describe('shift-sheet', () => {
  it('is a labelled modal dialog with a close button, and asks to close from the scrim and the button', async () => {
    const page = await newSpecPage({ components: [ShiftSheet], html: '<shift-sheet heading="Photos" close-label="Close"><p>content</p></shift-sheet>' });
    const root = page.root!.shadowRoot!;
    const dialog = root.querySelector('dialog')!;
    const requests: string[] = [];
    page.root!.addEventListener('sheetClose', () => requests.push('close'));

    expect(dialog.getAttribute('role')).toBe('dialog');
    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(root.getElementById('sheet-title')?.textContent).toBe('Photos');
    expect(dialog.getAttribute('aria-labelledby')).toBe('sheet-title');
    expect(root.querySelector('.close')?.getAttribute('aria-label')).toBe('Close');
    expect(dialog.hasAttribute('open')).toBe(false);

    (root.querySelector('.scrim') as HTMLElement).click();
    (root.querySelector('.close') as HTMLElement).click();
    expect(requests).toEqual(['close', 'close']);
  });
});
