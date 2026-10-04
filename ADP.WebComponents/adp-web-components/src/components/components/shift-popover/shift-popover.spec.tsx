import { newSpecPage, SpecPage } from '@stencil/core/testing';

import { ShiftPortal } from '../shift-portal';
import { ShiftPopover } from './shift-popover';
import { ShiftPopoverPanel } from './shift-popover-panel';

async function mount(attributes = '', setup?: (el: HTMLShiftPopoverElement) => void) {
  const page = await newSpecPage({ components: [ShiftPopover, ShiftPopoverPanel, ShiftPortal], html: `<input id="anchor" /><button id="other">x</button><div id="host"></div>` });
  const el = page.doc.createElement('shift-popover') as HTMLShiftPopoverElement;

  el.setAttribute('anchor', '#anchor');
  for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
  el.innerHTML = '<p id="content">Pick one <button id="inner">A</button></p>';
  setup?.(el);

  const opens: boolean[] = [];
  el.addEventListener('openChange', (event: CustomEvent) => opens.push(event.detail.open));

  page.body.querySelector('#host').appendChild(el);
  await page.waitForChanges();
  await page.waitForChanges();

  return { page, el, opens, anchor: page.body.querySelector<HTMLInputElement>('#anchor') };
}

const panels = (page: SpecPage) => Array.from(page.body.children).filter(node => node.tagName === 'SHIFT-POPOVER-PANEL') as HTMLElement[];
const panelOf = (page: SpecPage) => panels(page)[0] ?? null;
const box = (page: SpecPage) => panelOf(page).shadowRoot.querySelector<HTMLElement>('.pop-panel');
const win = (page: SpecPage) => page.win as unknown as typeof globalThis;
const fire = (page: SpecPage, target: Element, type: string, init: Record<string, unknown> = {}) =>
  target.dispatchEvent(new (win(page).Event)(type, { bubbles: true, composed: true, ...init }));

describe('panel', () => {
  it('is portaled to body with the content, and the appearance, scheme, size and classes of the popover', async () => {
    const { page, el } = await mount('appearance="soft" color-scheme="dark" size="sm" class="brand"');
    const panel = panelOf(page);

    expect(panel.querySelector('#content')).not.toBeNull();
    expect(el.querySelector('#content')).toBeNull();
    expect(panel.getAttribute('appearance')).toBe('soft');
    expect(panel.getAttribute('color-scheme')).toBe('dark');
    expect(panel.getAttribute('size')).toBe('sm');
    expect(panel.classList.contains('brand')).toBe(true);
    expect(panel.classList.contains('hydrated')).toBe(true);
  });

  it('the body-level host is the labelled dialog, hidden and inert while closed', async () => {
    const { page } = await mount('label="Pick a date"');
    const host = panelOf(page);

    expect(host.getAttribute('role')).toBe('dialog');
    expect(host.getAttribute('aria-label')).toBe('Pick a date');
    expect(host.getAttribute('aria-hidden')).toBe('true');
    expect(host.hasAttribute('inert')).toBe(true);
    expect(box(page).hasAttribute('role')).toBe(false);
    expect(box(page).hasAttribute('data-open')).toBe(false);
  });

  it('leaves the page when the popover does', async () => {
    const { page, el } = await mount();

    el.remove();
    await page.waitForChanges();
    expect(panelOf(page)).toBeNull();
  });
});

