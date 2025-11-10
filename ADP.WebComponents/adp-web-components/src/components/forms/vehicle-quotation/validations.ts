import { InferType, object, string } from 'yup';

import { FormLocale } from '~features/multi-lingual';
import { FormInputMeta, getPhoneValidator } from '~features/form-hook';

import vehicleQuotationSchema from '~locales/forms/vehicleQuotation/type';

export const phoneValidator = getPhoneValidator();

export const vehicleQuotationInputsValidation = object({
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
    .test('libphonenumber-validation', 'Phone number format invalid', () => phoneValidator.isValid()),
  dealer: string()
    .meta({ label: 'Dealer', placeholder: 'Select a dealer' } as FormInputMeta)
    .required('Dealer is required'),
  city: string()
    .meta({ label: 'City', placeholder: 'Select a City' } as FormInputMeta)
    .required('Select a City'),
  paymentType: string().meta({ label: 'Preferred purchasing method', placeholder: 'Select the purchasing method' } as FormInputMeta),
  contactTime: string().meta({ label: 'Preferred contact time', placeholder: 'Please select a time' } as FormInputMeta),
  ownVehicle: string().meta({ label: 'Do you own a vehicle?', placeholder: 'Do you own a vehicle?' } as FormInputMeta),
  currentVehicleBrand: string()
    .meta({ label: 'Your current vehicle', placeholder: 'Your current vehicle' } as FormInputMeta)
    .when('ownVehicle', {
      is: (val: string) => val === 'yes',
      then: schema => schema.required('Please answer this field'),
      otherwise: schema => schema.optional(),
    }),
  currentVehicleModel: string()
    .meta({ label: 'Vehicle Model', placeholder: 'Vehicle Model' } as FormInputMeta)
    .when(['ownVehicle', 'currentVehicleBrand'], {
      is: (ownVehicle: string, brand: string) => ownVehicle === 'yes' && brand !== 'Other',
      then: schema => schema.required('Please answer this field'),
      otherwise: schema => schema.optional(),
    }),
});

export type VehicleQuotation = InferType<typeof vehicleQuotationInputsValidation>;

export type VehicleQuotationFormLocale = FormLocale<typeof vehicleQuotationSchema>;
