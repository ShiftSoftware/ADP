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
import { serializeNodeToHtml } from '@stencil/core/mock-doc';
import { readdirSync, readFileSync } from 'fs';
import { join } from 'path';

import examples from '../../templates/forms/examples.json';

import { GeneralForm } from '../../components/forms/general-form';
import { GeneralInquiryForm } from '../../components/forms/general-inquiry';
import { ServiceBookingForm } from '../../components/forms/service-booking';
import { TestDriveForm } from '../../components/forms/test-drive';
import { SSCLookupForm } from '../../components/forms/ssc-lookup';
import { VehicleQuotationForm } from '../../components/forms/vehicle-quotation';
import { FormStructure } from '../../components/form-elements/form-structure';
import { FormStructureError } from '../../components/form-elements/form-structure-error';
import { FormInput as FormDialog } from '../../components/form-elements/form-dialog';
import { FormSubmit } from '../../components/form-elements/form-submit';
import { FormStepper as FormInputPreview } from '../../components/form-elements/form-input-preview';
import { FormFile } from '../../components/form-elements/form-file';
import { FormStepper } from '../../components/form-elements/form-stepper';
import { FormStepperControl } from '../../components/form-elements/form-stepper-control';
import { FormStepperSubmit } from '../../components/form-elements/form-stepper-submit';
import { FormInput } from '../../components/form-elements/form-input';
import { FormTextArea } from '../../components/form-elements/form-text-area';
import { FormVinInput } from '../../components/form-elements/form-vin-input';
import { FormPhoneNumber } from '../../components/form-elements/form-phone-number';
import { FormSelect } from '../../components/form-elements/form-select';
import { FormInput as FormShadowInput } from '../../components/form-elements/form-shadow-input';
import { FormPickerInput } from '../../components/form-elements/form-picker-input';
import { BranchSlotPicker } from '../../components/form-elements/branch-slot-picker';
import { BranchSlotDropdown } from '../../components/form-elements/branch-slot-dropdown';
import { BranchDatePicker } from '../../components/form-elements/branch-date-picker';
import { BranchDateDropdown } from '../../components/form-elements/branch-date-dropdown';
import { VinExtractor } from '../../components/vin-extractor/vin-extractor';
import { ShiftSelect } from '../../components/components/shift-select';
import { ShiftSelectDropdown } from '../../components/components/shift-select-dropdown';
import { ShiftPortal } from '../../components/components/shift-portal';
import { FlexibleContainer } from '../../components/components/flexible-container';

const COMPONENTS = [
  GeneralForm,
  GeneralInquiryForm,
  ServiceBookingForm,
  TestDriveForm,
  SSCLookupForm,
  VehicleQuotationForm,
  FormStructure,
  FormStructureError,
  FormDialog,
  FormSubmit,
  FormInputPreview,
  FormFile,
  FormStepper,
  FormStepperControl,
  FormStepperSubmit,
  FormInput,
  FormTextArea,
  FormVinInput,
  FormPhoneNumber,
  FormSelect,
  FormShadowInput,
  FormPickerInput,
  BranchSlotPicker,
  BranchSlotDropdown,
  BranchDatePicker,
  BranchDateDropdown,
  VinExtractor,
  ShiftSelect,
  ShiftSelectDropdown,
  ShiftPortal,
  FlexibleContainer,
];

const TODAY = '2026-03-16';
const API = 'https://example.invalid/api';
const SUBMIT = 'https://example.invalid/submit';
const PAGE_URL = 'http://testing.stenciljs.com/forms?utm_source=newsletter&utm_campaign=spring&gclid=test-click';
const LANGUAGES = ['en', 'ar'] as const;
const VALID_VIN = '1M8GDM9AXKP042788';

type Language = (typeof LANGUAGES)[number];
type FillValue = string | Record<Language, string>;
type Structure = Record<string, unknown>;

type Case = {
  id: string;
  component: string;
  structure?: Structure;
  fields?: Record<string, unknown>;
  hostProps?: Record<string, unknown>;
  fill: [string, FillValue][];
  domAfterFill?: boolean;
  renderOnly?: boolean;
};

type SelectElement = { options: { value: string }[]; handleSelection: (option: unknown) => void };

type Sent = { url: string; method: string; headers: unknown; body: unknown };

const branches = [
  {
    ID: 'branch-1',
    Name: 'North branch',
    IntegrationId: 'north',
    CompanyIntegrationId: 'company-a',
    Departments: [{ IntegrationId: 'showroom' }, { IntegrationId: 'service-center' }],
    Brands: [{ IntegrationId: 'brand-a' }],
  },
  {
    ID: 'branch-2',
    Name: 'South branch',
    IntegrationId: 'south',
    CompanyIntegrationId: 'company-a',
    Departments: [{ IntegrationId: 'service-center' }],
    Brands: [{ IntegrationId: 'brand-a' }],
  },
];

const brands = [
  {
    ID: 1,
    Name: 'Brand A',
    Models: [
      { ID: 101, Name: 'Model A1' },
      { ID: 102, Name: 'Model A2' },
    ],
  },
  { ID: 2, Name: 'Brand B', Models: [{ ID: 201, Name: 'Model B1' }] },
];

