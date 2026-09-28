import { dateType } from './date/date-type';
import type { InputType } from './input-type';
import { textType } from './text-type';

export type ShiftInputType = 'text' | 'date';

// A new type is one module here; the field reads everything type-specific through InputType.
export const INPUT_TYPES: Record<ShiftInputType, InputType> = {
  text: textType,
  date: dateType,
};
