import { newSpecPage, SpecPage } from '@stencil/core/testing';

import { CalendarMonthChangeDetail, CalendarViewChangeDetail, ShiftCalendar } from './shift-calendar';

async function mount(attributes: string, setup?: (el: HTMLShiftCalendarElement) => void) {
  const page = await newSpecPage({ components: [ShiftCalendar], html: `<div></div>` });
  const el = page.doc.createElement('shift-calendar') as HTMLShiftCalendarElement;

  for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
  setup?.(el);

  const dates: string[] = [];
  const months: CalendarMonthChangeDetail[] = [];
  const views: string[] = [];
  el.addEventListener('dateChange', (event: CustomEvent) => dates.push(event.detail.value));
  el.addEventListener('monthChange', (event: CustomEvent) => months.push(event.detail));
  el.addEventListener('viewChange', (event: CustomEvent<CalendarViewChangeDetail>) => views.push(event.detail.view));

  page.body.querySelector('div').appendChild(el);
  await page.waitForChanges();

  return { page, el, dates, months, views };
}

const root = (page: SpecPage) => page.body.querySelector('shift-calendar').shadowRoot;
const layer = (page: SpecPage) => root(page).querySelector('.cal-view[data-current] .cal-page[data-current]');
const cell = (page: SpecPage, date: string) => layer(page).querySelector<HTMLElement>(`[data-date="${date}"]`);
const pick = (page: SpecPage, index: number) => layer(page).querySelector<HTMLElement>(`[data-index="${index}"]`);
const heading = (page: SpecPage) => root(page).querySelector('.cal-header .cal-sr[aria-live]').textContent;
const weekdays = (page: SpecPage) => Array.from(root(page).querySelectorAll('.cal-view[data-current] .cal-weekday .cal-sr')).map(node => node.textContent);
const button = (page: SpecPage, part: string) =>
  root(page).querySelector<HTMLButtonElement>(`.cal-header > [part="${part}"], .cal-nav [part="${part}"], .cal-title[data-current] [part="${part}"]`);
const view = (page: SpecPage) => root(page).querySelector('.cal-root').getAttribute('data-view');
const focused = (page: SpecPage) => layer(page).querySelector('[tabindex="0"]');

async function click(page: SpecPage, part: string) {
  button(page, part).click();
  await page.waitForChanges();
}

async function press(page: SpecPage, key: string, shiftKey = false) {
  const target = focused(page) ?? root(page).querySelector('.cal-root');
  target.dispatchEvent(new (page.win as unknown as { KeyboardEvent: typeof KeyboardEvent }).KeyboardEvent('keydown', { key, shiftKey, bubbles: true, composed: true }));
  await page.waitForChanges();
}

const focusedDate = (page: SpecPage) => focused(page)?.getAttribute('data-date');
const focusedIndex = (page: SpecPage) => Number(focused(page)?.getAttribute('data-index'));

