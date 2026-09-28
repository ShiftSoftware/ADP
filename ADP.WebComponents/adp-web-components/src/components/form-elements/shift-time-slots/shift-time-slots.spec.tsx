import { newSpecPage, SpecPage } from '@stencil/core/testing';

import { ShiftTimeSlots } from './shift-time-slots';

async function mount(attributes: string, setup?: (el: HTMLShiftTimeSlotsElement) => void) {
  const page = await newSpecPage({ components: [ShiftTimeSlots], html: `<div></div>` });
  const el = page.doc.createElement('shift-time-slots') as HTMLShiftTimeSlotsElement;

  for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
  setup?.(el);

  const changes: string[] = [];
  el.addEventListener('timeChange', (event: CustomEvent) => changes.push(event.detail.value));

  page.body.querySelector('div').appendChild(el);
  await page.waitForChanges();

  return { page, el, changes };
}

const root = (page: SpecPage) => page.body.querySelector('shift-time-slots').shadowRoot;
const current = (page: SpecPage) => root(page).querySelector('.ts-layer[data-current]');
const chips = (page: SpecPage) => Array.from(current(page).querySelectorAll<HTMLButtonElement>('.ts-chip:not(.ts-skeleton)'));
const chip = (page: SpecPage, time: string) => current(page).querySelector<HTMLButtonElement>(`[data-time="${time}"]`);
const labels = (page: SpecPage) => chips(page).map(node => node.textContent);
const stop = (page: SpecPage) => current(page).querySelector('[tabindex="0"]')?.getAttribute('data-time');

async function press(page: SpecPage, key: string) {
  const target = current(page).querySelector('[tabindex="0"]') ?? current(page);
  target.dispatchEvent(new (page.win as unknown as { KeyboardEvent: typeof KeyboardEvent }).KeyboardEvent('keydown', { key, bubbles: true, composed: true }));
  await page.waitForChanges();
}

describe('times', () => {
  it('a radiogroup of sorted, unique times with one tab stop', async () => {
    const { page } = await mount('times="13:00, 09:00, 09:00, 25:00, 11:30, soon"');

    expect(root(page).querySelector('.ts-root').getAttribute('role')).toBe('radiogroup');
    expect(root(page).querySelector('.ts-root').getAttribute('aria-label')).toBe('Times');
    expect(labels(page)).toEqual(['09:00', '11:30', '13:00']);
    chips(page).forEach(node => {
      expect(node.getAttribute('role')).toBe('radio');
      expect(node.getAttribute('aria-checked')).toBe('false');
    });
    expect(current(page).querySelectorAll('[tabindex="0"]')).toHaveLength(1);
    expect(stop(page)).toBe('09:00');
  });

  it('accepts an array property and a JSON string', async () => {
    expect(labels((await mount('', el => (el.times = ['10:00', '08:00']))).page)).toEqual(['08:00', '10:00']);
    expect(labels((await mount('', el => (el.times = '["07:00","06:30"]'))).page)).toEqual(['06:30', '07:00']);
  });

  it('hour cycle and digits follow the language', async () => {
    expect(labels((await mount('times="00:00,09:05,12:00,23:30" hour-cycle="h12"')).page)).toEqual(['12:00 AM', '9:05 AM', '12:00 PM', '11:30 PM']);
    expect(labels((await mount('times="09:00,13:00" hour-cycle="h12" language="ar"')).page)).toEqual(['٩:٠٠ ص', '١:٠٠ م']);
    expect(labels((await mount('times="09:00,13:00" language="ku"')).page)).toEqual(['٠٩:٠٠', '١٣:٠٠']);
    expect(labels((await mount('times="09:00" language="ar" numerals="latn"')).page)).toEqual(['09:00']);
  });

  it('language sets direction and the group label', async () => {
    const { page } = await mount('times="09:00" language="ar"');
    const group = root(page).querySelector('.ts-root');

    expect(group.getAttribute('dir')).toBe('rtl');
    expect(group.getAttribute('lang')).toBe('ar');
    expect(group.getAttribute('aria-label')).toBe('الأوقات');
    expect(
      root((await mount('times="09:00" language="ku"')).page)
        .querySelector('.ts-root')
        .getAttribute('lang'),
    ).toBe('ckb');
  });

  it('appearance, color-scheme and size reflect; md and light by default', async () => {
    const plain = await mount('times="09:00"');
    expect(plain.el.hasAttribute('appearance')).toBe(false);
    expect([plain.el.getAttribute('color-scheme'), plain.el.getAttribute('size')]).toEqual(['light', 'md']);

    const { el, page } = await mount('times="09:00" appearance="sharp" color-scheme="dark" size="lg"');
    expect([el.getAttribute('appearance'), el.getAttribute('color-scheme'), el.getAttribute('size')]).toEqual(['sharp', 'dark', 'lg']);
    el.colorScheme = 'auto';
    await page.waitForChanges();
    expect(el.getAttribute('color-scheme')).toBe('auto');
  });
});

