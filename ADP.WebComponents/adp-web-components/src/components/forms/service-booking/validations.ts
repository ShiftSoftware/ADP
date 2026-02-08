import { InferType, object, string } from 'yup';

import { FormInputMeta } from '~features/form-hook';
import { phoneValidator } from './element-mapper';

export const serviceBookingInputsValidation = object({
  vehicle: string()
    .meta({ label: 'Vehicle', placeholder: 'Please select a Vehicle' } as FormInputMeta)
    .required('This field is required.'),
  name: string()
    .meta({ label: 'Full name', placeholder: 'Full name' } as FormInputMeta)
    .required('Full name is required')
    .min(3, 'Full name minimum'),
  phone: string()
    .meta({ label: 'Phone number', placeholder: 'Phone number' } as FormInputMeta)
    .required('Phone number is required')
    .test('phone-validation', 'Phone number format invalid', () => phoneValidator?.geIsValidPhoneNumber()),
  dealer: string()
    .meta({ label: 'Dealer', placeholder: 'Select a dealer' } as FormInputMeta)
    .required('Dealer is required'),
  city: string()
    .meta({ label: 'City', placeholder: 'Select a City' } as FormInputMeta)
    .required('This field is required.'),
  serviceType: string()
    .meta({ label: 'Service Type', placeholder: 'Select a service type' } as FormInputMeta)
    .when('$serviceTypeRequired', {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required('This field is required.'),
    }),
  contactTime: string()
    .meta({ label: 'Preferred contact time', placeholder: 'Please select a time' } as FormInputMeta)
    .when('$contactTimeRequired', {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required('This field is required.'),
    }),
  preferredDate: string()
    .meta({ label: 'Preferred Date', placeholder: 'Select preferred date' } as FormInputMeta)
    .when('$preferredDateRequired', {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required('This field is required.'),
    }),
  mileage: string()
    .meta({ label: 'Current Mileage', placeholder: 'Enter current mileage' } as FormInputMeta)
    .when('$mileageRequired', {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required('This field is required.'),
    }),
  additionalNotes: string()
    .meta({ label: 'Additional Notes', placeholder: 'Any additional notes or requests' } as FormInputMeta)
    .optional(),
});

export type ServiceBooking = InferType<typeof serviceBookingInputsValidation>;
