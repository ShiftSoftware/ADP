import { newSpecPage, SpecPage } from '@stencil/core/testing';

import { ShiftCalendar } from '../shift-calendar/shift-calendar';
import { ShiftDayStrip } from './shift-day-strip';

async function mount(attributes: string, setup?: (el: HTMLShiftDayStripElement) => void) {
  const page = await newSpecPage({ components: [ShiftDayStrip], html: `<div></div>` });
  const el = page.doc.createElement('shift-day-strip') as HTMLShiftDayStripElement;

  for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
  setup?.(el);

  const dates: string[] = [];
  el.addEventListener('dateChange', (event: CustomEvent) => dates.push(event.detail.value));

  page.body.querySelector('div').appendChild(el);
  await page.waitForChanges();

  return { page, el, dates };
}

const root = (page: SpecPage) => page.body.querySelector('shift-day-strip').shadowRoot;
const list = (page: SpecPage) => root(page).querySelector('[role="listbox"]');
const cards = (page: SpecPage) => Array.from(list(page).querySelectorAll<HTMLElement>('[role="option"]'));
const shown = (page: SpecPage) => cards(page).map(card => card.getAttribute('data-date'));
const card = (page: SpecPage, date: string) => list(page).querySelector<HTMLElement>(`[data-date="${date}"]`);
const focused = (page: SpecPage) => list(page).querySelector('[tabindex="0"]');
const focusedDate = (page: SpecPage) => focused(page)?.getAttribute('data-date');
const nav = (page: SpecPage, part: 'prev' | 'next') => root(page).querySelector<HTMLButtonElement>(`[part="${part}"]`);

async function press(page: SpecPage, key: string) {
  const target = focused(page) ?? list(page);
  target.dispatchEvent(new (page.win as unknown as { KeyboardEvent: typeof KeyboardEvent }).KeyboardEvent('keydown', { key, bubbles: true, composed: true }));
  await page.waitForChanges();
}

describe('days shown', () => {
  it('min to max, every day a card', async () => {
    const { page } = await mount('today="2026-09-27" min="2026-09-28" max="2026-10-04"');

    expect(shown(page)).toEqual(['2026-09-28', '2026-09-29', '2026-09-30', '2026-10-01', '2026-10-02', '2026-10-03', '2026-10-04']);
  });

  it('allow-list without a range: first to last enabled, the gaps disabled', async () => {
    const { page } = await mount('today="2026-09-27"', el => (el.enabledDates = ['2026-10-02', '2026-09-29']));

    expect(shown(page)).toEqual(['2026-09-29', '2026-09-30', '2026-10-01', '2026-10-02']);
    expect(card(page, '2026-09-30').getAttribute('aria-disabled')).toBe('true');
    expect(card(page, '2026-09-30').getAttribute('data-reason')).toBe('not-enabled');
  });

  it('thirty days from today by default', async () => {
    const { page } = await mount('today="2026-09-27"');

    expect(shown(page)).toHaveLength(30);
    expect(shown(page)[0]).toBe('2026-09-27');
  });

  it('weekday, number and a month label on the first card and each first of the month', async () => {
    const { page } = await mount('today="2026-09-27" min="2026-09-29" max="2026-10-02"');
    const text = (date: string, cls: string) => card(page, date).querySelector(`.${cls}`).textContent;

    expect(text('2026-09-29', 'ds-weekday')).toBe('Tue');
    expect(text('2026-09-29', 'ds-number')).toBe('29');
    expect(text('2026-09-29', 'ds-month')).toBe('Sep');
    expect(text('2026-09-30', 'ds-month')).toBe('');
    expect(text('2026-10-01', 'ds-month')).toBe('Oct');
    expect(card(page, '2026-09-29').querySelector('.ds-sr').textContent).toBe('Tuesday, 29 September 2026');
  });

  it('language: direction, names and digits', async () => {
    const { page } = await mount('today="2026-09-27" min="2026-09-29" max="2026-10-01" language="ar"');

    expect(root(page).querySelector('.ds-root').getAttribute('dir')).toBe('rtl');
    expect(card(page, '2026-09-29').querySelector('.ds-number').textContent).toBe('٢٩');
    expect(card(page, '2026-09-29').querySelector('.ds-weekday').textContent).toBe('ثلاثاء');
    expect(card(page, '2026-10-01').querySelector('.ds-month').textContent).toBe('ت١');

    const latin = await mount('today="2026-09-27" min="2026-09-29" max="2026-10-01" language="ar" numerals="latn"');
    expect(card(latin.page, '2026-10-01').querySelector('.ds-month').textContent).toBe('ت1');
  });

  it('appearance, color-scheme and size reflect with their defaults', async () => {
    const { el, page } = await mount('today="2026-09-27"');

    expect(el.hasAttribute('appearance')).toBe(false);
    expect(el.getAttribute('color-scheme')).toBe('light');
    expect(el.getAttribute('size')).toBe('md');

    el.appearance = 'sharp';
    el.colorScheme = 'dark';
    el.size = 'lg';
    await page.waitForChanges();
    expect([el.getAttribute('appearance'), el.getAttribute('color-scheme'), el.getAttribute('size')]).toEqual(['sharp', 'dark', 'lg']);
  });
});

