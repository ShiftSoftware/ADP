import { readFileSync } from 'fs';
import ts from 'typescript';
import { join } from 'path';

import errorsSchema from '~locales/errors/type';
import formsSchema from '~locales/forms/type';
import globalSchema from '~locales/type';
import generalSchema from '~locales/general/type';
import calendarEn from '../../locales/calendar/en.json';
import popoverEn from '../../locales/popover/en.json';
import formsEn from '../../locales/forms/en.json';
import formsRu from '../../locales/forms/ru.json';
import errorsEn from '../../locales/errors/en.json';
import errorsRu from '../../locales/errors/ru.json';

import getLanguageFromUrl from '~lib/get-language-from-url';
import { HOST_CONFIG_KEY, peekHostConfig } from '~features/host-bridge/host-config';
import { calendarLocale } from '../../components/form-elements/shift-calendar/calendar-locale';
import { popoverStrings } from '../../components/components/shift-popover/popover-locale';
import { inputStrings } from '../../components/form-elements/shift-input/input-locale';
import { bookingStrings } from '../../components/form-elements/shift-time-slots/time-slots';
import { dayStripStrings } from '../../components/form-elements/shift-day-strip/day-strip-locale';

import { getLocaleLanguage, getSharedFormLocal } from './get-local-language';
import { knownLanguages, languageMeta, LanguagePacks, LanguagePacksInput, packsRegistered } from './language-packs';

const localesRoot = join(__dirname, '../../');

// host-loader.js is shipped as plain ESM, which Jest does not transform.
function loadHostLoader(): { configureAdpHost: (options: Record<string, unknown>) => void } {
  const source = readFileSync(join(__dirname, '../../integration/host-loader.js'), 'utf8');
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2021 } });
  const module = { exports: {} };
  new Function('exports', 'module', outputText)(module.exports, module);

  return module.exports as { configureAdpHost: (options: Record<string, unknown>) => void };
}

const { configureAdpHost } = loadHostLoader();
const register = (languagePacks: LanguagePacksInput) => configureAdpHost({ languagePacks });
let requested: string[] = [];

const fakeFetch = jest.fn(async (input: unknown) => {
  const url = String(input);
  requested.push(url);
  const at = url.indexOf('/locales/');
  const body = JSON.parse(readFileSync(join(localesRoot, url.slice(at + 1)), 'utf8'));

  return { ok: true, status: 200, json: async () => body } as unknown as Response;
});

const clearStore = () => delete (globalThis as Record<symbol, unknown>)[HOST_CONFIG_KEY];

const setUrl = (href: string) => {
  window.location.href = href;
};

let warn: jest.SpyInstance;

beforeAll(() => {
  (globalThis as Record<string, unknown>).fetch = fakeFetch;
});

beforeEach(() => {
  clearStore();
  requested = [];
  warn = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
  jest.spyOn(console, 'error').mockImplementation(() => undefined);
});

afterEach(() => {
  clearStore();
  jest.restoreAllMocks();
});

describe('language packs: no pack registered', () => {
  it('returns the cached locale file itself and requests nothing extra', async () => {
    const first = await getLocaleLanguage('ru', 'errors', errorsSchema);
    const before = requested.length;
    const second = await getLocaleLanguage('ru', 'errors', errorsSchema);
    const forms = await getLocaleLanguage('ru', 'forms*', formsSchema);

    expect(second).toBe(first);
    expect(first).toEqual(errorsRu);
    expect(forms).toEqual(formsRu);
    expect(requested.slice(before).filter(url => url.includes('/errors/'))).toEqual([]);
    expect(requested.some(url => url.includes('/forms/en.json'))).toBe(false);
    expect(packsRegistered()).toBe(false);
    expect(peekHostConfig()).toBeUndefined();
  });

  it('keeps the bundled strings identical', () => {
    expect(calendarLocale('ru')).toBe(calendarLocale('ru'));
    expect(calendarLocale('uz')).toBe(calendarLocale('en'));
    expect(calendarLocale('en').strings).toBe(calendarEn);
    expect(popoverStrings('xx')).toBe(popoverEn);
    expect(knownLanguages()).toEqual(['en', 'ar', 'ku', 'ru']);
  });

  it('an unknown language still reads the en files, ltr', async () => {
    const global = await getLocaleLanguage('uz', '-', globalSchema);

    expect(global.lang).toBe('en');
    expect(global.direction).toBe('ltr');
  });
});

