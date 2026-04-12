import { LanguageKey } from './dynamic';

export type MessageField = 'label' | 'placeholder' | 'require' | 'format' | 'regex' | 'min' | 'max';

export type Localization = Partial<Record<LanguageKey, Partial<Record<MessageField, string>>>>;

export type DefaultFieldProps = {
  step?: string;
  required?: boolean;
  localization: Localization;
  defaultValue?: string | boolean;
};

export type InputField = {
  type: 'input';
  min?: number;
  max?: number;
  regex?: number;
} & DefaultFieldProps;

export type EmailField = {
  type: 'input';
  validationType: 'email';
} & DefaultFieldProps;

export type PhoneField = {
  type: 'input';
  validationType: 'phone';
  countryCode: string | string[];
} & DefaultFieldProps;

export type DateField = {
  type: 'date';
  format: string;
  min?: number[]; // [year, month, day, hour, minute]
  max?: number[]; // [year, month, day, hour, minute]
} & DefaultFieldProps;

export type TimeField = {
  type: 'time';
  format: string;
  min?: number[]; // [year, month, day, hour, minute]
  max?: number[]; // [year, month, day, hour, minute]
} & DefaultFieldProps;
