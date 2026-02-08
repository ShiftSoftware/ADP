import { FormHook } from './form-hook';
import { getSharedFormLocal, LanguageKeys, MultiLingual } from '~features/multi-lingual';
import { FormElementStructure, FormHookInterface } from './interface';
import { gistLoader } from './gist-loader';
import { fetchJson } from '~lib/fetch-json';
import { AnyObjectSchema } from 'yup';
import { Grecaptcha } from '~lib/recaptcha';

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

export type FormGetFormHandler = {
  form: FormHook<any>;
};

export const formGetFormHandler = async (formContext: FormGetFormHandler) => {
  if (formContext.form) return formContext.form;
};

export type FormSubmitHandler = {
  form: FormHook<any>;
};

export const formSubmitHandler = async (formContext: FormSubmitHandler) => {
  if (formContext.form) return formContext.form.submit();
};

export type FormStructureRenderedHandler = {
  formReadyCallback?: () => void;
};

export const formStructureRenderedHandler = async (formContext: FormStructureRenderedHandler, isRendered: boolean) => {
  if (isRendered) formContext.formReadyCallback?.();
};

declare const grecaptcha: Grecaptcha;

export type FormSubmittHandler<T> = {
  formValues: T;
  afterSuccess?: (payload: object, header: object) => void;
  context: FormHookInterface<T> & MultiLingual & { form: FormHook<any> };
  middleware?: (payload: object, header: object, url: string) => { header: object; payload: object; url: string };
};

export const formSubmittHandler = async <T>({ context, formValues, middleware, afterSuccess }: FormSubmittHandler<T>) => {
  try {
    context.setIsLoading(true);

    const hasAdditionalData = !!context.structure?.data?.truncatedFields && !!Object.keys(context.structure?.data?.truncatedFields)?.length;

    let additionalData: Record<string, string> = {};

    if (hasAdditionalData) {
      Object.entries(context.structure?.data?.truncatedFields as Record<string, string>).forEach(([oldKey, newKey]) => {
        if (formValues[oldKey]) additionalData[newKey] = formValues[oldKey];

        delete formValues[oldKey];
      });
    }

    let payload: Record<string, any> = { ...formValues };

    if (context.structure?.data?.extraPayload) payload = { ...payload, ...context.structure?.data?.extraPayload };

    if (hasAdditionalData) payload.additionalData = additionalData;

    if (!!context?.extraPayload) payload = { ...payload, ...context?.extraPayload };

    let header: Record<string, any> = {
      'Content-Type': 'application/json',
      'Accept-Language': context.localeLanguage || 'en',
    };

    if (!!context?.extraHeader) header = { ...header, ...context?.extraHeader };

    if (!!context.structure?.data?.extraHeader) header = { ...header, ...context.structure?.data?.extraHeader };

    let requestEndpoint = '';
    if (context.isMobileForm) {
      const token = await context.getMobileToken();

      if (token.toLowerCase().startsWith('bearer')) {
        header['Authorization'] = token;
        requestEndpoint = context.structure?.data?.requestAppUrl;
      } else {
        header['verification-token'] = token;
        requestEndpoint = context.structure?.data?.requestAppCheckUrl;
      }
    } else {
      requestEndpoint = context.structure?.data?.requestUrl;
      if (context.structure?.data?.recaptchaKey) {
        const token = await grecaptcha.execute(context.structure?.data?.recaptchaKey, { action: 'submit' });
        header['Recaptcha-Token'] = token;
      }
    }

    if (context.isDev) requestEndpoint = requestEndpoint.replaceAll('production=true', 'production=false');

    if (!requestEndpoint) {
      throw new Error('Request endpoint is not configured');
    }

    if (middleware) {
      const middlewareRes = middleware(payload, header, requestEndpoint);
      payload = { ...middlewareRes.payload };
      header = { ...middlewareRes.header };
      requestEndpoint = middlewareRes.url;
    }

    const response = await fetch(requestEndpoint, {
      headers: header,
      method: context.structure?.data?.requestMethod || 'POST',

      ...(context.structure?.data?.requestMethod?.toLowerCase() === 'get' ? {} : { body: JSON.stringify(payload) }),
    });

    if (response.ok) {
      const result = await response?.json();

      if (afterSuccess) await afterSuccess(payload, header);

      context.setSuccessCallback(result);

      setTimeout(() => {
        context.form.reset();
        context.form.rerender({ rerenderForm: true, rerenderAll: true });
      }, 100);
    } else {
      const contentType = response.headers.get('content-type');

      const errorText = contentType?.includes('application/json') ? (await response.json())?.message?.body : await response.text();

      throw new Error(errorText);
    }
  } catch (error) {
    console.error(error);

    context.setErrorCallback(error);
  } finally {
    context.setIsLoading(false);
  }
};

export const functionHooks = {
  formLanguageChange,
  formErrorHandler,
  formSubmitHandler,
  formLoadingHandler,
  formSuccessHandler,
  formDidLoadHandler,
  formGetFormHandler,
  formSubmittHandler,
  formStructureRenderedHandler,
};
