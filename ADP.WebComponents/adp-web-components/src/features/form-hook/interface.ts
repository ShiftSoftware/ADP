import { JSX } from '@stencil/core/internal';

import { FormHook } from '~features/form-hook/form-hook';
import formWrapperSchema from '~locales/forms/wrapper-type';

import { LanguageKeys } from '~features/multi-lingual';

export type FormElementStructureComponents<T> = {
  id?: string;
  class?: string;
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

export type FormElementStructure<T> = {} & FormElementStructureComponents<T>;

export interface FormHookInterface<T> {
  theme: string;
  gistId?: string;
  brandId: string;
  el: HTMLElement;
  isLoading: boolean;
  errorMessage: string;
  language: LanguageKeys;
  structure: FormElementStructure<any>;
  formSubmit: (formValues: T) => void;
  errorCallback?: (error: any) => void;
  successCallback?: (data: any) => void;
  setErrorCallback: (error: any) => void;
  setSuccessCallback: (data: any) => void;
  setIsLoading: (loading: boolean) => void;
  loadingChanges?: (loading: boolean) => void;
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

export type LocaleFormKeys = keyof typeof formWrapperSchema.fields;

export type Params = {
  [key: string]: any;
  formLocaleName: LocaleFormKeys;
};

export type FormFieldParams = Record<string, Params>;

export interface FormElement {
  reset: (newValue?: unknown) => void;
}

export type Subscribers = { name: string; context: FormElement }[];

export type FormElementMapperFunctionProps<T> = { form: FormHook<any>; isLoading: boolean; props: any; language: LanguageKeys; locale: T };

type FormElementMapperFunction<T> = (ElementContext: FormElementMapperFunctionProps<T>) => JSX.Element;

export type FormElementMapper<T, FORM_LOCALE> = {
  [K in keyof T]: FormElementMapperFunction<FORM_LOCALE>;
} & {
  submit: FormElementMapperFunction<FORM_LOCALE>;
};