describe('grid', () => {
  it('six weeks, outside days muted and unfocusable', async () => {
    const { page } = await mount('today="2026-02-10"');

    expect(heading(page)).toBe('February 2026');
    expect(layer(page).querySelectorAll('[role="row"]')).toHaveLength(6);
    expect(layer(page).querySelectorAll('[role="gridcell"]')).toHaveLength(42);

    const outside = layer(page).querySelectorAll('[data-outside]');
    expect(outside.length).toBe(42 - 28);
    outside.forEach(node => {
      expect(node.getAttribute('aria-disabled')).toBe('true');
      expect(node.hasAttribute('tabindex')).toBe(false);
    });
  });

  it('opening month: value, today, range edge, month', async () => {
    expect(heading((await mount('today="2026-09-27" value="2026-12-03"')).page)).toBe('December 2026');
    expect(heading((await mount('today="2026-09-27"')).page)).toBe('September 2026');
    expect(heading((await mount('today="2026-09-27" min="2026-11-02"')).page)).toBe('November 2026');
    expect(heading((await mount('today="2026-09-27" max="2026-07-02"')).page)).toBe('July 2026');
    expect(heading((await mount('today="2026-09-27" month="2027-03"')).page)).toBe('March 2027');
  });

  it('aria-selected and a single tab stop', async () => {
    const { page } = await mount('today="2026-09-27" value="2026-09-29"');

    expect(cell(page, '2026-09-29').getAttribute('aria-selected')).toBe('true');
    expect(cell(page, '2026-09-28').getAttribute('aria-selected')).toBe('false');
    expect(focusedDate(page)).toBe('2026-09-29');
    expect(layer(page).querySelectorAll('[tabindex="0"]')).toHaveLength(1);
  });

  it('language: names, direction, week start, digits', async () => {
    const english = (await mount('today="2026-09-27"')).page;
    expect(weekdays(english)[0]).toBe('Monday');
    expect(root(english).querySelector('.cal-root').getAttribute('dir')).toBe('ltr');

    const arabic = (await mount('today="2026-09-27" language="ar"')).page;
    expect(heading(arabic)).toBe('أيلول ٢٠٢٦');
    expect(weekdays(arabic)[0]).toBe('السبت');
    expect(root(arabic).querySelector('.cal-root').getAttribute('dir')).toBe('rtl');
    expect(cell(arabic, '2026-09-27').querySelector('.cal-day-number').textContent).toBe('٢٧');

    const kurdish = (await mount('today="2026-09-27" language="ku"')).page;
    expect(heading(kurdish)).toBe('ئەیلوول ٢٠٢٦');
    expect(weekdays(kurdish)).toEqual(['شەممە', 'یەکشەممە', 'دووشەممە', 'سێشەممە', 'چوارشەممە', 'پێنجشەممە', 'هەینی']);

    const russian = (await mount('today="2026-09-27" language="ru"')).page;
    expect(heading(russian)).toBe('Сентябрь 2026');
    expect(cell(russian, '2026-09-27').querySelector('.cal-sr').textContent).toBe('воскресенье, 27 сентября 2026');
  });

  it('week-starts-on and numerals override the language', async () => {
    const { page } = await mount('today="2026-09-27" language="ar" week-starts-on="0" numerals="latn"');

    expect(weekdays(page)[0]).toBe('الأحد');
    expect(heading(page)).toBe('أيلول 2026');
  });

  it('size reflects, md by default', async () => {
    const plain = await mount('today="2026-09-27"');
    expect(plain.el.getAttribute('size')).toBe('md');

    const { el, page } = await mount('today="2026-09-27" size="xl"');
    expect(el.getAttribute('size')).toBe('xl');
    el.size = 'sm';
    await page.waitForChanges();
    expect(el.getAttribute('size')).toBe('sm');
    expect(layer(page).querySelectorAll('[role="row"]')).toHaveLength(6);
  });

  it('appearance and color-scheme reflect; color-scheme defaults to light', async () => {
    const plain = await mount('today="2026-09-27"');
    expect(plain.el.hasAttribute('appearance')).toBe(false);
    expect(plain.el.getAttribute('color-scheme')).toBe('light');

    const { el, page } = await mount('today="2026-09-27" appearance="soft" color-scheme="dark"');
    expect(el.getAttribute('appearance')).toBe('soft');
    el.colorScheme = 'auto';
    await page.waitForChanges();
    expect(el.getAttribute('color-scheme')).toBe('auto');
  });
});