describe('open and close', () => {
  it('placement is bottom by default and reflects top', async () => {
    expect((await mount()).el.getAttribute('placement')).toBe('bottom');
    expect((await mount('placement="top"')).el.placement).toBe('top');
  });

  it('closes when its content reports a finished pick, and only then', async () => {
    const { page, el } = await mount();
    const pick = (complete: boolean) =>
      panelOf(page)
        .querySelector('#inner')
        .dispatchEvent(new (win(page).CustomEvent)('pickerChange', { bubbles: true, composed: true, detail: { value: 'x', label: 'x', complete } }));

    await el.show();
    await page.waitForChanges();
    pick(false);
    await page.waitForChanges();
    expect(el.open).toBe(true);

    pick(true);
    await page.waitForChanges();
    expect(el.open).toBe(false);
  });

  it('show, hide and toggle fire openChange and reflect open', async () => {
    const { page, el, opens } = await mount();

    await el.show();
    await page.waitForChanges();
    expect(el.hasAttribute('open')).toBe(true);
    expect(box(page).hasAttribute('data-open')).toBe(true);
    expect(panelOf(page).hasAttribute('inert')).toBe(false);

    await el.toggle();
    await page.waitForChanges();
    expect(el.open).toBe(false);

    await el.toggle();
    await el.hide();
    await page.waitForChanges();
    expect(opens).toEqual([true, false, true, false]);
  });

  it('on a foreign input: aria-haspopup and a resolving aria-controls, but no role and no aria-expanded', async () => {
    const { page, el, anchor } = await mount();

    await el.show();
    await page.waitForChanges();

    expect(anchor.getAttribute('aria-haspopup')).toBe('dialog');
    expect(page.doc.getElementById(anchor.getAttribute('aria-controls'))).toBe(panelOf(page));
    expect(anchor.hasAttribute('role')).toBe(false);
    expect(anchor.hasAttribute('aria-expanded')).toBe(false);
  });

  it('keeps the anchor’s own role, and sets aria-expanded only where the role allows it', async () => {
    const { page, el } = await mount();
    const button = page.body.querySelector<HTMLElement>('#other');
    const combobox = page.doc.createElement('input');
    const textbox = page.doc.createElement('input');

    combobox.setAttribute('role', 'combobox');
    textbox.setAttribute('role', 'textbox');
    page.body.append(combobox, textbox);

    for (const [anchor, expanded] of [
      [button, 'true'],
      [combobox, 'true'],
      [textbox, null],
    ] as [HTMLElement, string | null][]) {
      el.anchor = anchor;
      await el.show({ focus: false });
      await page.waitForChanges();
      expect([anchor.getAttribute('role'), anchor.getAttribute('aria-expanded')]).toEqual([anchor === button ? null : anchor.getAttribute('role'), expanded]);
      await el.hide();
      await page.waitForChanges();
    }

    expect(combobox.getAttribute('role')).toBe('combobox');
    expect(textbox.getAttribute('role')).toBe('textbox');
  });

  it('accepts an element as the anchor and releases the old one', async () => {
    const { page, el, anchor } = await mount();
    const other = page.body.querySelector<HTMLElement>('#other');

    el.anchor = other;
    await page.waitForChanges();

    expect(other.getAttribute('aria-haspopup')).toBe('dialog');
    expect(anchor.hasAttribute('aria-haspopup')).toBe(false);
    expect(anchor.hasAttribute('aria-controls')).toBe(false);
  });

  it('pointer-down outside closes, and the click that follows does not reopen through toggle', async () => {
    const { page, el } = await mount();

    await el.show();
    await page.waitForChanges();
    fire(page, page.body.querySelector('#other'), 'pointerdown');
    await page.waitForChanges();
    expect(el.open).toBe(false);

    await el.toggle();
    await page.waitForChanges();
    expect(el.open).toBe(false);
  });

  it('pointer-down on the anchor or in the content keeps it open', async () => {
    const { page, el, anchor } = await mount();

    await el.show({ focus: false });
    await page.waitForChanges();
    fire(page, anchor, 'pointerdown');
    fire(page, panelOf(page).querySelector('#inner'), 'pointerdown');
    await page.waitForChanges();

    expect(el.open).toBe(true);
  });

  it('Escape from the anchor or the content closes', async () => {
    const { page, el, anchor } = await mount();

    await el.show({ focus: false });
    await page.waitForChanges();
    fire(page, anchor, 'keydown', { key: 'Escape' });
    await page.waitForChanges();
    expect(el.open).toBe(false);

    await el.show({ focus: false });
    await page.waitForChanges();
    panelOf(page)
      .querySelector('#inner')
      .dispatchEvent(new (win(page).KeyboardEvent)('keydown', { key: 'Escape', bubbles: true, composed: true }));
    await page.waitForChanges();
    expect(el.open).toBe(false);
  });

  it('the Escape that closes it does not reach window, so a dialog around the anchor stays open', async () => {
    const { page, el, anchor } = await mount();
    const heard: string[] = [];

    page.win.addEventListener('keydown', (event: KeyboardEvent) => heard.push(event.key));
    await el.show({ focus: false });
    await page.waitForChanges();
    fire(page, anchor, 'keydown', { key: 'Escape' });
    await page.waitForChanges();
    expect(el.open).toBe(false);
    expect(heard).toEqual([]);

    fire(page, anchor, 'keydown', { key: 'Escape' });
    expect(heard).toEqual(['Escape']);
  });

  it('focus moving to another element closes; into another popover’s panel it does not', async () => {
    const { page, el } = await mount();
    const other = page.doc.createElement('shift-popover') as HTMLShiftPopoverElement;

    other.setAttribute('anchor', '#other');
    page.body.append(other);
    await page.waitForChanges();

    await el.show({ focus: false });
    await other.show({ focus: false });
    await page.waitForChanges();

    fire(page, panels(page)[1], 'focusin');
    await page.waitForChanges();
    expect([el.open, other.open]).toEqual([true, true]);

    fire(page, page.body.querySelector('#host'), 'focusin');
    await page.waitForChanges();
    expect([el.open, other.open]).toEqual([false, false]);
  });

  it('show() closes another popover only when they share the anchor', async () => {
    const { page, el } = await mount();
    const same = page.doc.createElement('shift-popover') as HTMLShiftPopoverElement;
    const elsewhere = page.doc.createElement('shift-popover') as HTMLShiftPopoverElement;

    same.setAttribute('anchor', '#anchor');
    elsewhere.setAttribute('anchor', '#other');
    page.body.append(same, elsewhere);
    await page.waitForChanges();

    await el.show({ focus: false });
    await elsewhere.show({ focus: false });
    await page.waitForChanges();
    expect([el.open, elsewhere.open]).toEqual([true, true]);

    await same.show({ focus: false });
    await page.waitForChanges();
    expect([el.open, same.open, elsewhere.open]).toEqual([false, true, true]);
  });
});

