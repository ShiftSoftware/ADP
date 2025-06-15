import { JSX } from '@stencil/core/internal';

import { FormHook } from '~lib/form-hook';
import { LanguageKeys } from './locale';
import formWrapperSchema from '~locales/forms/wrapper-type';

export interface FormHookInterface<T> {
  el: HTMLElement;
  structure: string;
  renderControl: {};
  isLoading: boolean;
  errorMessage: string;
  language: LanguageKeys;
  formSubmit: (formValues: T) => void;
}

export type FormSelectItem = {
  value: string;
  label: string;
};

export type FormSelectFetcher = (language: string, signal: AbortSignal) => Promise<FormSelectItem[]>;

export type ValidationType = 'onSubmit' | 'always';

export interface Field {
  name: string;
  isError: boolean;
  disabled: boolean;
  isRequired: boolean;
  errorMessage: string;
  continuousValidation: boolean;
}

export type FieldControllers = Record<string, Field>;

export interface FormStateOptions {
  validationType?: ValidationType;
}

type FormElementStructure = {
  id: string;
  class: string;
  children: FormElementStructure[];
  element: '' | 'div' | 'slot' | 'submit' | 'button' | string;
};

export type StructureArray = (string | StructureArray)[];

export type StructureObject = FormElementStructure | null;

export type LocaleFormKeys = keyof typeof formWrapperSchema.fields;

export type Params = {
  [key: string]: any;
  formLocaleName: LocaleFormKeys;
};

export type FormFieldParams = Record<string, Params>;

export interface FormElement {
  reset: (newValue?: string) => void;
}

export type Subscribers = { name: string; context: FormElement }[];

type FormElementMapperFunction = (ElementContext: { form: FormHook<any>; isLoading: boolean; structureElement: StructureObject; language: LanguageKeys }) => JSX.Element;

export type FormElementMapper<T> = {
  [K in keyof T]: FormElementMapperFunction;
} & {
  submit: FormElementMapperFunction;
};
