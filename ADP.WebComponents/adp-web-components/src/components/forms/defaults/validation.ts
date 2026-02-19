import { object, Schema, string } from 'yup';
import { FormInputMeta } from '~features/form-hook';
import validateVin from '~lib/validate-vin';

export const label = (name: string) => `${name}-label`;
export const format = (name: string) => `${name}-format`;
export const require = (name: string) => `${name}-require`;
export const placeholder = (name: string) => `${name}-placeholder`;

export const meta = (name): FormInputMeta => ({ label: label(name), placeholder: placeholder(name) });

export const condition = (name: string) => `$${name}Required`;

export const y = { label, format, require, condition, placeholder, meta };

export const getFormValidations = (stateObject: Record<string, any>, extraFields: Record<string, Schema> = {}) => {
  return object({
    name: string()
      .meta(meta('name'))
      .when(condition('name'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(require('name')).min(3, format('name')),
      }),

    lastName: string()
      .meta(meta('lastName'))
      .when(condition('lastName'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(require('lastName')).min(3, format('lastName')),
      }),

    message: string()
      .meta(meta('message'))
      .when(condition('message'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(require('message')).min(10, format('message')),
      }),

    email: string()
      .meta(meta('email'))
      .when(condition('email'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(require('email')),
      })
      .email(format('email')),

    vin: string()
      .meta(meta('vin'))
      .when(condition('vin'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(require('vin')).test(format('vin'), format('vin'), a => validateVin(a?.toUpperCase())),
      }),

    phone: string()
      .meta(meta('phone'))
      .when(condition('phone'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(require('phone')).test(format('phone'), format('phone'), () => stateObject?.phoneValidator?.geIsValidPhoneNumber()),
      }),

    vehicle: string()
      .meta(meta('vehicle'))
      .when(y.condition('vehicle'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(y.require('vehicle')),
      }),

    companyBranchId: string()
      .meta(meta('companyBranchId'))
      .when(y.condition('companyBranchId'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(y.require('companyBranchId')),
      }),

    cityId: string()
      .meta(meta('cityId'))
      .when(y.condition('cityId'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(y.require('cityId')),
      }),

    date: string()
      .meta(meta('date'))
      .when(condition('date'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(require('date')),
      }),

    time: string()
      .meta(meta('time'))
      .when(condition('time'), {
        is: true,
        otherwise: schema => schema.optional(),
        then: schema => schema.required(require('time')),
      }),

    ...extraFields,
  });
};
