import './validation.mock';

import { newSpecPage, SpecPage } from '@stencil/core/testing';
import { object } from 'yup';

import { FormHook } from '~features/form-hook/form-hook';

import { clearAvailabilityCache } from '~lib/booking-availability';

import { BranchSlotPicker, BranchSlotSelection } from '../branch-slot-picker';
import { ShiftCalendar } from '../shift-calendar/shift-calendar';
import { ShiftTimeSlots } from '../shift-time-slots/shift-time-slots';
import { ShiftBookingCalendar } from './shift-booking-calendar';

const day = (date: string, ...times: string[]) => ({ Date: date, Times: times.map(time => `${date} ${time}`) });

const DAYS = [day('2026-10-01', '09:00 AM', '10:00 AM', '12:00 PM', '01:00 PM'), day('2026-10-03', '09:00 AM', '02:00 PM'), day('2026-10-04', '10:00 AM', '01:00 PM')];

const TARGET = 'calendar-api="https://calendar.example/api/public/calendar" branch-id="Xr8pQ" department-id="showroom" brand-id="BRAND" today="2026-09-28"';

type Respond = (url: string) => Promise<unknown>;

let respond: Respond;
let requests: string[];

const ok = (payload: unknown) => () => Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(payload) });

beforeEach(() => {
  clearAvailabilityCache();
  requests = [];
  respond = ok(DAYS);
  (globalThis as unknown as { fetch: unknown }).fetch = (url: string) => {
    requests.push(url);
    return respond(url);
  };
});

const settle = async (page: SpecPage) => {
  for (let i = 0; i < 4; i++) {
    await new Promise(resolve => setTimeout(resolve, 0));
    await page.waitForChanges();
  }
};

interface Field {
  getValue: () => string;
  reset: (value?: unknown) => void;
}

async function mount(attributes: string) {
  const page = await newSpecPage({ components: [ShiftBookingCalendar, ShiftCalendar, ShiftTimeSlots], html: `<shift-booking-calendar ${attributes}></shift-booking-calendar>` });
  const el = page.root as HTMLShiftBookingCalendarElement;
  const field = page.rootInstance as Field;
  const slots: BranchSlotSelection[] = [];

  el.addEventListener('slotChange', (event: CustomEvent<BranchSlotSelection>) => slots.push(event.detail));
  await settle(page);

  return { page, el, field, slots };
}

const root = (page: SpecPage) => page.body.querySelector('shift-booking-calendar').shadowRoot;
const calendar = (page: SpecPage) => root(page).querySelector('shift-calendar') as HTMLShiftCalendarElement;
const slotsEl = (page: SpecPage) => root(page).querySelector('shift-time-slots') as HTMLShiftTimeSlotsElement;
const message = (page: SpecPage) => root(page).querySelector('.bc-message[data-active]');
const box = (page: SpecPage) => root(page).querySelector('.bc-box');
const times = (page: SpecPage) => root(page).querySelector('.bc-times');
const view = (page: SpecPage) => box(page).getAttribute('data-view');
const title = (page: SpecPage) => root(page).querySelector('.bc-title-line').textContent;
const announced = (page: SpecPage) => root(page).querySelector('.bc-root > .bc-sr[aria-live]').textContent;
const chip = (page: SpecPage, time: string) => slotsEl(page).shadowRoot.querySelector<HTMLButtonElement>(`.ts-layer[data-current] [data-time="${time}"]`);
const dayCell = (page: SpecPage, date: string) => calendar(page).shadowRoot.querySelector<HTMLElement>(`.cal-view[data-current] .cal-page[data-current] [data-date="${date}"]`);

async function pickTime(page: SpecPage, time: string) {
  chip(page, time).click();
  await settle(page);
}

async function pickDay(page: SpecPage, date: string) {
  dayCell(page, date).click();
  await settle(page);
}

async function back(page: SpecPage) {
  root(page).querySelector<HTMLButtonElement>('.bc-back').click();
  await settle(page);
}

async function key(page: SpecPage, name: string) {
  times(page).dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true }));
  await settle(page);
}

const expectDays = (page: SpecPage) => {
  expect(view(page)).toBe('days');
  expect(calendar(page).hasAttribute('inert')).toBe(false);
  expect(calendar(page).getAttribute('aria-hidden')).toBeNull();
  expect(times(page).hasAttribute('inert')).toBe(true);
  expect(times(page).getAttribute('aria-hidden')).toBe('true');
};

const expectTimes = (page: SpecPage) => {
  expect(view(page)).toBe('times');
  expect(calendar(page).hasAttribute('inert')).toBe(true);
  expect(calendar(page).getAttribute('aria-hidden')).toBe('true');
  expect(times(page).hasAttribute('inert')).toBe(false);
  expect(times(page).getAttribute('aria-hidden')).toBeNull();
};

