import { h } from '@stencil/core';

import { FormSelectFetcher, FormSelectItem, FormElementMapper } from '~features/form-hook';

import { CITY_ENDPOINT } from '~api/urls';
import { Demo, DemoFormLocale, phoneValidator } from './validations';

export const demoElements: FormElementMapper<Demo, DemoFormLocale> = {
  submit: ({ form, isLoading, props }) => <form-submit {...props} form={form} isLoading={isLoading} />,

  name: ({ props, form }) => <form-input {...props} form={form} name="name" />,

  email: ({ form, props }) => <form-input {...props} form={form} name="email" type="email" />,

  phone: ({ form, props }) => <form-phone-number {...props} form={form} name="phone" defaultValue={phoneValidator.default} validator={phoneValidator} />,

  message: ({ form, props }) => <form-text-area {...props} form={form} name="message" />,

  confirmPolicy: ({ form, props }) => <form-checkbox {...props} form={form} name="confirmPolicy" />,

  ageConfirmation: ({ form, props }) => <form-switch {...props} form={form} name="ageConfirmation" />,

  generalTicketType: ({ form, language, props }) => {
    const fetcher: FormSelectFetcher<DemoFormLocale> = async ({ locale }): Promise<FormSelectItem[]> => {
      const generalInquiryTypes: FormSelectItem[] = [
        {
          value: 'GeneralInquiry',
          label: locale.ticketTypes.generalInquiry,
        },
        {
          value: 'Complaint',
          label: locale.ticketTypes.complaint,
        },
      ];

      return generalInquiryTypes;
    };

    return <form-select {...props} form={form} fetcher={fetcher} language={language} name="generalTicketType" />;
  },

  cityId: ({ form, language, props }) => {
    const fetcher: FormSelectFetcher<DemoFormLocale> = async ({ language, signal }): Promise<FormSelectItem[]> => {
      const response = await fetch(CITY_ENDPOINT, { signal, headers: { 'Accept-Language': language } });

      const arrayRes = (await response.json()) as { Name: string; ID: string; IntegrationId: string }[];

      const selectItems = arrayRes.map(item => ({ label: item.Name, value: item.ID })) as FormSelectItem[];

      return selectItems;
    };

    return <form-select {...props} name="cityId" searchable form={form} fetcher={fetcher} language={language} />;
  },
} as const;

export type demoElementNames = keyof typeof demoElements;
