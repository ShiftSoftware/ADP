import { peekHostConfig } from '../host-bridge/host-config';
import type { LanguageCode, LanguageKeys, LocaleKeyEntries } from './types';

export type LanguagePackNamespace =
  | 'global'
  | 'errors'
  | 'forms'
  | 'general'
  | 'input'
  | 'calendar'
  | 'booking'
  | 'popover'
  | 'vehicleLookup.ssc'
  | 'vehicleLookup.specification'
  | 'vehicleLookup.warrantyTimeline'
  | 'vehicleLookup.serviceHistory'
  | 'vehicleLookup.saleInformation'
  | 'vehicleLookup.accessories'
  | 'vehicleLookup.paintThickness'
  | 'vehicleLookup.claimableItems';

export interface LanguageMeta {
  direction?: 'ltr' | 'rtl';
  weekStartsOn?: 0 | 1 | 2 | 3 | 4 | 5 | 6;
  numerals?: 'latn' | 'arab';
}

export interface LanguagePacks {
  version?: 1;
  languages?: Record<string, LanguageMeta>;
  strings?: Partial<Record<LanguagePackNamespace, Record<string, Record<string, unknown>>>>;
}

export type LanguagePacksInput = LanguagePacks | LanguagePacks[] | string;

export interface ResolvedLanguageMeta {
  direction: 'ltr' | 'rtl';
  weekStartsOn: 0 | 1 | 2 | 3 | 4 | 5 | 6;
  numerals: 'latn' | 'arab';
}

export const NAMESPACE_MAPPER_KEYS: Record<LanguagePackNamespace, LocaleKeyEntries> = {
  'global': '-',
  'errors': 'errors',
  'forms': 'forms*',
  'general': 'general',
  'input': 'input',
  'calendar': 'calendar',
  'booking': 'booking',
  'popover': 'popover',
  'vehicleLookup.ssc': 'vehicleLookup.ssc',
  'vehicleLookup.specification': 'vehicleLookup.specification',
  'vehicleLookup.warrantyTimeline': 'vehicleLookup.warrantyTimeline',
  'vehicleLookup.serviceHistory': 'vehicleLookup.serviceHistory',
  'vehicleLookup.saleInformation': 'vehicleLookup.saleInformation',
  'vehicleLookup.accessories': 'vehicleLookup.accessories',
  'vehicleLookup.paintThickness': 'vehicleLookup.paintThickness',
  'vehicleLookup.claimableItems': 'vehicleLookup.claimableItems',
};

const NAMESPACES = Object.keys(NAMESPACE_MAPPER_KEYS) as LanguagePackNamespace[];

const NAMESPACE_BY_MAPPER_KEY = new Map<string, LanguagePackNamespace>(NAMESPACES.map(namespace => [NAMESPACE_MAPPER_KEYS[namespace], namespace]));

export const BUILT_IN_LANGUAGES: readonly LanguageKeys[] = ['en', 'ar', 'ku', 'ru'];

const BUILT_IN_META: Record<LanguageKeys, ResolvedLanguageMeta> = {
  en: { direction: 'ltr', weekStartsOn: 1, numerals: 'latn' },
  ar: { direction: 'rtl', weekStartsOn: 6, numerals: 'arab' },
  ku: { direction: 'rtl', weekStartsOn: 6, numerals: 'arab' },
  ru: { direction: 'ltr', weekStartsOn: 1, numerals: 'latn' },
};

interface Snapshot {
  source: unknown;
  packs: LanguagePacks[];
  known: Set<string>;
  values: Map<string, unknown>;
}

type Strings = Record<string, unknown>;

const isPlainObject = (value: unknown): value is Strings => typeof value === 'object' && value !== null && !Array.isArray(value);

export const isBuiltInLanguage = (language: unknown): language is LanguageKeys => BUILT_IN_LANGUAGES.includes(language as LanguageKeys);

const warned = new Set<string>();

function warnOnce(message: string) {
  if (warned.has(message)) return;
  warned.add(message);
  console.warn(`[adp-web-components] ${message}`);
}

function parsePacks(input: unknown): LanguagePacks[] {
  if (typeof input === 'string') {
    try {
      input = JSON.parse(input);
    } catch {
      warnOnce('languagePacks is not valid JSON and was ignored.');
      return [];
    }
  }

  if (Array.isArray(input)) return input.flatMap(parsePacks);
  if (isPlainObject(input)) return [input as LanguagePacks];

  warnOnce('A language pack that is not an object was ignored.');
  return [];
}

function languagesOf(packs: LanguagePacks[]): Set<string> {
  const known = new Set<string>(BUILT_IN_LANGUAGES);

  packs.forEach(pack => {
    if (isPlainObject(pack.languages)) Object.keys(pack.languages).forEach(code => known.add(code));

    if (!isPlainObject(pack.strings)) return;

    Object.entries(pack.strings).forEach(([namespace, byLanguage]) => {
      if (!NAMESPACES.includes(namespace as LanguagePackNamespace)) return warnOnce(`Unknown language pack namespace "${namespace}" was ignored.`);
      if (isPlainObject(byLanguage)) Object.keys(byLanguage).forEach(code => known.add(code));
    });
  });

  return known;
}

let snapshot: Snapshot | undefined;

function current(): Snapshot | undefined {
  const source = peekHostConfig()?.languagePacks;
  if (source === undefined || source === null || source === '') return undefined;
  if (snapshot?.source === source) return snapshot;

  const packs = parsePacks(source);
  snapshot = { source, packs, known: languagesOf(packs), values: new Map() };

  return snapshot;
}

export const packsRegistered = (): boolean => !!current()?.packs.length;

export function knownLanguages(): LanguageCode[] {
  return [...(current()?.known ?? BUILT_IN_LANGUAGES)];
}