describe('states', () => {
  it('idle until every id is present: localized message, no request, days view', async () => {
    const { page } = await mount('calendar-api="https://calendar.example/api/public/calendar" department-id="showroom" brand-id="BRAND"');

    expect(root(page).querySelector('.bc-root').getAttribute('data-status')).toBe('idle');
    expect(message(page).getAttribute('data-message')).toBe('idle');
    expect(message(page).textContent).toBe('Choose a branch first');
    expect(requests).toEqual([]);
    expectDays(page);
    expect(calendar(page).enabledDates).toEqual([]);

    const arabic = await mount('language="ar"');
    expect(message(arabic.page).textContent).toBe('اختر الفرع أولاً');
    expect(root(arabic.page).querySelector('.bc-root').getAttribute('dir')).toBe('rtl');
  });

  it('loading: the calendar is busy in the days view', async () => {
    respond = () => new Promise(() => undefined);
    const { page } = await mount(TARGET);

    expect(message(page).getAttribute('data-message')).toBe('loading');
    expect(calendar(page).busy).toBe(true);
    expectDays(page);
  });

  it('ready: open days, range and month from the response; no day chosen yet', async () => {
    const { page } = await mount(TARGET);
    const url = new URL(requests[0]);

    expect(requests).toHaveLength(1);
    expect(url.searchParams.get('from')).toBe('2026-09-28');
    expect(url.searchParams.get('branchId')).toBe('Xr8pQ');
    expect(calendar(page).enabledDates).toEqual(['2026-10-01', '2026-10-03', '2026-10-04']);
    expect([calendar(page).min, calendar(page).max, calendar(page).value, calendar(page).month]).toEqual(['2026-10-01', '2026-10-04', '', '2026-10']);
    expect(calendar(page).busy).toBe(false);
    expectDays(page);
    expect(message(page).textContent).toBe('Pick a day');
    expect(calendar(page).dayMeta).toEqual({
      '2026-10-01': { badge: '4', tone: 'neutral', tooltip: '4 slots · 09:00 – 13:00', description: '4 slots · 09:00 – 13:00' },
      '2026-10-03': { badge: '2', tone: 'warning', tooltip: '2 slots · 09:00 – 14:00', description: '2 slots · 09:00 – 14:00' },
      '2026-10-04': { badge: '2', tone: 'warning', tooltip: '2 slots · 10:00 – 13:00', description: '2 slots · 10:00 – 13:00' },
    });
  });

  it('the few-left tone, tooltips and counts follow their props; the summary respects the hour cycle', async () => {
    const { page } = await mount(`${TARGET} few-slots="1" day-tooltips="false" hour-cycle="h12"`);

    expect(calendar(page).dayMeta).toEqual({
      '2026-10-01': { badge: '4', tone: 'neutral', description: '4 slots · 9:00 AM – 1:00 PM' },
      '2026-10-03': { badge: '2', tone: 'neutral', description: '2 slots · 9:00 AM – 2:00 PM' },
      '2026-10-04': { badge: '2', tone: 'neutral', description: '2 slots · 10:00 AM – 1:00 PM' },
    });

    const quiet = await mount(`${TARGET} slot-counts="false"`);
    expect(calendar(quiet.page).dayMeta['2026-10-01'].badge).toBeUndefined();
  });

  it('the times header carries the day’s summary; the calendar can hide Today', async () => {
    const { page } = await mount(`${TARGET} show-today="false"`);

    expect(calendar(page).showToday).toBe(false);
    await pickDay(page, '2026-10-01');
    expect(root(page).querySelector('.bc-title-summary').textContent).toBe('4 slots · 09:00 – 13:00');
  });

  it('empty: no times at this branch, in the same box', async () => {
    respond = ok([]);
    const { page } = await mount(TARGET);

    expect(message(page).textContent).toBe('No times available at this branch');
    expectDays(page);
    expect(calendar(page).enabledDates).toEqual([]);
  });

  it('empty: a veil over the greyed month says so, with an illustration and a polite announcement; it can be turned off', async () => {
    respond = ok([]);
    const { page, el } = await mount(TARGET);
    const veil = root(page).querySelector('.bc-empty');

    expect(veil.hasAttribute('data-shown')).toBe(true);
    expect(veil.querySelector('svg')).not.toBeNull();
    expect(veil.textContent).toBe('No available slots');
    expect(Array.from(root(page).querySelectorAll('.bc-sr[aria-live]')).map(node => node.textContent)).toContain('No available slots');
    expect(calendar(page).disabled).toBe(false);

    (el as unknown as { showEmptyState: boolean }).showEmptyState = false;
    await settle(page);
    expect(veil.hasAttribute('data-shown')).toBe(false);

    respond = ok(DAYS);
    const ready = await mount(TARGET.replace('branch-id="Xr8pQ"', 'branch-id="Mn2vK"'));
    expect(root(ready.page).querySelector('.bc-empty').hasAttribute('data-shown')).toBe(false);
  });

  it('error with retry', async () => {
    respond = () => Promise.resolve({ ok: false, status: 502, json: () => Promise.resolve({}) });
    const { page } = await mount(TARGET);

    expect(message(page).getAttribute('data-message')).toBe('error');
    expect(message(page).querySelector('.bc-message-text').textContent).toBe("Couldn't load available times");
    expect(requests).toHaveLength(1);
    expectDays(page);

    respond = ok(DAYS);
    message(page).querySelector<HTMLButtonElement>('.bc-retry').click();
    await settle(page);

    expect(requests).toHaveLength(2);
    expect(message(page).getAttribute('data-message')).toBe('pickDay');
  });

  it('every message stays mounted; only the active one is exposed', async () => {
    const { page } = await mount(TARGET);
    const messages = Array.from(root(page).querySelectorAll('.bc-message'));

    expect(messages.map(node => node.getAttribute('data-message'))).toEqual(['idle', 'loading', 'empty', 'error', 'pickDay', 'pickTime', 'selected', 'invalid']);
    for (const node of messages) {
      const active = node.hasAttribute('data-active');
      expect(node.getAttribute('aria-hidden')).toBe(active ? null : 'true');
      expect(node.hasAttribute('inert')).toBe(!active);
    }
  });

  it('the times view is always mounted inside the box, over the calendar, never below it', async () => {
    const { page } = await mount(TARGET);
    const children = Array.from(box(page).children).map(node => node.tagName.toLowerCase() + (node.className ? `.${node.className}` : ''));

    expect(children).toEqual(['shift-calendar', 'div.bc-empty', 'div.bc-times']);
    expect(times(page).querySelector('shift-time-slots')).not.toBeNull();
    expect(root(page).querySelector('.bc-slots, .bc-collapsible, .bc-error')).toBeNull();
    expect(Array.from(root(page).querySelector('.bc-root').children).map(node => node.className)).toEqual(['bc-collapse', 'bc-box', 'bc-sr', 'bc-sr', 'bc-collapse']);
  });

  it('the label and the status line are off by default and slide open when asked for', async () => {
    const { page, el } = await mount(TARGET);
    const [label, status] = Array.from(root(page).querySelectorAll('.bc-root > .bc-collapse'));

    expect(label.querySelector('.bc-label')).not.toBeNull();
    expect(status.querySelector('.bc-status')).not.toBeNull();
    for (const region of [label, status]) {
      expect(region.hasAttribute('data-open')).toBe(false);
      expect(region.getAttribute('aria-hidden')).toBe('true');
    }

    el.showLabel = true;
    el.showStatus = true;
    await settle(page);

    for (const region of [label, status]) {
      expect(region.hasAttribute('data-open')).toBe(true);
      expect(region.getAttribute('aria-hidden')).toBeNull();
    }
  });
});

