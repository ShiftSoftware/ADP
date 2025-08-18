import { contactUsElementNames } from './element-mapper';
import { FormElementStructure } from '~features/form-hook';

const tiq: FormElementStructure<contactUsElementNames> = {
  tag: 'div',
  id: 'container',
  children: [
    { tag: 'div', id: 'inputs_wrapper', children: ['name', 'email', 'cityId', 'phone', 'generalTicketType'] },
    { name: 'message', id: 'message' },
    { tag: 'div', id: 'recaptcha_container' },
    { name: 'submit', id: 'Submit' },
  ],
};

export const ContactUsStructures = { tiq };