describe('rules', () => {
  it('same inputs, same blocked days and reasons as shift-calendar', async () => {
    const page = await newSpecPage({ components: [ShiftCalendar, ShiftDayStrip], html: `<div></div>` });
    const configure = (el: HTMLShiftCalendarElement | HTMLShiftDayStripElement) => {
      el.setAttribute('today', '2026-09-27');
      el.setAttribute('min', '2026-09-03');
      el.setAttribute('max', '2026-09-28');
      el.disabledDates = '2026-09-10, 2026-09-18';
      el.disabledWeekdays = [5];
      el.enabledDates = ['2026-09-04', '2026-09-10', '2026-09-11', '2026-09-14', '2026-09-15', '2026-09-16', '2026-09-22', '2026-09-24', '2026-09-28'];
      el.isDateDisabled = date => date.endsWith('6');
      page.body.querySelector('div').appendChild(el);
    };

    const calendar = page.doc.createElement('shift-calendar') as HTMLShiftCalendarElement;
    const strip = page.doc.createElement('shift-day-strip') as HTMLShiftDayStripElement;
    calendar.setAttribute('month', '2026-09');
    configure(calendar);
    configure(strip);
    await page.waitForChanges();

    const read = (host: Element, selector: string) =>
      Object.fromEntries(
        Array.from(host.shadowRoot.querySelectorAll(selector))
          .filter(node => node.getAttribute('data-date') >= '2026-09-03' && node.getAttribute('data-date') <= '2026-09-28')
          .map(node => [node.getAttribute('data-date'), node.getAttribute('data-reason') ?? 'open']),
      );

    const fromCalendar = read(calendar, '.cal-view[data-current] .cal-page[data-current] [data-date]');
    const fromStrip = read(strip, '[role="option"]');

    expect(Object.keys(fromStrip)).toHaveLength(26);
    expect(fromStrip).toEqual(fromCalendar);
    expect(new Set(Object.values(fromStrip))).toEqual(new Set(['open', 'disabled-date', 'disabled-weekday', 'not-enabled', 'rule']));
  });

  it('selects an open day, ignores a blocked one', async () => {
    const { page, el, dates } = await mount('today="2026-09-27" min="2026-09-28" max="2026-10-04" disabled-weekdays="5"');

    card(page, '2026-09-29').click();
    await page.waitForChanges();
    expect(el.value).toBe('2026-09-29');
    expect(card(page, '2026-09-29').getAttribute('aria-selected')).toBe('true');
    expect(card(page, '2026-09-29').getAttribute('part')).toContain('day-selected');

    card(page, '2026-10-02').click();
    await page.waitForChanges();
    expect(el.value).toBe('2026-09-29');
    expect(card(page, '2026-10-02').getAttribute('data-kind')).toBe('closed');
    expect(card(page, '2026-10-02').getAttribute('part')).toBe('day day-disabled day-closed');
    expect(dates).toEqual(['2026-09-29']);
  });

  it('busy and disabled block selection; disabled leaves nothing in the tab order', async () => {
    const { page, el, dates } = await mount('today="2026-09-27" busy="true"');

    card(page, '2026-09-28').click();
    await page.waitForChanges();
    expect(dates).toEqual([]);
    expect(list(page).getAttribute('aria-busy')).toBe('true');

    el.busy = false;
    el.disabled = true;
    await page.waitForChanges();
    card(page, '2026-09-28').click();
    await page.waitForChanges();
    expect(dates).toEqual([]);
    expect(list(page).querySelectorAll('[tabindex]')).toHaveLength(0);
    expect(nav(page, 'prev').hasAttribute('disabled')).toBe(true);
    expect(nav(page, 'next').hasAttribute('disabled')).toBe(true);
  });

  it('host value moves focus, is not reported', async () => {
    const { page, el, dates } = await mount('today="2026-09-27"');

    el.value = '2026-10-05';
    await page.waitForChanges();
    expect(focusedDate(page)).toBe('2026-10-05');
    expect(card(page, '2026-10-05').hasAttribute('data-selected')).toBe(true);
    expect(dates).toEqual([]);
  });

  it('badges and their description', async () => {
    const { page } = await mount('today="2026-09-27" min="2026-09-28" max="2026-10-02"', el => {
      el.dayMeta = { '2026-09-28': { badge: '8', tone: 'success', description: '8 times available' } };
    });
    const badge = card(page, '2026-09-28').querySelector('.ds-badge');

    expect(badge.textContent).toBe('8');
    expect(badge.getAttribute('data-tone')).toBe('success');
    expect(card(page, '2026-09-29').querySelector('.ds-badge').hasAttribute('data-empty')).toBe(true);
    expect(card(page, '2026-09-28').querySelector('.ds-sr').textContent).toBe('Monday, 28 September 2026, 8 times available');
  });
});

