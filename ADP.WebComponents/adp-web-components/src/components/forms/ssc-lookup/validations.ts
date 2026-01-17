import { InferType, object, string } from 'yup';

import { FormInputMeta } from '~features/form-hook';
import { phoneValidator } from './element-mapper';
import validateVin from '~lib/validate-vin';

export const SSCLookupInputsValidation = object({
  name: string()
    .meta({ label: 'Full name', placeholder: 'Full name' } as FormInputMeta)
    .required('Full name is required')
    .min(3, 'Proper Name is required'),
  phone: string()
    .meta({ label: 'Phone number', placeholder: 'Phone number' } as FormInputMeta)
    .required('Phone number is required')
    .test('phone-validation', 'Phone number format invalid', () => phoneValidator?.geIsValidPhoneNumber()),
  vin: string()
    .meta({ label: 'VIN / Frame No.', placeholder: 'VIN / Frame No.' } as FormInputMeta)
    .required('Vin is required')
    .test('vin-validation', 'Vin is invalid', a => validateVin(a)),
});

export type SSCLookup = InferType<typeof SSCLookupInputsValidation>;