describe('trigger', () => {
  const key = (page: SpecPage, target: Element, name: string) =>
    target.dispatchEvent(new (win(page).KeyboardEvent)('keydown', { key: name, bubbles: true, composed: true, cancelable: true }));

  it('manual (default): the anchor does not open it', async () => {
    const { page, el, anchor } = await mount();

    fire(page, anchor, 'click');
    await page.waitForChanges();
    expect(el.open).toBe(false);
  });

  it('anchor: a click toggles it, and Enter, Space or ArrowDown open it', async () => {
    const { page, el, anchor } = await mount('trigger="anchor"');

    fire(page, anchor, 'click');
    await page.waitForChanges();
    expect(el.open).toBe(true);

    fire(page, anchor, 'click');
    await page.waitForChanges();
    expect(el.open).toBe(false);

    for (const name of ['Enter', ' ', 'ArrowDown']) {
      key(page, anchor, name);
      await page.waitForChanges();
      expect(el.open).toBe(true);
      await el.hide();
      await page.waitForChanges();
    }

    key(page, anchor, 'a');
    await page.waitForChanges();
    expect(el.open).toBe(false);
  });

  it('disabled keeps it shut, closes it when set while open, and blocks show()', async () => {
    const { page, el, anchor } = await mount('trigger="anchor"');

    await el.show();
    await page.waitForChanges();
    el.disabled = true;
    await page.waitForChanges();
    expect(el.open).toBe(false);

    fire(page, anchor, 'click');
    key(page, anchor, 'Enter');
    await el.show();
    await page.waitForChanges();
    expect(el.open).toBe(false);
  });

  it('a disabled anchor control, or one inside a wrapping anchor, keeps it shut', async () => {
    const { page, el, anchor } = await mount('trigger="anchor"');

    anchor.disabled = true;
    fire(page, anchor, 'click');
    await page.waitForChanges();
    expect(el.open).toBe(false);

    const wrap = page.doc.createElement('div');
    wrap.id = 'wrap';
    wrap.innerHTML = '<input disabled />';
    page.body.prepend(wrap);
    el.anchor = '#wrap';
    await page.waitForChanges();
    fire(page, wrap, 'click');
    await page.waitForChanges();
    expect(el.open).toBe(false);

    wrap.querySelector('input').disabled = false;
    fire(page, wrap, 'click');
    await page.waitForChanges();
    expect(el.open).toBe(true);
  });

  it('switching back to manual stops listening to the anchor', async () => {
    const { page, el, anchor } = await mount('trigger="anchor"');

    el.trigger = 'manual';
    await page.waitForChanges();
    fire(page, anchor, 'click');
    await page.waitForChanges();
    expect(el.open).toBe(false);
  });
});

