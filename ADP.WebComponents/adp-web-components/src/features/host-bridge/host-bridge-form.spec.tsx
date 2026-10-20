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

import { newSpecPage, SpecPage } from '@stencil/core/testing';
import { serializeNodeToHtml } from '@stencil/core/mock-doc';
import { readFileSync } from 'fs';
import { join } from 'path';

import { GeneralForm } from '../../components/forms/general-form';
import { FormStructure } from '../../components/form-elements/form-structure';
import { FormStructureError } from '../../components/form-elements/form-structure-error';
import { FormInput as FormDialog } from '../../components/form-elements/form-dialog';
import { FormSubmit } from '../../components/form-elements/form-submit';
import { FormInput } from '../../components/form-elements/form-input';
import { FormSelect } from '../../components/form-elements/form-select';
import { ShiftSelect } from '../../components/components/shift-select';
import { ShiftSelectDropdown } from '../../components/components/shift-select-dropdown';
import { ShiftPortal } from '../../components/components/shift-portal';
import { FlexibleContainer } from '../../components/components/flexible-container';
import ts from 'typescript';

import { HOST_CONFIG_KEY } from './host-config';
import { AdpRequestInfo } from './adp-fetch';
import { FORM_VALIDITY_KEY } from './validity';

// host-loader.js is shipped as plain ESM, which Jest does not transform.
const { getFormValidity } = (() => {
  const { outputText } = ts.transpileModule(readFileSync(join(__dirname, '../../integration/host-loader.js'), 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2021 },
  });
  const module = { exports: {} };
  new Function('exports', 'module', outputText)(module.exports, module);
  return module.exports as { getFormValidity: (form: Element) => Promise<unknown> };
})();

const COMPONENTS = [
  GeneralForm,
  FormStructure,
  FormStructureError,
  FormDialog,
  FormSubmit,
  FormInput,
  FormSelect,
  ShiftSelect,
  ShiftSelectDropdown,
  ShiftPortal,
  FlexibleContainer,
];

const API = 'https://example.invalid/api';
const SUBMIT = 'https://example.invalid/submit';
const EVENT_TYPES = [
  'adp-form-ready',
  'adp-form-loading-change',
  'adp-form-step-change',
  'adp-form-validity-change',
  'adp-form-submitted',
  'adp-form-error',
  'adp-form-size-change',
] as const;

type Structure = Record<string, unknown>;
type Seen = { type: string; detail: unknown; bubbles: boolean; composed: boolean };
type FormHost = HTMLElement & {
  submit: () => Promise<void>;
  getForm: () => Promise<{ updateStep: (step: number) => void; openDialog: () => void }>;
};

const contactForm = (data: Record<string, unknown> = {}): Structure => ({
  data: { requestUrl: SUBMIT, ...data },
  requiredContext: { name: true, email: true },
  tag: 'div',
  id: 'container',
  children: [{ name: 'name' }, { name: 'email' }, { name: 'submit' }],
});

const steppedForm = (): Structure => ({
  data: { requestUrl: SUBMIT },
  requiredContext: { name: true, email: true },
  steps: [{ en: { title: 'Who', stepTitle: 'Who', submitButton: 'Next' } }, { en: { title: 'Contact', stepTitle: 'Contact', back: 'Back', submitButton: 'Send' } }],
  tag: 'div',
  id: 'container',
  children: [
    { tag: 'div', id: 'one', step: 1, children: [{ name: 'name' }, { name: 'submit' }] },
    { tag: 'div', id: 'two', step: 2, children: [{ name: 'email' }, { name: 'submit' }] },
  ],
});

const branchForm = (): Structure => ({
  data: { requestUrl: SUBMIT },
  tag: 'div',
  id: 'container',
  children: [{ name: 'companyBranchId', branchApi: `${API}/branches` }, { name: 'submit' }],
});

const respond = (body: unknown, status = 200) => ({
  ok: status < 400,
  status,
  headers: { get: () => 'application/json' },
  json: async () => JSON.parse(JSON.stringify(body)),
  text: async () => JSON.stringify(body),
});