describe('language packs: fallback order', () => {
  it('an en override replaces the built-in en string and reaches a language that lacks the key', async () => {
    register({ languages: { uz: {} }, strings: { forms: { en: { noSelectOptions: 'Nothing to pick.' } } } });

    expect((await getLocaleLanguage('en', 'forms*', formsSchema)).noSelectOptions).toBe('Nothing to pick.');
    expect((await getLocaleLanguage('uz', 'forms*', formsSchema)).noSelectOptions).toBe('Nothing to pick.');
    expect((await getLocaleLanguage('ru', 'forms*', formsSchema)).noSelectOptions).toBe(formsRu.noSelectOptions);
  });

  it('a built-in language can be overridden key by key, nested objects merged', async () => {
    register({ strings: { forms: { ru: { submit: 'Отправить заявку', errors: { extra: 'x' } } }, errors: { ru: { invalidVin: 'Неверный VIN' } } } });

    const forms = await getLocaleLanguage('ru', 'forms*', formsSchema);
    const errors = await getLocaleLanguage('ru', 'errors', errorsSchema);

    expect(forms.submit).toBe('Отправить заявку');
    expect(forms.close).toBe(formsRu.close);
    expect(forms.errors).toEqual({ ...formsRu.errors, extra: 'x' });
    expect(errors.invalidVin).toBe('Неверный VIN');
    expect(errors.noPartsFound).toBe(errorsRu.noPartsFound);
  });

  it('adds a whole new language with its strings, meta and direction', async () => {
    register({
      languages: { uz: { direction: 'ltr' } },
      strings: {
        global: { uz: { language: 'o‘zbekcha', noData: 'Ma’lumot yo‘q.' } },
        forms: { uz: { submit: 'Yuborish', errors: { wildCard: 'Xatolik' } } },
        errors: { uz: { invalidVin: 'VIN noto‘g‘ri' } },
      },
    });

    const shared = await getSharedFormLocal('uz');

    expect(shared.submit).toBe('Yuborish');
    expect(shared.close).toBe(formsEn.close);
    expect(shared.language).toBe('o‘zbekcha');
    expect(shared.lang).toBe('uz');
    expect(shared.direction).toBe('ltr');
    expect(shared.invalidVin).toBe('VIN noto‘g‘ri');
    expect(shared.noPartsFound).toBe(errorsEn.noPartsFound);
    expect(knownLanguages()).toContain('uz');
  });

  it('an empty language joins the list and resolves every key to en', async () => {
    register({ languages: { tk: {} } });
    register({ strings: { forms: { kk: {} } } });

    expect(knownLanguages()).toEqual(expect.arrayContaining(['en', 'ar', 'ku', 'ru', 'tk', 'kk']));

    for (const code of ['tk', 'kk']) {
      expect(await getLocaleLanguage(code, 'forms*', formsSchema)).toEqual(formsEn);
      expect(await getLocaleLanguage(code, 'errors', errorsSchema)).toEqual(errorsEn);
      expect(calendarLocale(code).strings).toEqual(calendarEn);
      expect(calendarLocale(code).language).toBe(code);
      expect(languageMeta(code)).toEqual({ direction: 'ltr', weekStartsOn: 1, numerals: 'latn' });
    }
  });

  it('a missing key falls back to pack en, then to built-in en', async () => {
    register({ strings: { forms: { en: { submit: 'Send' }, uz: { close: 'Yopish' } } } });

    const forms = await getLocaleLanguage('uz', 'forms*', formsSchema);

    expect(forms.close).toBe('Yopish');
    expect(forms.submit).toBe('Send');
    expect(forms.reCaptchaIsRequired).toBe(formsEn.reCaptchaIsRequired);
  });

  it('pack en reaches a built-in language only for keys its own file lacks', async () => {
    register({ strings: { forms: { en: { submit: 'Send', onlyInEnglish: 'E' } } } });

    const forms = (await getLocaleLanguage('ru', 'forms*', formsSchema)) as Record<string, unknown>;

    expect(forms.submit).toBe(formsRu.submit);
    expect(forms.onlyInEnglish).toBe('E');
  });

  it('the later registration wins per key, and registering a pack again moves it last', async () => {
    const first: LanguagePacks = { strings: { general: { en: { close: 'first' } } } };
    const second: LanguagePacks = { strings: { general: { en: { close: 'second' } } } };

    register(first);
    register(second);
    expect((await getLocaleLanguage('en', 'general', generalSchema)).close).toBe('second');

    register(first);
    expect((await getLocaleLanguage('en', 'general', generalSchema)).close).toBe('first');
  });

  it('accepts one pack, an array and JSON text', async () => {
    register(JSON.stringify([{ strings: { general: { en: { close: 'json' } } } }, { strings: { general: { en: { submit: 'array' } } } }]));
    register([{ strings: { general: { en: { formSubmittedSuccessfully: 'list' } } } }]);
    register({ strings: { general: { en: { close: 'object' } } } });

    const general = (await getLocaleLanguage('en', 'general', generalSchema)) as Record<string, unknown>;

    expect(general).toMatchObject({ close: 'object', submit: 'array', formSubmittedSuccessfully: 'list' });
  });

  it('never mutates the cached locale file', async () => {
    const pristine = await getLocaleLanguage('en', 'errors', errorsSchema);
    const copy = JSON.parse(JSON.stringify(pristine));

    register({ strings: { errors: { en: { invalidVin: 'changed' } } } });
    await getLocaleLanguage('en', 'errors', errorsSchema);
    clearStore();

    const again = await getLocaleLanguage('en', 'errors', errorsSchema);

    expect(again).toBe(pristine);
    expect(again).toEqual(copy);
  });

  it('ignores bad input with one warning each', async () => {
    register('{not json');
    register({ strings: { nope: { en: { a: 'b' } } } as never });
    register({ strings: { calendar: { uz: { months: ['one'] } } }, languages: { uz: {} } });

    knownLanguages();
    knownLanguages();
    expect(calendarLocale('uz').strings.months).toEqual(calendarEn.months);
    expect(calendarLocale('uz').strings.months).toEqual(calendarEn.months);

    const messages = warn.mock.calls.map(call => String(call[0]));
    expect(messages.filter(message => message.includes('nope'))).toHaveLength(1);
    expect(messages.filter(message => message.includes('calendar.uz.months'))).toHaveLength(1);
  });
});

