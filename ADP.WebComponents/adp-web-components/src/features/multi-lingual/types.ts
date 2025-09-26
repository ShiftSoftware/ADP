import { AnyObjectSchema, InferType, object } from 'yup';

import globalSchema from '~locales/type';
import errorsSchema from '~locales/errors/type';

import localeNetworkMapper from '../../locale-mapper';
import formsSchema from '~locales/forms/type';

export type LocaleKeyEntries = keyof typeof localeNetworkMapper;

export type ErrorKeys = keyof InferType<typeof errorsSchema>;

export const sharedLocalesSchema = object({
  errors: errorsSchema,
}).concat(globalSchema);

export type SharedLocales = InferType<typeof sharedLocalesSchema>;

export type ComponentLocale<T extends AnyObjectSchema> = {
  sharedLocales?: SharedLocales;
} & InferType<T>;

export const sharedFormLocalesSchema = object({}).concat(globalSchema).concat(formsSchema).concat(errorsSchema);

export type SharedFormLocales = InferType<typeof sharedFormLocalesSchema>;

export type FormLocale<T extends AnyObjectSchema> = {
  sharedFormLocales?: SharedFormLocales;
} & InferType<T>;

export const ARABIC_JSON_FILE = 'ar.json';
export const ENGLISH_JSON_FILE = 'en.json';
export const KURDISH_JSON_FILE = 'ku.json';
export const RUSSIAN_JSON_FILE = 'ru.json';

export type LanguageKeys = 'en' | 'ar' | 'ku' | 'ru';

export const languageMapper = {
  ar: ARABIC_JSON_FILE,
  en: ENGLISH_JSON_FILE,
  ku: KURDISH_JSON_FILE,
  ru: RUSSIAN_JSON_FILE,
};
