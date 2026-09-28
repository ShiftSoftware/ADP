import fs from 'fs';
import path from 'path';
import { newSpecPage, SpecPage } from '@stencil/core/testing';

import { ShiftCalendar } from '../shift-calendar/shift-calendar';
import { ShiftPortal } from '../../components/shift-portal';
import { ShiftPopover } from '../../components/shift-popover/shift-popover';
import { ShiftPopoverPanel } from '../../components/shift-popover/shift-popover-panel';
import { ShiftInput } from './shift-input';
import { CALENDAR_PROPS } from './types/date/calendar-pass-through';

const COMPONENTS = [ShiftInput, ShiftPopover, ShiftPopoverPanel, ShiftPortal, ShiftCalendar];

async function mount(attributes = '', setup?: (el: HTMLShiftInputElement) => void) {
  const page = await newSpecPage({ components: COMPONENTS, html: `<div></div>` });
  const el = page.doc.createElement('shift-input') as HTMLShiftInputElement;

  for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
  setup?.(el);

  const values: string[] = [];
  el.addEventListener('valueChange', (event: CustomEvent) => values.push(event.detail.value));

  page.body.querySelector('div').appendChild(el);
  await page.waitForChanges();
  await page.waitForChanges();

  return { page, el, values };
}

const date = (attributes = '', setup?: (el: HTMLShiftInputElement) => void) => mount(`type="date" today="2026-09-27" ${attributes}`, setup);

const root = (page: SpecPage) => page.body.querySelector('shift-input').shadowRoot;
const input = (page: SpecPage) => root(page).querySelector<HTMLInputElement>('.in-input');
const part = (page: SpecPage, name: string) => root(page).querySelector<HTMLElement>(`[part~="${name}"]`);
const popover = (page: SpecPage) => root(page).querySelector<HTMLShiftPopoverElement>('shift-popover');
const calendar = (page: SpecPage) =>
  (Array.from(page.body.children)
    .find(node => node.tagName === 'SHIFT-POPOVER-PANEL')
    ?.querySelector('shift-calendar') ?? null) as HTMLShiftCalendarElement;
const win = (page: SpecPage) => page.win as unknown as typeof globalThis;

async function type(page: SpecPage, text: string, commit: 'blur' | 'Enter' | null = 'blur') {
  const field = input(page);

  field.value = text;
  field.dispatchEvent(new (win(page).Event)('input', { bubbles: true, composed: true }));
  await page.waitForChanges();

  if (commit === 'blur') field.dispatchEvent(new (win(page).Event)('blur'));
  if (commit === 'Enter') field.dispatchEvent(new (win(page).KeyboardEvent)('keydown', { key: 'Enter', bubbles: true, composed: true }));
  await page.waitForChanges();
}

