import { InferType, object, string } from 'yup';

import { FormLocale } from '~features/multi-lingual';
import contactUsSchema from '~locales/forms/contactUs/type';
import { FormInputMeta, getPhoneValidator } from '~features/form-hook';

export const phoneValidator = getPhoneValidator();

export const contactUsInputsValidation = object({
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

export type ContactUs = InferType<typeof contactUsInputsValidation>;

export type ContactUsFormLocale = FormLocale<typeof contactUsSchema>;
