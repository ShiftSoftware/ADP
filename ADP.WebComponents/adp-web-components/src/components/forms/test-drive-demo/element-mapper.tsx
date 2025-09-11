import { forceUpdate, h } from '@stencil/core';

import { FormElementMapper, FormSelectFetcher, FormSelectItem } from '~features/form-hook';

import { TestDriveDemo, TestDriveDemoFormLocale, phoneValidator } from './validations';

type AdditionalFields = 'submit';

export const testDriveDemoElements: FormElementMapper<TestDriveDemo, TestDriveDemoFormLocale, AdditionalFields> = {
  submit: ({ props }) => <form-submit {...props} />,

  name: ({ props }) => <form-input {...props} />,

  email: ({ props }) => <form-input {...props} type="email" />,

  phone: ({ props, isLoading }) => <form-phone-number {...props} isLoading={isLoading} defaultValue={phoneValidator.default} validator={phoneValidator} />,

  priority: ({ language, props }) => {
    const fetcher: FormSelectFetcher<TestDriveDemoFormLocale> = async ({}): Promise<FormSelectItem[]> => {
      return [
        {
          value: 'Low',
          label: 'Low',
        },
        {
          value: 'High',
          label: 'High',
        },
      ] as FormSelectItem[];
    };

    return <form-select {...props} clearable searchable fetcher={fetcher} language={language} />;
  },
  branch: ({ form, language, props }) => {
    const fetcher: FormSelectFetcher<TestDriveDemoFormLocale> = async ({ signal }): Promise<FormSelectItem[]> => {
      const branchEndpoint = form.context.structure?.data?.companyBranchUrl as string;

      const response = await fetch(branchEndpoint, { signal, headers: { 'Accept-Language': language } });

      form.context['branchList'] = await response.json();

      forceUpdate(form.formStructure);

      return form.context['branchList'].map(vehicle => ({
        label: vehicle.Name,
        value: `${vehicle.ID}`,
      })) as FormSelectItem[];
    };

    const params = new URLSearchParams(window.location.search);

    const defaultValue = params.get(form.context.structure.data?.vehicleIdQueryParam);

    return <form-select {...props} defaultValue={defaultValue} searchable fetcher={fetcher} language={language} />;
  },
} as const;

export type testDriveDemoElementNames = keyof typeof testDriveDemoElements;
