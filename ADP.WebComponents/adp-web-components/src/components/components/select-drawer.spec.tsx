// validation.ts declares a top-level `require`, which CommonJS (and so Jest) rejects; this loads the real file with that one name changed.
jest.mock('../forms/defaults/validation', () => {
  const { readFileSync } = require('fs');
  const { join } = require('path');
  const ts = require('typescript');

  const source = readFileSync(join(__dirname, '../forms/defaults/validation.ts'), 'utf8')
    .replace(/(?<![.\w-])require\b(?=\s*[(=,])/g, 'requireKey')
    .replace(/(export const y = \{[^}]*?)\brequireKey\b/, '$1require: requireKey');
  const { outputText } = ts.transpileModule(`${source}\nexport { requireKey as require };`, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2021 },
  });
  const aliases = { '~lib/': '../../global/lib/', '~features/': '../../features/' };
  const load = (id: string) => require(Object.entries(aliases).reduce((path, [alias, target]) => (path.startsWith(alias) ? target + path.slice(alias.length) : path), id));
  const module = { exports: {} };
  new Function('exports', 'require', 'module', outputText)(module.exports, load, module);
  return module.exports;
});

import { newSpecPage, SpecPage } from '@stencil/core/testing';
import { serializeNodeToHtml } from '@stencil/core/mock-doc';
import { readFileSync } from 'fs';
import { join } from 'path';

import { GeneralForm } from '../forms/general-form';
import { FormStructure } from '../form-elements/form-structure';
import { FormStructureError } from '../form-elements/form-structure-error';
import { FormInput as FormDialog } from '../form-elements/form-dialog';
import { FormSubmit } from '../form-elements/form-submit';
import { FormInput } from '../form-elements/form-input';
import { FormSelect } from '../form-elements/form-select';
import { FormInput as FormShadowInput } from '../form-elements/form-shadow-input';
import { ShiftSelect } from './shift-select';
import { ShiftSelectDropdown } from './shift-select-dropdown';
import { ShiftSelectSheet } from './shift-select-sheet';
import { ShiftPortal } from './shift-portal';
import { ShiftPopover } from './shift-popover/shift-popover';
import { ShiftPopoverPanel } from './shift-popover/shift-popover-panel';
import { FlexibleContainer } from './flexible-container';
import { resolveSelectDrawer } from './select-drawer';

const COMPONENTS = [
  GeneralForm,
  FormStructure,
  FormStructureError,
  FormDialog,
  FormSubmit,
  FormInput,
  FormSelect,
  FormShadowInput,
  ShiftSelect,
  ShiftSelectDropdown,
  ShiftSelectSheet,
  ShiftPortal,
  ShiftPopover,
  ShiftPopoverPanel,
  FlexibleContainer,
];

const API = 'https://example.invalid/api';
const SUBMIT = 'https://example.invalid/submit';
const BRANCHES = [
  { ID: 11, Name: 'North' },
  { ID: 12, Name: 'South' },
  { ID: 13, Name: 'Northeast' },
];

type Structure = Record<string, unknown>;
type FormHost = HTMLElement & {
  submit: () => Promise<void>;
  getForm: () => Promise<{
    addWatcher: (key: string, callback: (event: { values: Record<string, unknown> }) => void) => void;
    getValues: () => Record<string, unknown>;
    getFormErrors: () => Record<string, string>;
  }>;
};
type SelectHost = HTMLElement & { name: string; isOpen: boolean };

const form = (data: Record<string, unknown> = {}, year: Record<string, unknown> = {}, branch: Record<string, unknown> = {}): Structure => ({
  data: { requestUrl: SUBMIT, ...data },
  requiredContext: { year: true },
  tag: 'div',
  id: 'container',
  children: [{ name: 'year', min: 2001, max: 2024, ...year }, { name: 'companyBranchId', branchApi: `${API}/branches`, ...branch }, { name: 'submit' }],
});

const respond = (body: unknown, status = 200) => ({
  ok: status < 400,
  status,
  headers: { get: () => 'application/json' },
  json: async () => JSON.parse(JSON.stringify(body)),
  text: async () => JSON.stringify(body),
});