describe('text', () => {
  it('label, required mark, hint and the reserved support line', async () => {
    const { page } = await mount('label="Full name" is-required="true" hint="As on your ID"');

    expect(part(page, 'label').textContent).toBe('Full name*');
    expect(part(page, 'required').hasAttribute('data-hidden')).toBe(false);
    expect(part(page, 'label').getAttribute('for')).toBe(input(page).id);
    expect(input(page).getAttribute('aria-required')).toBe('true');
    expect(part(page, 'hint').textContent).toBe('As on your ID');
    expect(part(page, 'hint').hasAttribute('data-shown')).toBe(true);
    expect(part(page, 'error').hasAttribute('data-shown')).toBe(false);
    expect(input(page).getAttribute('aria-describedby')).toBe(`${part(page, 'hint').id} ${part(page, 'error').id}`);
  });

  it('label row and required mark stay mounted; they open and fade instead', async () => {
    const { page, el } = await mount('label="Full name"');
    const row = () => root(page).querySelector('.in-label-row');

    expect(part(page, 'required').hasAttribute('data-hidden')).toBe(true);
    expect(row().hasAttribute('data-open')).toBe(true);

    el.isRequired = true;
    await page.waitForChanges();
    expect(part(page, 'required').hasAttribute('data-hidden')).toBe(false);

    el.label = undefined;
    await page.waitForChanges();
    expect(row().hasAttribute('data-open')).toBe(false);
    expect(part(page, 'label').getAttribute('aria-hidden')).toBe('true');
  });

  it('typing commits on blur and fires valueChange once', async () => {
    const { page, el, values } = await mount('');

    await type(page, 'Aza', null);
    expect(el.value).toBe('');
    await type(page, 'Aza');

    expect(el.value).toBe('Aza');
    expect(values).toEqual(['Aza']);
  });

  it('value, defaultValue, placeholder, autocomplete and inputmode', async () => {
    const { page } = await mount('default-value="Hello" placeholder="Your name" autocomplete="name" inputmode="email"');

    expect(input(page).value).toBe('Hello');
    expect(input(page).placeholder).toBe('Your name');
    expect(input(page).getAttribute('autocomplete')).toBe('name');
    expect(input(page).getAttribute('inputmode')).toBe('email');
  });

  it('clearable: the clear button shows with a value and clears; hidden it keeps its room', async () => {
    const off = await mount('value="x"');
    expect(part(off.page, 'clear').hasAttribute('data-collapsed')).toBe(true);

    const { page, el, values } = await mount('value="x" clearable="true"');
    expect(part(page, 'clear').hasAttribute('data-hidden')).toBe(false);

    part(page, 'clear').click();
    await page.waitForChanges();
    expect(el.value).toBe('');
    expect(values).toEqual(['']);
    expect(part(page, 'clear').hasAttribute('data-hidden')).toBe(true);
    expect(part(page, 'clear').getAttribute('tabindex')).toBe('-1');
  });

  it('readonly and disabled', async () => {
    const readonly = await mount('value="x" readonly="true" clearable="true"');
    expect(input(readonly.page).hasAttribute('readonly')).toBe(true);
    expect(part(readonly.page, 'clear').hasAttribute('data-hidden')).toBe(true);

    const disabled = await mount('is-disabled="true"');
    expect(input(disabled.page).hasAttribute('disabled')).toBe(true);
    expect(part(disabled.page, 'root').hasAttribute('data-disabled')).toBe(true);
  });

  it('errorMessage replaces the hint in the same line', async () => {
    const { page, el } = await mount('hint="As on your ID"');

    el.errorMessage = 'Required';
    await page.waitForChanges();
    expect(part(page, 'error').textContent).toBe('Required');
    expect(part(page, 'error').hasAttribute('data-shown')).toBe(true);
    expect(part(page, 'hint').hasAttribute('data-shown')).toBe(false);
    expect(input(page).getAttribute('aria-invalid')).toBe('true');
  });

  it('reflects type, name, value, appearance, color-scheme and size; RTL follows the language', async () => {
    const { page, el } = await mount('name="fullName" value="x" appearance="sharp" color-scheme="dark" size="lg" language="ar"');

    expect(['type', 'name', 'value', 'appearance', 'color-scheme', 'size'].map(name => el.getAttribute(name))).toEqual(['text', 'fullName', 'x', 'sharp', 'dark', 'lg']);
    expect(part(page, 'root').getAttribute('dir')).toBe('rtl');
  });

  it('has no calendar', async () => {
    const { page } = await mount('');

    expect(popover(page)).toBeNull();
    expect(part(page, 'calendar-button')).toBeNull();
    expect(input(page).hasAttribute('role')).toBe(false);
    expect(input(page).hasAttribute('aria-haspopup')).toBe(false);
  });
});