let submitAnswer: () => Promise<unknown> = async () => respond({ id: 7, accepted: true });

async function fakeFetch(input: unknown, init: { method?: string } = {}) {
  const url = String(input);

  const localeAt = url.indexOf('/locales/');
  if (localeAt > -1) return respond(JSON.parse(readFileSync(join(__dirname, '../../', url.slice(localeAt + 1)), 'utf8')));
  if (url.startsWith(SUBMIT) && init.method) return submitAnswer();
  if (url.startsWith(`${API}/branches`)) return respond([{ ID: 11, Name: 'North' }]);

  throw new Error(`unexpected request ${url}`);
}

const fetchMock = jest.fn(fakeFetch);

class NoopObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
  takeRecords() {
    return [];
  }
}

type Observation = { callback: (entries: unknown[]) => void; targets: Element[]; disconnected: boolean };
let observations: Observation[] = [];

class RecordingResizeObserver {
  private record: Observation;

  constructor(callback: (entries: unknown[]) => void) {
    this.record = { callback, targets: [], disconnected: false };
    observations.push(this.record);
  }

  observe(target: Element) {
    this.record.targets.push(target);
  }

  unobserve() {}

  disconnect() {
    this.record.disconnected = true;
  }
}

// The browser builds this list from the form's named, enabled controls; mock-doc's FormData cannot read a mock form.
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
  setGlobal('fetch', fetchMock);
  setGlobal('FormData', FormDataFromForm);
  setGlobal('MutationObserver', NoopObserver);
  setGlobal('ResizeObserver', RecordingResizeObserver);
  setGlobal('IntersectionObserver', NoopObserver);
  setGlobal('requestAnimationFrame', (callback: () => void) => setTimeout(callback, 16));
});

afterAll(() => {
  restoreGlobals.reverse().forEach(restore => restore());
  jest.restoreAllMocks();
  jest.useRealTimers();
});

beforeEach(() => {
  observations = [];
  fetchMock.mockClear();
  submitAnswer = async () => respond({ id: 7, accepted: true });
});

afterEach(() => {
  delete (globalThis as Record<symbol, unknown>)[HOST_CONFIG_KEY];
  delete (globalThis as Record<symbol, unknown>)[FORM_VALIDITY_KEY];
});

const EVENTS = { formEvents: true };

const configure = (options: Record<string, unknown>) => {
  const scope = globalThis as Record<symbol, Record<string, unknown>>;
  scope[HOST_CONFIG_KEY] = { ...scope[HOST_CONFIG_KEY], ...options };
};

async function settle(page: SpecPage, ms = 2000) {
  for (let elapsed = 0; elapsed < ms; elapsed += 50) {
    await jest.advanceTimersByTimeAsync(50);
    await page.waitForChanges();
  }
}

async function mount(structure: Structure, props: Record<string, unknown> = {}, listen = true, host: Record<string, unknown> | null = EVENTS) {
  if (host) configure(host);
  const page = await newSpecPage({ components: COMPONENTS, html: '' });
  if (!page.win.matchMedia) (page.win as unknown as Record<string, unknown>).matchMedia = () => ({ matches: false, addEventListener() {}, removeEventListener() {} });

  const el = page.doc.createElement('general-form') as unknown as FormHost;
  Object.assign(el, { language: 'en', structure: JSON.parse(JSON.stringify(structure)), ...props });

  const seen: Seen[] = [];
  if (listen)
    for (const type of EVENT_TYPES)
      page.doc.addEventListener(type, (event: Event) => seen.push({ type, detail: (event as CustomEvent).detail, bubbles: event.bubbles, composed: event.composed }));

  page.body.appendChild(el);
  await settle(page);

  return { page, el, seen };
}

async function fill(page: SpecPage, el: HTMLElement, name: string, value: string) {
  const control = el.shadowRoot.querySelector(`input[name="${name}"]`) as HTMLInputElement;
  control.value = value;
  control.dispatchEvent(new (page.win as unknown as typeof globalThis).Event('input', { bubbles: true }));
  await settle(page, 300);
}