describe('language packs: global entry point', () => {
  it('configureAdpHost is the one entry point and ignores unknown keys', async () => {
    configureAdpHost({ languagePacks: { strings: { general: { en: { close: 'host' } } } }, fetch: undefined, somethingElse: 1 });

    const general = await getLocaleLanguage('en', 'general', generalSchema);

    expect(general.close).toBe('host');
    expect(Object.keys(peekHostConfig())).toEqual(['languagePacks']);
  });

  it('a pack written straight onto the symbol key is read, and a later configureAdpHost wins over it', async () => {
    (globalThis as Record<symbol, unknown>)[HOST_CONFIG_KEY] = { languagePacks: { strings: { general: { en: { close: 'direct', submit: 'direct' } } } } };
    expect((await getLocaleLanguage('en', 'general', generalSchema)).close).toBe('direct');

    configureAdpHost({ languagePacks: { strings: { general: { en: { submit: 'host' } } } } });

    const general = await getLocaleLanguage('en', 'general', generalSchema);

    expect(general.close).toBe('direct');
    expect(general.submit).toBe('host');
  });

  it('null, undefined and empty text register nothing', () => {
    configureAdpHost({ languagePacks: null });
    configureAdpHost({ languagePacks: undefined });
    configureAdpHost({ languagePacks: '' });

    expect(packsRegistered()).toBe(false);
    expect(knownLanguages()).toEqual(['en', 'ar', 'ku', 'ru']);
  });
});