describe('the dialog always has a name', () => {
  const nameOf = async (html: string, attributes = '') => {
    const page = await newSpecPage({ components: [ShiftPopover, ShiftPopoverPanel, ShiftPortal], html });
    const el = page.doc.createElement('shift-popover') as HTMLShiftPopoverElement;

    el.setAttribute('anchor', '#a1');
    for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
    page.body.append(el);
    await page.waitForChanges();
    await el.show({ focus: false });
    await page.waitForChanges();

    const dialog = panelOf(page);
    const labelledBy = dialog.getAttribute('aria-labelledby');
    const anchor = page.doc.getElementById('a1') ?? page.body.querySelector('input');

    return { page, el, anchor, label: dialog.getAttribute('aria-label'), labelledBy, target: labelledBy ? page.doc.getElementById(labelledBy) : null };
  };

  it('label wins', async () => {
    const { label, labelledBy } = await nameOf('<input id="a1" aria-label="Anchor one" />', 'label="Choose date"');
    expect([label, labelledBy]).toEqual(['Choose date', null]);
  });

  it('an input with <label for>: aria-labelledby points at the label, never at the input', async () => {
    const { label, labelledBy, target } = await nameOf('<label id="l1" for="a1">Booking date</label><input id="a1" />');

    expect([label, labelledBy, target.textContent]).toEqual([null, 'l1', 'Booking date']);
  });

  it('an input with aria-labelledby: the same ids are reused', async () => {
    const { label, labelledBy } = await nameOf('<span id="n1">Booking</span><span id="n2">date</span><input id="a1" aria-labelledby="n1 n2" />');

    expect([label, labelledBy]).toEqual([null, 'n1 n2']);
  });

  it('an input with aria-label only: the dialog takes that text, refreshed on show()', async () => {
    const { page, el, anchor, label, labelledBy } = await nameOf('<input id="a1" aria-label="Anchor one" />');
    expect([label, labelledBy]).toEqual(['Anchor one', null]);

    await el.hide();
    anchor.setAttribute('aria-label', 'Anchor renamed');
    await el.show({ focus: false });
    await page.waitForChanges();
    expect(panelOf(page).getAttribute('aria-label')).toBe('Anchor renamed');
  });

  it('a label wrapping the input: its text is copied without the input’s value, refreshed on show()', async () => {
    const { page, el, label, labelledBy } = await nameOf('<label>Wrapped   label <input id="a1" value="VALUE-LEAK-7" /></label>');
    expect([label, labelledBy]).toEqual(['Wrapped label', null]);
    expect(page.body.querySelector('label').hasAttribute('id')).toBe(false);

    await el.hide();
    page.body.querySelector('label').firstChild.textContent = 'Renamed ';
    await el.show({ focus: false });
    await page.waitForChanges();
    expect(panelOf(page).getAttribute('aria-label')).toBe('Renamed');
  });

  it('labels mixing both kinds: the copied text of all of them', async () => {
    const { label, labelledBy } = await nameOf('<label for="a1">Booking</label><label>date <input id="a1" value="VALUE-LEAK-7" /></label>');

    expect([label, labelledBy]).toEqual(['Booking date', null]);
  });

  it('a label without an id gets a temporary one, removed on cleanup', async () => {
    const { page, el, labelledBy } = await nameOf('<label for="a1">Booking date</label><input id="a1" />');
    const label = page.body.querySelector('label');

    expect(label.id).toBe(labelledBy);
    expect(labelledBy).not.toBe('');

    el.remove();
    await page.waitForChanges();
    expect(label.hasAttribute('id')).toBe(false);
  });

  it('a button keeps aria-labelledby pointing at itself', async () => {
    const { target, anchor } = await nameOf('<button id="a1">Open calendar</button>');

    expect(target).toBe(anchor);
  });

  it('an anchor without an id gets one, and loses it on cleanup', async () => {
    const page = await newSpecPage({ components: [ShiftPopover, ShiftPopoverPanel, ShiftPortal], html: '<button aria-label="Open calendar">x</button>' });
    const button = page.body.querySelector('button');
    const el = page.doc.createElement('shift-popover') as HTMLShiftPopoverElement;

    el.anchor = button;
    page.body.append(el);
    await page.waitForChanges();

    expect(button.id).not.toBe('');
    expect(panelOf(page).getAttribute('aria-labelledby')).toBe(button.id);

    el.remove();
    await page.waitForChanges();
    expect(button.hasAttribute('id')).toBe(false);
  });

  it('else the localized default', async () => {
    const english = await nameOf('<input id="a1" />');
    expect([english.label, english.labelledBy]).toEqual(['Pop-up', null]);

    const arabic = await nameOf('<input id="a1" />', 'language="ar"');
    expect(arabic.label).toBe('نافذة منبثقة');
  });
});

