import '../shift-booking-calendar/validation.mock';

import { newSpecPage, SpecPage } from '@stencil/core/testing';
import { object } from 'yup';

import { FormHook } from '~features/form-hook/form-hook';
import { clearAvailabilityCache, slotIso } from '~lib/booking-availability';

import { getFormMappers } from '../../forms/defaults/mappers';
import { ShiftPortal } from '../../components/shift-portal';
import { ShiftPopover } from '../../components/shift-popover/shift-popover';
import { ShiftPopoverPanel } from '../../components/shift-popover/shift-popover-panel';
import { ShiftBookingCalendar } from '../shift-booking-calendar/shift-booking-calendar';
import { ShiftCalendar } from '../shift-calendar/shift-calendar';
import { ShiftTimeSlots } from '../shift-time-slots/shift-time-slots';
import { ShiftInput } from './shift-input';

const day = (date: string, ...times: string[]) => ({ Date: date, Times: times.map(time => `${date} ${time}`) });

const DAYS = [day('2026-10-01', '08:00 AM', '10:00 AM', '01:00 PM', '03:30 PM'), day('2026-10-03', '09:00 AM')];

const TARGET =
  'calendar-api="https://calendar.example/api/public/calendar" branch-id="Xr8pQ" department-id="service-center" brand-id="BRAND-A" today="2026-09-28" utc-offset="+03:00"';

let requests: string[];

beforeEach(() => {
  clearAvailabilityCache();
  requests = [];
  (globalThis as unknown as { fetch: unknown }).fetch = (url: string) => {
    requests.push(url);
    return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(DAYS) });
  };
});

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

const settle = async (page: SpecPage) => {
  for (let i = 0; i < 6; i++) {
    await new Promise(resolve => setTimeout(resolve, 0));
    await page.waitForChanges();
  }
};

const setAttributes = (el: Element, attributes: string) => {
  for (const [, name, value] of attributes.matchAll(/([\w-]+)="([^"]*)"/g)) el.setAttribute(name, value);
};

async function inForm(inputAttributes: string, pickerAttributes = TARGET, requiredContext: Record<string, boolean> = {}) {
  const page = await newSpecPage({
    components: [ShiftInput, ShiftPopover, ShiftPopoverPanel, ShiftPortal, ShiftBookingCalendar, ShiftCalendar, ShiftTimeSlots],
    html: '<div id="host"><form></form></div>',
  });
  const submitted: Record<string, unknown>[] = [];
  const hook = new FormHook<Record<string, unknown>>(
    {
      el: page.body.querySelector('#host') as HTMLElement,
      isLoading: false,
      locale: {},
      language: 'en',
      structure: { requiredContext, name: 'bookingDate' },
      formSubmit: values => void submitted.push(values),
    } as never,
    object({}),
  );
  const input = page.doc.createElement('shift-input') as HTMLShiftInputElement;
  const picker = page.doc.createElement('shift-booking-calendar') as HTMLShiftBookingCalendarElement;

  setAttributes(input, `name="bookingDate" ${inputAttributes}`);
  setAttributes(picker, `slot="picker" ${pickerAttributes}`);
  input.form = hook as never;
  page.body.querySelector('form').addEventListener('input', hook.formController.onInput);
  input.appendChild(picker);
  page.body.querySelector('form').appendChild(input);
  await settle(page);

  return { page, input, picker, hook, submitted };
}

const field = (page: SpecPage) => page.body.querySelector('shift-input').shadowRoot;
const booking = (page: SpecPage) => page.body.querySelector('shift-booking-calendar') as HTMLShiftBookingCalendarElement;
const calendar = (page: SpecPage) => booking(page).shadowRoot.querySelector('shift-calendar') as HTMLShiftCalendarElement;
const slots = (page: SpecPage) => booking(page).shadowRoot.querySelector('shift-time-slots') as HTMLShiftTimeSlotsElement;
const text = (page: SpecPage) => (field(page).querySelector('.in-input') as HTMLInputElement).value;
const hint = (page: SpecPage) => field(page).querySelector('.in-hint');
const error = (page: SpecPage) => field(page).querySelector('.in-error');

