import { FormHook } from './form-hook';
import { getSharedFormLocal, LanguageKeys } from '~features/multi-lingual';
import { FormElementStructure, FormHookInterface } from './interface';
import { gistLoader } from './gist-loader';
import { fetchJson } from '~lib/fetch-json';
import { AnyObjectSchema } from 'yup';

export type FormLanguageChange = {
  form: FormHook<any>;
  locale: Record<string, any>;
  localeLanguage: LanguageKeys;
  structure: Record<string, any>;
};

export const formLanguageChange = async (formContext: FormLanguageChange, newLanguage: LanguageKeys) => {
  const sharedLocales = await getSharedFormLocal(newLanguage);

  const LS = formContext.structure?.data?.localization;
  const structureLocalization = typeof LS === 'object' ? (LS?.[newLanguage] ?? LS?.en ?? {}) : {};

  formContext.locale = { ...structureLocalization, sharedFormLocales: { ...sharedLocales, ...structureLocalization?.sharedFormLocales } };

  formContext.localeLanguage = newLanguage;

  formContext.form?.rerender({ rerenderAll: true });
};

export type FormErrorHandler = {
  errorMessage: string;
  locale: Record<string, any>;
  errorCallback?: (error: any, message: string) => void;
};

export const formErrorHandler = async (formContext: FormErrorHandler, error: any) => {
  const message = error.message || formContext.locale?.sharedFormLocales?.errors?.wildCard || '';

  if (formContext?.errorCallback) formContext.errorCallback(error, message);
  else formContext.errorMessage = message;
};

export type FormLoadingHandler = {
  isLoading: boolean;
  loadingChanges?: (loading: boolean) => void;
};

export const formLoadingHandler = async (formContext: FormLoadingHandler, isLoading: boolean) => {
  formContext.isLoading = isLoading;
  if (formContext.loadingChanges) formContext.loadingChanges(isLoading);
};

export type FormSuccessHandler = {
  el: HTMLElement;
  isLoading: boolean;
  form: FormHook<any>;
  locale: Record<string, any>;
  disableScrollToTop?: boolean;
  loadingChanges?: (loading: boolean) => void;
  successCallback?: (data: any, message?: string) => void;
};

export const formSuccessHandler = async (formContext: FormSuccessHandler, data: any) => {
  if (formContext.successCallback) formContext.successCallback(data, formContext.locale['Form submitted successfully.'] || '');
  else formContext.form.openDialog();

  if (!formContext.disableScrollToTop) {
    const formDom = formContext.el.shadowRoot || formContext.el;

    let targetElement = formDom instanceof ShadowRoot ? formDom.firstElementChild : formDom;

    const yOffset = -100;
    const y = targetElement.getBoundingClientRect().top + window.pageYOffset + yOffset;

    window.scrollTo({ top: Math.max(y, 0), behavior: 'smooth' });
  }
};

export type FormDidLoadHandler<B> = {
  gistId?: string;
  form: FormHook<B>;
  structureUrl?: string;
  isMobileForm: boolean;
  language: LanguageKeys;
  localeLanguage: LanguageKeys;
  structure: Record<string, any>;
  changeLanguage: (newLanguage: LanguageKeys) => Promise<void>;
};

export const formDidLoadHandler = async <T, B>(formContext: FormDidLoadHandler<B> & FormHookInterface<B>, validator: AnyObjectSchema) => {
  if (!formContext.structure) {
    if (formContext.gistId) {
      const [newGistStructure] = await Promise.all([gistLoader(formContext.gistId), formContext.changeLanguage(formContext.language)]);
      formContext.structure = newGistStructure as FormElementStructure<T>;
    } else if (formContext.structureUrl) {
      const [newGistStructure] = await Promise.all([fetchJson<FormElementStructure<T>>(formContext.structureUrl), formContext.changeLanguage(formContext.language)]);
      formContext.structure = newGistStructure;
    }
  }
  formContext.localeLanguage = formContext.language;

  await formContext.changeLanguage(formContext.language);

  if (!formContext.isMobileForm) {
    try {
      const key = formContext.structure?.data?.recaptchaKey;
      if (key) {
        const script = document.createElement('script');
        script.src = `https://www.google.com/recaptcha/api.js?render=${key}&hl=${formContext.language}`;
        script.async = true;
        script.defer = true;

        document.head.appendChild(script);
      }
    } catch (error) {
      console.log(error);
    }
  }

  formContext.form = new FormHook<B>(formContext, validator);
};
