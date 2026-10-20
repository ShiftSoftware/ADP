import { SubmitTrace, startSubmitTrace, takeSubmitTrace } from './adp-fetch';
import { hostFlag } from './host-config';

export type AdpFormErrorCode = 'http' | 'network' | 'upload' | 'config' | 'unknown';

export type AdpStepLabels = { title: string; stepTitle?: string; back?: string; submitButton?: string };

export type AdpFormValidity = { valid: boolean; step: number; invalidFields: string[] };

export interface AdpFormEventDetails {
  'adp-form-ready': { tag: string };
  'adp-form-loading-change': { loading: boolean };
  'adp-form-step-change': { step: number; previousStep: number; stepCount: number; labels: AdpStepLabels };
  'adp-form-validity-change': AdpFormValidity;
  'adp-form-submitted': { data: unknown; payload: Record<string, unknown> };
  'adp-form-error': { message: string; status?: number; code?: AdpFormErrorCode };
  'adp-form-size-change': { width: number; height: number };
}

export type AdpFormEventType = keyof AdpFormEventDetails;

type StepSource = {
  formStructure?: { currentStep?: number };
  context?: { el?: HTMLElement; structure?: { steps?: unknown[] } };
  getFormLocale?: () => [unknown, string];
  getStepLabels?: (language: string, step?: number) => Record<string, unknown> | undefined;
};

// The endpoint check in functions.ts throws this exact message before any request is made.
const MISSING_ENDPOINT = 'Request endpoint is not configured';

const isPlainObject = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value);

export function toJson(value: unknown, fallback: unknown = {}): unknown {
  if (value === undefined) return fallback;

  try {
    return JSON.parse(JSON.stringify(value));
  } catch {
    return fallback;
  }
}

export function dispatchFormEvent<K extends AdpFormEventType>(el: EventTarget | undefined, type: K, detail: AdpFormEventDetails[K]) {
  if (!el || typeof el.dispatchEvent !== 'function') return;

  el.dispatchEvent(new CustomEvent(type, { detail, bubbles: true, composed: true }));
}

const NOOP = () => {};

const formEvents = () => hostFlag('formEvents');

const elementOf = (context: object | undefined) => (context as { el?: HTMLElement } | undefined)?.el;

export function emitFormReady(el: HTMLElement | undefined) {
  if (el && formEvents()) dispatchFormEvent(el, 'adp-form-ready', { tag: el.tagName.toLowerCase() });
}

const loadingStates = new WeakMap<object, boolean>();

export function emitFormLoading(context: object | undefined, loading: boolean) {
  if (!formEvents()) return;
  const el = elementOf(context);
  if (!el) return;

  const next = !!loading;
  if (next) startSubmitTrace(el);
  if ((loadingStates.get(el) ?? false) === next) return;

  loadingStates.set(el, next);
  dispatchFormEvent(el, 'adp-form-loading-change', { loading: next });
}

const steps = new WeakMap<object, number>();

function stepLabels(form: StepSource, step: number): AdpStepLabels {
  let source: Record<string, unknown> | undefined;
  try {
    source = form.getStepLabels?.(form.getFormLocale?.()?.[1] ?? 'en', step);
  } catch {
    source = undefined;
  }

  const labels: AdpStepLabels = { title: typeof source?.title === 'string' ? source.title : '' };
  for (const key of ['stepTitle', 'back', 'submitButton'] as const) if (typeof source?.[key] === 'string') labels[key] = source[key] as string;

  return labels;
}

export function emitFormStep(form: StepSource | undefined) {
  if (!formEvents()) return;
  const el = form?.context?.el;
  const step = form?.formStructure?.currentStep;
  if (!el || typeof step !== 'number') return;

  const previousStep = steps.get(form) ?? 1;
  if (previousStep === step) return;

  steps.set(form, step);
  const stepCount = Array.isArray(form.context.structure?.steps) ? form.context.structure.steps.length : 0;
  dispatchFormEvent(el, 'adp-form-step-change', { step, previousStep, stepCount, labels: stepLabels(form, step) });
}

function sentPayload(trace: SubmitTrace | undefined): Record<string, unknown> {
  const body = trace?.body;
  if (typeof body !== 'string') return {};

  try {
    const parsed = JSON.parse(body);
    return isPlainObject(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

// Called as a handler starts, before its awaited callback, so a quick resubmit cannot swap this attempt's trace for its own.
export function formSubmittedEmitter(context: object | undefined): (data: unknown) => void {
  if (!formEvents()) return NOOP;
  const el = elementOf(context);
  if (!el) return NOOP;

  const payload = sentPayload(takeSubmitTrace(el));

  return data => dispatchFormEvent(el, 'adp-form-submitted', { data: toJson(data), payload });
}

export function formErrorCode(trace: SubmitTrace | undefined, error: unknown): AdpFormErrorCode {
  if (trace?.status !== undefined) return 'http';

  const rejectedBy = trace?.rejections.get(error);
  if (rejectedBy === 'submit') return 'network';
  if (rejectedBy === 'upload') return 'upload';

  if (error instanceof Error && error.message === MISSING_ENDPOINT) return 'config';
  if (trace && !trace.sent && (trace.uploadFailed || (error as { fromServer?: unknown })?.fromServer === true)) return 'upload';

  return 'unknown';
}

export function formErrorEmitter(context: object | undefined, error: unknown): (message: unknown) => void {
  if (!formEvents()) return NOOP;
  const el = elementOf(context);
  if (!el) return NOOP;

  const trace = takeSubmitTrace(el);
  const code = formErrorCode(trace, error);
  const status = trace?.status;

  return message => {
    const detail: AdpFormEventDetails['adp-form-error'] = { message: typeof message === 'string' ? message : '' };
    if (status !== undefined) detail.status = status;
    detail.code = code;

    dispatchFormEvent(el, 'adp-form-error', detail);
  };
}