describe('today', () => {
  it('aria-current always; marked only with highlightToday', async () => {
    const plain = await mount('today="2026-09-28" min="2026-09-27"');
    expect(card(plain.page, '2026-09-28').getAttribute('aria-current')).toBe('date');
    expect(card(plain.page, '2026-09-28').hasAttribute('data-today')).toBe(false);

    const marked = await mount('today="2026-09-28" min="2026-09-27" highlight-today="true"');
    expect(card(marked.page, '2026-09-28').hasAttribute('data-today')).toBe(true);
    expect(card(marked.page, '2026-09-28').getAttribute('part')).toContain('day-today');
  });
});

describe('keyboard', () => {
  it('one tab stop: the selected day, else today, else the first open day', async () => {
    expect(focusedDate((await mount('today="2026-09-27" value="2026-10-03"')).page)).toBe('2026-10-03');
    expect(focusedDate((await mount('today="2026-09-27"')).page)).toBe('2026-09-27');
    expect(focusedDate((await mount('today="2026-09-27" min="2026-10-01" disabled-weekdays="4"')).page)).toBe('2026-10-02');

    const { page } = await mount('today="2026-09-27"');
    expect(list(page).querySelectorAll('[tabindex="0"]')).toHaveLength(1);
  });

  it('arrows move a day, Home and End go to the ends, Enter selects', async () => {
    const { page, el, dates } = await mount('today="2026-09-27" min="2026-09-27" max="2026-10-10"');

    await press(page, 'ArrowRight');
    expect(focusedDate(page)).toBe('2026-09-28');
    await press(page, 'ArrowLeft');
    await press(page, 'ArrowLeft');
    expect(focusedDate(page)).toBe('2026-09-27');
    await press(page, 'End');
    expect(focusedDate(page)).toBe('2026-10-10');
    await press(page, 'Home');
    expect(focusedDate(page)).toBe('2026-09-27');
    expect(el.value).toBe('');

    await press(page, 'PageDown');
    expect(focusedDate(page)).toBe('2026-09-28');

    await press(page, 'Enter');
    expect(el.value).toBe('2026-09-28');
    expect(dates).toEqual(['2026-09-28']);
  });

  it('RTL swaps left and right', async () => {
    const { page } = await mount('today="2026-09-27" language="ar"');

    await press(page, 'ArrowLeft');
    expect(focusedDate(page)).toBe('2026-09-28');
    await press(page, 'ArrowRight');
    expect(focusedDate(page)).toBe('2026-09-27');
  });

  it('disabled days are focusable, not selectable', async () => {
    const { page, dates } = await mount('today="2026-09-27" disabled-dates="2026-09-28"');

    await press(page, 'ArrowRight');
    expect(focusedDate(page)).toBe('2026-09-28');
    await press(page, ' ');
    expect(dates).toEqual([]);
  });
});

describe('caption', () => {
  it('shut by default, open with text', async () => {
    const plain = await mount('today="2026-09-27"');
    expect(root(plain.page).querySelector('.ds-caption-wrap').hasAttribute('data-open')).toBe(false);
    expect(list(plain.page).hasAttribute('aria-describedby')).toBe(false);

    const { page } = await mount('today="2026-09-27" caption="15 days available"');
    const wrap = root(page).querySelector('.ds-caption-wrap');
    expect(wrap.hasAttribute('data-open')).toBe(true);
    expect(wrap.querySelector('[part="caption"]').textContent).toBe('15 days available');
    expect(list(page).getAttribute('aria-describedby')).toBe(wrap.querySelector('[part="caption"]').id);
  });
});
