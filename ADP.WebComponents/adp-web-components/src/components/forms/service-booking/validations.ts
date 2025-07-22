import { AsYouType } from 'libphonenumber-js';
import { InferType, object, string } from 'yup';

export const phoneValidator = new AsYouType('IQ') as AsYouType & {
  default: string;
  metadata: {
    numberingPlan: { metadata: [string] };
  };
};

phoneValidator.default = '+' + phoneValidator.metadata.numberingPlan.metadata[0];

phoneValidator.input(phoneValidator.default);

export const contactUsSchema = object({
  cityId: string(),
  email: string().email('emailAddressNotValid'),
  message: string().required('messageIsRequired'),
  generalTicketType: string().required('inquiryTypeIsRequired'),
  name: string().required('fullNameIsRequired').min(3, 'fullNameMinimum'),
  phone: string()
    .required('phoneNumberIsRequired')
    .test('libphonenumber-validation', 'phoneNumberFormatInvalid', () => phoneValidator.isValid()),
});

export type ContactUs = InferType<typeof contactUsSchema>;