describe('selection and rules', () => {
  it('selects an open day', async () => {
    const { page, el, dates } = await mount('today="2026-09-27"');

    cell(page, '2026-09-30').click();
    await page.waitForChanges();

    expect(dates).toEqual(['2026-09-30']);
    expect(el.getAttribute('value')).toBe('2026-09-30');
    expect(cell(page, '2026-09-30').getAttribute('aria-selected')).toBe('true');
  });

  it('ignores disabled, out-of-range and outside days', async () => {
    const { page, el, dates } = await mount('today="2026-09-27" min="2026-09-10" disabled-dates="2026-09-15" disabled-weekdays="5"');

    cell(page, '2026-09-15').click();
    cell(page, '2026-09-18').click();
    cell(page, '2026-09-05').click();
    (layer(page).querySelector('[data-outside]') as HTMLElement).click();
    await page.waitForChanges();

    expect(dates).toEqual([]);
    expect(el.value).toBe('');
    expect(cell(page, '2026-09-15').getAttribute('data-kind')).toBe('closed');
    expect(cell(page, '2026-09-18').getAttribute('data-reason')).toBe('disabled-weekday');
    expect(cell(page, '2026-09-05').getAttribute('data-kind')).toBe('unavailable');
  });

  it('allow-list and isDateDisabled', async () => {
    const { page, dates } = await mount('today="2026-09-27" enabled-dates="2026-09-29,2026-09-30,2026-10-01"', el => {
      el.isDateDisabled = date => date === '2026-09-30';
    });

    expect(cell(page, '2026-09-28').getAttribute('data-reason')).toBe('not-enabled');
    expect(cell(page, '2026-09-29').getAttribute('data-kind')).toBe('open');
    expect(cell(page, '2026-09-30').getAttribute('data-reason')).toBe('rule');

    cell(page, '2026-09-30').click();
    cell(page, '2026-09-29').click();
    await page.waitForChanges();

    expect(dates).toEqual(['2026-09-29']);
  });

  it('badges and their description', async () => {
    const { page } = await mount('today="2026-09-27"', el => {
      el.dayMeta = { '2026-09-29': { badge: '8', tone: 'success', description: '8 times available' } };
    });

    const badge = cell(page, '2026-09-29').querySelector('.cal-badge');
    expect(badge.textContent).toBe('8');
    expect(badge.getAttribute('data-tone')).toBe('success');
    expect(cell(page, '2026-09-29').querySelector('.cal-sr').textContent).toBe('Tuesday, 29 September 2026, 8 times available');
    expect(cell(page, '2026-09-30').querySelector('.cal-badge').hasAttribute('data-empty')).toBe(true);
  });

  it('badge digits follow numerals', async () => {
    const meta = { '2026-09-29': { badge: '12', tone: 'success' as const } };

    const arabic = await mount('today="2026-09-27" language="ku"', el => (el.dayMeta = meta));
    expect(cell(arabic.page, '2026-09-29').querySelector('.cal-badge').textContent).toBe('١٢');

    const latin = await mount('today="2026-09-27" language="ar" numerals="latn"', el => (el.dayMeta = meta));
    expect(cell(latin.page, '2026-09-29').querySelector('.cal-badge').textContent).toBe('12');
  });

  it('busy and disabled block selection', async () => {
    const { page, el, dates } = await mount('today="2026-09-27" busy=""');

    cell(page, '2026-09-29').click();
    await page.waitForChanges();
    expect(dates).toEqual([]);

    el.busy = false;
    el.disabled = true;
    await page.waitForChanges();

    cell(page, '2026-09-29').click();
    await page.waitForChanges();
    expect(dates).toEqual([]);
    expect(layer(page).querySelectorAll('[tabindex]')).toHaveLength(0);
    expect(button(page, 'next').hasAttribute('disabled')).toBe(true);
  });
});

describe('highlightToday', () => {
  it('off by default: aria-current only', async () => {
    const { page, el } = await mount('today="2026-09-27"');
    const today = cell(page, '2026-09-27');

    expect(el.highlightToday).toBe(false);
    expect(today.getAttribute('aria-current')).toBe('date');
    expect(today.hasAttribute('data-today')).toBe(false);
    expect(today.getAttribute('part')).toBe('day');
    expect(root(page).querySelectorAll('[data-today]')).toHaveLength(0);
  });

  it('on: days, months and years', async () => {
    const { page } = await mount('today="2026-09-27" highlight-today=""');

    expect(cell(page, '2026-09-27').hasAttribute('data-today')).toBe(true);
    expect(cell(page, '2026-09-27').getAttribute('part')).toContain('day-today');
    expect(cell(page, '2026-09-27').getAttribute('aria-current')).toBe('date');

    await click(page, 'month-button');
    expect(pick(page, 2026 * 12 + 8).hasAttribute('data-today')).toBe(true);
    expect(layer(page).querySelectorAll('[data-today]')).toHaveLength(1);

    await click(page, 'year-button');
    expect(pick(page, 2026).getAttribute('part')).toContain('year-today');
    expect(layer(page).querySelectorAll('[data-today]')).toHaveLength(1);
  });

  it('selected today keeps the selected state', async () => {
    const { page } = await mount('today="2026-09-27" value="2026-09-27" highlight-today=""');
    const today = cell(page, '2026-09-27');

    expect(today.hasAttribute('data-selected')).toBe(true);
    expect(today.getAttribute('part')).toContain('day-selected');
  });
});

