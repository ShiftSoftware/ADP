import { vehicleQuotationElementNames } from './element-mapper';
import { FormElementStructure } from '~features/form-hook';

const tiq: FormElementStructure<vehicleQuotationElementNames> = {
  tag: 'div',
  id: 'container',
  children: [
    { tag: 'div', id: 'inputs_wrapper', children: ['name', 'phone'] },
    { name: 'submit', id: 'Submit' },
  ],
};

export const VehicleQuotationStructures = { tiq };
