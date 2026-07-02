/**
 * UI-chrome localization for the renderer. The schema's own `LocalizedString`
 * objects handle survey content; this file only covers the renderer's own
 * built-in strings (buttons, fallback labels, error copy).
 *
 * Deliberately **not** i18next-based: there are ~10 strings, no pluralization,
 * no interpolation, and consumers can override locale-by-locale via
 * `<SurveyRenderer uiLocales={...} />`. If a consumer needs full i18next later,
 * they wrap `<SurveyRenderer>` with their own provider and hand in the
 * translated `UiStrings` via the same prop.
 */

export interface UiStrings {
  /** Primary button that advances to the next screen. */
  next: string;
  /** Button label while a submission is in flight. */
  submitting: string;
  /** Loading message shown while the schema is fetching (standalone app only). */
  loading: string;
  /** Fallback title shown in the `done` state if the schema has no title. */
  thankYou: string;
  /** First option in a dropdown when the schema doesn't supply a placeholder. */
  selectPlaceholder: string;
  /** Button label on the signature component. */
  clearSignature: string;
  /** Inline placeholder text for a screen with zero questions (shouldn't happen in practice). */
  noScreens: string;
  /** Developer placeholder when the schema's `type` has no matching registry entry. */
  unsupportedQuestion: string;
  /** Prefix for submission-error alerts (followed by the server message). */
  couldNotSubmit: string;
  /** Inline error under a required question the user tried to Next past. */
  requiredError: string;
  /** Inline constraint errors (client-side AnswerValidator mirror). `{n}` /
   *  `{min}` / `{max}` placeholders are substituted by `formatUi`. */
  minLengthError: string;
  maxLengthError: string;
  patternError: string;
  minError: string;
  maxError: string;
  rangeError: string;
  minSelectedError: string;
  maxSelectedError: string;
  /** Generic fallback for constraint codes without a dedicated template. */
  invalidAnswerError: string;
  /** Default labels for YesNoQuestion when the schema doesn't supply `yesLabel` / `noLabel`. */
  yes: string;
  no: string;
}

export interface LocaleConfig {
  /** Writing direction — applied to the renderer root as the `dir` attribute. */
  direction: 'ltr' | 'rtl';
  strings: UiStrings;
}

const en: LocaleConfig = {
  direction: 'ltr',
  strings: {
    next: 'Next',
    submitting: 'Submitting…',
    loading: 'Loading survey…',
    thankYou: 'Thank you.',
    selectPlaceholder: 'Select…',
    clearSignature: 'Clear',
    noScreens: 'No screens in this survey.',
    unsupportedQuestion: 'Unsupported question type:',
    couldNotSubmit: 'Could not submit:',
    requiredError: 'This question is required.',
    minLengthError: 'Must be at least {n} characters.',
    maxLengthError: 'Must be at most {n} characters.',
    patternError: 'Does not match the required format.',
    minError: 'Must be at least {n}.',
    maxError: 'Must be at most {n}.',
    rangeError: 'Must be between {min} and {max}.',
    minSelectedError: 'Select at least {n} option(s).',
    maxSelectedError: 'Select at most {n} option(s).',
    invalidAnswerError: 'Please check this answer.',
    yes: 'Yes',
    no: 'No',
  },
};

const ar: LocaleConfig = {
  direction: 'rtl',
  strings: {
    next: 'التالي',
    submitting: 'جاري الإرسال…',
    loading: 'جاري تحميل الاستبيان…',
    thankYou: 'شكراً لك.',
    selectPlaceholder: 'اختر…',
    clearSignature: 'مسح',
    noScreens: 'لا توجد شاشات في هذا الاستبيان.',
    unsupportedQuestion: 'نوع سؤال غير مدعوم:',
    couldNotSubmit: 'تعذر الإرسال:',
    requiredError: 'هذا السؤال مطلوب.',
    minLengthError: 'يجب ألا يقل عن {n} حرفاً.',
    maxLengthError: 'يجب ألا يزيد عن {n} حرفاً.',
    patternError: 'لا يطابق التنسيق المطلوب.',
    minError: 'يجب ألا يقل عن {n}.',
    maxError: 'يجب ألا يزيد عن {n}.',
    rangeError: 'يجب أن يكون بين {min} و {max}.',
    minSelectedError: 'اختر {n} خيارات على الأقل.',
    maxSelectedError: 'اختر {n} خيارات كحد أقصى.',
    invalidAnswerError: 'يرجى التحقق من هذه الإجابة.',
    yes: 'نعم',
    no: 'لا',
  },
};

/** Built-in locales. Consumers extend or override by passing `uiLocales` to
 *  `<SurveyRenderer>` — the merge is shallow at the locale level, so
 *  overriding `ar` with a partial object would require supplying a full
 *  `LocaleConfig`. If this becomes limiting, deep-merge here. */
export const builtInLocales: Record<string, LocaleConfig> = { en, ar };

/** Substitute `{name}` placeholders in a UI string template. Kept deliberately
 *  tiny — no plurals, no nesting; consumers needing more override `uiLocales`. */
export function formatUi(template: string, params?: Record<string, string | number>): string {
  if (!params) return template;
  return template.replace(/\{(\w+)\}/g, (match, key: string) =>
    key in params ? String(params[key]) : match,
  );
}

/** Resolve the active LocaleConfig for a survey. Priority:
 *   1. The requested `locale` (user or schema.defaultLocale).
 *   2. The schema's `defaultLocale`.
 *   3. `en`.
 *  Consumer-supplied locales take precedence over the built-ins within each step. */
export function resolveLocaleConfig(
  locale: string,
  defaultLocale: string | undefined,
  overrides?: Record<string, LocaleConfig>,
): LocaleConfig {
  const all = { ...builtInLocales, ...(overrides ?? {}) };
  return all[locale] ?? (defaultLocale ? all[defaultLocale] : undefined) ?? all['en'] ?? en;
}