describe('picker contract', () => {
  it('reports status for a host and the picked slot as ISO with an offset, complete on a time', async () => {
    const statuses: string[] = [];
    const changes: { value: string; label: string; complete: boolean }[] = [];
    const page = await newSpecPage({ components: [ShiftBookingCalendar, ShiftCalendar, ShiftTimeSlots], html: '<div></div>' });
    const el = page.doc.createElement('shift-booking-calendar') as HTMLShiftBookingCalendarElement;

    el.addEventListener('pickerStatus', (event: CustomEvent<{ text: string }>) => statuses.push(event.detail.text));
    el.addEventListener('pickerChange', (event: CustomEvent<{ value: string; label: string; complete: boolean }>) => changes.push(event.detail));
    for (const [, name, value] of `${TARGET} utc-offset="+03:00"`.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
    page.body.querySelector('div').appendChild(el);
    await settle(page);

    expect(statuses).toEqual(['Loading available days…', '']);

    await pickDay(page, '2026-10-01');
    expect(changes).toEqual([]);

    await pickTime(page, '13:00');
    expect(changes).toEqual([{ value: '2026-10-01T13:00:00+03:00', label: '1 Oct, 13:00', complete: true }]);
    expect(el.value).toBe('2026-10-01T13:00:00+03:00');

    el.branchId = '43';
    await settle(page);
    expect(changes[changes.length - 1]).toEqual({ value: '', label: '', complete: false });
  });

  it('is busy the moment its branch is set, in the same pass, so a host never shows the field enabled before loading', async () => {
    const { el } = await mount(TARGET.replace('branch-id="Xr8pQ"', ''));
    const busy: boolean[] = [];
    el.addEventListener('pickerStatus', (event: CustomEvent<{ busy: boolean }>) => busy.push(event.detail.busy));

    el.branchId = 'Xr8pQ';
    expect(busy).toEqual([true]);
  });

  it('a host hears loading in words, and nothing while idle or empty (empty is said inside the picker)', async () => {
    const heard = async (attributes: string) => {
      const statuses: string[] = [];
      const page = await newSpecPage({ components: [ShiftBookingCalendar, ShiftCalendar, ShiftTimeSlots], html: '<div></div>' });
      const el = page.doc.createElement('shift-booking-calendar');

      el.addEventListener('pickerStatus', (event: CustomEvent<{ text: string }>) => statuses.push(event.detail.text));
      for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
      page.body.querySelector('div').appendChild(el);
      await settle(page);

      return statuses;
    };

    expect(await heard('department-id="showroom"')).toEqual([]);

    respond = () => new Promise(() => undefined);
    expect(await heard(TARGET)).toEqual(['Loading available days…']);

    respond = ok([]);
    expect((await heard(TARGET)).pop()).toBe('');
  });

  it('takes a value in: a slot the branch has is chosen and labelled, empty clears', async () => {
    const changes: { value: string; label: string; complete: boolean }[] = [];
    const { page, el } = await mount(`${TARGET} utc-offset="+03:00"`);

    el.addEventListener('pickerChange', (event: CustomEvent<{ value: string; label: string; complete: boolean }>) => changes.push(event.detail));
    el.value = '2026-10-03T14:00:00+03:00';
    await settle(page);

    expect(calendar(page).value).toBe('2026-10-03');
    expect(changes).toEqual([{ value: '2026-10-03T14:00:00+03:00', label: '3 Oct, 14:00', complete: false }]);

    el.value = '';
    await settle(page);
    expect(calendar(page).value).toBe('');
  });

  it('the inner calendar’s own pickerChange never leaves the booking calendar', async () => {
    const { page, el } = await mount(TARGET);
    const seen: unknown[] = [];

    el.addEventListener('pickerChange', event => seen.push((event as CustomEvent).detail));
    await pickDay(page, '2026-10-01');

    expect(seen).toEqual([]);
  });
});

describe('value rules', () => {
  type Change = { value: string; label: string; complete: boolean };

  async function hosted(attributes: string) {
    const changes: Change[] = [];
    const page = await newSpecPage({ components: [ShiftBookingCalendar, ShiftCalendar, ShiftTimeSlots], html: '<div></div>' });
    const el = page.doc.createElement('shift-booking-calendar') as HTMLShiftBookingCalendarElement;

    el.addEventListener('pickerChange', (event: CustomEvent<Change>) => changes.push(event.detail));
    for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
    page.body.querySelector('div').appendChild(el);
    await settle(page);

    return { page, el, changes };
  }

  it('a Blazor ref hears the current label when it attaches, then every change through change-callback', async () => {
    const { page, el } = await hosted(`${TARGET} change-callback="OnPickerChange" value="2026-09-20T10:00:00+03:00"`);
    const calls: unknown[][] = [];
    await el.setBlazorRef({ invokeMethodAsync: async (name: string, ...args: unknown[]) => void calls.push([name, ...args]) });

    expect(calls).toEqual([['OnPickerChange', '2026-09-20T10:00:00+03:00', '20 Sep, 10:00', false]]);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '13:00');
    expect(calls.at(-1)).toEqual(['OnPickerChange', '2026-10-01T13:00:00+03:00', '1 Oct, 13:00', true]);
  });

  it('a time kept on another day is only confirmed when clicked, and only the booking calendar reports picks', async () => {
    const { page, changes } = await hosted(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '13:00');
    await pickDay(page, '2026-10-04');
    expect(changes.at(-1)).toMatchObject({ value: '2026-10-04T13:00:00', complete: false });

    await pickTime(page, '13:00');
    expect(changes.at(-1)).toMatchObject({ value: '2026-10-04T13:00:00', complete: true });
    expect(changes.every(change => /T\d\d:\d\d:00/.test(change.value) || change.value === '')).toBe(true);
  });

  it('a function change-callback set after load hears the current label, then every change', async () => {
    const { page, el } = await hosted(`${TARGET} value="2026-09-20T10:00:00+03:00"`);
    const calls: unknown[][] = [];
    el.changeCallback = (...args: unknown[]) => void calls.push(args);
    await settle(page);

    expect(calls).toEqual([['2026-09-20T10:00:00+03:00', '20 Sep, 10:00', false]]);
    await pickDay(page, '2026-10-01');
    await pickTime(page, '13:00');
    expect(calls.at(-1)).toEqual(['2026-10-01T13:00:00+03:00', '1 Oct, 13:00', true]);
  });

  it('a value that is not a time (past, or from elsewhere) is kept, labelled on load and never cleared by the picker', async () => {
    const { page, el, changes } = await hosted(`${TARGET} value="2026-09-20T10:00:00+03:00"`);

    expect(changes).toEqual([{ value: '2026-09-20T10:00:00+03:00', label: '20 Sep, 10:00', complete: false }]);
    expect(el.value).toBe('2026-09-20T10:00:00+03:00');
    expect(calendar(page).value).toBeFalsy();
    expect(await el.getValueLabel()).toBe('20 Sep, 10:00');

    await pickDay(page, '2026-10-01');
    expect(el.value).toBe('2026-09-20T10:00:00+03:00');
    expect(changes).toHaveLength(1);
  });

  it('the label comes before the branch does, while the picker is idle', async () => {
    const { el, changes } = await hosted('calendar-api="https://calendar.example/api/public/calendar" value="2026-10-03T14:00:00+03:00"');

    expect(changes).toEqual([{ value: '2026-10-03T14:00:00+03:00', label: '3 Oct, 14:00', complete: false }]);
    expect(el.value).toBe('2026-10-03T14:00:00+03:00');
  });

  it('a branch and a value set together are not a change; an id filled in later is not either', async () => {
    const { page, el, changes } = await hosted('calendar-api="https://calendar.example/api/public/calendar" department-id="showroom" today="2026-09-28"');

    el.branchId = 'Xr8pQ';
    el.value = '2026-10-03T14:00:00+03:00';
    await settle(page);
    el.brandId = 'BRAND-A';
    await settle(page);

    expect(el.value).toBe('2026-10-03T14:00:00+03:00');
    expect(slotsEl(page).value).toBe('14:00');
    expect(changes.every(change => change.value)).toBe(true);

    el.branchId = '43';
    el.value = '2026-10-04T10:00:00+03:00';
    await settle(page);
    expect(el.value).toBe('2026-10-04T10:00:00+03:00');
  });

  it('a later branch, department or brand change clears it, with value "" and complete false', async () => {
    for (const [prop, next] of [
      ['branchId', '43'],
      ['departmentId', 'service-center'],
      ['brandId', 'BRAND-B'],
    ] as const) {
      const { page, el, changes } = await hosted(`${TARGET} value="2026-09-20T10:00:00+03:00"`);

      el[prop] = next;
      await settle(page);

      expect(el.value).toBe('');
      expect(changes.at(-1)).toEqual({ value: '', label: '', complete: false });
    }
  });

  it('the endpoint or today changing keeps it', async () => {
    const { page, el } = await hosted(`${TARGET} value="2026-10-03T14:00:00+03:00"`);

    el.today = '2026-09-29';
    el.calendarApi = 'https://calendar.example/api/public/calendar?x=1';
    await settle(page);

    expect(el.value).toBe('2026-10-03T14:00:00+03:00');
    expect(slotsEl(page).value).toBe('14:00');
  });

  it('value-format writes the value as a pattern, and reads it back or as ISO', async () => {
    const { page, el, changes } = await hosted(`${TARGET} value-format="yyyy-MM-dd HH:mm" value="2026-10-03T14:00:00+03:00"`);

    expect(changes.at(-1)).toEqual({ value: '2026-10-03 14:00', label: '3 Oct, 14:00', complete: false });

    await pickDay(page, '2026-10-01');
    await pickTime(page, '13:00');
    expect(changes.at(-1)).toEqual({ value: '2026-10-01 13:00', label: '1 Oct, 13:00', complete: true });

    el.value = '2026-10-04 10:00';
    await settle(page);
    expect(calendar(page).value).toBe('2026-10-04');
    expect(slotsEl(page).value).toBe('10:00');
  });

  it('without utc-offset the ISO value carries no offset', async () => {
    const { page, changes } = await hosted(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '09:00');
    expect(changes.at(-1).value).toBe('2026-10-01T09:00:00');
  });
});

