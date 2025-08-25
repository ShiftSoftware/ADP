import { bool, InferType, object, string } from 'yup';

import { FormLocale } from '~features/multi-lingual';
import { FormInputMeta, getPhoneValidator } from '~features/form-hook';
import demoSchema from '~locales/forms/demo/type';

export const phoneValidator = getPhoneValidator();

export const demoInputsValidation = object({
  confirmPolicy: bool()
    .meta({ label: 'confirmPolicy' } as FormInputMeta)
    .required(),
  cityId: string().meta({ label: 'city', placeholder: 'selectCity' } as FormInputMeta),
  email: string()
    .meta({ label: 'emailAddress', placeholder: 'emailAddress' } as FormInputMeta)
    .email('emailAddressNotValid'),
  message: string()
    .meta({ label: 'writeAMessage', placeholder: 'leaveUsMessage' } as FormInputMeta)
    .required('messageIsRequired'),
  generalTicketType: string()
    .meta({ label: 'inquiryType', placeholder: 'selectInquiryType' } as FormInputMeta)
    .required('inquiryTypeIsRequired'),
  name: string()
    .meta({ label: 'fullName', placeholder: 'fullName' } as FormInputMeta)
    .required('fullNameIsRequired')
    .min(3, 'fullNameMinimum'),
  phone: string()
    .meta({ label: 'phoneNumber', placeholder: 'phoneNumber' } as FormInputMeta)
    .required('phoneNumberIsRequired')
    .test('libphonenumber-validation', 'phoneNumberFormatInvalid', () => phoneValidator.isValid()),
});

export type Demo = InferType<typeof demoInputsValidation>;

export type DemoFormLocale = FormLocale<typeof demoSchema>;