const apiResponses: Record<string, unknown> = {
  '/branches': branches,
  '/vehicles': [
    { ID: 11, Title: 'Model One', Image: 'https://example.invalid/images/model-one.png' },
    { ID: 12, Title: 'Model Two', Image: 'https://example.invalid/images/model-two.png' },
  ],
  '/vehicles-strapi': {
    data: [
      { id: 21, attributes: { GradeName: 'Grade One', Cover: { data: { attributes: { url: 'https://example.invalid/images/grade-one.png' } } } } },
      { id: 22, attributes: { GradeName: 'Grade Two', Cover: { data: { attributes: { url: 'https://example.invalid/images/grade-two.png' } } } } },
    ],
  },
  '/vehicles-dynamic': {
    result: {
      items: [
        { key: 'model-x', title: 'Model X' },
        { key: 'model-y', title: 'Model Y' },
      ],
    },
  },
  '/cities': [
    { ID: 31, Name: 'City One' },
    { ID: 32, Name: 'City Two' },
  ],
  '/vacancies': [
    { ID: 41, Title: 'Service advisor' },
    { ID: 42, Title: 'Sales consultant' },
  ],
  '/brands': brands,
  '/structures/callback.json': {
    data: { brandId: 'brand-a', requestUrl: `${SUBMIT}/general-inquiry`, extraPayload: { generalTicketType: 'GeneralInquiry' } },
    requiredContext: { name: true, phone: true },
    tag: 'div',
    children: [{ name: 'name' }, { name: 'phone', countryCode: 'AE' }, { name: 'submit' }],
  },
  '/calendar': [
    { Date: '2026-03-19', Times: ['11:00', '09:00', '10:00', '09:00'] },
    { Date: '2026-03-18', Times: ['09:00', '13:00'] },
    { Date: '2026-03-20', Times: [] },
  ],
};

// Submitted instants depend on the machine's zone; the wall-clock time a visitor picked does not, so that is what is compared.
const ISO_INSTANT = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d+)?)?(Z|[+-]\d{2}:\d{2})$/;
const pad = (n: number, width = 2) => String(n).padStart(width, '0');