describe('selection', () => {
  it('click selects, reflects and emits every time', async () => {
    const { page, el, changes } = await mount('times="09:00,10:00"');

    chip(page, '10:00').click();
    await page.waitForChanges();
    chip(page, '10:00').click();
    await page.waitForChanges();

    expect(changes).toEqual(['10:00', '10:00']);
    expect(el.getAttribute('value')).toBe('10:00');
    expect(chip(page, '10:00').getAttribute('aria-checked')).toBe('true');
    expect(chip(page, '10:00').getAttribute('part')).toBe('time time-selected');
    expect(stop(page)).toBe('10:00');
  });

  it('the value set by the host is the tab stop; it emits nothing', async () => {
    const { page, changes } = await mount('times="09:00,10:00,11:00" value="11:00"');

    expect(stop(page)).toBe('11:00');
    expect(chip(page, '11:00').hasAttribute('data-selected')).toBe(true);
    expect(changes).toEqual([]);
  });

  it('disabled times and the disabled prop block selection', async () => {
    const { page, el, changes } = await mount('times="09:00,10:00,11:00" disabled-times="09:00"');

    expect(chip(page, '09:00').getAttribute('aria-disabled')).toBe('true');
    expect(chip(page, '09:00').getAttribute('part')).toBe('time time-disabled');
    expect(stop(page)).toBe('10:00');
    chip(page, '09:00').click();
    await page.waitForChanges();
    expect(changes).toEqual([]);

    el.disabled = true;
    await page.waitForChanges();
    chip(page, '10:00').click();
    await page.waitForChanges();
    expect(changes).toEqual([]);
    expect(current(page).querySelectorAll('[tabindex="0"]')).toHaveLength(0);
    expect(root(page).querySelector('.ts-root').getAttribute('aria-disabled')).toBe('true');
  });
});

describe('keyboard', () => {
  it('arrows, Home and End move the tab stop; Enter and Space select', async () => {
    const { page, changes } = await mount('times="09:00,10:00,11:00,12:00"');

    await press(page, 'ArrowRight');
    expect(stop(page)).toBe('10:00');
    await press(page, 'ArrowDown');
    expect(stop(page)).toBe('11:00');
    await press(page, 'End');
    expect(stop(page)).toBe('12:00');
    await press(page, 'ArrowRight');
    expect(stop(page)).toBe('12:00');
    await press(page, 'Home');
    expect(stop(page)).toBe('09:00');
    await press(page, 'ArrowLeft');
    expect(stop(page)).toBe('09:00');
    expect(changes).toEqual([]);

    await press(page, 'ArrowRight');
    await press(page, 'Enter');
    await press(page, 'ArrowRight');
    await press(page, ' ');
    expect(changes).toEqual(['10:00', '11:00']);
  });

  it('mirrors left and right in RTL', async () => {
    const { page } = await mount('times="09:00,10:00,11:00" language="ar"');

    await press(page, 'ArrowLeft');
    expect(stop(page)).toBe('10:00');
    await press(page, 'ArrowRight');
    expect(stop(page)).toBe('09:00');
  });
});

describe('states', () => {
  it('loading: skeleton chips, aria-busy, no radios', async () => {
    const { page, el } = await mount('skeleton-count="6"', node => (node.loading = true));

    expect(el.hasAttribute('loading')).toBe(true);
    expect(root(page).querySelector('.ts-root').getAttribute('aria-busy')).toBe('true');
    expect(current(page).getAttribute('data-kind')).toBe('loading');
    expect(current(page).querySelectorAll('.ts-skeleton')).toHaveLength(6);
    expect(current(page).querySelector('.ts-grid').getAttribute('aria-hidden')).toBe('true');
    expect(chips(page)).toHaveLength(0);
    expect(root(page).querySelector('.ts-sr').textContent).toBe('Loading times…');
  });

  it('empty: the localized text, or the host’s', async () => {
    expect(current((await mount('')).page).textContent).toBe('No times available on this day');
    expect(current((await mount('language="ar"')).page).textContent).toBe('لا توجد أوقات متاحة في هذا اليوم');
    expect(current((await mount('empty-text="Closed"')).page).textContent).toBe('Closed');
  });

  it('a change keeps the outgoing layer, hidden and inert, beside the incoming one', async () => {
    const { page, el } = await mount('times="09:00,10:00"');

    el.loading = true;
    await page.waitForChanges();

    const layers = Array.from(root(page).querySelectorAll('.ts-frame > .ts-layer'));
    expect(layers.map(layer => layer.getAttribute('data-kind'))).toEqual(['times', 'loading']);
    expect(layers[0].getAttribute('data-phase')).toBe('leave');
    expect(layers[0].getAttribute('aria-hidden')).toBe('true');
    expect(layers[0].hasAttribute('inert')).toBe(true);
    expect(layers[1].getAttribute('data-phase')).toBe('enter');

    el.loading = false;
    el.times = ['14:00'];
    await page.waitForChanges();
    expect(labels(page)).toEqual(['14:00']);
    expect(root(page).querySelectorAll('.ts-frame > .ts-layer')).toHaveLength(2);
  });

  it('the same times again is not a change', async () => {
    const { page, el } = await mount('times="09:00,10:00"');

    el.times = ['10:00', '09:00'];
    await page.waitForChanges();

    expect(root(page).querySelectorAll('.ts-frame > .ts-layer')).toHaveLength(1);
  });

  it('entering chips rise one after another', async () => {
    const { page, el } = await mount('');

    el.times = Array.from({ length: 14 }, (_, index) => `${String(8 + index).padStart(2, '0')}:00`);
    await page.waitForChanges();

    const delays = chips(page).map(node => node.style.animationDelay);
    expect(delays.slice(0, 3)).toEqual(['0ms', '24ms', '48ms']);
    expect(delays.at(-1)).toBe('264ms');
  });
});

describe('setFocus', () => {
  it('is callable', async () => {
    const { el } = await mount('times="09:00"');

    await expect(el.setFocus()).resolves.toBeUndefined();
  });
});
