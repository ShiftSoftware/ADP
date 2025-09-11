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
  email: string()
    .meta({ label: 'Email Address', placeholder: 'Email Address' } as FormInputMeta)
    .email('Email Address is not valid'),
  priority: string()
    .meta({ label: 'Priority', placeholder: 'Ticket Priority' } as FormInputMeta)
    .required('Ticket Priority is required.'),
  branch: string()
    .meta({ label: 'Branch', placeholder: 'Select a branch' } as FormInputMeta)
    .required('Branch is required.'),
});

export type TestDriveDemo = InferType<typeof testDriveDemoInputsValidation>;

export type TestDriveDemoFormLocale = FormLocale<typeof testDriveDemoSchema>;
