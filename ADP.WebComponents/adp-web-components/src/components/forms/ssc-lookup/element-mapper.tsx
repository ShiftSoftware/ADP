import { h } from '@stencil/core';

import { FormElementMapper, getPhoneValidator, PhoneValidator } from '~features/form-hook';

import { SSCLookup } from './validations';

export let phoneValidator: PhoneValidator;

type AdditionalFields = 'submit';

export const SSCLookupElements: FormElementMapper<SSCLookup, AdditionalFields> = {
  submit: ({ props }) => <form-submit {...props} />,

  name: ({ props }) => {
    return <form-input {...props} />;
  },

  vin: ({ props }) => {
    return <form-vin-input {...props} />;
  },

  phone: ({ props, isLoading }) => {
    if (!phoneValidator) {
      phoneValidator = getPhoneValidator(props?.countryCode || '');
    }

    return <form-phone-number defaultValue={phoneValidator.default} {...props} isLoading={isLoading} validator={phoneValidator} />;
  },
};

export type SSCLookupElementNames = keyof typeof SSCLookupElements;