const of = (seen: Seen[], type: string) => seen.filter(entry => entry.type === type);
const details = (seen: Seen[], type: string) => of(seen, type).map(entry => entry.detail);
const roundTrips = (detail: unknown) => expect(JSON.parse(JSON.stringify(detail))).toEqual(detail);
const submitCalls = () => fetchMock.mock.calls.filter(([url]) => String(url).startsWith(SUBMIT));

describe('adpFetch routing in a form', () => {
  it('unset: the submit reaches the global fetch with the same two arguments as before', async () => {
    const { page, el } = await mount(contactForm(), {}, true, null);
    await fill(page, el, 'name', 'Test Person');
    await fill(page, el, 'email', 'person@example.com');

    await el.submit();
    await settle(page);

    expect(submitCalls()).toHaveLength(1);
    const call = submitCalls()[0] as unknown[];
    expect(call).toHaveLength(2);
    expect(call[0]).toBe(SUBMIT);
    expect(call[1]).toEqual(expect.objectContaining({ method: 'POST', body: expect.any(String) }));
    expect(JSON.parse((call[1] as { body: string }).body)).toEqual(expect.objectContaining({ name: 'Test Person', email: 'person@example.com' }));
  });

  it('set: the host gets the submit and the option list with their info, and locale files stay on the global fetch', async () => {
    const hostCalls: [string, RequestInit, AdpRequestInfo][] = [];
    const hostFetch = jest.fn(async (url: string, init: RequestInit, info: AdpRequestInfo) => {
      hostCalls.push([url, init, info]);
      return (await fakeFetch(url, init as { method?: string })) as unknown as Response;
    });
    (globalThis as Record<symbol, unknown>)[HOST_CONFIG_KEY] = { fetch: hostFetch };

    const { page, el } = await mount(branchForm());
    const options = hostCalls.find(([, , info]) => info.kind === 'options');

    expect(options[0]).toBe(`${API}/branches`);
    expect(options[1]).toEqual(expect.objectContaining({ headers: { 'Accept-Language': 'en' } }));
    expect(options[2]).toEqual({ kind: 'options', tag: 'general-form', element: el, field: 'companyBranchId' });

    await el.submit();
    await settle(page);

    const submit = hostCalls.find(([, , info]) => info.kind === 'submit');
    expect(submit[0]).toBe(SUBMIT);
    expect(submit[1]).toEqual(expect.objectContaining({ method: 'POST', body: expect.any(String) }));
    expect(submit[2]).toEqual({ kind: 'submit', tag: 'general-form', element: el });

    const globalUrls = fetchMock.mock.calls.map(([url]) => String(url));
    expect(globalUrls.every(url => url.includes('/locales/'))).toBe(true);
    expect(hostCalls.some(([url]) => url.includes('/locales/'))).toBe(false);
  });
});

