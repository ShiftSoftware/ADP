import { h } from '@stencil/core';
import { newSpecPage, SpecPage } from '@stencil/core/testing';
import { object, string } from 'yup';

import { FormHook } from '~features/form-hook/form-hook';
import { FormHookInterface } from '~features/form-hook/interface';

import { ShiftCalendar } from '../shift-calendar/shift-calendar';
import { ShiftPortal } from '../../components/shift-portal';
import { ShiftPopover } from '../../components/shift-popover/shift-popover';
import { ShiftPopoverPanel } from '../../components/shift-popover/shift-popover-panel';
import { ShiftInput } from './shift-input';

type Values = { visitDate: string };

// Node's FormData rejects the mock-doc <form>; the field's value reaches the form through getValue() anyway.
class FormDataStub {
  entries() {
    return [][Symbol.iterator]();
  }
}

beforeAll(() => (globalThis.FormData = FormDataStub as unknown as typeof FormData));

const locale = {
  'visitDate-label': 'Visit date',
  'visitDate-placeholder': 'dd/mm/yyyy',
  'visitDate-require': 'Choose a visit date',
  'sharedFormLocales': {},
};

async function mountForm(required: boolean, defaultValue?: string, type: 'date' | 'text' = 'date', appearance?: HTMLShiftInputElement['appearance']) {
  const submitted: Values[] = [];
  const context = {
    el: null as HTMLElement,
    locale,
    language: 'en',
    isLoading: false,
    formSubmit: (values: Values) => submitted.push(values),
  } as unknown as FormHookInterface<Values>;

  const field = string().meta({ label: 'visitDate-label', placeholder: 'visitDate-placeholder' });
  const form = new FormHook<Values>(context, object({ visitDate: required ? field.required('visitDate-require') : field.optional() }));

  const page = await newSpecPage({
    components: [ShiftInput, ShiftPopover, ShiftPopoverPanel, ShiftPortal, ShiftCalendar],
    template: () => (
      <div id="form-root">
        <form onSubmit={form.formController.onSubmit} onInput={form.formController.onInput}>
          <shift-input type={type} name="visitDate" form={form} today="2026-09-27" min="2026-09-01" defaultValue={defaultValue} appearance={appearance} />
        </form>
      </div>
    ),
  });

  context.el = page.body.querySelector('#form-root');

  return { page, form, submitted };
}

const root = (page: SpecPage) => page.body.querySelector('shift-input').shadowRoot;
const input = (page: SpecPage) => root(page).querySelector<HTMLInputElement>('.in-input');
const error = (page: SpecPage) => root(page).querySelector<HTMLElement>('[part~="error"]');
const win = (page: SpecPage) => page.win as unknown as typeof globalThis;

async function type(page: SpecPage, text: string) {
  input(page).value = text;
  input(page).dispatchEvent(new (win(page).Event)('input', { bubbles: true, composed: true }));
  await page.waitForChanges();
  input(page).dispatchEvent(new (win(page).Event)('blur'));
  await page.waitForChanges();
}

async function submit(page: SpecPage, form: FormHook<Values>) {
  form.submit();
  await new Promise(resolve => setTimeout(resolve, 20));
  await page.waitForChanges();
}

