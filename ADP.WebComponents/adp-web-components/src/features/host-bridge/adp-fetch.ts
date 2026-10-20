import { peekHostConfig } from './host-config';

export type AdpRequestKind = 'submit' | 'options' | 'calendar' | 'upload' | 'lookup';

export interface AdpRequestInfo {
  kind: AdpRequestKind;
  tag: string;
  element: HTMLElement;
  field?: string;
}

export type AdpFetch = (url: string, init: RequestInit, info: AdpRequestInfo) => Promise<Response>;

type SubmitTrace = {
  sent: boolean;
  body?: unknown;
  uploadFailed: boolean;
  rejections: Map<unknown, AdpRequestKind>;
};

const traces = new WeakMap<object, SubmitTrace>();

const newTrace = (): SubmitTrace => ({ sent: false, uploadFailed: false, rejections: new Map() });

export function hostFetch(): AdpFetch | undefined {
  const candidate = peekHostConfig()?.fetch;

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

export function releaseSubmitTrace(element: object | undefined) {
  if (element) traces.delete(element);
}

function traced(request: Promise<Response>, init: RequestInit, info: AdpRequestInfo): Promise<Response> {
  if (!info.element) return request;

  let trace = traces.get(info.element);
  if (!trace) {
    trace = newTrace();
    traces.set(info.element, trace);
  }

  if (info.kind === 'submit') {
    trace.sent = true;
    trace.body = init?.body;
  }

  return request.then(
    response => {
      if (info.kind === 'upload' && !response?.ok) trace.uploadFailed = true;
      return response;
    },
    reason => {
      trace.rejections.set(reason, info.kind);
      if (info.kind === 'upload') trace.uploadFailed = true;
      throw reason;
    },
  );
}

export function adpFetch(url: string, init: RequestInit, info: AdpRequestInfo): Promise<Response> {
  const override = hostFetch();

  let request: Promise<Response>;
  if (override) {
    try {
      request = Promise.resolve(override(url, init, info));
    } catch (error) {
      request = Promise.reject(error);
    }
  } else request = fetch(url, init);

  return info.kind === 'submit' || info.kind === 'upload' ? traced(request, init, info) : request;
}