describe('month navigation', () => {
  it('buttons page months and report each change', async () => {
    const { page, el, months } = await mount('today="2026-09-27"');

    await click(page, 'next');
    expect(heading(page)).toBe('October 2026');
    expect(el.month).toBe('2026-10');
    expect(months).toEqual([{ month: '2026-10', firstDate: '2026-10-01', lastDate: '2026-10-31' }]);

    button(page, 'prev').click();
    button(page, 'prev').click();
    await page.waitForChanges();
    expect(months.map(detail => detail.month)).toEqual(['2026-10', '2026-09', '2026-08']);
  });

  it('min/max disable the buttons and clamp month', async () => {
    const { page, el } = await mount('today="2026-09-27" min="2026-09-10" max="2026-10-20"');

    expect(button(page, 'prev').hasAttribute('disabled')).toBe(true);
    expect(button(page, 'next').hasAttribute('disabled')).toBe(false);

    await click(page, 'next');
    expect(button(page, 'next').hasAttribute('disabled')).toBe(true);

    el.month = '2027-05';
    await page.waitForChanges();
    expect(el.month).toBe('2026-10');
  });

  it('min after max: empty, still, no loop', async () => {
    const { page, el, dates, months } = await mount('today="2026-09-27" min="2026-12-01" max="2026-01-01"');

    expect(heading(page)).toBe('September 2026');
    expect(layer(page).querySelectorAll('[tabindex]')).toHaveLength(0);
    expect(layer(page).querySelectorAll('[data-kind="open"]')).toHaveLength(0);
    expect(['prev', 'next', 'today', 'month-button', 'year-button'].map(part => button(page, part).hasAttribute('disabled'))).toEqual([true, true, true, true, true]);

    cell(page, '2026-09-28').click();
    el.month = '2026-11';
    await page.waitForChanges();

    expect(dates).toEqual([]);
    expect(months).toEqual([]);
    expect(heading(page)).toBe('November 2026');
  });

  it('host month: followed, not reported', async () => {
    const { page, el, months } = await mount('today="2026-09-27"');

    el.month = '2027-01';
    await page.waitForChanges();

    expect(heading(page)).toBe('January 2027');
    expect(months).toEqual([]);
  });

  it('host value: moves and reports', async () => {
    const { page, el, months } = await mount('today="2026-09-27"');

    el.value = '2026-12-24';
    await page.waitForChanges();

    expect(heading(page)).toBe('December 2026');
    expect(months.map(detail => detail.month)).toEqual(['2026-12']);
    expect(focusedDate(page)).toBe('2026-12-24');
  });

  it('carousel: next leaves forward, previous back', async () => {
    const { page } = await mount('today="2026-09-27"');

    await click(page, 'next');
    const leaving = root(page).querySelector('.cal-page[data-phase="leave"]');
    expect(leaving.getAttribute('data-travel')).toBe('forward');
    expect(leaving.getAttribute('aria-hidden')).toBe('true');
    expect(leaving.querySelector('[tabindex]')).toBeNull();
    expect(layer(page).getAttribute('data-phase')).toBe('enter');
    expect(layer(page).getAttribute('data-travel')).toBe('forward');
    expect(root(page).querySelector('.cal-title[data-phase="leave"]')).toBeNull();

    await click(page, 'prev');
    expect(root(page).querySelector('.cal-page[data-phase="leave"]').getAttribute('data-travel')).toBe('back');
  });

  it('title: only the month rolls within a year', async () => {
    const { page } = await mount('today="2026-09-27"');

    await click(page, 'next');

    const slots = (name: string) => Array.from(root(page).querySelectorAll(`[data-slot="${name}"] .cal-slot-layer`)).map(layer => layer.getAttribute('data-phase'));
    expect(slots('month')).toEqual(['leave', 'enter']);
    expect(slots('year')).toEqual(['rest']);
    expect(heading(page)).toBe('October 2026');
  });

  it('title: year and month roll together across a year', async () => {
    const { page } = await mount('today="2026-12-10"');

    await click(page, 'next');

    const slots = (name: string) => Array.from(root(page).querySelectorAll(`[data-slot="${name}"] .cal-slot-layer`)).map(layer => layer.getAttribute('data-phase'));
    expect(slots('month')).toEqual(['leave', 'enter']);
    expect(slots('year')).toEqual(['leave', 'enter']);
    expect(root(page).querySelector('.cal-title[data-current] [part="year-button"]').textContent).toBe('2027');
    expect(heading(page)).toBe('January 2027');
  });

  it('title: year comes first, heading reads naturally', async () => {
    const { page } = await mount('today="2026-09-27" language="ar"');
    const title = root(page).querySelector('.cal-title[data-current]');

    expect(title.firstElementChild.getAttribute('data-slot')).toBe('year');
    expect(heading(page)).toBe('أيلول ٢٠٢٦');
  });

  it('carousel mirrors in RTL', async () => {
    const { page } = await mount('today="2026-09-27" language="ar"');

    await click(page, 'next');
    expect(root(page).querySelector('.cal-root').getAttribute('dir')).toBe('rtl');
    expect(layer(page).getAttribute('data-travel')).toBe('forward');
  });

  it('rapid clicks settle on the last month with one outgoing page', async () => {
    const { page, months } = await mount('today="2026-09-27"');

    for (let i = 0; i < 5; i++) button(page, 'next').click();
    await page.waitForChanges();

    expect(heading(page)).toBe('February 2027');
    expect(months.map(detail => detail.month)).toEqual(['2026-10', '2026-11', '2026-12', '2027-01', '2027-02']);
    expect(root(page).querySelectorAll('.cal-page')).toHaveLength(2);
    expect(root(page).querySelectorAll('.cal-title-band .cal-title')).toHaveLength(1);
    expect(root(page).querySelectorAll('[data-slot="year"] .cal-slot-layer')).toHaveLength(2);
    expect(root(page).querySelectorAll('[data-slot="month"] .cal-slot-layer')).toHaveLength(2);
  });

  it('Today: disabled outside range, selects from any view', async () => {
    const blocked = await mount('today="2026-09-27" min="2026-09-29"');
    expect(button(blocked.page, 'today').hasAttribute('disabled')).toBe(true);

    const { page, el, months, views, dates } = await mount('today="2026-09-27" month="2027-02"');
    await click(page, 'year-button');
    await click(page, 'today');

    expect(el.value).toBe('2026-09-27');
    expect(dates).toEqual(['2026-09-27']);

    expect(view(page)).toBe('days');
    expect(heading(page)).toBe('September 2026');
    expect(focusedDate(page)).toBe('2026-09-27');
    expect(months.map(detail => detail.month)).toEqual(['2026-09']);
    expect(views).toEqual(['years', 'days']);
  });

  it('Today on a disabled today: navigates, keeps value', async () => {
    const { page, el, dates, months } = await mount('today="2026-09-27" month="2026-05" value="2026-05-04" disabled-dates="2026-09-27"');

    await click(page, 'today');

    expect(heading(page)).toBe('September 2026');
    expect(focusedDate(page)).toBe('2026-09-27');
    expect(el.value).toBe('2026-05-04');
    expect(dates).toEqual([]);
    expect(months.map(detail => detail.month)).toEqual(['2026-09']);
  });
});

