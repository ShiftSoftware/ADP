import { h } from '@stencil/core';

import { FormElementMapper } from '~features/form-hook';

import { VehicleQuotation, VehicleQuotationFormLocale, phoneValidator } from './validations';

export const vehicleQuotationElements: FormElementMapper<VehicleQuotation, VehicleQuotationFormLocale> = {
  submit: ({ form, isLoading, props }) => <form-submit {...props} form={form} isLoading={isLoading} />,

  name: ({ props, form }) => <form-input {...props} form={form} name="name" />,

  phone: ({ form, props }) => <form-phone-number {...props} form={form} name="phone" defaultValue={phoneValidator.default} validator={phoneValidator} />,
} as const;

export type vehicleQuotationElementNames = keyof typeof vehicleQuotationElements;
