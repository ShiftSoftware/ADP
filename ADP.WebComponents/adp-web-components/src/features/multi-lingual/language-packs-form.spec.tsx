// validation.ts declares a top-level `require`, which CommonJS (and so Jest) rejects; this loads the real file with that one name changed.
jest.mock('../../components/forms/defaults/validation', () => {
  const { readFileSync } = require('fs');
  const { join } = require('path');
  const ts = require('typescript');

  const source = readFileSync(join(__dirname, '../../components/forms/defaults/validation.ts'), 'utf8')
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

import { h } from '@stencil/core';
import { newSpecPage, SpecPage } from '@stencil/core/testing';
import { readFileSync } from 'fs';
import { join } from 'path';
import ts from 'typescript';

import { GeneralForm } from '../../components/forms/general-form';
import { TestDriveForm } from '../../components/forms/test-drive';
import { FormStructure } from '../../components/form-elements/form-structure';
import { FormStructureError } from '../../components/form-elements/form-structure-error';
import { FormInput as FormDialog } from '../../components/form-elements/form-dialog';
import { FormSubmit } from '../../components/form-elements/form-submit';
import { ShiftPortal } from '../../components/components/shift-portal';
import { FlexibleContainer } from '../../components/components/flexible-container';
import { ShiftCalendar } from '../../components/form-elements/shift-calendar/shift-calendar';
import { HOST_CONFIG_KEY } from '~features/host-bridge/host-config';
import formsEn from '../../locales/forms/en.json';
import type { LanguagePacksInput } from './language-packs';

const COMPONENTS = [GeneralForm, TestDriveForm, FormStructure, FormStructureError, FormDialog, FormSubmit, ShiftPortal, FlexibleContainer, ShiftCalendar];

// host-loader.js is shipped as plain ESM, which Jest does not transform.
function loadHostLoader(): { configureAdpHost: (options: Record<string, unknown>) => void } {
  const source = readFileSync(join(__dirname, '../../integration/host-loader.js'), 'utf8');
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2021 } });
  const module = { exports: {} };
  new Function('exports', 'module', outputText)(module.exports, module);

  return module.exports as { configureAdpHost: (options: Record<string, unknown>) => void };
}

const { configureAdpHost } = loadHostLoader();
const register = (languagePacks: LanguagePacksInput) => configureAdpHost({ languagePacks });

const structure = (data: Record<string, unknown> = {}) => ({
  data: { requestUrl: 'https://example.invalid/api/tickets', ...data },
  tag: 'div',
  id: 'container',
  children: [{ name: 'submit' }],
});

const requested: string[] = [];

async function fakeFetch(input: unknown) {
  const url = String(input);
  requested.push(url);
  const at = url.indexOf('/locales/');
  if (at < 0) throw new Error(`unexpected request ${url}`);
  const body = JSON.parse(readFileSync(join(__dirname, '../../', url.slice(at + 1)), 'utf8'));

  return { ok: true, status: 200, json: async () => body };
}

class NoopObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
  takeRecords() {
    return [];
  }
}

const globals = { fetch: jest.fn(fakeFetch), MutationObserver: NoopObserver, ResizeObserver: NoopObserver, IntersectionObserver: NoopObserver };
const previous: Record<string, unknown> = {};

beforeAll(() => {
  jest.useFakeTimers({ now: new Date('2026-03-16T08:00:00.000Z'), doNotFake: ['nextTick', 'queueMicrotask', 'setImmediate'] });
  for (const method of ['log', 'warn', 'error'] as const) jest.spyOn(console, method).mockImplementation(() => undefined);
  for (const [name, value] of Object.entries(globals)) {
    previous[name] = (globalThis as Record<string, unknown>)[name];
    (globalThis as Record<string, unknown>)[name] = value;
  }
});

afterAll(() => {
  for (const [name, value] of Object.entries(previous)) (globalThis as Record<string, unknown>)[name] = value;
  jest.restoreAllMocks();
  jest.useRealTimers();
});

afterEach(() => {
  delete (globalThis as Record<symbol, unknown>)[HOST_CONFIG_KEY];
});