describe('views', () => {
  it('month button → months; month → days; value untouched', async () => {
    const { page, el, months, views } = await mount('today="2026-09-27" value="2026-09-10"');

    await click(page, 'month-button');
    expect(view(page)).toBe('months');
    expect(heading(page)).toBe('2026');
    expect(focusedIndex(page)).toBe(2026 * 12 + 8);

    pick(page, 2026 * 12 + 2).click();
    await page.waitForChanges();

    expect(view(page)).toBe('days');
    expect(heading(page)).toBe('March 2026');
    expect(el.value).toBe('2026-09-10');
    expect(months.map(detail => detail.month)).toEqual(['2026-03']);
    expect(views).toEqual(['months', 'days']);
  });

  it('year button → years → months → days', async () => {
    const { page, el, months, views } = await mount('today="2026-09-27"');

    await click(page, 'year-button');
    expect(view(page)).toBe('years');
    expect(heading(page)).toBe('2020–2031');

    pick(page, 2023).click();
    await page.waitForChanges();
    expect(view(page)).toBe('months');
    expect(heading(page)).toBe('2023');
    expect(el.month).toBe('2023-09');

    pick(page, 2023 * 12).click();
    await page.waitForChanges();
    expect(heading(page)).toBe('January 2023');
    expect(months.map(detail => detail.month)).toEqual(['2023-09', '2023-01']);
    expect(views).toEqual(['years', 'months', 'days']);
  });

  it('years pages by twelve with the carousel', async () => {
    const { page } = await mount('today="2026-09-27"');

    await click(page, 'year-button');
    await click(page, 'prev');
    expect(heading(page)).toBe('2008–2019');
    expect(root(page).querySelector('.cal-page[data-phase="leave"]').getAttribute('data-travel')).toBe('back');
    expect(button(page, 'prev').getAttribute('aria-label')).toBe('Previous 12 years');
  });

  it('Escape backs out without changes', async () => {
    const { page, el, months, views } = await mount('today="2026-09-27"');

    await click(page, 'month-button');
    await press(page, 'Escape');
    expect(view(page)).toBe('days');
    expect(el.month).toBe('2026-09');

    await click(page, 'month-button');
    await click(page, 'year-button');
    await press(page, 'Escape');
    expect(view(page)).toBe('months');
    await press(page, 'Escape');
    expect(view(page)).toBe('days');

    expect(months).toEqual([]);
    expect(views).toEqual(['months', 'days', 'months', 'years', 'months', 'days']);
  });

  it('views slide: deeper pushes down, shallower lifts', async () => {
    const { page } = await mount('today="2026-09-27"');

    await click(page, 'month-button');
    expect(root(page).querySelector('.cal-view[data-phase="leave"]').getAttribute('data-view')).toBe('days');
    expect(root(page).querySelector('.cal-view[data-phase="leave"]').getAttribute('data-shift')).toBe('deeper');
    expect(root(page).querySelector('.cal-view[data-current]').getAttribute('data-shift')).toBe('deeper');
    expect(root(page).querySelector('.cal-body').hasAttribute('data-switching')).toBe(true);

    await press(page, 'Escape');
    expect(root(page).querySelector('.cal-view[data-phase="leave"]').getAttribute('data-view')).toBe('months');
    expect(root(page).querySelector('.cal-view[data-current]').getAttribute('data-shift')).toBe('shallower');
  });

  it('Today, Escape and a chosen month all slide back with the title rolling', async () => {
    for (const leave of ['today', 'escape', 'pick']) {
      const { page } = await mount('today="2026-09-27" month="2027-03"');

      await click(page, 'month-button');
      if (leave === 'today') await click(page, 'today');
      if (leave === 'escape') await press(page, 'Escape');
      if (leave === 'pick') {
        pick(page, 2027 * 12).click();
        await page.waitForChanges();
      }

      const leaving = root(page).querySelector('.cal-view[data-phase="leave"]');
      expect(leaving?.getAttribute('data-shift')).toBe('shallower');
      expect(root(page).querySelector('.cal-view[data-current]').getAttribute('data-phase')).toBe('enter');
      expect(root(page).querySelector('.cal-title[data-phase="leave"]')).not.toBeNull();
    }
  });

  it('show-today=false hides Today in place', async () => {
    const { page, el } = await mount('today="2026-09-27" show-today="false"');
    const today = () => button(page, 'today');

    expect(today().hasAttribute('data-hidden')).toBe(true);
    expect(today().getAttribute('aria-hidden')).toBe('true');
    expect(today().hasAttribute('disabled')).toBe(true);
    expect(button(page, 'prev')).not.toBeNull();

    el.showToday = true;
    await page.waitForChanges();
    expect(today().hasAttribute('data-hidden')).toBe(false);
    expect(today().hasAttribute('disabled')).toBe(false);
  });

  it('disable-views: header is plain', async () => {
    const { page } = await mount('today="2026-09-27" disable-views=""');

    expect(button(page, 'month-button').hasAttribute('disabled')).toBe(true);
    expect(button(page, 'year-button').hasAttribute('disabled')).toBe(true);
  });

  it('header buttons follow the range', async () => {
    const sameYear = (await mount('today="2026-09-27" min="2026-02-01" max="2026-11-30"')).page;
    expect(button(sameYear, 'month-button').hasAttribute('disabled')).toBe(false);
    expect(button(sameYear, 'year-button').hasAttribute('disabled')).toBe(true);

    const sameMonth = (await mount('today="2026-09-27" min="2026-09-02" max="2026-09-20"')).page;
    expect(button(sameMonth, 'month-button').hasAttribute('disabled')).toBe(true);
    expect(button(sameMonth, 'year-button').hasAttribute('disabled')).toBe(true);
  });

  it('range disables months and years and stops paging', async () => {
    const { page } = await mount('today="2026-09-27" min="2025-03-15" max="2026-10-05"');

    await click(page, 'month-button');
    expect(pick(page, 2026 * 12 + 10).getAttribute('aria-disabled')).toBe('true');
    expect(pick(page, 2026 * 12 + 11).getAttribute('aria-disabled')).toBe('true');
    expect(pick(page, 2026 * 12 + 9).hasAttribute('aria-disabled')).toBe(false);
    expect(button(page, 'next').hasAttribute('disabled')).toBe(true);

    await click(page, 'prev');
    expect(pick(page, 2025 * 12 + 1).getAttribute('part')).toContain('month-disabled');
    expect(pick(page, 2025 * 12 + 2).hasAttribute('aria-disabled')).toBe(false);
    expect(button(page, 'prev').hasAttribute('disabled')).toBe(true);

    pick(page, 2025 * 12).click();
    await page.waitForChanges();
    expect(view(page)).toBe('months');

    await click(page, 'year-button');
    expect(pick(page, 2024).getAttribute('aria-disabled')).toBe('true');
    expect(pick(page, 2027).getAttribute('aria-disabled')).toBe('true');
    expect(['prev', 'next'].map(part => button(page, part).hasAttribute('disabled'))).toEqual([true, true]);
  });
});