describe('views', () => {
  it('an open day switches to its times: header, list, message and announcement', async () => {
    const { page, slots } = await mount(TARGET);

    await pickDay(page, '2026-10-01');

    expectTimes(page);
    expect(calendar(page).value).toBe('2026-10-01');
    expect(title(page)).toBe('Thu, 1 Oct');
    expect(slotsEl(page).times).toEqual(['09:00', '10:00', '12:00', '13:00']);
    expect(slotsEl(page).label).toBe('Times on Thursday, 1 October 2026');
    expect(message(page).textContent).toBe('Pick a time');
    expect(announced(page)).toBe('Times on Thursday, 1 October 2026');
    expect(slots).toEqual([]);
  });

  it('a closed day does not switch', async () => {
    const { page } = await mount(TARGET);

    await pickDay(page, '2026-10-02');

    expectDays(page);
    expect(calendar(page).value).toBeFalsy();
  });

  it('the header is localized and the back control is labelled', async () => {
    const { page } = await mount(`${TARGET} language="ar"`);

    await pickDay(page, '2026-10-01');

    expect(title(page)).toBe('١ تشرين الأول');
    expect(root(page).querySelector('.bc-back').getAttribute('aria-label')).toBe('تغيير التاريخ');
    expect(root(page).querySelector('.bc-back-text').textContent).toBe('تغيير التاريخ');
  });

  it('back returns to the days with the day still chosen and focused', async () => {
    const focusDay = jest.spyOn(ShiftCalendar.prototype, 'setFocus');
    const { page } = await mount(TARGET);

    await pickDay(page, '2026-10-03');
    focusDay.mockClear();
    await back(page);

    expectDays(page);
    expect(calendar(page).value).toBe('2026-10-03');
    expect(focusDay).toHaveBeenCalledTimes(1);
    expect(message(page).textContent).toBe('Pick a day');
    expect(announced(page)).toBe('October 2026');
    focusDay.mockRestore();
  });

  it('Escape in the times view does the same; elsewhere it is left alone', async () => {
    const focusDay = jest.spyOn(ShiftCalendar.prototype, 'setFocus');
    const { page } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    focusDay.mockClear();
    await key(page, 'Escape');

    expectDays(page);
    expect(calendar(page).value).toBe('2026-10-01');
    expect(focusDay).toHaveBeenCalledTimes(1);

    await key(page, 'Escape');
    expectDays(page);
    expect(focusDay).toHaveBeenCalledTimes(1);
    focusDay.mockRestore();
  });

  it('opening the times moves focus into the list', async () => {
    const focusTimes = jest.spyOn(ShiftTimeSlots.prototype, 'setFocus');
    const { page } = await mount(TARGET);

    await pickDay(page, '2026-10-01');

    expect(focusTimes).toHaveBeenCalledTimes(1);
    focusTimes.mockRestore();
  });

  it('a time commits, stays on the times view and shows in the header', async () => {
    const { page, field, slots } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '13:00');

    expectTimes(page);
    expect(slots).toEqual([{ date: '2026-10-01', raw: '2026-10-01 01:00 PM', value: '2026-10-01T13:00' }]);
    expect(field.getValue()).toBe('2026-10-01T13:00');
    expect(slotsEl(page).value).toBe('13:00');
    expect(root(page).querySelector('.bc-slot-layer:last-child').textContent).toBe('· 13:00');
    expect(message(page).textContent).toBe('Thursday, 1 October 2026 at 13:00');
  });

  it('the time in the header uses the hour cycle', async () => {
    const { page } = await mount(`${TARGET} hour-cycle="h12"`);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '13:00');

    expect(root(page).querySelector('.bc-slot-layer:last-child').textContent).toBe('· 1:00 PM');
  });

  it('reopening a day that has the chosen time keeps it; one without clears it', async () => {
    const { page, field, slots } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '09:00');
    await back(page);
    await pickDay(page, '2026-10-03');

    expectTimes(page);
    expect(slots.at(-1)).toEqual({ date: '2026-10-03', raw: '2026-10-03 09:00 AM', value: '2026-10-03T09:00' });
    expect(slotsEl(page).times).toEqual(['09:00', '14:00']);
    expect(slotsEl(page).value).toBe('09:00');
    expect(root(page).querySelector('.bc-title-day').textContent).toBe('Sat, 3 Oct');
    expect(root(page).querySelector('.bc-slot-layer:last-child').textContent).toBe('· 09:00');

    await back(page);
    await pickDay(page, '2026-10-04');

    expect(slots).toHaveLength(2);
    expect(slotsEl(page).value).toBe('');
    expect(field.getValue()).toBe('');
    expect(title(page)).toBe('Sun, 4 Oct');
    expect(message(page).getAttribute('data-message')).toBe('pickTime');
  });

  it('reopening the same day keeps its time and sends nothing new', async () => {
    const { page, slots } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '10:00');
    await back(page);
    await pickDay(page, '2026-10-01');

    expectTimes(page);
    expect(slotsEl(page).value).toBe('10:00');
    expect(slots).toHaveLength(1);
  });

  it('a new branch returns to the days and clears the selection', async () => {
    const { page, el, field } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '10:00');
    el.branchId = '43';
    await settle(page);

    expectDays(page);
    expect(requests).toHaveLength(2);
    expect(new URL(requests[1]).searchParams.get('branchId')).toBe('43');
    expect(field.getValue()).toBe('');
    expect(calendar(page).value).toBeFalsy();
  });

  it('ids becoming idle return to the days and clear the selection', async () => {
    const { page, el, field } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '10:00');
    el.branchId = undefined;
    await settle(page);

    expectDays(page);
    expect(message(page).getAttribute('data-message')).toBe('idle');
    expect(field.getValue()).toBe('');
    expect(calendar(page).value).toBeFalsy();
  });

  it('a branch change does not pull focus into the calendar unless it was in the times view', async () => {
    const focusDay = jest.spyOn(ShiftCalendar.prototype, 'setFocus');
    const { page, el } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    focusDay.mockClear();
    el.branchId = '43';
    await settle(page);

    expectDays(page);
    expect(focusDay).not.toHaveBeenCalled();
    focusDay.mockRestore();
  });

  it('a rule that closes the chosen day returns to the days', async () => {
    const { page, el } = await mount(TARGET);

    await pickDay(page, '2026-10-03');
    el.disabledDates = ['2026-10-03'];
    await settle(page);

    expectDays(page);
    expect(calendar(page).value).toBeFalsy();
  });
});

