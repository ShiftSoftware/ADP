/**
 * Client-side fetch + mapping for `optionsSource` questions. The renderer calls
 * `fetchOptions` when a sourced question's screen becomes current — only the
 * branch the respondent actually visits gets fetched, which is the point of
 * doing this client-side (a survey may use several query-param variations of
 * one endpoint across its branches).
 *
 * `Accept-Language` is sent from the active locale so endpoints that localize
 * server-side (e.g. the TIQ public APIs) return pre-localized labels; an
 * explicit `source.headers` entry wins over the automatic one.
 */

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

/** Fetches and maps a source's options. Throws on HTTP errors, non-JSON bodies,
 *  and non-array shapes — callers render an inline retry affordance. */
export async function fetchOptions(
  source: OptionsSource,
  init?: FetchOptionsInit,
): Promise<FetchedOption[]> {
  const fetchImpl = init?.fetchImpl ?? fetch;
  const headers: Record<string, string> = {};
  if (init?.locale) headers['Accept-Language'] = init.locale;
  Object.assign(headers, source.headers ?? {});
  const response = await fetchImpl(buildOptionsUrl(source), {
    headers,
    ...(init?.signal ? { signal: init.signal } : {}),
  });
  if (!response.ok) throw new Error(`optionsSource fetch failed: HTTP ${response.status}.`);
  return mapOptionsResponse(await response.json(), source);
}
