import { InferType, object, string } from 'yup';

import { FormInputMeta } from '~features/form-hook';
import { phoneValidator } from './element-mapper';

export const generalInquiryInputsValidation = object({
  name: string()
    .meta({ label: 'Full name', placeholder: 'Full name' } as FormInputMeta)
    .required('Full name is required')
    .min(3, 'Proper Name is required'),
  lastName: string()
    .meta({ label: 'Last name', placeholder: 'Last name' } as FormInputMeta)
    .required('Last name is required')
    .min(3, 'Proper Last name is required'),
  message: string()
    .meta({ label: 'Message', placeholder: 'Please enter a message' } as FormInputMeta)
    .min(3, 'Must be 10 characters or more'),
  email: string()
    .optional()
    .meta({ label: 'Email', placeholder: 'YourName@example.com' } as FormInputMeta)
    .email('Must be a valid email'),
  phone: string()
    .meta({ label: 'Phone number', placeholder: 'Phone number' } as FormInputMeta)
    .required('Phone number is required')
    .test('phone-validation', 'Phone number format invalid', () => phoneValidator?.geIsValidPhoneNumber()),
  companyBranchId: string()
    .optional()
    .meta({ label: 'Branch', placeholder: 'Please select a branch' } as FormInputMeta)
    .required('Branch is required'),
});

export type GeneralInquiry = InferType<typeof generalInquiryInputsValidation>;
