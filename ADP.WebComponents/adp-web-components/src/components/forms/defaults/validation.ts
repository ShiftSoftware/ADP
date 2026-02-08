import { string } from 'yup';
import { FormInputMeta } from '~features/form-hook';
import validateVin from '~lib/validate-vin';

export const label = (name: string) => `${name}-label`;
export const format = (name: string) => `${name}-format`;
export const require = (name: string) => `${name}-require`;
export const placeholder = (name: string) => `${name}-placeholder`;

export const condition = (name: string) => `$${name}Required`;

export const y = { label, format, require, condition, placeholder };

export const getDefaultValidaations = (stateObject: Record<string, any>) => ({
  name: string()
    .meta({ label: label('name'), placeholder: placeholder('name') } as FormInputMeta)
    .when(condition('name'), {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required(require('name')).min(3, format('name')),
    }),

  lastName: string()
    .meta({ label: label('lastName'), placeholder: placeholder('lastName') } as FormInputMeta)
    .when(condition('lastName'), {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required(require('lastName')).min(3, format('lastName')),
    }),

  message: string()
    .meta({ label: label('message'), placeholder: placeholder('message') } as FormInputMeta)
    .when(condition('message'), {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required(require('message')).min(10, format('message')),
    }),

  email: string()
    .meta({ label: label('email'), placeholder: placeholder('email') } as FormInputMeta)
    .when(condition('email'), {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required(require('email')),
    })
    .email(format('email')),

  vin: string()
    .meta({ label: label('vin'), placeholder: placeholder('vin') } as FormInputMeta)
    .when(condition('vin'), {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required(require('vin')).test(format('vin'), format('vin'), a => validateVin(a?.toUpperCase())),
    }),

  phone: string()
    .meta({ label: label('phone'), placeholder: placeholder('phone') } as FormInputMeta)
    .when(condition('phone'), {
      is: true,
      otherwise: schema => schema.optional(),
      then: schema => schema.required(require('phone')).test(format('phone'), format('phone'), () => stateObject?.phoneValidator?.geIsValidPhoneNumber()),
    }),
});