describe('selection', () => {
  it('a new branch drops a defaultValue given for the old one', async () => {
    const { page, el, field } = await mount(`${TARGET} default-value="2026-10-03T14:00"`);

    expect(field.getValue()).toBe('2026-10-03T14:00');

    respond = ok([day('2026-10-12', '03:00 PM')]);
    el.branchId = '43';
    await settle(page);

    expect(field.getValue()).toBe('');
    expect(slotsEl(page).value).toBe('');
  });

  it('ids that arrive while idle keep the defaultValue', async () => {
    const { page, el, field } = await mount(
      'calendar-api="https://calendar.example/api/public/calendar" department-id="showroom" brand-id="BRAND" today="2026-09-28" default-value="2026-10-03T14:00"',
    );

    el.branchId = 'Xr8pQ';
    await settle(page);

    expect(field.getValue()).toBe('2026-10-03T14:00');
    expect(slotsEl(page).value).toBe('14:00');
  });

  it('loading after a new branch shows no day, the same as loading from idle', async () => {
    const { page, el } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '10:00');
    respond = () => new Promise(() => undefined);
    el.branchId = '43';
    await settle(page);

    expect(message(page).getAttribute('data-message')).toBe('loading');
    expect(calendar(page).value).toBeFalsy();
    expectDays(page);
  });

  it('ids and endpoint changed together send one request', async () => {
    const { page, el } = await mount(TARGET);

    el.calendarApi = 'https://calendar.example/api/calendar';
    el.branchId = 'Mn2vK';
    el.departmentId = 'service-center';
    await settle(page);

    expect(requests).toHaveLength(2);
    expect(requests[1]).toMatch(/^https:\/\/calendar\.example\/api\/calendar\?from=[^&]+&to=[^&]+&branchId=Mn2vK&departmentId=service-center&/);
  });

  it('defaultValue that is a time opens on the times of its day, with the time chosen', async () => {
    const { page, field } = await mount(`${TARGET} default-value="2026-10-03T14:00"`);

    expect(calendar(page).value).toBe('2026-10-03');
    expect(slotsEl(page).value).toBe('14:00');
    expect(field.getValue()).toBe('2026-10-03T14:00');
    expectTimes(page);
  });

  it('disabled rules skip days; the month of the first open day opens', async () => {
    const { page } = await mount(`${TARGET} disabled-dates="2026-10-01"`);

    expect(calendar(page).month).toBe('2026-10');
    expect(calendar(page).value).toBeFalsy();
  });

  it('isDisabled disables both children', async () => {
    const { page } = await mount(`${TARGET} is-disabled="true"`);

    expect(calendar(page).disabled).toBe(true);
    expect(slotsEl(page).disabled).toBe(true);
    expect(root(page).querySelector('.bc-root').getAttribute('aria-disabled')).toBe('true');
  });

  it('appearance, colour scheme and size reach both children', async () => {
    const { page } = await mount(`${TARGET} appearance="soft" color-scheme="dark" size="lg"`);

    for (const child of [calendar(page), slotsEl(page)]) {
      expect([child.getAttribute('appearance'), child.getAttribute('color-scheme'), child.getAttribute('size')]).toEqual(['soft', 'dark', 'lg']);
    }
  });

  it('reset clears the chosen day and time and returns to the days', async () => {
    const { page, field } = await mount(TARGET);

    await pickDay(page, '2026-10-01');
    await pickTime(page, '10:00');
    field.reset();
    await settle(page);

    expect(field.getValue()).toBe('');
    expect(calendar(page).value).toBeFalsy();
    expectDays(page);
  });
});

