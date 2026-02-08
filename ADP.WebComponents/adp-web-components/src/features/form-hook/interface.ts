import { JSX } from '@stencil/core/internal';

import { FormHook } from '~features/form-hook/form-hook';

import { LanguageKeys } from '~features/multi-lingual';

export type FormElementStructureComponents<T> = {
  id?: string;
  class?: string;
  staticValue?: any;
  isHidden?: boolean;
  isDisabled?: boolean;
  children?: (FormElementStructureComponents<T> | T)[];
} & (
  | {
      tag?: string;
      name: T;
    }
  | {
      tag: string;
      name?: T;
    }
);

export type FormElementStructure<T> = {
  data?: Record<string, any>;
  requiredContext?: Record<string, boolean>;
} & FormElementStructureComponents<T>;

export interface FormHookInterface<T> {
  locale?: any;
  gistId?: string;
  el: HTMLElement;
  isDev?: boolean;
  isLoading: boolean;
  extraHeader?: object;
  errorMessage?: string;
  extraPayload?: object;
  isMobileForm?: boolean;
  localeLanguage?: string;
  language?: LanguageKeys;
  renderedFields?: string[];
  structureRendered?: boolean;
  getMobileToken?: () => string;
  structure?: FormElementStructure<any>;
  formSubmit: (formValues: T) => void;
  setErrorCallback?: (error: any) => void;
  setSuccessCallback?: (data: any) => void;
  setIsLoading?: (loading: boolean) => void;
  loadingChanges?: (loading: boolean) => void;
  errorCallback?: (error: any, message?: string) => void;
  successCallback?: (data: any, message?: string) => void;
}

export type ValidationType = 'onSubmit' | 'always';

export interface Field<MetaType> {
  name: string;
  meta?: MetaType;
  isError: boolean;
  disabled: boolean;
  isRequired: boolean;
  errorMessage: string;
  continuousValidation: boolean;
}

export type FieldControllers = Record<string, Field<any>>;

export interface FormStateOptions {
  validationType?: ValidationType;
}

export type LocaleFormKeys = string;

export type Params = {
  [key: string]: any;
  formLocaleName: LocaleFormKeys;
};

export type FormFieldParams = Record<string, Params>;

export interface FormElement {
  reset: (newValue?: unknown) => void;
}

export type Subscribers = { name: string; context: FormElement }[];

export type WatchCallback = (props: { form: FormHook<any>; values: Record<string, any> }) => void;

export type Watchers = Record<string, WatchCallback>;

export type FormElementMapperFunctionProps<T> = { form: FormHook<any>; isLoading: boolean; props: any; language: LanguageKeys; locale: T };

type FormElementMapperFunction<T> = (ElementContext: FormElementMapperFunctionProps<T>) => JSX.Element;

export type FormElementMapper<T, Extra extends string = never> = {
  [K in keyof T]: FormElementMapperFunction<any>;
} & {
  [K in Extra]: FormElementMapperFunction<any>;
};