describe('shift-input type=date through ~features/form-hook', () => {
  it('behaves like the form’s own fields whatever its appearance, and wears their look only with none chosen', async () => {
    const host = async (appearance?: HTMLShiftInputElement['appearance']) => (await mountForm(false, undefined, 'date', appearance)).page.body.querySelector('shift-input');
    const plain = await host();
    const sharp = await host('sharp');

    expect(plain.hasAttribute('data-in-form')).toBe(true);
    expect(plain.getAttribute('data-look')).toBe('form');
    expect(sharp.hasAttribute('data-in-form')).toBe(true);
    expect(sharp.hasAttribute('data-look')).toBe(false);
  });

  it('takes label, placeholder and required from the form schema and locale', async () => {
    const { page } = await mountForm(true);

    expect(root(page).querySelector('[part~="label"]').textContent).toBe('Visit date*');
    expect(input(page).placeholder).toBe('dd/mm/yyyy');
    expect(input(page).getAttribute('aria-required')).toBe('true');
    expect(root(page).querySelector('[part~="clear"]').hasAttribute('data-hidden')).toBe(true);
  });

  it('type=text: required blocks the submit, typed text is the submitted value', async () => {
    const { page, form, submitted } = await mountForm(true, undefined, 'text');

    await submit(page, form);
    expect(submitted).toEqual([]);
    expect(error(page).textContent).toBe('Choose a visit date');

    await type(page, 'Next Tuesday');
    await submit(page, form);
    expect(submitted).toEqual([{ visitDate: 'Next Tuesday' }]);
  });

  it('a required, empty field blocks the submit and shows the localized message', async () => {
    const { page, form, submitted } = await mountForm(true);

    await submit(page, form);

    expect(submitted).toEqual([]);
    expect(error(page).hasAttribute('data-shown')).toBe(true);
    expect(error(page).textContent).toBe('Choose a visit date');
  });

  it('a typed date is the submitted value, and fixing the field clears the error', async () => {
    const { page, form, submitted } = await mountForm(true);

    await submit(page, form);
    await type(page, '05/10/2026');

    expect(error(page).hasAttribute('data-shown')).toBe(false);
    expect(form.getValues<Values>().visitDate).toBe('2026-10-05');

    await submit(page, form);
    expect(submitted).toEqual([{ visitDate: '2026-10-05' }]);
  });

  it('a date chosen in the calendar is the submitted value', async () => {
    const { page, form, submitted } = await mountForm(true);
    const calendar = Array.from(page.body.children)
      .find(node => node.tagName === 'SHIFT-POPOVER-PANEL')
      .querySelector('shift-calendar');

    calendar.dispatchEvent(new (win(page).CustomEvent)('dateChange', { detail: { value: '2026-09-30' }, bubbles: true, composed: true }));
    await page.waitForChanges();
    await submit(page, form);

    expect(submitted).toEqual([{ visitDate: '2026-09-30' }]);
  });

  it('typed text that is not an allowed date blocks the submit of an optional field', async () => {
    const { page, form, submitted } = await mountForm(false);

    await type(page, '31/02/2026');
    await submit(page, form);

    expect(submitted).toEqual([]);
    expect(error(page).textContent).toBe('Enter a date as dd/mm/yyyy');

    await type(page, '');
    await submit(page, form);
    expect(submitted).toEqual([{ visitDate: '' }]);
  });

  it('a date before min is refused with its message', async () => {
    const { page, form, submitted } = await mountForm(false);

    await type(page, '15/08/2026');
    await submit(page, form);

    expect(submitted).toEqual([]);
    expect(error(page).textContent).toBe('Choose 01/09/2026 or later');
  });

  it('defaultValue fills the field and form.reset() returns to it', async () => {
    const { page, form } = await mountForm(true, '2026-09-15');
    form.formStructure = { currentStep: 1 } as never;

    expect(input(page).value).toBe('15/09/2026');
    expect(form.getValues<Values>().visitDate).toBe('2026-09-15');

    await type(page, '20/09/2026');
    expect(form.getValues<Values>().visitDate).toBe('2026-09-20');

    form.reset();
    await new Promise(resolve => setTimeout(resolve, 150));
    await page.waitForChanges();

    expect(input(page).value).toBe('15/09/2026');
    expect(form.getValues<Values>().visitDate).toBe('2026-09-15');
  });

  it('the form disables the field while it submits', async () => {
    const { page, form } = await mountForm(true);

    form.signal({ disabled: true });
    form.rerender({ rerenderAll: true });
    await page.waitForChanges();
    expect(input(page).disabled).toBe(true);

    form.signal({ disabled: false });
    form.rerender({ rerenderAll: true });
    await page.waitForChanges();
    expect(input(page).disabled).toBe(false);
  });

  it('unsubscribes when it leaves', async () => {
    const { page, form } = await mountForm(true);
    const subscribers = () => (form as unknown as { subscribers: { name: string }[] }).subscribers.map(sub => sub.name);

    expect(subscribers()).toEqual(['visitDate']);
    page.body.querySelector('shift-input').remove();
    await page.waitForChanges();
    expect(subscribers()).toEqual([]);
  });
});
