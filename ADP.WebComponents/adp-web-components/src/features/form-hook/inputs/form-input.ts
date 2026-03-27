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
    minMessage: string;
    maxMessage: string;
    betweenMessage: string;
  } & T;
};

export const getInputLocalization = (context: any, meta: FormInputMeta, errorMessage: string) => {
  const [locale, language] = context?.form?.getFormLocale();

  const label = context?.localization?.[language]?.label || getNestedValue(locale, meta?.label) || meta?.label;
  const placeholder = context?.localization?.[language]?.placeholder || getNestedValue(locale, meta?.placeholder) || meta?.placeholder;

  let localizationErrorMessage = '';

  if (errorMessage?.endsWith(y.format(''))) localizationErrorMessage = context?.localization?.[language]?.format;
  if (errorMessage?.endsWith(y.size(''))) localizationErrorMessage = context?.localization?.[language]?.size;
  else if (errorMessage?.endsWith(y.require(''))) localizationErrorMessage = context?.localization?.[language]?.require;
  else if (errorMessage === 'minMessage') localizationErrorMessage = context?.withSlots(context?.localization?.[language]?.minMessage || 'Min date is $minDate$');
  else if (errorMessage === 'maxMessage') localizationErrorMessage = context?.withSlots(context?.localization?.[language]?.maxMessage || 'Max date is $maxDate$');
  else if (errorMessage === 'betweenMessage')
    localizationErrorMessage = context?.withSlots(context?.localization?.[language]?.betweenMessage || 'Must be between $minDate$ and $maxDate$');

  const errorTextMessage = localizationErrorMessage || locale[errorMessage] || errorMessage;

  return { label, placeholder, errorTextMessage };
};
