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

import { GeneralForm } from '../../components/forms/general-form';
import { TestDriveForm } from '../../components/forms/test-drive';
import { FormStructure } from '../../components/form-elements/form-structure';
import { FormStructureError } from '../../components/form-elements/form-structure-error';
import { FormInput as FormDialog } from '../../components/form-elements/form-dialog';
import { FormSubmit } from '../../components/form-elements/form-submit';
import { ShiftPortal } from '../../components/components/shift-portal';
import { FlexibleContainer } from '../../components/components/flexible-container';
import { HOST_CONFIG_KEY } from '~features/host-bridge/host-config';

const COMPONENTS = [GeneralForm, TestDriveForm, FormStructure, FormStructureError, FormDialog, FormSubmit, ShiftPortal, FlexibleContainer];

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

describe('language packs on v1 forms', () => {
  it('without a pack the built-in string renders', async () => {
    const page = await mount('general-form', { structure: structure() });

    expect(submitText(page)).toBe('Submit');
    expect((globalThis as Record<symbol, unknown>)[HOST_CONFIG_KEY]).toBeUndefined();
  });

  it('the languagePacks prop overrides a built-in string', async () => {
    const page = await mount('general-form', { structure: structure(), languagePacks: { strings: { forms: { en: { submit: 'Send request' } } } } });

    expect(submitText(page)).toBe('Send request');
  });

  it('structure.data.languagePacks overrides a built-in string, above the element pack', async () => {
    const page = await mount('test-drive-form', {
      structure: structure({ languagePacks: { strings: { forms: { en: { submit: 'From the structure' } } } } }),
      languagePacks: JSON.stringify({ strings: { forms: { en: { submit: 'From the element' } } } }),
    });

    expect(submitText(page)).toBe('From the structure');
  });

  it('a pack language renders its own strings, falling back to en', async () => {
    const page = await mount('general-form', {
      language: 'uz',
      structure: structure(),
      languagePacks: { languages: { uz: {} }, strings: { forms: { uz: { submit: 'Yuborish' } } } },
    });

    expect(submitText(page)).toBe('Yuborish');
    expect(page.root.shadowRoot.querySelector('form').getAttribute('dir')).toBe('ltr');
  });
});