describe('form events', () => {
  it('ready, loading and submitted fire with JSON details, bubbling and composed, after the callbacks', async () => {
    const order: string[] = [];
    const successCallback = jest.fn(async () => {
      order.push('successCallback');
      return false;
    });
    const loadingChanges = jest.fn((loading: boolean) => order.push(`loadingChanges:${loading}`));
    const formReadyCallback = jest.fn(() => order.push('formReadyCallback'));

    const { page, el, seen } = await mount(contactForm(), { successCallback, loadingChanges, formReadyCallback });
    el.addEventListener('adp-form-ready', () => order.push('ready'));
    page.doc.addEventListener('adp-form-loading-change', (event: Event) => order.push(`loading:${(event as CustomEvent).detail.loading}`));
    page.doc.addEventListener('adp-form-submitted', () => order.push('submitted'));

    expect(details(seen, 'adp-form-ready')).toEqual([{ tag: 'general-form' }]);
    expect(formReadyCallback).toHaveBeenCalledTimes(1);

    const form = await el.getForm();
    const openDialog = jest.spyOn(form, 'openDialog');
    await fill(page, el, 'name', 'Test Person');
    await fill(page, el, 'email', 'person@example.com');
    await el.submit();
    await settle(page);

    expect(details(seen, 'adp-form-loading-change')).toEqual([{ loading: true }, { loading: false }]);
    expect(loadingChanges.mock.calls).toEqual([[true], [false]]);
    expect(order.filter(step => step.startsWith('loading'))).toEqual(['loadingChanges:true', 'loading:true', 'loadingChanges:false', 'loading:false']);

    const sentBody = JSON.parse((submitCalls()[0][1] as { body: string }).body);
    expect(details(seen, 'adp-form-submitted')).toEqual([{ data: { id: 7, accepted: true }, payload: sentBody }]);
    expect(successCallback).toHaveBeenCalledWith({ id: 7, accepted: true }, expect.any(String));
    expect(order.indexOf('successCallback')).toBeLessThan(order.indexOf('submitted'));
    expect(openDialog).not.toHaveBeenCalled();

    for (const entry of seen) {
      expect(entry.bubbles).toBe(true);
      expect(entry.composed).toBe(true);
      roundTrips(entry.detail);
    }
  });

  it('ready fires once per element, in step with formReadyCallback, and a language change does not fire it again', async () => {
    const formReadyCallback = jest.fn();
    const { page, el, seen } = await mount(contactForm(), { formReadyCallback });

    (el as unknown as { language: string }).language = 'ru';
    await settle(page);
    (el as unknown as { language: string }).language = 'en';
    await settle(page);

    expect(of(seen, 'adp-form-ready')).toHaveLength(1);
    expect(formReadyCallback).toHaveBeenCalledTimes(1);
  });

  it('without a successCallback the dialog still opens and the event still fires', async () => {
    const { page, el, seen } = await mount(contactForm());
    const openDialog = jest.spyOn(await el.getForm(), 'openDialog');
    await fill(page, el, 'name', 'Test Person');
    await fill(page, el, 'email', 'person@example.com');

    await el.submit();
    await settle(page);

    expect(openDialog).toHaveBeenCalledTimes(1);
    expect(of(seen, 'adp-form-submitted')).toHaveLength(1);
  });

  it('error: an HTTP failure carries its status and code, after errorCallback, whose false still suppresses the message', async () => {
    submitAnswer = async () => respond({ message: { body: 'Plate already booked' } }, 422);
    const order: string[] = [];
    const errorCallback = jest.fn(async (_error: Error, message: string) => {
      order.push(`errorCallback:${message}`);
      return false;
    });

    const { page, el, seen } = await mount(contactForm(), { errorCallback });
    page.doc.addEventListener('adp-form-error', () => order.push('error'));
    await fill(page, el, 'name', 'Test Person');
    await fill(page, el, 'email', 'person@example.com');
    await el.submit();
    await settle(page);

    expect(details(seen, 'adp-form-error')).toEqual([{ message: 'Plate already booked', status: 422, code: 'http' }]);
    expect(order).toEqual(['errorCallback:Plate already booked', 'error']);
    expect(errorCallback.mock.calls[0][0]).toBeInstanceOf(Error);
    expect(Object.keys(errorCallback.mock.calls[0][0])).toEqual([]);
    expect(((await el.getForm()) as unknown as { context: { errorMessage?: string } }).context.errorMessage).toBeFalsy();
    expect(details(seen, 'adp-form-submitted')).toEqual([]);
    roundTrips(details(seen, 'adp-form-error')[0]);
  });

  it('error: a rejected request is a network error and a missing endpoint a config error, and ADP still shows its message', async () => {
    submitAnswer = async () => {
      throw new TypeError('Failed to fetch');
    };
    const network = await mount(contactForm());
    await fill(network.page, network.el, 'name', 'Test Person');
    await fill(network.page, network.el, 'email', 'person@example.com');
    await network.el.submit();
    await settle(network.page);

    expect(details(network.seen, 'adp-form-error')).toEqual([{ message: 'Failed to fetch', code: 'network' }]);

    const config = await mount(contactForm({ requestUrl: '' }));
    await fill(config.page, config.el, 'name', 'Test Person');
    await fill(config.page, config.el, 'email', 'person@example.com');
    await config.el.submit();
    await settle(config.page);

    expect(details(config.seen, 'adp-form-error')).toEqual([{ message: 'Request endpoint is not configured', code: 'config' }]);
    expect(((await config.el.getForm()) as unknown as { context: { errorMessage?: string } }).context.errorMessage).toBe('Request endpoint is not configured');
  });

  it('a resubmit while the first successCallback is pending gives each submitted event its own payload', async () => {
    const release: (() => void)[] = [];
    const successCallback = jest.fn(() => new Promise<boolean>(resolve => release.push(() => resolve(false))));
    let answer = 0;
    submitAnswer = async () => respond({ id: ++answer });

    const { page, el, seen } = await mount(contactForm(), { successCallback });
    await fill(page, el, 'name', 'First Person');
    await fill(page, el, 'email', 'first@example.com');
    await el.submit();
    await settle(page);

    await fill(page, el, 'name', 'Second Person');
    await fill(page, el, 'email', 'second@example.com');
    await el.submit();
    await settle(page);

    expect(release).toHaveLength(2);
    release[1]();
    await settle(page, 100);
    release[0]();
    await settle(page, 100);

    const [first, second] = submitCalls().map(([, init]) => JSON.parse((init as { body: string }).body));
    expect(first).toEqual(expect.objectContaining({ name: 'First Person' }));
    expect(second).toEqual(expect.objectContaining({ name: 'Second Person' }));
    expect(details(seen, 'adp-form-submitted')).toEqual([
      { data: { id: 2 }, payload: second },
      { data: { id: 1 }, payload: first },
    ]);
  });

  it('without configureAdpHost no form event fires, and a submit still works', async () => {
    const formReadyCallback = jest.fn();
    const { page, el, seen } = await mount(contactForm(), { formReadyCallback }, true, null);
    await fill(page, el, 'name', 'Test Person');
    await fill(page, el, 'email', 'person@example.com');
    await el.submit();
    await settle(page);

    expect(formReadyCallback).toHaveBeenCalledTimes(1);
    expect(submitCalls()).toHaveLength(1);
    expect(seen).toEqual([]);
  });

  it('step-change reports the step, the previous one, the count and the labels, in both directions', async () => {
    const { page, el, seen } = await mount(steppedForm());
    await fill(page, el, 'name', 'Test Person');

    await el.submit();
    await settle(page);
    (await el.getForm()).updateStep(-1);
    await settle(page, 300);
    (await el.getForm()).updateStep(-1);
    await settle(page, 300);

    expect(details(seen, 'adp-form-step-change')).toEqual([
      { step: 2, previousStep: 1, stepCount: 2, labels: { title: 'Contact', stepTitle: 'Contact', back: 'Back', submitButton: 'Send' } },
      { step: 1, previousStep: 2, stepCount: 2, labels: { title: 'Who', stepTitle: 'Who', submitButton: 'Next' } },
    ]);
    details(seen, 'adp-form-step-change').forEach(roundTrips);
  });
});