async function pick(page: SpecPage, date: string, time: string) {
  calendar(page).shadowRoot.querySelector<HTMLElement>(`.cal-view[data-current] .cal-page[data-current] [data-date="${date}"]`).click();
  await settle(page);
  slots(page).shadowRoot.querySelector<HTMLButtonElement>(`.ts-layer[data-current] [data-time="${time}"]`).click();
  await settle(page);
}

describe('slotIso', () => {
  it('writes the slot with seconds and the configured offset', () => {
    expect(slotIso('2026-10-01T13:00', '+03:00')).toBe('2026-10-01T13:00:00+03:00');
    expect(slotIso('', '+03:00')).toBe('');
  });

  it('falls back to the browser offset for that wall-clock time', () => {
    expect(slotIso('2026-10-01T13:00')).toMatch(/^2026-10-01T13:00:00[+-]\d{2}:\d{2}$/);
    expect(slotIso('2026-10-01T13:00', 'Europe/Istanbul')).toMatch(/^2026-10-01T13:00:00[+-]\d{2}:\d{2}$/);
  });
});

describe('shift-input hosting a picker', () => {
  it('moves the picker into its popover, read-only, with no idle text', async () => {
    const { page, picker } = await inForm('is-disabled="true"', 'calendar-api="https://calendar.example/api/public/calendar"');

    expect(picker.parentElement.tagName).not.toBe('SHIFT-INPUT');
    expect(picker.hasAttribute('slot')).toBe(false);
    expect(field(page).querySelector('.in-root').hasAttribute('data-picker')).toBe(true);
    expect((field(page).querySelector('.in-input') as HTMLInputElement).readOnly).toBe(true);
    expect(hint(page).hasAttribute('data-shown')).toBe(false);
    expect(requests).toEqual([]);
  });

  it('keeps the focused look while its popover is open, though focus is inside the picker', async () => {
    const { page } = await inForm('');
    const control = () => field(page).querySelector('.in-control');

    expect(control().hasAttribute('data-focused')).toBe(false);

    (field(page).querySelector('.in-calendar-button') as HTMLButtonElement).click();
    await settle(page);
    expect(control().hasAttribute('data-focused')).toBe(true);

    await (field(page).querySelector('shift-popover') as HTMLShiftPopoverElement).hide();
    await settle(page);
    expect(control().hasAttribute('data-focused')).toBe(false);
  });

  it('hands mobile-sheet to its popover, off by default', async () => {
    const popover = (page: SpecPage) => field(page).querySelector('shift-popover') as HTMLShiftPopoverElement;

    expect(popover((await inForm('')).page).mobileSheet).toBe(false);
    expect(popover((await inForm('mobile-sheet="true"')).page).mobileSheet).toBe(true);
  });

  it('reads required from the form context, blocks an empty submit, and submits the picked slot as ISO with an offset', async () => {
    const { page, hook, submitted } = await inForm('', TARGET, { bookingDate: true });

    expect(hook.getInputState('bookingDate').isRequired).toBe(true);
    expect(field(page).querySelector('.in-required').hasAttribute('data-hidden')).toBe(false);

    hook.submit();
    await settle(page);

    expect(submitted).toEqual([]);
    expect(error(page).hasAttribute('data-shown')).toBe(true);
    expect(error(page).textContent).toBe('This field is required');

    await pick(page, '2026-10-01', '13:00');

    expect(text(page)).toBe('1 Oct, 13:00');
    expect(hook.getDisplayValue('bookingDate' as never)).toBe('1 Oct, 13:00');
    expect(hook.getInputState('bookingDate').isError).toBe(false);

    hook.submit();
    await settle(page);

    expect(submitted).toEqual([{ bookingDate: '2026-10-01T13:00:00+03:00' }]);
  });

  it('while the picker loads, a spinner by the icon and a live announcement, no visible text', async () => {
    (globalThis as unknown as { fetch: unknown }).fetch = () => new Promise(() => undefined);
    const { page } = await inForm('');

    expect(field(page).querySelector('.in-root').getAttribute('aria-busy')).toBe('true');
    expect(field(page).querySelector('.in-root').hasAttribute('data-disabled')).toBe(true);
    expect((field(page).querySelector('.in-input') as HTMLInputElement).disabled).toBe(true);
    expect(field(page).querySelector('.in-busy').hasAttribute('data-shown')).toBe(true);
    expect(hint(page).hasAttribute('data-shown')).toBe(false);
    expect(field(page).querySelector('.in-sr[aria-live]').textContent).toBe('Loading available days…');
  });

  it('a completed pick lets go of focus instead of returning it to the input', async () => {
    const { page, input } = await inForm('');
    const inner = field(page).querySelector('.in-input') as HTMLInputElement;
    const blur = jest.spyOn(inner, 'blur');
    const focus = jest.spyOn(inner, 'focus');

    await pick(page, '2026-10-01', '13:00');

    expect(input.value).toBe('2026-10-01T13:00:00+03:00');
    expect(blur).toHaveBeenCalled();
    expect(focus.mock.invocationCallOrder.every(order => order < blur.mock.invocationCallOrder[blur.mock.invocationCallOrder.length - 1])).toBe(true);
    expect(field(page).querySelector('.in-busy').hasAttribute('data-shown')).toBe(false);
  });

  it('clears value and text when the branch changes, and when it is cleared', async () => {
    const { page, input, picker, hook } = await inForm('');

    await pick(page, '2026-10-01', '10:00');
    expect(input.value).toBe('2026-10-01T10:00:00+03:00');

    picker.branchId = '43';
    await settle(page);

    expect(input.value).toBe('');
    expect(text(page)).toBe('');
    expect(requests.some(url => url.includes('branchId=43'))).toBe(true);

    await pick(page, '2026-10-03', '09:00');
    expect(hook.getValues<Record<string, string>>().bookingDate).toBe('2026-10-03T09:00:00+03:00');

    picker.branchId = '';
    input.isDisabled = true;
    await settle(page);

    expect(hook.getValues<Record<string, string>>().bookingDate).toBe('');
    expect(text(page)).toBe('');
    expect(hint(page).hasAttribute('data-shown')).toBe(false);
  });

  it('follows the form reset into the picker', async () => {
    const { page, hook } = await inForm('');

    await pick(page, '2026-10-01', '08:00');
    hook.formStructure = { currentStep: 1 } as FormHook<unknown>['formStructure'];
    hook.reset();
    await settle(page);
    await new Promise(resolve => setTimeout(resolve, 120));
    await settle(page);

    expect(hook.getValues<Record<string, string>>().bookingDate).toBe('');
    expect(booking(page).value).toBe('');
    expect(slots(page).value).toBe('');
  });

  it('wears the v1 form look inside a form with no appearance, and its own with one', async () => {
    const plain = await inForm('');
    expect(plain.input.getAttribute('data-look')).toBe('form');

    const styled = await inForm('appearance="soft"');
    expect(styled.input.hasAttribute('data-look')).toBe(false);
  });
});

