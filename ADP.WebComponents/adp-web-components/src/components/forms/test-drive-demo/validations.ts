import { InferType, object, string } from 'yup';

import { FormLocale } from '~features/multi-lingual';
import { FormInputMeta, getPhoneValidator } from '~features/form-hook';

import testDriveDemoSchema from '~locales/forms/testDriveDemo/type';

export const phoneValidator = getPhoneValidator();

export const testDriveDemoInputsValidation = object({
  name: string()
    .meta({ label: 'Full Name', placeholder: 'Enter a full name' } as FormInputMeta)
    .required('Full name is required.')
    .min(3, 'Must be 3 characters or more.'),
  phone: string()
    .meta({ label: 'Phone number', placeholder: 'Phone number' } as FormInputMeta)
    .required('Phone number is required.')
    .test('libphonenumber-validation', 'Please enter a valid phone number', () => phoneValidator.isValid()),
});

export type TestDriveDemo = InferType<typeof testDriveDemoInputsValidation>;

export type TestDriveDemoFormLocale = FormLocale<typeof testDriveDemoSchema>;