describe('validity', () => {
  it('with formValidity: once on render, then only when valid or the invalid fields change, without showing errors', async () => {
    const { page, el, seen } = await mount(contactForm(), {}, true, { formValidity: true });

    expect(details(seen, 'adp-form-validity-change')).toEqual([{ valid: false, step: 1, invalidFields: ['name', 'email'] }]);
    expect(el.shadowRoot.innerHTML).not.toContain('name-require');

    await fill(page, el, 'name', 'Te');
    await fill(page, el, 'name', 'Test Person');
    await fill(page, el, 'name', 'Test Person Two');
    await fill(page, el, 'email', 'person@example.com');

    expect(details(seen, 'adp-form-validity-change')).toEqual([
      { valid: false, step: 1, invalidFields: ['name', 'email'] },
      { valid: false, step: 1, invalidFields: ['email'] },
      { valid: true, step: 1, invalidFields: [] },
    ]);
    details(seen, 'adp-form-validity-change').forEach(roundTrips);
    const errorTexts = Array.from(el.shadowRoot.querySelectorAll('.error-message')).map(node => node.textContent.trim());
    expect(errorTexts.length).toBeGreaterThan(0);
    expect(errorTexts.every(text => text === '')).toBe(true);
  });

  it('follows the current step only', async () => {
    const { page, el, seen } = await mount(steppedForm(), {}, true, { formValidity: true });
    await fill(page, el, 'name', 'Test Person');
    await el.submit();
    await settle(page);

    expect(details(seen, 'adp-form-validity-change')).toEqual([
      { valid: false, step: 1, invalidFields: ['name'] },
      { valid: true, step: 1, invalidFields: [] },
      { valid: false, step: 2, invalidFields: ['email'] },
    ]);
  });

  it('without formValidity no validity event fires, and getFormValidity still answers', async () => {
    const { page, el, seen } = await mount(contactForm());

    await fill(page, el, 'name', 'Test Person');
    expect(await getFormValidity(el)).toEqual({ valid: false, step: 1, invalidFields: ['email'] });

    await fill(page, el, 'email', 'person@example.com');
    const validity = await getFormValidity(el);

    expect(validity).toEqual({ valid: true, step: 1, invalidFields: [] });
    roundTrips(validity);
    expect(of(seen, 'adp-form-validity-change')).toHaveLength(0);
  });

  it('without configureAdpHost nothing is registered and getFormValidity answers the empty default', async () => {
    const { page, el } = await mount(contactForm(), {}, true, null);
    await fill(page, el, 'name', 'Test Person');

    expect((globalThis as Record<symbol, unknown>)[FORM_VALIDITY_KEY]).toBeUndefined();
    expect(await getFormValidity(el)).toEqual({ valid: false, step: 1, invalidFields: [] });
  });
});

