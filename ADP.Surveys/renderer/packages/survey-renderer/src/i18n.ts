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
  /** Primary button label when advancing would end the survey — the press that
   *  actually submits. Answer-aware: swaps in whenever `computeNext` resolves
   *  to `end` for the current answers, so it's branch-accurate. */
  submit: string;
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
  /** Inline message while a question's external options are being fetched. */
  loadingOptions: string;
  /** Inline message when an external options fetch failed. */
  optionsLoadError: string;
  /** Button that re-attempts a failed external options fetch. */
  retry: string;
  /** Default labels for YesNoQuestion when the schema doesn't supply `yesLabel` / `noLabel`. */
  yes: string;
  no: string;
  /**
   * Confirmation under a file input. Deliberately says the file's DETAILS were recorded,
   * not the file — the platform stores `{name, size, type}` and discards the bytes.
   * `{name}` is substituted by `formatUi`.
   */
  fileRecordedName: string;
  /** Accessible name for the language picker shown when a survey declares more
   *  than one locale. Rendered per-locale so the control reads correctly in the
   *  language the respondent is currently in. */
  language: string;
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
    submit: 'Submit',
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
    loadingOptions: 'Loading options…',
    optionsLoadError: 'Could not load the options.',
    retry: 'Retry',
    yes: 'Yes',
    no: 'No',
    fileRecordedName: 'Recorded file details: {name}',
    language: 'Language',
  },
};

const ar: LocaleConfig = {
  direction: 'rtl',
  strings: {
    next: 'التالي',
    submit: 'إرسال',
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
    loadingOptions: 'جاري تحميل الخيارات…',
    optionsLoadError: 'تعذر تحميل الخيارات.',
    retry: 'إعادة المحاولة',
    yes: 'نعم',
    no: 'لا',
    fileRecordedName: 'تم تسجيل تفاصيل الملف: {name}',
    language: 'اللغة',
  },
};

// Sorani Kurdish, in Arabic script — RTL, like `ar`.
// NOTE: machine-written copy pending a native pass, same discipline as the
// web-components landing page. The strings are short and mechanical (button
// labels, validation messages); the direction and the language name are the
// parts that must be right, and those are.
const ku: LocaleConfig = {
  direction: 'rtl',
  strings: {
    next: 'دواتر',
    submit: 'ناردن',
    submitting: 'دەنێردرێت…',
    loading: 'ڕاپرسی باردەکرێت…',
    thankYou: 'سوپاس.',
    selectPlaceholder: 'هەڵبژێرە…',
    clearSignature: 'سڕینەوە',
    noScreens: 'هیچ پەڕەیەک لەم ڕاپرسییەدا نییە.',
    unsupportedQuestion: 'جۆری پرسیاری پشتگیری نەکراو:',
    couldNotSubmit: 'ناردن سەرکەوتوو نەبوو:',
    requiredError: 'ئەم پرسیارە پێویستە.',
    minLengthError: 'دەبێت لانیکەم {n} پیت بێت.',
    maxLengthError: 'دەبێت زۆرترین {n} پیت بێت.',
    patternError: 'لەگەڵ فۆرماتی داواکراودا یەک ناگرێتەوە.',
    minError: 'دەبێت لانیکەم {n} بێت.',
    maxError: 'دەبێت زۆرترین {n} بێت.',
    rangeError: 'دەبێت لە نێوان {min} و {max} بێت.',
    minSelectedError: 'لانیکەم {n} هەڵبژاردە هەڵبژێرە.',
    maxSelectedError: 'زۆرترین {n} هەڵبژاردە هەڵبژێرە.',
    invalidAnswerError: 'تکایە ئەم وەڵامە بپشکنە.',
    loadingOptions: 'هەڵبژاردەکان باردەکرێن…',
    optionsLoadError: 'نەتوانرا هەڵبژاردەکان باربکرێن.',
    retry: 'دووبارە هەوڵبدەوە',
    yes: 'بەڵێ',
    no: 'نەخێر',
    fileRecordedName: 'زانیاری فایل تۆمارکرا: {name}',
    language: 'زمان',
  },
};

// Same native-pass caveat as `ku`.
const ru: LocaleConfig = {
  direction: 'ltr',
  strings: {
    next: 'Далее',
    submit: 'Отправить',
    submitting: 'Отправка…',
    loading: 'Загрузка опроса…',
    thankYou: 'Спасибо.',
    selectPlaceholder: 'Выберите…',
    clearSignature: 'Очистить',
    noScreens: 'В этом опросе нет экранов.',
    unsupportedQuestion: 'Неподдерживаемый тип вопроса:',
    couldNotSubmit: 'Не удалось отправить:',
    requiredError: 'Этот вопрос обязателен.',
    minLengthError: 'Не менее {n} символов.',
    maxLengthError: 'Не более {n} символов.',
    patternError: 'Не соответствует требуемому формату.',
    minError: 'Не менее {n}.',
    maxError: 'Не более {n}.',
    rangeError: 'Должно быть от {min} до {max}.',
    minSelectedError: 'Выберите не менее {n} вариантов.',
    maxSelectedError: 'Выберите не более {n} вариантов.',
    invalidAnswerError: 'Пожалуйста, проверьте этот ответ.',
    loadingOptions: 'Загрузка вариантов…',
    optionsLoadError: 'Не удалось загрузить варианты.',
    retry: 'Повторить',
    yes: 'Да',
    no: 'Нет',
    fileRecordedName: 'Записаны сведения о файле: {name}',
    language: 'Язык',
  },
};

/** Built-in locales. Consumers extend or override by passing `uiLocales` to
 *  `<SurveyRenderer>` — the merge is shallow at the locale level, so
 *  overriding `ar` with a partial object would require supplying a full
 *  `LocaleConfig`. If this becomes limiting, deep-merge here.
 *
 *  The set matches the builder's default locale catalog (en/ar/ku) plus `ru`
 *  for the markets that author in it. A survey may still declare a locale that
 *  isn't here: its own content renders fine (that comes from the schema), the
 *  renderer's own chrome falls back per `resolveLocaleConfig`. */
export const builtInLocales: Record<string, LocaleConfig> = { en, ar, ku, ru };

/** Endonyms for the locales a deployment is likely to declare — a respondent
 *  picks their language by seeing it written in that language, never by a code.
 *  `Intl.DisplayNames` covers most of these but is inconsistent for `ku` across
 *  engines, so the known set is spelled out and Intl is only the fallback. */
const localeEndonyms: Record<string, string> = {
  en: 'English',
  ar: 'العربية',
  ku: 'کوردی',
  ru: 'Русский',
  tr: 'Türkçe',
  fa: 'فارسی',
};

/**
 * Human label for a locale code, in that locale's own language. Falls back to
 * `Intl.DisplayNames`, then to the raw code — a picker entry always renders
 * something selectable even for a locale nobody anticipated.
 */
export function localeDisplayName(code: string): string {
  const known = localeEndonyms[code];
  if (known) return known;

  try {
    const intl = new Intl.DisplayNames([code], { type: 'language' });
    const name = intl.of(code);
    if (name && name !== code) return name;
  } catch {
    // Old engine, or a code Intl rejects — the raw code is still a valid label.
  }
  return code;
}

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