const sent: string[] = [];

async function fakeFetch(input: unknown, init: { method?: string; body?: string } = {}) {
  const url = String(input);

  const localeAt = url.indexOf('/locales/');
  if (localeAt > -1) return respond(JSON.parse(readFileSync(join(__dirname, '../../', url.slice(localeAt + 1)), 'utf8')));
  if (url.startsWith(SUBMIT) && init.method) {
    sent.push(init.body);
    return respond({ id: 7 });
  }
  if (url.startsWith(`${API}/branches`)) return respond(BRANCHES);

  throw new Error(`unexpected request ${url}`);
}

class NoopObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
  takeRecords() {
    return [];
  }
}

class FormDataFromForm {
  private list: [string, unknown][] = [];

  constructor(form?: Element) {
    form?.querySelectorAll('input[name], select[name], textarea[name]').forEach((el: Element) => {
      const control = el as HTMLInputElement;
      if (control.disabled || ['submit', 'button', 'reset', 'image'].includes(control.type)) return;
      this.list.push([control.getAttribute('name'), control.value ?? '']);
    });
  }

  entries() {
    return this.list[Symbol.iterator]();
  }
}

const restoreGlobals: (() => void)[] = [];
const setGlobal = (name: string, value: unknown) => {
  const had = Object.prototype.hasOwnProperty.call(globalThis, name);
  const previous = (globalThis as Record<string, unknown>)[name];
  (globalThis as Record<string, unknown>)[name] = value;
  restoreGlobals.push(() => (had ? ((globalThis as Record<string, unknown>)[name] = previous) : delete (globalThis as Record<string, unknown>)[name]));
};

beforeAll(() => {
  jest.useFakeTimers({ now: new Date('2026-03-16T08:00:00.000Z'), doNotFake: ['nextTick', 'queueMicrotask', 'setImmediate'] });
  for (const method of ['log', 'warn', 'error'] as const) jest.spyOn(console, method).mockImplementation(() => undefined);
  setGlobal('fetch', jest.fn(fakeFetch));
  setGlobal('FormData', FormDataFromForm);
  setGlobal('MutationObserver', NoopObserver);
  setGlobal('ResizeObserver', NoopObserver);
  setGlobal('IntersectionObserver', NoopObserver);
  setGlobal('requestAnimationFrame', (callback: () => void) => setTimeout(callback, 16));
});

afterAll(() => {
  restoreGlobals.reverse().forEach(restore => restore());
  jest.restoreAllMocks();
  jest.useRealTimers();
});

beforeEach(() => {
  sent.length = 0;
});

async function settle(page: SpecPage, ms = 1500) {
  for (let elapsed = 0; elapsed < ms; elapsed += 50) {
    await jest.advanceTimersByTimeAsync(50);
    await page.waitForChanges();
  }
}

type Screen = { width: number; resize: (width: number) => void };

async function mount(structure: Structure, width: number | null = 390, props: Record<string, unknown> = {}) {
  const page = await newSpecPage({ components: COMPONENTS, html: '' });
  const listeners: (() => void)[] = [];
  const screen: Screen = {
    width: width ?? 1024,
    resize: (next: number) => {
      screen.width = next;
      listeners.forEach(listener => listener());
    },
  };

  if (width === null) (page.win as unknown as Record<string, unknown>).matchMedia = () => ({ matches: false, addEventListener() {}, removeEventListener() {} });
  else
    (page.win as unknown as Record<string, unknown>).matchMedia = (query: string) => ({
      get matches() {
        return query === '(max-width: 599px)' && screen.width < 600;
      },
      addEventListener: (_: string, listener: () => void) => listeners.push(listener),
      removeEventListener: (_: string, listener: () => void) => listeners.splice(listeners.indexOf(listener) >>> 0, 1),
    });

  const el = page.doc.createElement('general-form') as unknown as FormHost;
  Object.assign(el, { language: 'en', structure: JSON.parse(JSON.stringify(structure)), ...props });
  page.body.appendChild(el);
  await settle(page);

  return { page, el, screen };
}