describe('same contract as branch-slot-picker (D6)', () => {
  interface Legacy {
    handleTime: (raw: string) => void;
    getValue: () => string;
  }

  async function picker(attributes: string) {
    const page = await newSpecPage({ components: [BranchSlotPicker], html: `<branch-slot-picker ${attributes}></branch-slot-picker>` });
    const slots: BranchSlotSelection[] = [];

    page.root.addEventListener('slotChange', (event: CustomEvent<BranchSlotSelection>) => slots.push(event.detail));
    await settle(page);

    return { page, slots, instance: page.rootInstance as Legacy };
  }

  it('the same slot gives the same slotChange detail and the same submitted value', async () => {
    const legacy = await picker(TARGET);
    const booking = await mount(TARGET);

    for (const [raw, time, date] of [
      ['2026-10-01 01:00 PM', '13:00', '2026-10-01'],
      ['2026-10-03 09:00 AM', '09:00', '2026-10-03'],
      ['2026-10-04 10:00 AM', '10:00', '2026-10-04'],
      ['2026-10-01 12:00 PM', '12:00', '2026-10-01'],
    ]) {
      legacy.instance.handleTime(raw);
      await legacy.page.waitForChanges();

      if (calendar(booking.page).value !== date) {
        if (view(booking.page) === 'times') await back(booking.page);
        await pickDay(booking.page, date);
      }
      await pickTime(booking.page, time);

      expect(booking.slots.at(-1)).toEqual(legacy.slots.at(-1));
      expect(booking.field.getValue()).toBe(legacy.instance.getValue());
    }

    expect(legacy.slots).toHaveLength(4);
    expect(booking.slots.map(slot => slot.value)).toEqual(['2026-10-01T13:00', '2026-10-03T09:00', '2026-10-04T10:00', '2026-10-01T10:00', '2026-10-01T12:00']);
  });

  it('the same defaultValue submits the same value before anything is picked', async () => {
    const legacy = await picker(`${TARGET} default-value="2026-10-03T14:00"`);
    const booking = await mount(`${TARGET} default-value="2026-10-03T14:00"`);

    expect(booking.field.getValue()).toBe(legacy.instance.getValue());
    expect(booking.field.getValue()).toBe('2026-10-03T14:00');
  });
});