function wallClock(value: unknown): unknown {
  if (typeof value === 'string' && ISO_INSTANT.test(value)) {
    const d = new Date(value);
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())} local (sent as ${/\.\d{3}Z$/.test(value) ? 'a Date' : 'text'})`;
  }
  if (Array.isArray(value)) return value.map(wallClock);
  if (value && typeof value === 'object') return Object.fromEntries(Object.entries(value).map(([key, inner]) => [key, wallClock(inner)]));
  return value;
}

let sent: Sent[] = [];
let unexpected: string[] = [];

const respond = (body: unknown, status = 200) => ({
  ok: status < 400,
  status,
  headers: { get: () => 'application/json' },
  json: async () => JSON.parse(JSON.stringify(body)),
  text: async () => JSON.stringify(body),
});

async function fakeFetch(input: unknown, init: { method?: string; headers?: unknown; body?: string } = {}) {
  const url = String(input);

  const localeAt = url.indexOf('/locales/');
  if (localeAt > -1) return respond(JSON.parse(readFileSync(join(__dirname, '../../', url.slice(localeAt + 1)), 'utf8')));

  // Only the submit passes a method; every fetcher passes just a signal and headers.
  if (init.method) {
    sent.push({ url, method: init.method, headers: init.headers, body: init.body ? wallClock(JSON.parse(init.body)) : undefined });
    return respond({ id: 'TICKET-0001' });
  }

  if (url.startsWith(API)) {
    const path = url.slice(API.length).split('?')[0];
    if (path in apiResponses) return respond(apiResponses[path]);
  }

  // Vehicle pictures are refused, the way a host without cross-origin access refuses them.
  if (url.startsWith('https://example.invalid/images/')) throw new Error('image blocked');

  unexpected.push(url);
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

// The browser builds this list from the form's named, enabled controls; mock-doc's FormData cannot read a mock form.
class FormDataFromForm {
  private list: [string, unknown][] = [];

  constructor(form?: Element) {
    form?.querySelectorAll('input[name], select[name], textarea[name]').forEach((el: Element) => {
      const control = el as HTMLInputElement;
      if (control.disabled || ['submit', 'button', 'reset', 'image'].includes(control.type)) return;
      if (['checkbox', 'radio'].includes(control.type) && !control.checked) return;
      this.list.push([control.getAttribute('name'), control.type === 'file' ? '' : (control.value ?? '')]);
    });
  }

  entries() {
    return this.list[Symbol.iterator]();
  }
}

// A browser's defaultValue sets the value content attribute, which an untouched input reads as its value; mock-doc stores it as an unrelated attribute.
function browserDefaultValue() {
  const proto = Object.getPrototypeOf(document.createElement('input'));
  const previous = Object.getOwnPropertyDescriptor(proto, 'defaultValue');
  Object.defineProperty(proto, 'defaultValue', {
    configurable: true,
    get(this: Element) {
      return this.getAttribute('value') ?? '';
    },
    set(this: Element, value: unknown) {
      this.setAttribute('value', value == null ? '' : String(value));
    },
  });
  return () => (previous ? Object.defineProperty(proto, 'defaultValue', previous) : delete proto.defaultValue);
}

const restoreGlobals: (() => void)[] = [];
const setGlobal = (name: string, value: unknown) => {
  const had = Object.prototype.hasOwnProperty.call(globalThis, name);
  const previous = (globalThis as Record<string, unknown>)[name];
  (globalThis as Record<string, unknown>)[name] = value;
  restoreGlobals.push(() => (had ? ((globalThis as Record<string, unknown>)[name] = previous) : delete (globalThis as Record<string, unknown>)[name]));
};

beforeAll(() => {
  jest.useFakeTimers({ now: new Date(`${TODAY}T08:00:00.000Z`), doNotFake: ['nextTick', 'queueMicrotask', 'setImmediate'] });
  jest.spyOn(Math, 'random').mockReturnValue(0.42);
  // v1 logs Stencil dev-mode notices and its own debug output on every render; the snapshots carry the outcome.
  for (const method of ['log', 'warn', 'error'] as const) jest.spyOn(console, method).mockImplementation(() => undefined);

  restoreGlobals.push(browserDefaultValue());
  setGlobal('fetch', jest.fn(fakeFetch));
  setGlobal('FormData', FormDataFromForm);
  setGlobal('MutationObserver', NoopObserver);
  setGlobal('ResizeObserver', NoopObserver);
  setGlobal('IntersectionObserver', NoopObserver);
  setGlobal('grecaptcha', { ready: (callback: () => void) => callback(), execute: async () => 'recaptcha-token' });
});

afterAll(() => {
  restoreGlobals.reverse().forEach(restore => restore());
  jest.restoreAllMocks();
  jest.useRealTimers();
});

async function settle(page: SpecPage, ms = 3000) {
  for (let elapsed = 0; elapsed < ms; elapsed += 50) {
    await jest.advanceTimersByTimeAsync(50);
    await page.waitForChanges();
  }
}

async function mount(testCase: Case, language: Language) {
  sent = [];
  unexpected = [];

  const page = await newSpecPage({
    components: COMPONENTS,
    url: PAGE_URL,
    template: () =>
      h(testCase.component, {
        language,
        today: TODAY,
        structure: testCase.structure && JSON.parse(JSON.stringify(testCase.structure)),
        fields: testCase.fields,
        ...testCase.hostProps,
      }),
  });

  if (!page.win.matchMedia) (page.win as unknown as Record<string, unknown>).matchMedia = () => ({ matches: false, addEventListener() {}, removeEventListener() {} });

  await settle(page);

  return page;
}

// Mock-doc gives <textarea> no `name` property; a browser does, and form-hook reads it.
const nameTextareas = (page: SpecPage) =>
  page.root.shadowRoot.querySelectorAll('textarea[name]').forEach(el => {
    if ((el as HTMLTextAreaElement).name === undefined) Object.defineProperty(el, 'name', { configurable: true, get: () => el.getAttribute('name') });
  });

const dom = (page: SpecPage) => serializeNodeToHtml(page.body as never, { prettyHtml: true, serializeShadowRoot: true });

// A select only offers options it can label, so a missing option is recorded as part of the outcome, not thrown.
async function fill(page: SpecPage, entries: Case['fill'], language: Language) {
  const formRoot = page.root.shadowRoot;
  const unselectable: string[] = [];

  for (const [name, raw] of entries) {
    const value = typeof raw === 'string' ? raw : raw[language];
    const select = Array.from(formRoot.querySelectorAll('shift-select')).find(el => (el as unknown as { name: string }).name === name) as unknown as SelectElement | undefined;

    if (select) {
      const option = select.options.find(item => item.value === value);
      if (option) select.handleSelection(option);
      else unselectable.push(`${name}=${value} (offered: ${select.options.map(item => item.value).join(', ') || 'nothing'})`);
    } else {
      const control = formRoot.querySelector(`input[name="${name}"]:not([hidden]), textarea[name="${name}"]`) as HTMLInputElement | null;
      if (!control) throw new Error(`${name} has no control to fill`);
      control.value = value;
      control.dispatchEvent(new (page.win as unknown as typeof globalThis).Event('input', { bubbles: true }));
    }

    await settle(page, 500);
  }

  return unselectable;
}

async function submitUntilSent(page: SpecPage, steps: number) {
  const host = page.root as unknown as { submit: () => Promise<void>; getForm: () => Promise<{ getFormErrors: () => Record<string, string> }> };

  for (let attempt = 0; attempt < steps && !sent.length; attempt++) {
    await host.submit();
    await settle(page, 1500);
  }

  return sent.length ? sent : { notSent: (await host.getForm()).getFormErrors(), errorMessage: (page.rootInstance as { errorMessage?: string }).errorMessage };
}

const localized = (en: Record<string, string>, ar: Record<string, string>) => ({ en, ar });

const exampleCases: Case[] = examples.presets.map(preset => ({
  id: `example ${preset.id}`,
  component: preset.component,
  structure: preset.structure as unknown as Structure,
  fill: {
    'general-inquiry-form': [
      ['generalTicketType', 'Complaint'],
      ['name', 'Test Person'],
      ['email', 'person@example.com'],
      ['phone', '7501234567'],
      ['orderReference', 'REF-100'],
      ['message', 'A message long enough to pass.'],
    ],
    'service-booking-form': [
      ['vin', VALID_VIN],
      ['year', '2020'],
      ['plateNumber', 'PLATE-7'],
      ['date', '2026-03-18'],
      ['time', '10:30'],
      ['name', 'Test Person'],
      ['phone', '7501234567'],
      ['message', 'A message long enough to pass.'],
    ],
    'test-drive-form': [
      ['name', 'Test Person'],
      ['email', 'person@example.com'],
      ['phone', '7501234567'],
      ['licenceNumber', 'LIC-42'],
      ['ownVehicle', 'no'],
      ['date', '2026-03-18'],
      ['message', 'A message long enough to pass.'],
    ],
    'vehicle-quotation-form': [
      ['paymentType', 'Cash'],
      ['contactTime', 'Noon'],
      ['name', 'Test Person'],
      ['phone', '7501234567'],
    ],
  }[preset.component] as Case['fill'],
}));

const prototypeSetups: Record<string, Pick<Case, 'component' | 'fill' | 'domAfterFill'>> = {
  'test-drive-slots.json': {
    component: 'test-drive-form',
    domAfterFill: true,
    fill: [
      ['vehicle', 'Model Two'],
      ['name', 'Test Person'],
      ['email', 'person@example.com'],
      ['phone', '7501234567'],
      ['companyBranchId', 'branch-1'],
    ],
  },
};

const PROTOTYPES = join(__dirname, '../../templates/prototypes/structures');

const prototypeCases: Case[] = readdirSync(PROTOTYPES)
  .filter(file => file.endsWith('.json'))
  .sort()
  .map(file => ({
    id: `prototype ${file.replace(/\.json$/, '')}`,
    structure: JSON.parse(readFileSync(join(PROTOTYPES, file), 'utf8')),
    ...(prototypeSetups[file] ?? { component: `no setup for ${file}; add one to prototypeSetups`, fill: [] }),
  }));

const nameFields = [
  {
    name: 'name',
    localization: localized(
      { label: 'First name', placeholder: 'First name', require: 'First name is required', format: 'Too short' },
      { label: 'الاسم', placeholder: 'الاسم', require: 'مطلوب', format: 'قصير' },
    ),
  },
  {
    name: 'lastName',
    localization: localized(
      { label: 'Last name', placeholder: 'Last name', require: 'Last name is required', format: 'Too short' },
      { label: 'اللقب', placeholder: 'اللقب', require: 'مطلوب', format: 'قصير' },
    ),
  },
  { name: 'email', localization: localized({ label: 'Email', placeholder: 'Email', format: 'Not an email' }, { label: 'البريد', placeholder: 'البريد', format: 'غير صالح' }) },
];

const phoneField = (countryCode: string | string[]) => ({
  name: 'phone',
  countryCode,
  localization: localized({ label: 'Phone', placeholder: 'Phone', require: 'Phone is required' }, { label: 'الهاتف', placeholder: 'الهاتف', require: 'مطلوب' }),
});

const submitted = (en: string, ar: string) => ({ en: { 'Form submitted successfully.': en }, ar: { 'Form submitted successfully.': ar } });

const productionShapeCases: Case[] = [
  {
    id: 'shape contact with split name',
    component: 'general-form',
    structure: {
      data: {
        brandId: 'brand-a',
        recaptchaKey: 'test-site-key',
        requestUrl: `${SUBMIT}/general-inquiry?production=true`,
        extraPayload: { generalTicketType: 'GeneralInquiry' },
        truncatedFields: { lastName: 'lastName' },
        localization: submitted('Request sent.', 'تم الإرسال.'),
      },
      requiredContext: { name: true, phone: true, lastName: true },
      tag: 'div',
      id: 'container',
      children: [{ tag: 'div', id: 'inputs_2_col_wrapper', children: [...nameFields, phoneField(['AE', 'JO', 'SA'])] }, { name: 'message' }],
    },
    fill: [
      ['name', 'Test'],
      ['lastName', 'Person'],
      ['email', 'person@example.com'],
      ['phone', '501234567'],
      ['message', 'A message long enough to pass.'],
    ],
  },
  {
    id: 'shape inquiry type select',
    component: 'general-inquiry-form',
    structure: {
      data: {
        brandId: 'brand-a',
        recaptchaKey: 'test-site-key',
        requestUrl: `${SUBMIT}/general-inquiry`,
        truncatedFields: { lastName: 'lastName' },
        localization: submitted('Request sent.', 'تم الإرسال.'),
      },
      requiredContext: { name: true, phone: true, email: true, lastName: true, generalTicketType: true },
      tag: 'div',
      id: 'container',
      children: [
        { tag: 'div', id: 'inputs_2_col_wrapper', children: [...nameFields, phoneField(['AE', 'JO', 'SA'])] },
        {
          name: 'generalTicketType',
          options: [
            { value: 'GeneralInquiry', en: 'General', ar: 'عام' },
            { value: 'ServicePriceInquiry', en: 'Service price', ar: 'سعر الخدمة' },
            { value: 'Complaint', en: 'Feedback', ar: 'ملاحظات' },
          ],
          localization: localized({ placeholder: 'Topic', require: 'Pick a topic' }, { placeholder: 'الموضوع', require: 'مطلوب' }),
        },
        { name: 'message' },
      ],
    },
    fill: [
      ['name', 'Test'],
      ['lastName', 'Person'],
      ['email', 'person@example.com'],
      ['phone', '501234567'],
      ['generalTicketType', 'ServicePriceInquiry'],
      ['message', 'A message long enough to pass.'],
    ],
  },
  {
    id: 'shape branch callback',
    component: 'general-form',
    structure: {
      data: {
        brandId: 'brand-a',
        recaptchaKey: 'test-site-key',
        requestUrl: `${SUBMIT}/general-inquiry`,
        extraPayload: { generalTicketType: 'GeneralInquiry', message: 'Callback request' },
        localization: submitted('Request sent.', 'تم الإرسال.'),
      },
      requiredContext: { name: true, phone: true, companyBranchId: true },
      tag: 'div',
      id: 'container',
      children: [
        {
          name: 'companyBranchId',
          branchApi: `${API}/branches?dropDownMode=true`,
          localization: localized({ label: 'Branch', placeholder: 'Branch' }, { label: 'الفرع', placeholder: 'الفرع' }),
        },
        nameFields[0],
        phoneField('AE'),
      ],
    },
    fill: [
      ['companyBranchId', 'branch-2'],
      ['name', 'Test Person'],
      ['phone', '501234567'],
    ],
  },
  {
    id: 'shape vacancy with file upload',
    component: 'general-form',
    structure: {
      data: {
        brandId: 'brand-a',
        recaptchaKey: 'test-site-key',
        requestUrl: `${SUBMIT}/vacancy`,
        truncatedFields: { lastName: 'lastName' },
        localization: submitted('Application sent.', 'تم الإرسال.'),
      },
      requiredContext: { name: true, phone: true, email: true, lastName: true, vacancyId: true },
      tag: 'div',
      id: 'container',
      children: [
        { tag: 'div', id: 'inputs_2_col_wrapper', children: [...nameFields, phoneField(['AE', 'JO', 'SA'])] },
        { name: 'vacancyId', vacancyApi: `${API}/vacancies`, localization: localized({ placeholder: 'Vacancies' }, { placeholder: 'الوظائف' }) },
        { name: 'message' },
        {
          name: 'attachments',
          type: 'fileUploader',
          maxSize: 2,
          maxUpload: 1,
          signPrefix: 'Uploads/Test/',
          accountName: 'teststorage',
          containerName: 'files',
          signUrl: `${API}/sign-upload`,
          required: true,
          accept: '.pdf,.doc,.docx',
          localization: localized(
            { upload: 'Upload your file', size: 'Max 2 MB', require: 'File is required' },
            { upload: 'ارفع ملفك', size: 'الحد 2 ميغابايت', require: 'مطلوب' },
          ),
        },
      ],
    },
    fill: [
      ['name', 'Test'],
      ['lastName', 'Person'],
      ['email', 'person@example.com'],
      ['phone', '501234567'],
      ['vacancyId', '42'],
      ['message', 'A message long enough to pass.'],
    ],
  },
  {
    id: 'shape vin lookup',
    component: 'ssc-lookup-form',
    structure: {
      data: { requestMethod: 'GET', requestUrl: `${SUBMIT}/vehicle-lookup/\${vin}?log=true`, localization: submitted('Done.', 'تم.') },
      requiredContext: { name: true, phone: true, vin: true },
      tag: 'div',
      id: 'container',
      children: [nameFields[0], phoneField(['AE', 'JO', 'SA']), { name: 'vin', useOcr: true, ocrEndpoint: `${API}/ocr`, scannerIcon: 'camera' }],
    },
    fill: [
      ['name', 'Test Person'],
      ['phone', '501234567'],
      ['vin', VALID_VIN],
    ],
  },
  {
    id: 'shape service booking with date and time',
    component: 'service-booking-form',
    structure: {
      data: {
        brandId: 'brand-a',
        recaptchaKey: 'test-site-key',
        requestUrl: `${SUBMIT}/service-booking`,
        truncatedFields: { 'name': ['name', ' ', 'lastName'], 'bookingDate': ['date', ' ', 'time'], 'parse date: bookingDate': 'yyyy-MM-dd HH:mm', 'lastName': 'lastName' },
        localization: submitted('Booked.', 'تم الحجز.'),
      },
      requiredContext: { name: true, lastName: true, phone: true, companyBranchId: true, vin: true, date: true, time: true },
      tag: 'div',
      id: 'container',
      children: [
        { tag: 'div', id: 'inputs_2_col_wrapper', children: [...nameFields, phoneField('AE')] },
        { name: 'companyBranchId', branchApi: `${API}/branches` },
        { name: 'vehicle', useNamedValue: true, vehicleIdQueryParam: 'model', vehicleApi: `${API}/vehicles` },
        { name: 'vin' },
        {
          tag: 'div',
          class: 'inputs_2_col_wrapper',
          children: [
            { name: 'date', min: [0, 0, 1], max: [0, 2, 0] },
            { name: 'time', format: 'HH:mm', min: [0, 0, 0, 9, 0], max: [0, 0, 0, 18, 0], span: [0, 0, 0, 0, 30] },
          ],
        },
        { name: 'message' },
      ],
    },
    fill: [
      ['name', 'Test'],
      ['lastName', 'Person'],
      ['email', 'person@example.com'],
      ['phone', '501234567'],
      ['companyBranchId', 'branch-1'],
      ['vehicle', 'Model One'],
      ['vin', VALID_VIN],
      ['date', '2026-03-18'],
      ['time', '10:30'],
      ['message', 'A message long enough to pass.'],
    ],
  },
  {
    id: 'shape test drive with date and time',
    component: 'test-drive-form',
    structure: {
      data: {
        brandId: 'brand-a',
        recaptchaKey: 'test-site-key',
        requestUrl: `${SUBMIT}/test-drive`,
        truncatedFields: { 'name': ['name', ' ', 'lastName'], 'testDriveDate': ['date', ' ', 'time'], 'parse date: testDriveDate': 'yyyy-MM-dd HH:mm', 'lastName': 'lastName' },
        localization: submitted('Booked.', 'تم الحجز.'),
      },
      requiredContext: { vehicle: true, name: true, lastName: true, email: true, phone: true, companyBranchId: true, date: true, time: true },
      tag: 'div',
      id: 'container',
      children: [
        { tag: 'div', id: 'inputs_2_col_wrapper', children: [...nameFields, phoneField('AE')] },
        { name: 'companyBranchId', branchApi: `${API}/branches?services=test-drive` },
        { name: 'vehicle', useNamedValue: true, vehicleIdQueryParam: 'model', vehicleApi: `${API}/vehicles?availableForTestDrive=true` },
        {
          tag: 'div',
          class: 'inputs_2_col_wrapper',
          children: [
            { name: 'date', min: [0, 0, 1], max: [0, 2, 0] },
            { name: 'time', format: 'HH:mm', min: [0, 0, 0, 9, 0], max: [0, 0, 0, 18, 0], span: [0, 0, 0, 0, 30] },
          ],
        },
        { name: 'message' },
      ],
    },
    fill: [
      ['name', 'Test'],
      ['lastName', 'Person'],
      ['email', 'person@example.com'],
      ['phone', '501234567'],
      ['companyBranchId', 'branch-1'],
      ['vehicle', 'Model Two'],
      ['date', '2026-03-18'],
      ['time', '09:00'],
    ],
  },
  {
    id: 'shape booking calendar with model year',
    component: 'service-booking-form',
    structure: {
      data: {
        brandId: 'brand-a',
        recaptchaKey: 'test-site-key',
        requestUrl: `${SUBMIT}/service-booking`,
        truncatedFields: {
          'name': ['name', ' ', 'lastName'],
          'vehicle': ['vehicle', ' - ', 'year'],
          'parse date: bookingDate': "yyyy-MM-dd'T'HH:mm",
          'format date: bookingDate': "yyyy-MM-dd'T'HH:mm",
        },
        localization: submitted('Booked.', 'تم الحجز.'),
      },
      requiredContext: { vehicle: true, year: true, name: true, lastName: true, phone: true, companyBranchId: true, vin: true, bookingDate: true },
      tag: 'div',
      id: 'container',
      children: [
        { name: 'vehicle', useNamedValue: true, vehicleIdQueryParam: 'model', vehicleApi: `${API}/vehicles` },
        {
          name: 'year',
          reverseOptions: true,
          firstOption: { label: { en: 'Older model', ar: 'طراز أقدم' }, value: { en: 'Older model', ar: 'طراز أقدم' } },
        },
        nameFields[0],
        nameFields[1],
        phoneField('JO'),
        { name: 'companyBranchId', branchApi: `${API}/branches` },
        { name: 'vin' },
        { name: 'bookingDate', calendarApi: `${API}/calendar`, departmentId: 'service-center', brandId: 'brand-a' },
        { name: 'submit' },
      ],
    },
    domAfterFill: true,
    fill: [
      ['vehicle', 'Model One'],
      ['year', { en: 'Older model', ar: 'طراز أقدم' }],
      ['name', 'Test'],
      ['lastName', 'Person'],
      ['phone', '791234567'],
      ['companyBranchId', 'branch-1'],
      ['vin', VALID_VIN],
    ],
  },
  {
    id: 'shape contact with city',
    component: 'general-inquiry-form',
    structure: {
      data: { brandId: 'brand-a', recaptchaKey: 'test-site-key', requestUrl: `${SUBMIT}/general-inquiry`, theme: 'neutral' },
      requiredContext: { name: true, email: true, cityId: true, phone: true, generalTicketType: true, message: true },
      tag: 'div',
      id: 'container',
      children: [
        { name: 'name' },
        { name: 'email' },
        { name: 'cityId', cityApi: `${API}/cities` },
        phoneField('JO'),
        {
          name: 'generalTicketType',
          options: [
            { value: 'GeneralInquiry', en: 'General', ar: 'عام' },
            { value: 'Complaint', en: 'Complaint', ar: 'شكوى' },
          ],
        },
        { name: 'message' },
        { name: 'submit' },
      ],
    },
    fill: [
      ['name', 'Test Person'],
      ['email', 'person@example.com'],
      ['cityId', '32'],
      ['phone', '791234567'],
      ['generalTicketType', 'GeneralInquiry'],
      ['message', 'A message long enough to pass.'],
    ],
  },
  {
    id: 'shape fields override from the host',
    component: 'general-form',
    structure: {
      data: { brandId: 'brand-a', requestUrl: `${SUBMIT}/general-inquiry`, extraPayload: { generalTicketType: 'GeneralInquiry' } },
      requiredContext: { name: true, phone: true, companyBranchId: true, vehicle: true },
      tag: 'div',
      id: 'container',
      children: [
        { name: 'name' },
        phoneField(['AE', 'JO']),
        { name: 'companyBranchId', branchApi: `${API}/branches` },
        { name: 'vehicle', useNamedValue: true, vehicleApi: `${API}/vehicles` },
      ],
    },
    hostProps: { extraPayload: { sourceForm: 'page.cta', vehicle: 'Model One' } },
    fields: {
      name: { localization: localized({ label: 'Host name label' }, { label: 'اسم من المضيف' }) },
      vehicle: { staticValue: { value: 'Model Two', label: 'Model Two' } },
    },
    fill: [
      ['name', 'Test Person'],
      ['phone', '501234567'],
      ['companyBranchId', 'branch-1'],
    ],
  },
];

const coverageCases: Case[] = [
  {
    id: 'coverage general mapper keys',
    component: 'general-form',
    structure: {
      data: { requestUrl: `${SUBMIT}/general`, currentVehiclesApi: `${API}/brands`, theme: 'neutral' },
      requiredContext: { ownVehicle: true },
      tag: 'div',
      id: 'container',
      children: [
        { tag: 'h2', class: 'section-title', children: { en: 'Your vehicle', ar: 'مركبتك' } },
        { 'name': 'vehicle', 'dynamic': true, 'items': 'result.items', 'item-value': 'key', 'item-label': 'title', 'vehicleApi': `${API}/vehicles-dynamic` },
        { name: 'vehicleImage' },
        { name: 'ownVehicle', localization: localized({ yes: 'Yes', no: 'No' }, { yes: 'نعم', no: 'لا' }) },
        { name: 'conditionalCurrentVehicleBrand', brandApi: `${API}/brands`, useNamedValue: true },
        { name: 'conditionalCurrentVehicleModel', useNamedValue: true },
        { name: 'currentVehicleBrand' },
        { name: 'currentVehicleModel' },
        {
          name: 'year',
          min: 2022,
          max: 2024,
          firstOption: { label: { en: 'Newer', ar: 'أحدث' }, value: { en: 'newer', ar: 'newer' } },
          lastOption: { label: { en: 'Older', ar: 'أقدم' }, value: { en: 'older', ar: 'older' } },
        },
        { name: 'cityId', cityApi: `${API}/cities` },
        { name: 'modelCode', type: 'name', staticValue: 'CODE-1' },
        { name: 'referral', type: 'name', isHidden: true },
        { name: 'summary', type: 'inputPreview', localization: localized({ label: 'Summary', value: '${vehicle} / ${year}' }, { label: 'الملخص', value: '${vehicle} / ${year}' }) },
        { name: 'photo', type: 'file' },
        { name: 'bookingSlot', calendarApi: `${API}/calendar`, departmentId: 'showroom', brandId: 'brand-a' },
        { name: 'companyBranchId', branchApi: `${API}/branches` },
        { name: 'vehicleByGrade', type: 'vehicle', vehiclesApiStrapiFormat: true, vehicleApi: `${API}/vehicles-strapi` },
        { name: 'submit' },
      ],
    },
    domAfterFill: true,
    fill: [
      ['vehicle', 'model-y'],
      ['ownVehicle', 'yes'],
      ['conditionalCurrentVehicleBrand', 'Brand A'],
      ['conditionalCurrentVehicleModel', 'Model A2'],
      ['currentVehicleBrand', '2'],
      ['currentVehicleModel', '201'],
      ['year', 'older'],
      ['cityId', '31'],
      ['companyBranchId', 'branch-1'],
      ['vehicleByGrade', '22'],
    ],
  },
  {
    id: 'coverage vehicle quotation keys',
    component: 'vehicle-quotation-form',
    structure: {
      data: {
        requestUrl: `${SUBMIT}/vehicle-quotation`,
        recaptchaKey: 'test-site-key',
        brandId: 'brand-a',
        quotationType: 'NewVehiclePurchase',
        theme: 'neutral',
        vehicleApi: `${API}/vehicles`,
        vehicleIdQueryParam: 'model',
        dealerApi: `${API}/branches`,
        cityApi: `${API}/cities`,
        currentVehiclesApi: `${API}/brands`,
        localization: {
          en: {
            'Yes': 'Yes',
            'No': 'No',
            'Other': 'Other',
            'Cash': 'Cash',
            'Installments': 'Installments',
            'Flexible': 'Flexible',
            'Morning': 'Morning',
            'Noon': 'Noon',
            'Afternoon': 'Afternoon',
            'Choose': 'Choose',
            'Your current car': 'Your current car',
            'Contact Information': 'Contact information',
          },
          ar: {
            'Yes': 'نعم',
            'No': 'لا',
            'Other': 'أخرى',
            'Cash': 'نقدا',
            'Installments': 'أقساط',
            'Flexible': 'مرن',
            'Morning': 'صباحا',
            'Noon': 'ظهرا',
            'Afternoon': 'عصرا',
            'Choose': 'اختر',
            'Your current car': 'سيارتك الحالية',
            'Contact Information': 'معلومات الاتصال',
          },
        },
      },
      requiredContext: { ownVehicle: true, paymentType: true, contactTime: true },
      tag: 'div',
      id: 'container',
      children: [
        { name: 'choose' },
        { name: 'vehicle' },
        { name: 'vehicleImage' },
        { name: 'current car' },
        { name: 'ownVehicle' },
        { name: 'currentVehicleBrand' },
        { name: 'currentVehicleModel' },
        { name: 'contact information' },
        { name: 'paymentType' },
        { name: 'contactTime' },
        { name: 'dealer' },
        { name: 'city' },
        { name: 'name' },
        phoneField('AE'),
        { name: 'submit' },
      ],
    },
    fill: [
      ['vehicle', '12'],
      ['ownVehicle', 'yes'],
      ['currentVehicleBrand', '1'],
      ['currentVehicleModel', '102'],
      ['paymentType', 'Installments'],
      ['contactTime', 'Morning'],
      ['dealer', 'branch-2'],
      ['city', '32'],
      ['name', 'Test Person'],
      ['phone', '501234567'],
    ],
  },
  {
    id: 'coverage vehicle quotation named payload',
    component: 'vehicle-quotation-form',
    structure: {
      data: {
        requestUrl: `${SUBMIT}/vehicle-quotation`,
        recaptchaKey: 'test-site-key',
        brandId: 'brand-a',
        quotationType: 'UsedVehiclePurchase',
        nameContactedVehicles: true,
        pushAnalyticsEventTo: 'analyticsEvents',
        extraPayload: { campaign: 'spring' },
        vehicleApi: `${API}/vehicles-strapi`,
        vehiclesApiStrapiFormat: true,
        dealerApi: `${API}/branches`,
        currentVehiclesApi: `${API}/brands`,
        localization: { en: { Yes: 'Yes', No: 'No', Other: 'Other' }, ar: { Yes: 'نعم', No: 'لا', Other: 'أخرى' } },
      },
      requiredContext: { ownVehicle: true },
      tag: 'div',
      children: [
        { name: 'vehicle' },
        { name: 'ownVehicle' },
        { name: 'currentVehicleBrand' },
        { name: 'currentVehicleModel' },
        { name: 'dealer' },
        { name: 'name' },
        phoneField('AE'),
        { name: 'submit' },
      ],
    },
    fill: [
      ['vehicle', '21'],
      ['ownVehicle', 'yes'],
      ['currentVehicleBrand', '2'],
      ['currentVehicleModel', 'Other'],
      ['dealer', 'branch-1'],
      ['name', 'Test Person'],
      ['phone', '501234567'],
    ],
  },
  {
    id: 'coverage submit options',
    component: 'general-form',
    structure: {
      data: { requestUrl: `${SUBMIT}/general?production=true`, requestMethod: 'PUT', disableUTMLog: true, extraHeader: { 'X-Structure': 'structure-header' } },
      requiredContext: { name: true },
      tag: 'div',
      children: [{ name: 'name' }, { name: 'submit' }],
    },
    hostProps: { isDev: true, extraHeader: { 'X-Host': 'host-header' }, extraPayload: { fromHost: true } },
    fill: [['name', 'Test Person']],
  },
  {
    id: 'coverage mobile submit',
    component: 'general-form',
    structure: {
      data: { isMobileForm: true, requestAppUrl: `${SUBMIT}/app`, requestAppCheckUrl: `${SUBMIT}/app-check` },
      tag: 'div',
      children: [{ name: 'name' }],
    },
    hostProps: { getMobileToken: () => 'Bearer mobile-token' },
    fill: [['name', 'Test Person']],
  },
  {
    id: 'coverage structure from a url',
    component: 'general-form',
    hostProps: { structureUrl: `${API}/structures/callback.json` },
    fill: [
      ['name', 'Test Person'],
      ['phone', '501234567'],
    ],
  },
  {
    id: 'coverage phone entered with its country code',
    component: 'general-form',
    structure: {
      data: { requestUrl: `${SUBMIT}/general` },
      requiredContext: { phone: true },
      tag: 'div',
      children: [
        phoneField(['AE', 'JO']),
        { name: 'altPhone', type: 'phone', countryCode: 'AE', defaultValue: '+971 50 765 4321' },
        { name: 'fixedPhone', type: 'phone', countryCode: 'JO', staticValue: '+962 7 9765 4321' },
        { name: 'submit' },
      ],
    },
    fill: [['phone', '+971 50 123 4567']],
  },
  {
    id: 'coverage no structure',
    component: 'general-form',
    fill: [],
    renderOnly: true,
  },
];

const cases = [...exampleCases, ...prototypeCases, ...productionShapeCases, ...coverageCases];

describe('v1 forms freeze', () => {
  for (const testCase of cases) {
    const steps = ((testCase.structure as { steps?: unknown[] } | undefined)?.steps?.length ?? 0) || 1;
    const analyticsHolder = (testCase.structure?.data as { pushAnalyticsEventTo?: string } | undefined)?.pushAnalyticsEventTo;

    for (const language of LANGUAGES) {
      it(`${testCase.id} (${language}) renders and submits as before`, async () => {
        const page = await mount(testCase, language);

        expect(dom(page)).toMatchSnapshot('dom');

        if (testCase.renderOnly) return expect(unexpected).toEqual([]);

        nameTextareas(page);
        const unselectable = await fill(page, testCase.fill, language);
        nameTextareas(page);

        if (testCase.domAfterFill) expect(dom(page)).toMatchSnapshot('dom after fill');

        const form = await (page.root as unknown as { getForm: () => Promise<{ getValues: () => unknown }> }).getForm();
        expect({ values: form.getValues(), unselectable }).toMatchSnapshot('values');

        expect(await submitUntilSent(page, steps)).toMatchSnapshot('submitted');
        if (analyticsHolder) expect((page.win as unknown as Record<string, unknown>)[analyticsHolder]).toMatchSnapshot('analytics');
        expect(unexpected).toEqual([]);
      });
    }
  }
});