const win = (page: SpecPage) => page.win as unknown as typeof globalThis;
const fire = (page: SpecPage, target: Element, type: string, init: Record<string, unknown> = {}) =>
  target.dispatchEvent(new (win(page).Event)(type, { bubbles: true, composed: true, cancelable: true, ...init }));
const key = (page: SpecPage, target: Element, name: string, init: Record<string, unknown> = {}) =>
  target.dispatchEvent(new (win(page).KeyboardEvent)('keydown', { key: name, bubbles: true, composed: true, cancelable: true, ...init }));

// Mock-doc does not carry a composed event out of a shadow root, so Escape starts at the sheet element inside the panel.
const escape = (page: SpecPage, name: string) => key(page, sheetOf(page, name), 'Escape');
const formRoot = (page: SpecPage) => page.body.querySelector('general-form').shadowRoot;
const selectOf = (page: SpecPage, name: string) => Array.from(formRoot(page).querySelectorAll('shift-select')).find(el => (el as SelectHost).name === name) as SelectHost;
const triggerOf = (page: SpecPage, name: string) => selectOf(page, name).querySelector('input.form-input-select') as HTMLInputElement;
const sheetOf = (page: SpecPage, name: string) => page.body.querySelector(`shift-select-sheet.${name}-select`) as HTMLElement | null;
const drawerBox = (page: SpecPage, name: string) => (sheetOf(page, name)?.parentElement?.shadowRoot?.querySelector('.pop-panel') as HTMLElement) ?? null;
const dropdownBox = (page: SpecPage, name: string) => page.body.querySelector(`shift-select-dropdown.${name}-select`)?.shadowRoot?.querySelector('.shift-select-container') ?? null;
const rows = (page: SpecPage, name: string) => Array.from(sheetOf(page, name).shadowRoot.querySelectorAll('[role="option"]')) as HTMLElement[];
const searchOf = (page: SpecPage, name: string) => sheetOf(page, name).shadowRoot.querySelector('input.search') as HTMLInputElement | null;
const shadowValue = (page: SpecPage, name: string) => (formRoot(page).querySelector(`input.shadow-input[name="${name}"]`) as HTMLInputElement)?.value;

const drawerOpen = (page: SpecPage, name: string) => {
  const box = drawerBox(page, name);
  return !!box && box.hasAttribute('data-open') && box.hasAttribute('data-sheet');
};
const dropdownOpen = (page: SpecPage, name: string) => !!dropdownBox(page, name)?.classList.contains('shift-select-container-open');

async function open(page: SpecPage, name: string) {
  fire(page, triggerOf(page, name), 'click');
  await settle(page, 300);
}

function focusLog(page: SpecPage, name: string) {
  const seen: Element[] = [];
  sheetOf(page, name).shadowRoot.addEventListener('focus', (event: Event) => seen.push(event.target as Element), true);
  return seen;
}

describe('select drawer: resolving the setting', () => {
  it('the field wins, off keeps the dropdown, anything else counts as unset', () => {
    expect(resolveSelectDrawer(undefined, undefined)).toBeUndefined();
    expect(resolveSelectDrawer(undefined, 'mobile')).toBe('mobile');
    expect(resolveSelectDrawer('always', 'mobile')).toBe('always');
    expect(resolveSelectDrawer('mobile', 'always')).toBe('mobile');
    expect(resolveSelectDrawer('off', 'always')).toBeUndefined();
    expect(resolveSelectDrawer('sideways', 'always')).toBe('always');
    expect(resolveSelectDrawer(true, undefined)).toBeUndefined();
    expect(resolveSelectDrawer(undefined, 'off')).toBeUndefined();
  });
});

