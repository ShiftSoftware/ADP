import type { InputType } from './input-type';

export const textType: InputType = {
  inputMode: 'text',
  placeholder: () => '',
  normalize: value => (value === null || value === undefined ? '' : String(value)),
  display: value => value,
  read: text => ({ value: text }),
};
