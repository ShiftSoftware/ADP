import { Build } from '@stencil/core';
import { InferType, ObjectSchema } from 'yup';

import { version } from '../../../package.json';
import localeNetworkMapper from '../../locale-mapper';

import globalSchema from '~locales/type';
import errorsSchema from '~locales/errors/type';
import { LanguageKeys, languageMapper, LocaleKeyEntries, SharedLocales } from './types';

const cachedLocales = {};

export async function getSharedLocal(languageKey: LanguageKeys): Promise<SharedLocales> {
  const [errors, globalKeys] = await Promise.all([getLocaleLanguage(languageKey, 'errors', errorsSchema), getLocaleLanguage(languageKey, '-', globalSchema)]);

  return { errors, ...globalKeys };
}

export async function getLocaleLanguage<T extends ObjectSchema<any>>(languageKey: LanguageKeys, component: LocaleKeyEntries, schema: T): Promise<InferType<T>> {
  const languageFile = languageMapper[languageKey] || languageMapper.en;

  const localeFiles = localeNetworkMapper[component];

  if (!localeFiles || !localeFiles.length) throw new Error(`Locale file not found for component: ${component}`);

  if (localeFiles.length === 1) {
    const localeFile = localeFiles[0] + languageFile;
    const response = await requestLocaleFile(localeFile);

    try {
      schema.validateSync(response, { strict: true, abortEarly: false });
    } catch (error) {
      console.error(`Failed to parse locale file: ${localeFile}`);
      console.error(error);
    }

    return response;
  } else {
    const localeFilePromises = localeFiles.map(localeFile => requestLocaleFile(localeFile + languageFile));
    const localeFilesResponses = await Promise.all(localeFilePromises);

    const responseBluePrint = schema.getDefault();

    const parsedResponseMapper = {};

    recursiveParser(component, responseBluePrint, parsedResponseMapper, languageFile);

    try {
      schema.validateSync(parsedResponseMapper, { strict: true, abortEarly: false });
    } catch (error) {
      console.error(`Failed to parse locale file component: ${component}`);
      console.error(error);
    }

    return localeFilesResponses;
  }
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function recursiveParser(parents: string, bluePrint: object, parsedResponseMapper: object, languageFile: string) {
  let mappedSelf = false;

  Object.entries(bluePrint).forEach(([key, value]) => {
    if (typeof value === 'string' && !mappedSelf) {
      if (localeNetworkMapper[parents]) {
        const cacheKey = localeNetworkMapper[parents][0] + languageFile;
        if (cachedLocales[cacheKey]) Object.assign(parsedResponseMapper, cachedLocales[cacheKey]);
      }
      mappedSelf = true;
    } else if (isPlainObject(value)) {
      const childMapperKey = parents + '.' + key;

      const isNestedObject = Object.entries(value).some(([_, nestedValue]) => isPlainObject(nestedValue));

      if (isNestedObject) {
        parsedResponseMapper[key] = {};
        recursiveParser(childMapperKey, value, parsedResponseMapper[key], languageFile);
      } else if (localeNetworkMapper[childMapperKey] && !!localeNetworkMapper[childMapperKey].length) {
        const cacheKey = localeNetworkMapper[childMapperKey][0] + languageFile;

        if (cachedLocales[cacheKey]) parsedResponseMapper[key] = cachedLocales[cacheKey];
      }
    }
  });
}

async function requestLocaleFile(localeFile: string) {
  if (cachedLocales[localeFile]) return await cachedLocales[localeFile];

  try {
    const fetchPromise = (Build.isDev ? fetch('../../' + localeFile) : fetch(`https://cdn.jsdelivr.net/npm/adp-web-components@${version}/dist/${localeFile}`)).then(res => {
      if (!res.ok) delete cachedLocales[localeFile];
      return res.json();
    });

    cachedLocales[localeFile] = fetchPromise;

    const result = await fetchPromise;
    cachedLocales[localeFile] = result;

    return result;
  } catch (error) {
    delete cachedLocales[localeFile];
    return {};
  }
}