describe('size', () => {
  const formObservers = (el: Element) => observations.filter(observation => observation.targets.includes(el));
  const entry = (width: number, height: number) => ({ borderBoxSize: [{ inlineSize: width, blockSize: height }] });

  it('without formSize nothing observes the form', async () => {
    const { el, seen } = await mount(contactForm());

    expect(formObservers(el)).toHaveLength(0);
    expect(of(seen, 'adp-form-size-change')).toHaveLength(0);
  });

  it('with formSize: rounded border-box size, only on change, at most once per frame, released when removed', async () => {
    const { page, el, seen } = await mount(contactForm(), {}, true, { formEvents: true, formSize: true });
    const [observer] = formObservers(el);
    expect(formObservers(el)).toHaveLength(1);

    observer.callback([entry(320.4, 480.6)]);
    observer.callback([entry(320.2, 481.4)]);
    observer.callback([entry(360, 500)]);
    expect(details(seen, 'adp-form-size-change')).toEqual([{ width: 320, height: 481 }]);

    await jest.advanceTimersByTimeAsync(50);
    expect(details(seen, 'adp-form-size-change')).toEqual([
      { width: 320, height: 481 },
      { width: 360, height: 500 },
    ]);

    await jest.advanceTimersByTimeAsync(50);
    observer.callback([entry(360.3, 499.8)]);
    expect(of(seen, 'adp-form-size-change')).toHaveLength(2);
    details(seen, 'adp-form-size-change').forEach(roundTrips);

    el.remove();
    observer.callback([entry(0, 0)]);
    expect(observer.disconnected).toBe(true);
    expect(of(seen, 'adp-form-size-change')).toHaveLength(2);
    await settle(page, 100);
  });
});

describe('opting in changes no DOM', () => {
  it('a form with every listener and every host option renders the same DOM as one without configureAdpHost', async () => {
    const html = (el: HTMLElement) => serializeNodeToHtml(el as never, { prettyHtml: true, serializeShadowRoot: true });
    const filled = async ({ page, el }: { page: SpecPage; el: HTMLElement }) => {
      await fill(page, el, 'name', 'Te');
      await fill(page, el, 'email', 'person@example.com');
      return html(el);
    };

    const plain = await filled(await mount(contactForm(), {}, false, null));
    const opted = await mount(contactForm(), {}, true, { formEvents: true, formValidity: true, formSize: true });

    expect(await filled(opted)).toBe(plain);
    expect(of(opted.seen, 'adp-form-validity-change').length).toBeGreaterThan(0);
  });
});