describe('select drawer: unset', () => {
  it('renders and opens the dropdown with the same DOM as before the drawer existed', async () => {
    const { page } = await mount(form(), 390);
    const closed = serializeNodeToHtml(page.body as never, { prettyHtml: true, serializeShadowRoot: true });

    await open(page, 'year');
    const opened = serializeNodeToHtml(page.body as never, { prettyHtml: true, serializeShadowRoot: true });

    expect(closed).toMatchSnapshot('closed');
    expect(opened).toMatchSnapshot('dropdown open');
    expect(page.body.querySelector('shift-popover, shift-popover-panel, shift-select-sheet')).toBeNull();
    expect(dropdownOpen(page, 'year')).toBe(true);
  });

  it('a phone-width screen still gets the dropdown', async () => {
    const { page } = await mount(form(), 390);
    await open(page, 'year');

    expect(dropdownOpen(page, 'year')).toBe(true);
    expect(sheetOf(page, 'year')).toBeNull();
    expect(triggerOf(page, 'year').hasAttribute('role')).toBe(false);
    expect(triggerOf(page, 'year').hasAttribute('aria-expanded')).toBe(false);
  });
});

describe('select drawer: opting in', () => {
  it('form mobile at a phone width: a tap opens the bottom sheet and no dropdown', async () => {
    const { page } = await mount(form({ selectDrawer: 'mobile' }), 390);
    await open(page, 'year');

    expect(drawerOpen(page, 'year')).toBe(true);
    expect(drawerBox(page, 'year').getAttribute('part')).toBe('panel sheet');
    expect(dropdownOpen(page, 'year')).toBe(false);
    expect(page.doc.documentElement.style.overflow).toBe('hidden');
    expect(triggerOf(page, 'year').getAttribute('aria-expanded')).toBe('true');
    expect(triggerOf(page, 'year').getAttribute('role')).toBe('combobox');
    expect(selectOf(page, 'year').querySelector('.form-input-container').classList.contains('open')).toBe(true);
  });

  it('form mobile above the breakpoint: the dropdown opens as today', async () => {
    const { page } = await mount(form({ selectDrawer: 'mobile' }), 1024);
    await open(page, 'year');

    expect(dropdownOpen(page, 'year')).toBe(true);
    expect(drawerOpen(page, 'year')).toBe(false);
  });

  it('form always: the drawer opens at any width', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 1024);
    await open(page, 'year');

    expect(drawerOpen(page, 'year')).toBe(true);
    expect(page.body.querySelector('shift-select-dropdown.year-select')).toBeNull();
  });

  it('a field setting wins over the form: off keeps the dropdown, a field value opts in alone', async () => {
    const { page } = await mount(form({ selectDrawer: 'mobile' }, { selectDrawer: 'off' }), 390);
    await open(page, 'year');

    expect(dropdownOpen(page, 'year')).toBe(true);
    expect(sheetOf(page, 'year')).toBeNull();
    expect(triggerOf(page, 'year').hasAttribute('role')).toBe(false);

    await open(page, 'companyBranchId');
    expect(drawerOpen(page, 'companyBranchId')).toBe(true);
  });

  it('a field value through the fields prop opts in that select only', async () => {
    const { page } = await mount(form(), 1024, { fields: { year: { selectDrawer: 'always' } } });

    await open(page, 'year');
    expect(drawerOpen(page, 'year')).toBe(true);

    await open(page, 'companyBranchId');
    expect(sheetOf(page, 'companyBranchId')).toBeNull();
    expect(dropdownOpen(page, 'companyBranchId')).toBe(true);
  });

  it('crossing the breakpoint while open closes the drawer', async () => {
    const { page, screen } = await mount(form({ selectDrawer: 'mobile' }), 390);
    await open(page, 'year');
    expect(drawerOpen(page, 'year')).toBe(true);

    screen.resize(1024);
    await settle(page, 300);

    expect(drawerOpen(page, 'year')).toBe(false);
    expect(dropdownOpen(page, 'year')).toBe(false);
    expect(selectOf(page, 'year').isOpen).toBe(false);
  });
});