describe('field contract with form-hook as it is', () => {
  const NativeFormData = globalThis.FormData;

  // Node's FormData rejects mock-doc forms; the hook reads subscribed fields through getValue() anyway.
  beforeAll(
    () =>
      ((globalThis as unknown as { FormData: unknown }).FormData = class {
        entries() {
          return [][Symbol.iterator]();
        }
      }),
  );
  afterAll(() => (globalThis.FormData = NativeFormData));

  async function inForm(attributes: string) {
    const page = await newSpecPage({ components: [ShiftBookingCalendar, ShiftCalendar, ShiftTimeSlots], html: '<div id="host"><form></form></div>' });
    const submitted: Record<string, unknown>[] = [];
    const hook = new FormHook<Record<string, unknown>>(
      { el: page.body.querySelector('#host') as HTMLElement, isLoading: false, locale: {}, language: 'en', formSubmit: values => void submitted.push(values) },
      object({}),
    );
    const el = page.doc.createElement('shift-booking-calendar') as HTMLShiftBookingCalendarElement;

    for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
    el.form = hook;
    page.body.querySelector('form').appendChild(el);
    await settle(page);

    return { page, hook, submitted };
  }

  const invalid = (page: SpecPage) => root(page).querySelector('.bc-message[data-message="invalid"]');

  it('subscribes under its name, reports required in the status row, and submits the picked slot', async () => {
    const { page, hook, submitted } = await inForm(`${TARGET} is-required="true"`);

    expect(hook.getInputState('bookingCalendar').isRequired).toBe(true);
    expect(root(page).querySelector('.bc-required').hasAttribute('data-hidden')).toBe(false);

    hook.submit();
    await settle(page);

    expect(submitted).toEqual([]);
    expect(hook.getInputState('bookingCalendar').isError).toBe(true);
    expect(invalid(page).hasAttribute('data-active')).toBe(true);
    expect(invalid(page).querySelector('[part="error"]').getAttribute('role')).toBe('alert');
    expect(invalid(page).textContent).toBe('Please choose a date and time');

    await pickDay(page, '2026-10-01');
    await pickTime(page, '13:00');

    expect(hook.getInputState('bookingCalendar').isError).toBe(false);
    expect(invalid(page).hasAttribute('data-active')).toBe(false);
    expect(hook.getValues<Record<string, string>>().bookingCalendar).toBe('2026-10-01T13:00');

    hook.submit();
    await settle(page);

    expect(submitted).toEqual([{ bookingCalendar: '2026-10-01T13:00' }]);
  });

  it('shows the localization require text and follows the form reset', async () => {
    const { page, hook } = await inForm(`${TARGET} is-required="true" name="slot"`);
    const el = page.body.querySelector('shift-booking-calendar') as HTMLShiftBookingCalendarElement;

    el.localization = { en: { require: 'Choose a time to continue' } } as unknown as HTMLShiftBookingCalendarElement['localization'];
    hook.submit();
    await settle(page);

    expect(root(page).querySelector('.bc-error-text').textContent).toBe('Choose a time to continue');

    await pickDay(page, '2026-10-01');
    await pickTime(page, '10:00');
    expect(hook.getValues<Record<string, string>>().slot).toBe('2026-10-01T10:00');

    hook.formStructure = { currentStep: 1 } as FormHook<unknown>['formStructure'];
    hook.reset();
    await settle(page);
    await new Promise(resolve => setTimeout(resolve, 120));
    await settle(page);

    expect(hook.getValues<Record<string, string>>().slot).toBe('');
    expect(slotsEl(page).value).toBe('');
    expectDays(page);
  });

  it('an optional field submits empty', async () => {
    const { page, hook, submitted } = await inForm(TARGET);

    expect(hook.getInputState('bookingCalendar').isRequired).toBe(false);

    hook.submit();
    await settle(page);

    expect(submitted).toEqual([{ bookingCalendar: '' }]);
  });
});

