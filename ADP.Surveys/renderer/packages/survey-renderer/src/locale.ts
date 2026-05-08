import type { LocalizedString } from '@shiftsoftware/survey-sdk';

/** Minimal locale lookup. Prefers the caller's `locale`, falls back to the
 *  survey's `defaultLocale`, then any first value on the object. Empty on miss.
 *  Intentionally tiny — i18next integration lands with the multi-language slice. */
export function localize(
  value: LocalizedString | string | undefined | null,
  locale: string,
  defaultLocale?: string,
): string {
  if (value == null) return '';
  if (typeof value === 'string') return value;
  if (value[locale]) return value[locale]!;
  if (defaultLocale && value[defaultLocale]) return value[defaultLocale]!;
  const keys = Object.keys(value);
  if (keys.length > 0) return value[keys[0]!]!;
  return '';
}