describe('select drawer: inside the drawer', () => {
  it('lists every option in order with the selected one marked, and the label names the list', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    await open(page, 'year');

    const options = rows(page, 'year');
    expect(options.map(row => row.textContent.trim())).toEqual(Array.from({ length: 24 }, (_, index) => `${2001 + index}`));
    expect(options.every(row => row.getAttribute('aria-selected') === 'false')).toBe(true);
    expect(sheetOf(page, 'year').shadowRoot.querySelector('[role="listbox"]').getAttribute('part')).toBe('list');
    expect(searchOf(page, 'year')).toBeNull();

    fire(page, options[9], 'click');
    await settle(page, 300);
    await open(page, 'year');

    const marked = rows(page, 'year').filter(row => row.getAttribute('aria-selected') === 'true');
    expect(marked.map(row => row.textContent.trim())).toEqual(['2010']);
    expect(marked[0].getAttribute('part')).toBe('option option-selected');
    expect(marked[0].querySelector('[part="option-tick"]')).not.toBeNull();
  });

  it('a pick sets the value, validates and runs the watchers exactly as a dropdown click does, and submits the same body', async () => {
    const outcome = async (drawer: boolean) => {
      const { page, el } = await mount(form(drawer ? { selectDrawer: 'always' } : {}), 1024);
      const hook = await el.getForm();
      const watched: unknown[] = [];
      hook.addWatcher('year', ({ values }) => watched.push(values.year));

      await el.submit();
      await settle(page, 300);
      const errorBefore = hook.getFormErrors().year;

      await open(page, 'year');
      const option = drawer
        ? rows(page, 'year').find(row => row.textContent.trim() === '2010')
        : (Array.from(page.body.querySelector('shift-select-dropdown.year-select').shadowRoot.querySelectorAll('button')).find(
            button => button.textContent.trim() === '2010',
          ) as HTMLElement);
      fire(page, option, 'click');
      await settle(page, 500);

      const state = {
        errorBefore: !!errorBefore,
        value: hook.getValues().year,
        shadow: shadowValue(page, 'year'),
        trigger: triggerOf(page, 'year').value,
        errorAfter: hook.getFormErrors().year ?? '',
        isError: selectOf(page, 'year').querySelector('input.form-input-select').classList.contains('form-input-error-style'),
        watched: [...watched],
        open: selectOf(page, 'year').isOpen,
      };

      sent.length = 0;
      await el.submit();
      await settle(page, 500);

      return { ...state, body: sent[0] };
    };

    const dropdown = await outcome(false);
    const drawer = await outcome(true);

    expect(dropdown.value).toBe('2010');
    expect(dropdown.errorBefore).toBe(true);
    expect(dropdown.watched).toContain('2010');
    expect(dropdown.body).toContain('2010');
    expect(drawer).toEqual(dropdown);
  });

  it('Escape closes without changing the value and focus returns to the trigger', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    await open(page, 'year');
    fire(page, rows(page, 'year')[3], 'click');
    await settle(page, 300);

    await open(page, 'year');
    const triggerFocus = jest.fn();
    triggerOf(page, 'year').addEventListener('focus', triggerFocus);

    escape(page, 'year');
    await settle(page, 300);

    expect(drawerOpen(page, 'year')).toBe(false);
    expect(selectOf(page, 'year').isOpen).toBe(false);
    expect(triggerOf(page, 'year').value).toBe('2004');
    expect(triggerFocus).toHaveBeenCalled();
    expect(page.doc.documentElement.style.overflow).toBe('');
  });

  it('a backdrop tap closes without changing the value', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    await open(page, 'year');

    fire(page, sheetOf(page, 'year').parentElement.shadowRoot.querySelector('.pop-backdrop'), 'click');
    await settle(page, 300);

    expect(drawerOpen(page, 'year')).toBe(false);
    expect(selectOf(page, 'year').isOpen).toBe(false);
    expect(triggerOf(page, 'year').value).toBe('');
  });

  it('keyboard: Enter, Space or ArrowDown on the trigger open it, focus lands on the selected option, arrows, Home and End move, Enter picks', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    const trigger = triggerOf(page, 'year');

    for (const name of ['Enter', ' ', 'ArrowDown']) {
      key(page, trigger, name);
      await settle(page, 300);
      expect(drawerOpen(page, 'year')).toBe(true);
      escape(page, 'year');
      await settle(page, 300);
      expect(drawerOpen(page, 'year')).toBe(false);
    }

    const focused = focusLog(page, 'year');
    key(page, trigger, 'Enter');
    await settle(page, 300);
    expect(focused[focused.length - 1]).toBe(rows(page, 'year')[0]);

    key(page, rows(page, 'year')[0], 'ArrowDown');
    key(page, rows(page, 'year')[1], 'ArrowDown');
    expect(focused[focused.length - 1]).toBe(rows(page, 'year')[2]);
    key(page, rows(page, 'year')[2], 'ArrowUp');
    expect(focused[focused.length - 1]).toBe(rows(page, 'year')[1]);
    key(page, rows(page, 'year')[1], 'End');
    expect(focused[focused.length - 1]).toBe(rows(page, 'year')[23]);
    key(page, rows(page, 'year')[23], 'Home');
    expect(focused[focused.length - 1]).toBe(rows(page, 'year')[0]);
    key(page, rows(page, 'year')[0], 'ArrowDown');
    await settle(page, 50);
    expect(rows(page, 'year')[1].getAttribute('tabindex')).toBe('0');

    key(page, rows(page, 'year')[1], 'Enter');
    await settle(page, 300);
    expect(drawerOpen(page, 'year')).toBe(false);
    expect(trigger.value).toBe('2002');

    key(page, trigger, 'ArrowDown');
    await settle(page, 300);
    expect(focused[focused.length - 1]).toBe(rows(page, 'year')[1]);
    expect(rows(page, 'year')[1].getAttribute('aria-selected')).toBe('true');
  });

  it('the trigger is read-only in drawer mode, so tapping it never brings up a keyboard on the page', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);

    expect(triggerOf(page, 'companyBranchId').readOnly).toBe(true);
    expect(triggerOf(page, 'year').readOnly).toBe(true);
  });
});

