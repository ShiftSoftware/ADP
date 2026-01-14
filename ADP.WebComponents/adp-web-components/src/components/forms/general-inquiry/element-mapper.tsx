import { h } from '@stencil/core';

import { FormElementMapper, getPhoneValidator, PhoneValidator } from '~features/form-hook';

import { GeneralInquiry } from './validations';

export let phoneValidator: PhoneValidator;

type AdditionalFields = 'submit';

export const generalInquiryElements: FormElementMapper<GeneralInquiry, AdditionalFields> = {
  submit: ({ props }) => <form-submit {...props} />,

  name: ({ props }) => {
    return <form-input {...props} />;
  },

  phone: ({ props, isLoading }) => {
    if (!phoneValidator) {
      phoneValidator = getPhoneValidator(props?.countryCode || '');
    }

    return <form-phone-number defaultValue={phoneValidator.default} {...props} isLoading={isLoading} validator={phoneValidator} />;
  },
};

export type generalInquiryElementNames = keyof typeof generalInquiryElements;
