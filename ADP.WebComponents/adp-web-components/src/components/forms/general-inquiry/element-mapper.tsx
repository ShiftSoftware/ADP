import { h } from '@stencil/core';

import { FormElementMapper, FormSelectFetcher, FormSelectItem, getPhoneValidator, PhoneValidator } from '~features/form-hook';

import { GeneralInquiry } from './validations';

export let phoneValidator: PhoneValidator;

type AdditionalFields = 'submit';

export const generalInquiryElements: FormElementMapper<GeneralInquiry, AdditionalFields> = {
  submit: ({ props }) => <form-submit {...props} />,

  name: ({ props }) => {
    return <form-input {...props} />;
  },

  lastName: ({ props }) => {
    return <form-input {...props} />;
  },

  email: ({ props }) => {
    return <form-input type="email" {...props} />;
  },

  message: ({ props }) => {
    return <form-text-area {...props} />;
  },

  phone: ({ props, isLoading }) => {
    if (!phoneValidator) {
      phoneValidator = getPhoneValidator(props?.countryCode || '');
    }

    return <form-phone-number defaultValue={phoneValidator.default} {...props} isLoading={isLoading} validator={phoneValidator} />;
  },

  companyBranchId: ({ form, language, props }) => {
    // @ts-ignore
    const externalBranch = !!form?.context?.getBranchId() || !!form?.context?.getHideBranch();

    if (!!externalBranch) return;

    const fetcher: FormSelectFetcher = async ({ signal }): Promise<FormSelectItem[]> => {
      const branchEndpoint = form.context.structure?.data.branchApi as string;

      const response = await fetch(branchEndpoint, { signal, headers: { 'Accept-Language': language } });

      const branches = (await response.json()) as { ID?: string; Name?: string }[];

      const parsedOptions = branches.map(branch => ({ label: branch?.Name, value: branch?.ID })) as FormSelectItem[];

      return parsedOptions;
    };

    return <form-select {...props} searchable fetcher={fetcher} language={language} />;
  },
};

export type generalInquiryElementNames = keyof typeof generalInquiryElements;
