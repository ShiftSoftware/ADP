import { vehicleQuotationElementNames } from './element-mapper';
import { FormElementStructure } from '~features/form-hook';

const tiq: FormElementStructure<vehicleQuotationElementNames> = {
  data: {
    brandId: 'kWrKw',
    vehicleApi: 'https://tiq-publications-functions.azurewebsites.net/api/models',
  },
  tag: 'div',
  id: 'container',
  children: [
    'choose',
    { tag: 'div', id: 'vehicle-wrapper', children: ['vehicle', 'vehicleImage'] },
    { tag: 'hr' },
    'contact information',
    { tag: 'div', id: 'inputs_wrapper', children: ['name', 'phone'] },
    { name: 'submit', id: 'Submit' },
  ],
};

export const VehicleQuotationStructures = { tiq };
