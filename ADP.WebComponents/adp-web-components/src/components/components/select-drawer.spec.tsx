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
let branches: { ID: number; Name: string }[] = BRANCHES;
let branchGate: Promise<void> | null = null;

async function fakeFetch(input: unknown, init: { method?: string; body?: string } = {}) {
  const url = String(input);

  const localeAt = url.indexOf('/locales/');
  if (localeAt > -1) return respond(JSON.parse(readFileSync(join(__dirname, '../../', url.slice(localeAt + 1)), 'utf8')));
  if (url.startsWith(SUBMIT) && init.method) {
    sent.push(init.body);
    return respond({ id: 7 });
  }
  if (url.startsWith(`${API}/branches`)) {
    if (branchGate) await branchGate;
    return respond(branches);
  }

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
  branches = BRANCHES;
  branchGate = null;
});

async function settle(page: SpecPage, ms = 1500) {
  for (let elapsed = 0; elapsed < ms; elapsed += 50) {
    await jest.advanceTimersByTimeAsync(50);
    await page.waitForChanges();
  }
}

type Screen = { width: number; coarse: boolean; listeners: (() => void)[]; resize: (width: number) => void };

async function mount(structure: Structure, width: number | null = 390, props: Record<string, unknown> = {}, pointer: 'fine' | 'coarse' = 'fine', components = COMPONENTS) {
  const page = await newSpecPage({ components, html: '' });
  const listeners: (() => void)[] = [];
  const screen: Screen = {
    width: width ?? 1024,
    coarse: pointer === 'coarse',
    listeners,
    resize: (next: number) => {
      screen.width = next;
      listeners.forEach(listener => listener());
    },
  };

  if (width === null) (page.win as unknown as Record<string, unknown>).matchMedia = () => ({ matches: false, addEventListener() {}, removeEventListener() {} });
  else
    (page.win as unknown as Record<string, unknown>).matchMedia = (query: string) => ({
      get matches() {
        if (query === '(pointer: coarse)') return screen.coarse;
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
const formSelectOf = (page: SpecPage, name: string) =>
  Array.from(formRoot(page).querySelectorAll('form-select')).find(el => el.name === name) as HTMLElement & { selectDrawer?: string; fetcherKey?: unknown };
const listOf = (page: SpecPage, name: string) => sheetOf(page, name).shadowRoot.querySelector('[role="listbox"]') as HTMLElement;
const shadowValue = (page: SpecPage, name: string) => (formRoot(page).querySelector(`input.shadow-input[name="${name}"]`) as HTMLInputElement)?.value;

const drawerOpen = (page: SpecPage, name: string) => {
  const box = drawerBox(page, name);
  return !!box && box.hasAttribute('data-open') && box.hasAttribute('data-sheet');
};
const dropdownOpen = (page: SpecPage, name: string) => !!dropdownBox(page, name)?.classList.contains('shift-select-container-open');

const withoutSheet = COMPONENTS.filter(component => component !== ShiftSelectSheet);
const defineSheetLate = (page: SpecPage) => win(page).customElements.define('shift-select-sheet', class extends (win(page).HTMLElement as typeof HTMLElement) {});

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
  it('pins the unset render, closed and with the dropdown open (the 0.4.17 equality is proven by the external harness)', async () => {
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

  it('without shift-popover defined, as on a per-component host that did not load it, always keeps the dropdown', async () => {
    const withoutPopover = COMPONENTS.filter(component => component !== ShiftPopover && component !== ShiftPopoverPanel);
    const { page } = await mount(form({ selectDrawer: 'always' }), 390, {}, 'fine', withoutPopover);
    await open(page, 'year');

    expect(dropdownOpen(page, 'year')).toBe(true);
    expect(sheetOf(page, 'year')).toBeNull();
    expect(triggerOf(page, 'year').hasAttribute('role')).toBe(false);
  });

  it('with shift-popover defined but not shift-select-sheet, always keeps the dropdown; with both defined it opens the drawer', async () => {
    const bare = await mount(form({ selectDrawer: 'always' }), 390, {}, 'fine', withoutSheet);
    expect(win(bare.page).customElements.get('shift-popover')).toBeDefined();
    expect(win(bare.page).customElements.get('shift-select-sheet')).toBeUndefined();
    await open(bare.page, 'year');

    expect(dropdownOpen(bare.page, 'year')).toBe(true);
    expect(selectOf(bare.page, 'year').querySelector('shift-popover') ?? bare.page.body.querySelector('shift-popover')).toBeNull();
    expect(sheetOf(bare.page, 'year')).toBeNull();
    expect(triggerOf(bare.page, 'year').hasAttribute('role')).toBe(false);

    const full = await mount(form({ selectDrawer: 'always' }), 390);
    expect(win(full.page).customElements.get('shift-select-sheet')).toBeDefined();
    await open(full.page, 'year');

    expect(drawerOpen(full.page, 'year')).toBe(true);
    expect(sheetOf(full.page, 'year')).not.toBeNull();
    expect(dropdownOpen(full.page, 'year')).toBe(false);
    expect(triggerOf(full.page, 'year').getAttribute('role')).toBe('combobox');
  });

  it('shift-select-sheet defined after the select rendered: the closed select is untouched and the drawer is used from the next open', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390, {}, 'fine', withoutSheet);
    expect(triggerOf(page, 'year').hasAttribute('role')).toBe(false);
    const closed = selectOf(page, 'year').outerHTML;

    defineSheetLate(page);
    await settle(page, 100);

    expect(selectOf(page, 'year').outerHTML).toBe(closed);
    expect(dropdownBox(page, 'year')).not.toBeNull();
    await open(page, 'year');
    expect(drawerOpen(page, 'year')).toBe(true);
    expect(sheetOf(page, 'year')).not.toBeNull();
    expect(dropdownOpen(page, 'year')).toBe(false);
    expect(triggerOf(page, 'year').getAttribute('role')).toBe('combobox');
    expect(triggerOf(page, 'year').getAttribute('aria-expanded')).toBe('true');
  });

  it('shift-select-sheet defined after the select rendered: Enter on the closed trigger opens the drawer', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390, {}, 'fine', withoutSheet);

    defineSheetLate(page);
    await settle(page, 100);

    key(page, triggerOf(page, 'year'), 'Enter');
    await settle(page, 300);
    expect(drawerOpen(page, 'year')).toBe(true);
    expect(dropdownOpen(page, 'year')).toBe(false);
    expect(triggerOf(page, 'year').getAttribute('role')).toBe('combobox');
  });

  it('shift-select-sheet defined while the dropdown is open: the dropdown stays and closes as a dropdown, the drawer comes on the next open', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390, {}, 'fine', withoutSheet);
    await open(page, 'year');
    expect(dropdownOpen(page, 'year')).toBe(true);

    defineSheetLate(page);
    await settle(page, 100);
    (selectOf(page, 'year') as SelectHost & { placeholder: string }).placeholder = 'Pick a year';
    await settle(page, 100);

    expect(dropdownOpen(page, 'year')).toBe(true);
    expect(drawerOpen(page, 'year')).toBe(false);
    expect(sheetOf(page, 'year')).toBeNull();
    expect(triggerOf(page, 'year').hasAttribute('role')).toBe(false);

    key(page, page.body, 'Escape');
    await page.waitForChanges();
    expect(dropdownBox(page, 'year')).not.toBeNull();
    expect(dropdownOpen(page, 'year')).toBe(false);
    expect(sheetOf(page, 'year')).toBeNull();

    await settle(page, 300);
    expect(dropdownBox(page, 'year')).not.toBeNull();
    expect(triggerOf(page, 'year').hasAttribute('role')).toBe(false);

    await open(page, 'year');
    expect(drawerOpen(page, 'year')).toBe(true);
    expect(dropdownOpen(page, 'year')).toBe(false);
    expect(page.body.querySelector('shift-select-dropdown.year-select')).toBeNull();
  });

  it('shift-select-sheet defined after a pick: the clear icon clears without opening the drawer or the dropdown', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390, {}, 'fine', withoutSheet);
    await open(page, 'companyBranchId');
    const north = Array.from(page.body.querySelector('shift-select-dropdown.companyBranchId-select').shadowRoot.querySelectorAll('button')).find(
      button => button.textContent.trim() === 'North',
    ) as HTMLElement;
    fire(page, north, 'click');
    await settle(page, 300);
    expect(shadowValue(page, 'companyBranchId')).toBe('11');
    expect(dropdownOpen(page, 'companyBranchId')).toBe(false);

    defineSheetLate(page);
    await settle(page, 100);

    fire(page, selectOf(page, 'companyBranchId').querySelector('.form-input-select-icon.cross'), 'click');
    await settle(page, 300);

    expect(shadowValue(page, 'companyBranchId')).toBe('');
    expect(selectOf(page, 'companyBranchId').isOpen).toBe(false);
    expect(drawerOpen(page, 'companyBranchId')).toBe(false);
    expect(dropdownOpen(page, 'companyBranchId')).toBe(false);
  });

  it('opted in with shift-select-sheet missing: Enter, Space and ArrowDown on the trigger are not swallowed and open nothing, as on master', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390, {}, 'fine', withoutSheet);

    for (const name of ['Enter', ' ', 'ArrowDown']) {
      expect(key(page, triggerOf(page, 'year'), name)).toBe(true);
      await settle(page, 300);
      expect(dropdownOpen(page, 'year')).toBe(false);
      expect(drawerOpen(page, 'year')).toBe(false);
      expect(selectOf(page, 'year').isOpen).toBe(false);
    }
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

  it('selectDrawer can change at runtime: on, then off, and the breakpoint listener follows it', async () => {
    const { page, screen } = await mount(form(), 390);
    expect(screen.listeners).toHaveLength(0);

    formSelectOf(page, 'year').selectDrawer = 'mobile';
    await settle(page, 300);
    expect(screen.listeners.length).toBeGreaterThan(0);

    await open(page, 'year');
    expect(drawerOpen(page, 'year')).toBe(true);
    expect(dropdownOpen(page, 'year')).toBe(false);
    escape(page, 'year');
    await settle(page, 300);

    formSelectOf(page, 'year').selectDrawer = 'off';
    await settle(page, 300);
    expect(screen.listeners).toHaveLength(0);
    expect(sheetOf(page, 'year')).toBeNull();
    expect(triggerOf(page, 'year').hasAttribute('role')).toBe(false);

    await open(page, 'year');
    expect(dropdownOpen(page, 'year')).toBe(true);
  });

  // Mock-doc does not disconnect shadow-root descendants when their host is removed, so each select is removed itself.
  it('a select taken off the page removes its breakpoint listeners', async () => {
    const { page, screen } = await mount(form({ selectDrawer: 'mobile' }), 390);
    const perSelect = screen.listeners.length / 2;
    expect(perSelect).toBeGreaterThan(0);

    selectOf(page, 'year').remove();
    await settle(page, 300);
    expect(screen.listeners).toHaveLength(perSelect);

    selectOf(page, 'companyBranchId').remove();
    await settle(page, 300);
    expect(screen.listeners).toHaveLength(0);
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

  it('the clear icon clears without opening the drawer; without the drawer it still opens the dropdown', async () => {
    const clearAndLook = async (drawer: boolean) => {
      const { page } = await mount(form(drawer ? { selectDrawer: 'always' } : {}), drawer ? 390 : 1024);
      await open(page, 'companyBranchId');
      const north = drawer
        ? rows(page, 'companyBranchId')[0]
        : (Array.from(page.body.querySelector('shift-select-dropdown.companyBranchId-select').shadowRoot.querySelectorAll('button')).find(
            button => button.textContent.trim() === 'North',
          ) as HTMLElement);
      fire(page, north, 'click');
      await settle(page, 300);
      expect(shadowValue(page, 'companyBranchId')).toBe('11');

      fire(page, selectOf(page, 'companyBranchId').querySelector('.form-input-select-icon.cross'), 'click');
      await settle(page, 300);

      return { page, value: shadowValue(page, 'companyBranchId'), isOpen: selectOf(page, 'companyBranchId').isOpen };
    };

    const drawer = await clearAndLook(true);
    expect(drawer.value).toBe('');
    expect(drawer.isOpen).toBe(false);
    expect(drawerOpen(drawer.page, 'companyBranchId')).toBe(false);

    const dropdown = await clearAndLook(false);
    expect(dropdown.value).toBe('');
    expect(dropdown.isOpen).toBe(true);
    expect(dropdownOpen(dropdown.page, 'companyBranchId')).toBe(true);
  });

  it('a list that arrives while the drawer is open fades in, a list there at open does not', async () => {
    let release: () => void;
    branchGate = new Promise(resolve => (release = resolve));
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);

    await open(page, 'companyBranchId');
    expect(rows(page, 'companyBranchId')).toHaveLength(0);
    expect(sheetOf(page, 'companyBranchId').shadowRoot.querySelector('[part="spinner"]')).not.toBeNull();

    release();
    await settle(page, 300);
    expect(rows(page, 'companyBranchId')).toHaveLength(3);
    expect(listOf(page, 'companyBranchId').hasAttribute('data-waited')).toBe(true);

    escape(page, 'companyBranchId');
    await settle(page, 300);
    await open(page, 'companyBranchId');
    expect(listOf(page, 'companyBranchId').hasAttribute('data-waited')).toBe(false);

    const css = readFileSync(join(__dirname, 'shift-select-sheet.css'), 'utf8');
    expect(css).toMatch(/\.list\[data-waited\]:not\(:empty\)\s*\{\s*animation: select-sheet-enter var\(--_quick\) var\(--_curve\) both;/);
  });

  it('options swapped to a list without the pick open the drawer on the full new list', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    await open(page, 'companyBranchId');
    fire(page, rows(page, 'companyBranchId')[2], 'click');
    await settle(page, 300);
    expect(triggerOf(page, 'companyBranchId').value).toBe('Northeast');

    branches = [
      { ID: 21, Name: 'Central' },
      { ID: 22, Name: 'West' },
    ];
    formSelectOf(page, 'companyBranchId').fetcherKey = 'swapped';
    await settle(page, 300);
    expect(triggerOf(page, 'companyBranchId').value).toBe('');

    await open(page, 'companyBranchId');
    expect(drawerOpen(page, 'companyBranchId')).toBe(true);
    expect(rows(page, 'companyBranchId').map(row => row.textContent.trim())).toEqual(['Central', 'West']);
  });

  it('text typed into the dropdown does not filter the drawer after the screen narrows', async () => {
    const { page, screen } = await mount(form({ selectDrawer: 'mobile' }), 1024);
    const trigger = triggerOf(page, 'companyBranchId');
    trigger.value = 'sou';
    fire(page, trigger, 'input');
    await settle(page, 300);

    screen.resize(390);
    await settle(page, 300);
    await open(page, 'companyBranchId');

    expect(drawerOpen(page, 'companyBranchId')).toBe(true);
    expect(rows(page, 'companyBranchId').map(row => row.textContent.trim())).toEqual(['North', 'South', 'Northeast']);
  });

  it('the trigger is read-only in drawer mode, so tapping it never brings up a keyboard on the page', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);

    expect(triggerOf(page, 'companyBranchId').readOnly).toBe(true);
    expect(triggerOf(page, 'year').readOnly).toBe(true);
  });
});