describe('select drawer: search', () => {
  it('a searchable select gets a search field that takes focus and filters the drawer’s own list', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    const focused = focusLog(page, 'companyBranchId');
    await open(page, 'companyBranchId');

    const search = searchOf(page, 'companyBranchId');
    expect(search).not.toBeNull();
    expect(search.getAttribute('part')).toBe('search');
    expect(search.getAttribute('placeholder')).toBe('Search');
    expect(focused[focused.length - 1]).toBe(search);
    expect(rows(page, 'companyBranchId').map(row => row.textContent.trim())).toEqual(['North', 'South', 'Northeast']);

    search.value = 'NORTH';
    fire(page, search, 'input');
    await settle(page, 50);
    expect(rows(page, 'companyBranchId').map(row => row.textContent.trim())).toEqual(['North', 'Northeast']);

    search.value = 'west';
    fire(page, search, 'input');
    await settle(page, 50);
    expect(rows(page, 'companyBranchId')).toHaveLength(0);
    expect(sheetOf(page, 'companyBranchId').shadowRoot.querySelector('[part="empty"]').textContent).toBe('There is no options.');

    search.value = 'east';
    fire(page, search, 'input');
    await settle(page, 50);
    key(page, search, 'ArrowDown');
    expect(focused[focused.length - 1]).toBe(rows(page, 'companyBranchId')[0]);
    key(page, rows(page, 'companyBranchId')[0], 'ArrowUp');
    expect(focused[focused.length - 1]).toBe(search);

    key(page, search, 'ArrowDown');
    key(page, rows(page, 'companyBranchId')[0], 'Enter');
    await settle(page, 300);

    expect(drawerOpen(page, 'companyBranchId')).toBe(false);
    expect(triggerOf(page, 'companyBranchId').value).toBe('Northeast');
    expect(shadowValue(page, 'companyBranchId')).toBe('13');
    expect(selectOf(page, 'companyBranchId').getAttribute('search-value')).toBeNull();
  });

  it('the search is empty again on the next open', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    await open(page, 'companyBranchId');

    const search = searchOf(page, 'companyBranchId');
    search.value = 'south';
    fire(page, search, 'input');
    await settle(page, 50);
    escape(page, 'companyBranchId');
    await settle(page, 300);

    await open(page, 'companyBranchId');
    expect(searchOf(page, 'companyBranchId').value).toBe('');
    expect(rows(page, 'companyBranchId')).toHaveLength(3);
  });

  it('a select that is not searchable gets no search field', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    await open(page, 'year');

    expect(searchOf(page, 'year')).toBeNull();
  });
});