describe('date', () => {
  it('shows the value in the display format, dd/MM/yyyy by default, and hints it', async () => {
    expect(input((await date('value="2026-09-05"')).page).value).toBe('05/09/2026');
    expect(input((await date('value="2026-09-05" format="yyyy-MM-dd"')).page).value).toBe('2026-09-05');
    expect(input((await date('default-value="2026-09-05" format="d.M.yyyy"')).page).value).toBe('5.9.2026');
    expect(input((await date('')).page).placeholder).toBe('dd/mm/yyyy');
    expect(input((await date('language="ru" format="dd.MM.yyyy"')).page).placeholder).toBe('дд.мм.гггг');
    expect(input((await date('')).page).getAttribute('inputmode')).toBe('numeric');
  });

  it('a value that is not a date is dropped', async () => {
    expect((await date('value="yesterday"')).el.value).toBe('');
  });

  const rules = 'min="2026-09-01" max="2026-12-31" disabled-weekdays="5" disabled-dates="2026-10-06"';
  const table: [string, string, string, string][] = [
    ['05/10/2026', '2026-10-05', '05/10/2026', ''],
    ['5/10/2026', '2026-10-05', '05/10/2026', ''],
    ['05102026', '2026-10-05', '05/10/2026', ''],
    ['2026-10-05', '2026-10-05', '05/10/2026', ''],
    ['٠٥/١٠/٢٠٢٦', '2026-10-05', '05/10/2026', ''],
    ['31/09/2026', '', '31/09/2026', 'Enter a date as dd/mm/yyyy'],
    ['05/13/2026', '', '05/13/2026', 'Enter a date as dd/mm/yyyy'],
    ['abc', '', 'abc', 'Enter a date as dd/mm/yyyy'],
    ['05/10', '', '05/10', 'Enter a date as dd/mm/yyyy'],
    ['05/10/26', '', '05/10/26', 'Enter a date as dd/mm/yyyy'],
    ['06/10/2026', '', '06/10/2026', '06/10/2026 can’t be chosen'],
    ['02/10/2026', '', '02/10/2026', '02/10/2026 can’t be chosen'],
    ['15/08/2026', '', '15/08/2026', 'Choose 01/09/2026 or later'],
    ['01/01/2027', '', '01/01/2027', 'Choose 31/12/2026 or earlier'],
    ['', '', '', ''],
  ];

  it.each(table)('%s → value %s, text %s, message %s', async (text, value, shown, message) => {
    const { page, el } = await date(rules);

    await type(page, text);

    expect(el.value).toBe(value);
    expect(input(page).value).toBe(shown);
    expect(part(page, 'error').hasAttribute('data-shown')).toBe(!!message);
    if (message) expect(part(page, 'error').textContent).toBe(message);
  });

  it('never reformats while typing; Enter commits', async () => {
    const { page, el, values } = await date('');

    await type(page, '5/9/2026', null);
    expect(input(page).value).toBe('5/9/2026');
    expect(el.value).toBe('');

    await type(page, '5/9/2026', 'Enter');
    expect(input(page).value).toBe('05/09/2026');
    expect(values).toEqual(['2026-09-05']);
  });

  it('localization overrides the messages, with date slots', async () => {
    const { page } = await date('min="2026-09-01"', el => {
      el.localization = { en: { format: 'Bad date', minMessage: 'From $minDate$', maxMessage: '', betweenMessage: '', unavailableMessage: 'Closed' } };
      el.disabledWeekdays = [5];
    });

    await type(page, 'x');
    expect(part(page, 'error').textContent).toBe('Bad date');
    await type(page, '01/08/2026');
    expect(part(page, 'error').textContent).toBe('From 01/09/2026');
    await type(page, '02/10/2026');
    expect(part(page, 'error').textContent).toBe('Closed');
  });

  it('localized message and button label in Arabic', async () => {
    const { page } = await date('language="ar"');

    expect(part(page, 'calendar-button').getAttribute('aria-label')).toBe('اختر التاريخ');
    await type(page, 'x');
    expect(part(page, 'error').textContent).toBe('أدخل التاريخ بالشكل يي/شش/سسسس');
  });

  it('the calendar button names the date and opens the popover anchored to the input', async () => {
    expect(part((await date('value="2026-09-29"')).page, 'calendar-button').getAttribute('aria-label')).toBe('Change date, Tuesday, 29 September 2026');

    const { page } = await date('');
    expect(popover(page).anchor).toBe(input(page));
    expect(input(page).getAttribute('role')).toBe('combobox');
    expect(input(page).getAttribute('aria-haspopup')).toBe('dialog');

    part(page, 'calendar-button').click();
    await page.waitForChanges();
    await page.waitForChanges();
    expect(popover(page).open).toBe(true);
    expect(part(page, 'calendar-button').getAttribute('aria-expanded')).toBe('true');
    expect(input(page).getAttribute('aria-expanded')).toBe('true');
  });

  it('a click in the control opens without the calendar taking focus; openOnControlClick turns it off', async () => {
    const on = await date('');
    input(on.page).click();
    await on.page.waitForChanges();
    expect(popover(on.page).open).toBe(true);

    const off = await date('', el => (el.openOnControlClick = false));
    input(off.page).click();
    await off.page.waitForChanges();
    expect(popover(off.page).open).toBe(false);
  });

  it('Alt+ArrowDown opens; disabled and readonly never open', async () => {
    const { page } = await date('');
    input(page).dispatchEvent(new (win(page).KeyboardEvent)('keydown', { key: 'ArrowDown', altKey: true, bubbles: true }));
    await page.waitForChanges();
    expect(popover(page).open).toBe(true);

    for (const attributes of ['is-disabled="true"', 'readonly="true"']) {
      const closed = await date(attributes);
      part(closed.page, 'calendar-button').click();
      input(closed.page).click();
      await closed.page.waitForChanges();
      expect(popover(closed.page).open).toBe(false);
    }
  });

  it('a day chosen in the calendar is committed, shown, and closes the popover', async () => {
    const { page, el, values } = await date('');

    await popover(page).show({ focus: false });
    await page.waitForChanges();
    calendar(page).dispatchEvent(new (win(page).CustomEvent)('dateChange', { detail: { value: '2026-09-30' }, bubbles: true, composed: true }));
    await page.waitForChanges();

    expect(el.value).toBe('2026-09-30');
    expect(input(page).value).toBe('30/09/2026');
    expect(values).toEqual(['2026-09-30']);
    expect(popover(page).open).toBe(false);
  });

  it('the popover wears the field’s appearance, scheme, size and classes', async () => {
    const { page } = await date('appearance="material" color-scheme="dark" size="lg" class="brand"');

    expect([popover(page).appearance, popover(page).colorScheme, popover(page).size]).toEqual(['material', 'dark', 'lg']);
    expect(popover(page).className).toContain('brand');
  });
});

