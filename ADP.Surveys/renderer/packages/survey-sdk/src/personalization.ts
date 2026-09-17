/**
 * Renderer-side personalization — the mirror of
 * `ADP.Surveys.Shared/Personalization/PersonalizationTokens.cs`.
 *
 * The server fills `{{recipient.*}}` / `{{candidate.*}}` when it serves a schema and
 * leaves `{{answers.<questionId>}}` (and `{{answers.<questionId>.label}}`) untouched,
 * because only the browser knows what the respondent answered. This module fills
 * those at answer time: in the copy of the screen being shown (title, description,
 * question titles, help, placeholders, option labels…) and in the request fields of
 * an `optionsSource` just before it is fetched, so one question's endpoint can depend
 * on an earlier answer.
 *
 * Same grammar, same fallback chain (answer → inline `|fallback` → declared
 * `survey.variables[].fallback` → nothing), same per-surface escaping, same
 * "verbatim in copy, empty in a request field" rule for a token nothing can fill.
 * Change one side, change the other.
 */

import type { AnswerMap, LocalizedString, OptionsSource, Screen, Survey } from './schema.js';

/** Group 1 is the token path; group 2 the optional inline fallback (stops at `}`). */
const TOKEN_PATTERN = /\{\{\s*([A-Za-z_][A-Za-z0-9_.\-]*)\s*(?:\|([^}]*))?\}\}/g;

export const ANSWER_TOKEN_PREFIX = 'answers.';
export const ANSWER_LABEL_SUFFIX = '.label';

/**
 * Where a substituted value lands. Decides the escaping and what an unresolvable
 * token becomes: verbatim in `copy` (an author's typo shows itself), empty
 * everywhere else (an endpoint must never be sent raw braces). One-to-one with the
 * C# `TokenSurface` enum.
 */
export type TokenSurface =
  | 'copy'
  | 'url'
  | 'queryValue'
  | 'headerValue'
  | 'jsonBody'
  | 'formBody'
  | 'rawBody';

export function mightContainTokens(text: string | null | undefined): boolean {
  return typeof text === 'string' && text.includes('{{');
}

export function isAnswerToken(name: string | null | undefined): boolean {
  return (
    typeof name === 'string' && name.startsWith(ANSWER_TOKEN_PREFIX) && name.length > ANSWER_TOKEN_PREFIX.length
  );
}

/** `answers.nps` and `answers.nps.label` both yield `nps`; null for any other token. */
export function answerTokenQuestionId(name: string | null | undefined): string | null {
  if (!isAnswerToken(name)) return null;
  let path = (name as string).slice(ANSWER_TOKEN_PREFIX.length);
  if (path.endsWith(ANSWER_LABEL_SUFFIX)) path = path.slice(0, -ANSWER_LABEL_SUFFIX.length);
  return path.length === 0 ? null : path;
}

/** Escapes a resolved value for its surface. Mirrors `PersonalizationTokens.Encode`. */
export function encodeForSurface(value: string, surface: TokenSurface): string {
  switch (surface) {
    case 'url':
    case 'formBody':
      return encodeURIComponent(value);
    case 'jsonBody':
      // JSON string escaping without the surrounding quotes — the author wrote those.
      return JSON.stringify(value).slice(1, -1);
    case 'headerValue':
      return value.replace(/[\r\n]/g, '');
    default:
      return value;
  }
}

/** Which escaping a request body wants, from its media type. Mirrors `BodySurface`. */
export function bodySurface(contentType: string | null | undefined): TokenSurface {
  const mediaType = (contentType ?? '').split(';')[0]!.trim().toLowerCase();
  if (mediaType === 'application/json' || mediaType.endsWith('+json')) return 'jsonBody';
  if (mediaType === 'application/x-www-form-urlencoded') return 'formBody';
  return 'rawBody';
}

/** Value lookup + declared fallback for one token name. Null means "nothing". */
export interface TokenContext {
  value(name: string, locale: string): string | null;
  fallback(name: string, locale: string): string | null;
}

/**
 * Replaces every token in `input`. Resolution per token: the context's value, then
 * the inline `|fallback`, then the context's declared fallback; a token none of those
 * fill stays verbatim in copy and becomes empty in a request field.
 */
export function substituteTokens(
  input: string,
  context: TokenContext,
  locale: string,
  surface: TokenSurface = 'copy',
): string {
  if (!mightContainTokens(input)) return input;
  return input.replace(TOKEN_PATTERN, (match: string, name: string, inline?: string) => {
    const trimmedInline = inline?.trim();
    const resolved =
      context.value(name, locale) ?? (trimmedInline ? trimmedInline : null) ?? context.fallback(name, locale);
    if (resolved === null) return surface === 'copy' ? match : '';
    return encodeForSurface(resolved, surface);
  });
}

/** Every distinct token path written in `text`. */
export function collectTokenNames(text: string | null | undefined): string[] {
  if (!mightContainTokens(text)) return [];
  const names = new Set<string>();
  for (const match of (text as string).matchAll(TOKEN_PATTERN)) names.add(match[1]!);
  return Array.from(names);
}

// ─── Answer context ─────────────────────────────────────────────────────────

