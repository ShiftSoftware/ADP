import { y } from '../../../components/forms/defaults/validation';
import { getNestedValue } from '~lib/get-nested-value';

export type FormInputMeta = {
  label: string;
  placeholder: string;
};

export type FormInputLocalization<T = {}> = {
  [lang: string]: {
    label?: string;
    placeholder?: string;
    require?: string;
    format?: string;
  } & T;
};

export const getInputLocalization = (context: any, meta: FormInputMeta, errorMessage: string) => {
  const [locale, language] = context?.form?.getFormLocale();

  const label = context?.localization[language]?.label || getNestedValue(locale, meta?.label) || meta?.label;
  const placeholder = context?.localization[language]?.placeholder || getNestedValue(locale, meta?.placeholder) || meta?.placeholder;

  let localizationErrorMessage = '';

  if (errorMessage?.endsWith(y.format(''))) localizationErrorMessage = context?.localization[language]?.format;
  else if (errorMessage?.endsWith(y.require(''))) localizationErrorMessage = context?.localization[language]?.require;

  const errorTextMessage = localizationErrorMessage || locale[errorMessage] || errorMessage;

  return { label, placeholder, errorTextMessage };
};
