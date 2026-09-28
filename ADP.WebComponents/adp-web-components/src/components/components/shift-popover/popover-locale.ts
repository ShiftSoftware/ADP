import { InferType } from 'yup';

import popoverSchema from '~locales/popover/type';
import ar from '../../../locales/popover/ar.json';
import en from '../../../locales/popover/en.json';
import ku from '../../../locales/popover/ku.json';
import ru from '../../../locales/popover/ru.json';

export type PopoverStrings = InferType<typeof popoverSchema>;

const STRINGS: Record<string, PopoverStrings> = { en, ar, ku, ru };

export const popoverStrings = (language: string): PopoverStrings => STRINGS[language] ?? STRINGS.en;