/** Option shape the label lookup needs — authored (`label` localized) or fetched (`label` plain). */
export interface LabelledOption {
  id: string;
  label?: LocalizedString | string;
}

export interface AnswerContextOptions {
  schema: Survey;
  answers: AnswerMap;
  /**
   * Options of a sourced question once fetched, so `{{answers.x.label}}` can name
   * what the respondent picked from an endpoint. Undefined when not (yet) known —
   * the token then falls back to the stored value.
   */
  lookupOptions?: (questionId: string) => readonly LabelledOption[] | undefined;
  /** Display words for a yes/no answer's `.label` when the question declares none. */
  yesNoLabels?: { yes: string; no: string };
}

function pickLocale(value: LocalizedString | string | undefined | null, locale: string, defaultLocale?: string): string | null {
  if (value == null) return null;
  if (typeof value === 'string') return value || null;
  const exact = value[locale];
  if (exact) return exact;
  if (defaultLocale && value[defaultLocale]) return value[defaultLocale]!;
  for (const key of Object.keys(value)) if (value[key]) return value[key]!;
  return null;
}

type QuestionLike = Record<string, unknown> & { id?: string; type?: string; options?: LabelledOption[] };

function findQuestion(schema: Survey, questionId: string): QuestionLike | undefined {
  for (const screen of schema.screens) {
    for (const q of (screen.questions as QuestionLike[] | undefined) ?? []) {
      if (q && q.id === questionId) return q;
    }
  }
  return undefined;
}

/** The stored answer as text: what BI sees. Empty answers count as unanswered. */
export function formatAnswerValue(raw: unknown): string | null {
  if (raw === undefined || raw === null) return null;
  if (typeof raw === 'string') return raw.length === 0 ? null : raw;
  if (typeof raw === 'number' || typeof raw === 'boolean') return String(raw);
  if (Array.isArray(raw)) {
    const parts = raw.map(formatAnswerValue).filter((p): p is string => p !== null);
    return parts.length === 0 ? null : parts.join(', ');
  }
  if (typeof raw === 'object') {
    // File answers record { name, size, type }; the name is the sensible text.
    const name = (raw as { name?: unknown }).name;
    if (typeof name === 'string' && name.length > 0) return name;
    return JSON.stringify(raw);
  }
  return String(raw);
}

function optionLabel(
  options: readonly LabelledOption[] | undefined,
  id: string,
  locale: string,
  defaultLocale?: string,
): string | null {
  const option = options?.find((o) => o.id === id);
  return option ? pickLocale(option.label, locale, defaultLocale) : null;
}

/** The answer as the respondent saw it: option labels, Yes/No words, locale-formatted dates. */
export function formatAnswerLabel(
  question: QuestionLike | undefined,
  raw: unknown,
  locale: string,
  options: AnswerContextOptions,
): string | null {
  const value = formatAnswerValue(raw);
  if (value === null || !question) return value;
  const defaultLocale = options.schema.defaultLocale;
  const labelFor = (id: string) =>
    optionLabel(question.options, id, locale, defaultLocale) ??
    optionLabel(question.id ? options.lookupOptions?.(question.id) : undefined, id, locale, defaultLocale) ??
    id;

  switch (question.type) {
    case 'singleChoice':
    case 'dropdown':
    case 'navigationList':
      return labelFor(String(raw));
    case 'multiChoice':
      return Array.isArray(raw) ? raw.map((id) => labelFor(String(id))).join(', ') : labelFor(String(raw));
    case 'yesNo': {
      const yes = raw === true || raw === 'true';
      const declared = pickLocale(
        (question as { yesLabel?: LocalizedString; noLabel?: LocalizedString })[yes ? 'yesLabel' : 'noLabel'],
        locale,
        defaultLocale,
      );
      return declared ?? (yes ? options.yesNoLabels?.yes ?? 'Yes' : options.yesNoLabels?.no ?? 'No');
    }
    case 'date':
    case 'dateTime': {
      const date = new Date(value);
      if (Number.isNaN(date.getTime())) return value;
      try {
        return question.type === 'date'
          ? new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(date)
          : new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(date);
      } catch {
        return value;
      }
    }
    default:
      return value;
  }
}

/**
 * A token context over the respondent's answers. `answers.<id>` is the stored
 * value, `answers.<id>.label` the display form; declared fallbacks come from
 * `schema.variables`. Every other token name resolves to nothing here — the server
 * already had its turn with those.
 */
export function createAnswerContext(options: AnswerContextOptions): TokenContext {
  const { schema, answers } = options;
  return {
    value(name, locale) {
      const questionId = answerTokenQuestionId(name);
      if (questionId === null) return null;
      const raw = answers[questionId];
      if (!name.endsWith(ANSWER_LABEL_SUFFIX)) return formatAnswerValue(raw);
      return formatAnswerLabel(findQuestion(schema, questionId), raw, locale, options);
    },
    fallback(name, locale) {
      const declared = schema.variables?.find((v) => v.name?.trim() === name);
      return pickLocale(declared?.fallback, locale, schema.defaultLocale);
    },
  };
}