describe('bookingCalendar mapper', () => {
  const branch = {
    ID: 'Xr8pQ',
    IntegrationId: '42',
    Departments: [{ IntegrationId: 'showroom' }, { IntegrationId: 'service-center' }],
    Brands: [{ IntegrationId: 'BRAND-A' }],
  };
  const formWith = (value: string) =>
    ({ addWatcher: () => undefined, getValue: () => value, context: { companyBranchIdList: [{ value: branch.ID, meta: branch }] } }) as unknown as FormHook<unknown>;

  type VNode = { $tag$: string; $attrs$: Record<string, unknown>; $children$: VNode[] };

  it('renders shift-input with a headless booking calendar fed by the selected branch', () => {
    const vnode = getFormMappers().bookingCalendar({
      form: formWith('Xr8pQ'),
      language: 'en',
      props: { name: 'bookingDate', calendarApi: 'https://calendar.example/api/public/calendar', departmentPreference: ['service-center'], utcOffset: '+03:00', showToday: false },
    }) as unknown as VNode;
    const picker = vnode.$children$[0];

    expect(vnode.$tag$).toBe('shift-input');
    expect(vnode.$attrs$).toMatchObject({ name: 'bookingDate', isDisabled: false });
    expect(picker.$tag$).toBe('shift-booking-calendar');
    expect(picker.$attrs$).toMatchObject({
      slot: 'picker',
      branchId: 'Xr8pQ',
      departmentId: 'service-center',
      brandId: 'BRAND-A',
      utcOffset: '+03:00',
      showToday: false,
    });
    expect(picker.$attrs$).not.toHaveProperty('hourCycle');
    expect(picker.$attrs$).not.toHaveProperty('availabilityDots');
    expect(picker.$attrs$.form).toBeUndefined();
  });

  it('disables the field with no branch selected', () => {
    const vnode = getFormMappers().bookingCalendar({ form: formWith(''), language: 'en', props: { name: 'bookingDate' } }) as unknown as VNode;

    expect(vnode.$attrs$).toMatchObject({ isDisabled: true });
    expect(vnode.$children$[0].$attrs$).toMatchObject({ branchId: '' });
  });

  it('books only a listed department: a branch without one is disabled, loads nothing and says why', () => {
    const render = (props: Record<string, unknown>) =>
      getFormMappers().bookingCalendar({
        form: formWith('Xr8pQ'),
        language: 'en',
        props: { name: 'bookingDate', departmentPreference: ['quick-service-center'], ...props },
      }) as unknown as VNode;
    const vnode = render({});

    expect(vnode.$attrs$).toMatchObject({ isDisabled: true, hint: 'Online booking isn’t available at this branch' });
    expect(vnode.$children$[0].$attrs$).toMatchObject({ branchId: '', departmentId: '' });
    expect(render({ localization: { en: { branchUnavailable: 'Call the branch to book' } } }).$attrs$).toMatchObject({ hint: 'Call the branch to book' });
  });

  it('with services, books any branch and leaves the department to the endpoint unless one is set', () => {
    const render = (props: Record<string, unknown>) =>
      getFormMappers().bookingCalendar({
        form: formWith('Xr8pQ'),
        language: 'en',
        props: { name: 'bookingDate', departmentPreference: ['quick-service-center'], ...props },
      }) as unknown as VNode;

    const vnode = render({ services: 'auto-repair-and-maintenance' });
    expect(vnode.$attrs$).toMatchObject({ isDisabled: false });
    expect(vnode.$children$[0].$attrs$).toMatchObject({ branchId: 'Xr8pQ', departmentId: '', services: 'auto-repair-and-maintenance', brandId: 'BRAND-A' });

    expect(render({ services: ['auto-repair-and-maintenance', 'parts-counter-sale'] }).$children$[0].$attrs$).toMatchObject({ services: 'auto-repair-and-maintenance,parts-counter-sale' });
    expect(render({ services: 'auto-repair-and-maintenance', departmentId: 'showroom' }).$children$[0].$attrs$).toMatchObject({ departmentId: 'showroom' });
    expect(render({}).$children$[0].$attrs$.services).toBeUndefined();
  });

  it('the older bookingDate field still falls back to the branch’s first department', () => {
    const vnode = getFormMappers().bookingDate({
      form: formWith('Xr8pQ'),
      language: 'en',
      props: { name: 'bookingDate', departmentPreference: ['quick-service-center'] },
    }) as unknown as VNode;

    expect(vnode.$attrs$).toMatchObject({ departmentId: 'showroom', isDisabled: false });
  });

  it('passes mobileSheet from the structure to shift-input, and leaves it unset otherwise', () => {
    const render = (props: Record<string, unknown>) =>
      getFormMappers().bookingCalendar({ form: formWith('Xr8pQ'), language: 'en', props: { name: 'bookingDate', ...props } }) as unknown as VNode;

    expect(render({ mobileSheet: true }).$attrs$).toMatchObject({ mobileSheet: true });
    expect(render({}).$attrs$).not.toHaveProperty('mobileSheet');
  });
});