export const isKnownLanguage = (language: unknown): boolean => isBuiltInLanguage(language) || (typeof language === 'string' && !!current()?.known.has(language));

export const resolvedLanguage = (language: LanguageCode): LanguageCode => (isKnownLanguage(language) ? language : 'en');

const packStrings = (packs: LanguagePacks[], namespace: LanguagePackNamespace, language: string): Strings[] =>
  packs.map(pack => (isPlainObject(pack.strings) ? pack.strings[namespace]?.[language] : undefined)).filter(isPlainObject);

export const hasPackStrings = (namespace: LanguagePackNamespace, language: string): boolean => !!packStrings(current()?.packs ?? [], namespace, language).length;

export const packNamespaceOf = (mapperKey: string): LanguagePackNamespace | undefined => NAMESPACE_BY_MAPPER_KEY.get(mapperKey);

const isDirection = (value: unknown): value is ResolvedLanguageMeta['direction'] => value === 'ltr' || value === 'rtl';
const isNumerals = (value: unknown): value is ResolvedLanguageMeta['numerals'] => value === 'latn' || value === 'arab';
const isWeekday = (value: unknown): value is ResolvedLanguageMeta['weekStartsOn'] => Number.isInteger(value) && (value as number) >= 0 && (value as number) <= 6;

function lastValid<T>(values: unknown[], valid: (value: unknown) => value is T): T | undefined {
  for (const value of [...values].reverse()) if (valid(value)) return value;
  return undefined;
}

export function languageMeta(language: LanguageCode): ResolvedLanguageMeta {
  const packs = current()?.packs ?? [];
  const declared = packs.map(pack => (isPlainObject(pack.languages) ? pack.languages[language] : undefined)).filter(isPlainObject) as LanguageMeta[];
  const declaredValue = <T>(key: keyof LanguageMeta, valid: (value: unknown) => value is T) =>
    lastValid(
      declared.map(meta => meta[key]),
      valid,
    );
  const builtIn = isBuiltInLanguage(language) ? BUILT_IN_META[language] : undefined;

  return {
    direction:
      declaredValue('direction', isDirection) ??
      lastValid(
        packStrings(packs, 'global', language).map(strings => strings.direction),
        isDirection,
      ) ??
      builtIn?.direction ??
      'ltr',
    weekStartsOn: declaredValue('weekStartsOn', isWeekday) ?? builtIn?.weekStartsOn ?? 1,
    numerals: declaredValue('numerals', isNumerals) ?? builtIn?.numerals ?? 'latn',
  };
}

function mergeStrings(base: Strings, over: Strings, path: string): Strings {
  const merged: Strings = { ...base };

  Object.entries(over).forEach(([key, value]) => {
    const previous = merged[key];

    if (Array.isArray(previous) && (!Array.isArray(value) || value.length !== previous.length)) {
      warnOnce(`Language pack value ${path}.${key} must be a list of ${previous.length} entries and was ignored.`);
    } else if (isPlainObject(value)) {
      merged[key] = mergeStrings(isPlainObject(previous) ? previous : {}, value, `${path}.${key}`);
    } else if (value !== undefined) {
      merged[key] = value;
    }
  });

  return merged;
}

export function overlay<T>(namespace: LanguagePackNamespace, language: LanguageCode, builtIn: T, builtInEn?: T, variant = ''): T {
  const state = current();
  if (!state?.packs.length) return builtIn;

  const code = resolvedLanguage(language);
  const cacheKey = `overlay|${namespace}|${code}|${variant}`;
  const cached = state.values.get(cacheKey) as { builtIn: T; builtInEn: T; value: T } | undefined;
  if (cached && cached.builtIn === builtIn && cached.builtInEn === builtInEn) return cached.value;

  const localBuiltIn = isBuiltInLanguage(code) && code !== 'en';
  const english = packStrings(state.packs, namespace, 'en');
  const local = code === 'en' ? [] : packStrings(state.packs, namespace, code);
  const path = `${namespace}.${code}`;

  let value: Strings = {};

  if (!localBuiltIn || english.length) {
    const englishBase = localBuiltIn ? builtInEn : (builtInEn ?? builtIn);
    if (isPlainObject(englishBase)) value = mergeStrings(value, englishBase, path);
    english.forEach(strings => (value = mergeStrings(value, strings, path)));
  }

  if (localBuiltIn && isPlainObject(builtIn)) value = mergeStrings(value, builtIn, path);
  local.forEach(strings => (value = mergeStrings(value, strings, path)));

  if (namespace === 'global') {
    value.lang = code;
    value.direction = languageMeta(code).direction;
  }

  state.values.set(cacheKey, { builtIn, builtInEn, value });

  return value as T;
}

export function bundledStrings<T>(namespace: LanguagePackNamespace, language: LanguageCode, table: Record<string, T>, variant = ''): T {
  const builtIn = table[language] ?? table.en;
  if (!packsRegistered()) return builtIn;

  return overlay(namespace, language, builtIn, table.en, variant);
}

export function memoizeWithPacks<T>(key: string, compute: () => T): T {
  const state = current();
  if (!state) return compute();
  if (!state.values.has(key)) state.values.set(key, compute());

  return state.values.get(key) as T;
}

export function packLanguageFromUrl(query: string | null, path: string): LanguageCode | undefined {
  const state = current();
  if (!state?.packs.length) return undefined;

  const codes = [...state.known].filter(code => !isBuiltInLanguage(code));
  const wanted = query?.toLowerCase();
  const segments = path.toLowerCase().split('/');

  return codes.find(code => wanted === code.toLowerCase()) ?? codes.find(code => segments.includes(code.toLowerCase()));
}