async function mount(tag: string, props: Record<string, unknown>) {
  const page = await newSpecPage({ components: COMPONENTS, template: () => h(tag, { language: 'en', ...props }) });

  if (!page.win.matchMedia) (page.win as unknown as Record<string, unknown>).matchMedia = () => ({ matches: false, addEventListener() {}, removeEventListener() {} });

  for (let elapsed = 0; elapsed < 2000; elapsed += 50) {
    await jest.advanceTimersByTimeAsync(50);
    await page.waitForChanges();
  }

  return page;
}

const submitText = (page: SpecPage) => page.root.shadowRoot.querySelector('form-submit .form-submit-text-position')?.textContent?.trim();
const formLocale = (page: SpecPage) => (page.rootInstance as GeneralForm).locale.sharedFormLocales as Record<string, unknown>;
const calendarRoot = (page: SpecPage) => page.root.shadowRoot.querySelector('.cal-root');
const calendarHeading = (page: SpecPage) => page.root.shadowRoot.querySelector('.cal-header .cal-sr[aria-live]').textContent;

const months = ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'Iyun', 'Iyul', 'Avgust', 'Sentabr', 'Oktabr', 'Noyabr', 'Dekabr'];

describe('language packs on v1 forms', () => {
  it('without a pack the built-in string renders', async () => {
    const page = await mount('general-form', { structure: structure() });

    expect(submitText(page)).toBe('Submit');
    expect((globalThis as Record<symbol, unknown>)[HOST_CONFIG_KEY]).toBeUndefined();
  });

  it('language uz without a pack behaves as before: en strings, ltr', async () => {
    const page = await mount('general-form', { language: 'uz', structure: structure() });

    expect(submitText(page)).toBe('Submit');
    expect(formLocale(page).noSelectOptions).toBe(formsEn.noSelectOptions);
    expect(page.root.shadowRoot.querySelector('form').getAttribute('dir')).toBe('ltr');
    expect(page.root.getAttribute('language')).toBe('uz');
  });

  it('a registered pack overrides a built-in en string', async () => {
    register({ strings: { forms: { en: { submit: 'Send request', noSelectOptions: 'Nothing to pick.' } } } });
    const page = await mount('test-drive-form', { structure: structure() });

    expect(submitText(page)).toBe('Send request');
    expect(formLocale(page).noSelectOptions).toBe('Nothing to pick.');
  });

  it('a pack language renders its own strings through the language prop, missing keys fall back to en', async () => {
    register(JSON.stringify({ languages: { uz: {} }, strings: { forms: { uz: { submit: 'Yuborish' } } } }));
    const page = await mount('general-form', { language: 'uz', structure: structure() });

    expect(submitText(page)).toBe('Yuborish');
    expect(formLocale(page).close).toBe(formsEn.close);
    expect(page.root.shadowRoot.querySelector('form').getAttribute('dir')).toBe('ltr');
  });

  it('an empty pack language joins the list and renders en', async () => {
    register({ languages: { tk: {} } });
    const page = await mount('general-form', { language: 'tk', structure: structure() });

    expect(submitText(page)).toBe('Submit');
    expect(formLocale(page).lang).toBe('tk');
  });
});

describe('language packs on a new-design component', () => {
  // The calendar's header fit needs real layout; the spec DOM has none, as in the calendar's own spec.
  beforeEach(() => ((globalThis as Record<string, unknown>).ResizeObserver = undefined));
  afterEach(() => ((globalThis as Record<string, unknown>).ResizeObserver = NoopObserver));

  it('shift-calendar with language uz and no pack is the en calendar', async () => {
    const page = await mount('shift-calendar', { language: 'uz', today: '2026-09-27' });

    expect(calendarHeading(page)).toBe('September 2026');
    expect(calendarRoot(page).getAttribute('lang')).toBe('en');
    expect(calendarRoot(page).getAttribute('dir')).toBe('ltr');
  });

  it('shift-calendar renders a pack language, missing keys fall back to en', async () => {
    register({ languages: { uz: {} }, strings: { calendar: { uz: { months, calendar: 'Taqvim' } } } });
    const page = await mount('shift-calendar', { language: 'uz', today: '2026-09-27' });

    expect(calendarHeading(page)).toBe('Sentabr 2026');
    expect(calendarRoot(page).getAttribute('aria-label')).toBe('Taqvim');
    expect(calendarRoot(page).getAttribute('lang')).toBe('uz');
    expect(page.root.shadowRoot.querySelector('.cal-weekday .cal-sr').textContent).toBe('Monday');
  });
});
