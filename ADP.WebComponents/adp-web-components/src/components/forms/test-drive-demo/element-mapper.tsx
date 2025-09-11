import { h } from '@stencil/core';

import { FormElementMapper } from '~features/form-hook';

import { TestDriveDemo, TestDriveDemoFormLocale, phoneValidator } from './validations';

type AdditionalFields = 'submit';

export const testDriveDemoElements: FormElementMapper<TestDriveDemo, TestDriveDemoFormLocale, AdditionalFields> = {
  submit: ({ props }) => <form-submit {...props} />,

  name: ({ props }) => <form-input {...props} />,

  phone: ({ props, isLoading }) => <form-phone-number {...props} isLoading={isLoading} defaultValue={phoneValidator.default} validator={phoneValidator} />,
} as const;

export type testDriveDemoElementNames = keyof typeof testDriveDemoElements;