// ─── Typed walk over a screen's copy ─────────────────────────────────────────

/**
 * Every LocalizedString-typed property across the schema DTOs, by JSON name. Pinned
 * on the C# side by `PersonalizationAnswerTokenTests.LocalizedStringFields_MatchTheSdkWalkList`
 * — a new localized field fails that test until it is added here.
 */
export const LOCALIZED_KEYS: readonly string[] = [
  'title',
  'description',
  'help',
  'placeholder',
  'lowLabel',
  'highLabel',
  'unit',
  'yesLabel',
  'noLabel',
  'label',
];

function isLocalizedString(value: unknown): value is LocalizedString {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) return false;
  return Object.values(value as Record<string, unknown>).every((v) => typeof v === 'string');
}

/** Substitutes inside each locale's text, resolving for THAT locale. Same object back when nothing changed. */
function substituteLocalized(value: LocalizedString, context: TokenContext): LocalizedString {
  let changed = false;
  const next: LocalizedString = {};
  for (const [locale, text] of Object.entries(value)) {
    const replaced = substituteTokens(text, context, locale, 'copy');
    if (replaced !== text) changed = true;
    next[locale] = replaced;
  }
  return changed ? next : value;
}

/** Copy-on-write rewrite of the localized keys on one object (question, option, screen). */
function substituteCopyFields<T extends Record<string, unknown>>(node: T, context: TokenContext): T {
  let out: T | null = null;
  for (const key of LOCALIZED_KEYS) {
    const value = node[key];
    if (!isLocalizedString(value)) continue;
    const replaced = substituteLocalized(value, context);
    if (replaced === value) continue;
    out ??= { ...node };
    (out as Record<string, unknown>)[key] = replaced;
  }
  return out ?? node;
}

function substituteQuestion(question: Record<string, unknown>, context: TokenContext): Record<string, unknown> {
  let out = substituteCopyFields(question, context);
  const options = question['options'];
  if (Array.isArray(options)) {
    let optionsChanged = false;
    const nextOptions = options.map((option) => {
      if (option === null || typeof option !== 'object') return option;
      const replaced = substituteCopyFields(option as Record<string, unknown>, context);
      if (replaced !== option) optionsChanged = true;
      return replaced;
    });
    if (optionsChanged) out = { ...out, options: nextOptions };
  }
  return out;
}

/**
 * The screen with every answer token in its copy filled from `context` — title,
 * description, each question's localized fields and each option's label. Returns
 * the very same object when nothing changed, so memoised consumers stay stable.
 * Request fields (`optionsSource`) are deliberately left alone: those are
 * substituted at fetch time by `substituteRequestFields`, with request escaping.
 */
export function personalizeScreen(screen: Screen, context: TokenContext): Screen {
  let out = substituteCopyFields(screen as unknown as Record<string, unknown>, context) as unknown as Screen;
  const questions = screen.questions;
  if (Array.isArray(questions)) {
    let changed = false;
    const next = questions.map((q) => {
      if (q === null || typeof q !== 'object') return q;
      const replaced = substituteQuestion(q as Record<string, unknown>, context);
      if (replaced !== q) changed = true;
      return replaced;
    });
    if (changed) out = { ...out, questions: next };
  }
  return out;
}

/**
 * An options source with every token in its request fields filled: the URL
 * (percent-encoded), query-parameter values (raw — `buildOptionsUrl` encodes them),
 * header values (CR/LF stripped) and the body (escaped for its content type).
 * Returns the same object when nothing changed.
 */
export function substituteRequestFields(source: OptionsSource, context: TokenContext, locale: string): OptionsSource {
  let out: OptionsSource | null = null;
  const set = <K extends keyof OptionsSource>(key: K, value: OptionsSource[K]) => {
    out ??= { ...source };
    out[key] = value;
  };

  const url = substituteTokens(source.url, context, locale, 'url');
  if (url !== source.url) set('url', url);

  if (source.body != null) {
    const body = substituteTokens(source.body, context, locale, bodySurface(effectiveContentType(source)));
    if (body !== source.body) set('body', body);
  }

  const queryParams = substituteMapValues(source.queryParams, context, locale, 'queryValue');
  if (queryParams !== source.queryParams) set('queryParams', queryParams);

  const headers = substituteMapValues(source.headers, context, locale, 'headerValue');
  if (headers !== source.headers) set('headers', headers);

  return out ?? source;
}

/** The media type a body is sent as — explicit `contentType`, else JSON when there is a body. */
export function effectiveContentType(source: OptionsSource): string | undefined {
  const explicit = source.contentType?.trim();
  if (explicit) return explicit;
  return source.body != null ? 'application/json' : undefined;
}

function substituteMapValues(
  map: Record<string, string> | undefined,
  context: TokenContext,
  locale: string,
  surface: TokenSurface,
): Record<string, string> | undefined {
  if (!map) return map;
  let out: Record<string, string> | null = null;
  for (const [key, value] of Object.entries(map)) {
    const replaced = substituteTokens(value, context, locale, surface);
    if (replaced === value) continue;
    out ??= { ...map };
    out[key] = replaced;
  }
  return out ?? map;
}
