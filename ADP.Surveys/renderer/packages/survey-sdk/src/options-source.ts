/**
 * Client-side fetch + mapping for `optionsSource` questions. The renderer calls
 * `fetchOptions` when a sourced question's screen becomes current — only the
 * branch the respondent actually visits gets fetched, which is the point of
 * doing this client-side (a survey may use several query-param variations of
 * one endpoint across its branches).
 *
 * `Accept-Language` is sent from the active locale so endpoints that localize
 * server-side (as a deployment's own public reference APIs typically do) return
 * pre-localized labels; an explicit `source.headers` entry wins over the automatic one.
 */

import { effectiveContentType } from './personalization.js';
import type { OptionsSource } from './schema.js';

/** One fetched option, mapped via the source's value/label dot-paths. */
export interface FetchedOption {
  id: string;
  label: string;
}

/** Dot-path getter (`a.b.c`). Returns undefined on any miss — never throws. */
function getPath(value: unknown, path: string): unknown {
  let current = value;
  for (const part of path.split('.')) {
    if (current === null || typeof current !== 'object') return undefined;
    current = (current as Record<string, unknown>)[part];
  }
  return current;
}

/** The source URL with `queryParams` applied (overwriting duplicates already
 *  present in the URL, so overrides behave predictably). */
export function buildOptionsUrl(source: OptionsSource): string {
  const url = new URL(source.url);
  for (const [key, value] of Object.entries(source.queryParams ?? {})) {
    url.searchParams.set(key, value);
  }
  return url.toString();
}

/**
 * Maps a response body onto `FetchedOption`s using the source's paths
 * (`itemsPath` → array, then `valuePath` / `labelPath` per item; defaults
 * `ID` / `Name` — the common ShiftEntity public-endpoint shape). Items without
 * a value are skipped; items without a label fall back to the value. Throws
 * when the body (or `itemsPath`) doesn't yield an array.
 */
export function mapOptionsResponse(body: unknown, source: OptionsSource): FetchedOption[] {
  const items = source.itemsPath ? getPath(body, source.itemsPath) : body;
  if (!Array.isArray(items)) {
    throw new Error(
      `optionsSource response is not an array${source.itemsPath ? ` at '${source.itemsPath}'` : ''}.`,
    );
  }
  const valuePath = source.valuePath || 'ID';
  const labelPath = source.labelPath || 'Name';
  const options: FetchedOption[] = [];
  for (const item of items) {
    const value = getPath(item, valuePath);
    if (value === undefined || value === null || value === '') continue;
    const label = getPath(item, labelPath);
    options.push({
      id: String(value),
      label: label === undefined || label === null || label === '' ? String(value) : String(label),
    });
  }
  return options;
}

export interface FetchOptionsInit {
  /** Active renderer locale — sent as `Accept-Language`. */
  locale?: string;
  /** Injection point for tests / non-browser hosts. Defaults to global fetch. */
  fetchImpl?: typeof fetch;
  signal?: AbortSignal;
}

/** `POST` when the source says so (any case); everything else is a GET. */
function requestMethod(source: OptionsSource): 'GET' | 'POST' {
  return source.method?.trim().toUpperCase() === 'POST' ? 'POST' : 'GET';
}

/** The headers a fetch will send: `Accept-Language` from the locale, the body's
 *  content type, then the source's own headers, which win over both. */
function requestHeaders(source: OptionsSource, locale?: string): Record<string, string> {
  const headers: Record<string, string> = {};
  if (locale) headers['Accept-Language'] = locale;
  const contentType = requestMethod(source) === 'POST' && source.body != null ? effectiveContentType(source) : undefined;
  if (contentType) headers['Content-Type'] = contentType;
  for (const [key, value] of Object.entries(source.headers ?? {})) {
    // A source header replaces the automatic one whatever its casing.
    for (const existing of Object.keys(headers)) {
      if (existing.toLowerCase() === key.toLowerCase()) delete headers[existing];
    }
    headers[key] = value;
  }
  return headers;
}

/**
 * Everything that decides what a fetch sends — method, resolved URL, headers and
 * body — as one string. Cache keys are built from it so two sources that differ
 * only in a substituted header or body (an answer token, say) do not share a
 * cached response.
 */
export function requestSignature(source: OptionsSource, locale?: string): string {
  const headers = requestHeaders(source, locale);
  const headerList = Object.keys(headers)
    .sort()
    .map((k) => `${k}=${headers[k]}`)
    .join('\n');
  const body = requestMethod(source) === 'POST' && source.body != null ? source.body : '';
  return `${requestMethod(source)} ${buildOptionsUrl(source)}\n${headerList}\n${body}`;
}

/** Fetches and maps a source's options. Throws on HTTP errors, non-JSON bodies,
 *  and non-array shapes — callers render an inline retry affordance. Tokens in the
 *  request fields are NOT substituted here — pass a source that already went
 *  through `substituteRequestFields`. */
export async function fetchOptions(
  source: OptionsSource,
  init?: FetchOptionsInit,
): Promise<FetchedOption[]> {
  const fetchImpl = init?.fetchImpl ?? fetch;
  const method = requestMethod(source);
  const response = await fetchImpl(buildOptionsUrl(source), {
    method,
    headers: requestHeaders(source, init?.locale),
    ...(method === 'POST' && source.body != null ? { body: source.body } : {}),
    ...(init?.signal ? { signal: init.signal } : {}),
  });
  if (!response.ok) throw new Error(`optionsSource fetch failed: HTTP ${response.status}.`);
  return mapOptionsResponse(await response.json(), source);
}