describe('focus on open', () => {
  it('content with setFocus() chooses its own focus; show({ focus: false }) leaves focus alone', async () => {
    const setFocus = jest.fn(async () => undefined);
    const { page, el } = await mount('', popover => {
      const content = popover.querySelector('#content') as HTMLElement & { setFocus?: () => Promise<void> };
      content.setFocus = setFocus;
    });

    await el.show({ focus: false });
    await page.waitForChanges();
    expect(setFocus).not.toHaveBeenCalled();

    await el.hide();
    await el.show();
    await page.waitForChanges();
    expect(setFocus).toHaveBeenCalledTimes(1);
  });
});

describe('mobile sheet', () => {
  const screen = (width: number) => (popover: HTMLShiftPopoverElement) => {
    (popover.ownerDocument.defaultView as unknown as { matchMedia: unknown }).matchMedia = () => ({
      matches: width < 600,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
    });
  };

  const opened = async (attributes: string, width: number) => {
    const mounted = await mount(attributes, screen(width));
    await mounted.el.show();
    await mounted.page.waitForChanges();
    return mounted;
  };

  it('off by default: a phone-width screen keeps the anchored panel, with no backdrop and no scroll lock', async () => {
    const { page } = await opened('', 390);

    expect(box(page).hasAttribute('data-sheet')).toBe(false);
    expect(box(page).getAttribute('part')).toBe('panel');
    expect(page.doc.documentElement.style.overflow).toBe('');
  });

  it('mobile-sheet: a bottom sheet under 600px, which locks the page while open', async () => {
    const { page, el } = await opened('mobile-sheet="true"', 390);

    expect(box(page).hasAttribute('data-sheet')).toBe(true);
    expect(box(page).getAttribute('part')).toBe('panel sheet');
    expect(page.doc.documentElement.style.overflow).toBe('hidden');

    await el.hide();
    await page.waitForChanges();
    expect(page.doc.documentElement.style.overflow).toBe('');
  });

  it('mobile-sheet: still anchored from 600px up', async () => {
    const { page } = await opened('mobile-sheet="true"', 800);

    expect(box(page).hasAttribute('data-sheet')).toBe(false);
  });

  it('turning mobileSheet off while open turns the sheet back into the anchored panel', async () => {
    const { page, el } = await opened('mobile-sheet="true"', 390);

    el.mobileSheet = false;
    await page.waitForChanges();
    expect(box(page).hasAttribute('data-sheet')).toBe(false);
    expect(page.doc.documentElement.style.overflow).toBe('');
  });
});
