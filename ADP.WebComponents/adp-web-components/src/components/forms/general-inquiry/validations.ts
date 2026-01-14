import { InferType, object, string } from 'yup';

import { FormInputMeta } from '~features/form-hook';
import { phoneValidator } from './element-mapper';

export const generalInquiryInputsValidation = object({
  name: string()
    .meta({ label: 'Full name', placeholder: 'Full name' } as FormInputMeta)
    .required('Full name is required')
    .min(3, 'Proper Name is required'),
  phone: string()
    .meta({ label: 'Phone number', placeholder: 'Phone number' } as FormInputMeta)
    .required('Phone number is required')
    .test('phone-validation', 'Phone number format invalid', () => phoneValidator?.geIsValidPhoneNumber()),
});

export type GeneralInquiry = InferType<typeof generalInquiryInputsValidation>;
