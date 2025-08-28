import { InferType, object, string } from 'yup';

import { FormLocale } from '~features/multi-lingual';
import { FormInputMeta, getPhoneValidator } from '~features/form-hook';

import vehicleQuotationSchema from '~locales/forms/vehicleQuotation/type';

export const phoneValidator = getPhoneValidator();

export const vehicleQuotationInputsValidation = object({
  name: string()
    .meta({ label: 'Full Name', placeholder: 'Enter a full name' } as FormInputMeta)
    .required('Full name is required.')
    .min(3, 'Must be 3 characters or more.'),
  phone: string()
    .meta({ label: 'Phone number', placeholder: 'Phone number' } as FormInputMeta)
    .required('Phone number is required.')
    .test('libphonenumber-validation', 'Please enter a valid phone number', () => phoneValidator.isValid()),
});

export type VehicleQuotation = InferType<typeof vehicleQuotationInputsValidation>;

export type VehicleQuotationFormLocale = FormLocale<typeof vehicleQuotationSchema>;