describe('label format', () => {
  const VALUE = 'value="2026-09-20T10:00:00+03:00"';
  const label = async (attributes: string, setup?: (el: HTMLShiftBookingCalendarElement) => void) => {
    const { page, el } = await mount(`${VALUE} ${attributes}`);
    setup?.(el);
    await settle(page);
    return el.getValueLabel();
  };

  it('defaults to the language’s pattern, in its names and digits, following hour-cycle', async () => {
    expect(await label('')).toBe('20 Sep, 10:00');
    expect(await label('language="ar" hour-cycle="h12"')).toBe('٢٠ أيلول، ١٠:٠٠ ص');
  });

  it('a label-format from the host wins, with the calendar’s own month and day names', async () => {
    expect(await label('label-format="d MMMM yyyy, h:mm a"')).toBe('20 September 2026, 10:00 AM');
    expect(await label('language="ar" label-format="EEE d MMM"')).toBe('أحد ٢٠ أيلول');
  });

  it('per language: the language’s own pattern, else its default', async () => {
    const perLanguage = (el: HTMLShiftBookingCalendarElement) => (el.labelFormat = { ar: 'd MMMM' });

    expect(await label('language="ar"', perLanguage)).toBe('٢٠ أيلول');
    expect(await label('', perLanguage)).toBe('20 Sep, 10:00');
  });

  it('day-title-format sets the chosen day’s title, else the language’s default', async () => {
    const titled = async (attributes: string) => {
      const { page } = await mount(`${TARGET} ${attributes}`);
      await pickDay(page, '2026-10-01');
      return title(page);
    };

    expect(await titled('day-title-format="EEEE d MMMM"')).toBe('Thursday 1 October');
    expect(await titled('language="ar"')).toContain('١ تشرين الأول');
  });

  it('an unusable pattern falls back to the default', async () => {
    expect(await label('label-format="nope"')).toBe('20 Sep, 10:00');
  });
});
