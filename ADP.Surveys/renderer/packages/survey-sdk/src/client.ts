/**
 * HTTP client for the anonymous public-survey endpoints exposed by
 * `ADP.Surveys.API/Controllers/PublicSurveyController.cs`:
 *
 *   GET  {baseUrl}/SurveyInstances/{publicId}/schema    → resolved SurveyDto
 *   GET  {baseUrl}/SurveyInstances/{publicId}/status    → instance status blob
 *   POST {baseUrl}/SurveyInstances/{publicId}/responses → accepts answer map + meta
 *
 * The controller returns bare `{ Message, Errors? }` JSON on error (it does NOT use
 * the ShiftEntity envelope — that only wraps authenticated admin responses), so the
 * error mapping here is deliberately simple.
 */

import type { AnswerMap, Survey } from './schema.js';

export interface SurveyClientOptions {
  /** Base URL including the Surveys route prefix. E.g. `http://localhost:5134/api/Surveys`.
   *  A trailing slash is tolerated and stripped. */
  baseUrl: string;
  /** Optional injected fetch — used by tests and for non-browser hosts that bring
   *  their own fetch. Defaults to `globalThis.fetch`. */
  fetch?: typeof fetch;
}

export interface SubmissionMeta {
  startedAt?: string;
  completedAt?: string;
  agentId?: string;
  userAgent?: string;
  [key: string]: unknown;
}

export interface SurveySubmission {
  schemaVersion: number;
  answers: AnswerMap;
  meta?: SubmissionMeta;
}

export interface SurveyStatus {
  status: 'Pending' | 'Sent' | 'Opened' | 'Completed' | 'Expired' | string;
  schemaVersion: number;
  triggeredAt?: string;
}

export type SurveyClientErrorCode =
  | 'network'
  | 'notFound'
  | 'gone'
  | 'conflict'
  | 'badRequest'
  | 'server'
  | 'parse';

export class SurveyClientError extends Error {
  readonly status: number;
  readonly code: SurveyClientErrorCode;
  readonly serverMessage?: string;
  readonly validationErrors?: Array<{ questionId: string; message: string }>;
  readonly raw?: unknown;

  constructor(init: {
    status: number;
    code: SurveyClientErrorCode;
    message: string;
    serverMessage?: string;
    validationErrors?: Array<{ questionId: string; message: string }>;
    raw?: unknown;
  }) {
    super(init.message);
    this.name = 'SurveyClientError';
    this.status = init.status;
    this.code = init.code;
    this.serverMessage = init.serverMessage;
    this.validationErrors = init.validationErrors;
    this.raw = init.raw;
  }
}

export class SurveyClient {
  private readonly baseUrl: string;
  private readonly fetchFn: typeof fetch;

  constructor(options: SurveyClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/+$/, '');
    const f = options.fetch ?? globalThis.fetch;
    if (!f) throw new Error('SurveyClient: no fetch available. Provide options.fetch or run in an environment with a global fetch.');
    // Preserve fetch's `this` binding (important in browsers — globalThis.fetch
    // must be called with `window` as receiver on some engines).
    this.fetchFn = f.bind(globalThis);
  }

  async fetchSchema(publicId: string): Promise<Survey> {
    const response = await this.send('GET', `/SurveyInstances/${encodeURIComponent(publicId)}/schema`);
    return this.readJson<Survey>(response);
  }

  async getStatus(publicId: string): Promise<SurveyStatus> {
    const response = await this.send('GET', `/SurveyInstances/${encodeURIComponent(publicId)}/status`);
    const body = await this.readJson<Record<string, unknown>>(response);
    return {
      status: String(body['Status'] ?? body['status'] ?? 'Pending'),
      schemaVersion: Number(body['SchemaVersion'] ?? body['schemaVersion'] ?? 0),
      triggeredAt: (body['TriggeredAt'] ?? body['triggeredAt']) as string | undefined,
    };
  }

  async submitResponse(publicId: string, submission: SurveySubmission): Promise<void> {
    await this.send(
      'POST',
      `/SurveyInstances/${encodeURIComponent(publicId)}/responses`,
      submission,
    );
  }

  private async send(method: string, path: string, body?: unknown): Promise<Response> {
    let response: Response;
    try {
      response = await this.fetchFn(`${this.baseUrl}${path}`, {
        method,
        headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
        body: body === undefined ? undefined : JSON.stringify(body),
      });
    } catch (e) {
      throw new SurveyClientError({
        status: 0,
        code: 'network',
        message: `Network error calling ${method} ${path}: ${(e as Error).message ?? e}`,
      });
    }
    if (!response.ok) throw await this.toError(response, method, path);
    return response;
  }

  private async readJson<T>(response: Response): Promise<T> {
    const text = await response.text();
    if (!text) {
      throw new SurveyClientError({
        status: response.status,
        code: 'parse',
        message: `Empty body from ${response.url}`,
      });
    }
    try {
      return JSON.parse(text) as T;
    } catch (e) {
      throw new SurveyClientError({
        status: response.status,
        code: 'parse',
        message: `Could not parse JSON from ${response.url}: ${(e as Error).message}`,
        raw: text,
      });
    }
  }

  private async toError(response: Response, method: string, path: string): Promise<SurveyClientError> {
    const code: SurveyClientErrorCode =
      response.status === 404
        ? 'notFound'
        : response.status === 410
          ? 'gone'
          : response.status === 409
            ? 'conflict'
            : response.status === 400
              ? 'badRequest'
              : response.status >= 500
                ? 'server'
                : 'server';

    const text = await response.text();
    if (!text) {
      return new SurveyClientError({
        status: response.status,
        code,
        message: `${method} ${path} → ${response.status}`,
      });
    }
    let parsed: Record<string, unknown> | undefined;
    try {
      parsed = JSON.parse(text) as Record<string, unknown>;
    } catch {
      return new SurveyClientError({
        status: response.status,
        code,
        message: `${method} ${path} → ${response.status}: ${text.slice(0, 200)}`,
        raw: text,
      });
    }
    const serverMessage = (parsed['Message'] ?? parsed['message']) as string | undefined;
    const rawErrors = (parsed['Errors'] ?? parsed['errors']) as unknown;
    const validationErrors = Array.isArray(rawErrors)
      ? (rawErrors as Array<Record<string, unknown>>).flatMap((e) => {
          const qid = (e['QuestionId'] ?? e['questionId']) as string | undefined;
          const msg = (e['Message'] ?? e['message']) as string | undefined;
          return qid && msg ? [{ questionId: qid, message: msg }] : [];
        })
      : undefined;
    return new SurveyClientError({
      status: response.status,
      code,
      message: `${method} ${path} → ${response.status}${serverMessage ? ': ' + serverMessage : ''}`,
      serverMessage,
      validationErrors: validationErrors && validationErrors.length > 0 ? validationErrors : undefined,
      raw: parsed,
    });
  }
}