describe('calendar pass-through', () => {
  it('lists every shift-calendar prop', () => {
    const source = fs.readFileSync(path.resolve(__dirname, '../shift-calendar/shift-calendar.tsx'), 'utf8');
    const props = [...source.matchAll(/@Prop\([^)]*\)\s+(\w+)/g)].map(([, name]) => name).sort();

    expect([...CALENDAR_PROPS].sort()).toEqual(props);
  });

  it('every listed prop reaches the calendar', async () => {
    const values: Record<string, unknown> = {
      value: '2026-10-05',
      month: '2026-10',
      min: '2026-09-01',
      max: '2026-12-31',
      enabledDates: ['2026-10-05', '2026-10-06'],
      disabledDates: ['2026-10-07'],
      disabledWeekdays: [5],
      isDateDisabled: () => false,
      dayMeta: { '2026-10-05': { badge: '3' } },
      today: '2026-09-28',
      highlightToday: true,
      weekStartsOn: 0,
      numerals: 'arab',
      disableViews: true,
      showToday: false,
      busy: true,
      label: 'Visit date',
      language: 'ar',
      appearance: 'material',
      colorScheme: 'dark',
      size: 'lg',
    };

    const { page } = await mount('type="date"', el => Object.assign(el, values));
    const inner = calendar(page) as unknown as Record<string, unknown>;

    for (const name of CALENDAR_PROPS.filter(name => name !== 'disabled')) expect([name, inner[name]]).toEqual([name, values[name]]);

    const disabled = await mount('type="date" is-disabled="true"');
    expect(calendar(disabled.page).disabled).toBe(true);
  });
});