describe('language packs: meta and direction', () => {
  it('uz is ltr by default, a pack can declare an rtl language, built-ins keep theirs', async () => {
    register({ languages: { uz: {}, ug: { direction: 'rtl', weekStartsOn: 6, numerals: 'arab' } }, strings: { global: { fa: { direction: 'rtl' } } } });

    expect(languageMeta('uz').direction).toBe('ltr');
    expect(languageMeta('ug')).toEqual({ direction: 'rtl', weekStartsOn: 6, numerals: 'arab' });
    expect(languageMeta('fa').direction).toBe('rtl');
    expect(languageMeta('ar').direction).toBe('rtl');
    expect(languageMeta('ru').direction).toBe('ltr');

    expect((await getLocaleLanguage('ug', '-', globalSchema)).direction).toBe('rtl');
    expect((await getLocaleLanguage('ar', '-', globalSchema)).direction).toBe('rtl');
    expect((await getLocaleLanguage('uz', '-', globalSchema)).direction).toBe('ltr');
    expect(calendarLocale('ug')).toMatchObject({ language: 'ug', direction: 'rtl', weekStartsOn: 6, numerals: 'arab' });
    expect(calendarLocale('ar')).toMatchObject({ language: 'ar', direction: 'rtl', weekStartsOn: 6, numerals: 'arab' });
  });

  it('languages[L].direction beats strings.global[L].direction', () => {
    register({ languages: { ar: { direction: 'ltr' } }, strings: { global: { ar: { direction: 'rtl' } } } });

    expect(languageMeta('ar').direction).toBe('ltr');
  });
});

describe('language packs: bundled strings', () => {
  const months = ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'Iyun', 'Iyul', 'Avgust', 'Sentabr', 'Oktabr', 'Noyabr', 'Dekabr'];

  it('overlays calendar, booking, input, popover and day-strip strings for uz', () => {
    register({
      languages: { uz: {} },
      strings: {
        calendar: { uz: { months, today: 'Bugun' } },
        booking: { uz: { times: 'Vaqtlar', earlierDays: 'Oldingi kunlar' }, en: { idle: 'Pick a dealer first' } },
        input: { uz: { clear: 'Tozalash' } },
        popover: { uz: { dialog: 'Oyna' } },
      },
    });

    const locale = calendarLocale('uz');

    expect(locale.language).toBe('uz');
    expect(locale.strings.months).toEqual(months);
    expect(locale.strings.today).toBe('Bugun');
    expect(locale.strings.weekdays).toEqual(calendarEn.weekdays);
    expect(calendarLocale('uz')).toBe(locale);
    expect(bookingStrings('uz').times).toBe('Vaqtlar');
    expect(bookingStrings('uz').idle).toBe('Pick a dealer first');
    expect(bookingStrings('ru').idle).not.toBe('Pick a dealer first');
    expect(inputStrings('uz').clear).toBe('Tozalash');
    expect(popoverStrings('uz').dialog).toBe('Oyna');
    expect(dayStripStrings('uz').earlierDays).toBe('Oldingi kunlar');
  });

  it('a uz pack leaves the built-in ar calendar locale as it was', () => {
    const builtIn = JSON.parse(JSON.stringify(calendarLocale('ar')));
    register({ languages: { uz: {} }, strings: { calendar: { uz: { months, today: 'Bugun' } } } });

    expect(packsRegistered()).toBe(true);
    expect(calendarLocale('ar')).toEqual(builtIn);
    expect(calendarLocale('ar').direction).toBe('rtl');
  });

  it('a registration after the first read applies from the next read', () => {
    register({ languages: { uz: {} } });
    expect(popoverStrings('uz')).toEqual(popoverEn);

    register({ strings: { popover: { uz: { dialog: 'Oyna' } } } });
    expect(popoverStrings('uz').dialog).toBe('Oyna');
  });
});

describe('language packs: getLanguageFromUrl', () => {
  afterEach(() => setUrl('http://testing.stenciljs.com/'));

  it('without packs keeps today’s detection', () => {
    setUrl('http://host.invalid/uz/page');
    expect(getLanguageFromUrl()).toBe('en');
    setUrl('http://host.invalid/uzbekistan/ru/page');
    expect(getLanguageFromUrl()).toBe('ru');
  });

  it('matches a pack language exactly, only where it would have returned en', () => {
    register({ languages: { uz: {} } });

    const cases: [string, string][] = [
      ['http://host.invalid/uz/', 'uz'],
      ['http://host.invalid/site/uz', 'uz'],
      ['http://host.invalid/page?lang=uz', 'uz'],
      ['http://host.invalid/uzbekistan/ru/', 'ru'],
      ['http://host.invalid/uzbekistan/', 'en'],
      ['http://host.invalid/uzb/', 'en'],
      ['http://host.invalid/ar/uz/', 'ar'],
      ['http://host.invalid/uz/?lang=ru', 'ru'],
    ];

    for (const [href, expected] of cases) {
      setUrl(href);
      expect([href, getLanguageFromUrl()]).toEqual([href, expected]);
    }
  });
});