describe('select drawer: search', () => {
  it('a searchable select gets a search field that takes focus on a fine pointer and filters the drawer’s own list', async () => {
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

  it('on a coarse pointer the search field is there but focus goes to the selected row, or the first, so no keyboard rises', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390, {}, 'coarse');
    const focused = focusLog(page, 'companyBranchId');
    await open(page, 'companyBranchId');

    expect(searchOf(page, 'companyBranchId')).not.toBeNull();
    expect(focused).not.toContain(searchOf(page, 'companyBranchId'));
    expect(focused[focused.length - 1]).toBe(rows(page, 'companyBranchId')[0]);

    fire(page, rows(page, 'companyBranchId')[1], 'click');
    await settle(page, 300);
    await open(page, 'companyBranchId');

    expect(focused).not.toContain(searchOf(page, 'companyBranchId'));
    expect(focused[focused.length - 1]).toBe(rows(page, 'companyBranchId')[1]);
    expect(rows(page, 'companyBranchId')[1].getAttribute('aria-selected')).toBe('true');
  });

  it('Enter in the search picks the only match, and with more than one moves focus to the first row', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    const focused = focusLog(page, 'companyBranchId');
    await open(page, 'companyBranchId');
    const search = searchOf(page, 'companyBranchId');

    search.value = 'north';
    fire(page, search, 'input');
    await settle(page, 50);
    key(page, search, 'Enter');
    expect(focused[focused.length - 1]).toBe(rows(page, 'companyBranchId')[0]);
    expect(drawerOpen(page, 'companyBranchId')).toBe(true);

    search.value = 'sou';
    fire(page, search, 'input');
    await settle(page, 50);
    key(page, search, 'Enter');
    await settle(page, 300);

    expect(drawerOpen(page, 'companyBranchId')).toBe(false);
    expect(triggerOf(page, 'companyBranchId').value).toBe('South');
    expect(shadowValue(page, 'companyBranchId')).toBe('12');
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

  it('the search label follows the form language, and drawerSearchLabel still overrides it', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390, { language: 'ru' });
    await open(page, 'companyBranchId');

    expect(searchOf(page, 'companyBranchId').getAttribute('placeholder')).toBe('Поиск');
    expect(searchOf(page, 'companyBranchId').getAttribute('aria-label')).toBe('Поиск');

    (selectOf(page, 'companyBranchId') as SelectHost & { drawerSearchLabel: string }).drawerSearchLabel = 'Find a branch';
    await settle(page, 100);
    expect(searchOf(page, 'companyBranchId').getAttribute('placeholder')).toBe('Find a branch');
    expect(searchOf(page, 'companyBranchId').getAttribute('aria-label')).toBe('Find a branch');
  });

  it('a select that is not searchable gets no search field', async () => {
    const { page } = await mount(form({ selectDrawer: 'always' }), 390);
    await open(page, 'year');

    expect(searchOf(page, 'year')).toBeNull();
  });
});
