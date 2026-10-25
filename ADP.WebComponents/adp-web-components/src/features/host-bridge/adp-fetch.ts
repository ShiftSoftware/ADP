import { HostConfig, peekHostConfig } from './host-config';

export type AdpRequestKind = 'submit' | 'options' | 'calendar' | 'upload' | 'lookup';

export interface AdpRequestInfo {
  kind: AdpRequestKind;
  tag: string;
  element?: HTMLElement;
  field?: string;
}

export type AdpFetch = (url: string, init: RequestInit, info: AdpRequestInfo) => Promise<Response>;

export type SubmitTrace = {
  sent: boolean;
  body?: unknown;
  status?: number;
  uploadFailed: boolean;
  rejections: Map<unknown, AdpRequestKind>;
};

const traces = new WeakMap<object, SubmitTrace>();

const newTrace = (): SubmitTrace => ({ sent: false, uploadFailed: false, rejections: new Map() });

export function hostFetch(config: HostConfig | undefined = peekHostConfig()): AdpFetch | undefined {
  const candidate = config?.fetch;

  return typeof candidate === 'function' ? (candidate as AdpFetch) : undefined;
}

export function requestInfo(kind: AdpRequestKind, element: HTMLElement | undefined, field?: string): AdpRequestInfo {
  const info: AdpRequestInfo = { kind, tag: element?.tagName?.toLowerCase() ?? '', element };
  if (typeof field === 'string' && field) info.field = field;

  return info;
}

export function formRequestInfo(kind: AdpRequestKind, form: { context?: { el?: HTMLElement } } | undefined, field?: string): AdpRequestInfo {
  return requestInfo(kind, form?.context?.el, field);
}

export function optionsRequestInfo(select: { form?: { context?: { el?: HTMLElement } }; name?: string } | undefined): AdpRequestInfo {
  return formRequestInfo('options', select?.form, select?.name);
}

export function startSubmitTrace(element: object | undefined) {
  if (element) traces.set(element, newTrace());
}

export function submitTrace(element: object | undefined): SubmitTrace | undefined {
  return element ? traces.get(element) : undefined;
}

export function takeSubmitTrace(element: object | undefined): SubmitTrace | undefined {
  const trace = submitTrace(element);
  if (trace) traces.delete(element);

  return trace;
}

// Observes beside the caller instead of chaining, so the caller's await settles on the same tick as an untraced request.
function observe(request: Promise<Response>, init: RequestInit, info: AdpRequestInfo) {
  if (!info.element) return;

  if (!traces.has(info.element)) traces.set(info.element, newTrace());
  const trace = traces.get(info.element);

  if (info.kind === 'submit') {
    trace.sent = true;
    trace.body = init?.body;
  }

  request.then(
    response => {
      if (response?.ok) return;
      if (info.kind === 'upload') trace.uploadFailed = true;
      else if (typeof response?.status === 'number') trace.status = response.status;
    },
    reason => {
      trace.rejections.set(reason, info.kind);
      if (info.kind === 'upload') trace.uploadFailed = true;
    },
  );
}

// Info comes first so each call site keeps its init object as the last argument and its old one-line shape.
export function adpFetch(info: AdpRequestInfo, url: string, init: RequestInit): Promise<Response> {
  const config = peekHostConfig();
  if (!config) return fetch(url, init);

  const override = hostFetch(config);

  let request: Promise<Response>;
  if (override) {
    try {
      request = Promise.resolve(override(url, init, info));
    } catch (error) {
      request = Promise.reject(error);
    }
  } else request = fetch(url, init);

  if (config.formEvents === true && (info.kind === 'submit' || info.kind === 'upload')) observe(request, init, info);

  return request;
}