describe('keyboard', () => {
  it('days: day, week, month, year, select', async () => {
    const { page, dates } = await mount('today="2026-09-27"');

    await press(page, 'ArrowRight');
    expect(focusedDate(page)).toBe('2026-09-28');
    await press(page, 'ArrowDown');
    expect(focusedDate(page)).toBe('2026-10-05');
    expect(heading(page)).toBe('October 2026');
    await press(page, 'Home');
    expect(focusedDate(page)).toBe('2026-10-05');
    await press(page, 'End');
    expect(focusedDate(page)).toBe('2026-10-11');
    await press(page, 'PageDown');
    expect(focusedDate(page)).toBe('2026-11-11');
    await press(page, 'PageUp', true);
    expect(focusedDate(page)).toBe('2025-11-11');
    await press(page, 'Enter');
    await press(page, 'ArrowLeft');
    await press(page, ' ');

    expect(dates).toEqual(['2025-11-11', '2025-11-10']);
  });

  it('days: RTL swaps left and right', async () => {
    const { page, months } = await mount('today="2026-09-30" language="ar"');

    await press(page, 'ArrowLeft');
    expect(focusedDate(page)).toBe('2026-10-01');
    expect(months.map(detail => detail.month)).toEqual(['2026-10']);

    await press(page, 'ArrowRight');
    expect(focusedDate(page)).toBe('2026-09-30');
  });

  it('days: disabled days focusable, not selectable', async () => {
    const { page, dates } = await mount('today="2026-09-27" disabled-dates="2026-09-28"');

    await press(page, 'ArrowRight');
    expect(focusedDate(page)).toBe('2026-09-28');
    expect(cell(page, '2026-09-28').getAttribute('aria-disabled')).toBe('true');

    await press(page, 'Enter');
    expect(dates).toEqual([]);
  });

  it('days: stops at min and max', async () => {
    const { page } = await mount('today="2026-09-27" min="2026-09-27" max="2026-10-03"');

    await press(page, 'ArrowLeft');
    expect(focusedDate(page)).toBe('2026-09-27');
    await press(page, 'PageDown');
    expect(focusedDate(page)).toBe('2026-10-03');
    await press(page, 'ArrowDown');
    expect(focusedDate(page)).toBe('2026-10-03');
  });

  it('months: grid moves, pages and chooses', async () => {
    const { page } = await mount('today="2026-09-27"');

    await click(page, 'month-button');
    await press(page, 'ArrowRight');
    expect(focusedIndex(page)).toBe(2026 * 12 + 9);
    await press(page, 'ArrowDown');
    expect(focusedIndex(page)).toBe(2027 * 12);
    expect(heading(page)).toBe('2027');
    await press(page, 'End');
    expect(focusedIndex(page)).toBe(2027 * 12 + 2);
    await press(page, 'PageUp');
    expect(heading(page)).toBe('2026');
    await press(page, 'Enter');
    expect(view(page)).toBe('days');
    expect(heading(page)).toBe('March 2026');
  });

  it('years: RTL, paging and choose', async () => {
    const { page } = await mount('today="2026-09-27" language="ar" numerals="latn"');

    await click(page, 'year-button');
    await press(page, 'ArrowLeft');
    expect(focusedIndex(page)).toBe(2027);
    await press(page, 'PageUp');
    expect(focusedIndex(page)).toBe(2015);
    expect(heading(page)).toBe('2008–2019');
    await press(page, ' ');
    expect(view(page)).toBe('months');
    expect(heading(page)).toBe('2015');
  });
});
