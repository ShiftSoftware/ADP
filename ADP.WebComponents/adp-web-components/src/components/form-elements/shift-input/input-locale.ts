import { InferType } from 'yup';

import inputSchema from '~locales/input/type';
import ar from '../../../locales/input/ar.json';
import en from '../../../locales/input/en.json';
import ku from '../../../locales/input/ku.json';
import ru from '../../../locales/input/ru.json';

export type InputStrings = InferType<typeof inputSchema>;

// Bundled like the calendar's names: button labels and typed-entry errors must be there on first paint.
const STRINGS: Record<string, InputStrings> = { en, ar, ku, ru };

export const inputStrings = (language: string): InputStrings => STRINGS[language] ?? STRINGS.en;

export const fill = (template: string, values: Record<string, string>): string => template.replace(/\{(\w+)\}/g, (_, key) => values[key] ?? '');
