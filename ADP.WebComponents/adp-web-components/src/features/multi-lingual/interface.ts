import { AnyObjectSchema } from 'yup';

import { ComponentLocale, LanguageKeys } from './types';

export interface MultiLingual {
  language: LanguageKeys;
  locale: ComponentLocale<AnyObjectSchema>;

  componentWillLoad: () => Promise<void>;
  changeLanguage: (newLanguage: LanguageKeys) => Promise<void>;
}
